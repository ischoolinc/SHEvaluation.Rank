using Aspose.Cells;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;

namespace SHEvaluation.Rank.MotherGroup
{
    public static class MotherGroupExcelExchange
    {
        public static readonly string[] Headers = new string[]
        {
            "學號",
            "姓名",
            "學生狀態",
            "年排名母群",
            "班排名母群",
            "科排名母群",
            "類別一母群",
            "類別二母群",
            "年排名參與",
            "班排名參與",
            "科排名參與",
            "類別一參與",
            "類別二參與"
        };

        public static readonly string[] ExportHeaders = Headers;

        private struct StatusText
        {
            public int Code;
            public string Name;

            public StatusText(int code, string name)
            {
                Code = code;
                Name = name;
            }
        }

        private static readonly StatusText[] StatusTexts = new StatusText[]
        {
            new StatusText(1, "一般"),
            new StatusText(2, "延修"),
            new StatusText(4, "休學"),
            new StatusText(8, "輟學"),
            new StatusText(16, "畢業或離校"),
            new StatusText(256, "刪除")
        };

        private static readonly string[] RankTypes = new string[]
        {
            RankKinds.Year,
            RankKinds.Class,
            RankKinds.Department,
            RankKinds.Tag1,
            RankKinds.Tag2
        };

        public static bool TryStatusName(int status, out string name)
        {
            foreach (StatusText item in StatusTexts)
            {
                if (item.Code == status)
                {
                    name = item.Name;
                    return true;
                }
            }
            name = "";
            return false;
        }

        public static bool TryStatusCode(string name, out int status)
        {
            foreach (StatusText item in StatusTexts)
            {
                if (item.Name == name)
                {
                    status = item.Code;
                    return true;
                }
            }
            status = 0;
            return false;
        }

        public static List<string> MissingStatusIds(MotherGroupConfirmList list)
        {
            List<string> ids = new List<string>();
            if (list == null)
                return ids;
            foreach (ConfirmStudentRow row in list.Rows)
            {
                if (row == null || row.Group == null || row.Group.Status.HasValue)
                    continue;
                string id = row.Group.StudentId ?? "";
                if (id.Length == 0 || ids.Contains(id))
                    continue;
                ids.Add(id);
            }
            return ids;
        }

        public static string SelectCurrentStatusSql(IList<string> studentIds)
        {
            if (studentIds == null || studentIds.Count == 0)
                throw new ArgumentException("沒有需要查詢狀態的學生。");
            List<string> quoted = new List<string>();
            foreach (string id in studentIds)
            {
                if (string.IsNullOrEmpty(id))
                    throw new ArgumentException("學生識別是空白，不能查詢狀態。");
                quoted.Add("'" + id.Replace("'", "''") + "'");
            }
            return "SELECT student.id::text AS student_id, student.status AS student_status FROM student WHERE student.id::text IN (" + string.Join(", ", quoted.ToArray()) + ")";
        }

        public static Dictionary<string, int> ReadCurrentStatuses(IList<string> studentIds, DataTable table)
        {
            if (studentIds == null)
                throw new ArgumentNullException("studentIds");
            if (table == null || !table.Columns.Contains("student_id") || !table.Columns.Contains("student_status"))
                throw new InvalidOperationException("學生狀態查詢失敗。");

            Dictionary<string, int> found = new Dictionary<string, int>();
            List<string> errors = new List<string>();
            foreach (DataRow row in table.Rows)
            {
                string id = Convert.ToString(row["student_id"]).Trim();
                if (id.Length == 0)
                {
                    errors.Add("查詢結果有空白的學生識別。");
                    continue;
                }
                if (row.IsNull("student_status"))
                {
                    errors.Add("學生識別「" + id + "」的學生狀態是空白。");
                    continue;
                }
                int status;
                if (!int.TryParse(Convert.ToString(row["student_status"]), out status))
                {
                    errors.Add("學生識別「" + id + "」的學生狀態不在對照表。");
                    continue;
                }
                if (found.ContainsKey(id))
                {
                    errors.Add("學生識別「" + id + "」查到超過一筆狀態。");
                    continue;
                }
                string unused;
                if (!TryStatusName(status, out unused))
                {
                    errors.Add("學生識別「" + id + "」的學生狀態「" + status + "」不在對照表。");
                    continue;
                }
                found[id] = status;
            }
            foreach (string id in studentIds)
            {
                if (string.IsNullOrEmpty(id) || !found.ContainsKey(id))
                    errors.Add("學生識別「" + (id ?? "") + "」查不到目前學生狀態。");
            }
            if (errors.Count > 0)
                throw new InvalidOperationException(string.Join(Environment.NewLine, errors.ToArray()));
            return found;
        }

