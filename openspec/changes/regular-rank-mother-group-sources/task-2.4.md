# Task 2.4 獨立測試庫唯讀核對

狀態：未完成。本輪只移除 json 備援區段的 `jsonb` 轉換與快照名稱判斷，沒有連上任何資料庫，Task 2.4 維持未勾選。沒有修改正式程式、共用類別、`baseline/source` 或學期 Change，沒有執行排名、寫入、DDL 或資料庫設定。沒有開始 Task 3.1。

工作目錄 `C:\ischoolProject\SHEvaluation-Regular`。下面的程式位置是靜態閱讀，不是資料庫實測。

## 連線確認

本輪仍然沒有使用連線，也沒有放寬允許清單：

- `App.config` 與 `SHEvaluation.Rank.dll.config` 只有執行階段版本，沒有連線字串。
- 行程環境沒有 `PGHOST`、`PGPORT`、`PGUSER`、`PGDATABASE`、`PGSERVICE`、`PGSERVICEFILE`、`PGPASSFILE`、`DATABASE_URL`。
- `psql` 不在 PATH。`%APPDATA%\postgresql` 沒有 `pgpass.conf` 或 `pg_service.conf`。
- 每個 SQL 區段只接受 `1admin_coredev4` 與 `1admin_debug1`。這兩個名稱來自學期 Change 裡已經確認的測試庫，本輪沒有登入。
- 學期 Task 11.2 記錄本機桌面登入點是 `1admin_debug9`。那不是允許清單，本輪沒有使用。

缺少的資訊仍是：一個已經開啟、而且 `current_database()` 屬於允許清單的唯讀工作階段。沒有這項之前，不能補連線、不能猜帳號，也不能把學校正式庫拿來查。

## 執行方式

每次只執行一個區段檔。不要把多個檔案接成一次執行，也不要在同一個檔案裡跳過語句。每個檔案自己包含 `BEGIN TRANSACTION READ ONLY`、允許清單檢查與 `ROLLBACK`。

1. 執行 `task-2.4-01-precheck.sql`。A7 會依系統目錄給出下一檔名稱。A8 只有四張 `rank_*` 表都存在才計算列數；缺表時該區段停止。
2. `extension_section_file` 是三個區段 B 檔名之一時，只執行那一個檔。結果是 `do_not_run_extension_section` 時，不要執行任何區段 B。欄位不存在或型別不符時，該檔會停止，不會彙總錯誤型別。
3. `matrix_dept_section_file` 是 `task-2.4-03-matrix-dept.sql` 時才執行區段 C。否則不要執行。型別不是 `text`、`varchar` 或 `bpchar` 時，該檔停止。
4. A7 的 `missing_data_column_count` 為 0，而且 `setting_type_name` 是 `text`、`varchar`、`bpchar` 或 `xml` 時，才執行 `task-2.4-04-data.sql`。否則不要執行區段 D。
5. 成功時，檔案最後的 `ROLLBACK` 結束交易。前置檢查發出 `stop` 時，交易已中止；請再執行 `ROLLBACK` 結束工作階段，不要繼續同一檔後面的語句。
6. 結果只保存彙總、型別與限制定義。不要保存密碼、完整連線字串、學生姓名、學號或成績。
7. 把資料庫名稱、`server_version` 與各區段筆數回填本檔後，才能重新判斷 Task 2.4 是否可勾選。PostgreSQL 9.6 與 17 都要各有一份；只有一座時，另一座維持未驗證。

區段檔：

| 區段 | 檔案 | 前置條件 |
| --- | --- | --- |
| A 環境與結構 | `task-2.4-01-precheck.sql` | 已連上允許清單中的測試庫 |
| B extension 彙總 | `task-2.4-02-extension-jsonb.sql`、`task-2.4-02-extension-json.sql` 或 `task-2.4-02-extension-text.sql` | A7 指定的那一個型別 |
| C 科母群彙總 | `task-2.4-03-matrix-dept.sql` | `matrix_dept` 存在且為文字型別 |
| D 其餘資料核對 | `task-2.4-04-data.sql` | `setting` 與排名欄位符合 A7 |

