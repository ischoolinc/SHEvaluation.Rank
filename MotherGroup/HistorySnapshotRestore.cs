using System;
using System.Collections.Generic;
using System.Data;
using System.Xml;

namespace SHEvaluation.Rank.MotherGroup
{
    public sealed class HistorySnapshotLoad
    {
        public bool Restored { get; private set; }
        public string Message { get; private set; }
        public List<StudentMotherGroup> Students { get; private set; }
        public string SettingXml { get; private set; }
        public string SelectedBatchId { get; private set; }

        internal static HistorySnapshotLoad Fail(string selectedBatchId, string settingXml, string message)
        {
            HistorySnapshotLoad load = new HistorySnapshotLoad();
            load.Restored = false;
            load.Message = message;
            load.Students = new List<StudentMotherGroup>();
            load.SettingXml = settingXml ?? "";
            load.SelectedBatchId = selectedBatchId ?? "";
            return load;
        }

        internal static HistorySnapshotLoad Success(string selectedBatchId, string settingXml, List<StudentMotherGroup> students)
        {
            HistorySnapshotLoad load = new HistorySnapshotLoad();
            load.Restored = true;
            load.Message = "";
            load.Students = students;
            load.SettingXml = settingXml ?? "";
            load.SelectedBatchId = selectedBatchId ?? "";
            return load;
        }
    }

    public static class HistorySnapshotRestore
    {
        public const string MissingSnapshotMessage = "這筆歷史批次沒有可讀取的版本 1 母群快照。沒有快照的批次留待後續還原，本次不會改用目前學生資料。";
        public const string UnreadableSnapshotMessage = "這筆歷史批次的版本 1 母群快照無法還原。本次不會進入確認清單，也不會改用目前學生資料。";
        public const string WrongExamMessage = "這筆歷史批次的考試名稱不是學期成績，本次不會進入確認清單，也不會改用目前學生資料。";
        public const string UnrecognizedExamMessage = "這筆歷史批次沒有可辨識的考試名稱，本次不會進入確認清單，也不會改用目前學生資料。";
        public const string DuplicateSnapshotMessage = "這筆歷史批次有多個版本 1 母群快照，無法還原。本次不會進入確認清單，也不會改用目前學生資料。";

        public static string SelectSettingSql(string batchId)
        {
            long id;
            if (!long.TryParse(batchId == null ? "" : batchId.Trim(), out id) || id <= 0 || id.ToString() != batchId.Trim())
                throw new ArgumentException("歷史批次識別不是有效的數字，不查詢設定。");
            return "SELECT\n\tid\n\t, setting\n\t, extension\nFROM rank_batch\nWHERE id = " + id.ToString();
        }

        public static bool TryReadBatch(DataTable table, string batchId, out string settingXml, out string extensionJson)
        {
            extensionJson = "";
            if (!TryReadSetting(table, batchId, out settingXml))
                return false;
            if (table.Columns.Contains("extension"))
            {
                object extensionValue = table.Rows[0]["extension"];
                extensionJson = extensionValue == null || extensionValue == DBNull.Value ? "" : extensionValue.ToString();
            }
            return true;
        }

        public static bool TryReadSetting(DataTable table, string batchId, out string settingXml)
        {
            settingXml = "";
            if (table == null || table.Rows.Count != 1)
                return false;
            if (!table.Columns.Contains("id") || !table.Columns.Contains("setting"))
                return false;
            object idValue = table.Rows[0]["id"];
            string id = idValue == null || idValue == DBNull.Value ? "" : idValue.ToString().Trim();
            if (id != (batchId == null ? "" : batchId.Trim()))
                return false;
            object settingValue = table.Rows[0]["setting"];
            settingXml = settingValue == null || settingValue == DBNull.Value ? "" : settingValue.ToString();
            return true;
        }

        public static HistorySnapshotLoad LoadStored(string selectedBatchId, string extensionJson, string settingXml, string excludedCategory, IList<TagDefinition> tagDefinitions, IList<StudentTagLink> studentTags)
        {
            MotherGroupJsonRead json = MotherGroupSnapshotJson.Read(extensionJson);
            if (json.Present)
            {
                if (!json.Restored)
                    return HistorySnapshotLoad.Fail(selectedBatchId ?? "", settingXml ?? "", json.Message);
                ApplyExcluded(json.Students, excludedCategory, tagDefinitions, studentTags);
                return HistorySnapshotLoad.Success(selectedBatchId ?? "", settingXml ?? "", json.Students);
            }
            return Load(selectedBatchId, settingXml, excludedCategory, tagDefinitions, studentTags);
        }

