using System;
using System.Collections.Generic;
using System.Data;
using System.Xml;

namespace SHEvaluation.Rank.MotherGroup
{
    public enum HistorySnapshotOrigin
    {
        None,
        Json,
        Xml
    }

    public sealed class HistoryRankBatch
    {
        public string Id { get; private set; }
        public string SchoolYear { get; private set; }
        public string Semester { get; private set; }
        public string Description { get; private set; }
        public string GradeYears { get; private set; }
        public string ExamName { get; private set; }
        public string SourceType { get; private set; }
        public bool HasVersion1Snapshot { get; private set; }
        public HistorySnapshotOrigin SnapshotOrigin { get; private set; }
        public string DisplayText { get; private set; }

        public override string ToString()
        {
            return DisplayText;
        }

        internal static HistoryRankBatch Create(string id, string schoolYear, string semester, string description, string gradeYears, bool hasVersion1Snapshot, HistorySnapshotOrigin snapshotOrigin, string examName, string sourceType)
        {
            HistoryRankBatch batch = new HistoryRankBatch();
            batch.Id = id;
            batch.SchoolYear = schoolYear ?? "";
            batch.Semester = semester ?? "";
            batch.Description = description ?? "";
            batch.GradeYears = gradeYears ?? "";
            batch.ExamName = examName ?? "";
            batch.SourceType = sourceType ?? "";
            batch.HasVersion1Snapshot = hasVersion1Snapshot;
            batch.SnapshotOrigin = snapshotOrigin;
            string snapshotText = snapshotOrigin == HistorySnapshotOrigin.Json ? "有(JSON)" : snapshotOrigin == HistorySnapshotOrigin.Xml ? "有(XML)" : "無";
            batch.DisplayText = batch.Id
                + "｜" + batch.SourceType
                + "｜" + batch.SchoolYear + "學年度第" + batch.Semester + "學期"
                + "｜" + batch.ExamName
                + "｜" + batch.Description
                + "｜年級" + batch.GradeYears
                + "｜快照" + snapshotText;
            return batch;
        }
    }

    public static class SemesterHistoryBatchCatalog
    {
        // 這段會讀回每一筆 rank_batch.setting。批次很多或快照很大時，清單查詢會變慢。
        // 本次仍讀全部列，不在資料庫依考試名稱或快照提前篩選，避免漏掉同一學年期的舊批次。
        public const string SelectSql =
            "SELECT\n"
            + "\tid\n"
            + "\t, school_year\n"
            + "\t, semester\n"
            + "\t, calculation_description\n"
            + "\t, setting\n"
            + "\t, extension\n"
            + "FROM rank_batch\n"
            + "ORDER BY id DESC";

        public static List<HistoryRankBatch> Read(DataTable table)
        {
            List<HistoryRankBatch> batches = new List<HistoryRankBatch>();
            if (table == null)
                return batches;
            RequireColumn(table, "id");
            RequireColumn(table, "school_year");
            RequireColumn(table, "semester");
            RequireColumn(table, "calculation_description");
            RequireColumn(table, "setting");
            foreach (DataRow row in table.Rows)
            {
                HistoryRankBatch batch = TryRead(
                    Cell(row, "id"),
                    Cell(row, "school_year"),
                    Cell(row, "semester"),
                    Cell(row, "calculation_description"),
                    Cell(row, "setting"),
                    table.Columns.Contains("extension") ? Cell(row, "extension") : "");
                if (batch != null)
                    batches.Add(batch);
            }
            return batches;
        }

        public static void SortByIdDescending(List<HistoryRankBatch> batches)
        {
            if (batches == null)
                return;
            // 依 rank_batch.id 數字由大到小。資料表沒有建立時間欄位，這個順序不是建立時間。
            batches.Sort(CompareIdDescending);
        }

        public static List<HistoryRankBatch> FilterPeriod(IList<HistoryRankBatch> batches, string schoolYear, string semester)
        {
            List<HistoryRankBatch> matched = new List<HistoryRankBatch>();
            if (batches == null)
                return matched;
            string year = schoolYear == null ? "" : schoolYear.Trim();
            string term = semester == null ? "" : semester.Trim();
            foreach (HistoryRankBatch batch in batches)
            {
                if (batch != null && batch.SchoolYear == year && batch.Semester == term)
                    matched.Add(batch);
            }
            return matched;
        }

