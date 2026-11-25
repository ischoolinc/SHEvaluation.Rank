using System.Collections.Generic;
using System.Text;

namespace SHEvaluation.Rank.RegularRank
{
    public sealed class RegularRankPeriodSourceSelection
    {
        public string TargetSchoolYear { get; set; }
        public string TargetSemester { get; set; }
        public string TargetExamId { get; set; }
        public string TargetExamName { get; set; }
        public IList<RegularRankExamChoice> TargetExamChoices { get; set; }
        public string SourceText { get; set; }
        public string SourceSchoolYear { get; set; }
        public string SourceSemester { get; set; }
        public string SourceBatchId { get; set; }
        public string SourceExamId { get; set; }
        public string SourceExamName { get; set; }
        public IList<RegularRankExamChoice> SourceExamChoices { get; set; }
        public string ExcelIdentity { get; set; }
        public IList<int> GradeYears { get; set; }
        public string ExcludedCategory { get; set; }
        public string RankTag1 { get; set; }
        public string RankTag2 { get; set; }
    }

    public sealed class RegularRankPeriodSourceEnablement
    {
        public bool TargetSchoolYear { get; private set; }
        public bool TargetSemester { get; private set; }
        public bool SourceSchoolYear { get; private set; }
        public bool SourceSemester { get; private set; }
        public bool SourceExam { get; private set; }
        public bool SourceBatch { get; private set; }
        public bool ExcelIdentity { get; private set; }
        public bool CurrentSourceNote { get; private set; }

        public static RegularRankPeriodSourceEnablement For(RegularRankSourceKind kind)
        {
            RegularRankPeriodSourceEnablement item = new RegularRankPeriodSourceEnablement();
            item.TargetSchoolYear = true;
            item.TargetSemester = true;
            bool history = kind == RegularRankSourceKind.History;
            item.SourceSchoolYear = history;
            item.SourceSemester = history;
            item.SourceExam = history;
            item.SourceBatch = history;
            item.ExcelIdentity = kind == RegularRankSourceKind.Excel;
            item.CurrentSourceNote = kind == RegularRankSourceKind.Current;
            return item;
        }
    }

    public sealed class RegularRankExamListItem
    {
        public RegularRankExamListItem(string id, string name, string display)
        {
            Id = id ?? "";
            Name = name ?? "";
            Display = display ?? "";
        }

        public string Id { get; private set; }
        public string Name { get; private set; }
        public string Display { get; private set; }

        public override string ToString()
        {
            return Display;
        }
    }

    public sealed class RegularRankBatchListItem
    {
        public RegularRankBatchListItem(string id, string display)
        {
            Id = id ?? "";
            Display = display ?? "";
        }

        public string Id { get; private set; }
        public string Display { get; private set; }

        public override string ToString()
        {
            return Display;
        }
    }

    public static class RegularRankPeriodSourceEditor
    {
        public const string CurrentSourceText = "目前學生";
        public const string ExcelSourceText = "Excel";
        public const string HistorySourceText = "歷史固定排名";
        public const string CurrentSourceNote = "依目前學生母群計算指定期間成績";
        public const string SubjectQueryNotConnected = "母群與科目查詢尚未接入。這次不查資料，也不沿用系統期間的舊科目清單。";
        public const string DeferredSourceMessage = "歷史固定排名仍在整合中，尚未載入批次。本次沒有載入母群，也不沿用上一份科目。";

        public static string SourceText(RegularRankSourceKind kind)
        {
            if (kind == RegularRankSourceKind.History)
                return HistorySourceText;
            if (kind == RegularRankSourceKind.Excel)
                return ExcelSourceText;
            return CurrentSourceText;
        }

        public static bool TryReadSource(string text, out RegularRankSourceKind kind, out string error)
        {
            string value = (text ?? "").Trim();
            if (value == CurrentSourceText)
            {
                kind = RegularRankSourceKind.Current;
                error = "";
                return true;
            }
            if (value == ExcelSourceText)
            {
                kind = RegularRankSourceKind.Excel;
                error = "";
                return true;
            }
            if (value == HistorySourceText)
            {
                kind = RegularRankSourceKind.History;
                error = "";
                return true;
            }
            kind = RegularRankSourceKind.Current;
            error = "無法辨識母群來源。";
            return false;
        }

        public static IList<RegularRankExamListItem> BuildExamItems(IList<RegularRankExamChoice> choices)
        {
            List<RegularRankExamListItem> items = new List<RegularRankExamListItem>();
            Dictionary<string, int> nameCounts = new Dictionary<string, int>();
            if (choices != null)
            {
                foreach (RegularRankExamChoice choice in choices)
                {
                    if (choice == null)
                        continue;
                    string name = (choice.Name ?? "").Trim();
                    if (!nameCounts.ContainsKey(name))
                        nameCounts[name] = 0;
                    nameCounts[name]++;
                }
                foreach (RegularRankExamChoice choice in choices)
                {
                    if (choice == null)
                        continue;
                    string name = (choice.Name ?? "").Trim();
                    string id = (choice.Id ?? "").Trim();
                    string display = nameCounts[name] > 1 ? name + "（ID " + id + "）" : name;
                    items.Add(new RegularRankExamListItem(id, name, display));
                }
            }
            return items;
        }

