# new_debug6 發布介面查核

## 已確認的現象

2026-11-05 23:18 左右，1admin_new_debug6 的 adminquery 回傳 DSA 504「執行資料庫查詢時發生錯誤」。目前無 SQLSTATE，無法判斷是 SQL 執行、權限、服務包裝，或回傳資料解析失敗。不能直接下結論為 PostgreSQL 不支援。

實際資料庫名稱為 1admin_debug6，版本 170011。一般 SELECT、pg_catalog 欄位／索引查詢及大型成績唯讀試算都成功。114-2／exam ID 5／三年級 687 人的全參與假設，新舊各 30,764 筆，逐欄 EXCEPT ALL 缺少 0、多出 0。

以下兩條路徑都沒有取得成功證據，尚未執行排名發布或保存組態：

1. BEGIN SERIALIZABLE、SELECT 隔離層級、ROLLBACK 的完整腳本。
2. 單一 DO 匿名區塊，只檢查 read committed、取得交易 advisory lock、PERFORM 1；不新增 schema 或排名資料。

舊版通用錯誤報告只展開 AggregateException 的第一個分支，故原始紀錄不能提供匿名區塊的獨立錯誤內容。工作區已補上兩條路徑分開記錄，尚未換載這次修正，不要求使用者為此立即重登入。

## 請維護人員協助確認

- 查 23:18 左右該 adminquery 要求的底層錯誤、SQLSTATE、拒絕階段。請先移除登入資料、session、token 及學生資料。
- SQL 是否被服務包成子查詢、XML 聚合或其他 SELECT；是否只接受有結果欄位的單一查詢。
- 若 DO 已執行但因沒有結果集而回報錯誤，能否提供正式支援的非查詢執行方式與可確認的完成回應。
- 若提供多語句交易，是否保證同一個實體資料庫連線、整段同成同敗、錯誤回滾、無寫入自動重試。
- 若以上皆不可行，請提供可在測試環境驗證的後端發布服務方案。不要為繞過問題直接新增表、函式或把年級拆成多次提交。

## 無學生資料的重現腳本

僅在已授權的 new_debug6，以目前產品呼叫的同一個 adminquery 介面分別送出，不要混成一個要求。回報各次結果／錯誤與伺服器紀錄即可，勿提供憑證。

```sql
SELECT current_database() AS database_name,
       current_setting('server_version_num') AS server_version,
       current_setting('transaction_isolation') AS isolation_level;
```

```sql
BEGIN TRANSACTION ISOLATION LEVEL SERIALIZABLE;
SELECT current_database() AS database_name,
       current_setting('transaction_isolation') AS isolation_level;
ROLLBACK;
```

```sql
DO $rank_probe$
BEGIN
  IF current_setting('transaction_isolation') <> 'read committed' THEN
    RAISE EXCEPTION 'regular-rank requires read committed';
  END IF;
  PERFORM pg_advisory_xact_lock(1381126731, 170011);
  PERFORM 1;
END
$rank_probe$;
```

交易鎖會於該次交易結束時釋放。上述腳本不寫排名資料，但仍須確認服務會結束／回滾交易。若服務不支援交易控制，不要改成分三次要求，因為無法假定三次共用連線。

## 後續驗收

先證明介面執行契約，再驗證整次發布、四表筆數、五旗標及統計、零明細年級、失敗回滾、兩獨立連線重疊發布與回應中斷核對。只通過探測不會自動代表這些驗收完成。
