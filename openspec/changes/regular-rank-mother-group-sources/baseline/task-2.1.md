# Task 2.1 原版基準

狀態：已固定。本檔與 `baseline/source` 是唯讀基準，後續 Apply 不得改寫這些副本，也不得用它們取代工作區程式。本輪沒有修改 `CalculateRegularAssessmentRank.cs`、Designer 或 resx，沒有連線資料庫，沒有開啟 VS2022 Designer。

## 固定點

- 工作目錄：`C:\ischoolProject\SHEvaluation-Regular`
- 分支：`feature/regular-rank`
- Commit：`2ca88cbd31aa8dd1507b207973f860005529b30a`
- 副本與工作區檔案的 SHA256 相同，git blob 與該 commit 的檔案物件相同

| 檔案 | 行數 | git blob | SHA256 |
| --- | --- | --- | --- |
| CalculateRegularAssessmentRank.cs | 2680 | 2b46727de9c28624056bb570c11243fbf218ceb2 | C00858331EC24F316B2B97CA31A5B540CE2C6FB68BA0665E19DCAF6B6D3B8E39 |
| CalculateRegularAssessmentRank.Designer.cs | 993 | 4322874c4be385a31b3b49b3dfc4a7d91b0f4064 | D817173C9998B9726D8377AF159A342462CB4FD5612A86E2D6A8DCA87898FE45 |
| CalculateRegularAssessmentRank.resx | 147 | 4404c38bf20e89ace35c27ba6456b80a872fd419 | 868AF8024DF8B637389F77DE8EA8955F3778EF6697A49ED82F73450D94FC1706 |

SQL 沒有獨立檔案。科目查詢、學生列、批次插入與排名語句都內嵌在 `.cs` 副本。以下行號同時對應工作區與副本。

## 目前畫面

基準仍是單一 Form。`plSetting` 是條件頁，`plStudentView` 是學生頁，後者預設 `Visible = false`。兩個獨立 Form 是後續任務，本輪不改 Designer。

## SQL 位置

| 區段 | 行號 | 基準行為 |
| --- | --- | --- |
| 科目查詢 `reloadSubject` | 225–230、231–267 | 用勾選年級的目前班級、`_DefaultSchoolYear`、`_DefaultSemester`。225–230 行以名稱找 exam ID，迴圈沒有 break，同名時持續覆寫，留下最後一個同名 exam ID；科目 SQL 使用這個 ID |
| 學生列 | 485–494 | 由 Grid 組 `student_id`、姓名、年級、科、班、tag1、tag2 |
| 學校組態 Save | 506–575 | 計算前寫入「固定排名_定期評量排名計算」，只含考試名稱、類別與不採計科目 |
| 批次插入 | 710–754 | 獨立 `Select` 建立 `rank_batch`；失敗只寫 Console，`batchID` 維持 0 |
| 逐年級排名 | 760、816–2551、2564 | 每個年級各執行一次排名 `Select`，不能整次回滾 |

`rank_batch.setting` 現有屬性是學年度、學期、考試名稱、不排名學生類別、類別一、類別二，子元素是三組採計科目與年級。基準 XML 沒有 exam ID。已核准的 `ref_exam_id` 屬性屬於後續實作，不在這份副本。

同名考試有兩處不同選定。科目查詢 225–230 行的迴圈沒有 break，會持續覆寫，留下 `_ExamList` 中最後一個同名 exam ID。計算 599 行使用 `First()`，留下第一個同名 exam ID。兩處可以選到不同 ID。成績接合使用 `exam.exam_name = row.rank_exam_name`（880 行）。`row.ref_exam_id` 有傳入，但沒有用來篩選 `sce_take`。這是基準事實；後續改為 exam ID 時不得連帶改分數規則。

## 公式保護區

以下內容在全部參與回歸時必須與副本逐筆一致。允許的後續調整只限條件、母群輸入、參與集合與發布整合。

