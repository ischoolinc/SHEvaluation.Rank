using System.Collections.Generic;
using System.Globalization;

namespace SHEvaluation.Rank.RegularRank
{
    public static class RegularRankSubjectCatalog
    {
        public static bool TryCreate(
            string schoolYear,
            string semester,
            string examId,
            IList<string> studentIds,
            out string sql,
            out string error)
        {
            sql = "";
            string year;
            string term;
            string exam;
            if (!TryCanonical(schoolYear, "目標學年度", out year, out error))
                return false;
            if (!TryCanonical(semester, "目標學期", out term, out error))
                return false;
            if (term != "1" && term != "2")
            {
                error = "目標學期必須是 1 或 2。";
                return false;
            }
            if (!TryCanonical(examId, "目標 exam ID", out exam, out error))
                return false;

            List<string> ids = new List<string>();
            if (studentIds != null)
            {
                foreach (string studentId in studentIds)
                {
                    string canonical;
                    if (!TryCanonical(studentId, "學生識別", out canonical, out error))
                        return false;
                    if (!ids.Contains(canonical))
                        ids.Add(canonical);
                }
            }
            if (ids.Count == 0)
            {
                error = "沒有可解析排名年級的母群學生，不能查科目。";
                return false;
            }

            sql = @"
SELECT
	subject
FROM (
	SELECT
		subject
		, row_number() OVER () as subject_order
	FROM (
		SELECT DISTINCT
			course.credit
			, course.period
			, course.subject
		FROM
			sc_attend
			INNER JOIN course
				ON course.id = sc_attend.ref_course_id
				AND course.school_year = " + year + @"
				AND course.semester = " + term + @"
			INNER JOIN sce_take
				ON sce_take.ref_sc_attend_id = sc_attend.id
				AND sce_take.ref_exam_id = " + exam + @"
		WHERE
			sc_attend.ref_student_id IN (" + string.Join(", ", ids.ToArray()) + @")
		ORDER BY
			course.credit DESC
			, course.period DESC
			, course.subject
	) AS s
) AS s
GROUP BY
	subject
ORDER BY
	MIN(subject_order)";
            error = "";
            return true;
        }

        private static bool TryCanonical(string text, string label, out string canonical, out string error)
        {
            canonical = "";
            string trimmed = (text ?? "").Trim();
            long number;
            if (trimmed.Length == 0
                || !long.TryParse(trimmed, NumberStyles.None, CultureInfo.InvariantCulture, out number)
                || number <= 0
                || number.ToString(CultureInfo.InvariantCulture) != trimmed)
            {
                error = label + "必須是不含空白、正負號或前導零的正整數。";
                return false;
            }
            canonical = trimmed;
            error = "";
            return true;
        }
    }
}
