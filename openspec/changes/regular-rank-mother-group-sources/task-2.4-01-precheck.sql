-- Task 2.4 區段 A：環境與結構預檢。
-- 只執行本檔，不要和區段 B、C、D 接成一次執行。
-- 允許清單是 1admin_coredev4 與 1admin_debug1。不要放寬，不要加入連線字串。
-- READ ONLY。沒有 INSERT、UPDATE、DELETE、DDL、COMMIT。
-- 結果只保留彙總與系統目錄。不要輸出密碼、連線字串、學生姓名、學號或成績。

BEGIN TRANSACTION READ ONLY;

DO $$
BEGIN
    IF current_setting('transaction_read_only') IS DISTINCT FROM 'on' THEN
        RAISE EXCEPTION 'stop: transaction is not read only';
    END IF;
    IF current_database() NOT IN ('1admin_coredev4', '1admin_debug1') THEN
        RAISE EXCEPTION 'stop: database % is not a previously confirmed independent test database', current_database();
    END IF;
END $$;

-- A1. 環境識別。
SELECT
    current_database() AS database_name,
    current_user AS login_role,
    current_setting('transaction_read_only') AS transaction_read_only,
    current_setting('server_version') AS server_version,
    current_setting('server_version_num') AS server_version_num,
    current_setting('server_version_num')::INT / 10000 AS server_version_major;

-- A2. 四表與 exam 是否在 public。relkind r 是一般表，p 是分割父表。
SELECT
    c.relname AS table_name,
    c.relkind,
    c.reltuples::BIGINT AS planner_estimate
FROM
    pg_class AS c
    INNER JOIN pg_namespace AS n
        ON n.oid = c.relnamespace
WHERE
    n.nspname = 'public'
    AND c.relname IN ('rank_batch', 'rank_batch_student', 'rank_matrix', 'rank_detail', 'exam')
ORDER BY
    c.relname;

-- A3. 欄位、型別、NOT NULL、預設值。沒有出現的欄位就是不存在。
SELECT
    c.relname AS table_name,
    a.attnum,
    a.attname AS column_name,
    format_type(a.atttypid, a.atttypmod) AS data_type,
    t.typname AS type_name,
    a.attnotnull AS not_null,
    pg_get_expr(ad.adbin, ad.adrelid) AS column_default
FROM
    pg_class AS c
    INNER JOIN pg_namespace AS n
        ON n.oid = c.relnamespace
    INNER JOIN pg_attribute AS a
        ON a.attrelid = c.oid
    INNER JOIN pg_type AS t
        ON t.oid = a.atttypid
    LEFT JOIN pg_attrdef AS ad
        ON ad.adrelid = a.attrelid
        AND ad.adnum = a.attnum
WHERE
    n.nspname = 'public'
    AND c.relname IN ('rank_batch', 'rank_batch_student', 'rank_matrix', 'rank_detail')
    AND a.attnum > 0
    AND NOT a.attisdropped
ORDER BY
    c.relname,
    a.attnum;

-- A4. 主鍵、外鍵、唯一、檢查與排除限制。
SELECT
    c.relname AS table_name,
    con.conname AS constraint_name,
    con.contype AS constraint_type,
    pg_get_constraintdef(con.oid, true) AS definition,
    strpos(lower(pg_get_constraintdef(con.oid, true)), 'is_alive') > 0 AS mentions_is_alive,
    strpos(lower(pg_get_constraintdef(con.oid, true)), 'item_type') > 0 AS mentions_item_type,
    strpos(lower(pg_get_constraintdef(con.oid, true)), 'ref_exam_id') > 0 AS mentions_ref_exam_id
FROM
    pg_constraint AS con
    INNER JOIN pg_class AS c
        ON c.oid = con.conrelid
    INNER JOIN pg_namespace AS n
        ON n.oid = c.relnamespace
WHERE
    n.nspname = 'public'
    AND c.relname IN ('rank_batch', 'rank_batch_student', 'rank_matrix', 'rank_detail')
ORDER BY
    c.relname,
    con.contype,
    con.conname;

-- A5. 索引，包含部分唯一索引條件。
SELECT
    c.relname AS table_name,
    ic.relname AS index_name,
    ix.indisprimary AS is_primary,
    ix.indisunique AS is_unique,
    ix.indisexclusion AS is_exclusion,
    pg_get_indexdef(ix.indexrelid) AS index_definition,
    pg_get_expr(ix.indpred, ix.indrelid) AS index_predicate,
    strpos(
        lower(
            pg_get_indexdef(ix.indexrelid)
            || ' '
            || coalesce(pg_get_expr(ix.indpred, ix.indrelid), '')
        ),
        'is_alive'
    ) > 0 AS mentions_is_alive
FROM
    pg_index AS ix
    INNER JOIN pg_class AS c
        ON c.oid = ix.indrelid
    INNER JOIN pg_class AS ic
        ON ic.oid = ix.indexrelid
    INNER JOIN pg_namespace AS n
        ON n.oid = c.relnamespace
WHERE
    n.nspname = 'public'
    AND c.relname IN ('rank_batch', 'rank_batch_student', 'rank_matrix', 'rank_detail')
ORDER BY
    c.relname,
    ic.relname;

-- A6. 使用者建立的 trigger。
SELECT
    c.relname AS table_name,
    tg.tgname AS trigger_name,
    pg_get_triggerdef(tg.oid, true) AS trigger_definition
FROM
    pg_trigger AS tg
    INNER JOIN pg_class AS c
        ON c.oid = tg.tgrelid
    INNER JOIN pg_namespace AS n
        ON n.oid = c.relnamespace