        public static int IndexOfUniqueName(IList<RegularRankExamListItem> items, string name)
        {
            string expected = (name ?? "").Trim();
            if (items == null || expected.Length == 0)
                return -1;
            int found = -1;
            int count = 0;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] == null || items[i].Name != expected)
                    continue;
                count++;
                found = i;
            }
            if (count == 1)
                return found;
            return -1;
        }

        public static string Apply(RegularRankCalculationContext context, RegularRankPeriodSourceSelection selection)
        {
            if (context == null)
                return "沒有計算條件。";
            if (selection == null)
                return "沒有期間與來源選擇。";

            string periodError = context.SetTargetPeriod(selection.TargetSchoolYear, selection.TargetSemester);
            RegularRankExamResolution targetExam = RegularRankExamResolution.Resolve(
                selection.TargetExamId,
                selection.TargetExamName,
                selection.TargetExamChoices);
            string examError = "";
            if (!targetExam.Resolved)
            {
                context.ClearTargetExam();
                examError = targetExam.Message;
            }
            else
            {
                examError = context.SetTargetExam(targetExam);
            }

            string sourceError = ApplySource(context, selection);
            string gradeError = context.SetGradeYears(selection.GradeYears);
            context.SetRankCategories(selection.ExcludedCategory, selection.RankTag1, selection.RankTag2);

            if (periodError.Length > 0)
                return periodError;
            if (examError.Length > 0)
                return examError;
            if (sourceError.Length > 0)
                return sourceError;
            return gradeError;
        }

        public static string DescribeSubjectChanges(RegularRankCalculationContext context)
        {
            if (context == null)
                return "";
            StringBuilder builder = new StringBuilder();
            AppendGroup(builder, "一般", context.GeneralSubjects);
            AppendGroup(builder, "類別一", context.Tag1Subjects);
            AppendGroup(builder, "類別二", context.Tag2Subjects);
            return builder.ToString();
        }

        private static string ApplySource(RegularRankCalculationContext context, RegularRankPeriodSourceSelection selection)
        {
            RegularRankSourceKind kind;
            string sourceTextError;
            if (!TryReadSource(selection.SourceText, out kind, out sourceTextError))
                return sourceTextError;

            if (kind == RegularRankSourceKind.Current)
                return context.SetCurrentSource();

            if (kind == RegularRankSourceKind.Excel)
            {
                string identity = (selection.ExcelIdentity ?? "").Trim();
                if (identity.Length == 0)
                {
                    context.SetSourceKindWithoutIdentity(RegularRankSourceKind.Excel);
                    return "Excel 來源必須指定匯入識別。";
                }
                return context.SetExcelSource(identity);
            }

            string batchId = (selection.SourceBatchId ?? "").Trim();
            if (batchId.Length == 0)
            {
                context.SetSourceKindWithoutIdentity(RegularRankSourceKind.History);
                return "歷史批次目錄尚未載入，不能指定來源批次。來源期間與來源考試不會覆寫目標。";
            }

            RegularRankExamResolution sourceExam = RegularRankExamResolution.Resolve(
                selection.SourceExamId,
                selection.SourceExamName,
                selection.SourceExamChoices);
            if (!sourceExam.Resolved)
            {
                context.SetSourceKindWithoutIdentity(RegularRankSourceKind.History);
                return sourceExam.Message.Length > 0 ? sourceExam.Message : "歷史來源的考試無法唯一識別。";
            }

            string historyError = context.SetHistorySource(
                batchId,
                selection.SourceSchoolYear,
                selection.SourceSemester,
                sourceExam);
            if (historyError.Length > 0)
                context.SetSourceKindWithoutIdentity(RegularRankSourceKind.History);
            return historyError;
        }

        private static void AppendGroup(StringBuilder builder, string label, RegularRankSubjectGroup group)
        {
            if (group == null)
                return;
            if (group.Added.Count == 0 && group.Removed.Count == 0)
                return;
            if (builder.Length > 0)
                builder.Append(" ");
            builder.Append(label);
            builder.Append("科目");
            if (group.Added.Count > 0)
            {
                builder.Append("新增：");
                builder.Append(Join(group.Added));
                builder.Append("。");
            }
            if (group.Removed.Count > 0)
            {
                builder.Append("移除：");
                builder.Append(Join(group.Removed));
                builder.Append("。");
            }
        }

        private static string Join(IList<string> names)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < names.Count; i++)
            {
                if (i > 0)
                    builder.Append("、");
                builder.Append(names[i]);
            }
            return builder.ToString();
        }
    }
}
