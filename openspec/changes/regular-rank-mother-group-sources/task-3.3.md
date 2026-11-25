# Task 3.3 定期母群確認 Form

狀態：兩個獨立 Form／Designer／resx／csproj 已接上，並已清除條件 Form 的舊 `plStudentView`。預設建構子設計時期不查資料。沒有用 VS2022 實際開啟、儲存或重開。沒有在 iSchool 操作實際畫面。

## 畫面分工

- `CalculateRegularAssessmentRank`：只保留條件頁 `plSetting`。目標期間／考試、三種母群來源、來源欄位、年級與類別、三組科目、載入／確認科目／下一步都在這個 Designer。Form 的 `Controls` 只有 `plSetting`，`Dock = Fill`。
- `RegularRankMotherGroupConfirmForm`：獨立確認頁。學生清單、五種參與、人數、明細、搜尋、恢復、匯出、上一步與計算都在這個 Designer。

條件頁按下一步會 `Show` 確認頁並停用條件頁。確認頁上一步或使用者關閉會隱藏確認頁並重新啟用條件頁。已顯示的確認頁會重用，不另建第二個。計算中關閉會被取消。

## 舊學生頁清除

條件 Form Designer 與程式已移除 `plStudentView`、`dgvStudentList`、`btnCacluate`、`btnPrevious`、`pbLoading`、`btnExport`、`btnImport` 與舊標籤。不是 `Visible = false`。計算邏輯改由確認頁呼叫 `TryCalculateFromConfirm`，學生資料來自確認清單；計算入口仍停用。

## 驗證

- Debug 編譯成功，版本 1.0.0.63。
- `RegularRankDesignerStructureCheck`：ok。確認預設建構子可建立、三個 Grid 受保護、計算按鈕停用、隱藏後不銷毀條件頁。
- `RegularRankOperationCheck`：ok，並核對 Designer 不再含舊學生頁控制項。
- VS2022 Designer 開啟／調整／儲存／重開：未執行。
- iSchool 下一步／上一步／關閉／重開實測：未執行。
