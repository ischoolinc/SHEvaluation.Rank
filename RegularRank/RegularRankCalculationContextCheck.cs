using System;
using System.Collections.Generic;

namespace SHEvaluation.Rank.RegularRank
{
    public static class RegularRankCalculationContextCheck
    {
        public static int Main(string[] args)
        {
            List<string> errors = new List<string>();
            errors.AddRange(CheckPeriodAndCurrentHistoricalTarget());
            errors.AddRange(CheckExamIdentity());
            errors.AddRange(CheckSourceDoesNotReplaceTarget());
            errors.AddRange(CheckSubjectsAndStaleLoad());
            errors.AddRange(CheckWorkCopy());
            errors.AddRange(CheckCollectionsAndInvalidIds());
            if (errors.Count == 0)
            {
                Console.WriteLine("regular-rank-context-check: ok");
                return 0;
            }
            foreach (string error in errors)
                Console.WriteLine(error);
            return 1;
        }

        private static List<string> CheckPeriodAndCurrentHistoricalTarget()
        {
            List<string> errors = new List<string>();
            RegularRankCalculationContext context = RegularRankCalculationContext.CreateDefault("115", "1");
            Expect(errors, context.TargetSchoolYear == "115" && context.TargetSemester == "1", "預設目標期間應採用有效的系統期間");
            Expect(errors, context.SetTargetPeriod("113", "9").Length > 0 && context.TargetSchoolYear == "115" && context.TargetSemester == "1", "不合法學期不能改寫目標期間，也不能改成 1");
            Expect(errors, context.SetTargetPeriod("0113", "2").Length > 0, "學年度不能接受前導零");
            Expect(errors, context.SetTargetPeriod("113", "2") == "", "三種來源都應可指定歷年目標");
            Expect(errors, context.SetCurrentSource() == "", "目前來源可以選擇歷年目標");
            Expect(errors, context.QuerySchoolYear == "113" && context.QuerySemester == "2", "查詢期間使用目標 113-2");
            Expect(errors, context.SystemSchoolYear == "115" && context.QuerySchoolYear != context.SystemSchoolYear, "沒有修課時不能回查系統期間");

            RegularRankCalculationContext invalid = RegularRankCalculationContext.CreateDefault("115", "3");
            Expect(errors, invalid.TargetSchoolYear.Length == 0 && invalid.TargetSemester.Length == 0, "系統學期不合法時不預設成 1");
            return errors;
        }

        private static List<string> CheckExamIdentity()
        {
            List<string> errors = new List<string>();
            List<RegularRankExamChoice> exams = new List<RegularRankExamChoice>();
            exams.Add(Choice("20", "第二次定期評量"));
            exams.Add(Choice("10", "第二次定期評量"));
            exams.Add(Choice("30", "第一次定期評量"));

            RegularRankExamResolution ambiguous = RegularRankExamResolution.Resolve("", "第二次定期評量", exams);
            Expect(errors, !ambiguous.Resolved && ambiguous.ExamId.Length == 0, "同名考試不能任取第一筆或最後一筆");
            RegularRankExamResolution selected = RegularRankExamResolution.Resolve("10", "第二次定期評量", exams);
            Expect(errors, selected.Resolved && selected.ExamId == "10", "已指定的 exam ID 可以分辨同名考試");
            RegularRankExamResolution unique = RegularRankExamResolution.Resolve("", "第一次定期評量", exams);
            Expect(errors, unique.Resolved && unique.ExamId == "30", "名稱只對到一個 exam ID 時採用該 ID");

            exams.Add(Choice("10", "第二次定期評量"));
            RegularRankExamResolution duplicateId = RegularRankExamResolution.Resolve("10", "", exams);
            Expect(errors, !duplicateId.Resolved, "重複 exam ID 不能任取一筆");

            RegularRankCalculationContext context = ReadyCurrent("115", "1");
            string before = context.TargetExamId;
            Expect(errors, context.SetTargetExam(ambiguous).Length > 0 && context.TargetExamId == before, "歧義結果不能改掉已选定的目標考試");
            return errors;
        }

