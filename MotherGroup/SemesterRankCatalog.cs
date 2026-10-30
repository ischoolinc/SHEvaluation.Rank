using System;
using System.Collections.Generic;

namespace SHEvaluation.Rank.MotherGroup
{
    public sealed class CatalogFact
    {
        public string Section { get; set; }
        public string TableName { get; set; }
        public string SchemaName { get; set; }
        public string ObjectKind { get; set; }
        public string ObjectName { get; set; }
        public string ColumnName { get; set; }
        public string Definition { get; set; }
        public bool IsUnique { get; set; }
        public bool IsPrimary { get; set; }
        public bool IsPartial { get; set; }
        public bool MentionsIsAlive { get; set; }
        public bool ColumnNotNull { get; set; }
    }

    public sealed class CatalogAssessment
    {
        public string Verdict { get; private set; }
        public List<string> Reasons { get; private set; }

        public bool DatabaseExecuted
        {
            get { return false; }
        }

        internal CatalogAssessment(string verdict, List<string> reasons)
        {
            Verdict = verdict;
            Reasons = reasons;
        }
    }

    public sealed class PublishScopeAssessment
    {
        public bool SeparateBatchInsert { get; private set; }
        public bool GradeLoopUsesSeparateSelect { get; private set; }
        public bool BatchIdStartsAtZero { get; private set; }
        public bool UpdateOmitsItemType { get; private set; }
        public bool NoBeginOrCommit { get; private set; }
        public bool AllGradesInOneStatement { get; private set; }
        public List<string> Reasons { get; private set; }

        public bool DatabaseExecuted
        {
            get { return false; }
        }

        internal PublishScopeAssessment(bool separateBatchInsert, bool gradeLoopUsesSeparateSelect, bool batchIdStartsAtZero, bool updateOmitsItemType, bool noBeginOrCommit, bool allGradesInOneStatement, List<string> reasons)
        {
            SeparateBatchInsert = separateBatchInsert;
            GradeLoopUsesSeparateSelect = gradeLoopUsesSeparateSelect;
            BatchIdStartsAtZero = batchIdStartsAtZero;
            UpdateOmitsItemType = updateOmitsItemType;
            NoBeginOrCommit = noBeginOrCommit;
            AllGradesInOneStatement = allGradesInOneStatement;
            Reasons = reasons;
        }
    }

    public static class SemesterRankCatalog
    {
        public static bool DatabaseExecuted
        {
            get { return false; }
        }

        public static readonly string[] TableNames = new string[]
        {
            "rank_batch",
            "rank_batch_student",
            "rank_matrix",
            "rank_detail"
        };

