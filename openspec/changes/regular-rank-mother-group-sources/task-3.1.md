# Task 3.1 定期計算 context／request

狀態：離線驗證完成，並已修正公開清單、科目更新失敗與 ID 契約。沒有連線資料庫，沒有修改學期功能、原始成績公式、共用 MotherGroup 類別、`baseline/source` 或 Task 2.4 的唯讀 SQL。沒有開始 Task 3.2。

`RegularRankCalculationContext` 分開保存系統期間、目標學年度、目標學期、目標 exam ID、目標考試名稱、來源種類、來源期間、來源考試、來源批次、Excel 識別、年級、類別條件與三組科目。`RegularRankConfirmRequest` 是通過檢查後的複本。查詢期間與查詢 exam ID 只讀目標值，不回退系統期間，也不改用來源考試。

年級、候選科目、勾選科目與 request 的集合都是 `ReadOnlyCollection`。外部 `Add` 會被拒絕。呼叫端之後修改自己傳入的清單，不會改到已保存的複本。年級只能經 `SetGradeYears` 調整，變更後載入過期。勾選科目只能經 `SetCheckedSubjects` 調整，必須屬於該組候選，不能空白或重複；變更後撤銷科目確認，不要求三組各自至少勾一科。

`ApplySubjectCandidates` 一開始就撤銷確認與可用狀態。規劃失敗時保留舊清單供畫面顯示，但不套用其中任何一組，也不能建立 request。必須更新成功並重新確認後才恢復。

ID 契約來自現有程式，不是 Task 2.4 的目錄結果：

- 目標與來源 exam ID 依 `CalculateRegularAssessmentRank.cs` 的 `ref_exam_id::INT`，只接受 1 到 2147483647 的十進位整數，拒絕空白、正負號、前導零與溢位。`Resolve` 不會把無效 ID 標成成功。
- 歷史批次 ID 依 `HistoryRankRestore.SelectSql` 的 `long` 解析，只接受 1 到 9223372036854775807 的同一種表示法。
- 驗證失敗回傳錯誤，不改掉先前有效值。資料庫欄位實際型別仍是 Task 2.4 未驗證項目。

離線檢查 `RegularRankCalculationContextCheck` 另外覆蓋外部不可修改、輸入清單隔離、受控修改後撤銷、科目更新失敗後阻擋、重新載入並確認後恢復，以及無效 exam ID 與 batch ID。原有的期間分離、同名考試、三組科目與工作副本案例仍在同一支檢查。

驗證命令使用 .NET Framework `csc`，只編譯上述兩個新檔：

`C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /codepage:65001 /target:exe /main:SHEvaluation.Rank.RegularRank.RegularRankCalculationContextCheck`

結果：exit 0，輸出 `regular-rank-context-check: ok`。這不是方案的 MSBuild，也不是 PostgreSQL 或畫面驗收。Task 3.1 維持勾選。Task 2.4 的資料庫欄位型別仍未驗證。
