# Task 4.1 定期目前學生來源

狀態：已新增定期來源，不修改 `CurrentStudentMotherGroupBuilder.IsCurrentPopulation`。離線檢查通過。沒有連線資料庫，也沒有在 iSchool 按下「載入／更新母群及科目」。

## 資格與標籤

`RegularRankCurrentStudentSource` 保留狀態為一般或延修、有班級且有年級的學生。延修的 `IsRegular` 維持 false，`Status` 寫 2。休學、無班級或不在勾選年級者排除。

類別文字會去掉兩端方括號，再呼叫共用的 `ResolveTagIds` 與 `FirstTagName`。排除類別符合時，五種名稱保留、五種初始參與為 false。空白類別不參與。目前來源的空白科別依 D07 的 `Current` 規則維持參與。這次載入才呼叫 `MotherGroupInitialParticipation.Apply`；確認頁之後的修改不再呼叫它。

同一學生識別重複時停止，不任取一筆。科別查詢沿用 `CurrentStudentDeptQuery.Create`，但只在學生識別是正整數時才執行。

## 與學期共用來源的差異

同一批輸入下，學期 `CurrentStudentMotherGroupBuilder.Build` 只留下一般生。定期來源同時留下一般生與延修生。沒有改共用方法。
