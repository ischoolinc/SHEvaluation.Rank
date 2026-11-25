using SHEvaluation.Rank.MotherGroup;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace SHEvaluation.Rank.RegularRank
{
    public static class RegularRankResultVerification
    {
        // A read-only comparison of every result column, retaining duplicate rows.
        // It deliberately tests the all-participating hypothesis on a copy, not
        // the user's current flags and never the persisted ranking tables.
        public static string AllParticipationSql(RegularRankConfirmRequest request, MotherGroupConfirmList population)
        {
            MotherGroupConfirmList copy = population.CopyWorking();
            foreach (ConfirmStudentRow row in copy.Rows) foreach (RankSlot slot in row.Group.Working) slot.Participate = true;
            List<string> baseline = new List<string>();
            foreach (int grade in RegularRankCandidateSql.ActualGrades(request, copy))
            {
                List<string> students = new List<string>();
                foreach (ConfirmStudentRow row in copy.Rows)
                {
                    int parsed;
                    if (SemesterRankSql.TryParseGradeYear(row.Group.GetWorking(RankKinds.Year).GroupName, out parsed) && parsed == grade)
                        students.Add(RegularRankCandidateSql.StudentSql(row.Group, grade));
                }
                string condition = "SELECT " + RegularRankCandidateSql.Literal(request.TargetSchoolYear) + "::TEXT AS rank_school_year, "
                    + RegularRankCandidateSql.Literal(request.TargetSemester) + "::TEXT AS rank_semester, "
                    + RegularRankCandidateSql.Literal(request.TargetExamId) + "::TEXT AS ref_exam_id, "
                    + grade.ToString(CultureInfo.InvariantCulture) + "::INT AS rank_grade_year, "
                    + RegularRankCandidateSql.Literal(RegularRankSnapshotDocument.BuildSettingXml(request)) + "::TEXT AS calculation_setting";
                baseline.Add("SELECT * FROM (" + RegularRankFormulaSql.Build(condition, string.Join("\nUNION ALL\n", students.ToArray())) + ") AS baseline_grade");
            }
            if (baseline.Count == 0) throw new InvalidOperationException("沒有可核對的年級。");
            return "WITH original_result AS (" + string.Join("\nUNION ALL\n", baseline.ToArray()) + "), actual_result AS ("
                + RegularRankCandidateSql.Build(request, copy) + ")\nSELECT"
                + " (SELECT count(*) FROM original_result) AS original_count,"
                + " (SELECT count(*) FROM actual_result) AS actual_count,"
                + " (SELECT count(*) FROM (SELECT * FROM original_result EXCEPT ALL SELECT * FROM actual_result) missing_rows) AS missing_count,"
                + " (SELECT count(*) FROM (SELECT * FROM actual_result EXCEPT ALL SELECT * FROM original_result) extra_rows) AS extra_count";
        }
    }
}
