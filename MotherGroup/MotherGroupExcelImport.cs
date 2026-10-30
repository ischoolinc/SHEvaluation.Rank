using Aspose.Cells;
using System;
using System.Collections.Generic;
using System.Data;

namespace SHEvaluation.Rank.MotherGroup
{
    public sealed class MotherGroupExcelIssue
    {
        public int RowNumber { get; private set; }
        public string StudentNumber { get; private set; }
        public string Message { get; private set; }

        internal static MotherGroupExcelIssue Create(int rowNumber, string studentNumber, string message)
        {
            MotherGroupExcelIssue issue = new MotherGroupExcelIssue();
            issue.RowNumber = rowNumber;
            issue.StudentNumber = studentNumber ?? "";
            issue.Message = message ?? "";
            return issue;
        }
    }

    public sealed class MotherGroupExcelIdentity
    {
        public string StudentNumber { get; private set; }
        public int Status { get; private set; }

        internal static MotherGroupExcelIdentity Create(string studentNumber, int status)
        {
            MotherGroupExcelIdentity identity = new MotherGroupExcelIdentity();
            identity.StudentNumber = studentNumber ?? "";
            identity.Status = status;
            return identity;
        }
    }

    public sealed class MotherGroupExcelStudent
    {
        public int RowNumber { get; internal set; }
        public string StudentId { get; internal set; }
        public string StudentNumber { get; internal set; }
        public int? Status { get; internal set; }
        public string ExcelName { get; internal set; }
        public string DatabaseName { get; internal set; }
        public string NameWarning { get; internal set; }
        public List<RankSlot> Slots { get; internal set; }
    }

    public sealed class MotherGroupExcelValidation
    {
        public bool IsValid { get; internal set; }
        public List<MotherGroupExcelIssue> Issues { get; internal set; }
        public List<MotherGroupExcelStudent> Students { get; internal set; }

        internal static MotherGroupExcelValidation Create()
        {
            MotherGroupExcelValidation validation = new MotherGroupExcelValidation();
            validation.IsValid = false;
            validation.Issues = new List<MotherGroupExcelIssue>();
            validation.Students = new List<MotherGroupExcelStudent>();
            return validation;
        }
    }

    public static class MotherGroupExcelImport
    {
        public const string SheetName = "母群";

        private static readonly string[] RankTypes = new string[]
        {
            RankKinds.Year,
            RankKinds.Class,
            RankKinds.Department,
            RankKinds.Tag1,
            RankKinds.Tag2
        };

        public static string SelectSql(IList<MotherGroupExcelIdentity> identities)
        {
            List<string> pairs = new List<string>();
            if (identities != null)
            {
                foreach (MotherGroupExcelIdentity identity in identities)
                {
                    if (identity == null)
                        continue;
                    string number = identity.StudentNumber == null ? "" : identity.StudentNumber.Trim();
                    if (number.Length == 0)
                        continue;
                    string pair = "('" + number.Replace("'", "''") + "', " + identity.Status.ToString() + ")";
                    if (!pairs.Contains(pair))
                        pairs.Add(pair);
                }
            }
            if (pairs.Count == 0)
                throw new ArgumentException("沒有可查詢的學號。");
            return "SELECT\n"
                + "\tstudent.id::TEXT AS student_id\n"
                + "\t, COALESCE(student.student_number, '') AS student_number\n"
                + "\t, COALESCE(student.name, '') AS student_name\n"
                + "\t, student.status AS student_status\n"
                + "FROM student\n"
                + "WHERE (student.student_number, student.status) IN (" + string.Join(", ", pairs.ToArray()) + ")";
        }

        public static List<MotherGroupExcelIdentity> IdentityKeys(Workbook workbook)
        {
            List<MotherGroupExcelIdentity> identities = new List<MotherGroupExcelIdentity>();
            if (workbook == null)
                return identities;
            Worksheet sheet = FindSheet(workbook);
            if (sheet == null || HeaderError(sheet).Length > 0 || ExtraColumnIssues(sheet).Count > 0)
                return identities;
            int lastRow = sheet.Cells.MaxDataRow;
            for (int rowIndex = 1; rowIndex <= lastRow; rowIndex++)
            {
                string number = CellText(sheet.Cells[rowIndex, 0]).Trim();
                int status;
                if (number.Length == 0 || !MotherGroupExcelExchange.TryStatusCode(CellText(sheet.Cells[rowIndex, 2]).Trim(), out status))
                    continue;
                if (ContainsIdentity(identities, number, status))
                    continue;
                identities.Add(MotherGroupExcelIdentity.Create(number, status));
            }
            return identities;
        }

