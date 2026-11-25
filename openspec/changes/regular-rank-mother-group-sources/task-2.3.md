# Task 2.3 共用類別適用性

狀態：靜態分析完成，並已補上 JSON 還原成功後套用本次不排名預設的協調步驟。本輪沒有修改正式程式、共用類別、`baseline/source` 或學期 Change，沒有連線資料庫，沒有編譯或執行這些類別，也沒有開始 Task 2.4。

工作目錄 `C:\ischoolProject\SHEvaluation-Regular`，分支 `feature/regular-rank`，HEAD `2ca88cbd31aa8dd1507b207973f860005529b30a`。另一個 worktree `C:\ischoolProject\SHEvaluation.Rank` 停在同一個 commit 的 `master`，本輪沒有切換或修改它。

判讀依據是已核准的 `design.md`、四份 spec、`semester-reference.md`、`regular-decisions.md`，以及下列原始檔。行號是這次讀到的檔案內容。OpenSpec 驗證通過只表示 change 結構有效，不表示這些類別已在定期評量跑過。

共用型別都在組件 `SHEvaluation.Rank`，命名空間 `SHEvaluation.Rank.MotherGroup`，由 `SHEvaluation.Rank.csproj` 編譯。確認 Form 在命名空間 `SHEvaluation.Rank`。定期程式若放在同一個 csproj，可以呼叫 `public` 與 `internal` 成員。本輪沒有看到需要改可見性才能共用的點。

判定只用三種：

- **可原樣共用**：定期直接呼叫現有公開成員，不改該檔。
- **需定期專用轉接**：類別保留，定期只呼叫其中不含學期政策的成員，並由新的定期型別決定呼叫順序。
- **需新增專用類別**：現有型別的政策、宿主或 SQL 不能拿來當定期實作。

## 總判

| 型別 | 檔案 | 判定 |
| --- | --- | --- |
| `StudentMotherGroup` | `MotherGroup/StudentMotherGroup.cs` | 可原樣共用 |
| `RankSlot`、`RankKinds` | `MotherGroup/RankSlot.cs` | 可原樣共用 |
| `MotherGroupSourceKind`、`MotherGroupInitialParticipation` | `MotherGroup/MotherGroupInitialParticipation.cs` | 可原樣共用 |
| `MotherGroupConfirmList` 與統計／列型別 | `MotherGroup/MotherGroupConfirmList.cs` | 可原樣共用 |
| `MotherGroupConfirmGrid` | `MotherGroup/MotherGroupConfirmGrid.cs` | 可原樣共用 |
| `MotherGroupExcelExchange`、`MotherGroupExcelImport`、`MotherGroupExcelApply` 及匯入 DTO | `MotherGroup/MotherGroupExcelExchange.cs`、`MotherGroupExcelImport.cs`、`MotherGroupExcelApply.cs` | 可原樣共用 |
| `MotherGroupSnapshotJson`、`MotherGroupJsonRead`、`MotherGroupExtensionMark` | `MotherGroup/MotherGroupSnapshotJson.cs` | 可原樣共用 |
| `MotherGroupSnapshot` | `MotherGroup/MotherGroupSnapshot.cs` | 可原樣共用 |
| `HistoryStudentAdd` 與結果型別 | `MotherGroup/HistoryStudentAdd.cs` | 可原樣共用 |
| `HistoryCalculationScope` | `MotherGroup/HistoryCalculationScope.cs` | 可原樣共用 |
| `TagDefinition`、`StudentTagLink`、`CurrentStudentMotherGroupBuilder.ResolveTagIds`、`FirstTagName` | `MotherGroup/CurrentStudentMotherGroupBuilder.cs` | 可原樣共用 |
| `CurrentStudentDeptQuery.Create` | 同上 | 可原樣共用 |
| `SemesterRankSql.TryParseGradeYear` | `MotherGroup/SemesterRankSql.cs` | 可原樣共用 |
| `HistoryRankRestore.SelectBatchStudentSql`、`ReadBatchStudents` | `MotherGroup/HistoryRankRestore.cs` | 需定期專用轉接 |
| `CalculationPeriod` | `MotherGroup/CalculationPeriod.cs` | 需新增專用類別 |
| `CalculationPeriodBinder` | `MotherGroup/CalculationPeriodBinder.cs` | 需新增專用類別 |
| `CurrentStudentMotherGroupSource` | `MotherGroup/CurrentStudentMotherGroupSource.cs` | 需新增專用類別 |
| `CurrentStudentMotherGroupBuilder.Build`、`IsCurrentPopulation` | `MotherGroup/CurrentStudentMotherGroupBuilder.cs` | 需新增專用類別 |
| `HistorySnapshotRestore` | `MotherGroup/HistorySnapshotRestore.cs` | 需新增專用類別 |
| `SemesterHistoryBatchCatalog`、`HistoryRankBatch` | `MotherGroup/SemesterHistoryBatchCatalog.cs` | 需新增專用類別 |
| `SemesterRankSql.Build`、`ApplyParticipation`、`StudentSelect` | `MotherGroup/SemesterRankSql.cs` | 需新增專用類別 |
| `SemesterRankPublishSql` | `MotherGroup/SemesterRankPublishSql.cs` | 需新增專用類別 |
| `SemesterRankConfirmRequest` | `MotherGroup/SemesterRankConfirmRequest.cs` | 需新增專用類別 |
| `SemesterRankMotherGroupConfirmForm` | `SemesterRankMotherGroupConfirmForm.cs` | 需新增專用類別 |

