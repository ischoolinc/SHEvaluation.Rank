# Design

## Context

狀態：決策已於 2026-10-30 核准，Apply 進行中。本輪只完成決策同步與 Task 2.1，尚未改定期排名程式。工作目錄 SHEvaluation-Regular，分支 feature/regular-rank。原版基準 commit：2ca88cbd31aa8dd1507b207973f860005529b30a。以下位置均相對專案根目錄，為此基準的程式行號。公式保護區與十八個排名分支見 baseline/task-2.1.md。

### 現有事實與證據

- CalculateRegularAssessmentRank.cs:33 建構子讀取系統期間、考試、學生及 XML；:79 Load 保留一般與延修、有班級及年級學生。:209 reloadSubject 使用系統期間及目前班級年級；:225 以名稱找 exam ID，迴圈沒有 break，同名時留下最後一個；:599 計算使用 First()，同名時取第一個；:880 成績 SQL 比對 exam_name。同名考試有歧義，科目查詢與計算可以選到不同 ID。
- :277 btnNext_Click 直接刪除不排名類別學生；:475 btnCacluate_Click 從 Grid 組學生 SQL；:710 先獨立建立批次，失敗只寫 Console；:2566 逐年級各次 Select，不能回滾整次計算。
- :821 student_row；:825～839 三組採計科目；:840 score_detail_row；:898、1063、1277、1473、1670、1873 是科目、領域、總分、平均、加權總分、加權平均的一般排名視窗，每組另有 tag1/tag2。
- :1108、1177 類別領域來自 exam_score 再篩類別科目，類別總計則來自 exam_score_tag1/tag2；此差異是原版基準，不順帶修正。:2044 score_list 再算統計；:2411 舊矩陣失效；:2502 batch student 寫入未指定 matrix_dept。
- MotherGroup/CurrentStudentMotherGroupSource.cs:35 與 CurrentStudentMotherGroupBuilder.IsCurrentPopulation 只接受一般生，不能直接套用定期評量。
- MotherGroup/CalculationPeriod.cs 的歷史名稱與 CalculationPeriodBinder 帶學期語意；CalculateSemesterAssessmentRank.ApplyCalculationPeriodEditors 限定目前來源使用系統期間。
- MotherGroup/SemesterHistoryBatchCatalog.TryReadSetting 與 HistorySnapshotRestore.Load 限定學期 XML；LoadStored 的 JSON 路徑不取代外層來源類型驗證，還會套用目前排除標籤。
- MotherGroup/MotherGroupExcelExchange.Headers 是含學生狀態的 13 欄；MotherGroupExcelImport.SelectSql 使用學號加狀態對應。D08 已核准沿用此契約，不改成單純學號識別。
- MotherGroup/MotherGroupSnapshotJson.Compose/Read 使用 extension JSON 陣列的版本 1；StudentNode 不保存狀態、座號或獨立識別班級，且依賴 SemesterRankSql.TryParseGradeYear。
- SemesterRankMotherGroupConfirmForm.cs:12 直接耦合學期 host；SemesterRankSql.ApplyParticipation 與 SemesterRankPublishSql.Build 綁定學期 SQL 與 exam ID -1，不能直接當定期評量引擎。
- 學期 Change tasks 11.2、12.6、13.3、14.4、14.5 仍有人工／資料庫驗收未完成，不將其存在當成定期評量驗收證據。

## Goals / Non-Goals

目標：歷史期間、三種母群來源、五種獨立參與、可追溯快照與整次發布一致性，並在全部參與時重現原版結果。

非目標：修改學期排名 SQL／畫面／OpenSpec、調整成績公式、改變三組科目的既有運算語意、新增資料表、直接部署正式系統。本輪不修改排名程式。

## Decisions

regular-decisions.md 的建議方案已於 2026-10-30 核准，包含來源先載入再確認科目、過期快取防護、人工加人重載、零明細年級與跨連線並行。D02、D05、D06、D07、D08 採用 semester-reference.md 的參照方案。估時以 132～216h 為準。核准代表政策確定，不代表功能已實作。

### 1. 計算條件與來源分開建模

新增 RegularRankCalculationContext（暫名）：目標 SchoolYear、Semester、ExamId、ExamName、選定年級、三組科目、標籤條件及來源資訊。所有值於下一步驗證，計算前提交未完成編輯並複製為工作副本。來源批次的期間與考試可以與目標不同，不得默默取代目標條件；兩組都要顯示。來源批次無法唯一識別其考試時阻擋載入。

