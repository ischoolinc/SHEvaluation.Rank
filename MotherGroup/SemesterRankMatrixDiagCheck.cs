using System;
using System.Collections.Generic;
using System.Text;

namespace SHEvaluation.Rank.MotherGroup
{
    public static class SemesterRankMatrixDiagCheck
    {
        [STAThread]
        public static int Main(string[] args)
        {
            List<string> errors = new List<string>();
            try
            {
                string formula = MinimalFormula();
                SemesterRankGradeStudents grade = new SemesterRankGradeStudents();
                grade.GradeYear = 3;
                grade.Students = new List<SemesterRankStudentInput>();
                grade.Students.Add(Student(101, true));
                grade.Students.Add(Student(102, true));
                grade.Students.Add(Student(103, false));
                SemesterRankPublishStatement publish = SemesterRankPublishSql.Build(
                    new SemesterRankGradeStudents[] { grade },
                    "114",
                    "2",
                    "<Setting />",
                    null,
                    formula,
                    false);
                Expect(errors, publish.Sql.IndexOf(", insert_batch_data AS") >= 0 && publish.Sql.IndexOf(", update_data AS") >= 0 && publish.Sql.IndexOf(", insert_matrix_data AS") >= 0, "產品公布語句仍含批次、停用與矩陣寫入");

                string diag = SemesterRankMatrixDiagSql.ToReadOnlyCandidateSql(publish.Sql, 114, 2, 3, -1);
                Expect(errors, diag.IndexOf("WITH student_list AS") >= 0 && diag.IndexOf(", calc_condition AS") >= 0 && diag.IndexOf(", subject_score AS") >= 0 && diag.IndexOf(", score_list AS") >= 0, "唯讀診斷保留成績計算 CTE");
                Expect(errors, diag.IndexOf("insert_batch_data") < 0 && diag.IndexOf("update_data") < 0 && diag.IndexOf("insert_matrix_data") < 0 && diag.IndexOf("insert_batch_student_data") < 0 && diag.IndexOf("insert_detail_data") < 0, "唯讀診斷移除所有寫入 CTE");
                Expect(errors, !ContainsKeyword(diag, "INSERT") && !ContainsKeyword(diag, "UPDATE") && !ContainsKeyword(diag, "DELETE") && !ContainsKeyword(diag, "RETURNING"), "唯讀診斷不可含寫入關鍵字");
                Expect(errors, diag.IndexOf("duplicate_candidate") >= 0 && diag.IndexOf("overlap_alive") >= 0 && diag.IndexOf("stats_conflict") >= 0 && diag.IndexOf("pr_level_only_conflict") >= 0, "診斷要輸出九欄重複、統計衝突、五標區間衝突與既有有效鍵重疊");
                Expect(errors, diag.IndexOf("NULL::INT AS batch_id") >= 0 && diag.IndexOf("FROM\n\t\tinsert_batch_data") < 0, "計算條件改為不寫入的常數，不依賴 insert_batch_data");
                Expect(errors, SemesterRankMatrixDiagSql.CandidateGroupsMatchInsert(diag), "候選矩陣 GROUP BY 必須含正式 INSERT 的 pr_* 與 level_*");
                int keyGroup = diag.IndexOf("matrix_key AS");
                Expect(errors, keyGroup >= 0 && diag.IndexOf("candidate_row_count") > keyGroup && diag.IndexOf("FROM\n\t\tscore_list\n\tGROUP BY") < 0, "重複計數依九欄唯一鍵，不對含學生明細的 score_list 直接 COUNT");
                int finalSelect = diag.IndexOf("'duplicate_candidate' AS section");
                string resultPart = finalSelect >= 0 ? diag.Substring(finalSelect) : "";
                Expect(errors, finalSelect >= 0 && resultPart.IndexOf("student_id") < 0 && resultPart.IndexOf("student_name") < 0 && resultPart.IndexOf("score_list.student") < 0, "診斷結果不輸出學生個資欄");

                Dictionary<string, string> leakKey = SemesterRankMatrixDiagSql.PrLevelOnlyConflictFixtureKey();
                List<Dictionary<string, string>> leakRows = SemesterRankMatrixDiagSql.PrLevelOnlyConflictFixtureRows();
                Expect(errors, SemesterRankMatrixDiagSql.CountInsertShapedRowsWithoutPrLevel(leakRows, leakKey) == 1, "舊的缺 pr_*/level_* 分組會把只差五標或區間人數的兩列誤合成一筆");
                Expect(errors, SemesterRankMatrixDiagSql.CountInsertShapedRowsForUniqueKey(leakRows, leakKey) == 2, "對齊正式 INSERT 分組後，只差五標或區間人數仍判成兩筆寫入候選");

                string catalog = SemesterRankMatrixDiagSql.CatalogProbe();
                Expect(errors, catalog.IndexOf("rank_matrix_unique_key") < 0 && catalog.IndexOf("pg_constraint") >= 0 && catalog.IndexOf("pg_trigger") >= 0 && catalog.IndexOf("is_alive") >= 0, "目錄探查讀實際約束、索引、觸發器與 is_alive 可空性");

                string coverage = SemesterRankMatrixDiagSql.ExistingAliveCoverage(114, 2, 3, -1);
                Expect(errors, coverage.IndexOf("update_scope_gap") >= 0 && coverage.IndexOf("concurrent_rank_writers") >= 0 && coverage.IndexOf("null_alive_same_period") >= 0, "既有有效矩陣要對照停用範圍與並行寫入");

                string plan = SemesterRankMatrixDiagSql.UpdatePlanExplain(114, 2, 3, -1);
                Expect(errors, plan.StartsWith("EXPLAIN (COSTS OFF)") && plan.IndexOf("EXPLAIN ANALYZE") < 0 && plan.IndexOf("item_type LIKE '學期/%'") >= 0, "停用計畫只用 EXPLAIN，不用 EXPLAIN ANALYZE");

                string fullExplain = SemesterRankMatrixDiagSql.ToFullPublishExplain(publish.Sql);
                Expect(errors, fullExplain.StartsWith("EXPLAIN (COSTS OFF)\n") && fullExplain.IndexOf("EXPLAIN ANALYZE") < 0 && fullExplain.IndexOf(", update_data AS") >= 0 && fullExplain.IndexOf(", insert_matrix_data AS") >= 0 && fullExplain.IndexOf("update_data.id") >= 0, "完整發布 EXPLAIN 保留停用與插入，且不用 ANALYZE");

                string publishSource = System.IO.File.ReadAllText("MotherGroup\\SemesterRankPublishSql.cs", Encoding.UTF8);
                Expect(errors, publishSource.IndexOf("ON CONFLICT") < 0 && publishSource.IndexOf("DO NOTHING") < 0, "公布路徑不使用 ON CONFLICT DO NOTHING");

                errors.AddRange(CheckRealFormula());
            }
            catch (Exception ex)
            {
                errors.Add(ex.GetType().Name + ": " + ex.Message);
            }

            if (errors.Count == 0)
            {
                Console.WriteLine("semester-rank-matrix-diag-check: ok");
                Console.WriteLine("database-executed: false");
                Console.WriteLine("formula-changed: false");
                return 0;
            }
            foreach (string error in errors)
                Console.WriteLine(error);
            return 1;
        }

