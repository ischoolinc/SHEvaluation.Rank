# Tasks

2026-11-06 最高優先指示：本輪目標為所有程式功能實作完成及本機驗證，不需登入主機／new_debug6，不等待伺服器紀錄。依 implementation-plan-20261106.md（根目錄 Todo.md 同內容）連續實作；新增第 9 節追蹤本輪交付。下方第 1～8 節保留原規格及歷史實測證據，含真實環境驗收者未執行不補勾，但不作為停止本輪功能開發的前提。保留執行時安全檢查；不以永久停用、原型或假成功回傳代替實作完成。完成名稱為「完整功能實作版，主機整合待驗證」。

2026-11-05 最新範圍：完整定期評量測試版，含 Excel、歷史、人工加入及實際排名；取消先前限定目前來源／延後功能限制。new_debug6 的新增排名、停用舊排名、回滾及並行測試已由使用者授權；只在該測試環境驗證。以下完成狀態仍以各項證據為準。

2026-11-05 23:24 最新證據：三來源與五旗標候選／發布原型已實作；new_debug6 結構／索引及三年級 687 人的全參與唯讀核對通過（新舊各 30,764 筆、缺少／多出 0）。交易腳本及 DO 均未取得可用證據，前端 DSA 504，發布停用。使用者目前無法取得伺服器紀錄。完整接續點以 full-implementation-20261105.md 與 backend-transport-check-20261105.md 為準；下方各項歷史說明中的「未連線／未實作」已部分過時，未勾選仍表示整項驗收未完成，不表示目前完全沒有程式。不可因部分通過補勾整項。

2026-11-05 執行授權：依 cursor-adjustment-prompt.md 連續實作目前學生限定測試版。Excel、歷史母群與人工加入維持未完成。Task 2.4 沒有測試庫連線，計算與儲存入口維持停用。OpenSpec validate 與編譯通過不代表功能完成。

決策已於 2026-10-30 核准並開始 Apply。D01、D03、D04、D09、D10 採 regular-decisions.md；D02、D05、D06、D07、D08 採 semester-reference.md。每階段確認後才 Commit／Push，不自行 Archive 或合併。已完成第 1 節、Task 2.1、2.2、2.3、3.1、3.2、3.3、3.4、4.1、4.5、4.7、5.1、5.3 與 6.1 的離線範圍。Task 2.4、4.2、4.3、4.4、4.6、5.2、5.4、6.2～6.5、7 與 8 維持未勾選。交付等級是操作測試版。

## 1. 決策與設計核准

- [x] 1.1 確認 D01～D04：期間來源政策、歷史類型、來源／目標分離、目標科目查詢及重載操作。
- [x] 1.2 確認 D05～D07：無快照降級、無成績者保存、排除類別及歷史參與覆寫優先序。
- [x] 1.3 確認 D08～D10：Excel 識別、歷史加人、失效／空結果／並行發布。
- [x] 1.4 確認名稱唯讀、歷史識別欄位缺漏、exam ID 儲存相容方式；更新四份 spec 與 design，取得明確 Apply 指示。
- [x] 1.5 審閱 regular-decisions.md 的專用方案，確認載入／重載操作、加人資格、零明細年級及跨連線並行；核准後清理各文件待決文字。政策核准不表示功能已實作。

## 2. 原始基準與共用邊界

- [x] 2.1 固定原版 commit、完整原始程式及 SQL 基準，記錄公式保護區與六組／18 個排名分支清單。證據：baseline/task-2.1.md 與 baseline/source。
- [x] 2.2 建立以矩陣鍵＋學生識別逐筆比對的唯讀夾具，涵蓋全部統計、缺漏、重複與 NULL。證據：baseline/compare 與 baseline/compare/task-2.2.md。精度、大量結果與差異明細回歸已通過；尚未連線資料庫。
- [x] 2.3 核對共用類別可見性、依賴、預設政策；需要共用修改時先提出學期影響與版本整合建議。證據：task-2.3.md。結論是不修改共用原檔；尚未連線資料庫，也尚未編譯這些類別。
- [ ] 2.4 在獨立測試庫唯讀核對四表欄位、extension 型別、matrix_dept、限制、索引與既有查詢端，不改 schema。語句與未連線原因見 task-2.4.md 及 task-2.4-01 到 task-2.4-04 的區段檔。本輪沒有測試庫連線，不能勾選。

## 3. 條件與畫面模型

