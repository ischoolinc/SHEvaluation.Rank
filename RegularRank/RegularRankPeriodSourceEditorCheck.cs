using System;
using System.Collections.Generic;

namespace SHEvaluation.Rank.RegularRank
{
    public static class RegularRankPeriodSourceEditorCheck
    {
        public static int Main(string[] args)
        {
            List<string> errors = new List<string>();
            errors.AddRange(CheckCurrentSourceKeepsHistoricalTarget());
            errors.AddRange(CheckSourceDoesNotReplaceTarget());
            errors.AddRange(CheckAmbiguousExamIsBlocked());
            errors.AddRange(CheckExcelAndUnknownSource());
            errors.AddRange(CheckConditionChangeBlocksNext());
            errors.AddRange(CheckSubjectChangeText());
            if (errors.Count == 0)
            {
                Console.WriteLine("regular-rank-period-source-check: ok");
                return 0;
            }
            foreach (string error in errors)
                Console.WriteLine(error);
            return 1;
        }

        private static List<string> CheckCurrentSourceKeepsHistoricalTarget()
        {
            List<string> errors = new List<string>();
            RegularRankPeriodSourceEnablement current = RegularRankPeriodSourceEnablement.For(RegularRankSourceKind.Current);
            RegularRankPeriodSourceEnablement excel = RegularRankPeriodSourceEnablement.For(RegularRankSourceKind.Excel);
            RegularRankPeriodSourceEnablement history = RegularRankPeriodSourceEnablement.For(RegularRankSourceKind.History);
            Expect(errors, current.TargetSchoolYear && current.TargetSemester && excel.TargetSchoolYear && history.TargetSchoolYear, "三種來源的目標期間都可編輯");
            Expect(errors, current.CurrentSourceNote && !current.SourceSchoolYear && !excel.CurrentSourceNote && history.SourceExam, "目前來源顯示指定期間說明，歷史來源才開放來源期間與考試");
            Expect(errors, RegularRankPeriodSourceEditor.CurrentSourceNote.Contains("目前學生母群") && RegularRankPeriodSourceEditor.CurrentSourceNote.Contains("指定期間"), "目前來源說明標明目前母群與指定期間成績");

            RegularRankCalculationContext context = RegularRankCalculationContext.CreateDefault("115", "1");
            string error = RegularRankPeriodSourceEditor.Apply(context, Selection("113", "2", "30", "第一次定期評量", RegularRankPeriodSourceEditor.CurrentSourceText, "113", "2", "", "", ""));
            Expect(errors, error == "" && context.TargetSchoolYear == "113" && context.TargetSemester == "2", "目前來源可指定歷年目標");
            Expect(errors, context.QuerySchoolYear == "113" && context.QuerySemester == "2" && context.SystemSchoolYear == "115", "查詢期間使用目標，不回退系統期間");
            Expect(errors, context.SourceKind == RegularRankSourceKind.Current && context.SourceSchoolYear.Length == 0 && context.SourceExamId.Length == 0, "目前來源不把畫面上的來源期間寫進條件");
            Expect(errors, context.CanCreateRequest().Length > 0, "尚未載入母群與科目時不能進入下一步");
            Expect(errors, RegularRankPeriodSourceEditor.SubjectQueryNotConnected.Contains("不查資料") && RegularRankPeriodSourceEditor.SubjectQueryNotConnected.Contains("舊科目清單"), "尚未取得資料庫證據前，不能把舊說明改成已完成驗收");
            Expect(errors, RegularRankPeriodSourceEditor.DeferredSourceMessage.Contains("歷史") && RegularRankPeriodSourceEditor.DeferredSourceMessage.Contains("不沿用"), "未完成歷史來源不能沿用上一份科目");
            return errors;
        }