        private static List<string> CheckRealFormula()
        {
            List<string> errors = new List<string>();
            string formula = SemesterRankFormulaSql.BuildOriginOnly();
            Expect(errors, formula.IndexOf(", subject_score AS") >= 0 && formula.IndexOf(", update_data AS") >= 0 && formula.IndexOf(", insert_matrix_data AS") >= 0 && formula.IndexOf("SELECT count(*) FROM score_list") >= 0, "真實公式應含計算與寫入區段");
            Expect(errors, formula.IndexOf("+ schoolYearAdjScore") < 0 && formula.IndexOf("+ @\"") < 0 && formula.IndexOf("string insertRankSql") < 0 && formula.IndexOf("//, NULLIF") < 0, "真實公式不可殘留 C# 截碼字樣");
            Expect(errors, formula.IndexOf("@原始成績") >= 0 && formula.IndexOf("GREATEST") >= 0 && formula.IndexOf("unnest(xpath('/root/SemesterSubjectScoreInfo/Subject'") >= 0, "真實公式應保留科目展開與擇優 GREATEST");

            bool extractThrows = false;
            try
            {
                SemesterRankMatrixDiagSql.ExtractOriginOnlyFormula(System.IO.File.ReadAllText("CalculateSemesterAssessmentRank.cs", Encoding.UTF8));
            }
            catch (InvalidOperationException)
            {
                extractThrows = true;
            }
            Expect(errors, extractThrows, "截取 C# 原始碼的 ExtractOriginOnlyFormula 應停用");

            string form = System.IO.File.ReadAllText("CalculateSemesterAssessmentRank.cs", Encoding.UTF8);
            Expect(errors, form.IndexOf("SemesterRankFormulaSql.Build(gradeSelectCopy)") >= 0 && form.IndexOf("string insertRankSql = @\"") < 0, "產品畫面應改呼叫共用公式組句");

            SemesterRankGradeStudents grade = new SemesterRankGradeStudents();
            grade.GradeYear = 3;
            grade.Students = new List<SemesterRankStudentInput>();
            grade.Students.Add(Student(201, true));
            grade.Students.Add(Student(202, true));
            SemesterRankPublishStatement publish = SemesterRankPublishSql.Build(
                new SemesterRankGradeStudents[] { grade },
                "114",
                "2",
                "<Setting />",
                null,
                formula,
                false);
            string applied = SemesterRankSql.ApplyParticipation(publish.Sql);
            Expect(errors, applied.IndexOf("+ schoolYearAdjScore") < 0 && applied.IndexOf("+ @\"") < 0 && applied.IndexOf("//, NULLIF") < 0, "套用參與旗標後仍不可有 C# 殘留");
            string diag = SemesterRankMatrixDiagSql.ToReadOnlyCandidateSql(applied, 114, 2, 3, -1);
            Expect(errors, SemesterRankMatrixDiagSql.CandidateGroupsMatchInsert(diag) && diag.IndexOf("pr_88") >= 0 && diag.IndexOf("level_80") >= 0, "真實公式的唯讀候選要對齊正式 INSERT 分組");
            string explain = SemesterRankMatrixDiagSql.ToFullPublishExplain(applied);
            Expect(errors, explain.StartsWith("EXPLAIN (COSTS OFF)\n") && explain.IndexOf("EXPLAIN ANALYZE") < 0 && explain.IndexOf(", update_data AS") >= 0 && explain.IndexOf("update_data.id") >= 0 && explain.IndexOf("+ @\"") < 0, "真實公式的完整發布 EXPLAIN 可組出且不含 ANALYZE");

            string withAdj = SemesterRankFormulaSql.Build(new string[] { "學年調整成績", "補考成績" });
            Expect(errors, withAdj.IndexOf("@學年調整成績") >= 0 && withAdj.IndexOf("@補考成績") >= 0 && withAdj.IndexOf("+ schoolYearAdjScore") < 0, "勾選擇優項目時應嵌入對應 xpath，不是 C# 變數名");
            errors.AddRange(CheckSixteenScoreCombinations());
            return errors;
        }