`HistoryRankRestore.Read` 與 `SelectSql` 會聯集 `rank_detail`。定期降級只可呼叫 `ReadBatchStudents`，不可呼叫 `Read`。

## 模型與初始參與

### StudentMotherGroup、RankSlot

`StudentMotherGroup` 是 `public sealed`。公開成員有 `StudentId`、`StudentNumber`、`StudentName`、`Status`、`MatchesExcludedCategory`、`Original`、`Working`、`SetSourceSlots`、`GetOriginal`、`GetWorking`。五個順位固定來自 `RankKinds`：年排名、科排名、班排名、類別1排名、類別2排名。未知或重複類型會丟 `ArgumentException`。

`RankSlot` 是 `public sealed`，有 `RankType`、`GroupName`、`Participate`、`ConflictNames`、`HasConflict`、`Clone`。母群名稱與參與旗標分開。

這兩個型別沒有學期資格、考試或 SQL。定期三種來源都應產出它們。`Status` 可空；目前來源應寫入原版狀態碼，延修是 2。JSON v1 不保存狀態，還原後的 `Status` 可以是空的，這是既有契約，不是缺欄位要補進共用 JSON。

### MotherGroupSourceKind、MotherGroupInitialParticipation

列舉是 `public`，值為 `Current`、`HistorySnapshot`、`Excel`。`Apply` 先把目前 `Original` 複製一份，再用 `SetSourceSlots` 寫回 `Original` 與 `Working`，接著只改 `Working` 的 `Participate`：

- 類別一名稱或類別二名稱空白時，該項不參與。
- 只有 `Current` 會把空白科別改成參與。`HistorySnapshot` 不會。
- `matchesExcluded` 為 true 時，五種參與都設為 false。
- 有衝突名稱時，該項不參與。

母群名稱在 `Original` 與 `Working` 都保持複製前的來源名稱。`Original` 的參與值也保持來源值。`Apply` 不寫入資料庫、XML 或 extension。它每次都會用 `Original` 重設 `Working`，所以學生已在確認畫面改過旗標之後，不能再對同一批學生呼叫。

這符合已核准的 D07：先留來源名稱與來源旗標，再套本次不排名類別，而且可以在確認畫面覆寫。定期目前來源、Excel 與歷史還原應分別傳入 `Current`、`Excel`、`HistorySnapshot`。不要為了讓延修生通過而把 `IsRegular` 設成 true，也不要改 `Apply`。

空白科別只在 `Current` 被設成參與，是學期現況。歷史加入學生會傳 `Current`，因此沿用這個結果。本輪不把它改成定期專用規則。

## 確認清單與 Grid

