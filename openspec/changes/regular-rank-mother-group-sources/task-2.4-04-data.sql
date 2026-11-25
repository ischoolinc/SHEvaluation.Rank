-- Task 2.4 區段 D：設定 XML、定期 item、is_alive 與考試彙總。
-- 只執行本檔。必要欄位或 setting 型別不符時，D0 的常數檢查之後會停止，不會解析資料表裡的 setting。
-- xpath 與 ::XML 只放在 xml_is_well_formed_document 為真的 CASE 分支。
-- PostgreSQL 的 CASE 不計算用不到的分支，所以純文字、多根、損壞、空白與 NULL 不會進入解析。
-- 這些常數情境本輪沒有在 PostgreSQL 執行。
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

-- D0. 常數情境。預期尚未在 PostgreSQL 實測：
-- valid_document：is_document true，exam_name 為期中考。
-- plain_text、multiple_roots、damaged_xml、blank：is_document false，exam_name 為 NULL。
-- null_input：is_document 為 NULL，exam_name 為 NULL。
-- 真實資料列不會再細分純文字、多根與損壞，只併入 not_document。
SELECT
    sample.kind,
    xml_is_well_formed_document(sample.value) AS is_document,
    CASE
        WHEN xml_is_well_formed_document(sample.value)
            THEN (xpath('/*/@考試名稱', sample.value::XML))[1]::TEXT
        ELSE NULL
    END AS exam_name
FROM (
    SELECT 'valid_document'::TEXT AS kind, '<Setting 考試名稱="期中考"/>'::TEXT AS value
    UNION ALL SELECT 'plain_text', 'plain text'
    UNION ALL SELECT 'multiple_roots', '<a/><b/>'
    UNION ALL SELECT 'damaged_xml', '<a>'
    UNION ALL SELECT 'blank', ''
    UNION ALL SELECT 'null_input', NULL::TEXT
) AS sample;

-- D1. 資料表前置條件。setting 必須是文字或 xml，否則停止，不讀取內容。
DO $$
DECLARE
    setting_type TEXT;
    missing_columns TEXT;
