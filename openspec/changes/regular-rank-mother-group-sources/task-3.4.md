# Task 3.4 三個 Grid 的大量清單保護

狀態：新確認頁的學生清單、人數與分組明細都在加列前呼叫 `MotherGroupConfirmGrid.Configure`、`ConfigurePopulation`、`ConfigureDetail`。填列使用這些方法的 `Rows.Add` 路徑。`CalculateRegularAssessmentRank.cs` 已沒有 `new DataGridViewRow`。沒有重現 new_debug6，不能把這次檢查寫成已修復該問題。指定測試環境的跨程序操作仍屬 Task 8.7，維持未完成。

## 舊路徑

條件頁的 `plStudentView` 與舊 `dgvStudentList` 仍留在 Designer。下一步不再顯示該頁，也不再對它加一般資料列。舊計算按鈕文字改為「計算排名（已停用）」，點擊時直接停在 Task 2.4 阻擋訊息，不會讀舊 Grid、不會保存學校組態、不會執行排名 SQL。

## 本機 STA

命令先編譯 Debug DLL，再以 `RegularRankOperationRunner` 執行 `RegularRankOperationCheck`。

結果：exit 0。2066 名學生列與核取儲存格的輔助存取 Bounds 走訪 7ms，門檻 10000ms。分組明細 2068 列的 Bounds 走訪 0ms，門檻 10000ms。三個 Grid 的資料列型別都是受保護資料列。離屏最後一列回傳空範圍，捲到第 2051 列後範圍更新，核取 DefaultAction 可把年參與從 true 改成 false。

這是合成 `DataGridViewX`，不是 iSchool 載入 DLL 後的跨程序操作。匯出內容另在同一支檢查驗證，不把對話框開啟當成匯出成功。
