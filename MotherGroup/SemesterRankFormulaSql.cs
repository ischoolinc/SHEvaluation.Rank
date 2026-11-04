using System.Collections.Generic;

namespace SHEvaluation.Rank.MotherGroup
{
    /// <summary>
    /// 學期排名計算公式的執行時 SQL。產品畫面與診斷共用此組句，不從 C# 原始碼截取。
    /// </summary>
    public static class SemesterRankFormulaSql
    {
        public static string BuildOriginOnly()
        {
            return Build(null);
        }

        public static string Build(IList<string> selectedScoreTypes)
        {
            string schoolYearAdjScore = "";
            string handAdjScore = "";
            string makeUpScore = "";
            string retakeScore = "";
            if (Contains(selectedScoreTypes, "學年調整成績"))
                schoolYearAdjScore = ", NULLIF(array_to_string(xpath('/root/Subject/@學年調整成績', xmlparse(content concat('<root>', subj_score_ele , '</root>'))), ''), '')::DECIMAL";
            if (Contains(selectedScoreTypes, "手動調整成績"))
                handAdjScore = ", NULLIF(array_to_string(xpath('/root/Subject/@擇優採計成績', xmlparse(content concat('<root>', subj_score_ele , '</root>'))), ''), '')::DECIMAL";
            if (Contains(selectedScoreTypes, "補考成績"))
                makeUpScore = ", NULLIF(array_to_string(xpath('/root/Subject/@補考成績', xmlparse(content concat('<root>', subj_score_ele , '</root>'))), ''), '')::DECIMAL";
            if (Contains(selectedScoreTypes, "重修成績"))
                retakeScore = ", NULLIF(array_to_string(xpath('/root/Subject/@重修成績', xmlparse(content concat('<root>', subj_score_ele , '</root>'))), ''), '')::DECIMAL";

            return @"
, subject_score AS
(
	SELECT
		calc_condition.rank_school_year
		, calc_condition.rank_semester
		, student_list.student_id
		, student_list.student_name
		, student_list.rank_grade_year
		, student_list.rank_dept_name
		, student_list.rank_class_name
		, student_list.rank_tag1
		, student_list.rank_tag2
		, calc_condition.ref_exam_id::BIGINT AS exam_id
		, array_to_string(xpath('/root/Subject/@科目', xmlparse(content  concat('<root>', subj_score_ele , '</root>'))), '')::TEXT As subject
		, NULLIF(array_to_string(xpath('/root/Subject/@原始成績', xmlparse(content concat('<root>', subj_score_ele , '</root>'))), ''),'')::DECIMAL As subject_origin_score
		, GREATEST
		(
			NULLIF(array_to_string(xpath('/root/Subject/@原始成績', xmlparse(content concat('<root>', subj_score_ele , '</root>'))), ''), '')::DECIMAL"
                + schoolYearAdjScore + handAdjScore + makeUpScore + retakeScore
                + @"

		) AS subject_greatest_score
	FROM 
	(
		SELECT 
			sems_subj_score.*
			, unnest(xpath('/root/SemesterSubjectScoreInfo/Subject', xmlparse(content concat('<root>', score_info , '</root>') ))) AS subj_score_ele
		FROM 
			sems_subj_score
	) AS sems_subj_score_ext
	INNER JOIN student_list
		ON sems_subj_score_ext.ref_student_id = student_list.student_id
	INNER JOIN calc_condition
		ON sems_subj_score_ext.school_year = calc_condition.rank_school_year::INT
		AND sems_subj_score_ext.semester = calc_condition.rank_semester::INT
		AND student_list.rank_grade_year = calc_condition.rank_grade_year::INT
)
, subj_origin_rank AS
(
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_dept_name
		, rank_class_name
		, exam_id
		, subject::TEXT AS item_name
		, student_id
		, rank_tag1
		, rank_tag2
		, subject_origin_score AS origin_score
		, RANK() OVER(PARTITION BY rank_grade_year, subject ORDER BY subject_origin_score DESC) AS grade_origin_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name, subject ORDER BY subject_origin_score DESC) AS dept_origin_rank
		, RANK() OVER(PARTITION BY rank_class_name, subject ORDER BY subject_origin_score DESC) AS class_origin_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag1, subject ORDER BY subject_origin_score DESC) AS tag1_origin_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag2, subject ORDER BY subject_origin_score DESC) AS tag2_origin_rank
		, RANK() OVER(PARTITION BY rank_grade_year, subject ORDER BY subject_origin_score ASC) AS grade_origin_rank_reverse
		, RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name, subject ORDER BY subject_origin_score ASC) AS dept_origin_rank_reverse
		, RANK() OVER(PARTITION BY rank_class_name, subject ORDER BY subject_origin_score ASC) AS class_origin_rank_reverse
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag1, subject ORDER BY subject_origin_score ASC) AS tag1_origin_rank_reverse
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag2, subject ORDER BY subject_origin_score ASC) AS tag2_origin_rank_reverse
		, COUNT(student_id) OVER(PARTITION BY rank_grade_year, subject) AS grade_count
		, COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_dept_name, subject) AS dept_count
		, COUNT(student_id) OVER(PARTITION BY rank_class_name, subject) AS class_count
		, COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_tag1, subject) AS tag1_count
		, COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_tag2, subject) AS tag2_count
		, ROW_NUMBER() OVER(PARTITION BY rank_grade_year, subject ORDER BY subject_origin_score ASC) AS grade_origin_row_number
		, ROW_NUMBER() OVER(PARTITION BY rank_grade_year, rank_dept_name, subject ORDER BY subject_origin_score ASC) AS dept_origin_row_number
		, ROW_NUMBER() OVER(PARTITION BY rank_class_name, subject ORDER BY subject_origin_score ASC) AS class_origin_row_number
		, ROW_NUMBER() OVER(PARTITION BY rank_grade_year, rank_tag1, subject ORDER BY subject_origin_score ASC) AS tag1_origin_row_number
		, ROW_NUMBER() OVER(PARTITION BY rank_grade_year, rank_tag2, subject ORDER BY subject_origin_score ASC) AS tag2_origin_row_number
	FROM
		subject_score
	WHERE
		subject IS NOT NULL
		AND subject_origin_score IS NOT NULL
)
, subj_greatest_rank AS
(
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_dept_name
		, rank_class_name
		, exam_id
		, subject::TEXT AS item_name
		, student_id
		, rank_tag1
		, rank_tag2
		, subject_greatest_score AS greatest_score
		, RANK() OVER(PARTITION BY rank_grade_year, subject ORDER BY subject_greatest_score DESC) AS grade_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name, subject ORDER BY subject_greatest_score DESC) AS dept_rank
		, RANK() OVER(PARTITION BY rank_class_name, subject ORDER BY subject_greatest_score DESC) AS class_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag1, subject ORDER BY subject_greatest_score DESC) AS tag1_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag2, subject ORDER BY subject_greatest_score DESC) AS tag2_rank
		, RANK() OVER(PARTITION BY rank_grade_year, subject ORDER BY subject_greatest_score ASC) AS grade_rank_reverse
		, RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name, subject ORDER BY subject_greatest_score ASC) AS dept_rank_reverse
		, RANK() OVER(PARTITION BY rank_class_name, subject ORDER BY subject_greatest_score ASC) AS class_rank_reverse
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag1, subject ORDER BY subject_greatest_score ASC) AS tag1_rank_reverse
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag2, subject ORDER BY subject_greatest_score ASC) AS tag2_rank_reverse
		, COUNT(student_id) OVER(PARTITION BY rank_grade_year, subject) AS grade_count
		, COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_dept_name, subject) AS dept_count
		, COUNT(student_id) OVER(PARTITION BY rank_class_name, subject) AS class_count
		, COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_tag1, subject) AS tag1_count
		, COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_tag2, subject) AS tag2_count
		, ROW_NUMBER() OVER(PARTITION BY rank_grade_year, subject ORDER BY subject_greatest_score ASC) AS grade_row_number
		, ROW_NUMBER() OVER(PARTITION BY rank_grade_year, rank_dept_name, subject ORDER BY subject_greatest_score ASC) AS dept_row_number
		, ROW_NUMBER() OVER(PARTITION BY rank_class_name, subject ORDER BY subject_greatest_score ASC) AS class_row_number
		, ROW_NUMBER() OVER(PARTITION BY rank_grade_year, rank_tag1, subject ORDER BY subject_greatest_score ASC) AS tag1_row_number
		, ROW_NUMBER() OVER(PARTITION BY rank_grade_year, rank_tag2, subject ORDER BY subject_greatest_score ASC) AS tag2_row_number
	FROM
		subject_score
	WHERE
		subject IS NOT NULL
		AND subject_greatest_score IS NOT NULL
)
, subj_origin_rank_expand AS
(
	SELECT
		subj_origin_rank.*
		, FLOOR((grade_origin_rank::DECIMAL - 1)*100::DECIMAL / grade_count) + 1 AS graderank_origin_percentage
		, FLOOR((dept_origin_rank::DECIMAL - 1)*100::DECIMAL / dept_count) + 1 AS deptrank_origin_percentage
		, FLOOR((class_origin_rank::DECIMAL - 1)*100::DECIMAL / class_count) + 1 AS classrank_origin_percentage
		, FLOOR((tag1_origin_rank::DECIMAL - 1)*100::DECIMAL / tag1_count) + 1 AS tag1rank_origin_percentage
		, FLOOR((tag2_origin_rank::DECIMAL - 1)*100::DECIMAL / tag2_count) + 1 AS tag2rank_origin_percentage
		, FLOOR((grade_origin_rank_reverse::DECIMAL - 1)*100::DECIMAL / grade_count)  AS graderank_origin_pr
		, FLOOR((dept_origin_rank_reverse::DECIMAL - 1)*100::DECIMAL / dept_count)  AS deptrank_origin_pr
		, FLOOR((class_origin_rank_reverse::DECIMAL - 1)*100::DECIMAL / class_count)  AS classrank_origin_pr
		, FLOOR((tag1_origin_rank_reverse::DECIMAL - 1)*100::DECIMAL / tag1_count)  AS tag1rank_origin_pr
		, FLOOR((tag2_origin_rank_reverse::DECIMAL - 1)*100::DECIMAL / tag2_count)  AS tag2rank_origin_pr
	FROM
		subj_origin_rank
)
, subj_greatest_rank_expand AS
(
	SELECT
		subj_greatest_rank.*
		, FLOOR((grade_rank::DECIMAL - 1)*100::DECIMAL / grade_count) + 1 AS graderank_percentage
		, FLOOR((dept_rank::DECIMAL - 1)*100::DECIMAL / dept_count) + 1 AS deptrank_percentage
		, FLOOR((class_rank::DECIMAL - 1)*100::DECIMAL / class_count) + 1 AS classrank_percentage
		, FLOOR((tag1_rank::DECIMAL - 1)*100::DECIMAL / tag1_count) + 1 AS tag1rank_percentage
		, FLOOR((tag2_rank::DECIMAL - 1)*100::DECIMAL / tag2_count) + 1 AS tag2rank_percentage
		, FLOOR((grade_rank_reverse::DECIMAL - 1)*100::DECIMAL / grade_count) AS graderank_pr
		, FLOOR((dept_rank_reverse::DECIMAL - 1)*100::DECIMAL / dept_count) AS deptrank_pr
		, FLOOR((class_rank_reverse::DECIMAL - 1)*100::DECIMAL / class_count) AS classrank_pr
		, FLOOR((tag1_rank_reverse::DECIMAL - 1)*100::DECIMAL / tag1_count) AS tag1rank_pr
		, FLOOR((tag2_rank_reverse::DECIMAL - 1)*100::DECIMAL / tag2_count) AS tag2rank_pr
	FROM
		subj_greatest_rank
)
, entry_score_list AS
(
	SELECT
		calc_condition.rank_school_year
		, calc_condition.rank_semester
		, student_list.student_id
		, student_list.student_name
		, student_list.rank_grade_year
		, student_list.rank_dept_name
		, student_list.rank_class_name
		, student_list.rank_tag1
		, student_list.rank_tag2
		, calc_condition.ref_exam_id::BIGINT AS exam_id
		, array_to_string(xpath('/root/Entry/@分項', xmlparse(content  concat('<root>', entry_score_ele , '</root>'))),'')::TEXT As entry
		, NULLIF(array_to_string(xpath('/root/Entry/@成績', xmlparse(content   concat('<root>', entry_score_ele , '</root>'))), ''),'')::DECIMAL AS entry_score
	FROM
	(
		SELECT
			sems_entry_score.*
			, unnest(xpath('/root/SemesterEntryScore/Entry', xmlparse(content  concat('<root>', sems_entry_score.score_info , '</root>') ))) As entry_score_ele
		FROM
		sems_entry_score
	) AS sems_entry_score_ext
	INNER JOIN student_list
		ON sems_entry_score_ext.ref_student_id = student_list.student_id
	INNER JOIN calc_condition
		ON sems_entry_score_ext.school_year = calc_condition.rank_school_year::INT
		AND sems_entry_score_ext.semester = calc_condition.rank_semester::INT
		AND student_list.rank_grade_year = calc_condition.rank_grade_year::INT
)
, entry_rank_list AS
(
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_dept_name
		, rank_class_name
		, exam_id
		, entry::TEXT AS item_name
		, student_id
		, rank_tag1
		, rank_tag2
		, entry_score
		, RANK() OVER(PARTITION BY rank_grade_year, entry ORDER BY entry_score DESC) AS grade_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name, entry ORDER BY entry_score DESC) AS dept_rank
		, RANK() OVER(PARTITION BY rank_class_name, entry ORDER BY entry_score DESC) AS class_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag1, entry ORDER BY entry_score DESC) AS tag1_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag2, entry ORDER BY entry_score DESC) AS tag2_rank
		, RANK() OVER(PARTITION BY rank_grade_year, entry ORDER BY entry_score ASC) AS grade_rank_reverse
		, RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name, entry ORDER BY entry_score ASC) AS dept_rank_reverse
		, RANK() OVER(PARTITION BY rank_class_name, entry ORDER BY entry_score ASC) AS class_rank_reverse
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag1, entry ORDER BY entry_score ASC) AS tag1_rank_reverse
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag2, entry ORDER BY entry_score ASC) AS tag2_rank_reverse
		, COUNT(student_id) OVER(PARTITION BY rank_grade_year, entry) AS grade_count
		, COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_dept_name, entry) AS dept_count
		, COUNT(student_id) OVER(PARTITION BY rank_class_name, entry) AS class_count
		, COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_tag1, entry) AS tag1_count
		, COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_tag2, entry) AS tag2_count
		, ROW_NUMBER() OVER(PARTITION BY rank_grade_year, entry ORDER BY entry_score ASC) AS grade_row_number
		, ROW_NUMBER() OVER(PARTITION BY rank_grade_year, rank_dept_name, entry ORDER BY entry_score ASC) AS dept_row_number
		, ROW_NUMBER() OVER(PARTITION BY rank_class_name, entry ORDER BY entry_score ASC) AS class_row_number
		, ROW_NUMBER() OVER(PARTITION BY rank_grade_year, rank_tag1, entry ORDER BY entry_score ASC) AS tag1_row_number
		, ROW_NUMBER() OVER(PARTITION BY rank_grade_year, rank_tag2, entry ORDER BY entry_score ASC) AS tag2_row_number
	FROM
		entry_score_list
	WHERE
		entry IS NOT NULL
		AND entry_score IS NOT NULL
)
, entry_rank_expand AS
(
	SELECT
		entry_rank_list.*
		, FLOOR((grade_rank::DECIMAL - 1)*100::DECIMAL / grade_count) + 1 AS graderank_percentage
		, FLOOR((dept_rank::DECIMAL - 1)*100::DECIMAL / dept_count) + 1 AS deptrank_percentage
		, FLOOR((class_rank::DECIMAL - 1)*100::DECIMAL / class_count) + 1 AS classrank_percentage
		, FLOOR((tag1_rank::DECIMAL - 1)*100::DECIMAL / tag1_count) + 1 AS tag1rank_percentage
		, FLOOR((tag2_rank::DECIMAL - 1)*100::DECIMAL / tag2_count) + 1 AS tag2rank_percentage
        , FLOOR((grade_rank_reverse::DECIMAL - 1)*100::DECIMAL / grade_count)  AS graderank_pr
        , FLOOR((dept_rank_reverse::DECIMAL - 1)*100::DECIMAL / dept_count)  AS deptrank_pr
        , FLOOR((class_rank_reverse::DECIMAL - 1)*100::DECIMAL / class_count)  AS classrank_pr
        , FLOOR((tag1_rank_reverse::DECIMAL - 1)*100::DECIMAL / tag1_count)  AS tag1rank_pr
        , FLOOR((tag2_rank_reverse::DECIMAL - 1)*100::DECIMAL / tag2_count)  AS tag2rank_pr
	FROM
		entry_rank_list
)
, score_list AS
(
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, '學期/科目成績(原始)'::TEXT AS item_type
		, exam_id AS ref_exam_id
		, item_name
		, '年排名'::TEXT AS rank_type
		, '' || rank_grade_year || '年級'::TEXT AS rank_name
		, true AS is_alive
		, grade_count AS matrix_count
		, STDDEV_POP(origin_score) OVER(PARTITION BY rank_grade_year, item_name) AS std_dev_pop
	    , CASE WHEN (grade_count*88/100::DECIMAL % 1 <> 0) 
	        THEN AVG(origin_score::DECIMAL)FILTER(WHERE grade_count*88/100+1=grade_origin_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	        ELSE AVG(origin_score::DECIMAL)FILTER(WHERE grade_count*88/100=grade_origin_row_number OR grade_count*88/100+1 = grade_origin_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	      END pr_88
	    , CASE WHEN (grade_count*75/100::DECIMAL % 1 <> 0) 
	        THEN AVG(origin_score::DECIMAL)FILTER(WHERE grade_count*75/100+1=grade_origin_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	        ELSE AVG(origin_score::DECIMAL)FILTER(WHERE grade_count*75/100=grade_origin_row_number OR grade_count*75/100+1 = grade_origin_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	      END pr_75
	    , CASE WHEN (grade_count*50/100::DECIMAL % 1 <> 0) 
	        THEN AVG(origin_score::DECIMAL)FILTER(WHERE grade_count*50/100+1=grade_origin_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	        ELSE AVG(origin_score::DECIMAL)FILTER(WHERE grade_count*50/100=grade_origin_row_number OR grade_count*50/100+1 = grade_origin_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	      END pr_50
	    , CASE WHEN (grade_count*25/100::DECIMAL % 1 <> 0) 
	        THEN AVG(origin_score::DECIMAL)FILTER(WHERE grade_count*25/100+1=grade_origin_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	        ELSE AVG(origin_score::DECIMAL)FILTER(WHERE grade_count*25/100=grade_origin_row_number OR grade_count*25/100+1 = grade_origin_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	      END pr_25
	    , CASE WHEN (grade_count*12/100::DECIMAL % 1 <> 0) 
	        THEN AVG(origin_score::DECIMAL)FILTER(WHERE grade_count*12/100+1=grade_origin_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	        ELSE AVG(origin_score::DECIMAL)FILTER(WHERE grade_count*12/100=grade_origin_row_number OR grade_count*12/100+1 = grade_origin_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	      END pr_12
		, AVG(origin_score::DECIMAL) FILTER(WHERE grade_origin_rank *4 <= grade_count ) OVER(PARTITION BY rank_grade_year, item_name) AS avg_top_25
		, AVG(origin_score::DECIMAL) FILTER(WHERE grade_origin_rank *2 <= grade_count ) OVER(PARTITION BY rank_grade_year, item_name) AS avg_top_50
		, AVG(origin_score::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS avg
		, AVG(origin_score::DECIMAL) FILTER(WHERE (grade_count - grade_origin_rank + 1) *2 <= grade_count ) OVER(PARTITION BY rank_grade_year, item_name) AS avg_bottom_50
		, AVG(origin_score::DECIMAL) FILTER(WHERE (grade_count - grade_origin_rank + 1) *4 <= grade_count ) OVER(PARTITION BY rank_grade_year, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL<=origin_score) OVER(PARTITION BY rank_grade_year, item_name)AS level_gte100 
		, COUNT(*) FILTER(WHERE 90::DECIMAL<=origin_score AND origin_score <100::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL<=origin_score AND origin_score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL<=origin_score AND origin_score <80::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL<=origin_score AND origin_score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL<=origin_score AND origin_score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL<=origin_score AND origin_score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL<=origin_score AND origin_score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL<=origin_score AND origin_score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL<=origin_score AND origin_score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_10
		, COUNT(*) FILTER(WHERE origin_score<10::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_lt10 
		, student_id
		, origin_score AS score
		, grade_origin_rank AS rank
        , graderank_origin_pr AS pr
		, graderank_origin_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		subj_origin_rank_expand

	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, '學期/科目成績(原始)'::TEXT AS item_type
		, exam_id AS ref_exam_id
		, item_name
		, '科排名'::TEXT AS rank_type
		, rank_dept_name AS rank_name
		, true AS is_alive
		, dept_count AS matrix_count
		, STDDEV_POP(origin_score) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS std_dev_pop
	    , CASE WHEN (dept_count*88/100::DECIMAL % 1 <> 0) 
	        THEN AVG(origin_score::DECIMAL)FILTER(WHERE dept_count*88/100+1=dept_origin_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	        ELSE AVG(origin_score::DECIMAL)FILTER(WHERE dept_count*88/100=dept_origin_row_number OR dept_count*88/100+1 = dept_origin_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	      END pr_88
	    , CASE WHEN (dept_count*75/100::DECIMAL % 1 <> 0) 
	        THEN AVG(origin_score::DECIMAL)FILTER(WHERE dept_count*75/100+1=dept_origin_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	        ELSE AVG(origin_score::DECIMAL)FILTER(WHERE dept_count*75/100=dept_origin_row_number OR dept_count*75/100+1 = dept_origin_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	      END pr_75
	    , CASE WHEN (dept_count*50/100::DECIMAL % 1 <> 0) 
	        THEN AVG(origin_score::DECIMAL)FILTER(WHERE dept_count*50/100+1=dept_origin_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	        ELSE AVG(origin_score::DECIMAL)FILTER(WHERE dept_count*50/100=dept_origin_row_number OR dept_count*50/100+1 = dept_origin_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	      END pr_50
	    , CASE WHEN (dept_count*25/100::DECIMAL % 1 <> 0) 
	        THEN AVG(origin_score::DECIMAL)FILTER(WHERE dept_count*25/100+1=dept_origin_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	        ELSE AVG(origin_score::DECIMAL)FILTER(WHERE dept_count*25/100=dept_origin_row_number OR dept_count*25/100+1 = dept_origin_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	      END pr_25
	    , CASE WHEN (dept_count*12/100::DECIMAL % 1 <> 0) 
	        THEN AVG(origin_score::DECIMAL)FILTER(WHERE dept_count*12/100+1=dept_origin_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	        ELSE AVG(origin_score::DECIMAL)FILTER(WHERE dept_count*12/100=dept_origin_row_number OR dept_count*12/100+1 = dept_origin_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	      END pr_12
		, AVG(origin_score::DECIMAL) FILTER(WHERE dept_origin_rank * 4 <= dept_count ) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg_top_25
		, AVG(origin_score::DECIMAL) FILTER(WHERE dept_origin_rank * 2 <= dept_count ) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg_top_50
		, AVG(origin_score::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg
		, AVG(origin_score::DECIMAL) FILTER(WHERE ( dept_count - dept_origin_rank +1) * 2 <= dept_count ) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg_bottom_50
		, AVG(origin_score::DECIMAL) FILTER(WHERE ( dept_count - dept_origin_rank +1) * 4 <= dept_count ) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL<=origin_score) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_gte100 
		, COUNT(*) FILTER(WHERE 90::DECIMAL<=origin_score AND origin_score <100::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL<=origin_score AND origin_score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL<=origin_score AND origin_score <80::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL<=origin_score AND origin_score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL<=origin_score AND origin_score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL<=origin_score AND origin_score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL<=origin_score AND origin_score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL<=origin_score AND origin_score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL<=origin_score AND origin_score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_10
		, COUNT(*) FILTER(WHERE origin_score<10::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_lt10 
		, student_id
		, origin_score AS score
		, dept_origin_rank AS rank
        , deptrank_origin_pr AS pr
		, deptrank_origin_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		subj_origin_rank_expand

	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, '學期/科目成績(原始)'::TEXT AS item_type
		, exam_id AS ref_exam_id
		, item_name
		, '班排名'::TEXT AS rank_type
		, rank_class_name AS rank_name
		, true AS is_alive
		, class_count AS matrix_count
		, STDDEV_POP(origin_score) OVER(PARTITION BY rank_class_name, item_name) AS std_dev_pop
	    , CASE WHEN (class_count*88/100::DECIMAL % 1 <> 0) 
	        THEN AVG(origin_score::DECIMAL)FILTER(WHERE class_count*88/100+1=class_origin_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	        ELSE AVG(origin_score::DECIMAL)FILTER(WHERE class_count*88/100=class_origin_row_number OR class_count*88/100+1 = class_origin_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	      END pr_88
	    , CASE WHEN (class_count*75/100::DECIMAL % 1 <> 0) 
	        THEN AVG(origin_score::DECIMAL)FILTER(WHERE class_count*75/100+1=class_origin_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	        ELSE AVG(origin_score::DECIMAL)FILTER(WHERE class_count*75/100=class_origin_row_number OR class_count*75/100+1 = class_origin_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	      END pr_75
	    , CASE WHEN (class_count*50/100::DECIMAL % 1 <> 0) 
	        THEN AVG(origin_score::DECIMAL)FILTER(WHERE class_count*50/100+1=class_origin_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	        ELSE AVG(origin_score::DECIMAL)FILTER(WHERE class_count*50/100=class_origin_row_number OR class_count*50/100+1 = class_origin_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	      END pr_50
	    , CASE WHEN (class_count*25/100::DECIMAL % 1 <> 0) 
	        THEN AVG(origin_score::DECIMAL)FILTER(WHERE class_count*25/100+1=class_origin_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	        ELSE AVG(origin_score::DECIMAL)FILTER(WHERE class_count*25/100=class_origin_row_number OR class_count*25/100+1 = class_origin_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	      END pr_25
	    , CASE WHEN (class_count*12/100::DECIMAL % 1 <> 0) 
	        THEN AVG(origin_score::DECIMAL)FILTER(WHERE class_count*12/100+1=class_origin_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	        ELSE AVG(origin_score::DECIMAL)FILTER(WHERE class_count*12/100=class_origin_row_number OR class_count*12/100+1 = class_origin_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	      END pr_12
		, AVG(origin_score::DECIMAL) FILTER(WHERE class_origin_rank *4 <=  class_count ) OVER(PARTITION BY rank_class_name, item_name) AS avg_top_25
		, AVG(origin_score::DECIMAL) FILTER(WHERE class_origin_rank *2 <=  class_count ) OVER(PARTITION BY rank_class_name, item_name) AS avg_top_50
		, AVG(origin_score::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS avg
		, AVG(origin_score::DECIMAL) FILTER(WHERE (class_count -class_origin_rank +1) *2 <= class_count ) OVER(PARTITION BY rank_class_name, item_name) AS avg_bottom_50
		, AVG(origin_score::DECIMAL) FILTER(WHERE (class_count -class_origin_rank +1) *4 <= class_count ) OVER(PARTITION BY rank_class_name, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL<=origin_score) OVER(PARTITION BY rank_class_name, item_name)AS level_gte100 
		, COUNT(*) FILTER(WHERE 90::DECIMAL<=origin_score AND origin_score <100::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL<=origin_score AND origin_score <90::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL<=origin_score AND origin_score <80::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL<=origin_score AND origin_score <70::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL<=origin_score AND origin_score <60::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL<=origin_score AND origin_score <50::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL<=origin_score AND origin_score <40::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL<=origin_score AND origin_score <30::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL<=origin_score AND origin_score <20::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_10
		, COUNT(*) FILTER(WHERE origin_score<10::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_lt10 
		, student_id
		, origin_score AS score
		, class_origin_rank AS rank
        , classrank_origin_pr AS pr
		, classrank_origin_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		subj_origin_rank_expand

	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, '學期/科目成績(原始)'::TEXT AS item_type
		, exam_id AS ref_exam_id
		, item_name
		, '類別1排名'::TEXT AS rank_type
		, rank_tag1 AS rank_name
		, true AS is_alive
		, tag1_count AS matrix_count
		, STDDEV_POP(origin_score) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS std_dev_pop
	    , CASE WHEN (tag1_count*88/100::DECIMAL % 1 <> 0) 
	        THEN AVG(origin_score::DECIMAL)FILTER(WHERE tag1_count*88/100+1=tag1_origin_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	        ELSE AVG(origin_score::DECIMAL)FILTER(WHERE tag1_count*88/100=tag1_origin_row_number OR tag1_count*88/100+1 = tag1_origin_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	      END pr_88
	    , CASE WHEN (tag1_count*75/100::DECIMAL % 1 <> 0) 
	        THEN AVG(origin_score::DECIMAL)FILTER(WHERE tag1_count*75/100+1=tag1_origin_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	        ELSE AVG(origin_score::DECIMAL)FILTER(WHERE tag1_count*75/100=tag1_origin_row_number OR tag1_count*75/100+1 = tag1_origin_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	      END pr_75
	    , CASE WHEN (tag1_count*50/100::DECIMAL % 1 <> 0) 
	        THEN AVG(origin_score::DECIMAL)FILTER(WHERE tag1_count*50/100+1=tag1_origin_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	        ELSE AVG(origin_score::DECIMAL)FILTER(WHERE tag1_count*50/100=tag1_origin_row_number OR tag1_count*50/100+1 = tag1_origin_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	      END pr_50
	    , CASE WHEN (tag1_count*25/100::DECIMAL % 1 <> 0) 
	        THEN AVG(origin_score::DECIMAL)FILTER(WHERE tag1_count*25/100+1=tag1_origin_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	        ELSE AVG(origin_score::DECIMAL)FILTER(WHERE tag1_count*25/100=tag1_origin_row_number OR tag1_count*25/100+1 = tag1_origin_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	      END pr_25
	    , CASE WHEN (tag1_count*12/100::DECIMAL % 1 <> 0) 
	        THEN AVG(origin_score::DECIMAL)FILTER(WHERE tag1_count*12/100+1=tag1_origin_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	        ELSE AVG(origin_score::DECIMAL)FILTER(WHERE tag1_count*12/100=tag1_origin_row_number OR tag1_count*12/100+1 = tag1_origin_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	      END pr_12
		, AVG(origin_score::DECIMAL) FILTER(WHERE tag1_origin_rank *4 <= tag1_count) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_top_25
		, AVG(origin_score::DECIMAL) FILTER(WHERE tag1_origin_rank *2 <= tag1_count) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_top_50
		, AVG(origin_score::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg
		, AVG(origin_score::DECIMAL) FILTER(WHERE (tag1_count -tag1_origin_rank +1 ) *2 <= tag1_count ) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_bottom_50
		, AVG(origin_score::DECIMAL) FILTER(WHERE (tag1_count -tag1_origin_rank +1 ) *4 <= tag1_count ) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL<=origin_score) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)AS level_gte100 
		, COUNT(*) FILTER(WHERE 90::DECIMAL<=origin_score AND origin_score <100::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL<=origin_score AND origin_score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL<=origin_score AND origin_score <80::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL<=origin_score AND origin_score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL<=origin_score AND origin_score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL<=origin_score AND origin_score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL<=origin_score AND origin_score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL<=origin_score AND origin_score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL<=origin_score AND origin_score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_10
		, COUNT(*) FILTER(WHERE origin_score<10::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_lt10 
		, student_id
		, origin_score AS score
		, tag1_origin_rank AS rank
        , tag1rank_origin_pr AS pr
		, tag1rank_origin_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		subj_origin_rank_expand
	WHERE
		rank_tag1 IS NOT NULL
		AND rank_tag1 <> ''

	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, '學期/科目成績(原始)'::TEXT AS item_type
		, exam_id AS ref_exam_id
		, item_name
		, '類別2排名'::TEXT AS rank_type
		, rank_tag2 AS rank_name
		, true AS is_alive
		, tag2_count AS matrix_count
		, STDDEV_POP(origin_score) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS std_dev_pop
	    , CASE WHEN (tag2_count*88/100::DECIMAL % 1 <> 0) 
	        THEN AVG(origin_score::DECIMAL)FILTER(WHERE tag2_count*88/100+1=tag2_origin_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	        ELSE AVG(origin_score::DECIMAL)FILTER(WHERE tag2_count*88/100=tag2_origin_row_number OR tag2_count*88/100+1 = tag2_origin_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	      END pr_88
	    , CASE WHEN (tag2_count*75/100::DECIMAL % 1 <> 0) 
	        THEN AVG(origin_score::DECIMAL)FILTER(WHERE tag2_count*75/100+1=tag2_origin_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	        ELSE AVG(origin_score::DECIMAL)FILTER(WHERE tag2_count*75/100=tag2_origin_row_number OR tag2_count*75/100+1 = tag2_origin_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	      END pr_75
	    , CASE WHEN (tag2_count*50/100::DECIMAL % 1 <> 0) 
	        THEN AVG(origin_score::DECIMAL)FILTER(WHERE tag2_count*50/100+1=tag2_origin_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	        ELSE AVG(origin_score::DECIMAL)FILTER(WHERE tag2_count*50/100=tag2_origin_row_number OR tag2_count*50/100+1 = tag2_origin_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	      END pr_50
	    , CASE WHEN (tag2_count*25/100::DECIMAL % 1 <> 0) 
	        THEN AVG(origin_score::DECIMAL)FILTER(WHERE tag2_count*25/100+1=tag2_origin_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	        ELSE AVG(origin_score::DECIMAL)FILTER(WHERE tag2_count*25/100=tag2_origin_row_number OR tag2_count*25/100+1 = tag2_origin_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	      END pr_25
	    , CASE WHEN (tag2_count*12/100::DECIMAL % 1 <> 0) 
	        THEN AVG(origin_score::DECIMAL)FILTER(WHERE tag2_count*12/100+1=tag2_origin_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	        ELSE AVG(origin_score::DECIMAL)FILTER(WHERE tag2_count*12/100=tag2_origin_row_number OR tag2_count*12/100+1 = tag2_origin_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	      END pr_12
		, AVG(origin_score::DECIMAL) FILTER(WHERE tag2_origin_rank *4 <= tag2_count) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_top_25
		, AVG(origin_score::DECIMAL) FILTER(WHERE tag2_origin_rank *2 <= tag2_count) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_top_50
		, AVG(origin_score::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg
		, AVG(origin_score::DECIMAL) FILTER(WHERE ( tag2_count - tag2_origin_rank +1) *2 <= tag2_count ) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_bottom_50
		, AVG(origin_score::DECIMAL) FILTER(WHERE ( tag2_count - tag2_origin_rank +1) *4 <= tag2_count ) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL<=origin_score) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)AS level_gte100 
		, COUNT(*) FILTER(WHERE 90::DECIMAL<=origin_score AND origin_score <100::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL<=origin_score AND origin_score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL<=origin_score AND origin_score <80::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL<=origin_score AND origin_score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL<=origin_score AND origin_score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL<=origin_score AND origin_score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL<=origin_score AND origin_score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL<=origin_score AND origin_score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL<=origin_score AND origin_score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_10
		, COUNT(*) FILTER(WHERE origin_score<10::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_lt10 
		, student_id
		, origin_score AS score
		, tag2_origin_rank AS rank
        , tag2rank_origin_pr AS pr
		, tag2rank_origin_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		subj_origin_rank_expand
	WHERE
		rank_tag2 IS NOT NULL
		AND rank_tag2 <> ''

	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, '學期/科目成績'::TEXT AS item_type
		, exam_id AS ref_exam_id
		, item_name
		, '年排名'::TEXT AS rank_type
		, '' || rank_grade_year || '年級'::TEXT AS rank_name
		, true AS is_alive
		, grade_count AS matrix_count
		, STDDEV_POP(greatest_score) OVER(PARTITION BY rank_grade_year, item_name) AS std_dev_pop
	    , CASE WHEN (grade_count*88/100::DECIMAL % 1 <> 0) 
	        THEN AVG(greatest_score::DECIMAL)FILTER(WHERE grade_count*88/100+1=grade_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	        ELSE AVG(greatest_score::DECIMAL)FILTER(WHERE grade_count*88/100=grade_row_number OR grade_count*88/100+1 = grade_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	      END pr_88
	    , CASE WHEN (grade_count*75/100::DECIMAL % 1 <> 0) 
	        THEN AVG(greatest_score::DECIMAL)FILTER(WHERE grade_count*75/100+1=grade_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	        ELSE AVG(greatest_score::DECIMAL)FILTER(WHERE grade_count*75/100=grade_row_number OR grade_count*75/100+1 = grade_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	      END pr_75
	    , CASE WHEN (grade_count*50/100::DECIMAL % 1 <> 0) 
	        THEN AVG(greatest_score::DECIMAL)FILTER(WHERE grade_count*50/100+1=grade_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	        ELSE AVG(greatest_score::DECIMAL)FILTER(WHERE grade_count*50/100=grade_row_number OR grade_count*50/100+1 = grade_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	      END pr_50
	    , CASE WHEN (grade_count*25/100::DECIMAL % 1 <> 0) 
	        THEN AVG(greatest_score::DECIMAL)FILTER(WHERE grade_count*25/100+1=grade_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	        ELSE AVG(greatest_score::DECIMAL)FILTER(WHERE grade_count*25/100=grade_row_number OR grade_count*25/100+1 = grade_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	      END pr_25
	    , CASE WHEN (grade_count*12/100::DECIMAL % 1 <> 0) 
	        THEN AVG(greatest_score::DECIMAL)FILTER(WHERE grade_count*12/100+1=grade_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	        ELSE AVG(greatest_score::DECIMAL)FILTER(WHERE grade_count*12/100=grade_row_number OR grade_count*12/100+1 = grade_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	      END pr_12
		, AVG(greatest_score::DECIMAL) FILTER(WHERE grade_rank * 4 <= grade_count ) OVER(PARTITION BY rank_grade_year, item_name) AS avg_top_25
		, AVG(greatest_score::DECIMAL) FILTER(WHERE grade_rank * 2 <= grade_count ) OVER(PARTITION BY rank_grade_year, item_name) AS avg_top_50
		, AVG(greatest_score::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS avg
		, AVG(greatest_score::DECIMAL) FILTER(WHERE (grade_count - grade_rank +1) * 2 <= grade_count) OVER(PARTITION BY rank_grade_year, item_name) AS avg_bottom_50
		, AVG(greatest_score::DECIMAL) FILTER(WHERE (grade_count - grade_rank +1) * 4 <= grade_count) OVER(PARTITION BY rank_grade_year, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL<=greatest_score) OVER(PARTITION BY rank_grade_year, item_name)AS level_gte100 
		, COUNT(*) FILTER(WHERE 90::DECIMAL<=greatest_score AND greatest_score <100::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL<=greatest_score AND greatest_score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL<=greatest_score AND greatest_score <80::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL<=greatest_score AND greatest_score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL<=greatest_score AND greatest_score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL<=greatest_score AND greatest_score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL<=greatest_score AND greatest_score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL<=greatest_score AND greatest_score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL<=greatest_score AND greatest_score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_10
		, COUNT(*) FILTER(WHERE greatest_score<10::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_lt10 
		, student_id
		, greatest_score AS score
		, grade_rank AS rank
        , graderank_pr AS pr
		, graderank_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		subj_greatest_rank_expand
	
	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, '學期/科目成績'::TEXT AS item_type
		, exam_id AS ref_exam_id
		, item_name
		, '科排名'::TEXT AS rank_type
		, rank_dept_name AS rank_name
		, true AS is_alive
		, dept_count AS matrix_count
		, STDDEV_POP(greatest_score) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS std_dev_pop
	    , CASE WHEN (dept_count*88/100::DECIMAL % 1 <> 0) 
	        THEN AVG(greatest_score::DECIMAL)FILTER(WHERE dept_count*88/100+1=dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	        ELSE AVG(greatest_score::DECIMAL)FILTER(WHERE dept_count*88/100=dept_row_number OR dept_count*88/100+1 = dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	      END pr_88
	    , CASE WHEN (dept_count*75/100::DECIMAL % 1 <> 0) 
	        THEN AVG(greatest_score::DECIMAL)FILTER(WHERE dept_count*75/100+1=dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	        ELSE AVG(greatest_score::DECIMAL)FILTER(WHERE dept_count*75/100=dept_row_number OR dept_count*75/100+1 = dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	      END pr_75
	    , CASE WHEN (dept_count*50/100::DECIMAL % 1 <> 0) 
	        THEN AVG(greatest_score::DECIMAL)FILTER(WHERE dept_count*50/100+1=dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	        ELSE AVG(greatest_score::DECIMAL)FILTER(WHERE dept_count*50/100=dept_row_number OR dept_count*50/100+1 = dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	      END pr_50
	    , CASE WHEN (dept_count*25/100::DECIMAL % 1 <> 0) 
	        THEN AVG(greatest_score::DECIMAL)FILTER(WHERE dept_count*25/100+1=dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	        ELSE AVG(greatest_score::DECIMAL)FILTER(WHERE dept_count*25/100=dept_row_number OR dept_count*25/100+1 = dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	      END pr_25
	    , CASE WHEN (dept_count*12/100::DECIMAL % 1 <> 0) 
	        THEN AVG(greatest_score::DECIMAL)FILTER(WHERE dept_count*12/100+1=dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	        ELSE AVG(greatest_score::DECIMAL)FILTER(WHERE dept_count*12/100=dept_row_number OR dept_count*12/100+1 = dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	      END pr_12
		, AVG(greatest_score::DECIMAL) FILTER(WHERE dept_rank * 4 <= dept_count ) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg_top_25
		, AVG(greatest_score::DECIMAL) FILTER(WHERE dept_rank * 2 <= dept_count ) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg_top_50
		, AVG(greatest_score::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg
		, AVG(greatest_score::DECIMAL) FILTER(WHERE (dept_count - dept_rank + 1) * 2 <= dept_count ) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg_bottom_50
		, AVG(greatest_score::DECIMAL) FILTER(WHERE (dept_count - dept_rank + 1) * 4 <= dept_count ) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL<=greatest_score) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)AS level_gte100 
		, COUNT(*) FILTER(WHERE 90::DECIMAL<=greatest_score AND greatest_score <100::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL<=greatest_score AND greatest_score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL<=greatest_score AND greatest_score <80::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL<=greatest_score AND greatest_score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL<=greatest_score AND greatest_score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL<=greatest_score AND greatest_score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL<=greatest_score AND greatest_score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL<=greatest_score AND greatest_score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL<=greatest_score AND greatest_score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_10
		, COUNT(*) FILTER(WHERE greatest_score<10::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_lt10 
		, student_id
		, greatest_score AS score
		, dept_rank AS rank
        , deptrank_pr AS pr
		, deptrank_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		subj_greatest_rank_expand

	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, '學期/科目成績'::TEXT AS item_type
		, exam_id AS ref_exam_id
		, item_name
		, '班排名'::TEXT AS rank_type
		, rank_class_name AS rank_name
		, true AS is_alive
		, class_count AS matrix_count
		, STDDEV_POP(greatest_score) OVER(PARTITION BY rank_class_name, item_name) AS std_dev_pop
	    , CASE WHEN (class_count*88/100::DECIMAL % 1 <> 0) 
	        THEN AVG(greatest_score::DECIMAL)FILTER(WHERE class_count*88/100+1=class_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	        ELSE AVG(greatest_score::DECIMAL)FILTER(WHERE class_count*88/100=class_row_number OR class_count*88/100+1 = class_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	      END pr_88
	    , CASE WHEN (class_count*75/100::DECIMAL % 1 <> 0) 
	        THEN AVG(greatest_score::DECIMAL)FILTER(WHERE class_count*75/100+1=class_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	        ELSE AVG(greatest_score::DECIMAL)FILTER(WHERE class_count*75/100=class_row_number OR class_count*75/100+1 = class_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	      END pr_75
	    , CASE WHEN (class_count*50/100::DECIMAL % 1 <> 0) 
	        THEN AVG(greatest_score::DECIMAL)FILTER(WHERE class_count*50/100+1=class_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	        ELSE AVG(greatest_score::DECIMAL)FILTER(WHERE class_count*50/100=class_row_number OR class_count*50/100+1 = class_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	      END pr_50
	    , CASE WHEN (class_count*25/100::DECIMAL % 1 <> 0) 
	        THEN AVG(greatest_score::DECIMAL)FILTER(WHERE class_count*25/100+1=class_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	        ELSE AVG(greatest_score::DECIMAL)FILTER(WHERE class_count*25/100=class_row_number OR class_count*25/100+1 = class_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	      END pr_25
	    , CASE WHEN (class_count*12/100::DECIMAL % 1 <> 0) 
	        THEN AVG(greatest_score::DECIMAL)FILTER(WHERE class_count*12/100+1=class_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	        ELSE AVG(greatest_score::DECIMAL)FILTER(WHERE class_count*12/100=class_row_number OR class_count*12/100+1 = class_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	      END pr_12
		, AVG(greatest_score::DECIMAL) FILTER(WHERE class_rank *4 <= class_count ) OVER(PARTITION BY rank_class_name, item_name) AS avg_top_25
		, AVG(greatest_score::DECIMAL) FILTER(WHERE class_rank *2 <= class_count ) OVER(PARTITION BY rank_class_name, item_name) AS avg_top_50
		, AVG(greatest_score::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS avg
		, AVG(greatest_score::DECIMAL) FILTER(WHERE (class_count - class_rank +1) * 2  <= class_count ) OVER(PARTITION BY rank_class_name, item_name) AS avg_bottom_50
		, AVG(greatest_score::DECIMAL) FILTER(WHERE (class_count - class_rank +1) * 4  <= class_count ) OVER(PARTITION BY rank_class_name, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL<=greatest_score) OVER(PARTITION BY rank_class_name, item_name)AS level_gte100 
		, COUNT(*) FILTER(WHERE 90::DECIMAL<=greatest_score AND greatest_score <100::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL<=greatest_score AND greatest_score <90::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL<=greatest_score AND greatest_score <80::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL<=greatest_score AND greatest_score <70::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL<=greatest_score AND greatest_score <60::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL<=greatest_score AND greatest_score <50::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL<=greatest_score AND greatest_score <40::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL<=greatest_score AND greatest_score <30::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL<=greatest_score AND greatest_score <20::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_10
		, COUNT(*) FILTER(WHERE greatest_score<10::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_lt10 
		, student_id
		, greatest_score AS score
		, class_rank AS rank
        , classrank_pr AS pr
		, classrank_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		subj_greatest_rank_expand

	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, '學期/科目成績'::TEXT AS item_type
		, exam_id AS ref_exam_id
		, item_name
		, '類別1排名'::TEXT AS rank_type
		, rank_tag1 AS rank_name
		, true AS is_alive
		, tag1_count AS matrix_count
		, STDDEV_POP(greatest_score) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS std_dev_pop
	    , CASE WHEN (tag1_count*88/100::DECIMAL % 1 <> 0) 
	        THEN AVG(greatest_score::DECIMAL)FILTER(WHERE tag1_count*88/100+1=tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	        ELSE AVG(greatest_score::DECIMAL)FILTER(WHERE tag1_count*88/100=tag1_row_number OR tag1_count*88/100+1 = tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	      END pr_88
	    , CASE WHEN (tag1_count*75/100::DECIMAL % 1 <> 0) 
	        THEN AVG(greatest_score::DECIMAL)FILTER(WHERE tag1_count*75/100+1=tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	        ELSE AVG(greatest_score::DECIMAL)FILTER(WHERE tag1_count*75/100=tag1_row_number OR tag1_count*75/100+1 = tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	      END pr_75
	    , CASE WHEN (tag1_count*50/100::DECIMAL % 1 <> 0) 
	        THEN AVG(greatest_score::DECIMAL)FILTER(WHERE tag1_count*50/100+1=tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	        ELSE AVG(greatest_score::DECIMAL)FILTER(WHERE tag1_count*50/100=tag1_row_number OR tag1_count*50/100+1 = tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	      END pr_50
	    , CASE WHEN (tag1_count*25/100::DECIMAL % 1 <> 0) 
	        THEN AVG(greatest_score::DECIMAL)FILTER(WHERE tag1_count*25/100+1=tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	        ELSE AVG(greatest_score::DECIMAL)FILTER(WHERE tag1_count*25/100=tag1_row_number OR tag1_count*25/100+1 = tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	      END pr_25
	    , CASE WHEN (tag1_count*12/100::DECIMAL % 1 <> 0) 
	        THEN AVG(greatest_score::DECIMAL)FILTER(WHERE tag1_count*12/100+1=tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	        ELSE AVG(greatest_score::DECIMAL)FILTER(WHERE tag1_count*12/100=tag1_row_number OR tag1_count*12/100+1 = tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	      END pr_12
		, AVG(greatest_score::DECIMAL) FILTER(WHERE tag1_rank * 4 <= tag1_count ) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_top_25
		, AVG(greatest_score::DECIMAL) FILTER(WHERE tag1_rank * 2 <= tag1_count ) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_top_50
		, AVG(greatest_score::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg
		, AVG(greatest_score::DECIMAL) FILTER(WHERE ( tag1_count - tag1_rank +1 ) *2 <= tag1_count ) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_bottom_50
		, AVG(greatest_score::DECIMAL) FILTER(WHERE ( tag1_count - tag1_rank +1 ) *4 <= tag1_count ) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL<=greatest_score) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)AS level_gte100 
		, COUNT(*) FILTER(WHERE 90::DECIMAL<=greatest_score AND greatest_score <100::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL<=greatest_score AND greatest_score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL<=greatest_score AND greatest_score <80::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL<=greatest_score AND greatest_score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL<=greatest_score AND greatest_score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL<=greatest_score AND greatest_score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL<=greatest_score AND greatest_score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL<=greatest_score AND greatest_score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL<=greatest_score AND greatest_score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_10
		, COUNT(*) FILTER(WHERE greatest_score<10::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_lt10 
		, student_id
		, greatest_score AS score
		, tag1_rank AS rank
        , tag1rank_pr AS pr
		, tag1rank_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		subj_greatest_rank_expand
	WHERE
		rank_tag1 IS NOT NULL
		AND rank_tag1 <> ''

	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, '學期/科目成績'::TEXT AS item_type
		, exam_id AS ref_exam_id
		, item_name
		, '類別2排名'::TEXT AS rank_type
		, rank_tag2 AS rank_name
		, true AS is_alive
		, tag2_count AS matrix_count
		, STDDEV_POP(greatest_score) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS std_dev_pop
	    , CASE WHEN (tag2_count*88/100::DECIMAL % 1 <> 0) 
	        THEN AVG(greatest_score::DECIMAL)FILTER(WHERE tag2_count*88/100+1=tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	        ELSE AVG(greatest_score::DECIMAL)FILTER(WHERE tag2_count*88/100=tag2_row_number OR tag2_count*88/100+1 = tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	      END pr_88
	    , CASE WHEN (tag2_count*75/100::DECIMAL % 1 <> 0) 
	        THEN AVG(greatest_score::DECIMAL)FILTER(WHERE tag2_count*75/100+1=tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	        ELSE AVG(greatest_score::DECIMAL)FILTER(WHERE tag2_count*75/100=tag2_row_number OR tag2_count*75/100+1 = tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	      END pr_75
	    , CASE WHEN (tag2_count*50/100::DECIMAL % 1 <> 0) 
	        THEN AVG(greatest_score::DECIMAL)FILTER(WHERE tag2_count*50/100+1=tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	        ELSE AVG(greatest_score::DECIMAL)FILTER(WHERE tag2_count*50/100=tag2_row_number OR tag2_count*50/100+1 = tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	      END pr_50
	    , CASE WHEN (tag2_count*25/100::DECIMAL % 1 <> 0) 
	        THEN AVG(greatest_score::DECIMAL)FILTER(WHERE tag2_count*25/100+1=tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	        ELSE AVG(greatest_score::DECIMAL)FILTER(WHERE tag2_count*25/100=tag2_row_number OR tag2_count*25/100+1 = tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	      END pr_25
	    , CASE WHEN (tag2_count*12/100::DECIMAL % 1 <> 0) 
	        THEN AVG(greatest_score::DECIMAL)FILTER(WHERE tag2_count*12/100+1=tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	        ELSE AVG(greatest_score::DECIMAL)FILTER(WHERE tag2_count*12/100=tag2_row_number OR tag2_count*12/100+1 = tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	      END pr_12
		, AVG(greatest_score::DECIMAL) FILTER(WHERE tag2_rank *4 <=  tag2_count) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_top_25
		, AVG(greatest_score::DECIMAL) FILTER(WHERE tag2_rank *2 <=  tag2_count) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_top_50
		, AVG(greatest_score::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg
		, AVG(greatest_score::DECIMAL) FILTER(WHERE (tag2_count - tag2_rank+1 ) *2 <= tag2_count ) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_bottom_50
		, AVG(greatest_score::DECIMAL) FILTER(WHERE (tag2_count - tag2_rank+1 ) *4 <= tag2_count ) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL<=greatest_score) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)AS level_gte100 
		, COUNT(*) FILTER(WHERE 90::DECIMAL<=greatest_score AND greatest_score <100::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL<=greatest_score AND greatest_score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL<=greatest_score AND greatest_score <80::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL<=greatest_score AND greatest_score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL<=greatest_score AND greatest_score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL<=greatest_score AND greatest_score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL<=greatest_score AND greatest_score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL<=greatest_score AND greatest_score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL<=greatest_score AND greatest_score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_10
		, COUNT(*) FILTER(WHERE greatest_score<10::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_lt10 
		, student_id
		, greatest_score AS score
		, tag2_rank AS rank
        , tag2rank_pr AS pr
		, tag2rank_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		subj_greatest_rank_expand
	WHERE
		rank_tag2 IS NOT NULL
		AND rank_tag2 <> ''

	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, '學期/分項成績(原始)'::TEXT AS item_type
		, exam_id AS ref_exam_id
		, REPLACE(item_name, '(原始)', '') AS item_name
		, '年排名'::TEXT AS rank_type
		, '' || rank_grade_year || '年級'::TEXT AS rank_name
		, true AS is_alive
		, grade_count AS matrix_count
		, STDDEV_POP(entry_score) OVER(PARTITION BY rank_grade_year, item_name) AS std_dev_pop
	    , CASE WHEN (grade_count*88/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE grade_count*88/100+1=grade_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE grade_count*88/100=grade_row_number OR grade_count*88/100+1 = grade_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	      END pr_88
	    , CASE WHEN (grade_count*75/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE grade_count*75/100+1=grade_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE grade_count*75/100=grade_row_number OR grade_count*75/100+1 = grade_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	      END pr_75
	    , CASE WHEN (grade_count*50/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE grade_count*50/100+1=grade_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE grade_count*50/100=grade_row_number OR grade_count*50/100+1 = grade_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	      END pr_50
	    , CASE WHEN (grade_count*25/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE grade_count*25/100+1=grade_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE grade_count*25/100=grade_row_number OR grade_count*25/100+1 = grade_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	      END pr_25
	    , CASE WHEN (grade_count*12/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE grade_count*12/100+1=grade_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE grade_count*12/100=grade_row_number OR grade_count*12/100+1 = grade_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	      END pr_12
		, AVG(entry_score::DECIMAL) FILTER(WHERE grade_rank *4  <= grade_count ) OVER(PARTITION BY rank_grade_year, item_name) AS avg_top_25
		, AVG(entry_score::DECIMAL) FILTER(WHERE grade_rank *2  <= grade_count ) OVER(PARTITION BY rank_grade_year, item_name) AS avg_top_50
		, AVG(entry_score::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS avg
		, AVG(entry_score::DECIMAL) FILTER(WHERE (grade_count -grade_rank +1 )*2 <= grade_count ) OVER(PARTITION BY rank_grade_year, item_name) AS avg_bottom_50
		, AVG(entry_score::DECIMAL) FILTER(WHERE (grade_count -grade_rank +1 )*4 <= grade_count ) OVER(PARTITION BY rank_grade_year, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL<=entry_score) OVER(PARTITION BY rank_grade_year, item_name)AS level_gte100 
		, COUNT(*) FILTER(WHERE 90::DECIMAL<=entry_score AND entry_score <100::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL<=entry_score AND entry_score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL<=entry_score AND entry_score <80::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL<=entry_score AND entry_score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL<=entry_score AND entry_score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL<=entry_score AND entry_score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL<=entry_score AND entry_score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL<=entry_score AND entry_score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL<=entry_score AND entry_score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_10
		, COUNT(*) FILTER(WHERE entry_score<10::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_lt10 
		, student_id
		, entry_score AS score
		, grade_rank AS rank
        , graderank_pr AS pr
		, graderank_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		entry_rank_expand
	WHERE
		item_name LIKE '%(原始)%'
	
	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, '學期/分項成績(原始)'::TEXT AS item_type
		, exam_id AS ref_exam_id
		, REPLACE(item_name, '(原始)', '') AS item_name
		, '科排名'::TEXT AS rank_type
		, rank_dept_name AS rank_name
		, true AS is_alive
		, dept_count AS matrix_count
		, STDDEV_POP(entry_score) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS std_dev_pop
	    , CASE WHEN (dept_count*88/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE dept_count*88/100+1=dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE dept_count*88/100=dept_row_number OR dept_count*88/100+1 = dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	      END pr_88
	    , CASE WHEN (dept_count*75/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE dept_count*75/100+1=dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE dept_count*75/100=dept_row_number OR dept_count*75/100+1 = dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	      END pr_75
	    , CASE WHEN (dept_count*50/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE dept_count*50/100+1=dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE dept_count*50/100=dept_row_number OR dept_count*50/100+1 = dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	      END pr_50
	    , CASE WHEN (dept_count*25/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE dept_count*25/100+1=dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE dept_count*25/100=dept_row_number OR dept_count*25/100+1 = dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	      END pr_25
	    , CASE WHEN (dept_count*12/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE dept_count*12/100+1=dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE dept_count*12/100=dept_row_number OR dept_count*12/100+1 = dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	      END pr_12
		, AVG(entry_score::DECIMAL) FILTER(WHERE dept_rank * 4 <= dept_count ) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg_top_25
		, AVG(entry_score::DECIMAL) FILTER(WHERE dept_rank  *2 <= dept_count ) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg_top_50
		, AVG(entry_score::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg
		, AVG(entry_score::DECIMAL) FILTER(WHERE (dept_count -dept_rank +1 )*2 <= dept_count ) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg_bottom_50
		, AVG(entry_score::DECIMAL) FILTER(WHERE (dept_count- dept_rank +1 )*4 <= dept_count ) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL<=entry_score) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_gte100 
		, COUNT(*) FILTER(WHERE 90::DECIMAL<=entry_score AND entry_score <100::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL<=entry_score AND entry_score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL<=entry_score AND entry_score <80::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL<=entry_score AND entry_score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL<=entry_score AND entry_score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL<=entry_score AND entry_score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL<=entry_score AND entry_score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL<=entry_score AND entry_score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL<=entry_score AND entry_score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_10
		, COUNT(*) FILTER(WHERE entry_score<10::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_lt10 
		, student_id
		, entry_score AS score
		, dept_rank AS rank
        , deptrank_pr AS pr
		, deptrank_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		entry_rank_expand
	WHERE
		item_name LIKE '%(原始)%'
	
	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, '學期/分項成績(原始)'::TEXT AS item_type
		, exam_id AS ref_exam_id
		, REPLACE(item_name, '(原始)', '') AS item_name
		, '班排名'::TEXT AS rank_type
		, rank_class_name AS rank_name
		, true AS is_alive
		, class_count AS matrix_count
		, STDDEV_POP(entry_score) OVER(PARTITION BY rank_class_name, item_name) AS std_dev_pop
	    , CASE WHEN (class_count*88/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE class_count*88/100+1=class_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE class_count*88/100=class_row_number OR class_count*88/100+1 = class_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	      END pr_88
	    , CASE WHEN (class_count*75/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE class_count*75/100+1=class_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE class_count*75/100=class_row_number OR class_count*75/100+1 = class_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	      END pr_75
	    , CASE WHEN (class_count*50/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE class_count*50/100+1=class_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE class_count*50/100=class_row_number OR class_count*50/100+1 = class_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	      END pr_50
	    , CASE WHEN (class_count*25/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE class_count*25/100+1=class_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE class_count*25/100=class_row_number OR class_count*25/100+1 = class_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	      END pr_25
	    , CASE WHEN (class_count*12/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE class_count*12/100+1=class_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE class_count*12/100=class_row_number OR class_count*12/100+1 = class_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	      END pr_12
		, AVG(entry_score::DECIMAL) FILTER(WHERE  class_rank *4 <= class_count ) OVER(PARTITION BY rank_class_name, item_name) AS avg_top_25
		, AVG(entry_score::DECIMAL) FILTER(WHERE  class_rank *2 <= class_count ) OVER(PARTITION BY rank_class_name, item_name) AS avg_top_50
		, AVG(entry_score::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS avg
		, AVG(entry_score::DECIMAL) FILTER(WHERE (class_count -class_rank +1 ) *2 <= class_count ) OVER(PARTITION BY rank_class_name, item_name) AS avg_bottom_50
		, AVG(entry_score::DECIMAL) FILTER(WHERE (class_count -class_rank +1 ) *4 <= class_count ) OVER(PARTITION BY rank_class_name, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL<=entry_score) OVER(PARTITION BY rank_class_name, item_name)AS level_gte100 
		, COUNT(*) FILTER(WHERE 90::DECIMAL<=entry_score AND entry_score <100::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL<=entry_score AND entry_score <90::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL<=entry_score AND entry_score <80::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL<=entry_score AND entry_score <70::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL<=entry_score AND entry_score <60::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL<=entry_score AND entry_score <50::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL<=entry_score AND entry_score <40::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL<=entry_score AND entry_score <30::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL<=entry_score AND entry_score <20::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_10
		, COUNT(*) FILTER(WHERE entry_score<10::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_lt10 
		, student_id
		, entry_score AS score
		, class_rank AS rank
        , classrank_pr AS pr
		, classrank_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		entry_rank_expand
	WHERE
		item_name LIKE '%(原始)%'
	
	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, '學期/分項成績(原始)'::TEXT AS item_type
		, exam_id AS ref_exam_id
		, REPLACE(item_name, '(原始)', '') AS item_name
		, '類別1排名'::TEXT AS rank_type
		, rank_tag1 AS rank_name
		, true AS is_alive
		, tag1_count AS matrix_count
		, STDDEV_POP(entry_score) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS std_dev_pop
	    , CASE WHEN (tag1_count*88/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE tag1_count*88/100+1=tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE tag1_count*88/100=tag1_row_number OR tag1_count*88/100+1 = tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	      END pr_88
	    , CASE WHEN (tag1_count*75/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE tag1_count*75/100+1=tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE tag1_count*75/100=tag1_row_number OR tag1_count*75/100+1 = tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	      END pr_75
	    , CASE WHEN (tag1_count*50/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE tag1_count*50/100+1=tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE tag1_count*50/100=tag1_row_number OR tag1_count*50/100+1 = tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	      END pr_50
	    , CASE WHEN (tag1_count*25/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE tag1_count*25/100+1=tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE tag1_count*25/100=tag1_row_number OR tag1_count*25/100+1 = tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	      END pr_25
	    , CASE WHEN (tag1_count*12/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE tag1_count*12/100+1=tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE tag1_count*12/100=tag1_row_number OR tag1_count*12/100+1 = tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	      END pr_12
		, AVG(entry_score::DECIMAL) FILTER(WHERE tag1_rank *4 <= tag1_count ) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_top_25
		, AVG(entry_score::DECIMAL) FILTER(WHERE tag1_rank *2 <= tag1_count ) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_top_50
		, AVG(entry_score::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg
		, AVG(entry_score::DECIMAL) FILTER(WHERE (tag1_count - tag1_rank + 1 ) *2 <= tag1_count ) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_bottom_50
		, AVG(entry_score::DECIMAL) FILTER(WHERE (tag1_count - tag1_rank + 1 ) *4 <= tag1_count ) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL<=entry_score) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_gte100 
		, COUNT(*) FILTER(WHERE 90::DECIMAL<=entry_score AND entry_score <100::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL<=entry_score AND entry_score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL<=entry_score AND entry_score <80::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL<=entry_score AND entry_score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL<=entry_score AND entry_score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL<=entry_score AND entry_score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL<=entry_score AND entry_score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL<=entry_score AND entry_score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL<=entry_score AND entry_score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_10
		, COUNT(*) FILTER(WHERE entry_score<10::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_lt10 
		, student_id
		, entry_score AS score
		, tag1_rank AS rank
        , tag1rank_pr AS pr
		, tag1rank_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		entry_rank_expand
	WHERE
		item_name LIKE '%(原始)%'
		AND rank_tag1 IS NOT NULL
		AND rank_tag1 <> ''
	
	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, '學期/分項成績(原始)'::TEXT AS item_type
		, exam_id AS ref_exam_id
		, REPLACE(item_name, '(原始)', '') AS item_name
		, '類別2排名'::TEXT AS rank_type
		, rank_tag2 AS rank_name
		, true AS is_alive
		, tag2_count AS matrix_count
		, STDDEV_POP(entry_score) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS std_dev_pop
	    , CASE WHEN (tag2_count*88/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE tag2_count*88/100+1=tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE tag2_count*88/100=tag2_row_number OR tag2_count*88/100+1 = tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	      END pr_88
	    , CASE WHEN (tag2_count*75/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE tag2_count*75/100+1=tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE tag2_count*75/100=tag2_row_number OR tag2_count*75/100+1 = tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	      END pr_75
	    , CASE WHEN (tag2_count*50/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE tag2_count*50/100+1=tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE tag2_count*50/100=tag2_row_number OR tag2_count*50/100+1 = tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	      END pr_50
	    , CASE WHEN (tag2_count*25/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE tag2_count*25/100+1=tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE tag2_count*25/100=tag2_row_number OR tag2_count*25/100+1 = tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	      END pr_25
	    , CASE WHEN (tag2_count*12/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE tag2_count*12/100+1=tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE tag2_count*12/100=tag2_row_number OR tag2_count*12/100+1 = tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	      END pr_12
		, AVG(entry_score::DECIMAL) FILTER(WHERE tag2_rank  *4 <=  tag2_count) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_top_25
		, AVG(entry_score::DECIMAL) FILTER(WHERE tag2_rank  *2 <=  tag2_count) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_top_50
		, AVG(entry_score::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg
		, AVG(entry_score::DECIMAL) FILTER(WHERE (tag2_count -tag2_rank +1 ) *2 <= tag2_count ) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_bottom_50
		, AVG(entry_score::DECIMAL) FILTER(WHERE (tag2_count -tag2_rank +1 ) *4 <= tag2_count ) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL<=entry_score) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_gte100 
		, COUNT(*) FILTER(WHERE 90::DECIMAL<=entry_score AND entry_score <100::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL<=entry_score AND entry_score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL<=entry_score AND entry_score <80::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL<=entry_score AND entry_score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL<=entry_score AND entry_score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL<=entry_score AND entry_score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL<=entry_score AND entry_score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL<=entry_score AND entry_score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL<=entry_score AND entry_score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_10
		, COUNT(*) FILTER(WHERE entry_score<10::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_lt10 
		, student_id
		, entry_score AS score
		, tag2_rank AS rank
        , tag2rank_pr AS pr
		, tag2rank_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		entry_rank_expand
	WHERE
		item_name LIKE '%(原始)%'
		AND rank_tag2 IS NOT NULL
		AND rank_tag2 <> ''
	
	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, '學期/分項成績'::TEXT AS item_type
		, exam_id AS ref_exam_id
		, item_name
		, '年排名'::TEXT AS rank_type
		, '' || rank_grade_year || '年級'::TEXT AS rank_name
		, true AS is_alive
		, grade_count AS matrix_count
		, STDDEV_POP(entry_score) OVER(PARTITION BY rank_grade_year, item_name) AS std_dev_pop
	    , CASE WHEN (grade_count*88/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE grade_count*88/100+1=grade_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE grade_count*88/100=grade_row_number OR grade_count*88/100+1 = grade_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	      END pr_88
	    , CASE WHEN (grade_count*75/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE grade_count*75/100+1=grade_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE grade_count*75/100=grade_row_number OR grade_count*75/100+1 = grade_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	      END pr_75
	    , CASE WHEN (grade_count*50/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE grade_count*50/100+1=grade_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE grade_count*50/100=grade_row_number OR grade_count*50/100+1 = grade_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	      END pr_50
	    , CASE WHEN (grade_count*25/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE grade_count*25/100+1=grade_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE grade_count*25/100=grade_row_number OR grade_count*25/100+1 = grade_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	      END pr_25
	    , CASE WHEN (grade_count*12/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE grade_count*12/100+1=grade_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE grade_count*12/100=grade_row_number OR grade_count*12/100+1 = grade_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	      END pr_12
		, AVG(entry_score::DECIMAL) FILTER(WHERE grade_rank  *4 <=  grade_count ) OVER(PARTITION BY rank_grade_year, item_name) AS avg_top_25
		, AVG(entry_score::DECIMAL) FILTER(WHERE grade_rank  *2 <=  grade_count ) OVER(PARTITION BY rank_grade_year, item_name) AS avg_top_50
		, AVG(entry_score::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS avg
		, AVG(entry_score::DECIMAL) FILTER(WHERE ( grade_count -grade_rank +1 ) *2 <= grade_count ) OVER(PARTITION BY rank_grade_year, item_name) AS avg_bottom_50
		, AVG(entry_score::DECIMAL) FILTER(WHERE ( grade_count -grade_rank +1 ) *4 <= grade_count ) OVER(PARTITION BY rank_grade_year, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL<=entry_score) OVER(PARTITION BY rank_grade_year, item_name)AS level_gte100 
		, COUNT(*) FILTER(WHERE 90::DECIMAL<=entry_score AND entry_score <100::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL<=entry_score AND entry_score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL<=entry_score AND entry_score <80::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL<=entry_score AND entry_score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL<=entry_score AND entry_score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL<=entry_score AND entry_score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL<=entry_score AND entry_score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL<=entry_score AND entry_score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL<=entry_score AND entry_score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_10
		, COUNT(*) FILTER(WHERE entry_score<10::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_lt10 
		, student_id
		, entry_score AS score
		, grade_rank AS rank
        , graderank_pr AS pr
		, graderank_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		entry_rank_expand
	WHERE
		item_name NOT LIKE '%(原始)%'
	
	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, '學期/分項成績'::TEXT AS item_type
		, exam_id AS ref_exam_id
		, item_name
		, '科排名'::TEXT AS rank_type
		, rank_dept_name AS rank_name
		, true AS is_alive
		, dept_count AS matrix_count
		, STDDEV_POP(entry_score) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS std_dev_pop
	    , CASE WHEN (dept_count*88/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE dept_count*88/100+1=dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE dept_count*88/100=dept_row_number OR dept_count*88/100+1 = dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	      END pr_88
	    , CASE WHEN (dept_count*75/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE dept_count*75/100+1=dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE dept_count*75/100=dept_row_number OR dept_count*75/100+1 = dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	      END pr_75
	    , CASE WHEN (dept_count*50/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE dept_count*50/100+1=dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE dept_count*50/100=dept_row_number OR dept_count*50/100+1 = dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	      END pr_50
	    , CASE WHEN (dept_count*25/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE dept_count*25/100+1=dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE dept_count*25/100=dept_row_number OR dept_count*25/100+1 = dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	      END pr_25
	    , CASE WHEN (dept_count*12/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE dept_count*12/100+1=dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE dept_count*12/100=dept_row_number OR dept_count*12/100+1 = dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	      END pr_12
		, AVG(entry_score::DECIMAL) FILTER(WHERE dept_rank *4 <= dept_count ) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg_top_25
		, AVG(entry_score::DECIMAL) FILTER(WHERE dept_rank *2 <= dept_count ) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg_top_50
		, AVG(entry_score::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg
		, AVG(entry_score::DECIMAL) FILTER(WHERE (dept_count -dept_rank +1 ) *2 <= dept_count ) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg_bottom_50
		, AVG(entry_score::DECIMAL) FILTER(WHERE (dept_count -dept_rank +1 ) *4 <= dept_count ) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL<=entry_score) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_gte100 
		, COUNT(*) FILTER(WHERE 90::DECIMAL<=entry_score AND entry_score <100::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL<=entry_score AND entry_score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL<=entry_score AND entry_score <80::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL<=entry_score AND entry_score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL<=entry_score AND entry_score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL<=entry_score AND entry_score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL<=entry_score AND entry_score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL<=entry_score AND entry_score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL<=entry_score AND entry_score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_10
		, COUNT(*) FILTER(WHERE entry_score<10::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_lt10 
		, student_id
		, entry_score AS score
		, dept_rank AS rank
        , deptrank_pr AS pr
		, deptrank_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		entry_rank_expand
	WHERE
		item_name NOT LIKE '%(原始)%'
	
	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, '學期/分項成績'::TEXT AS item_type
		, exam_id AS ref_exam_id
		, item_name
		, '班排名'::TEXT AS rank_type
		, rank_class_name AS rank_name
		, true AS is_alive
		, class_count AS matrix_count
		, STDDEV_POP(entry_score) OVER(PARTITION BY rank_class_name, item_name) AS std_dev_pop
	    , CASE WHEN (class_count*88/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE class_count*88/100+1=class_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE class_count*88/100=class_row_number OR class_count*88/100+1 = class_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	      END pr_88
	    , CASE WHEN (class_count*75/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE class_count*75/100+1=class_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE class_count*75/100=class_row_number OR class_count*75/100+1 = class_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	      END pr_75
	    , CASE WHEN (class_count*50/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE class_count*50/100+1=class_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE class_count*50/100=class_row_number OR class_count*50/100+1 = class_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	      END pr_50
	    , CASE WHEN (class_count*25/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE class_count*25/100+1=class_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE class_count*25/100=class_row_number OR class_count*25/100+1 = class_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	      END pr_25
	    , CASE WHEN (class_count*12/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE class_count*12/100+1=class_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE class_count*12/100=class_row_number OR class_count*12/100+1 = class_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	      END pr_12
		, AVG(entry_score::DECIMAL) FILTER(WHERE class_rank  *4 <=  class_count ) OVER(PARTITION BY rank_class_name, item_name) AS avg_top_25
		, AVG(entry_score::DECIMAL) FILTER(WHERE class_rank  *2 <=  class_count ) OVER(PARTITION BY rank_class_name, item_name) AS avg_top_50
		, AVG(entry_score::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS avg
		, AVG(entry_score::DECIMAL) FILTER(WHERE (class_count -class_rank +1 )*2 <= class_count ) OVER(PARTITION BY rank_class_name, item_name) AS avg_bottom_50
		, AVG(entry_score::DECIMAL) FILTER(WHERE (class_count -class_rank +1 )*4 <= class_count ) OVER(PARTITION BY rank_class_name, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL<=entry_score) OVER(PARTITION BY rank_class_name, item_name)AS level_gte100 
		, COUNT(*) FILTER(WHERE 90::DECIMAL<=entry_score AND entry_score <100::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL<=entry_score AND entry_score <90::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL<=entry_score AND entry_score <80::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL<=entry_score AND entry_score <70::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL<=entry_score AND entry_score <60::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL<=entry_score AND entry_score <50::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL<=entry_score AND entry_score <40::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL<=entry_score AND entry_score <30::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL<=entry_score AND entry_score <20::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_10
		, COUNT(*) FILTER(WHERE entry_score<10::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_lt10 
		, student_id
		, entry_score AS score
		, class_rank AS rank
        , classrank_pr AS pr
		, classrank_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		entry_rank_expand
	WHERE
		item_name NOT LIKE '%(原始)%'
	
	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, '學期/分項成績'::TEXT AS item_type
		, exam_id AS ref_exam_id
		, item_name
		, '類別1排名'::TEXT AS rank_type
		, rank_tag1 AS rank_name
		, true AS is_alive
		, tag1_count AS matrix_count
		, STDDEV_POP(entry_score) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS std_dev_pop
	    , CASE WHEN (tag1_count*88/100::DECIMAL % 1 <> 0) 
	      THEN AVG(entry_score::DECIMAL)FILTER(WHERE tag1_count*88/100+1=tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	      ELSE AVG(entry_score::DECIMAL)FILTER(WHERE tag1_count*88/100=tag1_row_number OR tag1_count*88/100+1 = tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	    END pr_88
	    , CASE WHEN (tag1_count*75/100::DECIMAL % 1 <> 0) 
	      THEN AVG(entry_score::DECIMAL)FILTER(WHERE tag1_count*75/100+1=tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	      ELSE AVG(entry_score::DECIMAL)FILTER(WHERE tag1_count*75/100=tag1_row_number OR tag1_count*75/100+1 = tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	    END pr_75
	    , CASE WHEN (tag1_count*50/100::DECIMAL % 1 <> 0) 
	      THEN AVG(entry_score::DECIMAL)FILTER(WHERE tag1_count*50/100+1=tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	      ELSE AVG(entry_score::DECIMAL)FILTER(WHERE tag1_count*50/100=tag1_row_number OR tag1_count*50/100+1 = tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	    END pr_50
	    , CASE WHEN (tag1_count*25/100::DECIMAL % 1 <> 0) 
	      THEN AVG(entry_score::DECIMAL)FILTER(WHERE tag1_count*25/100+1=tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	      ELSE AVG(entry_score::DECIMAL)FILTER(WHERE tag1_count*25/100=tag1_row_number OR tag1_count*25/100+1 = tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	    END pr_25
	    , CASE WHEN (tag1_count*12/100::DECIMAL % 1 <> 0) 
	      THEN AVG(entry_score::DECIMAL)FILTER(WHERE tag1_count*12/100+1=tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	      ELSE AVG(entry_score::DECIMAL)FILTER(WHERE tag1_count*12/100=tag1_row_number OR tag1_count*12/100+1 = tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	    END pr_12
		, AVG(entry_score::DECIMAL) FILTER(WHERE tag1_rank *4 <= tag1_count ) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_top_25
		, AVG(entry_score::DECIMAL) FILTER(WHERE tag1_rank *2 <= tag1_count ) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_top_50
		, AVG(entry_score::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg
		, AVG(entry_score::DECIMAL) FILTER(WHERE ( tag1_count - tag1_rank +1 ) * 2 <= tag1_count ) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_bottom_50
		, AVG(entry_score::DECIMAL) FILTER(WHERE  (tag1_count - tag1_rank +1 ) * 4 <= tag1_count ) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL<=entry_score) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_gte100 
		, COUNT(*) FILTER(WHERE 90::DECIMAL<=entry_score AND entry_score <100::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL<=entry_score AND entry_score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL<=entry_score AND entry_score <80::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL<=entry_score AND entry_score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL<=entry_score AND entry_score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL<=entry_score AND entry_score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL<=entry_score AND entry_score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL<=entry_score AND entry_score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL<=entry_score AND entry_score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_10
		, COUNT(*) FILTER(WHERE entry_score<10::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_lt10 
		, student_id
		, entry_score AS score
		, tag1_rank AS rank
        , tag1rank_pr AS pr
		, tag1rank_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		entry_rank_expand
	WHERE
		item_name NOT LIKE '%(原始)%'
		AND rank_tag1 IS NOT NULL
		AND rank_tag1 <> ''
	
	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, '學期/分項成績'::TEXT AS item_type
		, exam_id AS ref_exam_id
		, item_name
		, '類別2排名'::TEXT AS rank_type
		, rank_tag2 AS rank_name
		, true AS is_alive
		, tag2_count AS matrix_count
		, STDDEV_POP(entry_score) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS std_dev_pop
	    , CASE WHEN (tag2_count*88/100::DECIMAL % 1 <> 0) 
	      THEN AVG(entry_score::DECIMAL)FILTER(WHERE tag2_count*88/100+1=tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	      ELSE AVG(entry_score::DECIMAL)FILTER(WHERE tag2_count*88/100=tag2_row_number OR tag2_count*88/100+1 = tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	    END pr_88
	    , CASE WHEN (tag2_count*75/100::DECIMAL % 1 <> 0) 
	      THEN AVG(entry_score::DECIMAL)FILTER(WHERE tag2_count*75/100+1=tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	      ELSE AVG(entry_score::DECIMAL)FILTER(WHERE tag2_count*75/100=tag2_row_number OR tag2_count*75/100+1 = tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	    END pr_75
	    , CASE WHEN (tag2_count*50/100::DECIMAL % 1 <> 0) 
	      THEN AVG(entry_score::DECIMAL)FILTER(WHERE tag2_count*50/100+1=tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	      ELSE AVG(entry_score::DECIMAL)FILTER(WHERE tag2_count*50/100=tag2_row_number OR tag2_count*50/100+1 = tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	    END pr_50
	    , CASE WHEN (tag2_count*25/100::DECIMAL % 1 <> 0) 
	      THEN AVG(entry_score::DECIMAL)FILTER(WHERE tag2_count*25/100+1=tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	      ELSE AVG(entry_score::DECIMAL)FILTER(WHERE tag2_count*25/100=tag2_row_number OR tag2_count*25/100+1 = tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	    END pr_25
	    , CASE WHEN (tag2_count*12/100::DECIMAL % 1 <> 0) 
	      THEN AVG(entry_score::DECIMAL)FILTER(WHERE tag2_count*12/100+1=tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	      ELSE AVG(entry_score::DECIMAL)FILTER(WHERE tag2_count*12/100=tag2_row_number OR tag2_count*12/100+1 = tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	    END pr_12
		, AVG(entry_score::DECIMAL) FILTER(WHERE tag2_rank  *4 <=  tag2_count ) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_top_25
		, AVG(entry_score::DECIMAL) FILTER(WHERE tag2_rank  *2 <=  tag2_count ) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_top_50
		, AVG(entry_score::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg
		, AVG(entry_score::DECIMAL) FILTER(WHERE (tag2_count -tag2_rank +1 ) *2 <= tag2_count ) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_bottom_50
		, AVG(entry_score::DECIMAL) FILTER(WHERE (tag2_count -tag2_rank +1 ) *4 <= tag2_count ) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL<=entry_score) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_gte100 
		, COUNT(*) FILTER(WHERE 90::DECIMAL<=entry_score AND entry_score <100::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL<=entry_score AND entry_score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL<=entry_score AND entry_score <80::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL<=entry_score AND entry_score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL<=entry_score AND entry_score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL<=entry_score AND entry_score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL<=entry_score AND entry_score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL<=entry_score AND entry_score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL<=entry_score AND entry_score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_10
		, COUNT(*) FILTER(WHERE entry_score<10::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_lt10 
		, student_id
		, entry_score AS score
		, tag2_rank AS rank
        , tag2rank_pr AS pr
		, tag2rank_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		entry_rank_expand
	WHERE
		item_name NOT LIKE '%(原始)%'
		AND rank_tag2 IS NOT NULL
		AND rank_tag2 <> ''
		 
)
, update_data AS
(
	UPDATE
		rank_matrix
	SET
		is_alive = NULL
	FROM
		calc_condition
	WHERE
		rank_matrix.is_alive = true
		AND rank_matrix.school_year = calc_condition.rank_school_year::INT
		AND rank_matrix.semester = calc_condition.rank_semester::INT
		AND rank_matrix.grade_year = calc_condition.rank_grade_year::INT
		AND rank_matrix.ref_exam_id = calc_condition.ref_exam_id::INT

	RETURNING rank_matrix.*
)
, insert_matrix_data AS
(
	INSERT INTO
		rank_matrix
		(
			ref_batch_id
			, school_year
			, semester
			, grade_year
			, item_type
			, ref_exam_id
			, item_name
			, rank_type
			, rank_name
			, is_alive
			, matrix_count
			, avg_top_25
			, avg_top_50
			, avg
			, avg_bottom_50
			, avg_bottom_25
			, level_gte100
			, level_90
			, level_80
			, level_70
			, level_60
			, level_50
			, level_40
			, level_30
			, level_20
			, level_10
			, level_lt10
			, pr_88
			, pr_75
			, pr_50
			, pr_25
			, pr_12
			, std_dev_pop
		)
		SELECT
			calc_condition.batch_id AS ref_batch_id
			, score_list.rank_school_year::INT
			, score_list.rank_semester::INT
			, score_list.rank_grade_year::INT
			, score_list.item_type
			, score_list.ref_exam_id
			, score_list.item_name
			, score_list.rank_type
			, score_list.rank_name
			, score_list.is_alive
			, score_list.matrix_count
			, score_list.avg_top_25
			, score_list.avg_top_50
			, score_list.avg
			, score_list.avg_bottom_50
			, score_list.avg_bottom_25
			, score_list.level_gte100
			, score_list.level_90
			, score_list.level_80
			, score_list.level_70
			, score_list.level_60
			, score_list.level_50
			, score_list.level_40
			, score_list.level_30
			, score_list.level_20
			, score_list.level_10
			, score_list.level_lt10
			, score_list.pr_88
			, score_list.pr_75
			, score_list.pr_50
			, score_list.pr_25
			, score_list.pr_12
			, score_list.std_dev_pop
		FROM
			score_list
			LEFT OUTER JOIN update_data
				ON update_data.id  < 0 --永遠為false，只是為了讓insert等待update執行完
			CROSS JOIN calc_condition
		GROUP BY
			calc_condition.batch_id
			, score_list.rank_school_year
			, score_list.rank_semester
			, score_list.rank_grade_year
			, score_list.item_type
			, score_list.ref_exam_id
			, score_list.item_name
			, score_list.rank_type
			, score_list.rank_name
			, score_list.is_alive
			, score_list.matrix_count
			, score_list.avg_top_25
			, score_list.avg_top_50
			, score_list.avg
			, score_list.avg_bottom_50
			, score_list.avg_bottom_25
			, score_list.level_gte100
			, score_list.level_90
			, score_list.level_80
			, score_list.level_70
			, score_list.level_60
			, score_list.level_50
			, score_list.level_40
			, score_list.level_30
			, score_list.level_20
			, score_list.level_10
			, score_list.level_lt10
			, score_list.pr_88
			, score_list.pr_75
			, score_list.pr_50
			, score_list.pr_25
			, score_list.pr_12
			, score_list.std_dev_pop

	RETURNING *
)
, insert_batch_student_data AS
(
	INSERT INTO
		rank_batch_student
		(
			ref_batch_id
			, ref_student_id
			, grade_year
			, matrix_grade
			, matrix_class
			, matrix_tag1
			, matrix_tag2
		)
		SELECT
			calc_condition.batch_id AS ref_batch_id
			, score_list.student_id
			, score_list.rank_grade_year::INT
			, score_list.rank_grade_year||'年級' AS matrix_grade
			, score_list.rank_class_name
			, score_list.rank_tag1
			, score_list.rank_tag2
		FROM
			score_list
			CROSS JOIN calc_condition
)
, insert_detail_data AS
(
	INSERT INTO
		rank_detail
		(
			ref_matrix_id
			, ref_student_id
			, score
			, rank
            , pr
			, percentile
		)
		SELECT
			insert_matrix_data.id AS ref_matrix_id
			, score_list.student_id AS ref_student_id
			, score_list.score AS score
			, score_list.rank AS rank
            , score_list.pr AS pr
			, score_list.percentile AS percentile
		FROM
			score_list
			LEFT OUTER JOIN
				insert_matrix_data
					ON insert_matrix_data.school_year = score_list.rank_school_year::INT
					AND insert_matrix_data.semester = score_list.rank_semester::INT
					AND insert_matrix_data.grade_year = score_list.rank_grade_year::INT
					AND insert_matrix_data.item_type = score_list.item_type
					AND insert_matrix_data.ref_exam_id = score_list.ref_exam_id
					AND insert_matrix_data.item_name = score_list.item_name
					AND insert_matrix_data.rank_type = score_list.rank_type
					AND insert_matrix_data.rank_name = score_list.rank_name
)
SELECT count(*) FROM score_list
";
        }

        private static bool Contains(IList<string> selectedScoreTypes, string name)
        {
            if (selectedScoreTypes == null)
                return false;
            for (int i = 0; i < selectedScoreTypes.Count; i++)
            {
                if (selectedScoreTypes[i] == name)
                    return true;
            }
            return false;
        }
    }
}