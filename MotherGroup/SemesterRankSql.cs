using System;
using System.Collections.Generic;
using System.Text;

namespace SHEvaluation.Rank.MotherGroup
{
    public sealed class SemesterRankStudentInput
    {
        public long StudentId { get; set; }
        public string StudentName { get; set; }
        public int GradeYear { get; set; }
        public string DeptName { get; set; }
        public string ClassName { get; set; }
        public string Tag1 { get; set; }
        public string Tag2 { get; set; }
        public bool ParticipateYear { get; set; }
        public bool ParticipateDept { get; set; }
        public bool ParticipateClass { get; set; }
        public bool ParticipateTag1 { get; set; }
        public bool ParticipateTag2 { get; set; }
    }

    public sealed class SemesterRankGradeStudents
    {
        public int GradeYear { get; set; }
        public List<string> Selects { get; set; }
        public List<SemesterRankStudentInput> Students { get; set; }
    }

    public sealed class SemesterRankStudentSqlResult
    {
        public List<SemesterRankGradeStudents> Grades { get; set; }
        public List<string> SkippedWithoutGrade { get; set; }

        public SemesterRankStudentSqlResult()
        {
            Grades = new List<SemesterRankGradeStudents>();
            SkippedWithoutGrade = new List<string>();
        }
    }

    public static class SemesterRankSql
    {
        public static SemesterRankStudentSqlResult Build(MotherGroupConfirmList list)
        {
            return Collect(list, false);
        }

        public static SemesterRankStudentSqlResult BuildLegacySelects(MotherGroupConfirmList list)
        {
            return Collect(list, true);
        }

        private static SemesterRankStudentSqlResult Collect(MotherGroupConfirmList list, bool legacySelects)
        {
            SemesterRankStudentSqlResult result = new SemesterRankStudentSqlResult();
            if (list == null)
                return result;

            Dictionary<int, SemesterRankGradeStudents> byGrade = new Dictionary<int, SemesterRankGradeStudents>();
            Dictionary<string, bool> seen = new Dictionary<string, bool>();
            foreach (ConfirmStudentRow row in list.Rows)
            {
                if (row == null || row.Group == null || string.IsNullOrEmpty(row.Group.StudentId))
                    continue;
                int gradeYear;
                if (!TryParseGradeYear(row.Group.GetWorking(RankKinds.Year).GroupName, out gradeYear))
                {
                    result.SkippedWithoutGrade.Add(row.Group.StudentId);
                    continue;
                }
                SemesterRankGradeStudents grade;
                if (!byGrade.TryGetValue(gradeYear, out grade))
                {
                    grade = new SemesterRankGradeStudents();
                    grade.GradeYear = gradeYear;
                    grade.Selects = new List<string>();
                    grade.Students = new List<SemesterRankStudentInput>();
                    byGrade.Add(gradeYear, grade);
                    result.Grades.Add(grade);
                }
                if (legacySelects)
                {
                    grade.Selects.Add(StudentSelect(row.Group, gradeYear));
                    continue;
                }
                SemesterRankStudentInput input = SemesterRankStudentJson.CreateInput(row.Group, gradeYear);
                if (seen.ContainsKey(input.StudentId.ToString()))
                    throw new InvalidOperationException("學生 " + input.StudentId.ToString() + " 在確認清單重複，不建立排名學生清單。本次不會取第一筆。");
                seen.Add(input.StudentId.ToString(), true);
                grade.Students.Add(input);
            }
            return result;
        }

        public static bool TryParseGradeYear(string yearName, out int gradeYear)
        {
            gradeYear = 0;
            if (yearName == null)
                return false;
            string text = yearName.Trim().Trim('年', '級');
            int parsed;
            if (!int.TryParse(text, out parsed) || parsed <= 0)
                return false;
            gradeYear = parsed;
            return true;
        }

