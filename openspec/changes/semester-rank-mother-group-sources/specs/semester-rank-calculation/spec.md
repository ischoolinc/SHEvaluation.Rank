# Spec Delta

## Purpose

定義五種參與狀態如何在排名統計前生效、舊排名結果如何保持相容，以及批次成功或失敗時有效排名如何維持一致。

## ADDED Requirements

### Requirement: 排除在排名與統計之前生效
年、班、科、類別一、類別二 MUST 各自使用確認清單中該項參與的學生。不參與的學生 MUST 在名次、母群人數、標準差、百分位、五標、前後段平均與級距計算前排除。系統 MUST NOT 先算出含該生的結果再刪除該生的排名明細。四種成績都 MUST 使用同一組參與狀態：科目原始、科目擇優、分項原始、分項成績。

#### Scenario: 只取消年排名
- **WHEN** 某生有成績，年排名不參與，其餘四種參與
- **THEN** 年排名的名次與統計人數不含該生，其餘四種排名仍包含該生

#### Scenario: 不參與但有成績
- **WHEN** 某生該科目有分數，且該項排名設為不參與
- **THEN** 該科目的該項排名沒有該生的名次，統計人數也不含該生

### Requirement: 沒有成績不產生該項名次
參與某項排名的學生若沒有該成績項目的分數，系統 MUST NOT 產生該項目的排名明細，也 MUST NOT 把該生計入該項目的統計人數。確認清單與母群快照 MUST 仍保留該生。

#### Scenario: 參與年排名但沒有分項成績
- **WHEN** 學生年排名為參與，且本次期間沒有某分項成績
- **THEN** 該分項沒有該生的年排名明細，該生仍留在本批母群快照

### Requirement: 預設全參與時排名結果與舊版一致
來源為目前學生、計算期間與擇優設定相同，且初始參與未經使用者改寫時，新計算的有效排名 MUST 與舊版排名公式一致。排名類型名稱 MUST 仍為年排名、科排名、班排名、類別1排名、類別2排名。成績項目名稱與原有統計欄位 MUST 保持相容。科目 XML 重複列造成的重複計數 MUST 維持舊行為。

#### Scenario: 同類別空白不產生類別排名
- **WHEN** 類別二母群名稱為空白
- **THEN** 不產生空白名稱的類別2排名，其他排名仍依既有公式計算

#### Scenario: 舊版會排名的空白科別
- **WHEN** 目前學生來源的科排名名稱為空白且該項為參與
- **THEN** 科排名仍包含該生，排名名稱維持空白

### Requirement: 快照寫入既有 extension 且保留 setting
成功計算的批次 MUST 在同一個 `rank_batch` INSERT 寫入 setting 與 extension。setting MUST 不含 `MotherGroupSnapshot`，且 MUST 仍可讀取學年度、學期、考試名稱、不排名學生類別、類別一、類別二、年級與擇優採計成績。系統 MUST NOT 先插入批次再更新 extension，也 MUST NOT 為快照新增資料表或排名欄位。

#### Scenario: 舊節點與快照同時存在
- **WHEN** 新批次寫入完成
- **THEN** setting 仍有原有屬性、年級與擇優採計成績且沒有 `MotherGroupSnapshot`，extension 可還原確認清單的每位學生

### Requirement: 快照保存確認清單的全部學生
快照 MUST 來自該次確認清單工作副本，包含全部學生、學生識別、學號、姓名、五種母群名稱與五種 JSON boolean 參與狀態。年排名名稱可解析時 MUST 另存正整數 `grade_year`，否則 MUST 為 JSON null。`source_kind` MUST 是 `Current`、`History` 或 `Excel`。`source_batch_id` MUST 只在歷史來源為正整數，其他來源 MUST 為 JSON null。

#### Scenario: 不參與的學生仍在快照
- **WHEN** 確認清單有學生五種排名都不參與
- **THEN** 該生仍在 extension 快照，五種參與狀態都是 JSON false

### Requirement: extension 陣列保留其他項目
寫入母群快照時，陣列中已有的其他 `extension_name` MUST 保留。同一個陣列 MUST NOT 留下兩筆 `MotherGroupSnapshot`。

#### Scenario: 其他 extension 保留
- **WHEN** 寫入前的 extension 陣列已有其他 `extension_name`
- **THEN** 該物件仍在，且陣列只有一筆 `MotherGroupSnapshot`