        public static MotherGroupExcelValidation Validate(Workbook workbook, DataTable students)
        {
            MotherGroupExcelValidation validation = MotherGroupExcelValidation.Create();
            if (workbook == null)
            {
                validation.Issues.Add(MotherGroupExcelIssue.Create(0, "", "沒有讀到 Excel 檔案。"));
                return validation;
            }

            Worksheet sheet = FindSheet(workbook);
            if (sheet == null)
            {
                validation.Issues.Add(MotherGroupExcelIssue.Create(0, "", "找不到工作表「母群」。"));
                return validation;
            }
            string headerError = HeaderError(sheet);
            if (headerError.Length > 0)
            {
                validation.Issues.Add(MotherGroupExcelIssue.Create(1, "", headerError));
                return validation;
            }
            List<MotherGroupExcelIssue> extraColumns = ExtraColumnIssues(sheet);
            if (extraColumns.Count > 0)
            {
                validation.Issues.AddRange(extraColumns);
                return validation;
            }

            List<ParsedRow> rows = ReadRows(sheet, validation.Issues);
            if (!HasImportableRow(rows))
            {
                validation.Issues.Add(MotherGroupExcelIssue.Create(0, "", "Excel 沒有可匯入的學生資料。"));
                return validation;
            }
            MarkDuplicates(rows, validation.Issues);
            if (NeedsLookup(rows))
                MatchStudents(rows, students, validation.Issues);
            MarkDuplicateStudentIds(rows, validation.Issues);

            if (validation.Issues.Count > 0)
                return validation;

            validation.IsValid = true;
            foreach (ParsedRow row in rows)
            {
                if (row.Empty)
                    continue;
                validation.Students.Add(row.Student);
            }
            return validation;
        }

        private static bool ContainsIdentity(List<MotherGroupExcelIdentity> identities, string studentNumber, int status)
        {
            foreach (MotherGroupExcelIdentity identity in identities)
            {
                if (identity.StudentNumber == studentNumber && identity.Status == status)
                    return true;
            }
            return false;
        }

        private static Worksheet FindSheet(Workbook workbook)
        {
            foreach (Worksheet sheet in workbook.Worksheets)
            {
                if (sheet.Name == SheetName)
                    return sheet;
            }
            return null;
        }

        private static string HeaderError(Worksheet sheet)
        {
            for (int column = 0; column < MotherGroupExcelExchange.Headers.Length; column++)
            {
                if (CellText(sheet.Cells[0, column]) != MotherGroupExcelExchange.Headers[column])
                    return "第 1 列欄位不符。應為：" + string.Join("、", MotherGroupExcelExchange.Headers);
            }
            return "";
        }

        private static List<MotherGroupExcelIssue> ExtraColumnIssues(Worksheet sheet)
        {
            List<MotherGroupExcelIssue> issues = new List<MotherGroupExcelIssue>();
            int lastColumn = sheet.Cells.MaxDataColumn;
            int lastRow = sheet.Cells.MaxDataRow;
            if (lastColumn < MotherGroupExcelExchange.Headers.Length || lastRow < 0)
                return issues;
            for (int rowIndex = 0; rowIndex <= lastRow; rowIndex++)
            {
                for (int column = MotherGroupExcelExchange.Headers.Length; column <= lastColumn; column++)
                {
                    if (CellText(sheet.Cells[rowIndex, column]).Trim().Length == 0)
                        continue;
                    issues.Add(MotherGroupExcelIssue.Create(rowIndex + 1, "", "第 " + (rowIndex + 1) + " 列第 " + (column + 1) + " 欄有額外資料。"));
                }
            }
            return issues;
        }

        private static bool HasImportableRow(List<ParsedRow> rows)
        {
            foreach (ParsedRow row in rows)
            {
                if (!row.Empty)
                    return true;
            }
            return false;
        }