`MotherGroupConfirmList` 是 `public sealed`。`Load` 記住進入清單時的參與值；`Restore` 回到那些值，衝突項仍強制不參與。`CopyWorking` 用當時的 `Working` 當新的來源槽。`SetParticipate` 依學生 ID 改一種排名。`VisibleStudentIds` 只產生顯示用 ID，不刪除 `Rows`。`Summarize` 用清單上的參與旗標計數，不查成績。`ContainsIdentity` 在學生 ID 相同，或學號 trim 後相同時回 true。`Add` 也用這個條件拒絕重複。

`MotherGroupConfirmGrid` 是 `public static`。`Configure` 建立識別用的班級、座號、學號、姓名，以及五種母群名稱與參與欄。文字欄與母群名稱欄 `ReadOnly = true`，只有參與核取方可編。`ApplyVisibility` 只改列的可見性。人口與明細 Grid 是唯讀統計。

定期確認畫面直接呼叫這兩個型別。歷史列的「班級／座號」若來自目前主檔，欄名要由定期 Form 在 `Configure` 之後改掉，標明目前資料。不要改共用 Grid 的預設標題，否則學期畫面標題會一起變。排名班名仍在「母群：班排名」，與識別班級是不同欄。

`ContainsIdentity` 會把不同 `student.id`、相同學號視為已在清單。D09 已核准保留，並要求攔下時說明原因。不要改這個方法。學期 tasks.md 的 5.7 仍是未完成的獨立待辦，本輪不把它寫成已修。

## 目前來源

`CurrentStudentMotherGroupSource.Load` 是 `public`。它把 `StudentRecord` 轉成 `CurrentStudentRow`，`IsRegular` 只在 `StudentRecord.StudentStatus.一般` 時為 true。延修會得到狀態碼 2，但 `IsRegular` 仍是 false。接著呼叫 `StudentTag.SelectAll()`，並用 `QueryHelper.Select` 查科別。本分析沒有執行這兩個呼叫。

`CurrentStudentMotherGroupBuilder.IsCurrentPopulation` 要求 `IsRegular`、有 `RefClassId`、而且年級有值。`Build` 先排除不符合的列，再組出五種母群：年排名名稱是「年級＋年級」、科別來自查詢、班名來自目前班級、類別來自標籤。它自己不呼叫 `MotherGroupInitialParticipation.Apply`；學期畫面是在建完後另外套用。

定期原版 `CalculateRegularAssessmentRank_Load` 保留的是有班級、年級，且狀態為一般或延修的學生。直接呼叫 `Load` 或 `Build` 會少掉延修生。

因此定期要新增自己的目前來源。它可以呼叫公開的 `ResolveTagIds`、`FirstTagName` 與 `CurrentStudentDeptQuery.Create`，再呼叫 `MotherGroupInitialParticipation.Apply(Current, ...)`。`CurrentStudentDeptQuery.Create` 把學生 ID 直接串進 SQL，沒有參數化；定期若沿用，只能傳已是數字的 ID。這是既有寫法，本輪不改。

`HasTag` 在 Builder、Excel、歷史還原與歷史加入裡各有一份 private 複本。不要為了去重而改這些檔案。

## 期間與 Binder

`CalculationPeriod` 是 `public sealed`。來源名稱常數是「依目前學生資料產生」、「歷史學期成績排名」、「Excel 母群清單」。它有目標學年度、學期、來源名稱、歷史學年度、歷史學期與歷史批次 ID，沒有 exam ID，也沒有來源考試與目標考試的區分。`CreateDefault` 把學期收成 1 或 2。

`MotherGroupSnapshot.KindFromSourceName` 只接受上面三個常數。定期若使用別的顯示名稱，不能靠這個方法轉種類，應直接傳 `MotherGroupSourceKind`。

`CalculationPeriodBinder.Apply` 會把來源下拉設成這三個學期名稱，並把歷史學年度、學期控制項停用。`RestoreSystemPeriod` 把計算期間寫回系統學年度與學期。學期 `CalculateSemesterAssessmentRank.ApplyCalculationPeriodEditors` 在目前來源時呼叫它，並停用期間控制項。定期 D01 是三種來源都可選歷年目標，預設才是系統期間。