        private static List<string> CheckSourceDoesNotReplaceTarget()
        {
            List<string> errors = new List<string>();
            RegularRankCalculationContext context = ReadyCurrent("114", "2");
            Expect(errors, context.SetTargetExam(RegularRankExamResolution.Resolve("42", "", new RegularRankExamChoice[] { Choice("42", "第二次定期評量") })) == "", "目標考試應以 exam ID 寫入");
            RegularRankExamResolution sourceExam = RegularRankExamResolution.Resolve("", "第一次定期評量", new RegularRankExamChoice[] { Choice("7", "第一次定期評量") });
            Expect(errors, context.SetHistorySource("91", "113", "2", sourceExam) == "", "歷史來源可以不同於目標期間與考試");
            Expect(errors, context.TargetSchoolYear == "114" && context.TargetSemester == "2" && context.TargetExamId == "42", "來源不能覆寫目標期間與目標考試");
            Expect(errors, context.SourceSchoolYear == "113" && context.SourceExamId == "7" && context.SourceBatchId == "91", "來源批次、期間與考試要另外保存");
            Expect(errors, context.SetHistorySource("91", "113", "2", RegularRankExamResolution.Resolve("", "第一次定期評量", new RegularRankExamChoice[] { Choice("7", "第一次定期評量"), Choice("8", "第一次定期評量") })).Length > 0, "來源考試無法唯一識別時阻擋");
            Expect(errors, context.SourceExamId == "7", "阻擋後保留原本已唯一識別的來源考試");
            Expect(errors, context.SetExcelSource("") .Length > 0, "Excel 來源缺少匯入識別時阻擋");
            Expect(errors, context.SetExcelSource("file-a") == "" && context.SourceExamId.Length == 0 && context.TargetExamId == "42", "Excel 識別不取代目標考試");
            return errors;
        }

        private static List<string> CheckSubjectsAndStaleLoad()
        {
            List<string> errors = new List<string>();
            RegularRankCalculationContext context = ReadyCurrent("113", "2");
            Expect(errors, context.MarkMotherGroupLoaded().Length > 0, "尚未選擇年級與考試時不能載入");
            Expect(errors, context.SetGradeYears(new int[] { 2, 1, 2 }) == "", "年級可指定");
            Expect(errors, context.GradeYears.Count == 2 && context.GradeYears[0] == 2 && context.GradeYears[1] == 1, "年級保留選擇順序並去掉重複");
            Expect(errors, context.SetTargetExam(RegularRankExamResolution.Resolve("42", "", new RegularRankExamChoice[] { Choice("42", "第二次定期評量") })) == "", "目標考試寫入");
            Expect(errors, context.MarkMotherGroupLoaded() == "", "條件完整後可載入母群");
            Expect(errors, context.ApplySubjectCandidates(
                new string[] { "數學", "國文", "英文" },
                new string[] { "歷史" },
                new string[] { "化學" },
                new string[] { "英文" },
                new string[0],
                new string[0]) == "", "第一次科目載入成功");
            Expect(errors, Same(context.GeneralSubjects.Available, "數學", "國文", "英文"), "科目維持傳入順序");
            Expect(errors, Same(context.GeneralSubjects.Checked, "數學", "國文"), "新科目依不採計設定初始化");
            Expect(errors, context.ConfirmSubjects() == "" && context.CanCreateRequest() == "", "確認三組科目後才可建立 request");

            Expect(errors, context.SetTargetExam(RegularRankExamResolution.Resolve("99", "", new RegularRankExamChoice[] { Choice("99", "期末") })) == "", "變更目標考試");
            Expect(errors, context.LoadState == RegularRankLoadState.Stale && context.CanCreateRequest().Length > 0, "變更考試後舊載入過期並禁止下一步");
            bool threw = false;
            try
            {
                context.CreateRequest();
            }
            catch (InvalidOperationException)
            {
                threw = true;
            }
            Expect(errors, threw, "過期條件不能建立 request");
            Expect(errors, context.MarkSubjectLoadFailed() == "" && context.LoadState == RegularRankLoadState.Failed, "重載失敗要記成失敗");
            Expect(errors, context.CanCreateRequest().IndexOf("不能用上一份科目") >= 0, "失敗時不能用舊科目搭配新考試");

            Expect(errors, context.MarkMotherGroupLoaded() == "", "失敗後可重新載入母群");
            string beforeGeneral = context.GeneralSubjects.Checked.Count > 0 ? context.GeneralSubjects.Checked[0] : "";
            Expect(errors, context.ApplySubjectCandidates(
                new string[] { "國文" },
                new string[] { "歷史" },
                new string[] { "化學", "化學" },
                new string[0],
                new string[0],
                new string[0]).Length > 0, "同一組科目重複時阻擋");
            Expect(errors, context.GeneralSubjects.Checked.Count > 0 && context.GeneralSubjects.Checked[0] == beforeGeneral, "一組失敗時不改寫其他組已接受的科目");

            Expect(errors, context.ApplySubjectCandidates(
                new string[] { "英文", "數學", "物理" },
                new string[] { "歷史", "地理" },
                new string[] { "化學" },
                new string[] { "物理" },
                new string[0],
                new string[0]) == "", "重載科目成功");
            Expect(errors, Same(context.GeneralSubjects.Checked, "數學"), "既有勾選以科目名稱留交集，先前未勾選與新的不採計科目都不勾選");
            Expect(errors, Same(context.GeneralSubjects.Added, "物理") && Same(context.GeneralSubjects.Removed, "國文"), "新增與移除的科目要分開保留");
            Expect(errors, !context.SubjectsConfirmed, "重載科目後不自動進入確認");
            Expect(errors, context.ConfirmSubjects() == "", "確認新的三組科目");
            RegularRankConfirmRequest request = context.CreateRequest();
            Expect(errors, request.QueryExamId == "99" && request.TargetExamId == "99", "request 使用確認後的目標 exam ID");
            Expect(errors, Same(request.GeneralSubjects, "數學") && Same(request.Tag1Subjects, "歷史", "地理"), "request 分開保存三組已勾選科目");
            return errors;
        }