        public static string ApplyParticipation(string sql)
        {
            if (sql == null)
                throw new ArgumentNullException("sql");
            string text = sql.Replace("\r\n", "\n").Replace("\r", "\n");
            text = InsertBefore(text, "array_to_string(xpath('/root/Subject/@科目'", ScoreFlags());
            text = InsertBefore(text, "array_to_string(xpath('/root/Entry/@分項'", ScoreFlags());
            text = InsertBefore(text, "subject_origin_score AS origin_score", RankFlags());
            text = InsertBefore(text, "subject_greatest_score AS greatest_score", RankFlags());
            text = InsertBefore(text, "\t\t, entry_score\n", RankFlags());
            text = ReplacePartition(text, "PARTITION BY rank_grade_year, rank_dept_name, subject", "participate_dept");
            text = ReplacePartition(text, "PARTITION BY rank_grade_year, rank_dept_name, item_name", "participate_dept");
            text = ReplacePartition(text, "PARTITION BY rank_grade_year, rank_dept_name, entry", "participate_dept");
            text = ReplacePartition(text, "PARTITION BY rank_grade_year, rank_tag1, subject", "participate_tag1");
            text = ReplacePartition(text, "PARTITION BY rank_grade_year, rank_tag1, item_name", "participate_tag1");
            text = ReplacePartition(text, "PARTITION BY rank_grade_year, rank_tag1, entry", "participate_tag1");
            text = ReplacePartition(text, "PARTITION BY rank_grade_year, rank_tag2, subject", "participate_tag2");
            text = ReplacePartition(text, "PARTITION BY rank_grade_year, rank_tag2, item_name", "participate_tag2");
            text = ReplacePartition(text, "PARTITION BY rank_grade_year, rank_tag2, entry", "participate_tag2");
            text = ReplacePartition(text, "PARTITION BY rank_class_name, subject", "participate_class");
            text = ReplacePartition(text, "PARTITION BY rank_class_name, item_name", "participate_class");
            text = ReplacePartition(text, "PARTITION BY rank_class_name, entry", "participate_class");
            text = ReplacePartition(text, "PARTITION BY rank_grade_year, subject", "participate_year");
            text = ReplacePartition(text, "PARTITION BY rank_grade_year, item_name", "participate_year");
            text = ReplacePartition(text, "PARTITION BY rank_grade_year, entry", "participate_year");
            return FilterScoreList(text);
        }

        public static string StudentSelect(StudentMotherGroup group, int gradeYear)
        {
            RankSlot year = group.GetWorking(RankKinds.Year);
            RankSlot dept = group.GetWorking(RankKinds.Department);
            RankSlot classSlot = group.GetWorking(RankKinds.Class);
            RankSlot tag1 = group.GetWorking(RankKinds.Tag1);
            RankSlot tag2 = group.GetWorking(RankKinds.Tag2);
            StringBuilder sql = new StringBuilder();
            sql.Append("SELECT\n'");
            sql.Append(Literal(group.StudentId)).Append("'::BIGINT AS student_id\n, '");
            sql.Append(Literal(group.StudentName)).Append("'::TEXT AS student_name\n, '");
            sql.Append(gradeYear).Append("'::INT AS rank_grade_year\n, '");
            sql.Append(Literal(dept.GroupName)).Append("'::TEXT AS rank_dept_name\n, '");
            sql.Append(Literal(classSlot.GroupName)).Append("'::TEXT AS rank_class_name\n, '");
            sql.Append(Literal(tag1.GroupName)).Append("'::TEXT AS rank_tag1\n, '");
            sql.Append(Literal(tag2.GroupName)).Append("'::TEXT AS rank_tag2\n, ");
            sql.Append(SqlBool(year.Participate)).Append(" AS participate_year\n, ");
            sql.Append(SqlBool(dept.Participate)).Append(" AS participate_dept\n, ");
            sql.Append(SqlBool(classSlot.Participate)).Append(" AS participate_class\n, ");
            sql.Append(SqlBool(tag1.Participate)).Append(" AS participate_tag1\n, ");
            sql.Append(SqlBool(tag2.Participate)).Append(" AS participate_tag2\n");
            return sql.ToString();
        }