定期條件應使用 design 已暫名的計算 context，自行保存目標學年度、學期、exam ID、考試名稱，以及來源期間與來源考試。不要呼叫 `CalculationPeriodBinder`。

## Excel

`MotherGroupExcelExchange.Headers` 是 13 欄：學號、姓名、學生狀態、五種母群、五種參與。狀態只接受一般、延修、休學、輟學、畢業或離校、刪除，對應 1、2、4、8、16、256。`TryStatusCode` 與 `TryStatusName` 是這個對照。缺狀態時 `SelectCurrentStatusSql` 用 `student.id` 查目前 `student.status`；查到的是目前狀態，不是歷史狀態。

`MotherGroupExcelImport.SelectSql` 用 `(student.student_number, student.status)` 配對。`Validate` 要求工作表「母群」、欄位順序正確、參與只接受「參與」或「不參與」。任一列錯誤或同一身份對到多位學生時，整份 `IsValid` 不會成立。相同學號、不同狀態可以是不同鍵。學號文字被保留，沒有在這段把前導零轉成數字。

`MotherGroupExcelApply.CreateGroups` 在驗證通過後建立 `StudentMotherGroup`。姓名不同時使用主檔姓名，並把警告留在 `NameWarning`。它以 `Excel` 呼叫 `MotherGroupInitialParticipation.Apply`，所以不會套用「空白科別改為參與」這條 `Current` 規則。

這組契約就是已核准的 D08。定期畫面呼叫這三個型別即可。歷史加入的身份是學號，與 Excel 的學號加狀態不同，不要合成同一個查詢。

本分析沒有開啟 Excel 檔，也沒有對資料庫執行 `SelectSql`。

## JSON 與 XML 快照

`MotherGroupSnapshotJson` 是 `public static`。`Compose` 在既有 extension JSON 陣列中替換或附加一筆 `MotherGroupSnapshot`，其他 extension 保留。版本固定為 1。`source_kind` 寫成 `Current`、`History` 或 `Excel`。只有歷史種類會寫正整數 `source_batch_id`，其他種類必須是 null。學生欄位只有 `student_id`、`student_number`、`student_name`、`grade_year`、`ranks`。沒有狀態、座號，也沒有獨立於排名班名的識別班級。`grade_year` 由年排名名稱交給 `SemesterRankSql.TryParseGradeYear`；解析失敗就寫 null，不丟棄學生。

`Read` 在空白文件時回 `NotPresent`。文件不是陣列、快照超過一筆、版本不是 1、欄位不符、學生空或學生 ID 重複時，`Present` 為 true 且 `Restored` 為 false。`Read` 只依 JSON 建立 `StudentMotherGroup`：`SetSourceSlots` 把快照的名稱與參與值同時放進 `Original` 與 `Working`。它不讀本次不排名類別，也不讀學生標籤，因此不會套排除預設。`Describe` 只報告快照筆數與是否有版本 1，不還原學生。

`MotherGroupSnapshot.Append` 與 `TryRestore` 處理 Setting 裡的 `MotherGroupSnapshot version="1"`。種類屬性是 `current`、`history`、`excel`。`TryRestore` 本身不看考試名稱；缺快照、XML 壞掉或內容不符都回 false。定期協調者必須先區分「沒有快照」與「快照損壞」，不能只靠 false 就降級。

`TryParseGradeYear` 會 trim，再去掉尾端的「年」「級」，然後要求正整數。它是純字串方法，不連線、不改 SQL。定期 JSON 與年級提示可以呼叫它。`SemesterRankSql` 的其他方法不能一起拿來用。

## 歷史目錄、還原與加入

`SemesterHistoryBatchCatalog.SelectSql` 讀全部 `rank_batch`，不在 SQL 依考試名稱篩選。`TryReadSetting` 要求根節點是 `Setting`，而且 `考試名稱` 等於「學期成績」；不符合就回 null，該列不會進入清單。顯示文字有批次、學年度、學期、說明、年級與快照來源，沒有考試 ID。因此定期批次不會出現在學期目錄，這是預期結果。定期不能改這個判斷來塞進定期批次。