### Requirement: 停用與啟用在同一個交易完成
系統 MUST 以單一次資料庫交易同時停用本次要取代的舊有效排名並啟用新排名。交易提交前，舊有效排名 MUST 維持有效。交易失敗或中斷時，舊有效排名 MUST 與交易前相同，新排名 MUST NOT 成為有效排名。系統 MUST NOT 以多次各自提交的方式只停用或部分啟用某些年級。批次識別無效時 MUST NOT 以 0 寫入排名。失敗時 MUST 顯示失敗，且 MUST NOT 顯示計算完成。

#### Scenario: 公布交易失敗
- **WHEN** 同時停用舊排名與啟用新排名的交易失敗
- **THEN** 原本有效排名仍全部可檢索，新排名不是有效排名，畫面顯示失敗

#### Scenario: 不能只公布部分年級
- **WHEN** 本次計算包含兩個年級，且公布交易沒有成功提交
- **THEN** 兩個年級的舊有效排名都保持原狀，兩個年級的新排名都不是有效排名

#### Scenario: 批次建立失敗
- **WHEN** 排名批次沒有取得有效識別
- **THEN** 系統中止後續排名寫入，且不寫入批次識別 0

### Requirement: 缺少年級母群的學生不臆測年級
年排名母群名稱無法轉成年級的學生 MUST 留在確認清單，並在計算前提示。系統 MUST NOT 改用目前班級年級補入，也 MUST NOT 為該生產生五種排名明細。

#### Scenario: 歷史還原沒有年排名名稱
- **WHEN** 學生年排名母群名稱為空白且使用者仍開始計算
- **THEN** 系統提示該生未納入本次排名計算，且不產生該生的新排名明細

### Requirement: 寫入前確認可取消
寫入閘門允許排名寫入，且本次有可計算年級時，系統 MUST 在儲存擇優組態與執行排名 SQL 之前顯示確認。確認內容 MUST 包含圖1學年度、學期、母群來源，以及本次工作副本會停用的年級。使用者取消時，系統 MUST NOT 儲存擇優組態，MUST NOT 執行排名 SQL，也 MUST NOT 留下這次停用的控制項。

#### Scenario: 使用者取消確認
- **WHEN** 使用者在確認框選擇取消
- **THEN** 擇優組態維持原內容，四張排名表沒有這次寫入，計算前可使用的參與編輯仍可使用

#### Scenario: 確認內容含期間與來源
- **WHEN** 使用者開啟確認框
- **THEN** 確認框顯示圖1學年度、學期、母群來源，以及這次工作副本與圖1勾選年級交集中的舊有效學期成績排名會被停用

### Requirement: 執行中防止重複計算
排名寫入進行期間，系統 MUST 忽略再次計算的請求，且 MUST NOT 同時執行兩筆排名寫入。

#### Scenario: 計算尚未結束時再次按下計算
- **WHEN** 排名寫入已開始且尚未結束
- **THEN** 第二次按下不啟動另一筆排名寫入

### Requirement: 驗證完成前不寫入排名
PostgreSQL 9.6 與 17.1 的必要測試證據全部通過前，系統 MUST NOT 寫入排名資料。此期間按下計算 MUST 表示尚未寫入，且 MUST NOT 顯示計算完成。

#### Scenario: 測試證據尚未完成
- **WHEN** 9.6 或 17.1 的必要測試證據尚未全部通過
- **THEN** 系統不寫入四張排名表，也不顯示計算完成

### Requirement: 首次寫入測試使用獨立資料庫
必要測試 MUST 使用獨立測試資料庫，MUST NOT 使用正式學校資料庫做首次寫入。9.6 與 17.1 都 MUST 以三種母群來源驗證五種參與，以及四張排名表的關聯。

#### Scenario: 三種來源都在兩個版本驗證
- **WHEN** 目前學生、歷史母群與 Excel 母群分別在 9.6 和 17.1 的獨立測試庫驗證
- **THEN** 每一組都檢查五種參與，以及批次、學生、母群與明細的關聯

### Requirement: 成功寫入後回復測試資料
測試成功時，新批次的四張排名表 MUST 彼此關聯，新排名 MUST 有效，且本次學年度、學期與年級的舊有效學期成績排名 MUST 已停用。檢查完成後 MUST 回復這次測試寫入，不把測試排名留在庫中。

#### Scenario: 成功公布後收回測試寫入
- **WHEN** 獨立測試庫的公布語句成功，且四張表與舊排名停用都已核對
- **THEN** 測試寫入被回復，測試庫不保留這次新排名