        private static string FilterScoreList(string sql)
        {
            const string startMark = ", score_list AS";
            const string endMark = ", update_data AS";
            int start = sql.IndexOf(startMark);
            int end = sql.IndexOf(endMark);
            if (start < 0 || end < start)
                throw new InvalidOperationException("排名 SQL 缺少 score_list 或 update_data。");
            string body = sql.Substring(start, end - start);
            string[] parts = body.Split(new string[] { "UNION ALL" }, StringSplitOptions.None);
            for (int i = 0; i < parts.Length; i++)
            {
                string flag = FlagForBranch(parts[i]);
                if (flag == null)
                    continue;
                parts[i] = AddParticipateFilter(parts[i], flag);
            }
            return sql.Substring(0, start) + string.Join("UNION ALL", parts) + sql.Substring(end);
        }

        private static string FlagForBranch(string branch)
        {
            if (branch.IndexOf("'類別1排名'::TEXT AS rank_type") >= 0)
                return "participate_tag1";
            if (branch.IndexOf("'類別2排名'::TEXT AS rank_type") >= 0)
                return "participate_tag2";
            if (branch.IndexOf("'科排名'::TEXT AS rank_type") >= 0)
                return "participate_dept";
            if (branch.IndexOf("'班排名'::TEXT AS rank_type") >= 0)
                return "participate_class";
            if (branch.IndexOf("'年排名'::TEXT AS rank_type") >= 0)
                return "participate_year";
            return null;
        }

        private static string AddParticipateFilter(string branch, string flag)
        {
            int fromAt = branch.LastIndexOf("FROM");
            if (fromAt < 0)
                throw new InvalidOperationException("score_list 分支缺少 FROM。");
            if (branch.Substring(fromAt).IndexOf(flag) >= 0)
                return branch;
            int end = branch.Length;
            while (end > 0 && char.IsWhiteSpace(branch[end - 1]))
                end--;
            bool closesCte = end > fromAt && branch[end - 1] == ')';
            string filter = branch.Substring(fromAt).IndexOf("WHERE") >= 0
                ? "\t\tAND " + flag + "\n"
                : "\tWHERE\n\t\t" + flag + "\n";
            if (closesCte)
                return branch.Substring(0, end - 1) + filter + branch.Substring(end - 1);
            return branch.Substring(0, end) + "\n" + filter + branch.Substring(end);
        }

        private static string ReplacePartition(string sql, string partition, string flag)
        {
            string withFlag = partition + ", " + flag;
            string[] tails = new string[] { " ", ")", "\n" };
            bool pending = false;
            foreach (string tail in tails)
            {
                if (sql.IndexOf(partition + tail) >= 0)
                    pending = true;
            }
            if (!pending)
            {
                if (sql.IndexOf(withFlag) >= 0)
                    return sql;
                throw new InvalidOperationException("排名 SQL 缺少窗口分組：" + partition);
            }
            foreach (string tail in tails)
                sql = sql.Replace(partition + tail, withFlag + tail);
            return sql;
        }

        private static string InsertBefore(string sql, string marker, string columns)
        {
            int at = sql.IndexOf(marker);
            if (at < 0)
                throw new InvalidOperationException("排名 SQL 缺少欄位插入位置：" + marker);
            int line = sql.LastIndexOf('\n', at);
            line = line < 0 ? 0 : line + 1;
            int look = Math.Max(0, line - 400);
            if (sql.Substring(look, line - look).IndexOf("participate_year") >= 0)
                return sql;
            return sql.Insert(line, columns);
        }

        private static string ScoreFlags()
        {
            return "\t\t, student_list.participate_year\n\t\t, student_list.participate_dept\n\t\t, student_list.participate_class\n\t\t, student_list.participate_tag1\n\t\t, student_list.participate_tag2\n";
        }

        private static string RankFlags()
        {
            return "\t\t, participate_year\n\t\t, participate_dept\n\t\t, participate_class\n\t\t, participate_tag1\n\t\t, participate_tag2\n";
        }

        private static string Literal(string value)
        {
            return (value ?? "").Replace("'", "''");
        }

        private static string SqlBool(bool value)
        {
            return value ? "true" : "false";
        }
    }
}
