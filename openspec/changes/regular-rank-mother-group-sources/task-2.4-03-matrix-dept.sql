-- Task 2.4 區段 C：matrix_dept 彙總。
-- 只執行本檔。欄位不存在，或型別不是 text、varchar、bpchar 時停止本區段。
-- 空白與 NULL 分開計。兩者都不是完整科母群，也不代表原始參與旗標。
-- 允許清單是 1admin_coredev4 與 1admin_debug1。READ ONLY。沒有資料寫入、DDL 或 COMMIT。

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

DO $$
DECLARE
    type_name TEXT;
    missing_columns TEXT;
BEGIN
    SELECT t.typname
    INTO type_name
    FROM pg_class AS c
    INNER JOIN pg_namespace AS n
        ON n.oid = c.relnamespace
    INNER JOIN pg_attribute AS a
        ON a.attrelid = c.oid
    INNER JOIN pg_type AS t
        ON t.oid = a.atttypid
    WHERE n.nspname = 'public'
        AND c.relname = 'rank_batch_student'
        AND a.attname = 'matrix_dept'
        AND a.attnum > 0
        AND NOT a.attisdropped;
    IF type_name IS NULL THEN
        RAISE EXCEPTION 'stop: rank_batch_student.matrix_dept does not exist';
    END IF;
    IF type_name NOT IN ('text', 'varchar', 'bpchar') THEN
        RAISE EXCEPTION 'stop: rank_batch_student.matrix_dept type is %, this section only accepts text, varchar, or bpchar', type_name;
    END IF;

    SELECT string_agg(required_column.label, ', ' ORDER BY required_column.label)
    INTO missing_columns
    FROM (
        SELECT 'rank_matrix.ref_batch_id' AS label, 'rank_matrix' AS table_name, 'ref_batch_id' AS column_name
        UNION ALL SELECT 'rank_matrix.item_type', 'rank_matrix', 'item_type'
        UNION ALL SELECT 'rank_batch_student.ref_batch_id', 'rank_batch_student', 'ref_batch_id'
    ) AS required_column
    WHERE NOT EXISTS (
        SELECT 1
        FROM pg_class AS c
        INNER JOIN pg_namespace AS n
            ON n.oid = c.relnamespace
        INNER JOIN pg_attribute AS a
            ON a.attrelid = c.oid
        WHERE n.nspname = 'public'
            AND c.relname = required_column.table_name
            AND a.attname = required_column.column_name
            AND a.attnum > 0
            AND NOT a.attisdropped
    );
    IF missing_columns IS NOT NULL THEN
        RAISE EXCEPTION 'stop: matrix_dept section missing columns: %', missing_columns;
    END IF;
END $$;

-- C1. 只統計 item_type 為定期評量的批次。不輸出科母群名稱。
WITH regular_batch AS (
    SELECT DISTINCT
        ref_batch_id
    FROM
        rank_matrix
    WHERE
        item_type LIKE '定期評量/%'
),
student_dept AS (
    SELECT
        student.ref_batch_id,
        count(*) AS student_rows,
        count(*) FILTER (WHERE student.matrix_dept IS NULL) AS dept_null,
        count(*) FILTER (WHERE student.matrix_dept IS NOT NULL AND btrim(student.matrix_dept::TEXT) = '') AS dept_blank,
        count(*) FILTER (WHERE student.matrix_dept IS NOT NULL AND btrim(student.matrix_dept::TEXT) <> '') AS dept_present
    FROM
        rank_batch_student AS student
        INNER JOIN regular_batch
            ON regular_batch.ref_batch_id = student.ref_batch_id
    GROUP BY
        student.ref_batch_id
)
SELECT
    count(*) AS regular_batches_with_students,
    coalesce(sum(student_rows), 0) AS student_rows,
    coalesce(sum(dept_null), 0) AS dept_null_rows,
    coalesce(sum(dept_blank), 0) AS dept_blank_rows,
    coalesce(sum(dept_present), 0) AS dept_present_rows,
    count(*) FILTER (WHERE dept_present = 0) AS batches_with_no_dept_name,
    count(*) FILTER (WHERE dept_present > 0 AND dept_present < student_rows) AS batches_with_partial_dept_name,
    count(*) FILTER (WHERE student_rows > 0 AND dept_present = student_rows) AS batches_with_dept_name_on_every_row
FROM
    student_dept;

-- C2. 有定期 item、但沒有 rank_batch_student 的批次數。沒有學生列不能降級還原。
WITH regular_batch AS (
    SELECT DISTINCT
        ref_batch_id
    FROM
        rank_matrix
    WHERE
        item_type LIKE '定期評量/%'
)
SELECT
    count(*) AS regular_batches_without_student_rows
FROM
    regular_batch
WHERE
    NOT EXISTS (
        SELECT 1
        FROM rank_batch_student AS student
        WHERE student.ref_batch_id = regular_batch.ref_batch_id
    );

ROLLBACK;
