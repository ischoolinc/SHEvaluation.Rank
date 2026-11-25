# 來源同步紀錄 2026-11-04

來源：C:\ischoolProject\SHEvaluation.Rank，提交 46137d9，版本 1.0.0.61。來源工作區無未提交修改；本次同步 c8b386d 之後的 31 個程式檔，另合併 csproj 新增項目。同步前比對內容指紋，待覆蓋檔案均與前次來源或最新來源一致，未發現本地衝突。

已納入學期發布專用傳輸（不自動重試）、操作識別及結果核對、檢索查詢及虛擬清單、母群明細輔助存取、Excel 列號提示、公式組句抽出與診斷程式。保留本地定期 Form、RegularRank、定期 OpenSpec 與 Accessibility 參考。

新增 FISCA.Authentication 與 FISCA.DSAClient 參考；Authentication 依來源設定使用 ..\fisca\Libraries\FISCA.Authentication.dll，當前環境存在。未複製 bin／obj、套用其他版本控制操作或部署。

驗證：VS2022 Community MSBuild Debug 成功；保留 useWeightedAvg 未使用與診斷呼叫過時方法兩項警告。RegularRankCalculationContextCheck、SemesterHistorySourceCheck、MotherGroupPhase3Task34Check、MotherGroupPhase7Task71Check、MotherGroupPhase7Task72Check、MotherGroupSnapshotJsonCheck 共六項離線檢查通過；git diff --check 通過。未操作 iSchool、未執行資料庫排名或跨程序驗收。

後續 Cursor 應以此版本重新核對共用元件；原 new-debug6-risk-review.md 仍是 2026-11-03 當時的風險檢查，不能當成最新共用程式完整驗收。定期發布 Task 7 實作前須分析本次學期的重送防護、操作識別與結果未確認處理是否適用；這些行為尚未整合至定期排名，不因同步檔案而勾選完成。學期來源提交仍列有歷史／Excel 大名單、完整回滾、回應中斷核對及獨立數值驗算等未完成複測。