        public static HistorySnapshotLoad LoadSemester(string selectedBatchId, string extensionJson, string settingXml, string excludedCategory, IList<TagDefinition> tagDefinitions, IList<StudentTagLink> studentTags)
        {
            string original = settingXml ?? "";
            string batchId = selectedBatchId ?? "";
            string examName;
            int version1Count;
            if (!TryInspectSnapshot(original, out examName, out version1Count) || (examName == null && version1Count < 0))
                return HistorySnapshotLoad.Fail(batchId, original, UnreadableSnapshotMessage);
            string sourceType;
            if (!SemesterHistoryBatchCatalog.TryClassifyExam(examName, out sourceType) || sourceType.Length == 0)
                return HistorySnapshotLoad.Fail(batchId, original, UnrecognizedExamMessage);

            MotherGroupJsonRead json = MotherGroupSnapshotJson.Read(extensionJson);
            if (json.Present)
            {
                if (!json.Restored)
                    return HistorySnapshotLoad.Fail(batchId, original, json.Message);
                ApplyExcluded(json.Students, excludedCategory, tagDefinitions, studentTags);
                return HistorySnapshotLoad.Success(batchId, original, json.Students);
            }
            if (version1Count == 0)
                return HistorySnapshotLoad.Fail(batchId, original, MissingSnapshotMessage);
            if (version1Count != 1)
                return HistorySnapshotLoad.Fail(batchId, original, DuplicateSnapshotMessage);

            List<StudentMotherGroup> students;
            string kind;
            string snapshotHistoryBatchId;
            if (!MotherGroupSnapshot.TryRestore(original, out students, out kind, out snapshotHistoryBatchId))
                return HistorySnapshotLoad.Fail(batchId, original, UnreadableSnapshotMessage);
            IgnoreSnapshotHistoryBatchId(batchId, snapshotHistoryBatchId, kind);
            ApplyExcluded(students, excludedCategory, tagDefinitions, studentTags);
            SemesterHistoryStudentParticipation.ApplyNoJsonYearDeptClass(students);
            return HistorySnapshotLoad.Success(batchId, original, students);
        }

        public static HistorySnapshotLoad Load(string selectedBatchId, string settingXml, string excludedCategory, IList<TagDefinition> tagDefinitions, IList<StudentTagLink> studentTags)
        {
            string original = settingXml ?? "";
            string batchId = selectedBatchId ?? "";
            string examName;
            int version1Count;
            if (!TryInspectSnapshot(original, out examName, out version1Count))
                return HistorySnapshotLoad.Fail(batchId, original, UnreadableSnapshotMessage);
            if (examName != "學期成績")
                return HistorySnapshotLoad.Fail(batchId, original, WrongExamMessage);
            if (version1Count == 0)
                return HistorySnapshotLoad.Fail(batchId, original, MissingSnapshotMessage);
            if (version1Count != 1)
                return HistorySnapshotLoad.Fail(batchId, original, DuplicateSnapshotMessage);

            List<StudentMotherGroup> students;
            string kind;
            string snapshotHistoryBatchId;
            if (!MotherGroupSnapshot.TryRestore(original, out students, out kind, out snapshotHistoryBatchId))
                return HistorySnapshotLoad.Fail(batchId, original, UnreadableSnapshotMessage);
            IgnoreSnapshotHistoryBatchId(batchId, snapshotHistoryBatchId, kind);
            ApplyExcluded(students, excludedCategory, tagDefinitions, studentTags);
            return HistorySnapshotLoad.Success(batchId, original, students);
        }

        private static void ApplyExcluded(List<StudentMotherGroup> students, string excludedCategory, IList<TagDefinition> tagDefinitions, IList<StudentTagLink> studentTags)
        {
            List<string> excludedTagIds = CurrentStudentMotherGroupBuilder.ResolveTagIds(tagDefinitions, excludedCategory);
            foreach (StudentMotherGroup student in students)
            {
                bool excluded = HasTag(studentTags, student.StudentId, excludedTagIds);
                MotherGroupInitialParticipation.Apply(MotherGroupSourceKind.HistorySnapshot, student, excluded);
            }
        }

        private static bool TryInspectSnapshot(string settingXml, out string examName, out int version1Count)
        {
            examName = "";
            version1Count = 0;
            if (string.IsNullOrWhiteSpace(settingXml))
                return true;

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
            foreach (XmlNode node in setting.GetElementsByTagName("MotherGroupSnapshot"))
            {
                XmlElement snapshot = node as XmlElement;
                if (snapshot != null && snapshot.GetAttribute("version") == MotherGroupSnapshot.Version)
                    version1Count++;
            }
            return true;
        }

        private static void IgnoreSnapshotHistoryBatchId(string selectedBatchId, string snapshotHistoryBatchId, string snapshotKind)
        {
            if (string.IsNullOrEmpty(selectedBatchId) && string.IsNullOrEmpty(snapshotHistoryBatchId) && string.IsNullOrEmpty(snapshotKind))
                return;
        }

        private static bool HasTag(IList<StudentTagLink> studentTags, string studentId, IList<string> tagIds)
        {
            if (studentTags == null || tagIds == null || tagIds.Count == 0)
                return false;
            foreach (StudentTagLink link in studentTags)
            {
                if (link != null && link.StudentId == studentId && tagIds.Contains(link.TagId))
                    return true;
            }
            return false;
        }
    }
}
