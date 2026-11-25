-- Task 2.4 區段 B，只給 rank_batch.extension 型別 json。
-- 只執行本檔。型別不符或欄位不存在時停止本區段。
-- 本區段不把 json 轉成 jsonb，也不檢查 MotherGroupSnapshot。
-- 沒有快照計數不代表快照不存在，也不代表筆數是 0。快照包含檢查只在 jsonb 區段。
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
    IF type_name <> 'json' THEN
        RAISE EXCEPTION 'stop: rank_batch.extension type is %, this section only accepts json', type_name;
    END IF;
END $$;

-- B1. json 彙總。只使用 json_typeof，不轉換整份 JSON。
-- 不輸出 MotherGroupSnapshot 計數；未檢查不能記成不存在或 0。
SELECT
    count(*) AS batch_count,
    count(*) FILTER (WHERE extension IS NULL) AS extension_null,
    count(*) FILTER (WHERE extension IS NOT NULL AND json_typeof(extension) = 'array') AS json_array,
    count(*) FILTER (WHERE extension IS NOT NULL AND json_typeof(extension) IS DISTINCT FROM 'array') AS json_not_array
FROM
    rank_batch;

ROLLBACK;