        public static bool TryClassifyExam(string examName, out string sourceType)
        {
            sourceType = "";
            string name = examName == null ? "" : examName.Trim();
            if (name != "學期成績")
                return false;
            sourceType = "學期成績";
            return true;
        }

        private static int CompareIdDescending(HistoryRankBatch left, HistoryRankBatch right)
        {
            long leftId;
            long rightId;
            bool leftNumber = long.TryParse(left.Id, out leftId);
            bool rightNumber = long.TryParse(right.Id, out rightId);
            if (leftNumber && rightNumber)
                return rightId.CompareTo(leftId);
            if (leftNumber)
                return -1;
            if (rightNumber)
                return 1;
            return 0;
        }

        public static HistoryRankBatch TryRead(string id, string schoolYear, string semester, string description, string settingXml)
        {
            return TryRead(id, schoolYear, semester, description, settingXml, "");
        }

        public static HistoryRankBatch TryRead(string id, string schoolYear, string semester, string description, string settingXml, string extensionJson)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;
            string grades;
            bool xmlSnapshot;
            string examName;
            string sourceType;
            if (!TryReadSetting(settingXml, out grades, out xmlSnapshot, out examName, out sourceType))
                return null;
            MotherGroupExtensionMark mark = MotherGroupSnapshotJson.Describe(extensionJson);
            HistorySnapshotOrigin origin = HistorySnapshotOrigin.None;
            if (mark.InvalidDocument || mark.SnapshotCount > 0)
                origin = HistorySnapshotOrigin.Json;
            else if (xmlSnapshot)
                origin = HistorySnapshotOrigin.Xml;
            bool hasVersion1 = origin == HistorySnapshotOrigin.Xml || (origin == HistorySnapshotOrigin.Json && mark.AnyVersion1);
            return HistoryRankBatch.Create(id.Trim(), schoolYear, semester, description, grades, hasVersion1, origin, examName, sourceType);
        }

        private static bool TryReadSetting(string settingXml, out string gradeYears, out bool hasVersion1Snapshot, out string examName, out string sourceType)
        {
            gradeYears = "";
            hasVersion1Snapshot = false;
            examName = "";
            sourceType = "";
            if (string.IsNullOrWhiteSpace(settingXml))
                return false;

            XmlDocument document = new XmlDocument();
            try
            {
                document.LoadXml(settingXml);
            }
            catch (XmlException)
            {
                return false;
            }

            XmlElement setting = document.DocumentElement;
            if (setting == null || setting.Name != "Setting")
                return false;
            examName = setting.GetAttribute("考試名稱");
            if (examName != null)
                examName = examName.Trim();
            if (!TryClassifyExam(examName, out sourceType))
                return false;

            List<string> grades = new List<string>();
            foreach (XmlNode node in setting.ChildNodes)
            {
                XmlElement grade = node as XmlElement;
                if (grade == null || grade.Name != "年級")
                    continue;
                string text = grade.InnerText == null ? "" : grade.InnerText.Trim();
                if (text.Length == 0 || grades.Contains(text))
                    continue;
                grades.Add(text);
            }
            gradeYears = string.Join("、", grades.ToArray());

            foreach (XmlNode node in setting.GetElementsByTagName("MotherGroupSnapshot"))
            {
                XmlElement snapshot = node as XmlElement;
                if (snapshot != null && snapshot.GetAttribute("version") == MotherGroupSnapshot.Version)
                    hasVersion1Snapshot = true;
            }
            return true;
        }

        private static void RequireColumn(DataTable table, string column)
        {
            if (!table.Columns.Contains(column))
                throw new InvalidOperationException("歷史批次查詢缺少欄位：" + column);
        }

        private static string Cell(DataRow row, string column)
        {
            object value = row[column];
            if (value == null || value == DBNull.Value)
                return "";
            return value.ToString();
        }
    }
}