WHERE
    n.nspname = 'public'
    AND c.relname IN ('rank_batch', 'rank_batch_student', 'rank_matrix', 'rank_detail')
    AND NOT tg.tgisinternal
ORDER BY
    c.relname,
    tg.tgname;

-- A7. 後續區段怎麼選。這裡只查系統目錄，不讀 extension、matrix_dept 或 setting 的內容。
WITH column_type AS (
    SELECT
        c.relname AS table_name,
        a.attname AS column_name,
        t.typname AS type_name
    FROM
        pg_class AS c
        INNER JOIN pg_namespace AS n
            ON n.oid = c.relnamespace
        INNER JOIN pg_attribute AS a
            ON a.attrelid = c.oid
        INNER JOIN pg_type AS t
            ON t.oid = a.atttypid
    WHERE
        n.nspname = 'public'
        AND c.relname IN ('rank_batch', 'rank_batch_student', 'rank_matrix')
        AND a.attnum > 0
        AND NOT a.attisdropped
        AND a.attname IN (
            'extension', 'setting', 'id', 'school_year', 'semester',
            'matrix_dept', 'ref_batch_id', 'ref_exam_id', 'item_type', 'is_alive'
        )
),
presence AS (
    SELECT
        (
            SELECT type_name
            FROM column_type
            WHERE table_name = 'rank_batch' AND column_name = 'extension'
        ) AS extension_type_name,
        (
            SELECT type_name
            FROM column_type
            WHERE table_name = 'rank_batch_student' AND column_name = 'matrix_dept'
        ) AS matrix_dept_type_name,
        (
            SELECT type_name
            FROM column_type
            WHERE table_name = 'rank_batch' AND column_name = 'setting'
        ) AS setting_type_name
)
SELECT
    presence.extension_type_name,
    CASE
        WHEN presence.extension_type_name IS NULL THEN 'do_not_run_extension_section'
        WHEN presence.extension_type_name = 'jsonb' THEN 'task-2.4-02-extension-jsonb.sql'
        WHEN presence.extension_type_name = 'json' THEN 'task-2.4-02-extension-json.sql'
        WHEN presence.extension_type_name IN ('text', 'varchar', 'bpchar') THEN 'task-2.4-02-extension-text.sql'
        ELSE 'do_not_run_extension_section'
    END AS extension_section_file,
    presence.matrix_dept_type_name,
    CASE
        WHEN presence.matrix_dept_type_name IS NULL THEN 'do_not_run_matrix_dept_section'
        WHEN presence.matrix_dept_type_name IN ('text', 'varchar', 'bpchar') THEN 'task-2.4-03-matrix-dept.sql'
        ELSE 'do_not_run_matrix_dept_section'
    END AS matrix_dept_section_file,
    presence.setting_type_name,
    (
        SELECT count(*)
        FROM (
            SELECT 'rank_batch' AS table_name, 'id' AS column_name
            UNION ALL SELECT 'rank_batch', 'school_year'
            UNION ALL SELECT 'rank_batch', 'semester'
            UNION ALL SELECT 'rank_batch', 'setting'
            UNION ALL SELECT 'rank_matrix', 'ref_batch_id'
            UNION ALL SELECT 'rank_matrix', 'ref_exam_id'
            UNION ALL SELECT 'rank_matrix', 'item_type'
            UNION ALL SELECT 'rank_matrix', 'is_alive'
            UNION ALL SELECT 'rank_batch_student', 'ref_batch_id'
        ) AS required_column
        WHERE NOT EXISTS (
            SELECT 1
            FROM column_type
            WHERE column_type.table_name = required_column.table_name
                AND column_type.column_name = required_column.column_name
        )
    ) AS missing_data_column_count,
    EXISTS (
        SELECT 1
        FROM pg_class AS c
        INNER JOIN pg_namespace AS n
            ON n.oid = c.relnamespace
        WHERE n.nspname = 'public'
            AND c.relname = 'exam'
            AND c.relkind IN ('r', 'p')
    ) AS exam_table_exists
FROM
    presence;

-- A8. 四表都存在才計算精確列數。缺表時停止本區段，不要改查其他來源。
DO $$
DECLARE
    missing_tables TEXT;
BEGIN
    SELECT string_agg(required_name, ', ' ORDER BY required_name)
    INTO missing_tables
    FROM (
        SELECT 'rank_batch' AS required_name
        UNION ALL SELECT 'rank_batch_student'
        UNION ALL SELECT 'rank_matrix'
        UNION ALL SELECT 'rank_detail'
    ) AS required_table
    WHERE NOT EXISTS (
        SELECT 1
        FROM pg_class AS c
        INNER JOIN pg_namespace AS n
            ON n.oid = c.relnamespace
        WHERE n.nspname = 'public'
            AND c.relname = required_table.required_name
            AND c.relkind IN ('r', 'p')
    );
    IF missing_tables IS NOT NULL THEN
        RAISE EXCEPTION 'stop: precheck row counts require tables: %', missing_tables;
    END IF;
END $$;

SELECT
    'rank_batch' AS table_name,
    count(*) AS row_count
FROM
    rank_batch
UNION ALL
SELECT
    'rank_batch_student',
    count(*)
FROM
    rank_batch_student
UNION ALL
SELECT
    'rank_matrix',
    count(*)
FROM
    rank_matrix
UNION ALL
SELECT
    'rank_detail',
    count(*)
FROM
    rank_detail
ORDER BY
    table_name;

ROLLBACK;