定期歷史目錄要新增專用型別。篩選必須核對定期批次設定與排名類型，不能只因名稱不是「學期成績」就收下。清單要能顯示來源批次、期間與考試，並在來源考試無法唯一識別時讓載入被擋下。這些規則尚未對資料庫執行；Task 2.4 才核對實際欄位。

`HistorySnapshotRestore.Load` 在 XML 無法剖析時失敗，在 `考試名稱` 不是「學期成績」時回 `WrongExamMessage`，快照缺漏、重複或還原失敗也失敗。`LoadStored` 若 JSON 已存在且失敗，直接失敗，不會改走 XML；JSON 不存在才呼叫 `Load`。兩種成功路徑都會用目前標籤再呼叫 `MotherGroupInitialParticipation.Apply(HistorySnapshot, ...)`。

定期不能呼叫 `Load` 或 `LoadStored`。JSON 不存在時，`Load` 會因考試名稱拒絕定期批次；若日後有人放寬這個判斷，又會改變學期行為。`ApplyExcluded` 是 `HistorySnapshotRestore` 的 private 方法，定期協調者要自己做同等步驟，不能改共用檔把該方法公開。

定期協調者在首次把一個歷史來源載入成確認清單時，依序做：

1. 先確認這筆是可接受的定期批次與來源考試。
2. 呼叫 `MotherGroupSnapshotJson.Read`。`Present` 為 true 且 `Restored` 為 false 時停止，不讀舊 XML，也不讀 `rank_batch_student`，不用目前學生名單代替。
3. `Present` 與 `Restored` 都為 true 時，用本次不排名類別與目前學生標籤判斷每一名學生，再套初始參與。做法與 `ApplyExcluded` 相同：`CurrentStudentMotherGroupBuilder.ResolveTagIds(tagDefinitions, excludedCategory)` 取得標籤 ID；學生的 `StudentTagLink` 裡有相同 `StudentId` 且 `TagId` 落在這份 ID 中，則 `matchesExcluded` 為 true。接著對每一名剛還原的學生呼叫 `MotherGroupInitialParticipation.Apply(MotherGroupSourceKind.HistorySnapshot, student, matchesExcluded)`。此呼叫保留快照母群名稱與 `Original` 裡的來源參與值，只調整 `Working` 的參與狀態，不回寫舊批次的 setting、extension 或排名表。完成後才把學生放進 `MotherGroupConfirmList.Load`。`Load` 記住的是套用預設後的 `Working`；恢復按鈕回到這個初始值，不是快照內的原始參與值。
4. `Read` 回 `NotPresent` 時，才檢查舊 XML。XML 存在但損壞、重複或無法還原就停止，不降級。可還原時呼叫 `MotherGroupSnapshot.TryRestore`，再對還原出的學生做與第 3 點相同的標籤判斷和 `Apply(HistorySnapshot, ...)`。
5. JSON 與 XML 母群快照都不存在時，才呼叫 `HistoryRankRestore.ReadBatchStudents`。該方法內部已經依同一組排除標籤呼叫過一次 `Apply(HistorySnapshot, ...)`。協調者不要對同一批學生再呼叫一次。

上述第 3 點與第 4 點的初始化只發生在這次來源首次進入確認清單時。科目重載只更新目標期間的科目候選，不得重新 `Read` 或對已在清單內的學生再呼叫 `Apply`，否則 `Apply` 會用 `Original` 蓋掉使用者已調整的 `Working`。歷史人工加人只對 `HistoryStudentAdd.Read` 新建的那一名學生套初始規則；該方法內部使用 `Current`。原清單上的學生保持已調整的 `Working`。取消加入時不把新學生寫入清單。計算使用確認後的 `Working`，不再次套不排名預設。

重新選擇來源批次、重新匯入 Excel，或重新從目前學生建立名冊，是另一次來源載入，要重新初始化。科目重載與歷史加入不是另一次來源載入。

`HistoryRankRestore.SelectSql` 與 `Read` 會把 `rank_batch_student` 的矩陣欄位和 `rank_matrix`／`rank_detail` 的名稱做聯集。`Read` 因此可能用排名明細補名稱。定期 D05 禁止用明細補母群，所以這兩個成員不是定期降級路徑。

