# Task 6 定期 Participation Gate

狀態：6.1 原型與覆蓋檢查完成。6.2 到 6.5 沒有把參與旗標插進十八個排名視窗，也沒有做全部參與的資料庫逐筆比對。計算入口維持停用。

## 6.1

`RegularRankParticipationGate.Cover` 要求十八個成績來源與十八個排名分支、`score_list`、原始 `-1` 轉 0，以及目標 exam ID 接合都存在。缺任一個標記就停止，不回傳部分結果。它拒絕 SQL 內出現 `ApplyParticipation`，定期程式沒有呼叫 `SemesterRankSql.ApplyParticipation`。

覆蓋結果同時記錄班排名 `PARTITION BY rank_class_name` 仍不含年級。後續合併發布不能把不同年級放進同一個班排名視窗。

`PopulationValues` 為完整清單建立五個旗標。年不參與的學生仍留在集合內，值是 false，不會先從名單刪除。學生識別不是正整數或重複時停止。

工作區排名 SQL 與 `baseline/source/CalculateRegularAssessmentRank.cs` 的 `RANK`、`FLOOR`、特殊分數、`STDDEV_POP` 與 `FILTER` 行數相同且逐行一致。

## 尚未接入

原型沒有改寫十八個視窗的 FROM 或 WHERE。因此目前不能宣稱年不參與、班仍參與會在排名前分開生效。6.2 到 6.5 維持未完成。在接入並完成全部參與回歸之前，計算不能開放。