        public static readonly string ReadOnlySql = @"
WITH found_table AS (
    SELECT
        c.oid,
        n.nspname,
        c.relname
    FROM
        pg_class AS c
        INNER JOIN pg_namespace AS n
            ON n.oid = c.relnamespace
    WHERE
        c.relkind IN ('r', 'p')
        AND c.relname IN ('rank_batch', 'rank_batch_student', 'rank_matrix', 'rank_detail')
        AND n.nspname <> 'information_schema'
        AND n.nspname NOT LIKE 'pg_%'
)
SELECT
    catalog.section,
    catalog.table_name,
    catalog.schema_name,
    catalog.object_kind,
    catalog.object_name,
    catalog.column_name,
    catalog.definition,
    catalog.is_unique,
    catalog.is_primary,
    catalog.is_partial,
    catalog.mentions_is_alive,
    catalog.column_not_null
FROM (
    SELECT
        'runtime'::text AS section,
        ''::text AS table_name,
        ''::text AS schema_name,
        'setting'::text AS object_kind,
        'search_path'::text AS object_name,
        ''::text AS column_name,
        current_setting('search_path') AS definition,
        false AS is_unique,
        false AS is_primary,
        false AS is_partial,
        false AS mentions_is_alive,
        false AS column_not_null
    UNION ALL
    SELECT
        'table'::text,
        expected.table_name,
        coalesce(found_table.nspname, ''),
        CASE WHEN found_table.oid IS NULL THEN 'missing-table' ELSE 'table' END,
        coalesce(found_table.nspname, ''),
        ''::text,
        ''::text,
        false,
        false,
        false,
        false,
        false
    FROM (
        VALUES
            ('rank_batch'),
            ('rank_batch_student'),
            ('rank_matrix'),
            ('rank_detail')
    ) AS expected(table_name)
        LEFT OUTER JOIN found_table
            ON found_table.relname = expected.table_name
    UNION ALL
    SELECT
        'column'::text,
        found_table.relname,
        found_table.nspname,
        'column'::text,
        a.attname,
        a.attname,
        format_type(a.atttypid, a.atttypmod),
        false,
        false,
        false,
        a.attname = 'is_alive',
        a.attnotnull
    FROM
        found_table
        INNER JOIN pg_attribute AS a
            ON a.attrelid = found_table.oid
            AND a.attnum > 0
            AND NOT a.attisdropped
    UNION ALL
    SELECT
        'constraint'::text,
        found_table.relname,
        found_table.nspname,
        CASE con.contype
            WHEN 'p' THEN 'primary key'
            WHEN 'u' THEN 'unique'
            WHEN 'f' THEN 'foreign key'
            WHEN 'c' THEN 'check'
            WHEN 'x' THEN 'exclusion'
            ELSE con.contype::text
        END,
        con.conname,
        coalesce((
            SELECT string_agg(a.attname, ',' ORDER BY cols.ord)
            FROM unnest(con.conkey) WITH ORDINALITY AS cols(attnum, ord)
                INNER JOIN pg_attribute AS a
                    ON a.attrelid = con.conrelid
                    AND a.attnum = cols.attnum
        ), ''),
        pg_get_constraintdef(con.oid),
        con.contype IN ('p', 'u'),
        con.contype = 'p',
        false,
        strpos(lower(coalesce(pg_get_constraintdef(con.oid), '')), 'is_alive') > 0,
        false
    FROM
        found_table
        INNER JOIN pg_constraint AS con
            ON con.conrelid = found_table.oid
    UNION ALL
    SELECT
        'index'::text,
        found_table.relname,
        found_table.nspname,
        CASE
            WHEN ix.indisprimary THEN 'primary key'
            WHEN ix.indisunique THEN 'unique index'
            ELSE 'index'
        END,
        index_rel.relname,
        coalesce((
            SELECT string_agg(a.attname, ',' ORDER BY cols.ord)
            FROM unnest(string_to_array(ix.indkey::text, ' ')) WITH ORDINALITY AS cols(attnum, ord)
                INNER JOIN pg_attribute AS a
                    ON a.attrelid = ix.indrelid
                    AND a.attnum = cols.attnum::integer
            WHERE
                cols.attnum <> '0'
        ), ''),
        pg_get_indexdef(ix.indexrelid),
        ix.indisunique,
        ix.indisprimary,
        ix.indpred IS NOT NULL,
        strpos(lower(coalesce(pg_get_indexdef(ix.indexrelid), '') || ' ' || coalesce(pg_get_expr(ix.indpred, ix.indrelid), '')), 'is_alive') > 0,
        false
    FROM
        found_table
        INNER JOIN pg_index AS ix
            ON ix.indrelid = found_table.oid
        INNER JOIN pg_class AS index_rel
            ON index_rel.oid = ix.indexrelid
) AS catalog
ORDER BY
    catalog.table_name,
    catalog.section,
    catalog.object_kind,
    catalog.object_name,
    catalog.column_name
";

        private static readonly string[] BatchRepeatedColumns = new string[]
        {
            "school_year", "semester", "calculation_description", "setting"
        };

        private static readonly string[] BatchStudentRepeatedColumns = new string[]
        {
            "ref_batch_id", "ref_student_id", "grade_year", "matrix_grade", "matrix_class", "matrix_tag1", "matrix_tag2"
        };

        private static readonly string[] DetailRepeatedColumns = new string[]
        {
            "ref_matrix_id", "ref_student_id", "score", "rank", "pr", "percentile"
        };

