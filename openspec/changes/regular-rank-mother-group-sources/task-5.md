# Task 5 期間、考試、科目與快照

狀態：5.1 與 5.3 的離線範圍完成。5.2 只完成成績接合的程式調整，資料庫情境未驗證。5.4 的人工加人未做。沒有連線資料庫，沒有保存學校組態。

## 5.1 科目查詢

「載入／更新母群及科目」先建立目前母群，再用 `RegularRankSubjectCatalog` 查科目。SQL 使用目標學年度、目標學期、目標 exam ID，以及可解析年級的母群學生 ID。不再使用系統期間，也不再以考試名稱迴圈留下最後一個同名 ID，也不再依目前班級年級過濾。

排序仍是學分、節數、科目。三組候選使用同一份科目。既有不採計科目維持不勾選，新科目與移除科目由既有 context 記錄。必須再按「確認三組科目」才可下一步。查詢失敗或沒有科目時停留條件頁，不保留上一份可用名單。

舊 `reloadSubject` 不再執行系統期間 SQL。它只提示改走載入按鈕。

離線檢查確認 SQL 含 `school_year = 113`、`semester = 2`、`ref_exam_id = 30` 與學生 `IN (10, 20)`，並拒絕前導零與空名單。沒有對 PostgreSQL 執行。

## 5.2 成績識別

排名 SQL 的成績接合由 `exam.exam_name = row.rank_exam_name` 改成 `exam.id = row.ref_exam_id::INT`。`CASE WHEN sce_take.score = -1`、`RANK()`、`FLOOR`、`STDDEV_POP` 與 `FILTER` 行和基準逐行相同。考試名稱仍留在顯示欄與 Setting。

這段 SQL 在計算入口停用之後，目前不會執行。同名考試是否只命中指定 ID、以及沒有目前班級的學生是否仍能取得目標期間修課，都還沒有資料庫證據。5.2 不勾選。

## 5.3 快照

`RegularRankSnapshotDocument` 以共用 `MotherGroupSnapshotJson.Compose` 寫版本 1，來源種類是 Current，不新增狀態、座號或歷史班級欄位。既有其他 extension 保留。Setting XML 仍是 `Setting`，並加上 `ref_exam_id`，不用 JSON 取代 XML。

離線往返涵蓋特殊字元、五種之一為 false，以及年排名名稱無法解析成年級的學生。計算按鈕不會呼叫保存，所以這次沒有把 XML 或 JSON 寫進學校組態或資料庫。

## 5.4 已覆蓋與未做

變更目標考試會使載入狀態過期並停用下一步。科目載入失敗後，`CanCreateRequest` 拒絕用上一份科目搭配新條件。三組勾選保留交集，新科目依不採計清單初始化。

人工加人與加人後保留既有 Working 旗標本輪沒有做。5.4 不勾選。
