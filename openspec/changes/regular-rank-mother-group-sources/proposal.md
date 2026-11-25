# Proposal

## Why

高中定期評量固定排名目前使用系統學年期與目前學生名單，無法完整支援歷史期間、Excel 或歷史排名母群，以及五種排名獨立參與。需要沿用既有 MotherGroup 基礎，同時保護定期評量原始成績公式與仍在內部測試的學期排名。

## What Changes

- 新增可指定的目標學年度、學期，預設系統目前期間；課程、評量、科目、計算與儲存共用同一份條件。
- 三種來源統一為 StudentMotherGroup：目前學生、Excel、歷史固定排名批次。D01～D10 已核准，見 design.md。
- 新增支援 VS2022 Designer 的定期評量專用母群確認 Form，提供完整清單、五種獨立參與、批次操作、統計、搜尋、恢復與 Excel 匯出。
- 參與集合在對應排名及統計視窗之前生效；保留原有特殊分數、三組採計科目、科目／領域／總計、PR、百分比及所有統計公式。
- 母群快照沿用版本 1 MotherGroupSnapshot JSON，寫入既有 rank_batch.extension；setting 保留定期評量 XML、考試與三組採計科目。
- 整次排名的批次、全部選定年級、舊排名失效與新結果發布須具原子性，失敗不留下部分有效排名。
- 建立原版基準、全參與逐筆比對、獨立參與與失敗回滾驗收。

決策已於 2026-10-30 核准並開始 Apply。學期對照與定期差異見 semester-reference.md。通過 OpenSpec 結構驗證不代表功能完成，也不修改學期 Change。

## Capabilities

### New Capabilities

- `regular-rank-calculation-scope`: 目標期間、考試唯一識別與三組採計科目資料流。
- `regular-rank-mother-group-sources`: 三種來源、歷史還原及 Excel 交換契約。
- `regular-rank-mother-group-confirmation`: 第二畫面、五種獨立參與及 Designer 相容。
- `regular-rank-calculation-publish`: 公式保護、Participation Gate、完整快照、原子發布與驗收。

### Modified Capabilities

- 無；不修改學期排名 Change 或其規格。

## Impact

- 預計調整 CalculateRegularAssessmentRank.cs、Designer、resx 與 SHEvaluation.Rank.csproj，新增定期評量專用協調類別及確認畫面。
- 共用 StudentMotherGroup、RankSlot、MotherGroupConfirmList/Grid 與 JSON／Excel 基礎，優先不修改共用原檔；任何必要修改先提出學期影響及版本整合建議。
- 沿用 rank_batch、rank_batch_student、rank_matrix、rank_detail，不新增資料表或欄位。實際欄位、型別、限制及索引在獨立測試庫核實。
- 維持 .NET Framework 4.8、既有 FISCA／DotNetBar／Aspose 相依與 PostgreSQL 9.6～17 相容目標。
- 不包含學期 SQL 重構、公式修正、正式資料庫操作、Commit、Push、合併與 Archive。