三個區段 B 的驗證能力不同，不能把一種型別的結果套到另一種：

- `task-2.4-02-extension-jsonb.sql` 保留 `jsonb_typeof` 與 `extension @>` 的 `MotherGroupSnapshot` 包含檢查。這個計數只在欄位型別是 `jsonb` 時才有意義。
- `task-2.4-02-extension-json.sql` 只回 `batch_count`、`extension_null`、`json_array`、`json_not_array`。它不把 `json` 轉成 `jsonb`，也不驗證 `MotherGroupSnapshot` 是否存在。結果裡沒有這個計數，不能寫成快照不存在，也不能寫成 0。
- `task-2.4-02-extension-text.sql` 只計 NULL、空白與開頭字元，不把文字轉成 JSON。壞 JSON 因此不會中止區段，但文字欄位也尚未證明是有效 JSON 陣列，更沒有快照名稱檢查。

## XML 檢查

區段 D 改用 `xml_is_well_formed_document()`。它要求單一根節點的 XML 文件，不受 `xmloption` 把內容片段當成合法的影響。`xpath` 與 `::XML` 只寫在 `CASE` 的 `WHEN xml_is_well_formed_document(...)` 分支；PostgreSQL 不計算用不到的 `CASE` 分支。NULL、空白、純文字、多根節點與損壞 XML 都只進入計數。

真實資料列只分成 `null`、`blank`、`document`、`not_document`。純文字、多根與損壞都屬於 `not_document`，因為函式只回答是不是單根文件，再細分就必須做會失敗的解析。

D0 用常數列出六種情境。下表是文件預期，本輪沒有在 PostgreSQL 執行：

| kind | 輸入 | 預期 is_document | 預期 exam_name |
| --- | --- | --- | --- |
| valid_document | `<Setting 考試名稱="期中考"/>` | true | 期中考 |
| plain_text | `plain text` | false | NULL |
| multiple_roots | `<a/><b/>` | false | NULL |
| damaged_xml | `<a>` | false | NULL |
| blank | 空字串 | false | NULL |
| null_input | NULL | NULL | NULL |

D4、D5 對資料表的同一規則也還沒有在 PostgreSQL 執行。

## 本輪靜態檢查

已在工作區搜尋，沒有連線資料庫：

- 新的六個區段檔都有 `BEGIN TRANSACTION READ ONLY`、允許清單 `1admin_coredev4` 與 `1admin_debug1`，以及最後的 `ROLLBACK`。
- 新的區段檔沒有 `xml_is_well_formed()`。資料表上的 `xpath` 都在 `xml_is_well_formed_document` 為真的 `CASE` 分支。
- 操作文件不再要求整份執行。舊的 `task-2.4-readonly.sql` 已移除。
- 區段檔的 SQL 語句沒有 `INSERT`、`UPDATE`、`DELETE`、`ALTER` 或 `COMMIT`。這些字只出現在註解，用來聲明不使用它們。
- `task-2.4-02-extension-json.sql` 沒有 `extension::JSONB`，也沒有 `MotherGroupSnapshot` 包含判斷。`task-2.4-02-extension-jsonb.sql` 仍保留該包含判斷。

這只證明檔案內容符合上述文字條件，不證明函式在 PostgreSQL 9.6 或 17 上的回傳值。

## 已驗證

沒有資料庫結果可標成已驗證。程式閱讀只支持查詢端相容性，不支持 schema、舊批次內容、XML 函式結果或唯一鍵結論。

## 未驗證

以下都要等允許的測試庫實際跑完對應區段才算：

