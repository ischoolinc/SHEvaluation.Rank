# 實作接手紀錄

## 2026-11-06 完整功能實作版，主機整合待驗證

交付名稱：**完整功能實作版，主機整合待驗證**。本輪依 implementation-plan-20261106.md 完成程式功能與本機驗證；**沒有**登入主機／new_debug6、沒有部署 DLL、沒有改 schema、沒有 Commit／Push／Merge／Archive。不得解讀為 new_debug6 全數驗收通過。

### 功能實作完成（程式呼叫鏈）

1. 條件與科目：來源／目標分離、唯一 exam ID、三組科目、條件過期與重載失敗不沿用舊名單。
2. 目前／Excel／歷史來源與歷史加人（學號唯一、成功才採用、保留 Working 旗標）。
3. 確認頁五種參與、批次、搜尋隔離、統計、恢復、匯出、上一步、三 Grid 保護。
4. 排名計算：`RegularRankCandidateSql` 依年級×五種類先套參與再跑原公式；跨年級同名班隔離。
5. 確認 → 傳輸檢查 →（通過後）組態 Save → 原子發布 SQL → 結果／不確定核對；零明細整次失敗；防重入。
6. 可替換傳輸：`IRegularRankSqlExecutor`／替代執行器本機驗證；真實路徑仍用 `SemesterRankPublishTransport.OpenWriter`。
7. 結果檢索／矩陣明細：`RegularAssessmentRankQuery` 轉接；只查定期；空結果可恢復。

### 本機驗證結果

- MSBuild Debug：成功（既有兩項警告未擴大）。
- Check：Operation（2066 人 Bounds 15ms）、DesignerStructure、PublishPipeline、Retrieval、CandidateSql、Excel、History、Context、Period 皆 exit 0。
- `openspec validate regular-rank-mother-group-sources --strict --no-interactive`：通過。
- `git diff --check`：通過。
- DLL：`C:\ischoolProject\SHEvaluation-Regular\bin\Debug\SHEvaluation.Rank.dll`
- 版本：**1.0.0.65**
- SHA-256：`5FCB747131125F00B0CEAEBB1B4259F0F937C3D701E60BDFFED873BE552A9AF0`

### 延後的主機整合驗證（明確未完成）

- 遠端 DSA adminquery 對 SERIALIZABLE 多語句／單一 DO 的執行契約（既有 DSA 504，無 SQLSTATE）。**此為儲存功能唯一未確認邊界**；未通過前寫入路徑會在 Save 前停止並提示，不假成功。
- Task 2.4 完整結構／索引／既有查詢端資料庫勾選；Task 7／8 回滾、並行、PG 9.6／17、VS2022 Designer 人工、iSchool 畫面往返。
- 五旗標實際排除組合數值、全部年級寫入、零明細年級實測、回應中斷核對。
- 既有 new_debug6 唯讀證據仍有效：114-2／exam 5／三年級 687 人全參與假設各 30,764 筆、缺少／多出 0（見 full-implementation-20261105.md）。

### 主要修改位置（本輪）

- `RegularRank/RegularRankPublishExecutor.cs`（新）、`RegularRankPublicationService.cs`、`RegularRankCalculationEntry.cs`、`RegularRankPublishPlan.cs`
- `RegularRankMotherGroupConfirmForm(.cs/.Designer.cs)`、`CalculateRegularAssessmentRank.cs`（計算提示與 Save 次序）
- `DAO/RegularAssessmentRankQuery.cs`（新）、`RegularAssessmentRankSelect.cs`、`RegularMatrixRankSelect.cs`
- `RegularRankRetrievalCheck.cs`、`RegularRankPublishPipelineCheck.cs`、Operation／Designer Check 期望更新
- `Properties/AssemblyInfo.cs` → 1.0.0.65；`tasks.md` 第 9 節、`tester-guide.md`

### 未來登入後最短驗收步驟

1. 換載本機 1.0.0.65 DLL 至授權 debug6，核對雜湊。
2. 載入目前來源 → 確認頁；點計算；若傳輸探測仍 504，應明確失敗且不 Save／不寫排名。
3. 依 backend-transport-check-20261105.md 確認閘道契約後，再測整次發布、四表筆數、零明細、兩連線並行與檢索／明細。

### 歷史摘要

2026-11-05 前狀態與 .64 換載證據見 full-implementation-20261105.md。舊 Task 勾選狀態不因本輪功能完成而補勾真實環境項。