        private static List<string> CheckSourceDoesNotReplaceTarget()
        {
            List<string> errors = new List<string>();
            RegularRankCalculationContext context = RegularRankCalculationContext.CreateDefault("115", "1");
            RegularRankPeriodSourceSelection selection = Selection("114", "2", "42", "第二次定期評量", RegularRankPeriodSourceEditor.HistorySourceText, "113", "2", "", "7", "第一次定期評量");
            string blocked = RegularRankPeriodSourceEditor.Apply(context, selection);
            Expect(errors, blocked.Length > 0, "沒有歷史批次時阻擋載入");
            Expect(errors, context.TargetSchoolYear == "114" && context.TargetSemester == "2" && context.TargetExamId == "42", "來源期間與來源考試不覆寫目標");
            Expect(errors, context.SourceKind == RegularRankSourceKind.History && context.SourceExamId.Length == 0 && context.SourceBatchId.Length == 0, "批次尚未載入時不保存來源考試");

            selection.SourceBatchId = "91";
            string stored = RegularRankPeriodSourceEditor.Apply(context, selection);
            Expect(errors, stored == "", "批次與來源考試都能唯一識別時可保存歷史來源");
            Expect(errors, context.TargetExamId == "42" && context.TargetSchoolYear == "114" && context.SourceSchoolYear == "113" && context.SourceExamId == "7" && context.SourceBatchId == "91", "來源與目標同時保留");

            selection.SourceExamId = "";
            selection.SourceExamName = "第一次定期評量";
            selection.SourceExamChoices = new RegularRankExamChoice[] { Choice("7", "第一次定期評量"), Choice("8", "第一次定期評量") };
            string ambiguous = RegularRankPeriodSourceEditor.Apply(context, selection);
            Expect(errors, ambiguous.Length > 0 && context.SourceExamId.Length == 0 && context.TargetExamId == "42", "來源考試歧義時清空來源識別且保留目標");
            return errors;
        }

        private static List<string> CheckAmbiguousExamIsBlocked()
        {
            List<string> errors = new List<string>();
            List<RegularRankExamChoice> choices = new List<RegularRankExamChoice>();
            choices.Add(Choice("20", "第二次定期評量"));
            choices.Add(Choice("10", "第二次定期評量"));
            choices.Add(Choice("30", "第一次定期評量"));
            IList<RegularRankExamListItem> items = RegularRankPeriodSourceEditor.BuildExamItems(choices);
            Expect(errors, items.Count == 3 && items[0].Display.Contains("ID 20") && items[1].Display.Contains("ID 10") && items[2].Display == "第一次定期評量", "同名考試顯示 ID，唯一名稱保持原名");
            Expect(errors, RegularRankPeriodSourceEditor.IndexOfUniqueName(items, "第二次定期評量") < 0, "同名考試不以名稱自動選第一筆或最後一筆");
            Expect(errors, RegularRankPeriodSourceEditor.IndexOfUniqueName(items, "第一次定期評量") == 2, "唯一名稱可以對到單一項目");

            RegularRankCalculationContext context = RegularRankCalculationContext.CreateDefault("115", "1");
            RegularRankPeriodSourceEditor.Apply(context, Selection("115", "1", "30", "第一次定期評量", RegularRankPeriodSourceEditor.CurrentSourceText, "", "", "", "", ""));
            RegularRankPeriodSourceSelection ambiguous = Selection("115", "1", "", "第二次定期評量", RegularRankPeriodSourceEditor.CurrentSourceText, "", "", "", "", "");
            ambiguous.TargetExamChoices = choices;
            string error = RegularRankPeriodSourceEditor.Apply(context, ambiguous);
            Expect(errors, error.Length > 0 && context.TargetExamId.Length == 0 && context.TargetExamName.Length == 0, "目標考試改成歧義名稱時清除舊 exam ID");
            return errors;
        }

        private static List<string> CheckExcelAndUnknownSource()
        {
            List<string> errors = new List<string>();
            RegularRankCalculationContext context = RegularRankCalculationContext.CreateDefault("115", "1");
            RegularRankPeriodSourceSelection excel = Selection("113", "2", "30", "第一次定期評量", RegularRankPeriodSourceEditor.ExcelSourceText, "115", "1", "", "30", "第一次定期評量");
            excel.ExcelIdentity = "   ";
            string missing = RegularRankPeriodSourceEditor.Apply(context, excel);
            Expect(errors, missing.Length > 0 && context.SourceKind == RegularRankSourceKind.Excel && context.ExcelIdentity.Length == 0, "空白 Excel 識別不能當成已匯入");
            Expect(errors, context.TargetSchoolYear == "113" && context.SourceSchoolYear.Length == 0, "Excel 不採用來源期間欄位");

            excel.SourceText = "依目前學生資料產生";
            string semesterName = RegularRankPeriodSourceEditor.Apply(context, excel);
            Expect(errors, semesterName.Length > 0 && context.SourceKind == RegularRankSourceKind.Excel, "不把學期來源名稱當成定期目前來源");

            excel.SourceText = "歷史學期成績排名";
            string semesterHistory = RegularRankPeriodSourceEditor.Apply(context, excel);
            Expect(errors, semesterHistory.Length > 0, "不把學期歷史名稱當成定期歷史來源");
            return errors;
        }