        private static List<string> CheckWorkCopy()
        {
            List<string> errors = new List<string>();
            RegularRankCalculationContext context = ReadyCurrent("114", "2");
            context.SetGradeYears(new int[] { 1 });
            context.SetTargetExam(RegularRankExamResolution.Resolve("42", "", new RegularRankExamChoice[] { Choice("42", "第二次定期評量") }));
            context.SetHistorySource("91", "113", "2", RegularRankExamResolution.Resolve("7", "", new RegularRankExamChoice[] { Choice("7", "第一次定期評量") }));
            context.MarkMotherGroupLoaded();
            context.ApplySubjectCandidates(new string[] { "國文" }, new string[] { "歷史" }, new string[] { "化學" }, new string[0], new string[0], new string[0]);
            context.ConfirmSubjects();
            RegularRankCalculationContext copy = context.CreateWorkCopy();
            bool blocked = false;
            try
            {
                ((IList<string>)context.GeneralSubjects.Checked).Add("不應進入副本");
            }
            catch (NotSupportedException)
            {
                blocked = true;
            }
            Expect(errors, blocked, "工作副本與原物件的科目清單都不能直接修改");
            Expect(errors, context.SetGradeYears(new int[] { 3 }) == "", "原物件可經受控方法改年級");
            Expect(errors, copy.TargetSchoolYear == "114" && copy.TargetSemester == "2", "工作副本保留確認時的目標期間");
            Expect(errors, copy.SourceSchoolYear == "113" && copy.SourceExamId == "7" && copy.TargetExamId == "42", "工作副本同時保留來源與目標考試");
            Expect(errors, copy.GradeYears.Count == 1 && copy.GradeYears[0] == 1, "工作副本的年級與原物件分開");
            Expect(errors, copy.GeneralSubjects.Checked.Count == 1 && copy.GeneralSubjects.Checked[0] == "國文", "工作副本的科目清單與原物件分開");
            Expect(errors, copy.CanCreateRequest() == "", "已確認的工作副本仍可建立 request");
            return errors;
        }