        public static CatalogAssessment Assess(IList<CatalogFact> facts)
        {
            if (facts == null)
                throw new ArgumentNullException("facts");
            List<string> reasons = new List<string>();
            List<string> stops = new List<string>();
            List<string> unknowns = new List<string>();
            if (facts.Count == 0)
            {
                unknowns.Add("沒有目錄列。這不是資料庫查詢結果。");
                return Finish(stops, unknowns, reasons);
            }

            foreach (string tableName in TableNames)
            {
                int schemas = SchemaCount(facts, tableName);
                bool missing = HasKind(facts, tableName, "missing-table");
                bool present = HasKind(facts, tableName, "table");
                if (!present || missing || schemas == 0)
                    unknowns.Add(tableName + " 沒有出現在非系統 schema。");
                else if (schemas > 1)
                    unknowns.Add(tableName + " 出現在多個 schema，無法由這份目錄決定 search_path 會使用哪一個。");
            }

            if (!HasColumn(facts, "rank_matrix", "is_alive"))
            {
                if (HasKind(facts, "rank_matrix", "table"))
                    stops.Add("rank_matrix 沒有 is_alive 欄位，現有語句無法把舊排名改成無效。");
            }
            else if (ColumnIsNotNull(facts, "rank_matrix", "is_alive"))
            {
                stops.Add("rank_matrix.is_alive 不允許 NULL。現有語句會把舊列的 is_alive 設成 NULL，該語句無法完成。");
            }

            foreach (CatalogFact fact in facts)
            {
                if (fact == null || fact.TableName != "rank_matrix")
                    continue;
                if (fact.ObjectKind != "check" || !fact.MentionsIsAlive)
                    continue;
                if (ContainsText(fact.Definition, "is_alive is not null"))
                    stops.Add("rank_matrix 的 CHECK 不允許 is_alive 為 NULL：" + fact.ObjectName);
            }

            foreach (CatalogFact fact in facts)
            {
                if (fact == null)
                    continue;
                if (fact.ObjectKind == "exclusion")
                {
                    unknowns.Add(fact.TableName + " 有 exclusion 限制 " + fact.ObjectName + "，這次無法判斷它是否允許新舊列並存。");
                    continue;
                }
                if (!fact.IsUnique)
                    continue;
                ClassifyUnique(fact, unknowns);
            }

            return Finish(stops, unknowns, reasons);
        }

        public static PublishScopeAssessment InspectPublishScope(string formSource)
        {
            if (formSource == null)
                throw new ArgumentNullException("formSource");
            bool separateBatch = formSource.IndexOf("queryHelper.Select(insertRankBatchSql)") >= 0;
            bool gradeLoop = formSource.IndexOf("// 依年級分批計算") >= 0;
            int rankSelect = formSource.IndexOf("queryHelper.Select(insertRankSql)");
            int build = formSource.IndexOf("SemesterRankPublishSql.Build(");
            bool batchZero = formSource.IndexOf("int batchID = 0;") >= 0;
            int updateAt = formSource.IndexOf(", update_data AS");
            int insertAt = formSource.IndexOf(", insert_matrix_data AS", updateAt < 0 ? 0 : updateAt);
            string updateBody = updateAt >= 0 && insertAt > updateAt
                ? formSource.Substring(updateAt, insertAt - updateAt)
                : "";
            bool omitsItemType = updateBody.Length > 0 && updateBody.IndexOf("item_type") < 0;
            bool noTransaction = formSource.IndexOf("BEGIN") < 0 && formSource.IndexOf("COMMIT") < 0;
            bool oneStatement = build >= 0 && rankSelect > build && !separateBatch && !gradeLoop && !batchZero && noTransaction;

            List<string> reasons = new List<string>();
            reasons.Add("表單先呼叫 SemesterRankPublishSql.Build，再只執行一次 queryHelper.Select。這是程式文字，尚未在 PostgreSQL 執行。");
            reasons.Add("rank_batch 的 INSERT ... RETURNING 與所有勾選年級的 calc_condition 都在這一句裡，不再使用批次識別 0。");
            reasons.Add("表單裡的停用範本仍未寫 item_type。組好的語句才加上 item_type LIKE '學期/%'。這尚未用資料庫確認不會碰到其他排名。");
            reasons.Add("語句沒有 BEGIN 或 COMMIT。QueryHelper.Select 不能接受多句指令稿。");
            reasons.Add("語句長度與閘道上限尚未量測。");
            reasons.Add("目錄尚未在 PostgreSQL 執行。單句能否涵蓋所有勾選年級，不能由 C# 編譯結果認定。");
            if (!oneStatement || !omitsItemType)
                reasons.Add("表單裡的公布流程與預期的文字位置不一致，需重新核對後才能沿用這份判斷。");
            return new PublishScopeAssessment(separateBatch, gradeLoop, batchZero, omitsItemType, noTransaction, oneStatement, reasons);
        }

