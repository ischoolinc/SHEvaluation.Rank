using SHEvaluation.Rank.MotherGroup;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace SHEvaluation.Rank.RegularRank
{
    public static class RegularRankCandidateSql
    {
        // Each ranking kind gets its own population BEFORE the original window
        // functions run. Each grade also stays isolated, as in the old calculator.
        public static string Build(RegularRankConfirmRequest request, MotherGroupConfirmList population)
        {
            if (request == null || population == null) throw new ArgumentNullException("request");
            string error;
            if (RegularRankParticipationGate.PopulationValues(population, out error).Length == 0)
                throw new InvalidOperationException(error);
            List<string> results = new List<string>();
            foreach (int grade in ActualGrades(request, population))
            {
                foreach (string kind in RankKinds.All)
                {
                    List<string> students = new List<string>();
                    foreach (ConfirmStudentRow row in population.Rows)
                    {
                        int parsed;
                        if (SemesterRankSql.TryParseGradeYear(row.Group.GetWorking(RankKinds.Year).GroupName, out parsed)
                            && parsed == grade && row.Group.GetWorking(kind).Participate)
                            students.Add(StudentSql(row.Group, grade));
                    }
                    if (students.Count == 0) continue;
                    string conditions = "SELECT " + Literal(request.TargetSchoolYear) + "::TEXT AS rank_school_year, "
                        + Literal(request.TargetSemester) + "::TEXT AS rank_semester, "
                        + Literal(request.TargetExamId) + "::TEXT AS ref_exam_id, "
                        + Literal(request.TargetExamName) + "::TEXT AS rank_exam_name, "
                        + grade.ToString(CultureInfo.InvariantCulture) + "::INT AS rank_grade_year, "
                        + Literal(RegularRankSnapshotDocument.BuildSettingXml(request)) + "::TEXT AS calculation_setting";
                    string formula = RegularRankFormulaSql.Build(conditions, string.Join("\nUNION ALL\n", students.ToArray()));
                    RegularRankParticipationCoverage coverage = RegularRankParticipationGate.Cover(formula);
                    if (!coverage.Covered) throw new InvalidOperationException(coverage.Message);
                    results.Add("SELECT * FROM (\n" + formula + "\n) AS candidate WHERE rank_type = " + Literal(kind));
                }
            }
            if (results.Count == 0) throw new InvalidOperationException("沒有可計算的參與學生；保留舊排名。");
            return string.Join("\nUNION ALL\n", results.ToArray());
        }

        public static List<int> ActualGrades(RegularRankConfirmRequest request, MotherGroupConfirmList population)
        {
            List<int> grades = new List<int>();
            foreach (ConfirmStudentRow row in population.Rows)
            {
                int grade;
                if (row != null && row.Group != null
                    && SemesterRankSql.TryParseGradeYear(row.Group.GetWorking(RankKinds.Year).GroupName, out grade)
                    && request.GradeYears.Contains(grade) && !grades.Contains(grade)) grades.Add(grade);
            }
            grades.Sort();
            return grades;
        }

        public static string StudentSql(StudentMotherGroup student, int grade)
        {
            long id;
            if (student == null || !long.TryParse(student.StudentId, NumberStyles.None, CultureInfo.InvariantCulture, out id)
                || id <= 0 || id.ToString(CultureInfo.InvariantCulture) != student.StudentId)
                throw new InvalidOperationException("學生 ID 無效。");
            return "SELECT " + student.StudentId + "::BIGINT AS student_id, " + Literal(student.StudentName)
                + "::TEXT AS student_name, " + grade.ToString(CultureInfo.InvariantCulture) + "::INT AS rank_grade_year, "
                + Literal(student.GetWorking(RankKinds.Department).GroupName) + "::TEXT AS rank_dept_name, "
                + Literal(student.GetWorking(RankKinds.Class).GroupName) + "::TEXT AS rank_class_name, "
                + Literal(student.GetWorking(RankKinds.Tag1).GroupName) + "::TEXT AS rank_tag1, "
                + Literal(student.GetWorking(RankKinds.Tag2).GroupName) + "::TEXT AS rank_tag2";
        }

        public static string Literal(string value) { return "'" + (value ?? "").Replace("'", "''") + "'"; }
    }
}