        private static List<string> CheckCollectionsAndInvalidIds()
        {
            List<string> errors = new List<string>();
            RegularRankCalculationContext context = ReadyCurrent("114", "2");
            List<int> grades = new List<int>();
            grades.Add(1);
            Expect(errors, context.SetGradeYears(grades) == "", "年級寫入");
            grades.Add(-1);
            grades[0] = 9;
            Expect(errors, context.GradeYears.Count == 1 && context.GradeYears[0] == 1, "修改輸入的年級集合不影響已保存資料");
            context.SetTargetExam(RegularRankExamResolution.Resolve("42", "", new RegularRankExamChoice[] { Choice("42", "第二次定期評量") }));
            context.MarkMotherGroupLoaded();
            List<string> available = new List<string>();
            available.Add("國文");
            available.Add("英文");
            Expect(errors, context.ApplySubjectCandidates(available, new string[] { "歷史" }, new string[] { "化學" }, new string[0], new string[0], new string[0]) == "", "科目載入");
            available.Add("NeverLoaded");
            available[0] = "";
            Expect(errors, context.GeneralSubjects.Available.Count == 2 && context.GeneralSubjects.Available[0] == "國文", "修改輸入的科目集合不影響已保存資料");
            Expect(errors, context.ConfirmSubjects() == "", "確認科目");

            bool gradeBlocked = false;
            bool subjectBlocked = false;
            try
            {
                ((IList<int>)context.GradeYears).Add(-1);
            }
            catch (NotSupportedException)
            {
                gradeBlocked = true;
            }
            try
            {
                ((IList<string>)context.GeneralSubjects.Checked).Add("NeverLoaded");
            }
            catch (NotSupportedException)
            {
                subjectBlocked = true;
            }
            Expect(errors, gradeBlocked && subjectBlocked && context.CanCreateRequest() == "", "外部不能修改 context 的年級或勾選科目");
            RegularRankConfirmRequest request = context.CreateRequest();
            bool requestBlocked = false;
            try
            {
                ((IList<string>)request.GeneralSubjects).Add("NeverLoaded");
            }
            catch (NotSupportedException)
            {
                requestBlocked = true;
            }
            Expect(errors, requestBlocked && request.GeneralSubjects.Count == 2, "外部不能修改 request 的科目集合");

            Expect(errors, context.SetCheckedSubjects(RegularRankSubjectSet.General, new string[] { "NeverLoaded" }).Length > 0 && context.CanCreateRequest() == "", "未載入科目不能勾選，也不撤銷原確認");
            Expect(errors, context.SetCheckedSubjects(RegularRankSubjectSet.General, new string[] { "英文" }) == "" && context.CanCreateRequest().Length > 0, "受控修改勾選後撤銷科目確認");
            Expect(errors, context.ConfirmSubjects() == "" && context.CanCreateRequest() == "", "重新確認勾選後恢復");
            Expect(errors, context.SetCheckedSubjects(RegularRankSubjectSet.General, new string[0]) == "", "不要求每一組都至少勾一科");
            Expect(errors, context.ConfirmSubjects() == "" && context.CreateRequest().GeneralSubjects.Count == 0, "一般組可以沒有勾選科目");

            Expect(errors, context.SetGradeYears(new int[] { -1 }).Length > 0 && context.GradeYears.Count == 1 && context.GradeYears[0] == 1 && context.CanCreateRequest() == "", "無效年級不破壞原值");
            Expect(errors, context.SetGradeYears(new int[] { 1, 2 }) == "" && context.LoadState == RegularRankLoadState.Stale && context.CanCreateRequest().Length > 0, "年級變更使載入過期");

            context.SetGradeYears(new int[] { 1 });
            context.MarkMotherGroupLoaded();
            context.ApplySubjectCandidates(new string[] { "國文", "英文" }, new string[] { "歷史" }, new string[] { "化學" }, new string[0], new string[0], new string[0]);
            context.ConfirmSubjects();
            string duplicate = context.ApplySubjectCandidates(new string[] { "國文" }, new string[] { "歷史" }, new string[] { "化學", "化學" }, new string[0], new string[0], new string[0]);
            bool createBlocked = false;
            try
            {
                context.CreateRequest();
            }
            catch (InvalidOperationException)
            {
                createBlocked = true;
            }
            Expect(errors, duplicate.Length > 0 && context.CanCreateRequest().Length > 0 && createBlocked, "科目更新失敗後不能建立 request");
            Expect(errors, context.GeneralSubjects.Available.Count == 2 && context.GeneralSubjects.Available[1] == "英文", "失敗時保留舊清單");
            Expect(errors, context.ApplySubjectCandidates(new string[] { "國文" }, new string[] { "歷史" }, new string[] { "化學" }, new string[0], new string[0], new string[0]) == "", "重新載入科目成功");
            Expect(errors, context.CanCreateRequest().Length > 0 && context.ConfirmSubjects() == "" && context.CanCreateRequest() == "", "重新載入後須再確認才恢復");

            RegularRankExamResolution invalidExam = RegularRankExamResolution.Resolve("not-a-number", "", new RegularRankExamChoice[] { Choice("not-a-number", "壞考試") });
            Expect(errors, !invalidExam.Resolved, "Resolve 不把無效 exam ID 標為成功");
            RegularRankExamResolution namedInvalid = RegularRankExamResolution.Resolve("", "壞考試", new RegularRankExamChoice[] { Choice("not-a-number", "壞考試") });
            Expect(errors, !namedInvalid.Resolved, "唯一名稱配到無效 exam ID 時也不成功");
            Expect(errors, !RegularRankExamResolution.Resolve("2147483648", "", new RegularRankExamChoice[] { Choice("2147483648", "溢位") }).Resolved, "超過 INT 範圍的 exam ID 被拒絕");
            Expect(errors, !RegularRankExamResolution.Resolve("01", "", new RegularRankExamChoice[] { Choice("01", "前導零") }).Resolved, "有前導零的 exam ID 被拒絕");
            string beforeExam = context.TargetExamId;
            Expect(errors, context.SetTargetExam(invalidExam).Length > 0 && context.TargetExamId == beforeExam, "無效目標考試不破壞先前有效值");
            string beforeBatch = context.SourceBatchId;
            Expect(errors, context.SetHistorySource("9223372036854775808", "113", "2", RegularRankExamResolution.Resolve("7", "", new RegularRankExamChoice[] { Choice("7", "第一次定期評量") })).Length > 0 && context.SourceBatchId == beforeBatch, "溢位的歷史批次 ID 被拒絕");
            Expect(errors, context.SetHistorySource("not-a-number", "113", "2", RegularRankExamResolution.Resolve("7", "", new RegularRankExamChoice[] { Choice("7", "第一次定期評量") })).Length > 0, "非整數的歷史批次 ID 被拒絕");
            Expect(errors, context.SetHistorySource("0", "113", "2", RegularRankExamResolution.Resolve("7", "", new RegularRankExamChoice[] { Choice("7", "第一次定期評量") })).Length > 0, "非正數的歷史批次 ID 被拒絕");
            Expect(errors, context.SetHistorySource("91", "113", "2", RegularRankExamResolution.Resolve("not-a-number", "", new RegularRankExamChoice[] { Choice("not-a-number", "壞考試") })).Length > 0 && context.TargetExamId == beforeExam, "無效來源 exam ID 被拒絕且不改目標");
            return errors;
        }

        private static RegularRankCalculationContext ReadyCurrent(string schoolYear, string semester)
        {
            RegularRankCalculationContext context = RegularRankCalculationContext.CreateDefault("115", "1");
            context.SetTargetPeriod(schoolYear, semester);
            context.SetCurrentSource();
            return context;
        }

        private static RegularRankExamChoice Choice(string id, string name)
        {
            RegularRankExamChoice choice = new RegularRankExamChoice();
            choice.Id = id;
            choice.Name = name;
            return choice;
        }

        private static bool Same(IList<string> values, params string[] expected)
        {
            if (values == null || values.Count != expected.Length)
                return false;
            for (int i = 0; i < expected.Length; i++)
            {
                if (values[i] != expected[i])
                    return false;
            }
            return true;
        }

        private static void Expect(List<string> errors, bool condition, string message)
        {
            if (!condition)
                errors.Add(message);
        }
    }
}