        private static void ClassifyUnique(CatalogFact fact, List<string> unknowns)
        {
            string[] columns = SplitColumns(fact.ColumnName);
            if (columns.Length == 0)
            {
                unknowns.Add(fact.TableName + " 的唯一限制 " + fact.ObjectName + " 沒有可讀的欄位清單。");
                return;
            }
            if (fact.TableName == "rank_matrix")
                ClassifyMatrixUnique(fact, columns, unknowns);
            else if (fact.TableName == "rank_batch_student")
                ClassifyRepeatedUnique(fact, columns, BatchStudentRepeatedColumns, unknowns);
            else if (fact.TableName == "rank_detail")
                ClassifyRepeatedUnique(fact, columns, DetailRepeatedColumns, unknowns);
            else if (fact.TableName == "rank_batch")
                ClassifyRepeatedUnique(fact, columns, BatchRepeatedColumns, unknowns);
        }

        private static void ClassifyMatrixUnique(CatalogFact fact, string[] columns, List<string> unknowns)
        {
            if (fact.IsPartial)
            {
                if (IsAliveOnlyPredicate(fact.Definition))
                    return;
                unknowns.Add("rank_matrix 的部分唯一索引 " + fact.ObjectName + " 需要索引條件才能判斷 is_alive 為 NULL 的舊列是否仍占著唯一鍵。");
                return;
            }
            if (Contains(columns, "id"))
                return;
            if (Contains(columns, "is_alive"))
                return;
            if (Contains(columns, "ref_batch_id"))
            {
                unknowns.Add("rank_matrix 的唯一限制 " + fact.ObjectName + " 含 ref_batch_id。新舊列是否不同要看實際批次識別，不能只靠欄位名稱認定。");
                return;
            }
            unknowns.Add("rank_matrix 的唯一限制 " + fact.ObjectName + " 不能只靠欄位名稱認定新列必然與舊列衝突。需要比對實際插入鍵與仍然留在表上的列。");
        }

        private static void ClassifyRepeatedUnique(CatalogFact fact, string[] columns, string[] repeatedColumns, List<string> unknowns)
        {
            if (Contains(columns, "id"))
                return;
            if (fact.TableName == "rank_batch" && OnlyColumns(columns, repeatedColumns))
            {
                unknowns.Add("rank_batch 的唯一限制 " + fact.ObjectName + " 不能只靠欄位名稱認定再次插入必然失敗。需要該學年度學期是否已經有批次。");
                return;
            }
            if (fact.TableName == "rank_batch_student" && Contains(columns, "ref_student_id") && OnlyColumns(columns, repeatedColumns))
            {
                unknowns.Add("rank_batch_student 的唯一限制 " + fact.ObjectName + " 是一批次一學生。插入來自 score_list 且沒有 DISTINCT，同一學生有多列時鍵會重複，只有一列時不會。需要實際成績列才能判斷。");
                return;
            }
            if (fact.TableName == "rank_detail" && Contains(columns, "ref_matrix_id") && Contains(columns, "ref_student_id") && OnlyColumns(columns, repeatedColumns))
            {
                unknowns.Add("rank_detail 的唯一限制 " + fact.ObjectName + " 不會只因不同科目而失敗，不同科目的 ref_matrix_id 不同。同一科目被重複展開時才會產生相同鍵，需要實際科目內容才能判斷。");
                return;
            }
            if (fact.TableName == "rank_detail" && Contains(columns, "ref_student_id") && !Contains(columns, "ref_matrix_id"))
            {
                unknowns.Add("rank_detail 的唯一限制 " + fact.ObjectName + " 含 ref_student_id 且不含 ref_matrix_id。不同科目會共用這個學生鍵，但是否插入多列要看實際成績，不能因此認定插入必然失敗。");
                return;
            }
            unknowns.Add(fact.TableName + " 的唯一限制 " + fact.ObjectName + " 不能只靠欄位名稱認定插入必然失敗。需要實際插入鍵值才能判斷。");
        }

