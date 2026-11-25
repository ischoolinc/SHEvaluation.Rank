using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SHEvaluation.Rank.DAO
{
    /// <summary>
    /// Regular assessment retrieval helpers. Only regular (定期評量) matrices;
    /// never mixes semester ranks. Used by RegularAssessmentRankSelect and offline checks.
    /// </summary>
    public static class RegularAssessmentRankQuery
    {
        public const string ScoreType = "定期評量";

        public static string AliveFilterOptionsSql()
        {
            return @"
SELECT 
    rank_matrix.school_year
	, rank_matrix.semester
    , SUBSTRING(rank_matrix.item_type, 1, position('/' in rank_matrix.item_type) - 1) as score_type
	, SUBSTRING(rank_matrix.item_type, position('/' in rank_matrix.item_type) + 1, LENGTH(rank_matrix.item_type)) as score_category
    , rank_matrix.ref_exam_id
    , rank_matrix.ref_batch_id
FROM 
    rank_matrix 
WHERE 
    rank_matrix.is_alive = true
	AND SUBSTRING(rank_matrix.item_type, 1, position('/' in rank_matrix.item_type) - 1) = '定期評量'";
        }

        public static string DetailSql(string schoolYear, string semester, string scoreCategory, string gradeYear, string rankType)
        {
            if (string.IsNullOrEmpty(schoolYear) || string.IsNullOrEmpty(semester))
                return "";
            StringBuilder sql = new StringBuilder();
            sql.Append(@"
SELECT 
    *
FROM
(
    SELECT 
        rank_matrix.id AS rank_matrix_id 
        , SUBSTRING(rank_matrix.item_type, 1, position('/' in rank_matrix.item_type) - 1) AS score_type
        , SUBSTRING(rank_matrix.item_type, position('/' in rank_matrix.item_type) + 1, LENGTH(rank_matrix.item_type)) AS score_category 
        , exam.exam_name 
        , rank_matrix.item_name 
        , rank_matrix.rank_type 
        , rank_matrix.rank_name 
        , class.class_name 
        , student.seat_no 
        , student.student_number
        , student.name AS student_name
        , rank_detail.score
        , rank_detail.rank
        , rank_detail.pr
        , rank_detail.percentile
        , rank_matrix.school_year
        , rank_matrix.semester 
        , rank_matrix.create_time
        , rank_detail.ref_student_id
        , rank_matrix.grade_year
        , rank_matrix.item_type
        , rank_matrix.ref_exam_id
        , rank_matrix.ref_batch_id
    FROM rank_matrix 
        LEFT OUTER JOIN 
            rank_detail ON rank_detail.ref_matrix_id = rank_matrix.id 
        LEFT OUTER JOIN 
            student ON student.id = rank_detail.ref_student_id 
        LEFT OUTER JOIN 
            class ON class.id = student.ref_class_id 
        LEFT OUTER JOIN 
            exam ON exam.id=rank_matrix.ref_exam_id
    WHERE 
        rank_matrix.is_alive = true
        AND rank_matrix.item_type LIKE '定期評量/%'
) AS Rank_Table
WHERE  
    school_year = ").Append(LiteralNumber(schoolYear)).Append(@"
    And semester = ").Append(LiteralNumber(semester)).Append(@"
    And score_type = '定期評量'
    And score_category = '").Append(Literal(scoreCategory)).Append("'");
            if (!string.IsNullOrEmpty(gradeYear))
                sql.Append(" AND grade_year = '").Append(Literal(gradeYear)).Append("'");
            if (!string.IsNullOrEmpty(rankType))
                sql.Append(" AND rank_type='").Append(Literal(rankType)).Append("'");
            return sql.ToString();
        }

        public static string MatrixBatchJoinSql(long sourceMatrixId, long studentId)
        {
            if (sourceMatrixId <= 0 || studentId <= 0)
                throw new ArgumentOutOfRangeException("matrix or student id");
            return @"
SELECT 
	rank_matrix.id AS rank_matrix_id
    , rank_matrix.ref_batch_id
	, SUBSTRING(rank_matrix.item_type, 1, position('/' in rank_matrix.item_type) - 1) as score_type
	, SUBSTRING(rank_matrix.item_type, position('/' in rank_matrix.item_type) + 1, LENGTH(rank_matrix.item_type)) as score_category 
	, exam.exam_name 
	, rank_matrix.item_name 
	, rank_matrix.rank_type
	, rank_matrix.rank_name
	, rank_matrix.school_year
	, rank_matrix.semester 
	, rank_matrix.is_alive
	, rank_matrix.create_time
	, rank_matrix.matrix_count
    , rank_matrix.ref_exam_id
FROM 
	rank_matrix AS source
    INNER JOIN rank_matrix
		ON rank_matrix.school_year = source.school_year
		AND rank_matrix.semester = source.semester
		AND rank_matrix.grade_year = source.grade_year
		AND rank_matrix.item_type = source.item_type
		AND rank_matrix.ref_exam_id = source.ref_exam_id
		AND rank_matrix.item_name = source.item_name
		AND rank_matrix.rank_type = source.rank_type
		AND rank_matrix.rank_name = source.rank_name
	LEFT OUTER JOIN exam 
        ON exam.id=rank_matrix.ref_exam_id
WHERE
	source.id = " + sourceMatrixId.ToString(CultureInfo.InvariantCulture) + @"::BIGINT
    AND source.item_type LIKE '定期評量/%'
    AND rank_matrix.id IN (
        SELECT ref_matrix_id FROM rank_detail WHERE ref_student_id = " + studentId.ToString(CultureInfo.InvariantCulture) + @"::BIGINT
    )
ORDER BY
	create_time DESC";
        }

        public static List<RankDataInfo> Filter(IList<RankDataInfo> source, string examName, string itemName, string rankType, string studentNumber)
        {
            List<RankDataInfo> result = new List<RankDataInfo>();
            if (source == null) return result;
            foreach (RankDataInfo row in source)
            {
                if (row == null) continue;
                if (!string.IsNullOrEmpty(examName) && examName != "全部" && row.exam_name != examName) continue;
                if (!string.IsNullOrEmpty(itemName) && itemName != "全部" && row.item_name != itemName) continue;
                if (!string.IsNullOrEmpty(rankType) && rankType != "全部" && row.rank_type != rankType) continue;
                if (!string.IsNullOrEmpty(studentNumber) && (row.student_number == null || row.student_number.IndexOf(studentNumber, StringComparison.Ordinal) < 0)) continue;
                result.Add(row);
            }
            return result;
        }

        private static string Literal(string value)
        {
            return (value ?? "").Replace("'", "''");
        }

        private static string LiteralNumber(string value)
        {
            long number;
            if (!long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out number))
                throw new ArgumentException("期間必須為整數。");
            return number.ToString(CultureInfo.InvariantCulture);
        }
    }
}
