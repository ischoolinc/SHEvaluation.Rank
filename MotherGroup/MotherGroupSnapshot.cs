using System;
using System.Collections.Generic;
using System.Xml;

namespace SHEvaluation.Rank.MotherGroup
{
    public static class MotherGroupSnapshot
    {
        public const string Version = "1";

        public static void Append(XmlElement setting, MotherGroupConfirmList list, MotherGroupSourceKind sourceKind, string historyBatchId)
        {
            if (setting == null)
                throw new ArgumentNullException("setting");
            if (setting.OwnerDocument == null)
                throw new InvalidOperationException("設定節點沒有 XML 文件，不建立母群快照。");
            if (list == null)
                throw new InvalidOperationException("沒有確認清單，不建立母群快照。");

            List<StudentMotherGroup> groups = ValidatedGroups(list);
            string kind = KindAttribute(sourceKind);

            XmlDocument document = setting.OwnerDocument;
            XmlElement snapshot = document.CreateElement("MotherGroupSnapshot");
            snapshot.SetAttribute("version", Version);
            XmlElement source = document.CreateElement("Source");
            source.SetAttribute("kind", kind);
            source.SetAttribute("historyBatchId", historyBatchId ?? "");
            snapshot.AppendChild(source);
            foreach (StudentMotherGroup group in groups)
                snapshot.AppendChild(WriteStudent(document, group));

            XmlElement previous = FindSnapshot(setting);
            if (previous != null)
                setting.RemoveChild(previous);
            setting.AppendChild(snapshot);
        }

        public static bool TryRestore(string settingXml, out List<StudentMotherGroup> students)
        {
            string kind;
            string historyBatchId;
            return TryRestore(settingXml, out students, out kind, out historyBatchId);
        }

        public static bool TryRestore(string settingXml, out List<StudentMotherGroup> students, out string kind, out string historyBatchId)
        {
            students = new List<StudentMotherGroup>();
            kind = "";
            historyBatchId = "";
            if (string.IsNullOrEmpty(settingXml))
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
            XmlElement snapshot = FindSnapshot(setting);
            if (snapshot == null || snapshot.GetAttribute("version") != Version)
                return false;

            XmlElement source = FindChild(snapshot, "Source");
            if (source == null)
                return false;
            string parsedKind = source.GetAttribute("kind");
            string parsedHistoryBatchId = source.GetAttribute("historyBatchId");
            if (parsedKind != "current" && parsedKind != "history" && parsedKind != "excel")
                return false;

            List<StudentMotherGroup> parsed = new List<StudentMotherGroup>();
            Dictionary<string, bool> seenIds = new Dictionary<string, bool>();
            foreach (XmlNode node in snapshot.ChildNodes)
            {
                XmlElement student = node as XmlElement;
                if (student == null || student.Name != "Student")
                    continue;
                StudentMotherGroup group = ReadStudent(student);
                if (group == null || seenIds.ContainsKey(group.StudentId))
                    return false;
                seenIds.Add(group.StudentId, true);
                parsed.Add(group);
            }

            students = parsed;
            kind = parsedKind;
            historyBatchId = parsedHistoryBatchId;
            return true;
        }

        public static MotherGroupSourceKind KindFromSourceName(string sourceName)
        {
            if (sourceName == CalculationPeriod.CurrentSourceName)
                return MotherGroupSourceKind.Current;
            if (sourceName == CalculationPeriod.HistorySourceName)
                return MotherGroupSourceKind.HistorySnapshot;
            if (sourceName == CalculationPeriod.ExcelSourceName)
                return MotherGroupSourceKind.Excel;
            throw new ArgumentException("無法由母群來源名稱決定快照種類：" + sourceName);
        }

