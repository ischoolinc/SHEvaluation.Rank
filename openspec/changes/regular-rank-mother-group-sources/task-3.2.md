# Task 3.2 期間與來源固定控制項

狀態：控制項與 D01～D04 的畫面政策已接到條件頁，離線檢查與專案編譯通過。沒有開啟 VS2022 Designer，沒有連線資料庫，沒有操作實際畫面。沒有修改學期 Form、學期 SQL 或學期 OpenSpec。沒有新增 schema，沒有改定期排名公式。new_debug6 沒有在本輪重現，也沒有修復。

## 控制項

`CalculateRegularAssessmentRank.Designer.cs` 新增並固定這些條件頁控制項：

- 目標學年度、目標學期。三種來源都可改，預設採用系統期間。
- 目標考試沿用 `cboExamType`，項目改為帶 exam ID 的物件。同名考試顯示 ID；儲存的考試名稱對到多個 ID 時不自動選第一筆或最後一筆。
- 母群來源：目前學生、Excel、歷史固定排名。
- 目前來源說明：「依目前學生母群計算指定期間成績」。
- 來源學年度、來源學期、來源考試、來源批次。來源批次目錄尚未載入，控制項保持空白。
- Excel 識別為唯讀，本輪沒有匯入檔。
- 「載入／更新母群及科目」、「確認三組科目」、狀態文字。下一步預設停用。

Designer 沒有 SQL。表單沒有呼叫 `CalculationPeriodBinder`，也不把學期的「依目前學生資料產生」或「歷史學期成績排名」當成定期來源。

## D01～D04

- D01：目前、Excel、歷史三種來源的目標學年度與學期都保持可編輯。查詢期間只讀目標值，不因選擇目前來源而鎖回系統期間。
- D03：來源期間、來源考試與目標分開顯示、分開寫入。來源寫入失敗或尚未有批次時，不覆寫已保存的目標期間與目標 exam ID。
- D04：變更目標期間、考試、年級、來源或類別條件會使已載入狀態過期，並停用下一步。確認三組科目是獨立按鈕，不會自動進入學生頁。載入按鈕只檢查畫面條件；母群與科目查詢尚未接入，所以不查資料、不標記載入成功，也不沿用系統期間的舊科目清單。
- 原 `reloadSubject` 的科目 SQL 字串維持基準內容，期間與來源控制項不呼叫它。

`RegularRankCalculationContext` 另加 `ClearTargetExam` 與 `SetSourceKindWithoutIdentity`。目標考試改成歧義或尚未選擇時，會清掉先前的 exam ID，避免畫面與條件不一致。這不改變既有 `SetTargetExam` 在失敗時保留原值的行為。

## 驗證

離線檢查只編譯 context 與期間來源編輯器：

`C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /codepage:65001 /target:exe /main:SHEvaluation.Rank.RegularRank.RegularRankPeriodSourceEditorCheck`

結果：exit 0，輸出 `regular-rank-period-source-check: ok`。原 Task 3.1 的 context 檢查重新編譯後同樣為 `regular-rank-context-check: ok`。

專案編譯：

`MSBuild SHEvaluation.Rank.csproj /t:Build /p:Configuration=Debug`

結果：exit 0，產生 `bin\Debug\SHEvaluation.Rank.dll`。警告只有既有的 `useWeightedAvg` 未使用，與本任務無關。這不是 VS2022 Designer 開啟、儲存、重開，也不是畫面或資料庫驗收。

## 未完成

- 沒有用 VS2022 開啟、調整或儲存 Designer。控制項位置尚未在畫面上確認。
- 沒有連線測試庫。Task 2.4 維持未勾選。考試清單仍來既有建構子已讀取的 `Exam.SelectAll()`，不是歷史批次目錄。
- 載入按鈕不能取得母群或三組科目。下一步因此停在條件頁，原學生頁與排名計算不會從這次新流程進入。
- 根目錄沒有 `Todo.md`。本輪只讀到其他 `Todo-*.md`，沒有把那些檔案當成這項任務的規格。
