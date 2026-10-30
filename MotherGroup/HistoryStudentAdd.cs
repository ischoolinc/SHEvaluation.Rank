using System;
using System.Collections.Generic;
using System.Data;

namespace SHEvaluation.Rank.MotherGroup
{
    public enum HistoryStudentAddStatus
    {
        Added,
        NotFound,
        Ambiguous,
        AlreadyListed,
        Unreadable
    }

    public sealed class HistoryStudentAddResult
    {
        public HistoryStudentAddStatus Status { get; private set; }
        public string Message { get; private set; }
        public StudentMotherGroup Student { get; private set; }
        public string ClassName { get; private set; }
        public string SeatNo { get; private set; }

        internal static HistoryStudentAddResult Fail(HistoryStudentAddStatus status, string message)
        {
            HistoryStudentAddResult result = new HistoryStudentAddResult();
            result.Status = status;
            result.Message = message ?? "";
            result.Student = null;
            result.ClassName = "";
            result.SeatNo = "";
            return result;
        }

        internal static HistoryStudentAddResult Ok(StudentMotherGroup student, string className, string seatNo)
        {
            HistoryStudentAddResult result = new HistoryStudentAddResult();
            result.Status = HistoryStudentAddStatus.Added;
            result.Message = "";
            result.Student = student;
            result.ClassName = className ?? "";
            result.SeatNo = seatNo ?? "";
            return result;
        }
    }

    public static class HistoryStudentAdd
    {
        public static string SelectSql(string studentNumber)
        {
            string number = studentNumber == null ? "" : studentNumber.Trim();
            if (number.Length == 0)
                throw new ArgumentException("請輸入學號。");
            return "SELECT\n"
                + "\tstudent.id::TEXT AS student_id\n"
                + "\t, COALESCE(student.student_number, '') AS student_number\n"
                + "\t, COALESCE(student.name, '') AS student_name\n"
                + "\t, COALESCE(class.grade_year::TEXT, '') AS grade_year\n"
                + "\t, COALESCE(class.class_name, '') AS class_name\n"
                + "\t, COALESCE(student.seat_no::TEXT, '') AS seat_no\n"
                + "\t, COALESCE(dept.name, '') AS dept_name\n"
                + "FROM student\n"
                + "LEFT OUTER JOIN class\n"
                + "\tON class.id = student.ref_class_id\n"
                + "LEFT OUTER JOIN dept\n"
                + "\tON dept.id = COALESCE(student.ref_dept_id, class.ref_dept_id)\n"
                + "WHERE student.student_number = '" + number.Replace("'", "''") + "'";
        }

        public static HistoryStudentAddResult Read(
            string studentNumber,
            DataTable table,
            MotherGroupConfirmList list,
            string excludedCategory,
            string tag1Category,
            string tag2Category,
            IList<TagDefinition> tagDefinitions,
            IList<StudentTagLink> studentTags)
        {
            string number = studentNumber == null ? "" : studentNumber.Trim();
            if (table == null)
                return HistoryStudentAddResult.Fail(HistoryStudentAddStatus.Unreadable, "沒有讀到學生資料，沒有新增。");
            string missing = MissingColumn(table);
            if (missing.Length > 0)
                return HistoryStudentAddResult.Fail(HistoryStudentAddStatus.Unreadable, "查詢學生資料缺少欄位：" + missing + "。沒有新增。");
            if (table.Rows.Count == 0)
                return HistoryStudentAddResult.Fail(HistoryStudentAddStatus.NotFound, "找不到學號「" + number + "」的學生，沒有新增。");
            if (table.Rows.Count > 1)
                return HistoryStudentAddResult.Fail(HistoryStudentAddStatus.Ambiguous, "學號「" + number + "」對應多位學生，無法確定要加入哪一位，沒有新增。");

            DataRow row = table.Rows[0];
            string studentId = Cell(row, "student_id").Trim();
            if (studentId.Length == 0)
                return HistoryStudentAddResult.Fail(HistoryStudentAddStatus.Unreadable, "查到的學生沒有識別，沒有新增。");
            if (list != null && list.ContainsIdentity(studentId, number))
                return HistoryStudentAddResult.Fail(HistoryStudentAddStatus.AlreadyListed, "學號「" + number + "」的學生已在確認清單，沒有重複新增。");

            string yearText = Cell(row, "grade_year").Trim();
            int gradeYear;
            string yearName = "";
            if (int.TryParse(yearText, out gradeYear) && gradeYear > 0)
                yearName = gradeYear + "年級";
            string className = Cell(row, "class_name");
            string deptName = Cell(row, "dept_name");
            List<string> tag1Ids = CurrentStudentMotherGroupBuilder.ResolveTagIds(tagDefinitions, tag1Category);
            List<string> tag2Ids = CurrentStudentMotherGroupBuilder.ResolveTagIds(tagDefinitions, tag2Category);
            string tag1Name = CurrentStudentMotherGroupBuilder.FirstTagName(studentTags, studentId, tag1Ids);
            string tag2Name = CurrentStudentMotherGroupBuilder.FirstTagName(studentTags, studentId, tag2Ids);

            StudentMotherGroup student = new StudentMotherGroup();
            student.StudentId = studentId;
            student.StudentNumber = Cell(row, "student_number");
            student.StudentName = Cell(row, "student_name");
            student.SetSourceSlots(new RankSlot[]
            {
                new RankSlot { RankType = RankKinds.Year, GroupName = yearName, Participate = true },
                new RankSlot { RankType = RankKinds.Department, GroupName = deptName, Participate = true },
                new RankSlot { RankType = RankKinds.Class, GroupName = className, Participate = true },
                new RankSlot { RankType = RankKinds.Tag1, GroupName = tag1Name, Participate = tag1Name != "" },
                new RankSlot { RankType = RankKinds.Tag2, GroupName = tag2Name, Participate = tag2Name != "" }
            });
            List<string> excludedIds = CurrentStudentMotherGroupBuilder.ResolveTagIds(tagDefinitions, excludedCategory);
            MotherGroupInitialParticipation.Apply(MotherGroupSourceKind.Current, student, HasTag(studentTags, studentId, excludedIds));
            return HistoryStudentAddResult.Ok(student, className, Cell(row, "seat_no"));
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

        private static string MissingColumn(DataTable table)
        {
            string[] columns = new string[] { "student_id", "student_number", "student_name", "grade_year", "class_name", "seat_no", "dept_name" };
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
    }
}
