-- Task 2.4 區段 B，只給 rank_batch.extension 型別 text、varchar 或 bpchar。
-- 只執行本檔。型別不符或欄位不存在時停止本區段。
-- 不把文字轉成 json 或 jsonb，避免壞值中止交易。前綴只是外觀計數，不是有效 JSON。
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
        AND c.relname = 'rank_batch'
        AND a.attname = 'extension'
        AND a.attnum > 0
        AND NOT a.attisdropped;
    IF type_name IS NULL THEN
        RAISE EXCEPTION 'stop: rank_batch.extension does not exist';
    END IF;
    IF type_name NOT IN ('text', 'varchar', 'bpchar') THEN
        RAISE EXCEPTION 'stop: rank_batch.extension type is %, this section only accepts text, varchar, or bpchar', type_name;
    END IF;
END $$;

-- B1. 文字外觀彙總。不解析 JSON，也不輸出內容。
SELECT
    count(*) AS batch_count,
    count(*) FILTER (WHERE extension IS NULL) AS extension_null,
    count(*) FILTER (WHERE extension IS NOT NULL AND btrim(extension::TEXT) = '') AS extension_blank,
    count(*) FILTER (WHERE extension IS NOT NULL AND left(btrim(extension::TEXT), 1) = '[') AS text_leading_bracket,
    count(*) FILTER (
        WHERE extension IS NOT NULL
            AND btrim(extension::TEXT) <> ''
            AND left(btrim(extension::TEXT), 1) IS DISTINCT FROM '['
    ) AS text_other
FROM
    rank_batch;

ROLLBACK;