`SelectBatchStudentSql` 與 `ReadBatchStudents` 只讀 `rank_batch_student` 左接 `student` 的學號與姓名。科別空白時，`MergeSlot` 不把目前科別填進去。非空名稱依 `RankParticipates` 預設參與；空白名稱不參與並產生警告。多名衝突不任取一筆。這是降級路徑可呼叫的部分。`matrix_dept` 缺值時，定期畫面必須把降級講明白；本輪沒有查實際資料，不能宣稱舊定期批次的科別現況。

`HistoryStudentAdd.SelectSql` 只依 `student.student_number` 查詢，沒有狀態條件。零列是 `NotFound`，多列是 `Ambiguous`。`Read` 再用 `ContainsIdentity` 拒絕已在清單的學生。年級能解析才產生「N年級」，否則年排名名稱是空的，但初始 `Participate` 仍先設 true；因為傳入的是 `Current`，空白科別也維持參與。沒有班級時年排名名稱可以是空白且參與為 true。這是學期遺留行為，D09 要求定期不要為了改它而去動共用方法。無法解析年級的學生仍留在清單；計算前的提示可呼叫 `HistoryCalculationScope.UnparsedYearMessage`。

`HistoryCalculationScope.KeepsStudentWithoutScore` 只檢查清單裡有沒有該學生 ID，不查成績。它可以用來表達「無成績者仍在清單」，不能代替日後的成績查詢驗收。

## SQL 與確認 Form

`SemesterRankSql.Build` 依年排名名稱分年級，解析失敗的學生放入 `SkippedWithoutGrade`，不另造年級。`ApplyParticipation` 在學期公式裡尋找 `subject`、`entry`、`rank_dept_name`、`rank_tag1`、`rank_tag2`、`rank_class_name` 與 `score_list` 到 `update_data` 的片段，再插入參與旗標。定期成績 SQL 的視窗與 `item_type` 不是這組文字。Task 6.1 已規定不直接套用 `ApplyParticipation`。定期要另寫參與轉換，並在找不到預期標記時停止。

`SemesterRankPublishSql.Build` 把每個年級的 `ref_exam_id` 寫成 `-1`、考試名稱寫成「學期成績」，並把舊矩陣停用條件加上 `item_type LIKE '學期/%'`。失敗訊息也寫「沒有符合排名條件的學期成績」。定期發布必須使用目標 exam ID、目標考試名稱，以及 `定期評量/` 的 item type，失效範圍還要限縮實際計算年級。這不能由呼叫參數轉成定期行為，因為 `-1` 與學期 item type 寫在這個類別裡。

`SemesterRankConfirmRequest` 是 `public sealed`。它有學年度、學期、來源名稱、歷史批次與歷史期間、學生、標籤、年級、排除類別與兩個類別名稱。沒有目標 exam ID、來源考試、來源期間與三組科目的分開欄位。`Students` 的型別是 `K12.Data.StudentRecord`，由學期宿主再轉母群。

`SemesterRankMotherGroupConfirmForm` 是 `public partial`，繼承 `FISCA.Presentation.Controls.BaseForm`。無參數建構子只呼叫 `InitializeComponent`，這段沒有查資料。另一個建構子要求 `CalculateSemesterAssessmentRank` 宿主。計算、上一步、搜尋、恢復與加入學生都轉呼宿主的同名方法。`Request` 是 `internal`。`OnFormClosing` 在 `RankCalculationRunning` 時取消關閉；使用者關閉則轉成上一步。`Load` 把 `MaximumSize` 與 `MinimumSize` 都設成目前大小，學期這個 Form 因此不能縮放。定期驗收要求縮放，新 Form 不能複製這一行。

定期需要自己的 request、條件 Form 與確認 Form。確認 Form 的無參數建構子同樣只初始化控制項。宿主應是定期條件 Form，而不是學期 Form。

## 學期與定期的差異