### Requirement: 故意失敗先解除已中止交易
故意失敗的語句錯誤後，測試 MUST 先以儲存點回復或另一筆獨立交易解除已中止的交易，再檢查結果。兩個年級的舊有效排名 MUST 與執行前相同，且 MUST NOT 留下新的有效排名。

#### Scenario: 語句失敗後交易已中止
- **WHEN** 含兩個年級的公布語句在測試交易中失敗
- **THEN** 測試先解除已中止狀態，再確認兩個年級的舊有效排名都在，且沒有新的有效排名

### Requirement: 語句超過閘道則停止公布
公布語句長度 MUST 與實際閘道限制比對。超過時系統 MUST 停止公布並回報，且 MUST NOT 改成逐年級各自提交。

#### Scenario: 語句長度過長
- **WHEN** 組好的公布語句超過實際閘道可接受的長度
- **THEN** 系統不公布，舊有效排名保持原狀

### Requirement: 證據通過後才開放寫入
只有必要測試證據全部通過後，系統 MUST 才開放實際排名寫入，並同時更新原本要求寫入保持關閉的靜態檢查。開放後若計算失敗，畫面 MUST 顯示失敗，且 MUST NOT 顯示成功。排名公式與三種母群來源規則 MUST 維持不變。

#### Scenario: 證據未齊不開放
- **WHEN** 任一必要測試尚未通過
- **THEN** 實際排名寫入維持關閉

#### Scenario: 開放後語句失敗
- **WHEN** 寫入已開放且公布語句失敗
- **THEN** 畫面顯示失敗，不顯示計算完成，舊有效排名與執行前相同

### Requirement: 沒有可計算年級時不開始計算
寫入閘門允許寫入後，系統 MUST 先檢查圖1勾選年級與確認清單可解析年排名的交集。交集為空時 MUST 顯示「沒有可計算的年級」並返回。系統 MUST NOT 顯示計算確認框，MUST NOT 儲存擇優組態，也 MUST NOT 產生或執行排名 SQL。此檢查 MUST 在寫入閘門返回之後。寫入閘門仍關閉時，系統 MUST 仍先顯示既有阻擋訊息並返回。

#### Scenario: 勾選年級與清單沒有交集
- **WHEN** 圖1只勾選一年級，確認清單沒有可解析的一年級，且寫入閘門已允許寫入
- **THEN** 系統顯示「沒有可計算的年級」，不顯示確認框，不儲存擇優組態，不執行排名 SQL

#### Scenario: 寫入閘門仍先返回
- **WHEN** 寫入閘門不允許寫入
- **THEN** 系統顯示既有阻擋訊息並返回，不因沒有可計算年級而儲存擇優組態

### Requirement: 計算開始前固定工作副本
系統 MUST 在畫面執行緒建立一次獨立的計算工作副本。副本 MUST 包含學生 ID、學號、姓名、五種母群名稱、五種參與狀態與母群衝突資訊，且 MUST NOT 與畫面清單共用可變的學生或排名欄位物件。學生狀態若已存在，副本 MUST 一併複製，但排名 SQL MUST NOT 讀取它。擇優採計 MUST 另行複製。學年度、學期、不排名類別、類別一、類別二、來源種類與歷史批次識別 MUST 固定為本次值。建立副本前，表格尚未提交的編輯 MUST 已寫入確認清單。

#### Scenario: 尚未提交的參與勾選被納入
- **WHEN** 使用者改了參與核取但儲存格尚未提交就按下計算
- **THEN** 這次工作副本使用提交後的參與狀態

#### Scenario: 副本與畫面不共用物件
- **WHEN** 工作副本建立完成後，畫面清單的參與狀態再被修改
- **THEN** 工作副本的五種名稱與參與狀態維持建立時的值

### Requirement: 排名語句與快照使用同一份副本
確認框的年級、排名 SQL、母群快照、擇優設定與 SQL 擇優片段 MUST 使用同一次建立的工作副本。背景計算 MUST NOT 直接讀取畫面確認清單或畫面擇優清單。新快照 MUST 寫入 extension 的 JSON 版本 1，舊 XML 版本 1 MUST 仍可讀。排名公式、三種母群來源規則與四張排名表結構 MUST 維持不變。