        private static List<ParsedRow> ReadRows(Worksheet sheet, List<MotherGroupExcelIssue> issues)
        {
            List<ParsedRow> rows = new List<ParsedRow>();
            int lastRow = sheet.Cells.MaxDataRow;
            for (int rowIndex = 1; rowIndex <= lastRow; rowIndex++)
            {
                string[] values = new string[MotherGroupExcelExchange.Headers.Length];
                bool any = false;
                for (int column = 0; column < values.Length; column++)
                {
                    values[column] = CellText(sheet.Cells[rowIndex, column]);
                    if (values[column].Trim().Length > 0)
                        any = true;
                }
                ParsedRow parsed = new ParsedRow();
                parsed.RowNumber = rowIndex + 1;
                if (!any)
                {
                    parsed.Empty = true;
                    rows.Add(parsed);
                    continue;
                }
                parsed.StudentNumber = values[0].Trim();
                parsed.StatusText = values[2].Trim();
                if (parsed.StudentNumber.Length == 0)
                {
                    issues.Add(MotherGroupExcelIssue.Create(parsed.RowNumber, "", "第 " + parsed.RowNumber + " 列學號是空白。"));
                    parsed.Blocked = true;
                }
                if (parsed.StatusText.Length == 0)
                {
                    issues.Add(MotherGroupExcelIssue.Create(parsed.RowNumber, parsed.StudentNumber, "第 " + parsed.RowNumber + " 列學生狀態是空白。"));
                    parsed.Blocked = true;
                }
                else
                {
                    int status;
                    if (!MotherGroupExcelExchange.TryStatusCode(parsed.StatusText, out status))
                    {
                        issues.Add(MotherGroupExcelIssue.Create(parsed.RowNumber, parsed.StudentNumber, "第 " + parsed.RowNumber + " 列的學生狀態「" + parsed.StatusText + "」不在對照表。"));
                        parsed.Blocked = true;
                    }
                    else
                    {
                        parsed.StatusCode = status;
                        parsed.HasStatus = true;
                    }
                }
                List<RankSlot> slots = new List<RankSlot>();
                bool participationValid = true;
                for (int rank = 0; rank < RankTypes.Length; rank++)
                {
                    string participation = values[8 + rank];
                    if (participation != "參與" && participation != "不參與")
                    {
                        issues.Add(MotherGroupExcelIssue.Create(parsed.RowNumber, parsed.StudentNumber, "第 " + parsed.RowNumber + " 列的" + ParticipationHeader(rank) + "不是「參與」或「不參與」。"));
                        participationValid = false;
                    }
                    slots.Add(new RankSlot
                    {
                        RankType = RankTypes[rank],
                        GroupName = values[3 + rank],
                        Participate = participation == "參與"
                    });
                }
                if (!participationValid)
                    parsed.Blocked = true;
                parsed.Student = new MotherGroupExcelStudent();
                parsed.Student.RowNumber = parsed.RowNumber;
                parsed.Student.StudentNumber = parsed.StudentNumber;
                parsed.Student.Status = parsed.HasStatus ? (int?)parsed.StatusCode : null;
                parsed.Student.ExcelName = values[1];
                parsed.Student.DatabaseName = "";
                parsed.Student.NameWarning = "";
                parsed.Student.Slots = slots;
                rows.Add(parsed);
            }
            return rows;
        }

        private static void MarkDuplicates(List<ParsedRow> rows, List<MotherGroupExcelIssue> issues)
        {
            Dictionary<string, int> firstRow = new Dictionary<string, int>();
            foreach (ParsedRow row in rows)
            {
                if (row.Empty || row.StudentNumber.Length == 0 || !row.HasStatus)
                    continue;
                string key = row.StudentNumber + "\n" + row.StatusCode.ToString();
                int earlier;
                if (firstRow.TryGetValue(key, out earlier))
                {
                    issues.Add(MotherGroupExcelIssue.Create(row.RowNumber, row.StudentNumber, "學號「" + row.StudentNumber + "」、學生狀態「" + row.StatusText + "」在第 " + earlier + " 列與第 " + row.RowNumber + " 列重複。"));
                    row.Blocked = true;
                    BlockIdentity(rows, row.StudentNumber, row.StatusCode);
                }
                else
                {
                    firstRow.Add(key, row.RowNumber);
                }
            }
        }

        private static void BlockIdentity(List<ParsedRow> rows, string studentNumber, int status)
        {
            foreach (ParsedRow row in rows)
            {
                if (!row.Empty && row.HasStatus && row.StudentNumber == studentNumber && row.StatusCode == status)
                    row.Blocked = true;
            }
        }

        private static bool NeedsLookup(List<ParsedRow> rows)
        {
            foreach (ParsedRow row in rows)
            {
                if (!row.Empty && !row.Blocked && row.StudentNumber.Length > 0 && row.HasStatus)
                    return true;
            }
            return false;
        }