        public static Workbook Create(MotherGroupConfirmList list)
        {
            return Create(list, null);
        }

        public static Workbook Create(MotherGroupConfirmList list, IDictionary<string, int> currentStatusByStudentId)
        {
            if (list == null)
                throw new ArgumentNullException("list");

            List<string> statusNames = new List<string>();
            List<string> errors = new List<string>();
            foreach (ConfirmStudentRow row in list.Rows)
            {
                if (row == null || row.Group == null)
                    continue;
                statusNames.Add(ResolveStatusName(row.Group, currentStatusByStudentId, errors));
            }
            if (errors.Count > 0)
                throw new InvalidOperationException(string.Join(Environment.NewLine, errors.ToArray()));

            Workbook workbook = new Workbook();
            Worksheet worksheet = workbook.Worksheets[0];
            worksheet.Name = "母群";
            for (int column = 0; column < ExportHeaders.Length; column++)
                worksheet.Cells[0, column].PutValue(ExportHeaders[column]);

            int rowIndex = 1;
            int statusIndex = 0;
            foreach (ConfirmStudentRow row in list.Rows)
            {
                if (row == null || row.Group == null)
                    continue;
                PutText(worksheet.Cells[rowIndex, 0], row.Group.StudentNumber ?? "");
                worksheet.Cells[rowIndex, 1].PutValue(row.Group.StudentName ?? "");
                worksheet.Cells[rowIndex, 2].PutValue(statusNames[statusIndex]);
                for (int rank = 0; rank < RankTypes.Length; rank++)
                {
                    RankSlot slot = row.Group.GetWorking(RankTypes[rank]);
                    worksheet.Cells[rowIndex, 3 + rank].PutValue(slot.GroupName ?? "");
                    worksheet.Cells[rowIndex, 8 + rank].PutValue(slot.Participate ? "參與" : "不參與");
                }
                statusIndex++;
                rowIndex++;
            }
            return workbook;
        }

        public static void Save(string path, MotherGroupConfirmList list)
        {
            Save(path, list, null);
        }

        public static void Save(string path, MotherGroupConfirmList list, IDictionary<string, int> currentStatusByStudentId)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("沒有匯出檔案路徑。");
            Workbook workbook = Create(list, currentStatusByStudentId);
            workbook.Save(path);
        }

        public static Workbook Open(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("沒有匯入檔案路徑。");
            return new Workbook(path);
        }

        public static Workbook Open(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");
            return new Workbook(stream);
        }

        private static string ResolveStatusName(StudentMotherGroup group, IDictionary<string, int> currentStatusByStudentId, List<string> errors)
        {
            int? status = group.Status;
            int lookedUp;
            if (!status.HasValue && currentStatusByStudentId != null && !string.IsNullOrEmpty(group.StudentId) && currentStatusByStudentId.TryGetValue(group.StudentId, out lookedUp))
                status = lookedUp;

            string name;
            if (!status.HasValue)
            {
                errors.Add(Describe(group) + "查不到目前學生狀態。");
                return "";
            }
            if (!TryStatusName(status.Value, out name))
            {
                errors.Add(Describe(group) + "的學生狀態「" + status.Value + "」不在對照表。");
                return "";
            }
            return name;
        }

        private static string Describe(StudentMotherGroup group)
        {
            return "學生識別「" + (group.StudentId ?? "") + "」、學號「" + (group.StudentNumber ?? "") + "」";
        }

        private static void PutText(Cell cell, string value)
        {
            Style style = cell.GetStyle();
            style.Number = 49;
            cell.SetStyle(style);
            cell.PutValue(value, false);
        }
    }
}
