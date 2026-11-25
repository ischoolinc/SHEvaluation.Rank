namespace SHEvaluation.Rank.RegularRank
{
    // Original regular formula, extracted without changing ranking or statistics expressions.
    public static class RegularRankFormulaSql
    {
        public static string Build(string conditionSql, string studentSql)
        {
            return @"
WITH row AS (
" + conditionSql + @"
), student_row AS (
" + studentSql + @"
), calc_subject AS ( --採計科目
        SELECT
         unnest(xpath('/Setting/採計科目/text()', xmlparse(content calculation_setting)))::TEXT AS subject
    FROM
        row
), calc_subject_tag1 AS ( --類別一採計科目
    SELECT
        unnest(xpath('/Setting/類別一採計科目/text()', xmlparse(content calculation_setting)))::TEXT AS subject
    FROM
        row	
), calc_subject_tag2 AS ( --類別二採計科目
     SELECT
        unnest(xpath('/Setting/類別二採計科目/text()', xmlparse(content calculation_setting)))::TEXT AS subject
    FROM
        row			
), score_detail_row AS (--取得學生的定期評量成績
	SELECT
		student_row.student_id
   		, student_row.student_name
		, sc_attend.id AS sc_attend_id
		, course.course_name
		, course.school_year AS rank_school_year
		, course.semester AS rank_semester
		, course.subject
		, course.domain
		, course.credit
		, exam.id AS exam_id
		, exam.exam_name
		, student_row.rank_class_name
		, student_row.rank_grade_year
        , student_row.rank_dept_name
		, student_row.rank_tag1
		, student_row.rank_tag2
		, CASE WHEN sce_take.score = -1 THEN 0 ELSE sce_take.score END AS score
	FROM  
        sce_take
		LEFT JOIN sc_attend 
			ON ref_sc_attend_id = sc_attend.id
		LEFT JOIN exam 
			ON ref_exam_id = exam.id
		LEFT JOIN course 
			ON sc_attend.ref_course_id = course.id
            AND course.subject IN (
                SELECT subject FROM calc_subject
				UNION
				SELECT subject FROM calc_subject_tag1
				UNION
				SELECT subject FROM calc_subject_tag2 
            )
		INNER JOIN student_row
			ON sc_attend.ref_student_id = student_row.student_id
		INNER JOIN row
			ON course.school_year = row.rank_school_year::int
			AND course.semester = row.rank_semester::int
			AND student_row.rank_grade_year = row.rank_grade_year::int
			AND exam.id = row.ref_exam_id::INT
	WHERE 
        sce_take.score IS NOT NULL
        AND sce_take.score <> -2

    --2.1 科目成績 年排名
    --2.2 科目成績 班排名
    --2.5 科目成績 科排名
), exam_score AS (-------結算定期評量總成績
	SELECT	
		score_detail_row.*
	FROM 
		score_detail_row
	WHERE 
	    subject IN (
            SELECT subject
            FROM calc_subject
        )
), subject_rank_row AS (--------計算科目排名
	SELECT
		student_id
		, rank_tag1
		, rank_tag2
		, '定期評量/科目成績'::TEXT AS item_type
		, subject AS item_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, rank_dept_name
		, exam_id
		, score
		, RANK() OVER(PARTITION BY rank_grade_year,subject ORDER BY score DESC) AS grade_rank
		, RANK() OVER(PARTITION BY rank_class_name, subject ORDER BY score DESC) AS class_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name, subject ORDER BY score DESC) AS dept_rank
		, RANK() OVER(PARTITION BY rank_grade_year,subject ORDER BY score ASC) AS grade_rank_reverse
		, RANK() OVER(PARTITION BY rank_class_name, subject ORDER BY score ASC) AS class_rank_reverse
		, RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name, subject ORDER BY score ASC) AS dept_rank_reverse
		, COUNT (student_id) OVER(PARTITION BY rank_grade_year,subject ) AS grade_count
		, COUNT (student_id) OVER(PARTITION BY rank_class_name, subject) AS class_count
		, COUNT (student_id) OVER(PARTITION BY rank_grade_year, rank_dept_name, subject) AS dept_count
	    , ROW_NUMBER() OVER(PARTITION BY rank_grade_year,subject ORDER BY score ASC) AS grade_row_number
	    , ROW_NUMBER() OVER(PARTITION BY rank_class_name, subject ORDER BY score ASC) AS class_row_number
	    , ROW_NUMBER() OVER(PARTITION BY rank_grade_year, rank_dept_name, subject ORDER BY score ASC) AS dept_row_number
	FROM 
        exam_score
	WHERE 
        subject IS NOT NULL
), subject_rank AS (-----------計算科目排名排名百分比及PR
	SELECT  
		s1.*
		, FLOOR((grade_rank::DECIMAL-1)*100::DECIMAL/grade_count)+1 AS graderank_percentage
		, FLOOR((class_rank::DECIMAL-1)*100::DECIMAL/class_count)+1 AS classrank_percentage
		, FLOOR((dept_rank::DECIMAL-1)*100::DECIMAL/dept_count)+1 AS deptrank_percentage
        , FLOOR((grade_rank_reverse::DECIMAL-1)*100::DECIMAL/grade_count) AS graderank_pr
        , FLOOR((class_rank_reverse::DECIMAL-1)*100::DECIMAL/class_count) AS classrank_pr
        , FLOOR((dept_rank_reverse::DECIMAL-1)*100::DECIMAL/dept_count) AS deptrank_pr
	FROM 
		subject_rank_row AS s1



    --2.3 科目成績 類別1排名
), exam_score_tag1 AS (-------結算定期評量總成績
	SELECT	
		score_detail_row.*
	FROM 
		score_detail_row
	WHERE 
	    subject IN (
            SELECT subject
            FROM calc_subject_tag1
        )
), subject_rank_row_tag1 AS (--------計算科目排名
	SELECT
		student_id
		, rank_tag1
		, rank_tag2
		, '定期評量/科目成績'::TEXT AS item_type
		, subject AS item_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, score
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag1, subject ORDER BY score DESC) AS tag1_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag1, subject ORDER BY score ASC) AS tag1_rank_reverse
		, COUNT (student_id) OVER(PARTITION BY rank_grade_year, rank_tag1, subject) AS tag1_count
		, ROW_NUMBER() OVER(PARTITION BY rank_grade_year, rank_tag1, subject ORDER BY score ASC) AS tag1_row_number
	FROM 
        exam_score_tag1
	WHERE 
        subject IS NOT NULL
), subject_rank_tag1 AS (-----------計算科目排名排名百分比及PR
	SELECT  
		s1.*
		, FLOOR((tag1_rank::DECIMAL-1)*100::DECIMAL/tag1_count)+1 AS tag1rank_percentage
        , FLOOR((tag1_rank_reverse::DECIMAL-1)*100::DECIMAL/tag1_count) AS tag1rank_pr
	FROM 
		subject_rank_row_tag1 AS s1



    --2.4 科目成績 類別2排名
), exam_score_tag2 AS (-------結算定期評量總成績
	SELECT	
		score_detail_row.*
	FROM 
		score_detail_row
	WHERE 
	    subject IN (
            SELECT subject
            FROM calc_subject_tag2
        )
), subject_rank_row_tag2 AS (--------計算科目排名
	SELECT
		student_id
		, rank_tag1
		, rank_tag2
		, '定期評量/科目成績'::TEXT AS item_type
		, subject AS item_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, score
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag2, subject ORDER BY score DESC) AS tag2_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag2, subject ORDER BY score ASC) AS tag2_rank_reverse
		, COUNT (student_id) OVER(PARTITION BY rank_grade_year, rank_tag2, subject) AS tag2_count
		, ROW_NUMBER() OVER(PARTITION BY rank_grade_year, rank_tag2, subject ORDER BY score ASC) AS tag2_row_number
	FROM 
        exam_score_tag2
	WHERE 
        subject IS NOT NULL
), subject_rank_tag2 AS (-----------計算科目排名排名百分比及PR
	SELECT  
		s1.*
		, FLOOR((tag2_rank::DECIMAL-1)*100::DECIMAL/tag2_count)+1 AS tag2rank_percentage
        , FLOOR((tag2_rank_reverse::DECIMAL-1)*100::DECIMAL/tag2_count) AS tag2rank_pr
	FROM 
		subject_rank_row_tag2 AS s1



    --1.1 領域成績 年排名
    --1.2 領域成績 班排名
), domain_score AS (-----結算領域成績
	SELECT 
		student_id
		, student_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, rank_dept_name
		, exam_id
		, domain
		, rank_tag1
		, rank_tag2
		, CASE 
            WHEN SUM(credit::DECIMAL) IS NULL THEN NULL::DECIMAL
            WHEN SUM(credit::DECIMAL) = 0 THEN 0::DECIMAL
            ELSE SUM(score::DECIMAL * credit::DECIMAL) / SUM(credit::DECIMAL)
            END AS score
	FROM  
		exam_score
	WHERE
		score IS NOT NULL
		AND credit IS NOT NULL
	GROUP BY
		domain
        , rank_school_year
        , rank_semester
        , rank_grade_year
        , rank_class_name
		, rank_dept_name
        , exam_id
        , student_id
        , student_name
        , rank_tag1
        , rank_tag2
), domain_rank_row AS (-------計算領域排名
	SELECT
		student_id
		, rank_tag1
		, rank_tag2
		, '定期評量/領域成績'::TEXT AS item_type
		, domain::TEXT AS item_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, rank_dept_name
		, exam_id
		, score
		, RANK() OVER(PARTITION BY rank_grade_year, domain ORDER BY score DESC) AS grade_rank
		, RANK() OVER(PARTITION BY rank_class_name, domain ORDER BY score DESC) AS class_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name, domain ORDER BY score DESC) AS dept_rank
		, RANK() OVER(PARTITION BY rank_grade_year, domain ORDER BY score ASC) AS grade_rank_reverse
		, RANK() OVER(PARTITION BY rank_class_name, domain ORDER BY score ASC) AS class_rank_reverse
		, RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name, domain ORDER BY score ASC) AS dept_rank_reverse
		, COUNT (student_id) OVER(PARTITION BY rank_grade_year, domain) AS grade_count
		, COUNT (student_id) OVER(PARTITION BY rank_class_name, domain) AS class_count
		, COUNT (student_id) OVER(PARTITION BY rank_grade_year, rank_dept_name, domain) AS dept_count
	    , ROW_NUMBER() OVER(PARTITION BY rank_grade_year, domain ORDER BY score ASC) AS grade_row_number
	    , ROW_NUMBER() OVER(PARTITION BY rank_class_name, domain ORDER BY score ASC) AS class_row_number
	    , ROW_NUMBER() OVER(PARTITION BY rank_grade_year, rank_dept_name, domain ORDER BY score ASC) AS dept_row_number
	FROM 
		domain_score
	WHERE
		domain IS NOT NULL
), domain_rank AS (-----------計算領域排名排名百分比及PR
	SELECT  
		s1.*
		, FLOOR((grade_rank::DECIMAL-1)*100::DECIMAL/grade_count)+1 AS graderank_percentage
		, FLOOR((class_rank::DECIMAL-1)*100::DECIMAL/class_count)+1 AS classrank_percentage
		, FLOOR((dept_rank::DECIMAL-1)*100::DECIMAL/dept_count)+1 AS deptrank_percentage
        , FLOOR((grade_rank_reverse::DECIMAL-1)*100::DECIMAL/grade_count) AS graderank_pr
        , FLOOR((class_rank_reverse::DECIMAL-1)*100::DECIMAL/class_count) AS classrank_pr
        , FLOOR((dept_rank_reverse::DECIMAL-1)*100::DECIMAL/dept_count) AS deptrank_pr
	FROM 
		domain_rank_row AS s1



    --1.3 領域成績 類別1排名
), domain_score_tag1 AS (-----結算領域成績
	SELECT 
		student_id
		, student_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, domain
		, rank_tag1
		, rank_tag2
		, CASE 
            WHEN SUM(credit::DECIMAL) IS NULL THEN NULL::DECIMAL
            WHEN SUM(credit::DECIMAL) = 0 THEN 0::DECIMAL
            ELSE SUM(score::DECIMAL * credit::DECIMAL) / SUM(credit::DECIMAL)
            END AS score
	FROM  
		exam_score
	WHERE
		score IS NOT NULL
		AND credit IS NOT NULL
	    AND subject IN (
            SELECT subject
            FROM calc_subject_tag1
        )
	GROUP BY
		domain
        , rank_school_year
        , rank_semester
        , rank_grade_year
        , rank_class_name
        , exam_id
        , student_id
        , student_name
        , rank_tag1
        , rank_tag2
), domain_rank_row_tag1 AS (-------計算領域排名
	SELECT
		student_id
		, rank_tag1
		, rank_tag2
		, '定期評量/領域成績'::TEXT AS item_type
		, domain::TEXT AS item_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, score
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag1, domain ORDER BY score DESC) AS tag1_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag1, domain ORDER BY score ASC) AS tag1_rank_reverse
		, COUNT (student_id) OVER(PARTITION BY rank_grade_year, rank_tag1, domain) AS tag1_count
		, ROW_NUMBER() OVER(PARTITION BY rank_grade_year, rank_tag1, domain ORDER BY score ASC) AS tag1_row_number
	FROM 
		domain_score_tag1
	WHERE
		domain IS NOT NULL
), domain_rank_tag1 AS (-----------計算領域排名排名百分比及PR
	SELECT  
		s1.*
		, FLOOR((tag1_rank::DECIMAL-1)*100::DECIMAL/tag1_count)+1 AS tag1rank_percentage
        , FLOOR((tag1_rank_reverse::DECIMAL-1)*100::DECIMAL/tag1_count) AS tag1rank_pr
	FROM 
		domain_rank_row_tag1 AS s1



    --1.4 領域成績 類別2排名
), domain_score_tag2 AS (-----結算領域成績
	SELECT 
		student_id
		, student_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, domain
		, rank_tag1
		, rank_tag2
		, CASE 
            WHEN SUM(credit::DECIMAL) IS NULL THEN NULL::DECIMAL
            WHEN SUM(credit::DECIMAL) = 0 THEN 0::DECIMAL
            ELSE SUM(score::DECIMAL * credit::DECIMAL) / SUM(credit::DECIMAL)
            END AS score
	FROM  
		exam_score
	WHERE
		score IS NOT NULL
		AND credit IS NOT NULL
	    AND subject IN (
            SELECT subject
            FROM calc_subject_tag2
        )
	GROUP BY
		domain
        , rank_school_year
        , rank_semester
        , rank_grade_year
        , rank_class_name
        , exam_id
        , student_id
        , student_name
        , rank_tag1
        , rank_tag2
), domain_rank_row_tag2 AS (-------計算領域排名
	SELECT
		student_id
		, rank_tag1
		, rank_tag2
		, '定期評量/領域成績'::TEXT AS item_type
		, domain::TEXT AS item_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, score
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag2, domain ORDER BY score DESC) AS tag2_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag2, domain ORDER BY score ASC) AS tag2_rank_reverse
		, COUNT (student_id) OVER(PARTITION BY rank_grade_year, rank_tag2, domain) AS tag2_count
		, ROW_NUMBER() OVER(PARTITION BY rank_grade_year, rank_tag2, domain ORDER BY score ASC) AS tag2_row_number
	FROM 
		domain_score_tag2
	WHERE
		domain IS NOT NULL
), domain_rank_tag2 AS (-----------計算領域排名排名百分比及PR
	SELECT  
		s1.*
		, FLOOR((tag2_rank::DECIMAL-1)*100::DECIMAL/tag2_count)+1 AS tag2rank_percentage
        , FLOOR((tag2_rank_reverse::DECIMAL-1)*100::DECIMAL/tag2_count) AS tag2rank_pr
	FROM 
		domain_rank_row_tag2 AS s1



    --3.1 總計成績 總分 年排名
    --3.2 總計成績 總分 班排名
    --3.5 總計成績 總分 科排名
), calc_sum_score AS (------算數總分排名所需成績
	SELECT
		student_id
		, student_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, rank_dept_name
		, exam_id
		, rank_tag1
		, rank_tag2
		, SUM( score::decimal ) AS score
	FROM 
		exam_score
	WHERE
		score IS NOT NULL
		AND credit IS NOT NULL
	GROUP BY 
		student_id
        , student_name
        , rank_school_year
        , rank_semester
        , rank_grade_year
        , rank_class_name
		, rank_dept_name
        , exam_id
        , rank_tag1
        , rank_tag2
), calc_sum_rank_row AS (-----------計算總分排名
	SELECT 
		student_id
		, rank_tag1
		, rank_tag2
		, '定期評量/總計成績'::text AS item_type
		, '總分'::TEXT As item_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, rank_dept_name
		, exam_id
		, score
		, RANK() OVER(PARTITION BY rank_grade_year ORDER BY score DESC) AS grade_rank
		, RANK() OVER(PARTITION BY rank_class_name ORDER BY score DESC) AS class_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name ORDER BY score DESC) AS dept_rank
		, RANK() OVER(PARTITION BY rank_grade_year ORDER BY score ASC) AS grade_rank_reverse
		, RANK() OVER(PARTITION BY rank_class_name ORDER BY score ASC) AS class_rank_reverse
		, RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name ORDER BY score ASC) AS dept_rank_reverse
		, COUNT (*) OVER(PARTITION BY rank_grade_year) AS grade_count
		, COUNT (*) OVER(PARTITION BY rank_class_name) AS class_count
		, COUNT (*) OVER(PARTITION BY rank_grade_year, rank_dept_name) AS dept_count
	    , ROW_NUMBER() OVER(PARTITION BY rank_grade_year ORDER BY score ASC) AS grade_row_number
	    , ROW_NUMBER() OVER(PARTITION BY rank_class_name ORDER BY score ASC) AS class_row_number
	    , ROW_NUMBER() OVER(PARTITION BY rank_grade_year, rank_dept_name ORDER BY score ASC) AS dept_row_number
	FROM 
		calc_sum_score
), calc_sum_rank AS (-----------計算總分排名百分比及PR
	SELECT  
		s1.*
		, FLOOR((grade_rank::DECIMAL-1)*100::DECIMAL/grade_count)+1 AS graderank_percentage
		, FLOOR((class_rank::DECIMAL-1)*100::DECIMAL/class_count)+1 AS classrank_percentage
		, FLOOR((dept_rank::DECIMAL-1)*100::DECIMAL/dept_count)+1 AS deptrank_percentage
        , FLOOR((grade_rank_reverse::DECIMAL-1)*100::DECIMAL/grade_count) AS graderank_pr
        , FLOOR((class_rank_reverse::DECIMAL-1)*100::DECIMAL/class_count) AS classrank_pr
        , FLOOR((dept_rank_reverse::DECIMAL-1)*100::DECIMAL/dept_count) AS deptrank_pr
	FROM 
		calc_sum_rank_row AS s1



    --3.3 總計成績 總分 類別1排名
), calc_sum_score_tag1 AS (------算數總分排名所需成績
	SELECT
		student_id
		, student_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, rank_tag1
		, rank_tag2
		, SUM( score::decimal ) AS score
	FROM 
		exam_score_tag1
	WHERE
		score IS NOT NULL
		AND credit IS NOT NULL
	    AND subject IN (
            SELECT subject
            FROM calc_subject_tag1
        )
	GROUP BY 
		student_id
        , student_name
        , rank_school_year
        , rank_semester
        , rank_grade_year
        , rank_class_name
        , exam_id
        , rank_tag1
        , rank_tag2
), calc_sum_rank_row_tag1 AS (-----------計算總分排名
	SELECT 
		student_id
		, rank_tag1
		, rank_tag2
		, '定期評量/總計成績'::text AS item_type
		, '總分'::TEXT As item_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, score
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag1 ORDER BY score DESC) AS tag1_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag1 ORDER BY score ASC) AS tag1_rank_reverse
		, COUNT (*) OVER(PARTITION BY rank_grade_year, rank_tag1) AS tag1_count
		, ROW_NUMBER() OVER(PARTITION BY rank_grade_year, rank_tag1 ORDER BY score ASC) AS tag1_row_number
	FROM 
		calc_sum_score_tag1
), calc_sum_rank_tag1 AS (-----------計算總分排名百分比及PR
	SELECT  
		s1.*
		, FLOOR((tag1_rank::DECIMAL-1)*100::DECIMAL/tag1_count)+1 AS tag1rank_percentage
        , FLOOR((tag1_rank_reverse::DECIMAL-1)*100::DECIMAL/tag1_count) AS tag1rank_pr
	FROM 
		calc_sum_rank_row_tag1 AS s1



    --3.4 總計成績 總分 類別2排名
), calc_sum_score_tag2 AS (------算數總分排名所需成績
	SELECT
		student_id
		, student_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, rank_tag1
		, rank_tag2
		, SUM( score::decimal ) AS score
	FROM 
		exam_score_tag2
	WHERE
		score IS NOT NULL
		AND credit IS NOT NULL
	    AND subject IN (
            SELECT subject
            FROM calc_subject_tag2
        )
	GROUP BY 
		student_id
        , student_name
        , rank_school_year
        , rank_semester
        , rank_grade_year
        , rank_class_name
        , exam_id
        , rank_tag1
        , rank_tag2
), calc_sum_rank_row_tag2 AS (-----------計算總分排名
	SELECT 
		student_id
		, rank_tag1
		, rank_tag2
		, '定期評量/總計成績'::text AS item_type
		, '總分'::TEXT As item_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, score
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag2 ORDER BY score DESC) AS tag2_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag2 ORDER BY score ASC) AS tag2_rank_reverse
		, COUNT (*) OVER(PARTITION BY rank_grade_year, rank_tag2) AS tag2_count
		, ROW_NUMBER() OVER(PARTITION BY rank_grade_year, rank_tag2 ORDER BY score ASC) AS tag2_row_number
	FROM 
		calc_sum_score_tag2
), calc_sum_rank_tag2 AS (-----------計算總分排名百分比及PR
	SELECT  
		s1.*
		, FLOOR((tag2_rank::DECIMAL-1)*100::DECIMAL/tag2_count)+1 AS tag2rank_percentage
        , FLOOR((tag2_rank_reverse::DECIMAL-1)*100::DECIMAL/tag2_count) AS tag2rank_pr
	FROM 
		calc_sum_rank_row_tag2 AS s1



    --4.1 總計成績 平均 年排名
    --4.2 總計成績 平均 班排名
    --4.5 總計成績 平均 科排名
), calc_avg_score AS (------算數平均排名所需成績
	SELECT
		student_id
		, student_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, rank_dept_name
		, exam_id
		, rank_tag1
		, rank_tag2
		, AVG( score::decimal ) AS score
	FROM 
		exam_score
	WHERE
		score IS NOT NULL
		AND credit IS NOT NULL
	GROUP BY 
		student_id
        , student_name
        , rank_school_year
        , rank_semester
        , rank_grade_year
        , rank_class_name
		, rank_dept_name
        , exam_id
        , rank_tag1
        , rank_tag2
), calc_avg_rank_row AS (-----------計算平均排名
	SELECT 
		student_id
		, rank_tag1
		, rank_tag2
		, '定期評量/總計成績'::text AS item_type
		, '平均'::TEXT As item_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, rank_dept_name
		, exam_id
		, score
		, RANK() OVER(PARTITION BY rank_grade_year ORDER BY score DESC) AS grade_rank
		, RANK() OVER(PARTITION BY rank_class_name ORDER BY score DESC) AS class_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name ORDER BY score DESC) AS dept_rank
		, RANK() OVER(PARTITION BY rank_grade_year ORDER BY score ASC) AS grade_rank_reverse
		, RANK() OVER(PARTITION BY rank_class_name ORDER BY score ASC) AS class_rank_reverse
		, RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name ORDER BY score ASC) AS dept_rank_reverse
		, COUNT (*) OVER(PARTITION BY rank_grade_year) AS grade_count
		, COUNT (*) OVER(PARTITION BY rank_class_name) AS class_count
		, COUNT (student_id) OVER(PARTITION BY rank_grade_year, rank_dept_name) AS dept_count
	    , ROW_NUMBER() OVER(PARTITION BY rank_grade_year ORDER BY score ASC) AS grade_row_number
	    , ROW_NUMBER() OVER(PARTITION BY rank_class_name ORDER BY score ASC) AS class_row_number
	    , ROW_NUMBER() OVER(PARTITION BY rank_grade_year, rank_dept_name ORDER BY score ASC) AS dept_row_number
	FROM 
		calc_avg_score
), calc_avg_rank AS (-----------計算平均排名百分比及PR
	SELECT  
		s1.*
		, FLOOR((grade_rank::DECIMAL-1)*100::DECIMAL/grade_count)+1 AS graderank_percentage
		, FLOOR((class_rank::DECIMAL-1)*100::DECIMAL/class_count)+1 AS classrank_percentage
		, FLOOR((dept_rank::DECIMAL-1)*100::DECIMAL/dept_count)+1 AS deptrank_percentage
        , FLOOR((grade_rank_reverse::DECIMAL-1)*100::DECIMAL/grade_count) AS graderank_pr
        , FLOOR((class_rank_reverse::DECIMAL-1)*100::DECIMAL/class_count) AS classrank_pr
        , FLOOR((dept_rank_reverse::DECIMAL-1)*100::DECIMAL/dept_count) AS deptrank_pr
	FROM 
		calc_avg_rank_row AS s1



    --4.3 總計成績 平均 類別1排名
), calc_avg_score_tag1 AS (------算數平均排名所需成績
	SELECT
		student_id
		, student_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, rank_tag1
		, rank_tag2
		, AVG( score::decimal ) AS score
	FROM 
		exam_score_tag1
	WHERE
		score IS NOT NULL
		AND credit IS NOT NULL
	    AND subject IN (
            SELECT subject
            FROM calc_subject_tag1
        )
	GROUP BY 
		student_id
        , student_name
        , rank_school_year
        , rank_semester
        , rank_grade_year
        , rank_class_name
        , exam_id
        , rank_tag1
        , rank_tag2
), calc_avg_rank_row_tag1 AS (-----------計算平均排名
	SELECT 
		student_id
		, rank_tag1
		, rank_tag2
		, '定期評量/總計成績'::text AS item_type
		, '平均'::TEXT As item_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, score
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag1 ORDER BY score DESC) AS tag1_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag1 ORDER BY score ASC) AS tag1_rank_reverse
		, COUNT (*) OVER(PARTITION BY rank_grade_year, rank_tag1) AS tag1_count
		, ROW_NUMBER() OVER(PARTITION BY rank_grade_year, rank_tag1 ORDER BY score ASC) AS tag1_row_number
	FROM 
		calc_avg_score_tag1 AS calc_avg_score
), calc_avg_rank_tag1 AS (-----------計算平均排名百分比及PR
	SELECT  
		s1.*
		, FLOOR((tag1_rank::DECIMAL-1)*100::DECIMAL/tag1_count)+1 AS tag1rank_percentage
        , FLOOR((tag1_rank_reverse::DECIMAL-1)*100::DECIMAL/tag1_count) AS tag1rank_pr
	FROM 
		calc_avg_rank_row_tag1 AS s1



    --4.4 總計成績 平均 類別2排名
), calc_avg_score_tag2 AS (------算數平均排名所需成績
	SELECT
		student_id
		, student_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, rank_tag1
		, rank_tag2
		, AVG( score::decimal ) AS score
	FROM 
		exam_score_tag2
	WHERE
		score IS NOT NULL
		AND credit IS NOT NULL
	    AND subject IN (
            SELECT subject
            FROM calc_subject_tag2
        )
	GROUP BY 
		student_id
        , student_name
        , rank_school_year
        , rank_semester
        , rank_grade_year
        , rank_class_name
        , exam_id
        , rank_tag1
        , rank_tag2
), calc_avg_rank_row_tag2 AS (-----------計算平均排名
	SELECT 
		student_id
		, rank_tag1
		, rank_tag2
		, '定期評量/總計成績'::text AS item_type
		, '平均'::TEXT As item_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, score
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag2 ORDER BY score DESC) AS tag2_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag2 ORDER BY score ASC) AS tag2_rank_reverse
		, COUNT (*) OVER(PARTITION BY rank_grade_year, rank_tag2) AS tag2_count
		, ROW_NUMBER() OVER(PARTITION BY rank_grade_year, rank_tag2 ORDER BY score ASC) AS tag2_row_number
	FROM 
		calc_avg_score_tag2 AS calc_avg_score
), calc_avg_rank_tag2 AS (-----------計算平均排名百分比及PR
	SELECT  
		s1.*
		, FLOOR((tag2_rank::DECIMAL-1)*100::DECIMAL/tag2_count)+1 AS tag2rank_percentage
        , FLOOR((tag2_rank_reverse::DECIMAL-1)*100::DECIMAL/tag2_count) AS tag2rank_pr
	FROM 
		calc_avg_rank_row_tag2 AS s1




    --5.1 總計成績 加權總分 年排名
    --5.2 總計成績 加權總分 班排名
    --5.5 總計成績 加權總分 科排名
), weight_sum_score AS (------加權總分排名所需成績
	SELECT
		student_id
		, student_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, rank_dept_name
		, exam_id
		, rank_tag1
		, rank_tag2
		, SUM( score::decimal * credit::decimal ) AS score
	FROM 
		exam_score
	WHERE
		score IS NOT NULL
		AND credit IS NOT NULL
	GROUP BY 
		student_id
        , student_name
        , rank_school_year
        , rank_semester
        , rank_grade_year
        , rank_class_name
		, rank_dept_name
        , exam_id
        , rank_tag1
        , rank_tag2
), weight_sum_rank_row AS (-----------計算加權總分排名
	SELECT 
		student_id
		, rank_tag1
		, rank_tag2
		, '定期評量/總計成績'::text AS item_type
		, '加權總分'::TEXT As item_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, rank_dept_name
		, exam_id
		, score
		, RANK() OVER(PARTITION BY rank_grade_year ORDER BY score DESC) AS grade_rank
		, RANK() OVER(PARTITION BY rank_class_name ORDER BY score DESC) AS class_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name ORDER BY score DESC) AS dept_rank
		, RANK() OVER(PARTITION BY rank_grade_year ORDER BY score ASC) AS grade_rank_reverse
		, RANK() OVER(PARTITION BY rank_class_name ORDER BY score ASC) AS class_rank_reverse
		, RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name ORDER BY score ASC) AS dept_rank_reverse
		, COUNT (*) OVER(PARTITION BY rank_grade_year) AS grade_count
		, COUNT (*) OVER(PARTITION BY rank_class_name) AS class_count
		, COUNT (student_id) OVER(PARTITION BY rank_grade_year, rank_dept_name) AS dept_count
		, ROW_NUMBER() OVER(PARTITION BY rank_grade_year ORDER BY score ASC) AS grade_row_number
	    , ROW_NUMBER() OVER(PARTITION BY rank_class_name ORDER BY score ASC) AS class_row_number
	    , ROW_NUMBER() OVER(PARTITION BY rank_grade_year, rank_dept_name ORDER BY score ASC) AS dept_row_number
	FROM 
		weight_sum_score
), weight_sum_rank AS (-----------計算加權總分排名百分比及PR
	SELECT  
		s1.*
		, FLOOR((grade_rank::DECIMAL-1)*100::DECIMAL/grade_count)+1 AS graderank_percentage
		, FLOOR((class_rank::DECIMAL-1)*100::DECIMAL/class_count)+1 AS classrank_percentage
		, FLOOR((dept_rank::DECIMAL-1)*100::DECIMAL/dept_count)+1 AS deptrank_percentage
        , FLOOR((grade_rank_reverse::DECIMAL-1)*100::DECIMAL/grade_count) AS graderank_pr
        , FLOOR((class_rank_reverse::DECIMAL-1)*100::DECIMAL/class_count) AS classrank_pr
        , FLOOR((dept_rank_reverse::DECIMAL-1)*100::DECIMAL/dept_count) AS deptrank_pr
	FROM 
		weight_sum_rank_row AS s1




    --5.3 總計成績 加權總分 類別1排名
), weight_sum_score_tag1 AS (------加權總分排名所需成績
	SELECT
		student_id
		, student_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, rank_tag1
		, rank_tag2
		, SUM( score::decimal * credit::decimal ) AS score
	FROM 
		exam_score_tag1
	WHERE
		score IS NOT NULL
		AND credit IS NOT NULL
	    AND subject IN (
            SELECT subject
            FROM calc_subject_tag1
        )
	GROUP BY 
		student_id
        , student_name
        , rank_school_year
        , rank_semester
        , rank_grade_year
        , rank_class_name
        , exam_id
        , rank_tag1
        , rank_tag2
), weight_sum_rank_row_tag1 AS (-----------計算加權總分排名
	SELECT 
		student_id
		, rank_tag1
		, rank_tag2
		, '定期評量/總計成績'::text AS item_type
		, '加權總分'::TEXT As item_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, score
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag1 ORDER BY score DESC) AS tag1_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag1 ORDER BY score ASC) AS tag1_rank_reverse
		, COUNT (*) OVER(PARTITION BY rank_grade_year, rank_tag1) AS tag1_count
		, ROW_NUMBER() OVER(PARTITION BY rank_grade_year, rank_tag1 ORDER BY score ASC) AS tag1_row_number
	FROM 
		weight_sum_score_tag1 AS weight_sum_score
), weight_sum_rank_tag1 AS (-----------計算加權總分排名百分比及PR
	SELECT  
		s1.*
		, FLOOR((tag1_rank::DECIMAL-1)*100::DECIMAL/tag1_count)+1 AS tag1rank_percentage
        , FLOOR((tag1_rank_reverse::DECIMAL-1)*100::DECIMAL/tag1_count) AS tag1rank_pr
	FROM 
		weight_sum_rank_row_tag1 AS s1




    --5.4 總計成績 加權總分 類別2排名
), weight_sum_score_tag2 AS (------加權總分排名所需成績
	SELECT
		student_id
		, student_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, rank_tag1
		, rank_tag2
		, SUM( score::decimal * credit::decimal ) AS score
	FROM 
		exam_score_tag2
	WHERE
		score IS NOT NULL
		AND credit IS NOT NULL
	    AND subject IN (
            SELECT subject
            FROM calc_subject_tag2
        )
	GROUP BY 
		student_id
        , student_name
        , rank_school_year
        , rank_semester
        , rank_grade_year
        , rank_class_name
        , exam_id
        , rank_tag1
        , rank_tag2
), weight_sum_rank_row_tag2 AS (-----------計算加權總分排名
	SELECT 
		student_id
		, rank_tag1
		, rank_tag2
		, '定期評量/總計成績'::text AS item_type
		, '加權總分'::TEXT As item_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, score
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag2 ORDER BY score DESC) AS tag2_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag2 ORDER BY score ASC) AS tag2_rank_reverse
		, COUNT (*) OVER(PARTITION BY rank_grade_year, rank_tag2) AS tag2_count
		, ROW_NUMBER() OVER(PARTITION BY rank_grade_year, rank_tag2 ORDER BY score ASC) AS tag2_row_number
	FROM 
		weight_sum_score_tag2 AS weight_sum_score
), weight_sum_rank_tag2 AS (-----------計算加權總分排名百分比及PR
	SELECT  
		s1.*
		, FLOOR((tag2_rank::DECIMAL-1)*100::DECIMAL/tag2_count)+1 AS tag2rank_percentage
        , FLOOR((tag2_rank_reverse::DECIMAL-1)*100::DECIMAL/tag2_count) AS tag2rank_pr
	FROM 
		weight_sum_rank_row_tag2 AS s1




    --6.1 總計成績 加權平均 年排名
    --6.2 總計成績 加權平均 班排名
    --6.5 總計成績 加權平均 科排名
), weight_avg_score AS (------加權平均排名所需成績
	SELECT
		student_id
		, student_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, rank_dept_name
		, exam_id
		, rank_tag1
		, rank_tag2
		, CASE 
            WHEN SUM(credit::DECIMAL) IS NULL THEN NULL::DECIMAL
            WHEN SUM(credit::DECIMAL) = 0 THEN 0::DECIMAL
            ELSE SUM(score::DECIMAL * credit::DECIMAL) / SUM(credit::DECIMAL)
            END AS score
	FROM 
		exam_score
	WHERE
		score IS NOT NULL
		AND credit IS NOT NULL
	GROUP BY 
		student_id
        , student_name
        , rank_school_year
        , rank_semester
        , rank_grade_year
        , rank_class_name
		, rank_dept_name
        , exam_id
        , rank_tag1
        , rank_tag2
), weight_avg_rank_row AS (-----------計算加權平均排名
	SELECT 
		student_id
		, rank_tag1
		, rank_tag2
		, '定期評量/總計成績'::text AS item_type
		, '加權平均'::TEXT As item_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, rank_dept_name
		, exam_id
		, score
		, RANK() OVER(PARTITION BY rank_grade_year ORDER BY score DESC) AS grade_rank
		, RANK() OVER(PARTITION BY rank_class_name ORDER BY score DESC) AS class_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name ORDER BY score DESC) AS dept_rank
		, RANK() OVER(PARTITION BY rank_grade_year ORDER BY score ASC) AS grade_rank_reverse
		, RANK() OVER(PARTITION BY rank_class_name ORDER BY score ASC) AS class_rank_reverse
		, RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name ORDER BY score ASC) AS dept_rank_reverse
		, COUNT (*) OVER(PARTITION BY rank_grade_year) AS grade_count
		, COUNT (*) OVER(PARTITION BY rank_class_name) AS class_count
		, COUNT (student_id) OVER(PARTITION BY rank_grade_year, rank_dept_name) AS dept_count
		, ROW_NUMBER() OVER(PARTITION BY rank_grade_year ORDER BY score ASC) AS grade_row_number
	    , ROW_NUMBER() OVER(PARTITION BY rank_class_name ORDER BY score ASC) AS class_row_number
	    , ROW_NUMBER() OVER(PARTITION BY rank_grade_year, rank_dept_name ORDER BY score ASC) AS dept_row_number
	FROM 
		weight_avg_score
), weight_avg_rank AS (-----------計算加權平均排名百分比及PR
	SELECT  
		s1.*
		, FLOOR((grade_rank::DECIMAL-1)*100::DECIMAL/grade_count)+1 AS graderank_percentage
		, FLOOR((class_rank::DECIMAL-1)*100::DECIMAL/class_count)+1 AS classrank_percentage
		, FLOOR((dept_rank::DECIMAL-1)*100::DECIMAL/dept_count)+1 AS deptrank_percentage
        , FLOOR((grade_rank_reverse::DECIMAL-1)*100::DECIMAL/grade_count) AS graderank_pr
        , FLOOR((class_rank_reverse::DECIMAL-1)*100::DECIMAL/class_count) AS classrank_pr
        , FLOOR((dept_rank_reverse::DECIMAL-1)*100::DECIMAL/dept_count) AS deptrank_pr
	FROM 
		weight_avg_rank_row AS s1




    --6.3 總計成績 加權平均 類別1排名
), weight_avg_score_tag1 AS (------加權平均排名所需成績
	SELECT
		student_id
		, student_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, rank_tag1
		, rank_tag2
		, CASE 
            WHEN SUM(credit::DECIMAL) IS NULL THEN NULL::DECIMAL
            WHEN SUM(credit::DECIMAL) = 0 THEN 0::DECIMAL
            ELSE SUM(score::DECIMAL * credit::DECIMAL) / SUM(credit::DECIMAL)
            END AS score
	FROM 
		exam_score_tag1
	WHERE
		score IS NOT NULL
		AND credit IS NOT NULL
	    AND subject IN (
            SELECT subject
            FROM calc_subject_tag1
        )
	GROUP BY 
		student_id
        , student_name
        , rank_school_year
        , rank_semester
        , rank_grade_year
        , rank_class_name
        , exam_id
        , rank_tag1
        , rank_tag2
), weight_avg_rank_row_tag1 AS (-----------計算加權平均排名
	SELECT 
		student_id
		, rank_tag1
		, rank_tag2
		, '定期評量/總計成績'::text AS item_type
		, '加權平均'::TEXT As item_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, score
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag1 ORDER BY score DESC) AS tag1_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag1 ORDER BY score ASC) AS tag1_rank_reverse
		, COUNT (*) OVER(PARTITION BY rank_grade_year, rank_tag1) AS tag1_count
		, ROW_NUMBER() OVER(PARTITION BY rank_grade_year, rank_tag1 ORDER BY score ASC) AS tag1_row_number
	FROM 
		weight_avg_score_tag1 AS weight_avg_score
), weight_avg_rank_tag1 AS (-----------計算加權平均排名百分比及PR
	SELECT  
		s1.*
		, FLOOR((tag1_rank::DECIMAL-1)*100::DECIMAL/tag1_count)+1 AS tag1rank_percentage
        , FLOOR((tag1_rank_reverse::DECIMAL-1)*100::DECIMAL/tag1_count) AS tag1rank_pr
	FROM 
		weight_avg_rank_row_tag1 AS s1




    --6.4 總計成績 加權平均 類別2排名
), weight_avg_score_tag2 AS (------加權平均排名所需成績
	SELECT
		student_id
		, student_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, rank_tag1
		, rank_tag2
		, CASE 
            WHEN SUM(credit::DECIMAL) IS NULL THEN NULL::DECIMAL
            WHEN SUM(credit::DECIMAL) = 0 THEN 0::DECIMAL
            ELSE SUM(score::DECIMAL * credit::DECIMAL) / SUM(credit::DECIMAL)
            END AS score
	FROM 
		exam_score_tag2
	WHERE
		score IS NOT NULL
		AND credit IS NOT NULL
	    AND subject IN (
            SELECT subject
            FROM calc_subject_tag2
        )
	GROUP BY 
		student_id
        , student_name
        , rank_school_year
        , rank_semester
        , rank_grade_year
        , rank_class_name
        , exam_id
        , rank_tag1
        , rank_tag2
), weight_avg_rank_row_tag2 AS (-----------計算加權平均排名
	SELECT 
		student_id
		, rank_tag1
		, rank_tag2
		, '定期評量/總計成績'::text AS item_type
		, '加權平均'::TEXT As item_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, score
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag2 ORDER BY score DESC) AS tag2_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag2 ORDER BY score ASC) AS tag2_rank_reverse
		, COUNT (*) OVER(PARTITION BY rank_grade_year, rank_tag2) AS tag2_count
		, ROW_NUMBER() OVER(PARTITION BY rank_grade_year, rank_tag2 ORDER BY score ASC) AS tag2_row_number
	FROM 
		weight_avg_score_tag2 AS weight_avg_score
), weight_avg_rank_tag2 AS (-----------計算加權平均排名百分比及PR
	SELECT  
		s1.*
		, FLOOR((tag2_rank::DECIMAL-1)*100::DECIMAL/tag2_count)+1 AS tag2rank_percentage
        , FLOOR((tag2_rank_reverse::DECIMAL-1)*100::DECIMAL/tag2_count) AS tag2rank_pr
	FROM 
		weight_avg_rank_row_tag2 AS s1
), score_list AS (

    --X.1 年排名
	SELECT
		rank_school_year 
		, rank_semester
		, rank_grade_year
		, item_type
		, exam_id AS ref_exam_id
		, item_name
		, '年排名'::TEXT AS rank_type
		, '' || rank_grade_year || '年級'::TEXT AS rank_name
		, true AS is_alive
		, grade_count AS matrix_count
		, STDDEV_POP(score) OVER(PARTITION BY rank_grade_year, item_type, item_name) AS std_dev_pop
	    , CASE WHEN (grade_count*88/100::DECIMAL % 1 <> 0) 
	        THEN AVG(score::DECIMAL)FILTER(WHERE grade_count*88/100+1=grade_row_number) OVER(PARTITION BY rank_grade_year, item_type, item_name)::DECIMAL 
	        ELSE AVG(score::DECIMAL)FILTER(WHERE grade_count*88/100=grade_row_number OR grade_count*88/100+1 = grade_row_number) OVER(PARTITION BY rank_grade_year, item_type, item_name)::DECIMAL 
	      END pr_88
	    , CASE WHEN (grade_count*75/100::DECIMAL % 1 <> 0) 
	        THEN AVG(score::DECIMAL)FILTER(WHERE grade_count*75/100+1=grade_row_number) OVER(PARTITION BY rank_grade_year, item_type, item_name)::DECIMAL 
	        ELSE AVG(score::DECIMAL)FILTER(WHERE grade_count*75/100=grade_row_number OR grade_count*75/100+1 = grade_row_number) OVER(PARTITION BY rank_grade_year, item_type, item_name)::DECIMAL 
	      END pr_75
	    , CASE WHEN (grade_count*50/100::DECIMAL % 1 <> 0) 
	        THEN AVG(score::DECIMAL)FILTER(WHERE grade_count*50/100+1=grade_row_number) OVER(PARTITION BY rank_grade_year, item_type, item_name)::DECIMAL 
	        ELSE AVG(score::DECIMAL)FILTER(WHERE grade_count*50/100=grade_row_number OR grade_count*50/100+1 = grade_row_number) OVER(PARTITION BY rank_grade_year, item_type, item_name)::DECIMAL 
	      END pr_50
	    , CASE WHEN (grade_count*25/100::DECIMAL % 1 <> 0) 
	        THEN AVG(score::DECIMAL)FILTER(WHERE grade_count*25/100+1=grade_row_number) OVER(PARTITION BY rank_grade_year, item_type, item_name)::DECIMAL 
	        ELSE AVG(score::DECIMAL)FILTER(WHERE grade_count*25/100=grade_row_number OR grade_count*25/100+1 = grade_row_number) OVER(PARTITION BY rank_grade_year, item_type, item_name)::DECIMAL 
	      END pr_25
	    , CASE WHEN (grade_count*12/100::DECIMAL % 1 <> 0) 
	        THEN AVG(score::DECIMAL)FILTER(WHERE grade_count*12/100+1=grade_row_number) OVER(PARTITION BY rank_grade_year, item_type, item_name)::DECIMAL 
	        ELSE AVG(score::DECIMAL)FILTER(WHERE grade_count*12/100=grade_row_number OR grade_count*12/100+1 = grade_row_number) OVER(PARTITION BY rank_grade_year, item_type, item_name)::DECIMAL 
	      END pr_12
		, AVG(Score::DECIMAL)FILTER(WHERE grade_rank * 4 <= grade_count) OVER(PARTITION BY rank_grade_year, item_type, item_name) AS avg_top_25
		, AVG(Score::DECIMAL)FILTER(WHERE grade_rank * 2 <= grade_count) OVER(PARTITION BY rank_grade_year, item_type, item_name) AS avg_top_50
		, AVG(Score::DECIMAL)OVER(PARTITION BY rank_grade_year, item_type, item_name) AS avg
		, AVG(Score::DECIMAL)FILTER(WHERE ( grade_count - grade_rank + 1 ) * 2 <= grade_count) OVER(PARTITION BY rank_grade_year, item_type, item_name) AS avg_bottom_50
		, AVG(Score::DECIMAL)FILTER(WHERE ( grade_count - grade_rank + 1 ) * 4 <= grade_count) OVER(PARTITION BY rank_grade_year, item_type, item_name) AS avg_bottom_25
		, COUNT(*) FILTER (WHERE 100::DECIMAL<=score::DECIMAL ) OVER(PARTITION BY rank_grade_year, item_type, item_name)AS level_gte100 
		, COUNT(*) FILTER (WHERE 90::DECIMAL<=score AND score <100::DECIMAL)  OVER(PARTITION BY rank_grade_year, item_type, item_name) AS level_90
		, COUNT(*) FILTER (WHERE 80::DECIMAL<=score AND score <90::DECIMAL)  OVER(PARTITION BY rank_grade_year, item_type, item_name) AS level_80
		, COUNT(*) FILTER (WHERE 70::DECIMAL<=score AND  score <80::DECIMAL)  OVER(PARTITION BY rank_grade_year, item_type, item_name)AS level_70
		, COUNT(*) FILTER (WHERE 60::DECIMAL<=score AND score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, item_type, item_name) AS level_60
		, COUNT(*) FILTER (WHERE 50::DECIMAL<=score AND score <60::DECIMAL)  OVER (PARTITION BY rank_grade_year, item_type, item_name)AS level_50
		, COUNT(*) FILTER (WHERE 40::DECIMAL<=score AND score <50::DECIMAL) OVER (PARTITION BY rank_grade_year, item_type, item_name)AS level_40
		, COUNT(*) FILTER (WHERE 30::DECIMAL<=score AND score <40::DECIMAL)   OVER (PARTITION BY rank_grade_year, item_type, item_name)AS level_30
		, COUNT(*) FILTER (WHERE 20::DECIMAL<=score AND score <30::DECIMAL)  OVER (PARTITION BY rank_grade_year, item_type, item_name) AS level_20
		, COUNT(*) FILTER (WHERE 10::DECIMAL<=score AND score <20::DECIMAL)   OVER (PARTITION BY rank_grade_year, item_type, item_name)AS level_10
		, COUNT(*) FILTER (WHERE score<10::DECIMAL) OVER (PARTITION BY rank_grade_year, item_type, item_name)AS level_lt10
		, student_id
		, score
		, grade_rank AS rank
		, graderank_percentage AS percentile
		, graderank_pr AS pr
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		(
 		    SELECT * FROM domain_rank--1.1 領域成績 年排名
            UNION ALL
            SELECT * FROM subject_rank--2.1 科目成績 年排名
            UNION ALL
            SELECT * FROM calc_sum_rank--3.1 總計成績 總分 年排名
            UNION ALL
            SELECT * FROM calc_avg_rank--4.1 總計成績 平均 年排名
            UNION ALL
            SELECT * FROM weight_sum_rank--5.1 總計成績 加權總分 年排名
            UNION ALL
            SELECT * FROM weight_avg_rank--6.1 總計成績 加權平均 年排名
        ) AS data
	UNION ALL
    --X.2 班排名
	SELECT
		rank_school_year 
		, rank_semester
		, rank_grade_year
		, item_type
		, exam_id AS ref_exam_id
		, item_name
		, '班排名'::TEXT AS rank_type
		, rank_class_name AS rank_name
		, true AS is_alive
		, class_count AS matrix_count
		, STDDEV_POP(score) OVER(PARTITION BY rank_class_name, item_type, item_name) AS std_dev_pop
	    , CASE WHEN (class_count*88/100::DECIMAL % 1 <> 0) 
	        THEN AVG(score::DECIMAL)FILTER(WHERE class_count*88/100+1=class_row_number) OVER(PARTITION BY rank_class_name, item_type, item_name)::DECIMAL 
	        ELSE AVG(score::DECIMAL)FILTER(WHERE class_count*88/100=class_row_number OR class_count*88/100+1 = class_row_number) OVER(PARTITION BY rank_class_name, item_type, item_name)::DECIMAL 
	      END pr_88
	    , CASE WHEN (class_count*75/100::DECIMAL % 1 <> 0) 
	        THEN AVG(score::DECIMAL)FILTER(WHERE class_count*75/100+1=class_row_number) OVER(PARTITION BY rank_class_name, item_type, item_name)::DECIMAL 
	        ELSE AVG(score::DECIMAL)FILTER(WHERE class_count*75/100=class_row_number OR class_count*75/100+1 = class_row_number) OVER(PARTITION BY rank_class_name, item_type, item_name)::DECIMAL 
	      END pr_75
	    , CASE WHEN (class_count*50/100::DECIMAL % 1 <> 0) 
	        THEN AVG(score::DECIMAL)FILTER(WHERE class_count*50/100+1=class_row_number) OVER(PARTITION BY rank_class_name, item_type, item_name)::DECIMAL 
	        ELSE AVG(score::DECIMAL)FILTER(WHERE class_count*50/100=class_row_number OR class_count*50/100+1 = class_row_number) OVER(PARTITION BY rank_class_name, item_type, item_name)::DECIMAL 
	      END pr_50
	    , CASE WHEN (class_count*25/100::DECIMAL % 1 <> 0) 
	        THEN AVG(score::DECIMAL)FILTER(WHERE class_count*25/100+1=class_row_number) OVER(PARTITION BY rank_class_name, item_type, item_name)::DECIMAL 
	        ELSE AVG(score::DECIMAL)FILTER(WHERE class_count*25/100=class_row_number OR class_count*25/100+1 = class_row_number) OVER(PARTITION BY rank_class_name, item_type, item_name)::DECIMAL 
	      END pr_25
	    , CASE WHEN (class_count*12/100::DECIMAL % 1 <> 0) 
	        THEN AVG(score::DECIMAL)FILTER(WHERE class_count*12/100+1=class_row_number) OVER(PARTITION BY rank_class_name, item_type, item_name)::DECIMAL 
	        ELSE AVG(score::DECIMAL)FILTER(WHERE class_count*12/100=class_row_number OR class_count*12/100+1 = class_row_number) OVER(PARTITION BY rank_class_name, item_type, item_name)::DECIMAL 
	      END pr_12
		, AVG(Score::DECIMAL)FILTER(WHERE class_rank * 4 <= class_count) OVER(PARTITION BY rank_class_name, item_type, item_name) AS avg_top_25
		, AVG(Score::DECIMAL)FILTER(WHERE class_rank * 2 <= class_count) OVER(PARTITION BY rank_class_name, item_type, item_name) AS avg_top_50
		, AVG(Score::DECIMAL)OVER(PARTITION BY rank_class_name, item_type, item_name) AS avg
		, AVG(Score::DECIMAL)FILTER(WHERE ( class_count - class_rank + 1 ) * 2 <= class_count) OVER(PARTITION BY rank_class_name, item_type, item_name) AS avg_bottom_50
		, AVG(Score::DECIMAL)FILTER(WHERE ( class_count - class_rank + 1 ) * 4 <= class_count) OVER(PARTITION BY rank_class_name, item_type, item_name) AS avg_bottom_25
		, COUNT(*) FILTER (WHERE 100::DECIMAL<=score::DECIMAL ) OVER(PARTITION BY rank_class_name, item_type, item_name)AS level_gte100 
		, COUNT(*) FILTER (WHERE 90::DECIMAL<=score AND score <100::DECIMAL)  OVER(PARTITION BY rank_class_name, item_type, item_name) AS level_90
		, COUNT(*) FILTER (WHERE 80::DECIMAL<=score AND score <90::DECIMAL)  OVER(PARTITION BY rank_class_name, item_type, item_name) AS level_80
		, COUNT(*) FILTER (WHERE 70::DECIMAL<=score AND score <80::DECIMAL)  OVER(PARTITION BY rank_class_name, item_type, item_name)AS level_70
		, COUNT(*) FILTER (WHERE 60::DECIMAL<=score AND score <70::DECIMAL) OVER(PARTITION BY rank_class_name, item_type, item_name) AS level_60
		, COUNT(*) FILTER (WHERE 50::DECIMAL<=score AND score <60::DECIMAL)  OVER (PARTITION BY rank_class_name, item_type, item_name)AS level_50
		, COUNT(*) FILTER (WHERE 40::DECIMAL<=score AND score <50::DECIMAL) OVER (PARTITION BY rank_class_name, item_type, item_name)AS level_40
		, COUNT(*) FILTER (WHERE 30::DECIMAL<=score AND score <40::DECIMAL)   OVER (PARTITION BY rank_class_name, item_type, item_name)AS level_30
		, COUNT(*) FILTER (WHERE 20::DECIMAL<=score AND score <30::DECIMAL)  OVER (PARTITION BY rank_class_name, item_type, item_name) AS level_20
		, COUNT(*) FILTER (WHERE 10::DECIMAL<=score AND score <20::DECIMAL)   OVER (PARTITION BY rank_class_name, item_type, item_name)AS level_10
		, COUNT(*) FILTER (WHERE score<10::DECIMAL) OVER (PARTITION BY rank_class_name, item_type, item_name)AS level_lt10 
		, student_id
		, score
		, class_rank AS rank
		, classrank_percentage AS percentile
		, classrank_pr AS pr
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		(
            SELECT * FROM domain_rank--1.2 領域成績 班排名
            UNION ALL
            SELECT * FROM subject_rank--2.2 科目成績 班排名
            UNION ALL
            SELECT * FROM calc_sum_rank--3.2 總計成績 總分 班排名
            UNION ALL
            SELECT * FROM calc_avg_rank--4.2 總計成績 平均 班排名
            UNION ALL
            SELECT * FROM weight_sum_rank--5.2 總計成績 加權總分 班排名
            UNION ALL
            SELECT * FROM weight_avg_rank--6.2 總計成績 加權平均 班排名
        ) AS data
	UNION ALL
    --X.5 科排名
	SELECT
		rank_school_year 
		, rank_semester
		, rank_grade_year
		, item_type
		, exam_id AS ref_exam_id
		, item_name
		, '科排名'::TEXT AS rank_type
		, rank_dept_name AS rank_name
		, true AS is_alive
		, dept_count AS matrix_count
		, STDDEV_POP(score) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_type, item_name) AS std_dev_pop
	    , CASE WHEN (dept_count*88/100::DECIMAL % 1 <> 0) 
	        THEN AVG(score::DECIMAL)FILTER(WHERE dept_count*88/100+1=dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_type, item_name)::DECIMAL 
	        ELSE AVG(score::DECIMAL)FILTER(WHERE dept_count*88/100=dept_row_number OR dept_count*88/100+1 = dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_type, item_name)::DECIMAL 
	      END pr_88
	    , CASE WHEN (dept_count*75/100::DECIMAL % 1 <> 0) 
	        THEN AVG(score::DECIMAL)FILTER(WHERE dept_count*75/100+1=dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_type, item_name)::DECIMAL 
	        ELSE AVG(score::DECIMAL)FILTER(WHERE dept_count*75/100=dept_row_number OR dept_count*75/100+1 = dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_type, item_name)::DECIMAL 
	      END pr_75
	    , CASE WHEN (dept_count*50/100::DECIMAL % 1 <> 0) 
	        THEN AVG(score::DECIMAL)FILTER(WHERE dept_count*50/100+1=dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_type, item_name)::DECIMAL 
	        ELSE AVG(score::DECIMAL)FILTER(WHERE dept_count*50/100=dept_row_number OR dept_count*50/100+1 = dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_type, item_name)::DECIMAL 
	      END pr_50
	    , CASE WHEN (dept_count*25/100::DECIMAL % 1 <> 0) 
	        THEN AVG(score::DECIMAL)FILTER(WHERE dept_count*25/100+1=dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_type, item_name)::DECIMAL 
	        ELSE AVG(score::DECIMAL)FILTER(WHERE dept_count*25/100=dept_row_number OR dept_count*25/100+1 = dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_type, item_name)::DECIMAL 
	      END pr_25
	    , CASE WHEN (dept_count*12/100::DECIMAL % 1 <> 0) 
	        THEN AVG(score::DECIMAL)FILTER(WHERE dept_count*12/100+1=dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_type, item_name)::DECIMAL 
	        ELSE AVG(score::DECIMAL)FILTER(WHERE dept_count*12/100=dept_row_number OR dept_count*12/100+1 = dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_type, item_name)::DECIMAL 
	      END pr_12
		, AVG(score::DECIMAL)FILTER(WHERE dept_rank * 4 <= dept_count) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_type, item_name) AS avg_top_25
		, AVG(score::DECIMAL)FILTER(WHERE dept_rank * 2 <= dept_count) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_type, item_name) AS avg_top_50
		, AVG(score::DECIMAL)OVER(PARTITION BY rank_grade_year, rank_dept_name, item_type, item_name) AS avg
		, AVG(score::DECIMAL)FILTER(WHERE ( dept_count - dept_rank + 1 ) * 2 <= dept_count) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_type, item_name) AS avg_bottom_50
		, AVG(score::DECIMAL)FILTER(WHERE ( dept_count - dept_rank + 1 ) * 4 <= dept_count) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_type, item_name) AS avg_bottom_25
		, COUNT(*) FILTER (WHERE 100::DECIMAL<=score::DECIMAL ) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_type, item_name)AS level_gte100 
		, COUNT(*) FILTER (WHERE 90::DECIMAL<=score AND score <100::DECIMAL)  OVER(PARTITION BY rank_grade_year, rank_dept_name, item_type, item_name) AS level_90
		, COUNT(*) FILTER (WHERE 80::DECIMAL<=score AND score <90::DECIMAL)  OVER(PARTITION BY rank_grade_year, rank_dept_name, item_type, item_name) AS level_80
		, COUNT(*) FILTER (WHERE 70::DECIMAL<=score AND score <80::DECIMAL)  OVER(PARTITION BY rank_grade_year, rank_dept_name, item_type, item_name)AS level_70
		, COUNT(*) FILTER (WHERE 60::DECIMAL<=score AND score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_type, item_name) AS level_60
		, COUNT(*) FILTER (WHERE 50::DECIMAL<=score AND score <60::DECIMAL)  OVER (PARTITION BY rank_grade_year, rank_dept_name, item_type, item_name)AS level_50
		, COUNT(*) FILTER (WHERE 40::DECIMAL<=score AND score <50::DECIMAL) OVER (PARTITION BY rank_grade_year, rank_dept_name, item_type, item_name)AS level_40
		, COUNT(*) FILTER (WHERE 30::DECIMAL<=score AND score <40::DECIMAL)   OVER (PARTITION BY rank_grade_year, rank_dept_name, item_type, item_name)AS level_30
		, COUNT(*) FILTER (WHERE 20::DECIMAL<=score AND score <30::DECIMAL)  OVER (PARTITION BY rank_grade_year, rank_dept_name, item_type, item_name) AS level_20
		, COUNT(*) FILTER (WHERE 10::DECIMAL<=score AND score <20::DECIMAL)   OVER (PARTITION BY rank_grade_year, rank_dept_name, item_type, item_name)AS level_10
		, COUNT(*) FILTER (WHERE score<10::DECIMAL) OVER (PARTITION BY rank_grade_year, rank_dept_name, item_type, item_name)AS level_lt10 
		, student_id
		, score
		, dept_rank AS rank
		, deptrank_percentage AS percentile
		, deptrank_pr AS pr
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		(
            SELECT * FROM domain_rank--1.5 領域成績 科排名
            UNION ALL
            SELECT * FROM subject_rank--2.5 科目成績 科排名
            UNION ALL
            SELECT * FROM calc_sum_rank--3.5 總計成績 總分 科排名
            UNION ALL
            SELECT * FROM calc_avg_rank--4.5 總計成績 平均 科排名
            UNION ALL
            SELECT * FROM weight_sum_rank--5.5 總計成績 加權總分 科排名
            UNION ALL
            SELECT * FROM weight_avg_rank--6.5 總計成績 加權平均 科排名
        ) AS data
	UNION ALL
    --X.3 類別1排名
    SELECT
		rank_school_year 
		, rank_semester
		, rank_grade_year
		, item_type
		, exam_id AS ref_exam_id
		, item_name
		, '類別1排名'::TEXT AS rank_type
		, rank_tag1 AS rank_name
		, true AS is_alive
		, tag1_count AS matrix_count
		, STDDEV_POP(score) OVER(PARTITION BY rank_grade_year, rank_tag1, item_type, item_name) AS std_dev_pop
	    , CASE WHEN (tag1_count*88/100::DECIMAL % 1 <> 0) 
	        THEN AVG(score::DECIMAL)FILTER(WHERE tag1_count*88/100+1=tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_type, item_name)::DECIMAL 
	        ELSE AVG(score::DECIMAL)FILTER(WHERE tag1_count*88/100=tag1_row_number OR tag1_count*88/100+1 = tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_type, item_name)::DECIMAL 
	      END pr_88
	    , CASE WHEN (tag1_count*75/100::DECIMAL % 1 <> 0) 
	        THEN AVG(score::DECIMAL)FILTER(WHERE tag1_count*75/100+1=tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_type, item_name)::DECIMAL 
	        ELSE AVG(score::DECIMAL)FILTER(WHERE tag1_count*75/100=tag1_row_number OR tag1_count*75/100+1 = tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_type, item_name)::DECIMAL 
	      END pr_75
	    , CASE WHEN (tag1_count*50/100::DECIMAL % 1 <> 0) 
	        THEN AVG(score::DECIMAL)FILTER(WHERE tag1_count*50/100+1=tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_type, item_name)::DECIMAL 
	        ELSE AVG(score::DECIMAL)FILTER(WHERE tag1_count*50/100=tag1_row_number OR tag1_count*50/100+1 = tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_type, item_name)::DECIMAL 
	      END pr_50
	    , CASE WHEN (tag1_count*25/100::DECIMAL % 1 <> 0) 
	        THEN AVG(score::DECIMAL)FILTER(WHERE tag1_count*25/100+1=tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_type, item_name)::DECIMAL 
	        ELSE AVG(score::DECIMAL)FILTER(WHERE tag1_count*25/100=tag1_row_number OR tag1_count*25/100+1 = tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_type, item_name)::DECIMAL 
	      END pr_25
	    , CASE WHEN (tag1_count*12/100::DECIMAL % 1 <> 0) 
	        THEN AVG(score::DECIMAL)FILTER(WHERE tag1_count*12/100+1=tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_type, item_name)::DECIMAL 
	        ELSE AVG(score::DECIMAL)FILTER(WHERE tag1_count*12/100=tag1_row_number OR tag1_count*12/100+1 = tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_type, item_name)::DECIMAL 
	      END pr_12
		, AVG(Score::DECIMAL)FILTER(WHERE tag1_rank * 4 <= tag1_count) OVER(PARTITION BY rank_grade_year, rank_tag1, item_type, item_name) AS avg_top_25
		, AVG(Score::DECIMAL)FILTER(WHERE tag1_rank * 2 <= tag1_count) OVER(PARTITION BY rank_grade_year, rank_tag1, item_type, item_name) AS avg_top_50
		, AVG(Score::DECIMAL)OVER(PARTITION BY rank_grade_year, rank_tag1, item_type, item_name) AS avg
		, AVG(Score::DECIMAL)FILTER(WHERE ( tag1_count - tag1_rank + 1 ) * 2 <= tag1_count) OVER(PARTITION BY rank_grade_year, rank_tag1, item_type, item_name) AS avg_bottom_50
		, AVG(Score::DECIMAL)FILTER(WHERE ( tag1_count - tag1_rank + 1 ) * 4 <= tag1_count) OVER(PARTITION BY rank_grade_year, rank_tag1, item_type, item_name) AS avg_bottom_25
		, COUNT(*) FILTER (WHERE 100::DECIMAL<=score::DECIMAL ) OVER(PARTITION BY rank_grade_year, rank_tag1, item_type, item_name)AS level_gte100 
		, COUNT(*) FILTER (WHERE 90::DECIMAL<=score AND score <100::DECIMAL)OVER(PARTITION BY rank_grade_year, rank_tag1, item_type, item_name) AS level_90
		, COUNT(*) FILTER (WHERE 80::DECIMAL<=score AND score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_type, item_name) AS level_80
		, COUNT(*) FILTER (WHERE 70::DECIMAL<=score AND score <80::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_type, item_name)AS level_70
		, COUNT(*) FILTER (WHERE 60::DECIMAL<=score AND score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_type, item_name) AS level_60
		, COUNT(*) FILTER (WHERE 50::DECIMAL<=score AND score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_type, item_name)AS level_50
		, COUNT(*) FILTER (WHERE 40::DECIMAL<=score AND score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_type, item_name)AS level_40
		, COUNT(*) FILTER (WHERE 30::DECIMAL<=score AND score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_type, item_name)AS level_30
		, COUNT(*) FILTER (WHERE 20::DECIMAL<=score AND score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_type, item_name) AS level_20
		, COUNT(*) FILTER (WHERE 10::DECIMAL<=score AND score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_type, item_name)AS level_10
		, COUNT(*) FILTER (WHERE score<10::DECIMAL) OVER (PARTITION BY rank_grade_year, rank_tag1, item_type, item_name)AS level_lt10
		, student_id
		, score
		, tag1_rank AS rank
		, tag1rank_percentage AS percentile
		, tag1rank_pr AS pr
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		(
            SELECT * FROM domain_rank_tag1--1.3 領域成績 類別1排名
            UNION ALL
            SELECT * FROM subject_rank_tag1--2.3 科目成績 類別1排名
            UNION ALL
            SELECT * FROM calc_sum_rank_tag1--3.3 總計成績 總分 類別1排名
            UNION ALL
            SELECT * FROM calc_avg_rank_tag1--4.3 總計成績 平均 類別1排名
            UNION ALL
            SELECT * FROM weight_sum_rank_tag1--5.3 總計成績 加權總分 類別1排名
            UNION ALL
            SELECT * FROM weight_avg_rank_tag1--6.3 總計成績 加權平均 類別1排名
        ) AS data
	WHERE
		rank_tag1 IS NOT NULL
		AND rank_tag1 <> ''
	UNION ALL
    --X.4 類別2排名
	SELECT
		rank_school_year 
		, rank_semester
		, rank_grade_year
		, item_type
		, exam_id AS ref_exam_id
		, item_name
		, '類別2排名'::TEXT AS rank_type
		, rank_tag2 AS rank_name
		, true AS is_alive
		, tag2_count AS matrix_count
		, STDDEV_POP(score) OVER(PARTITION BY rank_grade_year, rank_tag2, item_type, item_name) AS std_dev_pop
	    , CASE WHEN (tag2_count*88/100::DECIMAL % 1 <> 0) 
	        THEN AVG(score::DECIMAL)FILTER(WHERE tag2_count*88/100+1=tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_type, item_name)::DECIMAL 
	        ELSE AVG(score::DECIMAL)FILTER(WHERE tag2_count*88/100=tag2_row_number OR tag2_count*88/100+1 = tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_type, item_name)::DECIMAL 
	      END pr_88
	    , CASE WHEN (tag2_count*75/100::DECIMAL % 1 <> 0) 
	        THEN AVG(score::DECIMAL)FILTER(WHERE tag2_count*75/100+1=tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_type, item_name)::DECIMAL 
	        ELSE AVG(score::DECIMAL)FILTER(WHERE tag2_count*75/100=tag2_row_number OR tag2_count*75/100+1 = tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_type, item_name)::DECIMAL 
	      END pr_75
	    , CASE WHEN (tag2_count*50/100::DECIMAL % 1 <> 0) 
	        THEN AVG(score::DECIMAL)FILTER(WHERE tag2_count*50/100+1=tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_type, item_name)::DECIMAL 
	        ELSE AVG(score::DECIMAL)FILTER(WHERE tag2_count*50/100=tag2_row_number OR tag2_count*50/100+1 = tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_type, item_name)::DECIMAL 
	      END pr_50
	    , CASE WHEN (tag2_count*25/100::DECIMAL % 1 <> 0) 
	        THEN AVG(score::DECIMAL)FILTER(WHERE tag2_count*25/100+1=tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_type, item_name)::DECIMAL 
	        ELSE AVG(score::DECIMAL)FILTER(WHERE tag2_count*25/100=tag2_row_number OR tag2_count*25/100+1 = tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_type, item_name)::DECIMAL 
	      END pr_25
	    , CASE WHEN (tag2_count*12/100::DECIMAL % 1 <> 0) 
	        THEN AVG(score::DECIMAL)FILTER(WHERE tag2_count*12/100+1=tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_type, item_name)::DECIMAL 
	        ELSE AVG(score::DECIMAL)FILTER(WHERE tag2_count*12/100=tag2_row_number OR tag2_count*12/100+1 = tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_type, item_name)::DECIMAL 
	      END pr_12
		, AVG(Score::DECIMAL)FILTER(WHERE tag2_rank * 4 <= tag2_count) OVER(PARTITION BY rank_grade_year, rank_tag2, item_type, item_name) AS avg_top_25
		, AVG(Score::DECIMAL)FILTER(WHERE tag2_rank * 2 <= tag2_count) OVER(PARTITION BY rank_grade_year, rank_tag2, item_type, item_name) AS avg_top_50
		, AVG(Score::DECIMAL)OVER(PARTITION BY rank_grade_year, rank_tag2, item_type, item_name) AS avg
		, AVG(Score::DECIMAL)FILTER(WHERE ( tag2_count - tag2_rank + 1 ) * 2 <= tag2_count) OVER(PARTITION BY rank_grade_year, rank_tag2, item_type, item_name) AS avg_bottom_50
		, AVG(Score::DECIMAL)FILTER(WHERE ( tag2_count - tag2_rank + 1 ) * 4 <= tag2_count) OVER(PARTITION BY rank_grade_year, rank_tag2, item_type, item_name) AS avg_bottom_25
		, COUNT(*) FILTER (WHERE 100::DECIMAL<=score::DECIMAL ) OVER(PARTITION BY rank_grade_year, rank_tag2, item_type, item_name)AS level_gte100 
		, COUNT(*) FILTER (WHERE 90::DECIMAL<=score AND score <100::DECIMAL)OVER(PARTITION BY rank_grade_year, rank_tag2, item_type, item_name) AS level_90
		, COUNT(*) FILTER (WHERE 80::DECIMAL<=score AND score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_type, item_name) AS level_80
		, COUNT(*) FILTER (WHERE 70::DECIMAL<=score AND score <80::DECIMAL)OVER(PARTITION BY rank_grade_year, rank_tag2, item_type, item_name)AS level_70
		, COUNT(*) FILTER (WHERE 60::DECIMAL<=score AND score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_type, item_name) AS level_60
		, COUNT(*) FILTER (WHERE 50::DECIMAL<=score AND score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_type, item_name)AS level_50
		, COUNT(*) FILTER (WHERE 40::DECIMAL<=score AND score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_type, item_name)AS level_40
		, COUNT(*) FILTER (WHERE 30::DECIMAL<=score AND score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_type, item_name)AS level_30
		, COUNT(*) FILTER (WHERE 20::DECIMAL<=score AND score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_type, item_name) AS level_20
		, COUNT(*) FILTER (WHERE 10::DECIMAL<=score AND score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_type, item_name)AS level_10
		, COUNT(*) FILTER (WHERE score<10::DECIMAL) OVER (PARTITION BY rank_grade_year, rank_tag2, item_type, item_name)AS level_lt10
		, student_id
		, score
		, tag2_rank AS rank
		, tag2rank_percentage AS percentile
		, tag2rank_pr AS pr
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		(
            SELECT * FROM domain_rank_tag2--1.4 領域成績 類別2排名
            UNION ALL
            SELECT * FROM subject_rank_tag2--2.4 科目成績 類別2排名
            UNION ALL
            SELECT * FROM calc_sum_rank_tag2--3.4 總計成績 總分 類別2排名
            UNION ALL
            SELECT * FROM calc_avg_rank_tag2--4.4 總計成績 平均 類別2排名
            UNION ALL
            SELECT * FROM weight_sum_rank_tag2--5.4 總計成績 加權總分 類別2排名
            UNION ALL
            SELECT * FROM weight_avg_rank_tag2--6.4 總計成績 加權平均 類別2排名
        ) AS data
	WHERE
		rank_tag2 IS NOT NULL
		AND rank_tag2 <> ''
)
SELECT * FROM score_list";
        }
    }
}