| 主題 | 學期程式實際做法 | 定期已核准做法 | 共用檔是否要改 |
| --- | --- | --- | --- |
| 目前資格 | `IsCurrentPopulation` 只收一般生，且要有班級與年級 | 原版一般＋延修，仍要有班級與年級 | 不改。定期另寫來源 |
| 目標期間 | 目前來源會被 `RestoreSystemPeriod` 鎖回系統期間 | 三種來源都可選歷年目標，預設系統期間 | 不改 Binder |
| 考試 | 發布寫死 exam ID `-1` 與「學期成績」 | 以目標 exam ID 識別、名稱顯示；來源考試可以不同 | 不改發布類別 |
| 歷史目錄 | `考試名稱` 必須是「學期成績」 | 只列定期批次，含失效批次，排除學期與技職繁星；不能只靠名稱不同 | 不改學期目錄 |
| 快照閘門 | `Load` 拒絕非「學期成績」 | JSON 與 XML 都不存在才降級；損壞時停止 | 不改 `HistorySnapshotRestore` |
| 降級資料 | `Read` 會聯集排名明細 | 只使用 `rank_batch_student`，不以明細補名 | 不改 `Read`。定期只呼叫 `ReadBatchStudents` |
| 排除預設 | `LoadStored` 在 `Read` 成功後才呼叫 private `ApplyExcluded`；`Read` 本身不套標籤 | 定期自行在 `Read` 或 XML `TryRestore` 成功後呼叫 `Apply(HistorySnapshot, ...)`。只在首次載入來源時做；科目重載與歷史加入不覆寫原學生 `Working` | 不改 `Apply`，也不把 `ApplyExcluded` 改成 public |
| Excel 身份 | 13 欄，學號＋狀態 | 沿用同一契約 | 不改 |
| 歷史加入身份 | 只依學號，多筆阻擋 | 同樣只依學號，不套一般＋延修，也不改成學號＋狀態 | 不改 |
| Form 宿主 | 確認 Form 的欄位事件轉呼學期條件 Form | 兩個定期 Form 各自獨立，確認頁不轉呼學期宿主 | 不改學期 Form |

## 共用修改建議

本輪沒有必須修改的共用原檔。定期差異都落在新的來源、目錄、還原協調、request、Form 與 SQL 轉換上；模型、清單、Grid、Excel、JSON、XML 快照元素、標籤解析與 `rank_batch_student` 降級讀取可以照現況呼叫。

若日後仍想把 `TryParseGradeYear` 移出 `SemesterRankSql`，最小範圍是新增一個不含 SQL 的 public 方法，並讓 `SemesterRankSql.TryParseGradeYear` 轉呼叫它，字串規則保持不變。`MotherGroupSnapshotJson` 改呼叫新方法。學期影響限於編譯，以及既有 JSON 快照檢查仍要能讀寫版本 1。這不是目前的必要修改，本輪不核准、也不實作。

不要把「一般或延修」加進 `IsCurrentPopulation`，不要放寬 `HistorySnapshotRestore` 的考試名稱，不要讓 `SemesterHistoryBatchCatalog` 接受定期批次，也不要讓 `SemesterRankPublishSql` 接受外部 exam ID。那些改動會直接改變仍在內測的學期行為。

版本整合維持同一個 `SHEvaluation.Rank.csproj`。後續只新增定期檔案與對應的 `Compile` 項目。共用檔與學期 Form 的 diff 應保持空白。學期 tasks.md 裡尚未勾選的資料庫與人工驗收繼續維持未完成，不能把「共用檔沒改」寫成那些驗收已通過。定期自己的編譯、資料庫與畫面驗收分別屬於 Task 8.1、8.2、8.4 與 8.5。

## 後續任務怎麼接