        private static List<string> CheckSixteenScoreCombinations()
        {
            List<string> errors = new List<string>();
            string[] options = new string[] { "學年調整成績", "手動調整成績", "補考成績", "重修成績" };
            string[] xpathAttrs = new string[] { "學年調整成績", "擇優採計成績", "補考成績", "重修成績" };
            for (int mask = 0; mask < 16; mask++)
            {
                List<string> selected = new List<string>();
                for (int bit = 0; bit < 4; bit++)
                {
                    if ((mask & (1 << bit)) != 0)
                        selected.Add(options[bit]);
                }
                string sql = SemesterRankFormulaSql.Build(selected);
                Expect(errors, sql.IndexOf("+ schoolYearAdjScore") < 0 && sql.IndexOf("+ @\"") < 0 && sql.IndexOf("//,") < 0, "組合 " + mask + " 不可殘留 C# 接縫");
                Expect(errors, sql.IndexOf("@原始成績") >= 0 && sql.IndexOf("GREATEST") >= 0 && sql.IndexOf(", update_data AS") >= 0, "組合 " + mask + " 應是可執行的完整公式");
                for (int bit = 0; bit < 4; bit++)
                {
                    bool want = (mask & (1 << bit)) != 0;
                    bool has = sql.IndexOf("xpath('/root/Subject/@" + xpathAttrs[bit] + "'") >= 0;
                    Expect(errors, has == want, "組合 " + mask + " 對 " + options[bit] + " 的 xpath 應" + (want ? "出現" : "省略"));
                }
            }
            return errors;
        }