#### Scenario: 計算中改畫面不影響這次結果
- **WHEN** 計算已經開始，畫面確認清單的參與狀態隨後改變
- **THEN** 這次排名 SQL 與母群快照仍使用開始時的工作副本

### Requirement: 計算期間不能改這次的參與設定
確認計算後，系統 MUST 停用五種參與核取、右鍵批次參與修改、還原參與，以及歷史來源的加入學生。相關事件在計算執行中 MUST 拒絕修改，不得只依賴控制項是否啟用。計算完成或失敗後，MUST 恢復這些控制項原本的可用與可見狀態，MUST NOT 把原本停用或隱藏的控制項無條件啟用。確認框取消或沒有可計算年級而返回時，MUST NOT 留下這次停用的狀態。

#### Scenario: 執行中忽略參與修改
- **WHEN** 排名計算執行中，使用者再次變更參與核取、右鍵批次設定、還原參與或加入學生
- **THEN** 這次計算的工作副本不變，畫面確認清單也不被這次操作修改

#### Scenario: 原本隱藏的加入學生維持隱藏
- **WHEN** 目前學生來源的計算結束
- **THEN** 加入學生維持隱藏，不因計算結束而被啟用

#### Scenario: 取消或沒有年級後不殘留停用
- **WHEN** 使用者在確認框選擇取消，或因沒有可計算年級而返回
- **THEN** 計算前可編輯的參與控制項恢復可編輯，原本隱藏的控制項仍隱藏

### Requirement: 學生清單以 JSONB 傳入
正式公布的 `student_list` MUST 由確認清單工作副本的一個 JSON 陣列，經 `jsonb_to_recordset` 展開。系統 MUST NOT 再為每位學生產生 `SELECT` 後以 `UNION ALL` 接成學生清單。科目、分項、擇優、`RANK`、`COUNT` 與統計公式 MUST 維持不變。成績 CTE 裡原有的 `UNION ALL` MUST 保留。

#### Scenario: 大量學生仍是一份學生清單
- **WHEN** 確認清單有 1000 名可計算學生
- **THEN** `student_list` 使用一個 `jsonb_to_recordset`，成績 CTE 仍保留原有的 `UNION ALL`

### Requirement: JSON 欄位保留舊學生列語意
JSON MUST 含 `student_id` 數字、姓名、排名年級、科班與兩個類別名稱，以及五種 `participate_*` boolean。空白名稱 MUST 是空字串，MUST NOT 寫成 JSON null。`student_list` MUST 投影這 12 欄，讓既有 `ApplyParticipation` 繼續讀到參與旗標。

#### Scenario: 空白類別且不參與
- **WHEN** 類別二名稱為空白且該項不參與
- **THEN** JSON 的 `rank_tag2` 是空字串，`participate_tag2` 是 false

### Requirement: 重複學生與未選年級不得進入計算
同一學生識別出現兩次時 MUST 停止並提示，MUST NOT 取第一筆。非正整數或含前導零的識別 MUST 停止。未勾選年級與無法解析年級的學生 MUST NOT 進入 `student_list`。`calc_condition` MUST 仍是勾選後每個年級一列，年級之間維持 `UNION ALL`。

#### Scenario: 只勾選一年級
- **WHEN** 確認清單同時有一年級與二年級，圖1只勾選一年級
- **THEN** JSON 與 `calc_condition` 都只有一年級

### Requirement: SQL 學生 JSON 不寫進 extension
SQL 用的學生 JSON 與 `MotherGroupSnapshot` MUST 來自同一份工作副本。系統 MUST NOT 把 SQL 用的 JSON 再寫進 `rank_batch.extension`。快照 MUST 仍含確認清單的全部學生。

#### Scenario: 無法解析年級的學生只留在快照
- **WHEN** 某生的年排名名稱無法解析成年級
- **THEN** 該生不進入 `student_list`，但仍留在母群快照

### Requirement: 排名公式與四表寫入不因學生清單改寫
`subject_score`、擇優、分項與 `score_list` 的讀取、JOIN、`GREATEST` 與分數 `UNION ALL` MUST NOT 改變。四張排名表的寫入欄位、單一語句與失敗回復 MUST 維持。三種母群來源 MUST 共用這份 JSON 學生清單。

#### Scenario: 歷史來源指定另一個計算期間
- **WHEN** 歷史批次是 113-2，本次計算指定 114-2
- **THEN** 成績條件使用 114-2，學生母群仍來自確認清單，而不是目前學籍年級
