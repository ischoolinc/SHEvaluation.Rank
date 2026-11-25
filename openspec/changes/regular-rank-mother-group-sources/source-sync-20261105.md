# 學期來源同步 2026-11-05

來源：C:\ischoolProject\SHEvaluation.Rank，最新提交 dc1b5b1（1.0.0.63），來源工作區無未提交修改。相對前次 46137d9 有六檔差異。

本次複製四個無本地衝突的學期程式：DAO/SemesterAssessmentRankQuery.cs、MotherGroup/SemesterRankFailureReport.cs、MotherGroup/SemesterRankRetrievalCheck.cs、SemesterAssessmentRankSelect.cs。涵蓋每批最多 1000 筆讀取、畫面每頁 500 筆、資料庫篩選、背景排序／匯出、失敗清空及矩陣一致性檢查。

另兩檔未覆蓋：.gitignore 的來源新增規則屬本機檢查產物；Properties/AssemblyInfo.cs 的本地版本已為 1.0.0.63，保留本地定期「目前學生來源操作測試版，計算與儲存停用」說明。csproj 無來源新增差異。保留全部定期程式、OpenSpec 與目前未提交修改。

驗證：VS2022 Community MSBuild Debug 成功，輸出 bin/Debug/SHEvaluation.Rank.dll；兩項警告為既有過時診斷方法與 useWeightedAvg 未使用。檢索 CheckQuerySql、CheckPagedRetrieval、CheckFailureReport、CheckScreenSource、CheckPublishBoundary 五項局部離線檢查，以及定期 Context／PeriodSourceEditor 兩項檢查通過。CheckFailureReport 初次由 PowerShell 反射執行因未載入 FISCA.DSAUtil 失敗，明確載入 Library 相依後重跑通過；未修改產品程式。git diff --check 通過（定期既有檔案有 LF／CRLF 提示）。

沒有執行完整 SemesterRankRetrievalCheck、實際 iSchool／資料庫排名或部署；來源的 new_debug6 大量結果驗證不當成本工作區實測。此同步不代表定期功能完成，也未更新其他測試交付目錄或既有封裝 DLL。