        private static string MinimalFormula()
        {
            return @", subject_score AS
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
		, '國文'::TEXT AS subject
		, 80::DECIMAL AS subject_origin_score
		, 80::DECIMAL AS subject_greatest_score
		, student_list.participate_year
		, student_list.participate_dept
		, student_list.participate_class
		, student_list.participate_tag1
		, student_list.participate_tag2
	FROM
		student_list
		INNER JOIN calc_condition
			ON student_list.rank_grade_year = calc_condition.rank_grade_year::INT
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
		, participate_year
		, participate_dept
		, participate_class
		, participate_tag1
		, participate_tag2
		, RANK() OVER(PARTITION BY rank_grade_year, subject ORDER BY subject_origin_score DESC) AS grade_origin_rank
		, 1 AS grade_origin_row_number
		, COUNT(student_id) OVER(PARTITION BY rank_grade_year, subject) AS grade_count
		, 1 AS graderank_origin_pr
		, 1 AS graderank_origin_percentage
	FROM
		subject_score
	WHERE
		subject_origin_score IS NOT NULL
)
, subj_origin_rank_expand AS
(
	SELECT * FROM subj_origin_rank
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
		, 0::FLOAT8 AS std_dev_pop
		, 80::DECIMAL AS pr_88
		, 80::DECIMAL AS pr_75
		, 80::DECIMAL AS pr_50
		, 80::DECIMAL AS pr_25
		, 80::DECIMAL AS pr_12
		, 80::DECIMAL AS avg_top_25
		, 80::DECIMAL AS avg_top_50
		, 80::DECIMAL AS avg
		, 80::DECIMAL AS avg_bottom_50
		, 80::DECIMAL AS avg_bottom_25
		, 0 AS level_gte100
		, 0 AS level_90
		, 1 AS level_80
		, 0 AS level_70
		, 0 AS level_60
		, 0 AS level_50
		, 0 AS level_40
		, 0 AS level_30
		, 0 AS level_20
		, 0 AS level_10
		, 0 AS level_lt10
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
	WHERE
		participate_year
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
	INSERT INTO rank_matrix
	(
		ref_batch_id, school_year, semester, grade_year, item_type, ref_exam_id, item_name, rank_type, rank_name, is_alive
		, matrix_count, avg_top_25, avg_top_50, avg, avg_bottom_50, avg_bottom_25
		, level_gte100, level_90, level_80, level_70, level_60, level_50, level_40, level_30, level_20, level_10, level_lt10
		, pr_88, pr_75, pr_50, pr_25, pr_12, std_dev_pop
	)
	SELECT
		calc_condition.batch_id
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
			ON update_data.id < 0
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
)
, insert_batch_student_data AS
(
	INSERT INTO rank_batch_student (ref_batch_id, ref_student_id, grade_year, matrix_grade, matrix_class, matrix_tag1, matrix_tag2)
	SELECT
		calc_condition.batch_id
		, score_list.student_id
		, score_list.rank_grade_year::INT
		, score_list.rank_grade_year || '年級'
		, score_list.rank_class_name
		, score_list.rank_tag1
		, score_list.rank_tag2
	FROM
		score_list
		CROSS JOIN calc_condition
)
, insert_detail_data AS
(
	INSERT INTO rank_detail (ref_matrix_id, ref_student_id, score, rank, pr, percentile)
	SELECT
		insert_matrix_data.id AS ref_matrix_id
		, score_list.student_id
		, score_list.score
		, score_list.rank
		, score_list.pr
		, score_list.percentile
	FROM
		score_list
		LEFT OUTER JOIN insert_matrix_data
			ON insert_matrix_data.school_year = score_list.rank_school_year::INT
			AND insert_matrix_data.semester = score_list.rank_semester::INT
			AND insert_matrix_data.grade_year = score_list.rank_grade_year::INT
			AND insert_matrix_data.item_type = score_list.item_type
			AND insert_matrix_data.ref_exam_id = score_list.ref_exam_id
			AND insert_matrix_data.item_name = score_list.item_name
			AND insert_matrix_data.rank_type = score_list.rank_type
			AND insert_matrix_data.rank_name = score_list.rank_name
)
SELECT count(*) FROM score_list";
        }

        private static SemesterRankStudentInput Student(long id, bool participate)
        {
            SemesterRankStudentInput input = new SemesterRankStudentInput();
            input.StudentId = id;
            input.StudentName = "S" + id;
            input.GradeYear = 3;
            input.DeptName = "普通科";
            input.ClassName = "301";
            input.Tag1 = "類組A";
            input.Tag2 = "全校";
            input.ParticipateYear = participate;
            input.ParticipateDept = participate;
            input.ParticipateClass = participate;
            input.ParticipateTag1 = participate;
            input.ParticipateTag2 = participate;
            return input;
        }

        private static void Expect(List<string> errors, bool ok, string message)
        {
            if (!ok)
                errors.Add(message);
        }

        private static bool ContainsKeyword(string sql, string keyword)
        {
            int at = 0;
            while (at <= sql.Length - keyword.Length)
            {
                at = sql.IndexOf(keyword, at, StringComparison.OrdinalIgnoreCase);
                if (at < 0)
                    return false;
                bool left = at == 0 || !(char.IsLetterOrDigit(sql[at - 1]) || sql[at - 1] == '_');
                int end = at + keyword.Length;
                bool right = end >= sql.Length || !(char.IsLetterOrDigit(sql[end]) || sql[end] == '_');
                if (left && right)
                    return true;
                at = end;
            }
            return false;
        }
    }
}
