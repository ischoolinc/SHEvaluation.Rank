# Cursor 實作交接

2026-11-04 最新授權：依 cursor-adjustment-prompt.md 連續實作目前學生限定測試版，只在必要決策／環境阻礙時詢問。下文舊有「每次一個子任務、完成就等待」不適用本輪。3.2 已完成，從 3.3 接續。Excel／歷史／人工加人延後但不刪規格；每段更新 implementation-handoff.md，供額度不足後接手。

2026-11-03 補充：另讀根目錄 Todo.md 及本 change 的 new-debug6-risk-review.md。下一任務為 3.2，依序 3.3、3.4；新增 4.7（同步後共用政策隔離）及 8.7（大量清單輔助存取與跨程序驗收）。指令範本見 cursor-adjustment-prompt.md。學期修正已同步，不代表定期舊 Grid 自動受保護，也不代表歷史政策可直接沿用。

## 工作範圍

工作目錄：C:\ischoolProject\SHEvaluation-Regular

預期分支：feature/regular-rank。每次開始先檢查 git status、branch、worktree；不切換分支、不碰其他工作目錄。原版基準：2ca88cbd31aa8dd1507b207973f860005529b30a。

Change：regular-rank-mother-group-sources。先完整閱讀 proposal.md、design.md、四份 specs、tasks.md、semester-reference.md、regular-decisions.md。本交接不取代規格。regular-decisions.md 的建議方案已於 2026-10-30 核准。

## 已明確的 UI 架構

採與學期相同的兩個獨立 Windows Form。CalculateRegularAssessmentRank 是條件頁，RegularRankMotherGroupConfirmForm 是新增確認頁；各有 cs／Designer.cs／resx。禁止仍用原定期兩 Panel 當成兩 Form，禁止把查詢放 Designer。固定控制項由 Designer 管理，預設建構子不查 DB。

## 開始方式

決策已核准並開始 Apply。D01、D03、D04、D09、D10 採 regular-decisions.md；D02、D05、D06、D07、D08 採 semester-reference.md。每輪仍只做使用者指定的 tasks.md 子任務。

若只授權不依賴待決政策的基礎任務，僅做該指定任務，例如 2.1 原始基準與保護區整理；不得順帶決定上述政策。使用者明確選定任務後不重複詢問已授權範圍。

## 每次 Apply 的順序

2026-11-01 開發順序修訂：目前可直接開始 Task 3.1，接續 3.2、3.3。Task 2.4 沒有可用測試庫時保持未完成，不阻擋模型、兩個獨立 Form、離線來源轉接及 SQL 組成的開發；不要每輪停下討論連線，也不要反覆修訂已審查通過的唯讀腳本。每輪仍只做指定子任務。資料庫相依的整合、發布與驗收須保留必要證據關卡，不得因離線編譯或學期經驗而勾選通過。學期既有經驗可供設計與建立測試夾具參考，不能當作本次實測結果。

1. 執行 openspec instructions apply --change regular-rank-mother-group-sources --json，閱讀其要求與相關文件。
2. 每次只處理一個 tasks.md 子任務；依相依順序，先基準與共用影響，再條件／兩 Form、來源、快照與科目、Participation、原子發布、驗收。
3. 不因 tasks 由小到大就跳過決策核准；也不把規格通過驗證當作功能完成。
4. 完成任務後作適當驗證並記錄證據，只有實際完成才勾選。資料庫／Designer 未實測要明示，不以編譯替代。
5. 回報本次變更檔案、完成任務、驗證與下一任務；等待使用者下一階段指示。不自行 Commit／Push／Archive／Merge／Rebase／Cherry-pick。

## 不可跨越的邊界

- 不修改學期 CalculateSemesterAssessmentRank、SemesterRankMotherGroupConfirmForm 或既有學期 OpenSpec Change。
- 不修改定期原始成績來源、特殊分數、三組採計規則、科目／領域／總計公式、PR、百分比、五標與統計公式。
- 五種參與在對應視窗前生效，不能先排名再刪人；不要直接套學期 SQL 字串轉換器。
- 共用 MotherGroup 基礎先分析適用性；需要修改共用原檔先提出學期影響與整合建議，不能靜默重構。
- 不新增表或欄位、不碰正式學校 DB。測試庫寫入依指定驗收任務及連線範圍執行；不得把正式連線寫進設定。
- 原子發布必須涵蓋全部本次年級，不能因閘道長度等原因降級成逐年級提交。

## 重要差異

目前來源保留一般＋延修，與學期僅一般不同。Excel 參照 13 欄、學號＋狀態；歷史加人依學號，兩者不能混為同一身份契約。歷史還原後套本次排除預設、可人工覆寫。JSON 與 XML 母群快照均不存在時才從 rank_batch_student 降級；快照存在但損壞、重複、版本不支援或內容無法還原時停止，不得當成不存在。不查排名明細補名。

來源期間／考試不等於目標條件，科目重載需依核准方案。班排名原窗口沒有年級，合併發布不得造成跨年級同名班混算。類別領域使用一般 exam_score 的既有交集不能順帶修正。

## 交付檢查

執行 openspec validate regular-rank-mother-group-sources --strict --no-interactive。程式任務另跑適當編譯／比較。每階段列出實際 diff 與未完成驗收；學期既有未完成測試不補勾。
