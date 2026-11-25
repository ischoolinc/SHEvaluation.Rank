# 定期評量測試指南（2026-11-06）

交付名稱：**完整功能實作版，主機整合待驗證**。程式功能與本機離線檢查已完成；**尚未**在 new_debug6 完成寫入／回滾／並行主機驗收，不得標為全數通過。

## 版本

- 本機建置：`C:\ischoolProject\SHEvaluation-Regular\bin\Debug\SHEvaluation.Rank.dll`
- 版本：**1.0.0.65**
- SHA-256：`5FCB747131125F00B0CEAEBB1B4259F0F937C3D701E60BDFFED873BE552A9AF0`
- 先前已換載測試程式仍可能是 1.0.0.64（FA6A2655…）；換載前請同時核對版本與雜湊。
- 本輪**未**部署、未要求重登入。

## 功能實作完成（可測範圍說明）

- 三種來源（目前／Excel／歷史）、歷史加人、確認頁五種參與、搜尋隔離、完整匯出、上一步。
- 計算按鈕：確認清單就緒即可按；**寫入前**會再跑結構／傳輸檢查。未通過則不保存學校組態、不寫排名，並顯示原因（不是永久 Task 勾選停用）。
- 排名結果檢索／矩陣明細已接定期轉接，只查 `定期評量`，不混學期。

## 本機驗證結果

- Debug 建置通過；2066 人 Grid／公式保護／三來源／發布替代執行器／檢索轉接等離線 Check 通過。
- OpenSpec strict 與 `git diff --check` 通過。
- 替代執行器可證明組句→執行→核對次序與傳輸失敗停止；**不能**冒充資料庫回滾或鎖語意已驗收。

## 延後的主機整合驗證

1. **唯一未確認儲存邊界**：DSA adminquery 是否能在同一連線執行 SERIALIZABLE 交易腳本或單一 DO。既有探測為 DSA 504，無底層 SQLSTATE（見 `backend-transport-check-20261105.md`）。契約未確認前，計算點下後應明確失敗並停止。
2. 實際三來源往返、五旗標排除組合數值、全部年級原子寫入、零明細年級、故障回滾、兩獨立連線並行、回應中斷核對。
3. 計算後批次能否在 RegularAssessmentRankSelect／RegularMatrixRankSelect 找到；VS2022 Designer 開啟儲存重開；學期回歸。
4. 既有唯讀證據可沿用：114-2／exam ID 5／三年級 687 人全參與假設新舊各 30,764、缺少／多出 0。

## 登入後最短步驟

1. 換載 1.0.0.65 並核對 SHA-256。
2. 目前來源載入 → 確認三組科目 → 確認頁 → 計算：確認失敗訊息與「未 Save」行為，或契約通過後的批次／明細。
3. 用既有「核對試算（不儲存）」複核全參與假設；再依 backend-transport-check 腳本確認閘道後才做寫入與並行。

## 紀錄

- `%LocalAppData%\ischool\RankVerification\`：`regular-rank-contract.txt`、`regular-rank-transport-probes.txt`、`regular-result-verification.txt`。
- 回報請附版本、雜湊、操作條件與去識別錯誤；勿附憑證或完整學生資料。