1. **分數入口** `score_detail_row` 840–883 行。`sce_take.score = -1` 轉成 0；`score IS NOT NULL` 且 `score <> -2`。科目範圍是三組採計科目的聯集。
2. **科目分數** 直接使用上述 score。一般、類別一、類別二分別由 `calc_subject`、`calc_subject_tag1`、`calc_subject_tag2` 篩選。
3. **領域分數** 1028–1062、1108–1144、1177–1213 行。`SUM(credit)` 為 NULL 則分數 NULL，為 0 則分數 0，否則 `SUM(score * credit) / SUM(credit)`。條件是 score 與 credit 都不是 NULL。
4. **類別領域的來源交集保持原樣。** 類別一、類別二領域從一般 `exam_score` 再篩該類科目，不是從 `exam_score_tag1` 或 `exam_score_tag2`。類別總分、平均、加權總分、加權平均則來自對應的 `exam_score_tag1` 或 `exam_score_tag2`。這項差異不修正。
5. **總分** `SUM(score)`。**平均** `AVG(score)`。**加權總分** `SUM(score * credit)`。**加權平均**與領域相同的學分加權。四項都要求 score 與 credit 不是 NULL。
6. **排名窗。** 一般人口同時計算年、班、科：`RANK` 由高到低、反向 `RANK` 由低到高、`COUNT`、`ROW_NUMBER` 由低到高。班排名 partition 只有 `rank_class_name` 與項目，沒有年級。類別人口只有類別排名，partition 含年級與 tag。
7. **百分比與 PR。** 百分比是 `FLOOR((rank - 1) * 100 / count) + 1`。PR 是 `FLOOR((reverse_rank - 1) * 100 / count)`，沒有再加 1。
8. **統計** `score_list` 2044–2410 行。保留 `STDDEV_POP`、pr_88／75／50／25／12 的整除判斷、前 25%／前 50%／全體／後 50%／後 25% 平均，以及 100、90、80、70、60、50、40、30、20、10 的人數分布。
9. **平均的科分母寫法保持原樣。** `calc_avg_rank_row` 1495 行的科分母是 `COUNT(student_id)`，同年級與班級分母是 `COUNT(*)`。不把兩者改成同一寫法。
10. **項目字面值。** 科目是 `定期評量/科目成績`，領域是 `定期評量/領域成績`，總分、平均、加權總分、加權平均都是 `定期評量/總計成績`。排名類型是年排名、班排名、科排名、類別1排名、類別2排名。

`insert_batch_student` 2502–2522 行沒有寫 `matrix_dept`。這是基準缺漏，還原時必須明示，不用目前科別補上。

## 十八個排名分支

六個成績組各有一般、類別一、類別二，共十八個計算排名的 CTE。一般分支在 `score_list` 再投影成年、班、科；類別分支只進自己的類別排名。十八個 CTE 都會進入 `score_list` 的對應 UNION。

| 分支 | 成績 CTE | 排名 CTE | 人口與視窗 |
| --- | --- | --- | --- |
| 1 科目／一般 | exam_score 888 | subject_rank_row 898 | 年、班、科 |
| 2 科目／類別一 | exam_score_tag1 943 | subject_rank_row_tag1 953 | 類別1 |
| 3 科目／類別二 | exam_score_tag2 985 | subject_rank_row_tag2 995 | 類別2 |
| 4 領域／一般 | domain_score 1028 | domain_rank_row 1063 | 年、班、科 |
| 5 領域／類別一 | domain_score_tag1 1108 | domain_rank_row_tag1 1145 | 類別1；來源是 exam_score |
| 6 領域／類別二 | domain_score_tag2 1177 | domain_rank_row_tag2 1214 | 類別2；來源是 exam_score |
| 7 總分／一般 | calc_sum_score 1248 | calc_sum_rank_row 1277 | 年、班、科 |
| 8 總分／類別一 | calc_sum_score_tag1 1320 | calc_sum_rank_row_tag1 1351 | 類別1 |
| 9 總分／類別二 | calc_sum_score_tag2 1381 | calc_sum_rank_row_tag2 1412 | 類別2 |
| 10 平均／一般 | calc_avg_score 1444 | calc_avg_rank_row 1473 | 年、班、科 |
| 11 平均／類別一 | calc_avg_score_tag1 1516 | calc_avg_rank_row_tag1 1547 | 類別1 |
| 12 平均／類別二 | calc_avg_score_tag2 1577 | calc_avg_rank_row_tag2 1608 | 類別2 |
| 13 加權總分／一般 | weight_sum_score 1641 | weight_sum_rank_row 1670 | 年、班、科 |
| 14 加權總分／類別一 | weight_sum_score_tag1 1714 | weight_sum_rank_row_tag1 1745 | 類別1 |
| 15 加權總分／類別二 | weight_sum_score_tag2 1776 | weight_sum_rank_row_tag2 1807 | 類別2 |
| 16 加權平均／一般 | weight_avg_score 1840 | weight_avg_rank_row 1873 | 年、班、科 |
| 17 加權平均／類別一 | weight_avg_score_tag1 1917 | weight_avg_rank_row_tag1 1952 | 類別1 |
| 18 加權平均／類別二 | weight_avg_score_tag2 1983 | weight_avg_rank_row_tag2 2018 | 類別2 |

`score_list` 的五段 UNION 起點是 2105、2177、2249、2321、2396 行，各收六個分支。類別1、類別2 另要求 tag 名稱不是 NULL 也不是空字串（2333、2408 行）。

## 本輪沒有做的驗證

沒有執行排名 SQL，沒有比對資料庫結果，沒有編譯，沒有用 VS2022 開啟 Designer。這些留給後續任務。