考試以 ID 作識別、名稱作顯示。同名舊批次無法唯一辨識時提示歧義，不任取第一筆或最後一筆。歷史目標考試清單須核對目標課程與 sce_take 的 ref_exam_id，不能只列全校名稱。rank_batch.setting 保留既有 Setting XML，並在同一元素以屬性 ref_exam_id 保存考試 ID；既有只讀已知屬性的讀取端仍可讀，不新增資料表，不以 JSON 取代 XML。

科目查詢不能只把 _DefaultSchoolYear 換成畫面值：Excel／歷史來源的學生可能已無目前班級。以目標期間、目標 exam ID 及來源學生修課資料取得科目。變更期間、考試、年級、來源批次、匯入檔或影響母群的類別設定後，載入狀態過期並禁止下一步；重載成功且確認三組科目後才解除。三組勾選以科目名稱留交集。失敗時不用舊名單配新條件。歷史加人須回條件頁更新科目，原學生 Working 旗標保留，取消不寫入新學生。

### 2. 共用模型，新增專用協調

直接研究重用 public 的 StudentMotherGroup、RankSlot、MotherGroupConfirmList、MotherGroupConfirmGrid、MotherGroupSnapshotJson、MotherGroupExcelExchange/Import/Apply；其相依、空白類別及排除類別政策仍須逐項核對。

新增 RegularCurrentStudentMotherGroupSource、RegularHistoryBatchCatalog、RegularHistoryMotherGroupRestore、RegularRankConfirmRequest、RegularRankMotherGroupConfirmForm、RegularRankParticipationSql、RegularRankPublishSql（皆暫名）。目前來源保留一般＋延修資格，不以偽裝 IsRegular 或修改學期 Builder 達成。可呼叫無學期資格限制的標籤解析／模型基礎方法。

CalculationPeriod 的歷史顯示文字、Binder、來源名稱轉換，以及 HistoryCalculationScope 的 SemesterRankSql 相依不直接移植。JSON 可用來源 enum 呼叫；年級解析的既有唯讀相依須記錄並測試。若需抽取共用 helper，先交付影響與整合建議，不在此草案暗中批准。

### 3. 第二畫面與 Designer

UI 架構明確採兩個獨立 Windows Form：沿用 CalculateRegularAssessmentRank 作條件頁，新增 RegularRankMotherGroupConfirmForm 作確認頁。各自有 cs／Designer.cs／resx，不延續原定期單一 Form 兩 Panel 切換。下一步開啟確認 Form 時停用條件 Form；上一步保留條件並返回，關閉與計算中關閉參照學期 Form 生命週期。

專用 Form 有無參數建構子，只 InitializeComponent，不查資料。固定控制項置於 Designer；業務與 SQL 置於普通類別。識別班級／座號與排名母群分開顯示；歷史識別資料若只有目前值，欄名與說明標明目前資料，不能以目前班級冒充歷史值。五種母群名稱唯讀。

來源初始值、套用政策後的初始參與值、使用者 Working 值分清楚；恢復按鈕回到進入第二畫面的初始值。完整清單與搜尋顯示集合分開，搜尋不能縮小計算或匯出集合。計算中禁止重入與修改工作副本。

### 4. SQL 邊界與參與集合

A 段：條件、學生與五種旗標。B 段：既有成績公式、排名與統計。C 段：結果發布。先固定基準與標記，再作最小必要修改，不大規模搬動 SQL。

參與 Gate 的核准方向：各排名視窗 partition 加入該種 participate_*，使 true/false 在 RANK、反向 RANK、COUNT、ROW_NUMBER 前分開；score_list 各類型分支再以對應旗標限制輸入，使後續 STDDEV_POP、五標、平均、分布只包含參與者。此方向已核准，正確性仍須於 Task 6 以覆蓋檢查與逐筆比對證明，本輪不改 SQL。

六組一般排名同時算年／班／科，不能用同一 WHERE 排除任一旗標。六組及 tag1/tag2 全部覆蓋，包含 domain、無科目維度的總計、PR 與 percentile。旗標可在聚合完成後按唯一 student_id 接回，避免修改成績聚合公式；若需隨欄位傳遞，須證明不改聚合筆數／分組。

禁止改 score_detail_row 的 -1→0、NULL／-2 排除、學分、領域／四項總計算式、三組科目來源、PR／percentile、新五標、標準差與分布算式。禁止算完名次才刪學生。

若全部年級合併發布，保留原版逐年級計算隔離；尤其 class partition 原本沒有 grade_year，不能讓跨年級同名班級混算。可保留每年級獨立計算 CTE，再統一發布；具體方案由 SQL 比對證據決定。