        private static void MatchStudents(List<ParsedRow> rows, DataTable students, List<MotherGroupExcelIssue> issues)
        {
            if (students == null)
            {
                issues.Add(MotherGroupExcelIssue.Create(0, "", "沒有讀到學生資料，沒有套用匯入結果。"));
                return;
            }
            string missing = MissingColumn(students);
            if (missing.Length > 0)
            {
                issues.Add(MotherGroupExcelIssue.Create(0, "", "學生查詢缺少欄位：" + missing + "。沒有套用匯入結果。"));
                return;
            }

            foreach (ParsedRow row in rows)
            {
                if (row.Empty || row.StudentNumber.Length == 0 || !row.HasStatus || row.Blocked)
                    continue;
                List<DataRow> matches = new List<DataRow>();
                foreach (DataRow dataRow in students.Rows)
                {
                    int status;
                    if (!int.TryParse(Cell(dataRow, "student_status").Trim(), out status))
                        continue;
                    if (Cell(dataRow, "student_number").Trim() == row.StudentNumber && status == row.StatusCode)
                        matches.Add(dataRow);
                }
                if (matches.Count == 0)
                {
                    issues.Add(MotherGroupExcelIssue.Create(row.RowNumber, row.StudentNumber, "找不到學號「" + row.StudentNumber + "」、學生狀態「" + row.StatusText + "」的學生。"));
                    row.Blocked = true;
                    continue;
                }
                if (matches.Count > 1)
                {
                    issues.Add(MotherGroupExcelIssue.Create(row.RowNumber, row.StudentNumber, "學號「" + row.StudentNumber + "」、學生狀態「" + row.StatusText + "」對應多位學生，沒有任選。"));
                    row.Blocked = true;
                    continue;
                }
                string studentId = Cell(matches[0], "student_id").Trim();
                if (studentId.Length == 0)
                {
                    issues.Add(MotherGroupExcelIssue.Create(row.RowNumber, row.StudentNumber, "學號「" + row.StudentNumber + "」、學生狀態「" + row.StatusText + "」的學生沒有識別。"));
                    row.Blocked = true;
                    continue;
                }
                row.Student.StudentId = studentId;
                row.Student.DatabaseName = Cell(matches[0], "student_name");
                if (row.Student.ExcelName != row.Student.DatabaseName)
                    row.Student.NameWarning = "學號「" + row.StudentNumber + "」、學生狀態「" + row.StatusText + "」的 Excel 姓名與學生主檔不同，身分仍以學號與學生狀態對到的學生為準。";
            }
        }

        private static void MarkDuplicateStudentIds(List<ParsedRow> rows, List<MotherGroupExcelIssue> issues)
        {
            Dictionary<string, int> firstRow = new Dictionary<string, int>();
            foreach (ParsedRow row in rows)
            {
                if (row.Empty || row.Student == null || string.IsNullOrEmpty(row.Student.StudentId))
                    continue;
                int earlier;
                if (firstRow.TryGetValue(row.Student.StudentId, out earlier))
                {
                    issues.Add(MotherGroupExcelIssue.Create(row.RowNumber, row.StudentNumber, "學生識別「" + row.Student.StudentId + "」在第 " + earlier + " 列與第 " + row.RowNumber + " 列重複。"));
                    row.Blocked = true;
                    BlockStudentId(rows, row.Student.StudentId);
                }
                else
                {
                    firstRow.Add(row.Student.StudentId, row.RowNumber);
                }
            }
        }

        private static void BlockStudentId(List<ParsedRow> rows, string studentId)
        {
            foreach (ParsedRow row in rows)
            {
                if (!row.Empty && row.Student != null && row.Student.StudentId == studentId)
                    row.Blocked = true;
            }
        }

        private static string ParticipationHeader(int rank)
        {
            return MotherGroupExcelExchange.Headers[8 + rank];
        }

        private static string CellText(Cell cell)
        {
            if (cell == null || cell.Type == CellValueType.IsNull)
                return "";
            return cell.StringValue ?? "";
        }

        private static string MissingColumn(DataTable table)
        {
            string[] columns = new string[] { "student_id", "student_number", "student_name", "student_status" };
            foreach (string column in columns)
            {
                if (!table.Columns.Contains(column))
                    return column;
            }
            return "";
        }

        private static string Cell(DataRow row, string column)
        {
            object value = row[column];
            if (value == null || value == DBNull.Value)
                return "";
            return value.ToString();
        }

        private sealed class ParsedRow
        {
            public int RowNumber;
            public bool Empty;
            public bool Blocked;
            public bool HasStatus;
            public int StatusCode;
            public string StatusText = "";
            public string StudentNumber = "";
            public MotherGroupExcelStudent Student;
        }
    }
}
