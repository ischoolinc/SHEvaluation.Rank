using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace SHEvaluation.Rank.MotherGroup
{
    /// <summary>
    /// 把產品公布語句改成不寫入的矩陣候選診斷。
    /// 會移除 insert_batch_data、update_data 與所有 INSERT／UPDATE／DELETE 及其相依，
    /// 不可只改最末 SELECT。
    /// </summary>
    public static class SemesterRankMatrixDiagSql
    {
        /// <summary>
        /// 與正式 insert_matrix_data 的 GROUP BY 相同（不含 batch_id）。
        /// 含五標 pr_* 與分數區間 level_*，漏任一欄都會漏判只在這些欄位不同的重複鍵。
        /// </summary>
        public static readonly string[] InsertMatrixGroupColumns = new string[]
        {
            "rank_school_year", "rank_semester", "rank_grade_year", "item_type", "ref_exam_id",
            "item_name", "rank_type", "rank_name", "is_alive", "matrix_count",
            "avg_top_25", "avg_top_50", "avg", "avg_bottom_50", "avg_bottom_25",
            "level_gte100", "level_90", "level_80", "level_70", "level_60", "level_50",
            "level_40", "level_30", "level_20", "level_10", "level_lt10",
            "pr_88", "pr_75", "pr_50", "pr_25", "pr_12", "std_dev_pop"
        };

        public static readonly string[] UniqueKeyColumns = new string[]
        {
            "school_year", "semester", "grade_year", "item_type", "ref_exam_id",
            "item_name", "rank_type", "rank_name", "is_alive"
        };

        public static readonly string[] PrColumns = new string[] { "pr_88", "pr_75", "pr_50", "pr_25", "pr_12" };

        public static readonly string[] LevelColumns = new string[]
        {
            "level_gte100", "level_90", "level_80", "level_70", "level_60", "level_50",
            "level_40", "level_30", "level_20", "level_10", "level_lt10"
        };

        public static string CatalogProbe()
        {
            return @"
SELECT
    'connection' AS section
    , current_database() AS database_name
    , current_setting('server_version') AS server_version
    , current_setting('server_version_num') AS server_version_num
UNION ALL
SELECT
    'column'
    , c.relname
    , a.attname
    , CASE WHEN a.attnotnull THEN 'not_null' ELSE 'nullable' END
FROM pg_attribute AS a
INNER JOIN pg_class AS c ON c.oid = a.attrelid
INNER JOIN pg_namespace AS n ON n.oid = c.relnamespace
WHERE c.relname = 'rank_matrix'
    AND a.attname = 'is_alive'
    AND a.attnum > 0
    AND NOT a.attisdropped
    AND n.nspname NOT IN ('pg_catalog', 'information_schema')
    AND n.nspname NOT LIKE 'pg_toast%'
UNION ALL
SELECT
    'constraint'
    , c.relname
    , con.conname
    , pg_get_constraintdef(con.oid)
FROM pg_constraint AS con
INNER JOIN pg_class AS c ON c.oid = con.conrelid
INNER JOIN pg_namespace AS n ON n.oid = c.relnamespace
WHERE c.relname = 'rank_matrix'
    AND con.contype IN ('u', 'p', 'x', 'c')
    AND n.nspname NOT IN ('pg_catalog', 'information_schema')
UNION ALL
SELECT
    'index'
    , c.relname
    , i.relname
    , COALESCE(pg_get_indexdef(ix.indexrelid), '') || CASE WHEN ix.indisunique THEN ' UNIQUE' ELSE '' END || CASE WHEN pg_get_expr(ix.indpred, ix.indrelid) IS NULL THEN '' ELSE ' WHERE ' || pg_get_expr(ix.indpred, ix.indrelid) END
FROM pg_index AS ix
INNER JOIN pg_class AS c ON c.oid = ix.indrelid
INNER JOIN pg_class AS i ON i.oid = ix.indexrelid
INNER JOIN pg_namespace AS n ON n.oid = c.relnamespace
WHERE c.relname = 'rank_matrix'
    AND n.nspname NOT IN ('pg_catalog', 'information_schema')
UNION ALL
SELECT
    'trigger'
    , c.relname
    , t.tgname
    , pg_get_triggerdef(t.oid)
FROM pg_trigger AS t
INNER JOIN pg_class AS c ON c.oid = t.tgrelid
INNER JOIN pg_namespace AS n ON n.oid = c.relnamespace
WHERE c.relname IN ('rank_matrix', 'rank_batch', 'rank_batch_student', 'rank_detail')
    AND NOT t.tgisinternal
    AND n.nspname NOT IN ('pg_catalog', 'information_schema')
ORDER BY 1, 2, 3";
        }

        public static string ExistingAliveCoverage(int schoolYear, int semester, int gradeYear, int refExamId)
        {
            StringBuilder sql = new StringBuilder();
            sql.Append("SELECT\n");
            sql.Append("\t'alive_summary' AS section\n");
            sql.Append("\t, COUNT(*) AS alive_count\n");
            sql.Append("\t, COUNT(*) FILTER (WHERE item_type LIKE '學期/%') AS semester_item_count\n");
            sql.Append("\t, COUNT(*) FILTER (WHERE item_type NOT LIKE '學期/%' OR item_type IS NULL) AS other_item_count\n");
            sql.Append("\t, COUNT(*) FILTER (WHERE ref_exam_id IS DISTINCT FROM ").Append(refExamId).Append(") AS other_exam_count\n");
            sql.Append("\t, COUNT(DISTINCT item_type) AS item_type_n\n");
            sql.Append("\t, COUNT(DISTINCT rank_type) AS rank_type_n\n");
            sql.Append("\t, COUNT(DISTINCT item_name) AS item_name_n\n");
            sql.Append("\t, COUNT(DISTINCT rank_name) AS rank_name_n\n");
            sql.Append("FROM rank_matrix\n");
            sql.Append("WHERE is_alive = true\n");
            sql.Append("\tAND school_year = ").Append(schoolYear).Append("\n");
            sql.Append("\tAND semester = ").Append(semester).Append("\n");
            sql.Append("\tAND grade_year = ").Append(gradeYear).Append("\n");
            sql.Append("UNION ALL\n");
            sql.Append("SELECT\n");
            sql.Append("\t'alive_by_key'\n");
            sql.Append("\t, COUNT(*)\n");
            sql.Append("\t, NULL\n");
            sql.Append("\t, NULL\n");
            sql.Append("\t, NULL\n");
            sql.Append("\t, NULL\n");
            sql.Append("\t, NULL\n");
            sql.Append("\t, NULL\n");
            sql.Append("\t, NULL\n");
            sql.Append("FROM (\n");
            sql.Append("\tSELECT school_year, semester, grade_year, item_type, ref_exam_id, item_name, rank_type, rank_name, is_alive\n");
            sql.Append("\tFROM rank_matrix\n");
            sql.Append("\tWHERE is_alive = true\n");
            sql.Append("\t\tAND school_year = ").Append(schoolYear).Append("\n");
            sql.Append("\t\tAND semester = ").Append(semester).Append("\n");
            sql.Append("\t\tAND grade_year = ").Append(gradeYear).Append("\n");
            sql.Append("\tGROUP BY school_year, semester, grade_year, item_type, ref_exam_id, item_name, rank_type, rank_name, is_alive\n");
            sql.Append("\tHAVING COUNT(*) > 1\n");
            sql.Append(") AS duplicated_alive\n");
            sql.Append("UNION ALL\n");
            sql.Append("SELECT\n");
            sql.Append("\t'update_scope_gap'\n");
            sql.Append("\t, COUNT(*)\n");
            sql.Append("\t, COUNT(*) FILTER (WHERE NOT (item_type LIKE '學期/%'))\n");
            sql.Append("\t, COUNT(*) FILTER (WHERE ref_exam_id IS DISTINCT FROM ").Append(refExamId).Append(")\n");
            sql.Append("\t, NULL\n");
            sql.Append("\t, NULL\n");
            sql.Append("\t, NULL\n");
            sql.Append("\t, NULL\n");
            sql.Append("\t, NULL\n");
            sql.Append("FROM rank_matrix\n");
            sql.Append("WHERE is_alive = true\n");
            sql.Append("\tAND school_year = ").Append(schoolYear).Append("\n");
            sql.Append("\tAND semester = ").Append(semester).Append("\n");
            sql.Append("\tAND grade_year = ").Append(gradeYear).Append("\n");
            sql.Append("\tAND NOT (\n");
            sql.Append("\t\tref_exam_id = ").Append(refExamId).Append("\n");
            sql.Append("\t\tAND item_type LIKE '學期/%'\n");
            sql.Append("\t)\n");
            sql.Append("UNION ALL\n");
            sql.Append("SELECT\n");
            sql.Append("\t'null_alive_same_period'\n");
            sql.Append("\t, COUNT(*)\n");
            sql.Append("\t, COUNT(*) FILTER (WHERE item_type LIKE '學期/%')\n");
            sql.Append("\t, NULL\n");
            sql.Append("\t, NULL\n");
            sql.Append("\t, COUNT(DISTINCT item_type)\n");
            sql.Append("\t, COUNT(DISTINCT rank_type)\n");
            sql.Append("\t, COUNT(DISTINCT item_name)\n");
            sql.Append("\t, COUNT(DISTINCT rank_name)\n");
            sql.Append("FROM rank_matrix\n");
            sql.Append("WHERE is_alive IS NULL\n");
            sql.Append("\tAND school_year = ").Append(schoolYear).Append("\n");
            sql.Append("\tAND semester = ").Append(semester).Append("\n");
            sql.Append("\tAND grade_year = ").Append(gradeYear).Append("\n");
            sql.Append("UNION ALL\n");
            sql.Append("SELECT\n");
            sql.Append("\t'concurrent_rank_writers'\n");
            sql.Append("\t, COUNT(*)\n");
            sql.Append("\t, NULL, NULL, NULL, NULL, NULL, NULL, NULL\n");
            sql.Append("FROM pg_stat_activity\n");
            sql.Append("WHERE datname = current_database()\n");
            sql.Append("\tAND pid <> pg_backend_pid()\n");
            sql.Append("\tAND state <> 'idle'\n");
            sql.Append("\tAND (\n");
            sql.Append("\t\tquery ILIKE '%insert_matrix_data%'\n");
            sql.Append("\t\tOR query ILIKE '%rank_matrix%'\n");
            sql.Append("\t\tOR query ILIKE '%計算學期成績排名%'\n");
            sql.Append("\t)");
            return sql.ToString();
        }

        public static string UpdatePlanExplain(int schoolYear, int semester, int gradeYear, int refExamId)
        {
            StringBuilder sql = new StringBuilder();
            sql.Append("EXPLAIN (COSTS OFF)\n");
            sql.Append("UPDATE rank_matrix\n");
            sql.Append("SET is_alive = NULL\n");
            sql.Append("FROM (\n");
            sql.Append("\tSELECT\n");
            sql.Append("\t\t'").Append(gradeYear).Append("'::TEXT AS rank_grade_year\n");
            sql.Append("\t\t, '").Append(schoolYear).Append("'::TEXT AS rank_school_year\n");
            sql.Append("\t\t, '").Append(semester).Append("'::TEXT AS rank_semester\n");
            sql.Append("\t\t, '").Append(refExamId).Append("'::TEXT AS ref_exam_id\n");
            sql.Append(") AS calc_condition\n");
            sql.Append("WHERE rank_matrix.is_alive = true\n");
            sql.Append("\tAND rank_matrix.school_year = calc_condition.rank_school_year::INT\n");
            sql.Append("\tAND rank_matrix.semester = calc_condition.rank_semester::INT\n");
            sql.Append("\tAND rank_matrix.grade_year = calc_condition.rank_grade_year::INT\n");
            sql.Append("\tAND rank_matrix.ref_exam_id = calc_condition.ref_exam_id::INT\n");
            sql.Append("\tAND rank_matrix.item_type LIKE '學期/%'");
            return sql.ToString();
        }

        /// <summary>
        /// 完整產品公布語句的執行計畫。只用 EXPLAIN，不用 ANALYZE，避免實際寫入。
        /// </summary>
        public static string ToFullPublishExplain(string publishSql)
        {
            if (string.IsNullOrEmpty(publishSql))
                throw new ArgumentException("公布語句是空的。");
            string text = publishSql.Replace("\r\n", "\n").Replace("\r", "\n").Trim();
            if (text.IndexOf("EXPLAIN ANALYZE", StringComparison.OrdinalIgnoreCase) >= 0)
                throw new InvalidOperationException("不可對寫入語句使用 EXPLAIN ANALYZE。");
            if (text.StartsWith("EXPLAIN", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("公布語句已含 EXPLAIN。");
            if (text.IndexOf(", update_data AS") < 0 || text.IndexOf(", insert_matrix_data AS") < 0)
                throw new InvalidOperationException("完整發布 EXPLAIN 需要保留 update_data 與 insert_matrix_data。");
            while (text.EndsWith(";"))
                text = text.Substring(0, text.Length - 1).TrimEnd();
            return "EXPLAIN (COSTS OFF)\n" + text;
        }

        [Obsolete("請改用 SemesterRankFormulaSql.BuildOriginOnly()，不要從 C# 原始碼截取 SQL。")]
        public static string ExtractOriginOnlyFormula(string calculateSource)
        {
            throw new InvalidOperationException("請改用 SemesterRankFormulaSql.BuildOriginOnly()，不要從 CalculateSemesterAssessmentRank.cs 截取 SQL。");
        }

        public static bool CandidateGroupsMatchInsert(string candidateSql)
        {
            if (string.IsNullOrEmpty(candidateSql))
                return false;
            int candidate = candidateSql.IndexOf("matrix_candidate AS");
            int key = candidateSql.IndexOf("matrix_key AS", candidate < 0 ? 0 : candidate);
            if (candidate < 0 || key <= candidate)
                return false;
            string body = candidateSql.Substring(candidate, key - candidate);
            for (int i = 0; i < InsertMatrixGroupColumns.Length; i++)
            {
                string column = InsertMatrixGroupColumns[i];
                if (body.IndexOf("score_list." + column) < 0)
                    return false;
            }
            for (int i = 0; i < PrColumns.Length; i++)
            {
                if (body.IndexOf("score_list." + PrColumns[i]) < 0)
                    return false;
            }
            for (int i = 0; i < LevelColumns.Length; i++)
            {
                if (body.IndexOf("score_list." + LevelColumns[i]) < 0)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 以正式 INSERT 分組形狀計算同一九欄唯一鍵會產生幾列。用來測只差 pr_*／level_* 的漏判。
        /// </summary>
        public static int CountInsertShapedRowsForUniqueKey(IList<Dictionary<string, string>> rows, Dictionary<string, string> uniqueKey)
        {
            if (rows == null || uniqueKey == null)
                return 0;
            Dictionary<string, bool> shapes = new Dictionary<string, bool>();
            foreach (Dictionary<string, string> row in rows)
            {
                if (row == null || !MatchesUniqueKey(row, uniqueKey))
                    continue;
                string shape = InsertShapeKey(row);
                if (!shapes.ContainsKey(shape))
                    shapes.Add(shape, true);
            }
            return shapes.Count;
        }

        public static int CountInsertShapedRowsWithoutPrLevel(IList<Dictionary<string, string>> rows, Dictionary<string, string> uniqueKey)
        {
            if (rows == null || uniqueKey == null)
                return 0;
            Dictionary<string, bool> shapes = new Dictionary<string, bool>();
            foreach (Dictionary<string, string> row in rows)
            {
                if (row == null || !MatchesUniqueKey(row, uniqueKey))
                    continue;
                string shape = InsertShapeKeyWithoutPrLevel(row);
                if (!shapes.ContainsKey(shape))
                    shapes.Add(shape, true);
            }
            return shapes.Count;
        }

        public static Dictionary<string, string> PrLevelOnlyConflictFixtureKey()
        {
            Dictionary<string, string> key = new Dictionary<string, string>();
            key["school_year"] = "114";
            key["semester"] = "2";
            key["grade_year"] = "3";
            key["item_type"] = "學期/科目成績(原始)";
            key["ref_exam_id"] = "-1";
            key["item_name"] = "國文";
            key["rank_type"] = "年排名";
            key["rank_name"] = "3年級";
            key["is_alive"] = "true";
            return key;
        }

        public static List<Dictionary<string, string>> PrLevelOnlyConflictFixtureRows()
        {
            Dictionary<string, string> a = BaseFixtureRow();
            a["pr_88"] = "88.1";
            a["level_80"] = "10";
            Dictionary<string, string> b = BaseFixtureRow();
            b["pr_88"] = "88.2";
            b["level_80"] = "11";
            return new List<Dictionary<string, string>> { a, b };
        }

        private static Dictionary<string, string> BaseFixtureRow()
        {
            Dictionary<string, string> row = PrLevelOnlyConflictFixtureKey();
            row["rank_school_year"] = row["school_year"];
            row["rank_semester"] = row["semester"];
            row["rank_grade_year"] = row["grade_year"];
            row["matrix_count"] = "664";
            row["avg_top_25"] = "90";
            row["avg_top_50"] = "85";
            row["avg"] = "80";
            row["avg_bottom_50"] = "70";
            row["avg_bottom_25"] = "60";
            row["level_gte100"] = "0";
            row["level_90"] = "0";
            row["level_80"] = "10";
            row["level_70"] = "0";
            row["level_60"] = "0";
            row["level_50"] = "0";
            row["level_40"] = "0";
            row["level_30"] = "0";
            row["level_20"] = "0";
            row["level_10"] = "0";
            row["level_lt10"] = "0";
            row["pr_88"] = "88.1";
            row["pr_75"] = "75";
            row["pr_50"] = "50";
            row["pr_25"] = "25";
            row["pr_12"] = "12";
            row["std_dev_pop"] = "1.5";
            return row;
        }

        private static bool MatchesUniqueKey(Dictionary<string, string> row, Dictionary<string, string> uniqueKey)
        {
            for (int i = 0; i < UniqueKeyColumns.Length; i++)
            {
                string column = UniqueKeyColumns[i];
                string expected = uniqueKey.ContainsKey(column) ? uniqueKey[column] : null;
                string actual = RowValue(row, column);
                if (!string.Equals(expected, actual, StringComparison.Ordinal))
                    return false;
            }
            return true;
        }

        private static string InsertShapeKey(Dictionary<string, string> row)
        {
            StringBuilder text = new StringBuilder();
            for (int i = 0; i < InsertMatrixGroupColumns.Length; i++)
            {
                if (i > 0)
                    text.Append('\u001f');
                text.Append(RowValue(row, InsertMatrixGroupColumns[i]));
            }
            return text.ToString();
        }

        private static string InsertShapeKeyWithoutPrLevel(Dictionary<string, string> row)
        {
            StringBuilder text = new StringBuilder();
            for (int i = 0; i < InsertMatrixGroupColumns.Length; i++)
            {
                string column = InsertMatrixGroupColumns[i];
                if (IsPrOrLevel(column))
                    continue;
                if (text.Length > 0)
                    text.Append('\u001f');
                text.Append(RowValue(row, column));
            }
            return text.ToString();
        }

        private static bool IsPrOrLevel(string column)
        {
            for (int i = 0; i < PrColumns.Length; i++)
            {
                if (PrColumns[i] == column)
                    return true;
            }
            for (int i = 0; i < LevelColumns.Length; i++)
            {
                if (LevelColumns[i] == column)
                    return true;
            }
            return false;
        }

        private static string RowValue(Dictionary<string, string> row, string column)
        {
            if (row.ContainsKey(column))
                return row[column] ?? "";
            if (column == "school_year" && row.ContainsKey("rank_school_year"))
                return row["rank_school_year"] ?? "";
            if (column == "semester" && row.ContainsKey("rank_semester"))
                return row["rank_semester"] ?? "";
            if (column == "grade_year" && row.ContainsKey("rank_grade_year"))
                return row["rank_grade_year"] ?? "";
            if (column.StartsWith("rank_") && row.ContainsKey(column.Substring(5)))
                return row[column.Substring(5)] ?? "";
            return "";
        }

        public static string ToReadOnlyCandidateSql(string publishSql, int schoolYear, int semester, int gradeYear, int refExamId)
        {
            if (string.IsNullOrEmpty(publishSql))
                throw new ArgumentException("公布語句是空的。");
            string text = publishSql.Replace("\r\n", "\n").Replace("\r", "\n");
            EnsureSelectOnlyAfterRewrite(text, false);
            int studentAt = IndexOfKeyword(text, "WITH student_list AS");
            if (studentAt < 0)
                studentAt = IndexOfKeyword(text, "with student_list as");
            if (studentAt < 0)
                throw new InvalidOperationException("公布語句缺少 student_list。");
            int subjectAt = text.IndexOf(", subject_score AS");
            if (subjectAt < 0)
                throw new InvalidOperationException("公布語句缺少 subject_score。");
            int scoreAt = text.IndexOf(", score_list AS");
            int updateAt = text.IndexOf(", update_data AS");
            if (scoreAt < 0 || updateAt < scoreAt)
                throw new InvalidOperationException("公布語句缺少 score_list 或 update_data。");
            if (ContainsWriteKeyword(text.Substring(subjectAt, updateAt - subjectAt)))
                throw new InvalidOperationException("計算區段已含寫入關鍵字，不能改成唯讀診斷。");

            string studentList = ExtractStudentList(text, studentAt, subjectAt);
            string calc = ReadOnlyCalcCondition(schoolYear, semester, gradeYear, refExamId);
            string body = text.Substring(subjectAt, updateAt - subjectAt).TrimEnd();
            if (body.EndsWith(")"))
                body = body.Substring(0, body.Length - 1).TrimEnd();
            if (body.EndsWith(","))
                body = body.Substring(0, body.Length - 1);

            StringBuilder sql = new StringBuilder();
            sql.Append(studentList.TrimEnd());
            if (!sql.ToString().EndsWith("\n"))
                sql.Append("\n");
            sql.Append(calc);
            sql.Append(body);
            sql.Append("\n)\n");
            sql.Append(CandidateSelect());
            string result = sql.ToString();
            EnsureSelectOnlyAfterRewrite(result, true);
            if (ContainsCte(result, "insert_batch_data")
                || ContainsCte(result, "update_data")
                || ContainsCte(result, "insert_matrix_data")
                || ContainsCte(result, "insert_batch_student_data")
                || ContainsCte(result, "insert_detail_data"))
                throw new InvalidOperationException("唯讀診斷仍殘留寫入 CTE。");
            return result;
        }

        private static string ExtractStudentList(string text, int studentAt, int subjectAt)
        {
            string head = text.Substring(studentAt, subjectAt - studentAt);
            int insertBatch = head.IndexOf(", insert_batch_data AS");
            if (insertBatch >= 0)
                head = head.Substring(0, insertBatch);
            int calc = head.IndexOf(", calc_condition AS");
            if (calc >= 0)
                head = head.Substring(0, calc);
            head = head.TrimEnd();
            if (head.EndsWith(","))
                head = head.Substring(0, head.Length - 1);
            return head;
        }

        private static string ReadOnlyCalcCondition(int schoolYear, int semester, int gradeYear, int refExamId)
        {
            StringBuilder sql = new StringBuilder();
            sql.Append(", calc_condition AS\n(\n");
            sql.Append("\tSELECT\n");
            sql.Append("\t\t'").Append(gradeYear).Append("'::TEXT AS rank_grade_year\n");
            sql.Append("\t\t, '").Append(schoolYear).Append("'::TEXT AS rank_school_year\n");
            sql.Append("\t\t, '").Append(semester).Append("'::TEXT AS rank_semester\n");
            sql.Append("\t\t, '").Append(refExamId).Append("'::TEXT AS ref_exam_id\n");
            sql.Append("\t\t, '學期成績'::TEXT AS rank_exam_name\n");
            sql.Append("\t\t, ''::TEXT AS calculation_setting\n");
            sql.Append("\t\t, NULL::INT AS batch_id\n");
            sql.Append(")\n");
            return sql.ToString();
        }

        private static string CandidateSelect()
        {
            return @", matrix_candidate AS
(
	SELECT
		score_list.rank_school_year::INT AS school_year
		, score_list.rank_semester::INT AS semester
		, score_list.rank_grade_year::INT AS grade_year
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
		, COUNT(*) AS score_row_count
	FROM
		score_list
		INNER JOIN calc_condition
			ON score_list.rank_grade_year = calc_condition.rank_grade_year::INT
	GROUP BY
		score_list.rank_school_year
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
, matrix_key AS
(
	SELECT
		school_year
		, semester
		, grade_year
		, item_type
		, ref_exam_id
		, item_name
		, rank_type
		, rank_name
		, is_alive
		, COUNT(*) AS candidate_row_count
		, COUNT(DISTINCT matrix_count) AS matrix_count_n
		, COUNT(DISTINCT avg) AS avg_n
		, COUNT(DISTINCT std_dev_pop) AS std_dev_n
		, COUNT(DISTINCT pr_88::TEXT || chr(1) || pr_75::TEXT || chr(1) || pr_50::TEXT || chr(1) || pr_25::TEXT || chr(1) || pr_12::TEXT) AS pr_shape_n
		, COUNT(DISTINCT level_gte100::TEXT || chr(1) || level_90::TEXT || chr(1) || level_80::TEXT || chr(1) || level_70::TEXT || chr(1) || level_60::TEXT || chr(1) || level_50::TEXT || chr(1) || level_40::TEXT || chr(1) || level_30::TEXT || chr(1) || level_20::TEXT || chr(1) || level_10::TEXT || chr(1) || level_lt10::TEXT) AS level_shape_n
		, SUM(score_row_count) AS score_row_sum
	FROM
		matrix_candidate
	GROUP BY
		school_year
		, semester
		, grade_year
		, item_type
		, ref_exam_id
		, item_name
		, rank_type
		, rank_name
		, is_alive
)
SELECT
	'duplicate_candidate' AS section
	, matrix_key.item_type
	, matrix_key.rank_type
	, matrix_key.rank_name
	, matrix_key.item_name
	, matrix_key.ref_exam_id
	, matrix_key.candidate_row_count
	, matrix_key.matrix_count_n
	, matrix_key.avg_n
	, matrix_key.std_dev_n
	, matrix_key.pr_shape_n
	, matrix_key.level_shape_n
	, matrix_key.score_row_sum
	, NULL::BIGINT AS existing_alive_id
FROM
	matrix_key
WHERE
	matrix_key.candidate_row_count > 1
UNION ALL
SELECT
	'stats_conflict'
	, matrix_key.item_type
	, matrix_key.rank_type
	, matrix_key.rank_name
	, matrix_key.item_name
	, matrix_key.ref_exam_id
	, matrix_key.candidate_row_count
	, matrix_key.matrix_count_n
	, matrix_key.avg_n
	, matrix_key.std_dev_n
	, matrix_key.pr_shape_n
	, matrix_key.level_shape_n
	, matrix_key.score_row_sum
	, NULL
FROM
	matrix_key
WHERE
	matrix_key.matrix_count_n > 1
	OR matrix_key.avg_n > 1
	OR matrix_key.std_dev_n > 1
	OR matrix_key.pr_shape_n > 1
	OR matrix_key.level_shape_n > 1
UNION ALL
SELECT
	'pr_level_only_conflict'
	, matrix_key.item_type
	, matrix_key.rank_type
	, matrix_key.rank_name
	, matrix_key.item_name
	, matrix_key.ref_exam_id
	, matrix_key.candidate_row_count
	, matrix_key.matrix_count_n
	, matrix_key.avg_n
	, matrix_key.std_dev_n
	, matrix_key.pr_shape_n
	, matrix_key.level_shape_n
	, matrix_key.score_row_sum
	, NULL
FROM
	matrix_key
WHERE
	matrix_key.candidate_row_count > 1
	AND matrix_key.matrix_count_n = 1
	AND matrix_key.avg_n = 1
	AND matrix_key.std_dev_n = 1
	AND (matrix_key.pr_shape_n > 1 OR matrix_key.level_shape_n > 1)
UNION ALL
SELECT
	'overlap_alive'
	, matrix_key.item_type
	, matrix_key.rank_type
	, matrix_key.rank_name
	, matrix_key.item_name
	, matrix_key.ref_exam_id
	, matrix_key.candidate_row_count
	, matrix_key.matrix_count_n
	, matrix_key.avg_n
	, matrix_key.std_dev_n
	, matrix_key.pr_shape_n
	, matrix_key.level_shape_n
	, matrix_key.score_row_sum
	, alive.id
FROM
	matrix_key
	INNER JOIN rank_matrix AS alive
		ON alive.is_alive = true
		AND alive.school_year = matrix_key.school_year
		AND alive.semester = matrix_key.semester
		AND alive.grade_year = matrix_key.grade_year
		AND alive.item_type = matrix_key.item_type
		AND alive.ref_exam_id = matrix_key.ref_exam_id
		AND alive.item_name = matrix_key.item_name
		AND alive.rank_type = matrix_key.rank_type
		AND alive.rank_name = matrix_key.rank_name
UNION ALL
SELECT
	'candidate_summary'
	, NULL
	, NULL
	, NULL
	, NULL
	, NULL
	, COUNT(*)
	, COUNT(*) FILTER (WHERE candidate_row_count > 1)
	, COUNT(*) FILTER (WHERE matrix_count_n > 1 OR avg_n > 1 OR std_dev_n > 1 OR pr_shape_n > 1 OR level_shape_n > 1)
	, NULL
	, COUNT(*) FILTER (WHERE candidate_row_count > 1 AND matrix_count_n = 1 AND avg_n = 1 AND std_dev_n = 1 AND (pr_shape_n > 1 OR level_shape_n > 1))
	, NULL
	, COALESCE(SUM(score_row_sum), 0)
	, NULL
FROM
	matrix_key
ORDER BY
	1
	, 2
	, 3
	, 4
	, 5";
        }

        private static void EnsureSelectOnlyAfterRewrite(string sql, bool rewritten)
        {
            if (!rewritten)
                return;
            if (ContainsWriteKeyword(sql))
                throw new InvalidOperationException("唯讀診斷含有資料寫入關鍵字。");
        }

        private static bool ContainsWriteKeyword(string sql)
        {
            string[] forbidden = new string[]
            {
                "INSERT", "UPDATE", "DELETE", "TRUNCATE", "MERGE", "ALTER", "DROP", "CREATE", "COPY", "CALL", "RETURNING"
            };
            foreach (string keyword in forbidden)
            {
                if (ContainsKeyword(sql, keyword))
                    return true;
            }
            return false;
        }

        private static bool ContainsCte(string sql, string name)
        {
            return Regex.IsMatch(sql, @"[,\s]" + Regex.Escape(name) + @"\s+AS\s*\(", RegexOptions.IgnoreCase);
        }

        private static bool ContainsKeyword(string sql, string keyword)
        {
            int at = 0;
            while (at <= sql.Length - keyword.Length)
            {
                at = sql.IndexOf(keyword, at, StringComparison.OrdinalIgnoreCase);
                if (at < 0)
                    return false;
                bool left = at == 0 || !IsIdent(sql[at - 1]);
                int end = at + keyword.Length;
                bool right = end >= sql.Length || !IsIdent(sql[end]);
                if (left && right)
                    return true;
                at = end;
            }
            return false;
        }

        private static int IndexOfKeyword(string sql, string keyword)
        {
            int at = 0;
            while (at <= sql.Length - keyword.Length)
            {
                at = sql.IndexOf(keyword, at, StringComparison.OrdinalIgnoreCase);
                if (at < 0)
                    return -1;
                bool left = at == 0 || !IsIdent(sql[at - 1]);
                int end = at + keyword.Length;
                bool right = end >= sql.Length || char.IsWhiteSpace(sql[end]) || sql[end] == '(';
                if (left && right)
                    return at;
                at = end;
            }
            return -1;
        }

        private static bool IsIdent(char ch)
        {
            return char.IsLetterOrDigit(ch) || ch == '_';
        }
    }
}