### 5. 快照與歷史還原

setting 保留既有定期評量 XML 與三組科目，新增 exam ID 的相容表示須驗證既有讀取端。完整確認清單（含不參與與無成績者）以既有 JSON v1 寫 extension，同一次 INSERT 完成；不把 SQL 輸入 JSON 當 Snapshot。保留其他 extension 項目。

外層先核對來源批次類型／考試，再讀 JSON。JSON 不存在才評估既有 XML。JSON 或舊 XML 存在但損壞、重複、版本不支援或內容無法還原時停止，不得當成不存在；舊 XML 損壞時不回退 rank_batch_student。JSON 與 XML 母群快照均不存在時才依 D05 降級。舊定期批次缺 matrix_dept 或旗標時，不推定完整成功、不用目前學籍補母群。

參照學期 history-restore 規格，兩種母群快照均不存在時才讀 rank_batch_student，不以 rank_matrix/rank_detail 補足科母群。非空名稱預設參與、空白名稱不參與並提示；這是本次初始政策，不宣稱恢復原始旗標。多名衝突不任取一筆。原始參與狀態無法從「沒有排名明細」推得，因為也可能沒有成績。

### 6. 發布與失敗處理

優先研究一個 SQL statement 含批次建立、各年級結果、失效、新矩陣、學生及明細；透過 RETURNING 明確建立依賴。若使用交易，先證明 QueryHelper 同連線控制能力，禁止多次 Select 各自提交後假稱原子交易。

結果檢查涵蓋批次 ID、明細對應矩陣、學生對應、寫入筆數與空結果政策。SQL 內失敗需使整次回滾；只在 C# 查到零筆後顯示失敗，不能回滾已提交語句。失效範圍限目標學年期、目標 exam ID、實際計算年級與定期評量 item_type。全體無明細，或任一實際取代年級沒有新明細時，整次失敗並保留舊有效排名。個別學生或單科無成績仍依原公式省略該項明細。

學校組態 Save 在確認後、排名 SQL 前，不屬排名交易；排名失敗不回滾該設定，確認文案須明示。取消不 Save。同一目標學年期與 exam ID 的發布互斥，不按年級拆鎖；鎖只涵蓋發布交易。閘道無法提供可靠互斥時維持阻擋，不新增資料表，不改成逐年級提交。

## 已核准決策

使用者於 2026-10-30 同意 regular-decisions.md 的建議方案並授權 Apply。D02、D05、D06、D07、D08 採用 semester-reference.md 的參照方案。以下為核准結果。依據與學期差異仍記在 semester-reference.md。

- D01：三種來源都可選歷年目標，預設系統期間。目前來源是目前一般＋延修母群加上指定期間成績，不重建當時名冊。
- D02：歷史目錄只含定期評量批次，包含失效批次，排除學期與技職繁星，並核對批次設定與排名類型。
- D03：來源期間與來源考試可以不同於目標，兩組同時顯示，來源不覆寫目標；來源考試無法唯一識別時阻擋。
- D04：先載入母群，再依目標期間、目標 exam ID 與來源學生修課確認三組科目。條件變更使載入過期。加人後回條件頁，保留原學生 Working 旗標。
- D05：JSON 與 XML 母群快照均不存在時，才從 rank_batch_student 降級。快照存在但損壞、重複、版本不支援或內容無法還原時停止，不得當成不存在。非空名稱預設參與，空白不參與並提示。不查明細補名。
- D06：無成績學生留在清單與 Snapshot，只不產生該項明細。
- D07：三種來源先保留來源值，再套本次不排名類別。符合者五種初始不參與，名稱不變且可覆寫。不回寫舊批次。
- D08：Excel 沿用 13 欄與學號＋狀態。
- D09：歷史加人只依學號，不加狀態篩選；多筆阻擋。保留既有相同學號去重，不改學期共用方法。無法解析年級者留清單且不進排名。
- D10：整次原子發布。全體無明細，或任一實際取代年級無新明細，整次失敗並保留舊排名。失效限目標期間、目標 exam ID、實際計算年級與定期評量 item_type。同一目標期間與 exam ID 的發布互斥，不按年級拆鎖。

五種母群名稱唯讀。JSON v1 不新增狀態、座號或歷史識別班級。exam ID 寫入既有 Setting 的 ref_exam_id 屬性。學校組態先保存，排名失敗不回滾該設定；取消不保存。

## Risks / Trade-offs

