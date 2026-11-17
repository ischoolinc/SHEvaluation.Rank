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

        public const string FillGuideText =
            "空白範本只有表頭與格式，請從第 2 列填寫，不要改第 1 列欄名，也不要在表內放示意資料。\r\n"
            + "欄位順序：學號、姓名、學生狀態、年排名母群、班排名母群、科排名母群、類別一母群、類別二母群、年排名參與、班排名參與、科排名參與、類別一參與、類別二參與。\r\n"
            + "完整示意一列（只顯示在這份說明，不要寫進範本）：001234｜示意學生｜一般｜2年級｜高二甲｜普通科｜類別甲｜類別乙｜參與｜參與｜參與｜不參與｜不參與\r\n"
            + "\r\n"
            + "必填是學號、學生狀態，以及五個參與欄。系統以學號＋學生狀態比對 student.student_number 與 student.status，不用姓名比對。\r\n"
            + "學號如 001234 必須保留前導零。A 欄已設為文字格式，請直接輸入，不要讓 Excel 把學號變成數字。\r\n"
            + "學生狀態只能填：一般、延修、休學、輟學、畢業或離校、刪除。\r\n"
            + "五個參與欄都要填「參與」或「不參與」。母群名稱可以空白；類別母群空白時，該項不建立排名。\r\n"
            + "\r\n"
            + "整列空白會略過。學號或學生狀態空白、狀態不在上列、參與值不是「參與」或「不參與」、同一學號加同一狀態重複、查無學生、查到多筆，或同一學生出現兩次時，整份檔案都不匯入，並提示 Excel 列號與原因。姓名與學籍不同時，採用學籍姓名並另外提示。";

        public static Workbook CreateBlankTemplate()
        {
            Workbook workbook = new Workbook();
            Worksheet worksheet = workbook.Worksheets[0];
            worksheet.Name = "母群";
            for (int column = 0; column < Headers.Length; column++)
                worksheet.Cells[0, column].PutValue(Headers[column]);

            int styleIndex = workbook.Styles.Add();
            Style textStyle = workbook.Styles[styleIndex];
            textStyle.Number = 49;
            StyleFlag flag = new StyleFlag();
            flag.NumberFormat = true;
            worksheet.Cells.ApplyColumnStyle(0, textStyle, flag);
            worksheet.Cells.SetColumnWidth(0, 16);
            return workbook;
        }

        public static void SaveWorkbook(string path, Workbook workbook)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("沒有匯出檔案路徑。");
            if (workbook == null)
                throw new ArgumentNullException("workbook");
            workbook.Save(path);
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