- D0 六種 XML 常數情境，以及 D4、D5 對真實 `setting` 的單根文件篩選。
- 四表是否存在，以及欄位、型別、預設值、`NOT NULL`、主鍵、外鍵、唯一限制、排除限制、索引與使用者 trigger。
- `rank_batch.extension` 是否存在，以及實際型別。只有實際跑過 `jsonb` 區段，才有 `MotherGroupSnapshot` 包含計數。`json` 區段即使跑完，也只知道 NULL 與是不是陣列，不能由此判斷快照是否存在。文字區段連 JSON 是否有效都尚未證明。
- `rank_batch_student.matrix_dept` 是否存在。舊定期批次的科母群是空白、NULL、部分有值，還是每列都有值。
- 定期批次如何從 `setting`、考試與 `rank_matrix.item_type` 辨識，包含沒有矩陣的批次、`is_alive` 不是 true 的批次，以及同名考試是否對到多個 exam ID。
- 新列 `is_alive = true` 與舊列 `is_alive = NULL` 是否被現有唯一或排除限制擋住。
- 兩座測試庫的 PostgreSQL 版本與上述結果是否相同。

空的 `matrix_dept`、缺少該欄，或沒有學生列，都不能解讀成科母群可完整還原，也不能解讀成原始參與旗標。

## 阻擋

本輪的阻擋是沒有已確認的測試庫連線。在補上兩座測試庫的目錄結果前，Task 2.4 不能勾選，也不能把學期 Task 10.1 的目錄結論借用為本次實測。

若實際目錄出現下列任一項，發布或快照方案另外形成阻擋，須先寫進本檔再決定後續任務：

- `rank_matrix.is_alive` 不存在，或 `NOT NULL`，或檢查限制不允許 NULL。現行程式會把舊列更新成 NULL。
- 唯一或排除限制在 `is_alive` 改成 NULL 後仍然擋住同一排名鍵的新列。單語句原子發布就不能用現在的更新後插入。
- `rank_batch.extension` 不存在，或型別不是可保存 JSON 陣列的 `jsonb`。規格不允許為此新增欄位。
- 測試庫名稱不在允許清單。語句會拒絕，這是預期保護，不是核對通過。

## 查詢端相容性

這一節只讀程式。它說明預計加在 `rank_batch.setting` 上的 `ref_exam_id` 屬性會不會被現有讀取端拒絕。它沒有證明資料庫裡的舊 XML 都能解析。

定期批次目前寫入 `CalculateRegularAssessmentRank.btnCacluate_Click` 的 `insertRankBatchSql`，位置在 `CalculateRegularAssessmentRank.cs` 710–739 行。`setting` 由同方法 667–702 行的 `Setting` 元素組成，屬性是學年度、學期、考試名稱、不排名學生類別、類別一、類別二。同一段沒有寫 `ref_exam_id`，也沒有寫 `extension`。考試 ID 是 599 行以考試名稱取 `_ExamList.First(...)`，再放進計算 SQL 的 `ref_exam_id`，最後寫到 `rank_matrix.ref_exam_id`。同名考試現在會取第一筆，這是程式行為；資料庫裡是否真有同名考試仍未驗證。

`rank_batch_student` 的插入在 2503–2520 行，欄位只有 `ref_batch_id`、`ref_student_id`、`grade_year`、`matrix_grade`、`matrix_class`、`matrix_tag1`、`matrix_tag2`。這段沒有 `matrix_dept`。因此即使資料庫有這個欄，現行程式的新批次也不會寫入科母群。

有效排名切換在 2411–2499 行的 `update_data` 與 `insert_matrix_data`。舊列條件是 `is_alive = true`、學年度、學期、年級與 `ref_exam_id`，然後把 `is_alive` 設成 NULL。新列寫入 `is_alive = true`。這個更新沒有限制 `item_type`。同一次 `SELECT` 後面還插入學生與明細。語句沒有自己的 `BEGIN` 或 `COMMIT`，所以原子性依賴資料庫把整句當成一個交易，也依賴唯一限制允許 NULL 舊列與 true 新列並存。這兩項都還沒有在本次測試庫證明。

現有讀取端與新增屬性的關係：