- 最高風險為公式回歸、漏接視窗、跨年級混算與部分發布；以基準指紋、逐筆比較及故障注入驗收。
- 歷史資料缺欄位／缺快照只能有界還原，不能製造原始資料。
- SQL 字串替換脆弱；專用轉換必須驗證每個標記與預期覆蓋數，找不到即停止。
- 共用元件仍在學期內測；優先只讀重用，對學期現有未完成驗收不補勾。
- PostgreSQL 9.6～17 語法以最低版本為限；9.6 與 17 做完整比對，中間版本做相容 smoke；測試環境可用性與真實 schema 尚待核實。

## Validation / Rollout

原版基準先固定於上述 commit。用相同期間、考試、母群及三組科目做全參與逐筆比較，涵蓋所有矩陣識別鍵、score/rank/pr/percentile/matrix_count 與全部統計欄位，另比較重複列數與缺漏，不能只比總筆數。

測試包含一般＋延修、五旗標組合、同分、NULL／-1／-2、零／NULL 學分、三組不同科目、類別領域既有交集、空類別、無成績、歷史學生缺學籍、同名考試、跨年級同名班、年級不可解析、Excel 往返、JSON/XML／無快照／壞快照、全部不參與與並行發布。

在獨立測試庫驗證成功寫入及批次／矩陣／學生／明細階段故障回滾，保存前後四表與有效矩陣證據。VS2022 實際開啟、儲存、重開兩個定期 Form，完成縮放、上一步、取消、重試、防重入與學期回歸。靜態檢查或編譯不等於此人工驗收。

## 2026-11-03 new_debug6 修正整合

2026-11-05 交易原型實測：new_debug6 為 PostgreSQL 17.11，四表、extension=jsonb、matrix_dept 及矩陣唯一限制已取得目錄證據；QueryHelper 的顯式 BEGIN／SELECT／ROLLBACK 探測被 adminquery 以 DSA 504 拒絕，不能採用未證明的多指令方案。

新增待驗證替代：單一 DO 匿名區塊，不建立資料表／欄位／持久函式。先以獨立 PL/pgSQL 命令取得目標交易鎖，再執行整次排名發布命令，結果由既有操作識別另行核對，不重送發布。依 [PostgreSQL 9.6 DO 文件](https://www.postgresql.org/docs/9.6/sql-do.html)、[函式快照說明](https://www.postgresql.org/docs/17/xfunc-volatility.html) 及 [匿名區塊編譯原始碼](https://github.com/postgres/postgres/blob/REL9_6_STABLE/src/pl/plpgsql/src/pl_comp.c)，匿名區塊按非唯讀程序處理，內部命令於 READ COMMITTED 可取得新快照；這是設計依據，仍須以兩連線重疊發布實測證明鎖後可見性與最終結果，不能僅靠此推論勾選 7.5。資料服務若不接受單一 DO 探測，保持計算停用，提出閘道能力需求，不降級為分次提交。

問題依據與驗收詳見 new-debug6-risk-review.md。本次檢查尚未在定期畫面重現；現有定期使用一般 DataGridViewRow，具有相同輔助存取風險。新確認頁三個 Grid 在加列前使用 MotherGroupConfirmGrid.Configure／ConfigurePopulation／ConfigureDetail，並沿用其受保護填列路徑；只設定 RowTemplate 不會修正直接 new 出來的資料列或既存列。Task 3.4 盤點新舊路徑，Task 8.7 分別保留本機 STA 與實際跨程序證據。

共用元件已同步學期 c8b386d，原 Task 2.3 分析不代表新版本政策已驗證。Task 4.7 重新檢查定期 D02／D05／D07：歷史目錄只含定期批次，空白／衝突與排除預設沿用定期核准規格。以定期來源轉接與回歸保護差異，不把學期最新預設自動視為定期新政策，不修改學期既有行為。

## 2026-11-24 評量排名發布調整

評量排名改用單一 `WITH … SELECT` 經 `QueryHelper.Select` 發布，沿用學期排名的送出方式。發布前只確認測試資料庫及排名表結構。鎖、停用舊排名、寫入四張排名表與完整性檢查均在同一條語句。回應無法確認時使用操作識別查詢批次。上方交易腳本與匿名區塊為歷史原型。主機端寫入、回滾及雙連線並行仍待驗證。

## Estimated Effort

規格 12～18h；基準與比較 8～12h；條件資料流 12～20h；來源與畫面 24～36h；快照 8～14h；Participation 20～32h；發布 12～20h；整合驗收與修正 24～40h。加上來源快取失效、加人返回重載與跨連線並行原型後，合計 132～216h（約 16.5～27 人日），不含等待與無法取得測試環境的時間。若改共用契約或跨類型歷史來源須重估。
