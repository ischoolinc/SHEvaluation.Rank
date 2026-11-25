## ADDED Requirements

### Requirement: 專用母群確認畫面
系統 SHALL 提供定期評量專用 Form、Designer.cs、resx，固定控制項由 Designer 管理；Designer MUST NOT 包含 SQL 或業務查詢。預設建構子 SHALL 可供設計工具使用；驗收 SHALL 實際以 VS2022 開啟、儲存與重開。

條件畫面 SHALL 沿用 CalculateRegularAssessmentRank，確認畫面 SHALL 新增 RegularRankMotherGroupConfirmForm，兩者為獨立 Windows Form，MUST NOT 以同一 Form 切換兩個 Panel 代替。兩者各自具有 .cs、.Designer.cs、.resx。下一步成功開啟確認 Form 時，條件 Form 暫停操作；上一步返回原條件 Form 並保留條件。確認 Form 關閉與計算中關閉行為 SHALL 比照學期既有生命週期，避免條件頁永久停用或重複建立確認視窗。

#### Scenario: 設計工具載入
- **WHEN** 在 VS2022 Designer 開啟定期評量條件及確認畫面
- **THEN** 兩者能正常顯示與儲存，且不執行資料庫查詢

#### Scenario: 兩個獨立畫面往返
- **WHEN** 使用者從條件 Form 按下一步，再於確認 Form 按上一步
- **THEN** 開啟及返回的是兩個獨立 Form，條件值保留，沒有同時可操作的兩份計算條件或重複確認視窗

### Requirement: 大量清單輔助存取可回應
系統 SHALL 在學生清單、母群人數及分組明細三個 Grid 保留完整輔助存取子項，避免離屏資料列 Bounds 遞迴造成操作無回應。系統 SHALL 在加列前套用已修正的共用 Grid 設定，並使用受保護的資料列建立路徑；MUST NOT 以直接建立一般 DataGridViewRow 繞過保護，也不得以隱藏子項、全程式停用輔助存取或延長逾時代替修正。仍可到達的舊確認清單路徑 SHALL 一併盤點，未修正者不得宣告驗收通過。

#### Scenario: 大量學生與離屏列
- **WHEN** 實際 DataGridViewX 載入至少 2066 名合成學生，查詢列與核取儲存格的輔助存取 Bounds
- **THEN** 離屏列回傳空矩形，捲動後位置正確更新，完整學生與儲存格子項保留，核取 DefaultAction 仍可用，且記錄耗時並通過測試門檻

#### Scenario: 實際測試環境操作
- **WHEN** 在指定測試環境確認載入版本後，以跨程序工具操作大量名單
- **THEN** 捲動、上一步、重載、搜尋與清除搜尋、匯出開啟及取消後返回皆可回應；搜尋須核對實際文字與結果，匯出內容另行驗證，不以對話框開啟代替；本機控制項測試不得代替此驗收

### Requirement: 五種參與互相獨立
畫面 SHALL 顯示完整清單、學號／姓名／班級／座號識別欄位與五種母群及獨立參與，支援批次修改、學生搜尋、初始參與恢復、人數統計、匯出、上一步與計算。五種母群名稱 SHALL 唯讀。歷史識別班級或座號若只能查到目前值，欄名與說明 SHALL 標明目前資料，不得冒充歷史值。搜尋 SHALL 只改變顯示；完整匯出與計算 SHALL 使用完整底層清單。恢復 SHALL 作用於完整清單，按鈕文字為「恢復全部學生初始參與」，並回到進入確認畫面時的初始值。

#### Scenario: 只排除年排名
- **WHEN** 使用者只把一名學生年排名設為不參與
- **THEN** 其他四種旗標與五種母群名稱不變

#### Scenario: 搜尋與恢復
- **WHEN** 使用者搜尋部分學生並恢復初始參與
- **THEN** 恢復作用於完整清單，搜尋不刪除底層清單，計算不僅限可見列

### Requirement: 清單操作不預先大量查成績
統計 SHALL 依清單計算總人數與五種參與／排除人數，不預先大量查完整成績。沒有本次成績的學生 SHALL 留在清單與 Snapshot；計算只對有有效成績的項目產生明細。排除類別初始化及歷史覆寫 SHALL 依來源規格的本次不排名預設處理。

#### Scenario: 調整參與後統計
- **WHEN** 一名學生班排名從參與改為不參與
- **THEN** 班排名參與減一、排除加一，其他種類人數不變且不觸發完整成績載入

### Requirement: 計算使用不可變工作副本
系統 SHALL 提交尚未完成的參與編輯後複製條件與完整清單，計算中防重入；恢復或修改畫面不得改變已開始的 SQL 或 Snapshot。失敗與取消 SHALL 可恢復操作。取消 SHALL 不儲存學校組態，也不寫入排名。

#### Scenario: 執行中重按計算
- **WHEN** 一次計算尚在進行且再次觸發計算
- **THEN** 不建立第二次排名作業