開發順序調整（2026-11-01）：Task 2.4 與後續本機開發可並行，不是 Task 3 的整體前置阻擋。沒有測試庫連線時，保持 2.4 未勾選，直接從 3.1、3.2、3.3 依序開發，再進行可離線驗證的來源轉接、確認操作與 SQL 組成。資料庫欄位仍屬假設時須記錄；依賴該結構的整合與發布不得宣告通過。Task 7 實際發布整合及資料庫驗收仍須取得 2.4 必要證據。不要為等待連線反覆改寫已通過審查的唯讀 SQL。

- [x] 3.1 新增定期專用計算 context／request，保留目標與來源資訊，驗證期間、exam ID 與三組科目。證據：RegularRank/RegularRankCalculationContext.cs、RegularRank/RegularRankCalculationContextCheck.cs 與 task-3.1.md。審查修正後離線檢查仍通過；集合不可由外部修改，科目更新失敗會撤銷確認，exam ID 與批次 ID 依既有程式契約驗證。尚未連線資料庫，也尚未做 Task 3.2 的畫面。
- [x] 3.2 Designer 建立期間與來源固定控制項，套用已核准 D01～D04，不先改 SQL 公式。證據：RegularRank/RegularRankPeriodSourceEditor.cs、RegularRank/RegularRankPeriodSourceEditorCheck.cs、CalculateRegularAssessmentRank.Designer.cs 與 task-3.2.md。離線政策檢查與專案編譯已通過。尚未開啟 VS2022 Designer，尚未連線資料庫，尚未操作實際畫面。載入按鈕不查科目，也不呼叫原 reloadSubject。
- [x] 3.3 建立專用確認 Form／Designer／resx 與 csproj 關聯；預設建構子不查資料。證據：RegularRank/RegularRankMotherGroupConfirmForm.cs、Designer.cs、resx、條件 Form 已清除 plStudentView、task-3.3.md、designer-structure-20261105.md。離線結構檢查通過。尚未用 VS2022 開啟、儲存、重開，也尚未做 iSchool 往返實測。

- [x] 3.4 依 new-debug6-risk-review.md 盤點舊定期 Grid 與新確認頁三個 Grid，沿用已修正的 MotherGroupConfirmGrid 設定及受保護填列路徑；確認無直接 new DataGridViewRow 繞過保護，記錄仍可到達的舊路徑處理。證據：task-3.4.md。本機 2066 人 STA 通過；沒有重現 new_debug6，也沒有做指定環境跨程序驗收。Task 8.7 維持未勾選。

## 4. 三種來源與確認操作

- [x] 4.1 建立定期目前來源，保留一般＋延修資格及原標籤處理，套用核准 D07。證據：RegularRank/RegularRankCurrentStudentSource.cs 與 task-4.1.md。離線資格通過。沒有連線資料庫，畫面載入路徑尚未在 iSchool 操作。
- [ ] 4.2 依 D08 使用相容 Excel 格式，驗證唯一識別、姓名差異、空白、重複、無對應及無效參與值。
- [ ] 4.3 建立定期歷史目錄，正確顯示及驗證批次／期間／考試，避免名稱歧義。
- [ ] 4.4 整合 JSON、允許的舊 XML、D05 無快照降級。JSON 與 XML 母群快照均不存在時才降級；快照存在但損壞、重複、版本不支援或內容無法還原時停止，不得當成不存在後改走 rank_batch_student。母群不被目前學籍覆蓋。
- [x] 4.5 共用確認清單／Grid 實作五種獨立參與、批次操作、搜尋、統計、恢復、匯出與上一步。證據：task-4.5.md。2066 人匯出內容與搜尋隔離已在合成 Grid 驗證。匯出對話框、取消與 iSchool 實際畫面未操作。
- [ ] 4.6 依 D06 處理無成績學生；依 D09 實作或明確不提供歷史加人，補齊失敗／取消／重試／防重入。

- [x] 4.7 重新核對同步後共用類別的預設政策，對照 D02／D05／D07，加入定期來源回歸；定期僅限定期批次、空白／衝突與排除預設依定期核准規格，不能靜默採用學期最新行為。差異留存 task-4.7.md，必要時使用定期轉接而非修改學期行為。本輪用定期來源轉接保留延修，沒有修改學期共用資格。

## 5. 期間、考試與快照整合