        public static string KindAttribute(MotherGroupSourceKind sourceKind)
        {
            if (sourceKind == MotherGroupSourceKind.Current)
                return "current";
            if (sourceKind == MotherGroupSourceKind.HistorySnapshot)
                return "history";
            if (sourceKind == MotherGroupSourceKind.Excel)
                return "excel";
            throw new ArgumentException("未知的母群來源：" + sourceKind);
        }

        private static List<StudentMotherGroup> ValidatedGroups(MotherGroupConfirmList list)
        {
            List<StudentMotherGroup> groups = new List<StudentMotherGroup>();
            Dictionary<string, bool> seen = new Dictionary<string, bool>();
            foreach (ConfirmStudentRow row in list.Rows)
            {
                if (row == null || row.Group == null || string.IsNullOrWhiteSpace(row.Group.StudentId))
                    throw new InvalidOperationException("確認清單有缺少學生識別的列，不建立母群快照。");
                if (seen.ContainsKey(row.Group.StudentId))
                    throw new InvalidOperationException("確認清單的學生識別重複，不建立母群快照：" + row.Group.StudentId);
                foreach (string rankType in RankKinds.All)
                {
                    if (row.Group.GetWorking(rankType) == null)
                        throw new InvalidOperationException("確認清單缺少排名母群，不建立母群快照：" + row.Group.StudentId);
                }
                seen.Add(row.Group.StudentId, true);
                groups.Add(row.Group);
            }
            return groups;
        }

        private static XmlElement WriteStudent(XmlDocument document, StudentMotherGroup group)
        {
            XmlElement student = document.CreateElement("Student");
            student.SetAttribute("id", group.StudentId);
            student.SetAttribute("number", group.StudentNumber ?? "");
            student.SetAttribute("name", group.StudentName ?? "");
            foreach (string rankType in RankKinds.All)
            {
                RankSlot slot = group.GetWorking(rankType);
                XmlElement rank = document.CreateElement("Rank");
                rank.SetAttribute("type", rankType);
                rank.SetAttribute("name", slot.GroupName ?? "");
                rank.SetAttribute("participate", slot.Participate ? "true" : "false");
                student.AppendChild(rank);
            }
            return student;
        }

        private static StudentMotherGroup ReadStudent(XmlElement student)
        {
            string studentId = student.GetAttribute("id");
            if (string.IsNullOrWhiteSpace(studentId))
                return null;
            List<RankSlot> slots = new List<RankSlot>();
            Dictionary<string, bool> seen = new Dictionary<string, bool>();
            foreach (XmlNode node in student.ChildNodes)
            {
                XmlElement rank = node as XmlElement;
                if (rank == null || rank.Name != "Rank")
                    continue;
                string rankType = rank.GetAttribute("type");
                if (!ContainsRank(rankType) || seen.ContainsKey(rankType))
                    return null;
                string participate = rank.GetAttribute("participate");
                if (participate != "true" && participate != "false")
                    return null;
                seen.Add(rankType, true);
                slots.Add(new RankSlot
                {
                    RankType = rankType,
                    GroupName = rank.GetAttribute("name"),
                    Participate = participate == "true"
                });
            }
            if (seen.Count != RankKinds.All.Length)
                return null;

            StudentMotherGroup group = new StudentMotherGroup();
            group.StudentId = studentId;
            group.StudentNumber = student.GetAttribute("number");
            group.StudentName = student.GetAttribute("name");
            group.SetSourceSlots(slots);
            return group;
        }

        private static bool ContainsRank(string rankType)
        {
            foreach (string known in RankKinds.All)
            {
                if (known == rankType)
                    return true;
            }
            return false;
        }

        private static XmlElement FindSnapshot(XmlElement setting)
        {
            return FindChild(setting, "MotherGroupSnapshot");
        }

        private static XmlElement FindChild(XmlElement parent, string name)
        {
            foreach (XmlNode node in parent.ChildNodes)
            {
                XmlElement element = node as XmlElement;
                if (element != null && element.Name == name)
                    return element;
            }
            return null;
        }
    }
}