        private static CatalogAssessment Finish(List<string> stops, List<string> unknowns, List<string> reasons)
        {
            reasons.Add("這個判斷只解釋傳入的目錄列，沒有連線到 PostgreSQL。");
            if (stops.Count > 0)
            {
                reasons.AddRange(stops);
                reasons.AddRange(unknowns);
                reasons.Add("停止公布。不改寫舊列母群名稱，不刪除舊批次，不逐年級提交有效排名。");
                return new CatalogAssessment("stop", reasons);
            }
            if (unknowns.Count > 0)
            {
                reasons.AddRange(unknowns);
                reasons.Add("索引與約束尚未足以判斷新舊列能否並存。");
                return new CatalogAssessment("not-verified", reasons);
            }
            reasons.Add("依傳入的目錄列，舊的無效列與新的有效列可以同時存在。這不是學校資料庫的查詢結果。");
            return new CatalogAssessment("coexist", reasons);
        }

        private static bool IsAliveOnlyPredicate(string definition)
        {
            string text = (definition ?? "").ToLowerInvariant();
            if (text.IndexOf("is_alive") < 0)
                return false;
            if (text.IndexOf("is_alive is not null") >= 0)
                return true;
            if (text.IndexOf("is_alive = true") >= 0 || text.IndexOf("is_alive is true") >= 0)
                return true;
            return text.IndexOf("where (is_alive)") >= 0;
        }

        private static bool HasKind(IList<CatalogFact> facts, string tableName, string objectKind)
        {
            foreach (CatalogFact fact in facts)
            {
                if (fact != null && fact.TableName == tableName && fact.ObjectKind == objectKind)
                    return true;
            }
            return false;
        }

        private static int SchemaCount(IList<CatalogFact> facts, string tableName)
        {
            List<string> schemas = new List<string>();
            foreach (CatalogFact fact in facts)
            {
                if (fact == null || fact.TableName != tableName || fact.ObjectKind != "table")
                    continue;
                string schema = fact.SchemaName ?? "";
                if (schema.Length == 0 || Contains(schemas, schema))
                    continue;
                schemas.Add(schema);
            }
            return schemas.Count;
        }

        private static bool HasColumn(IList<CatalogFact> facts, string tableName, string columnName)
        {
            foreach (CatalogFact fact in facts)
            {
                if (fact != null && fact.TableName == tableName && fact.ObjectKind == "column" && fact.ColumnName == columnName)
                    return true;
            }
            return false;
        }

        private static bool ColumnIsNotNull(IList<CatalogFact> facts, string tableName, string columnName)
        {
            foreach (CatalogFact fact in facts)
            {
                if (fact != null && fact.TableName == tableName && fact.ObjectKind == "column" && fact.ColumnName == columnName && fact.ColumnNotNull)
                    return true;
            }
            return false;
        }

        private static string[] SplitColumns(string columns)
        {
            if (string.IsNullOrEmpty(columns))
                return new string[0];
            string[] parts = columns.Split(',');
            List<string> names = new List<string>();
            foreach (string part in parts)
            {
                string name = part.Trim();
                if (name.Length > 0)
                    names.Add(name);
            }
            return names.ToArray();
        }

        private static bool OnlyColumns(string[] columns, string[] allowed)
        {
            foreach (string column in columns)
            {
                if (!Contains(allowed, column))
                    return false;
            }
            return true;
        }

        private static bool Contains(IList<string> names, string value)
        {
            foreach (string name in names)
            {
                if (name == value)
                    return true;
            }
            return false;
        }

        private static bool ContainsText(string text, string value)
        {
            return (text ?? "").ToLowerInvariant().IndexOf(value) >= 0;
        }
    }
}