- [x] 5.1 改寫 reloadSubject 的輸入來源，使用核准的目標期間／考試／母群修課範圍，三組科目重載需確認。證據：RegularRank/RegularRankSubjectCatalog.cs 與 task-5.md。科目 SQL 組成已離線檢查。沒有對資料庫執行。
- [ ] 5.2 統一成績 SQL 考試識別與期間，驗證同名考試及歷史學生無目前班級，不改成績規則。成績接合已改為 `exam.id = row.ref_exam_id::INT`，公式行與基準一致；同名考試與無目前班級尚未對資料庫驗證，不能勾選。見 task-5.md。
- [x] 5.3 保留定期 XML，整合完整確認清單 JSON v1；測試其他 extension、特殊字元、false 與無法解析年級。證據：RegularRank/RegularRankSnapshotDocument.cs 與 task-5.md。離線往返通過。沒有寫入資料庫；計算停用時也不保存學校組態。
- [ ] 5.4 依核准方案驗證條件變更使快取過期、重載失敗不能用舊名單計算、保留三組科目交集及新科目提示、人工加人重載保留既有 Working 旗標。條件過期、失敗不沿用舊名單、科目交集與新增提示已有離線檢查。人工加人本輪未提供，不能勾選。見 task-5.md。

## 6. Participation SQL

- [x] 6.1 建立專用 Gate 原型與覆蓋檢查，標記缺漏即停止；不直接套用 SemesterRankSql.ApplyParticipation。證據：RegularRank/RegularRankParticipationGate.cs 與 task-6.md。十八個分支標記與原始公式行比對通過。參與集合尚未插進實際排名視窗，6.2～6.5 維持未勾選。
- [ ] 6.2 科目與領域一般／tag1／tag2 視窗接入五旗標，保持類別領域原版來源交集。
- [ ] 6.3 總分、平均、加權總分、加權平均全部分支接入，保持聚合、PR 及百分比公式。
- [ ] 6.4 score_list 各類型統計只接收對應參與集合，驗證新五標、分布、標準差與空類別。
- [ ] 6.5 全參與與原版逐筆比對，五旗標組合測試及跨年級同名班隔離通過後才整合發布。

## 7. 原子發布

- [ ] 7.1 依核准 D10 實作單語句或經證明同連線交易方案，批次及全部年級同成同敗。離線計畫要求單次同成同敗且禁止逐年級提交，但沒有 Task 2.4 證據，計算入口停用，不能勾選。見 task-7.md。
- [ ] 7.2 設定舊排名失效範圍及零結果政策，驗證批次 ID、矩陣／學生／明細對應、並行重算。
- [ ] 7.3 Snapshot 同次 INSERT，明確處理組態 Save 邊界；計算工作副本、防重入、失敗 UI 恢復。
- [ ] 7.4 在批次、後續年級、矩陣、學生與明細階段故障注入，核對四表及舊有效排名完整回滾。
- [ ] 7.5 驗證整次與單一年級零明細政策；兩個獨立連線同時發布重疊年級，核對鎖等待後快照與最終有效矩陣唯一性，不以 UI 防重入替代。

## 8. Verify 與交付

- [ ] 8.1 .NET Framework 4.8 編譯；檢查差異僅限核准範圍，原始公式基準未變。
- [ ] 8.2 PostgreSQL 9.6 與 17 完整結果比對／回滾／JSON 往返，中間版本相容 smoke，記錄版本與證據。
- [ ] 8.3 Excel 往返、JSON／XML／無快照／壞快照、無成績、缺學籍、同名考試與歷史目標資料流驗收。
- [ ] 8.4 VS2022 開啟／儲存／重開定期兩 Form，完成縮放、導覽、取消、重試與防重入人工驗收。
- [ ] 8.5 學期排名回歸；既有未完成驗收維持原狀，沒有證據不補勾。
- [ ] 8.6 列出變更檔、測試證據、限制與更新估時，等待階段確認後才 Commit／Push；不自行 Archive 或合併。
- [ ] 8.7 以實際 DataGridViewX、至少 2066 人，在 STA 驗證三個 Grid 的離屏 Bounds、捲動更新、完整子項與核取 DefaultAction；記錄耗時與門檻。再於指定測試環境核對 DLL 載入版本並跨程序驗證捲動、上一步、重載、搜尋、匯出及取消；未實測保持未完成，不以學期既有紀錄替代。

## 9. 2026-11-06 全功能實作交付（不需登入主機）

本節只追蹤功能程式與本機驗證，執行細節及完成標準見 implementation-plan-20261106.md。交付名稱：**完整功能實作版，主機整合待驗證**。舊 Task 2.4／7／8 含真實資料庫驗收者維持未勾選。

