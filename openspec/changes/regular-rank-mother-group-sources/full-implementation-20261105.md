# 完整測試版實作進行紀錄

使用者要求完整功能，取消限定目前來源。已授權只在 new_debug6 新增排名、停用舊排名、回滾及兩連線並行驗證。此紀錄不是交付完成證明。

## 最新實測（優先於下方初次換載紀錄）

- 實際換載版本 1.0.0.64，SHA-256 `FA6A265596D54DA8937BA7ED641D42C8793CAC7D831E80C74060B3F94EE0C841`。這是目前測試程式的版本，不是後續重建檔案的雜湊。
- new_debug6 實際資料庫 `1admin_debug6`，PostgreSQL 170011；四表欄位、extension jsonb、matrix_dept 與矩陣唯一鍵已有 pg_catalog 證據。既有結果檢索相容驗收仍未完成，Task 2.4 不整項補勾。
- 目前來源曾載入 2071 人／35 科；本次三年級載入 687 人／14 科，確認頁年／科／班排名初始參與各 664、排除各 23。三組科目確認、下一步、唯讀核對及匯出取消返回均有實際畫面證據。
- 2026-11-05 23:24（UTC+8），114-2／期末考 exam ID 5／三年級，在五種全參與假設下，新舊 score_list 全欄含重複列 EXCEPT ALL：原版 30,764、新版 30,764、缺少 0、多出 0。未改工作旗標、未保存組態、未寫排名。此證據不涵蓋實際排除旗標組合、矩陣 INSERT、其他年級／期間或版本。
- 交易腳本與單一 DO 探測均未取得成功證據，前端收到 DSA 504。無底層 SQLSTATE，不能認定 PostgreSQL 不支援或確定是服務包裝問題。發布停用，未執行實際排名。
- 使用者表示目前無法取得伺服器紀錄。必要後端查核與無學生資料的重現腳本見 `backend-transport-check-20261105.md`；不再為相同探測反覆換載要求登入。
- 最新工作區另修正：分別保存兩條探測例外，避免 AggregateException 只顯示第一分支；確認頁改在 OnShown 限制螢幕邊界，避免 CenterParent 在 Load 後再次移出畫面。已編譯，尚未換載或在 iSchool 實測，不把這兩項記成已驗收。
- 此後工作區建置 SHA-256 `CF6E72DDC085FF8BBCF2D3BA46B2E914485116431BCC05D83A0F8D322BDC4C36`；OperationCheck（2066 人 Bounds 7ms、明細 2068 列 0ms，含搜尋／完整匯出隔離）及 DesignerStructureCheck、OpenSpec strict、diff check 皆通過。兩項現存編譯警告未變。測試程式仍為上方 FA6… 版本，未要求重登入。

離線 Excel／歷史／人工加入／五旗標候選與發布程式已實作，不再是舊版未提供入口；整體實際驗收尚未完成，不能交付為與學期同等完整可測。

## 本輪新增

- Excel：條件頁選檔時保存位元組與 SHA-256；沿用 13 欄、學號＋狀態、整份驗證。只對可解析且勾選年級的學生查目標科目；完整名單仍保留。
- 歷史：只讀定期批次，含失效批次，依 ID 降冪。由唯一矩陣考試 ID 驗證來源身分；設定與矩陣不符、複數 ID 阻擋。JSON 優先，XML 次之，只有兩者皆不存在才讀 rank_batch_student。壞快照不降級；載入前重新核對設定與 extension 未變。
- 人工加入：歷史來源依學號唯一查詢，不套目前一般＋延修限制。複製工作清單，重載科目成功後才採用，保留既有 Working 旗標，返回條件頁重新確認；失敗不採用半份名單。
- 匯出：背景處理完整工作副本；歷史缺狀態者批次讀目前狀態，不改母群；確認頁提供返回條件頁匯入 Excel。
- 公式：從原定期 SQL 抽出 RegularRankFormulaSql。獨立候選組句按實際年級與五種參與種類分開輸入，再使用原排名／統計公式並只取對應種類。避免先算後刪及跨年級同名班混算。公式逐行保護檢查通過。
- 發布原型：明確要求同一連線的 SERIALIZABLE 交易腳本；預先唯讀探測隔離層級、檢查四表結構／限制／索引，只允許名稱含 debug6 的資料庫。整次發布含完整快照、全部實際年級、舊矩陣失效、完整可解析年級學生、矩陣與明細寫入；每年級須有結果，筆數檢查失敗回滾。操作識別與專用不重試傳輸沿用已同步基礎。未證明閘道能力時 UI 維持停用。
- 介面：新計算入口與背景執行、防重入／關閉保護、完整目標／來源／年級／設定先保存提示已接線。舊逐年級函式改名 LegacyCalculateFromConfirm，沒有入口呼叫；保留原公式基準供比對。

## 離線驗證

Debug 編譯成功。RegularRankExcelSourceCheck、RegularRankHistorySourceCheck、RegularRankCandidateSqlCheck、RegularRankCalculationContextCheck、RegularRankPeriodSourceEditorCheck 通過。既有 RegularRankOperationCheck 通過：2066 人列／核取儲存格 Bounds 38ms，2068 列明細 0ms。OpenSpec strict validate 通過。

候選 SQL 的檢查包含年級／種類人口隔離、無法解析年級不進排名、保留原公式、只讀、全不參與停止，以及交易腳本／失效範圍／矩陣年級接合／明細後失敗注入。這些不等於 PostgreSQL 執行或數值正確性驗收。

## 換載與待實測

已編譯 1.0.0.64 開發驗證版，SHA-256 F36BA5127D1AF11AB3D305DD66A71C816A38AE78832358F95A7C00AF3D53EF41，換載至 D:\ischool_test\1admin_debug6\modules\SHEvaluation\SHEvaluation.Rank.dll。原檔備份於 bin/sync-backups/20261105-before-regular64/SHEvaluation.Rank.dll。使用者已回覆登入新版；尚待啟動完成及實際載入檢查。此雜湊只代表此時換載版本，後續重建需重算。

尚未完成：實際來源／確認／匯出往返、Task 2.4 證據與既有讀取端檢查、閘道交易探測、SQL 執行、原版全參與及五旗標數值比對、零明細、故障注入回滾、兩連線並行、回應中斷核對、定期檢索／明細、Designer／縮放及學期回歸。不能把此版交付標為完整驗收通過。
