namespace SHEvaluation.Rank.RegularRank
{
    public static class RegularRankMatrixInsertSql
    {
        public const string Cte = @"insert_matrix_data AS (
	INSERT INTO rank_matrix(
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
	SELECT DISTINCT
		row.batch_id AS ref_batch_id
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
	FROM
		score_list
		LEFT OUTER JOIN update_data
			ON update_data.id  < 0 --永遠為false，只是為了讓insert等待update執行完
		CROSS JOIN row
	RETURNING *
)";
    }
}