| 讀取端 | 位置 | 使用方式 | 對 `setting` 上的新屬性 |
| --- | --- | --- | --- |
| `CalculateRegularAssessmentRank` 建構子 | `CalculateRegularAssessmentRank.cs` 49–75、175–204 行 | 讀學校組態「固定排名_定期評量排名計算」，用 `GetAttribute` 取已知屬性，用 `SelectNodes` 取不採計科目 | 這份是學校組態，不是 `rank_batch.setting`。未知屬性不會被讀到，也不會造成讀取失敗 |
| `CalculateRegularAssessmentRank.btnCacluate_Click` | 同檔 509–575 行 | 重建學校組態 `Setting` 並 `Save` | 重建時只寫已知屬性。它不會保留組態裡原先多出來的屬性；這不影響另一份 `rank_batch.setting` |
| 排名 SQL 的科目 xpath | 同檔 827、832、837 行 | `xpath('/Setting/採計科目/text()')` 與兩個類別科目 | 只讀子元素文字，不讀 `Setting` 屬性 |
| `RegularMatrixRankSelect.RegularMatrixRankSelect_Load` | `RegularMatrixRankSelect.cs` 58–114、124 行 | 以 `rank_matrix.ref_exam_id` 接 `exam`，用 `is_alive` 標示目前採計，並用九個矩陣欄位找同一排名鍵 | 不讀 `rank_batch.setting`。新屬性不會改變這段查詢 |
| `SemesterMatrixRankSelect.SemesterMatrixRankSelect_Load` | `SemesterMatrixRankSelect.cs` 89–161 行 | `xpath('//Setting')` 後再取 `//擇優採計成績`；同樣用矩陣 `ref_exam_id` 與 `is_alive` | `//Setting` 不因多一個屬性而失敗。這段不讀 `ref_exam_id` 屬性 |
| `SemesterHistoryBatchCatalog.TryReadSetting` | `MotherGroup/SemesterHistoryBatchCatalog.cs` 156 行 | `GetAttribute("考試名稱")` 必須等於「學期成績」 | 未知屬性被略過。定期批次不會因此變成學期批次 |
| `HistorySnapshotRestore.TryInspectSnapshot` | `MotherGroup/HistorySnapshotRestore.cs` 151 行 | 同樣只讀考試名稱，並找子元素 `MotherGroupSnapshot` | 未知屬性被略過 |

`XmlElement.GetAttribute` 在屬性不存在時回傳空字串，多出來的屬性不會讓已知屬性讀取失敗。上述 xpath 與 `SelectNodes` 找的是元素，不是屬性白名單。因此把 `ref_exam_id` 加到 `rank_batch.setting` 的 `Setting` 元素，就目前這些讀取端來看是附加屬性，不會改寫它們的篩選結果。

這個相容性判斷沒有讓現有查詢開始使用該屬性。`RegularMatrixRankSelect` 仍以 `rank_matrix.ref_exam_id` 識別考試，批次設定裡的考試名稱仍只是文字。沒有矩陣列的舊批次，或同名考試對到多個 ID 的情況，不能靠現有查詢端自動消除。區段 D 的 D5、D6、D9 就是在計這幾種彙總；結果尚未取得。

## 與後續任務的邊界

2026-11-01 調整：本任務缺少連線只阻擋自身實測與依賴 schema 證據的後續整合，不阻擋 Task 3.1～3.3 等本機開發。Cursor 應維持本任務未完成並前進，不反覆改寫已通過審查的 SQL。資料庫連線可用後再回來完成本任務；發布與資料庫驗收不得跳過必要證據。

Task 4.3 的定期歷史目錄仍要同時看批次設定、排名類型與考試 ID，不能只因考試名稱不是「學期成績」就收下。Task 4.4 的降級仍只在兩種母群快照都不存在時讀 `rank_batch_student`。Task 7 的單語句發布仍要等本次唯一限制的目錄結果，不能用本檔的程式閱讀代替。