| 任務 | 依本分析應使用的邊界 |
| --- | --- |
| 2.4 | 唯讀核對四表、extension、`matrix_dept`、索引與既有查詢。本分析沒有查資料庫 |
| 3.1 | 新增定期 context／request，分開目標期間、目標 exam ID 與來源期間、來源考試 |
| 3.2 | 定期 Designer 自己綁期間與來源，不呼叫 `CalculationPeriodBinder` |
| 3.3 | 新增定期確認 Form。無參數建構子不查資料，不鎖定 `MaximumSize`，宿主不是學期 Form |
| 4.1 | 新增定期目前來源，資格為一般＋延修；標籤解析可呼叫現有 public 方法，再套 `Apply(Current)` |
| 4.2 | 直接呼叫 Excel 三個型別，身份維持學號＋狀態 |
| 4.3 | 新增定期歷史目錄，不用 `SemesterHistoryBatchCatalog` |
| 4.4 | 新增定期還原協調。JSON `Read` 失敗即停止。成功後依本次不排名類別與學生標籤呼叫 `Apply(HistorySnapshot, ...)`，保留名稱與 `Original`，不回寫舊批次。XML 成功後做相同的一次 `Apply`。兩種快照都不存在才呼叫 `ReadBatchStudents`，且不再重複套用 |
| 4.5 | 確認清單與 Grid 原樣呼叫。`Load` 發生在首次套用排除預設之後。歷史識別欄位的「目前」字樣由定期 Form 設定 |
| 4.6 | 無成績者留在清單。歷史加入只讓 `HistoryStudentAdd.Read` 為新學生套初始規則，不對原清單再呼叫 `Apply` |
| 5.1～5.4 | 科目重載使用目標期間、目標 exam ID 與來源學生，不重新還原母群，也不再套排除預設。人工加人後的科目重載保留原學生 `Working`。新快照寫入才呼叫 `MotherGroupSnapshotJson.Compose`，Setting XML 另存 `ref_exam_id` |
| 6.1～6.5 | 新增定期參與轉換。禁止呼叫 `ApplyParticipation` |
| 7.1～7.5 | 新增定期發布語句。禁止呼叫 `SemesterRankPublishSql.Build` |
| 8.5 | 學期回歸只在共用 diff 仍為空時，把既有未完成項目維持未勾 |

文件對照：`specs/regular-rank-mother-group-sources/spec.md` 已要求三種來源先留來源名稱與旗標，再套本次不排名類別，歷史快照不回寫舊批次，計算不再次套預設。`design.md` 的 D04、D07 與 `regular-decisions.md` 已要求科目重載及歷史加入保留原學生 `Working`，只有新加入者套初始規則。`tasks.md` 的 4.4、4.6 與 5.4 同樣維持這些邊界。先前遺漏的是本檔把定期 JSON 路徑寫成只呼叫 `Read`。上面的協調步驟補上這段，不改那些已核准文件。

## 尚存技術問題

以下是讀程式得到的邊界，尚未用測試或資料庫證實。

- 定期批次的實際 Setting 屬性、排名類型與失效旗標還沒有對到資料庫。不能只寫「名稱不是學期成績」就當成目錄規則已可實作。
- `MotherGroupSnapshotJson.Read` 不會套本次不排名預設。定期協調者必須在成功後自行呼叫 `Apply`；這項順序已寫入本檔，但還沒有程式，也還沒有測試。
- `MotherGroupSnapshot.TryRestore` 對缺快照與壞 XML 都回 false。定期協調者若沒有先檢查，會把損壞快照降級成 `rank_batch_student`。這是實作時必須避免的點，不是已測過的保護。
- 舊定期批次的 `matrix_dept` 是否為空，本輪沒有查。D05 要求缺值時明示降級。
- 同名考試在定期原版會分別留下最後一個與第一個 exam ID。共用類別沒有處理這個歧義；定期 context 仍須在後續任務解決。
- 班排名視窗沒有年級。這是 Task 6 的 SQL 隔離問題，不是本輪共用類別能先改掉的。
- `HistoryStudentAdd` 在年排名名稱空白時仍可能初始參與。這是保留的學期行為，定期計算必須另以年級解析跳過該生，不能靠改共用方法。
- JSON 不保存狀態、座號與識別班級。歷史畫面若顯示這些目前值，必須在定期 Form 標明，不能回寫進 v1 欄位。
- `CurrentStudentDeptQuery.Create` 與多個 `SelectSql` 以字串拼接 SQL。沿用時要維持目前的數字或已跳脫文字前提，本輪沒有對資料庫執行。
- 學期 Change 的 4.2、4.3、5.1～5.7、7.1～7.3、11.2、12.6、13.3、14.4、14.5 等項目仍未勾選。它們不是定期分析的驗收證據。
