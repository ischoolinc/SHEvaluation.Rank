using SHEvaluation.Rank.MotherGroup;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Xml;

namespace SHEvaluation.Rank.RegularRank
{
    public sealed class RegularRankHistoryBatch
    {
        public string Id { get; internal set; }
        public string SchoolYear { get; internal set; }
        public string Semester { get; internal set; }
        public RegularRankExamResolution Exam { get; internal set; }
        public string Setting { get; internal set; }
        public string Extension { get; internal set; }
        public string Error { get; internal set; }
        public override string ToString()
        {
            return Id + "｜" + SchoolYear + "-" + Semester + "｜" + (Exam != null && Exam.Resolved ? Exam.ExamName + "（ID " + Exam.ExamId + "）" : Error);
        }
    }

    public static class RegularRankHistorySource
    {
        public static string CatalogSql(string schoolYear, string semester)
        {
            int year;
            if (!int.TryParse(schoolYear, NumberStyles.None, CultureInfo.InvariantCulture, out year)
                || year <= 0 || year.ToString(CultureInfo.InvariantCulture) != schoolYear
                || (semester != "1" && semester != "2")) throw new ArgumentException("歷史來源期間無效。");
            return "SELECT b.id, b.school_year, b.semester, b.setting, b.extension,"
                + " (SELECT count(DISTINCT m.ref_exam_id) FROM rank_matrix m WHERE m.ref_batch_id=b.id) AS source_exam_count,"
                + " (SELECT min(m.ref_exam_id) FROM rank_matrix m WHERE m.ref_batch_id=b.id) AS source_exam_id FROM rank_batch b"
                + " WHERE b.school_year = " + schoolYear + " AND b.semester = " + semester
                + " AND EXISTS (SELECT 1 FROM rank_matrix m WHERE m.ref_batch_id=b.id AND m.item_type LIKE '定期評量/%')"
                + " AND NOT EXISTS (SELECT 1 FROM rank_matrix m WHERE m.ref_batch_id=b.id AND m.item_type NOT LIKE '定期評量/%') ORDER BY b.id DESC";
        }

        public static List<RegularRankHistoryBatch> ReadCatalog(DataTable table, IList<RegularRankExamChoice> exams)
        {
            List<RegularRankHistoryBatch> batches = new List<RegularRankHistoryBatch>();
            HashSet<string> seen = new HashSet<string>();
            foreach (DataRow row in table.Rows)
            {
                string id, error;
                if (!RegularRankIdentifiers.TryReadBatchId(Convert.ToString(row["id"]), out id, out error) || !seen.Add(id))
                    throw new InvalidOperationException("歷史目錄含無效或重複批次。");
                RegularRankHistoryBatch batch = new RegularRankHistoryBatch
                {
                    Id = id, SchoolYear = Convert.ToString(row["school_year"]), Semester = Convert.ToString(row["semester"]),
                    Setting = Convert.ToString(row["setting"]), Extension = Convert.ToString(row["extension"]), Error = ""
                };
                try
                {
                    XmlElement setting = ReadSetting(batch.Setting);
                    string name = setting.GetAttribute("考試名稱");
                    if (name == "學期成績" || name.Contains("技職繁星")) throw new InvalidOperationException("批次考試類型不符。");
                    if (Convert.ToString(row["source_exam_count"]) != "1") throw new InvalidOperationException("批次考試 ID 不唯一。");
                    string matrixExam = Convert.ToString(row["source_exam_id"]);
                    string storedExam = setting.GetAttribute("ref_exam_id");
                    if (storedExam.Length > 0 && storedExam != matrixExam) throw new InvalidOperationException("批次設定與矩陣考試 ID 不一致。");
                    batch.Exam = RegularRankExamResolution.Resolve(matrixExam, name, exams);
                    if (!batch.Exam.Resolved) batch.Error = batch.Exam.Message;
                }
                catch (Exception ex) { batch.Error = ex.Message; }
                batches.Add(batch);
            }
            return batches;
        }

        public static List<ConfirmStudentRow> Restore(RegularRankHistoryBatch batch,
            Func<string, DataTable> query, string excludedCategory,
            IList<TagDefinition> definitions, IList<StudentTagLink> links, out string notice)
        {
            if (batch == null || batch.Error.Length > 0 || batch.Exam == null || !batch.Exam.Resolved)
                throw new InvalidOperationException(batch == null ? "尚未選擇歷史批次。" : batch.Error);
            notice = "";
            XmlElement setting = ReadSetting(batch.Setting);
            MotherGroupJsonRead json = MotherGroupSnapshotJson.Read(batch.Extension);
            List<StudentMotherGroup> groups;
            if (json.Present)
            {
                if (!json.Restored) throw new InvalidOperationException(json.Message);
                groups = json.Students;
            }
            else
            {
                XmlNodeList snapshots = setting.GetElementsByTagName("MotherGroupSnapshot");
                if (snapshots.Count > 0)
                {
                    if (snapshots.Count != 1 || !MotherGroupSnapshot.TryRestore(batch.Setting, out groups))
                        throw new InvalidOperationException("歷史 XML 快照損壞、重複或版本不支援；停止還原。");
                }
                else
                {
                    HistoryRankLoad fallback = HistoryRankRestore.ReadBatchStudents(batch.Id,
                        query(HistoryRankRestore.SelectBatchStudentSql(batch.Id)), "", null, null);
                    if (!fallback.Restored) throw new InvalidOperationException(fallback.Message);
                    groups = fallback.Students;
                    notice = "無母群快照，以批次學生欄位降級還原；參與不是歷史原始旗標。\n" + fallback.Notice;
                }
            }
            if (groups.Count == 0) throw new InvalidOperationException("歷史母群沒有學生。");
            List<string> excludedIds = CurrentStudentMotherGroupBuilder.ResolveTagIds(definitions,
                RegularRankCurrentStudentSource.NormalizeCategory(excludedCategory));
            List<ConfirmStudentRow> rows = new List<ConfirmStudentRow>();
            HashSet<string> seen = new HashSet<string>();
            foreach (StudentMotherGroup group in groups)
            {
                if (group == null || !seen.Add(group.StudentId)) throw new InvalidOperationException("歷史快照含重複學生。");
                bool excluded = false;
                if (links != null) foreach (StudentTagLink link in links)
                    if (link != null && link.StudentId == group.StudentId && excludedIds.Contains(link.TagId)) excluded = true;
                MotherGroupInitialParticipation.Apply(MotherGroupSourceKind.HistorySnapshot, group, excluded);
                rows.Add(new ConfirmStudentRow { Group = group, ClassName = "", SeatNo = "" });
            }
            return rows;
        }

        private static XmlElement ReadSetting(string xml)
        {
            XmlDocument document = new XmlDocument { XmlResolver = null };
            document.LoadXml(xml);
            if (document.DocumentElement == null || document.DocumentElement.Name != "Setting")
                throw new InvalidOperationException("批次設定不是有效的 Setting XML。");
            return document.DocumentElement;
        }
    }
}