BEGIN
    SELECT t.typname
    INTO setting_type
    FROM pg_class AS c
    INNER JOIN pg_namespace AS n
        ON n.oid = c.relnamespace
    INNER JOIN pg_attribute AS a
        ON a.attrelid = c.oid
    INNER JOIN pg_type AS t
        ON t.oid = a.atttypid
    WHERE n.nspname = 'public'
        AND c.relname = 'rank_batch'
        AND a.attname = 'setting'
        AND a.attnum > 0
        AND NOT a.attisdropped;
    IF setting_type IS NULL THEN
        RAISE EXCEPTION 'stop: rank_batch.setting does not exist';
    END IF;
    IF setting_type NOT IN ('text', 'varchar', 'bpchar', 'xml') THEN
        RAISE EXCEPTION 'stop: rank_batch.setting type is %, this section only accepts text, varchar, bpchar, or xml', setting_type;
    END IF;

    SELECT string_agg(required_column.label, ', ' ORDER BY required_column.label)
    INTO missing_columns
    FROM (
        SELECT 'rank_batch.id' AS label, 'rank_batch' AS table_name, 'id' AS column_name
        UNION ALL SELECT 'rank_batch.school_year', 'rank_batch', 'school_year'
        UNION ALL SELECT 'rank_batch.semester', 'rank_batch', 'semester'
        UNION ALL SELECT 'rank_matrix.ref_batch_id', 'rank_matrix', 'ref_batch_id'
        UNION ALL SELECT 'rank_matrix.ref_exam_id', 'rank_matrix', 'ref_exam_id'
        UNION ALL SELECT 'rank_matrix.item_type', 'rank_matrix', 'item_type'
        UNION ALL SELECT 'rank_matrix.is_alive', 'rank_matrix', 'is_alive'
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
        RAISE EXCEPTION 'stop: data section missing columns: %', missing_columns;
    END IF;
END $$;

-- D2. 定期 item 粗分類。沒有矩陣列的批次不在這裡。
SELECT
    count(*) AS matrix_rows,
    count(DISTINCT ref_batch_id) AS batch_count,
    count(*) FILTER (WHERE item_type LIKE '定期評量/%') AS regular_item_rows,
    count(DISTINCT ref_batch_id) FILTER (WHERE item_type LIKE '定期評量/%') AS regular_item_batches,
    count(*) FILTER (WHERE item_type LIKE '學期/%') AS semester_item_rows,
    count(*) FILTER (WHERE item_type NOT LIKE '定期評量/%' AND item_type NOT LIKE '學期/%') AS other_item_rows
FROM
    rank_matrix;

-- D3. 定期 item 的 is_alive 彙總。NULL、true 與其他值分開，不把 NULL 當成可還原旗標。
SELECT
    count(*) FILTER (WHERE is_alive IS TRUE) AS alive_true,
    count(*) FILTER (WHERE is_alive IS NOT TRUE AND is_alive IS NOT NULL) AS alive_not_true,
    count(*) FILTER (WHERE is_alive IS NULL) AS alive_null,
    count(DISTINCT ref_batch_id) FILTER (WHERE is_alive IS TRUE) AS batches_with_alive_true,
    count(DISTINCT ref_batch_id) AS batches_with_regular_item
FROM
    rank_matrix
WHERE
    item_type LIKE '定期評量/%';

-- D4. setting 分類與屬性彙總。只有單根文件才進入 xpath。不輸出 XML 或考試名稱清單。
WITH regular_batch AS (
    SELECT DISTINCT
        ref_batch_id
    FROM
        rank_matrix
    WHERE
        item_type LIKE '定期評量/%'
),
classified AS (
    SELECT
        CASE
            WHEN batch.setting IS NULL THEN 'null'
            WHEN btrim(batch.setting::TEXT) = '' THEN 'blank'
            WHEN xml_is_well_formed_document(batch.setting::TEXT) THEN 'document'
            ELSE 'not_document'
        END AS setting_class,
        CASE
            WHEN xml_is_well_formed_document(batch.setting::TEXT)
                THEN (xpath('/*/@考試名稱', batch.setting::XML))[1]::TEXT
            ELSE NULL
        END AS exam_name,
        CASE
            WHEN xml_is_well_formed_document(batch.setting::TEXT)
                THEN (xpath('/*/@ref_exam_id', batch.setting::XML))[1]::TEXT
            ELSE NULL
        END AS setting_ref_exam_id
    FROM
        rank_batch AS batch
        INNER JOIN regular_batch
            ON regular_batch.ref_batch_id = batch.id
)
SELECT
    count(*) AS regular_item_batches,
    count(*) FILTER (WHERE setting_class = 'null') AS setting_null,
    count(*) FILTER (WHERE setting_class = 'blank') AS setting_blank,
    count(*) FILTER (WHERE setting_class = 'not_document') AS setting_not_document,
    count(*) FILTER (WHERE setting_class = 'document') AS setting_document,
    count(*) FILTER (WHERE setting_class = 'document' AND (exam_name IS NULL OR btrim(exam_name) = '')) AS document_exam_name_missing,
    count(*) FILTER (WHERE setting_class = 'document' AND exam_name = '學期成績') AS document_exam_name_is_semester_literal,
    count(*) FILTER (WHERE setting_class = 'document' AND (setting_ref_exam_id IS NULL OR btrim(setting_ref_exam_id) = '')) AS document_ref_exam_id_missing,
    count(*) FILTER (WHERE setting_class = 'document' AND setting_ref_exam_id IS NOT NULL AND btrim(setting_ref_exam_id) <> '') AS document_ref_exam_id_present
FROM
    classified;

-- D5. 同期間、同考試名稱是否對到多個矩陣 exam ID。不是單根文件的批次不進入分組。
WITH regular_matrix AS (
    SELECT
        ref_batch_id,
        ref_exam_id
    FROM
        rank_matrix
    WHERE
        item_type LIKE '定期評量/%'
),
classified AS (
    SELECT
        batch.id,
        batch.school_year,
        batch.semester,
        xml_is_well_formed_document(batch.setting::TEXT) IS TRUE AS is_document,
        CASE
            WHEN xml_is_well_formed_document(batch.setting::TEXT)
                THEN (xpath('/*/@考試名稱', batch.setting::XML))[1]::TEXT
            ELSE NULL
        END AS exam_name
    FROM
        rank_batch AS batch
    WHERE
        EXISTS (
            SELECT 1
            FROM regular_matrix
            WHERE regular_matrix.ref_batch_id = batch.id
        )
),
grouped AS (
    SELECT
        classified.school_year,
        classified.semester,
        classified.exam_name,
        count(DISTINCT classified.id) AS batch_count,
        count(DISTINCT regular_matrix.ref_exam_id) AS distinct_exam_ids
    FROM
        classified
        INNER JOIN regular_matrix
            ON regular_matrix.ref_batch_id = classified.id
    WHERE
        classified.is_document
    GROUP BY
        classified.school_year,
        classified.semester,
        classified.exam_name
)
SELECT
    (SELECT count(*) FROM classified WHERE NOT is_document) AS regular_batches_excluded_not_document,
    count(*) AS document_name_groups,
    count(*) FILTER (WHERE batch_count > 1) AS groups_with_multiple_batches,
    count(*) FILTER (WHERE distinct_exam_ids > 1) AS groups_with_multiple_exam_ids
FROM
    grouped;

-- D6. 單一定期批次的矩陣有多個 ref_exam_id。
SELECT
    count(*) AS regular_batches_with_multiple_exam_ids
FROM (
    SELECT
        ref_batch_id
    FROM
        rank_matrix
    WHERE
        item_type LIKE '定期評量/%'
    GROUP BY
        ref_batch_id
    HAVING
        count(DISTINCT ref_exam_id) > 1
) AS multiple_exam;

-- D7. 沒有矩陣的批次不能用 item_type 辨識成定期批次。
SELECT
    count(*) AS batches_without_matrix
FROM
    rank_batch AS batch
WHERE
    NOT EXISTS (
        SELECT 1
        FROM rank_matrix
        WHERE rank_matrix.ref_batch_id = batch.id
    );

-- D8. exam 不存在時停止本區段。前面的彙總已經回傳；不要改查其他考試來源。
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_class AS c
        INNER JOIN pg_namespace AS n
            ON n.oid = c.relnamespace
        WHERE n.nspname = 'public'
            AND c.relname = 'exam'
            AND c.relkind IN ('r', 'p')
    ) THEN
        RAISE EXCEPTION 'stop: public.exam does not exist';
    END IF;
END $$;

-- D9. 同名考試有多個 ID 的名稱數。不輸出考試名稱。
SELECT
    count(*) AS exam_names_with_multiple_ids
FROM (
    SELECT
        exam_name
    FROM
        exam
    GROUP BY
        exam_name
    HAVING
        count(*) > 1
) AS duplicate_name;

ROLLBACK;
