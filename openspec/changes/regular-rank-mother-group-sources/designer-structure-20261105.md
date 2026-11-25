# 兩 Form Designer 結構補正 2026-11-05

## 變更摘要

條件 Form 只保留第一畫面；確認 Form 承接全部第二畫面。舊 `plStudentView` 及其專屬控制項、事件與過渡引用已刪除。計算與儲存維持停用。

## 編譯

```
MSBuild SHEvaluation.Rank.csproj /t:Build /p:Configuration=Debug
```

結果：成功。警告仍是既有 `useWeightedAvg` 與學期診斷過時方法。

DLL：`C:\ischoolProject\SHEvaluation-Regular\bin\Debug\SHEvaluation.Rank.dll`  
版本：1.0.0.63  
SHA-256：`D57A8AA8FFB5295348C70790F2867F78C66CAB7B89498F04ECF7C80A3101A1D3`

## 離線結構與生命週期檢查

```
RegularRankDesignerStructureCheck.exe
```

結果：`regular-rank-designer-structure-check: ok`

已核對：

- 條件 Designer 含母群來源、載入、確認科目、下一步；Form 只加入 `plSetting`。
- 條件 Designer／主程式沒有 `plStudentView`、`dgvStudentList`、舊計算／上一步／Loading。
- 條件建構子與 Load 在設計時期不查資料庫。
- 確認 Designer 有三個 Grid、搜尋、恢復、匯出、上一步、計算；沒有鎖 `MaximumSize`。
- 確認預設建構子可建立兩個實例；填列後計算按鈕停用；`HideForHost` 後不可見。
- 計算入口仍停用。

`RegularRankOperationCheck` 同步通過，含 2066 人合成 Grid。

## 未實測

| 項目 | 狀態 |
| --- | --- |
| VS2022 開啟條件 Form Designer | 未執行 |
| VS2022 開啟確認 Form Designer | 未執行 |
| Designer 調整後儲存與重開 | 未執行 |
| iSchool 下一步／上一步／關閉／重開 | 未執行 |
| 實際畫面位置與縮放 | 未執行 |
| 資料庫載入母群 | 未執行 |

編譯與離線結構檢查不能代替 VS2022 Designer 或 iSchool 畫面驗收。