        private static List<string> CheckConditionChangeBlocksNext()
        {
            List<string> errors = new List<string>();
            RegularRankCalculationContext context = RegularRankCalculationContext.CreateDefault("115", "1");
            RegularRankPeriodSourceSelection selection = Selection("115", "1", "30", "第一次定期評量", RegularRankPeriodSourceEditor.CurrentSourceText, "", "", "", "", "");
            selection.GradeYears = new int[] { 1, 2 };
            Expect(errors, RegularRankPeriodSourceEditor.Apply(context, selection) == "", "目前來源條件可寫入");
            Expect(errors, context.MarkMotherGroupLoaded() == "", "母群載入標記");
            Expect(errors, context.ApplySubjectCandidates(new string[] { "國文" }, new string[] { "歷史" }, new string[] { "化學" }, new string[0], new string[0], new string[0]) == "", "科目候選");
            Expect(errors, context.ConfirmSubjects() == "" && context.CanCreateRequest() == "", "確認後才可建立 request");

            selection.TargetSchoolYear = "113";
            string changed = RegularRankPeriodSourceEditor.Apply(context, selection);
            Expect(errors, changed == "" && context.LoadState == RegularRankLoadState.Stale && context.CanCreateRequest().Length > 0, "變更目標期間後禁止下一步");
            Expect(errors, context.QuerySchoolYear == "113" && context.TargetExamId == "30", "過期後仍保留新目標期間與原考試");

            selection.GradeYears = new int[] { 2 };
            RegularRankPeriodSourceEditor.Apply(context, selection);
            Expect(errors, context.LoadState == RegularRankLoadState.Stale, "變更年級後維持過期");
            return errors;
        }

        private static List<string> CheckSubjectChangeText()
        {
            List<string> errors = new List<string>();
            RegularRankCalculationContext context = RegularRankCalculationContext.CreateDefault("115", "1");
            context.SetTargetPeriod("115", "1");
            context.SetTargetExam(RegularRankExamResolution.Resolve("30", "", new RegularRankExamChoice[] { Choice("30", "第一次定期評量") }));
            context.SetCurrentSource();
            context.SetGradeYears(new int[] { 1 });
            context.MarkMotherGroupLoaded();
            context.ApplySubjectCandidates(new string[] { "國文", "英文" }, new string[] { "歷史" }, new string[] { "化學" }, new string[0], new string[0], new string[0]);
            string first = RegularRankPeriodSourceEditor.DescribeSubjectChanges(context);
            Expect(errors, first.Contains("一般科目新增：國文、英文") && first.Contains("類別一科目新增：歷史") && first.Contains("類別二科目新增：化學"), "新科目要出現在畫面說明");

            context.ConfirmSubjects();
            context.MarkMotherGroupLoaded();
            context.ApplySubjectCandidates(new string[] { "英文", "數學" }, new string[] { "歷史" }, new string[] { "化學" }, new string[0], new string[0], new string[0]);
            string next = RegularRankPeriodSourceEditor.DescribeSubjectChanges(context);
            Expect(errors, next.Contains("一般科目新增：數學") && next.Contains("移除：國文"), "科目交集的新增與移除都要說明");
            return errors;
        }

        private static RegularRankPeriodSourceSelection Selection(
            string targetYear,
            string targetSemester,
            string targetExamId,
            string targetExamName,
            string sourceText,
            string sourceYear,
            string sourceSemester,
            string batchId,
            string sourceExamId,
            string sourceExamName)
        {
            RegularRankPeriodSourceSelection selection = new RegularRankPeriodSourceSelection();
            selection.TargetSchoolYear = targetYear;
            selection.TargetSemester = targetSemester;
            selection.TargetExamId = targetExamId;
            selection.TargetExamName = targetExamName;
            selection.TargetExamChoices = new RegularRankExamChoice[]
            {
                Choice("42", "第二次定期評量"),
                Choice("30", "第一次定期評量"),
                Choice("7", "第一次定期評量")
            };
            selection.SourceText = sourceText;
            selection.SourceSchoolYear = sourceYear;
            selection.SourceSemester = sourceSemester;
            selection.SourceBatchId = batchId;
            selection.SourceExamId = sourceExamId;
            selection.SourceExamName = sourceExamName;
            selection.SourceExamChoices = new RegularRankExamChoice[]
            {
                Choice("7", "第一次定期評量"),
                Choice("42", "第二次定期評量")
            };
            selection.ExcelIdentity = "";
            selection.GradeYears = new int[] { 1 };
            selection.ExcludedCategory = "";
            selection.RankTag1 = "";
            selection.RankTag2 = "";
            return selection;
        }

        private static RegularRankExamChoice Choice(string id, string name)
        {
            RegularRankExamChoice choice = new RegularRankExamChoice();
            choice.Id = id;
            choice.Name = name;
            return choice;
        }

        private static void Expect(List<string> errors, bool condition, string message)
        {
            if (!condition)
                errors.Add(message);
        }
    }
}