- [x] 9.1 完成條件、來源／目標期間與唯一 exam ID、三組科目、失效／重載／確認；預設建構子及 Designer 無資料庫存取。程式：`RegularRankPeriodSourceEditor`、`RegularRankCalculationContext`、`RegularRankSubjectCatalog`、`CalculateRegularAssessmentRank`；本機：`RegularRankPeriodSourceEditorCheck`、`RegularRankCalculationContextCheck`、`RegularRankDesignerStructureCheck` ok。
- [x] 9.2 完成目前、Excel、歷史來源及歷史加人，識別／參與／快照政策、取消與失敗恢復完整。程式：`RegularRankCurrentStudentSource`、`RegularRankExcelSource`、`RegularRankHistorySource`、`CalculateRegularAssessmentRank` 載入／加人；本機：Excel／History／Context Check ok。主機實際往返仍待驗證。
- [x] 9.3 完成獨立確認頁、五種參與、批次、搜尋、統計、恢復、完整匯出／匯入往返、上一步及大量 Grid 保護。程式：`RegularRankMotherGroupConfirmForm`；本機：`RegularRankOperationCheck` 2066 人 Bounds 15ms、明細 2068 列 0ms ok。
- [x] 9.4 完成所有原版排名分支的參與集合、跨年級隔離及原公式保護，補有意義的離線數值／資料夾具比較。程式：`RegularRankCandidateSql`（年級×五種類先套參與再跑公式）、`RegularRankFormulaSql`、`RegularRankParticipationGate`；本機：`RegularRankCandidateSqlCheck`、Operation 公式行比對、baseline/compare 夾具既有通過。主機五旗標數值仍待驗證。
- [x] 9.5 完成確認到計算、背景執行、組態 Save、整次原子儲存、快照與四表結果處理的正式呼叫鏈；零明細、取消、防重入、錯誤恢復完整，不以硬停用或原型取代功能。程式：`TryCalculateFromConfirm` → `VerifyTestTransport`（通過後才 Save）→ `PublishForVerification`；`RegularRankPublishPlan`／`RegularRankCalculationGuard`；確認頁計算按鈕依清單就緒啟用。傳輸未通過時明確停止、不假成功。
- [x] 9.6 完成可替換發布傳輸邊界、實際 adapter、不重送、操作識別、不確定回應核對及並行策略程式；本機替代執行器驗證次序與失敗流程。程式：`RegularRankPublishExecutor`（`IRegularRankSqlExecutor`／`SubstituteRegularRankSqlExecutor`／`RegularRankPublishPipeline`）、沿用 `SemesterRankPublishTransport`；本機：`RegularRankPublishPipelineCheck` ok。**唯一未確認邊界**：遠端 DSA adminquery 是否支援同連線 SERIALIZABLE 多語句或單一 DO（既有 DSA 504，見 backend-transport-check-20261105.md）；接線方式為 `QueryHelperRegularRankSqlExecutor` 包 `OpenWriter` 後呼叫 `VerifyTestTransport`／`ComposePublishSql`／`ExecutePublish`。不宣稱資料庫回滾或鎖語意已驗收。
- [x] 9.7 完成 RegularAssessmentRankSelect 的期間／exam／批次／有效排名及原有篩選、空結果、刷新、錯誤處理與計算結果契約銜接。程式：`DAO/RegularAssessmentRankQuery.cs` 轉接、`RegularAssessmentRankSelect`；本機：`RegularRankRetrievalCheck` ok。主機實際檢索仍待驗證。
- [x] 9.8 完成 RegularMatrixRankSelect 的矩陣／學生明細、統計與母群對應、明細接合、匯出及查詢錯誤恢復，不混入學期資料。程式：`RegularMatrixRankSelect` 加上 `定期評量/%` 限定、空結果提示、`MatrixBatchJoinSql` 契約；本機：RetrievalCheck ok。
- [x] 9.9 完成 .NET Framework 4.8 建置、相關離線 Check、至少 2066 人 Grid、OpenSpec strict 與 diff check，保留實際命令及結果。MSBuild Debug 成功；Operation／Designer／Pipeline／Retrieval／Candidate／Excel／History／Context／Period Check 皆 exit 0；`openspec validate … --strict --no-interactive` 通過；`git diff --check` 通過。
- [x] 9.10 更新功能清單、交接及測試指南，交付 DLL 版本／雜湊與未來主機最短驗收步驟；分列功能實作、本機驗證、主機待驗證及仍未完成的程式缺口。見 `implementation-handoff.md`、`tester-guide.md`。未 Commit／Push／Merge／Archive／部署／改 schema。
