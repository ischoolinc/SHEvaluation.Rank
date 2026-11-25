# Task 2.2 唯讀逐筆比較夾具

狀態：比較夾具已依審查修正並重新驗證。沒有連線資料庫，沒有修改 `baseline/source`、正式排名程式、原始 SQL 或學期功能。沒有進入 Task 2.3。

## 比較單位

一列是一個矩陣裡的一名學生。識別鍵取自基準 SQL 把 `score_list` 接上 `rank_matrix` 的條件，再加學生 ID：

`school_year`、`semester`、`grade_year`、`item_type`、`ref_exam_id`、`item_name`、`rank_type`、`rank_name`、`student_id`

`ref_batch_id` 不在鍵裡。每次計算都會新建批次，把它放進鍵會讓同一份結果全部變成缺漏與多出。`is_alive` 是發布旗標，不是成績統計，不列入比較值。

識別欄位若是 JSON 數字，先正規化再分組，所以 `105` 與 `105.0` 是同一個學生。報告仍印出該側 JSON 的原始字面；兩邊字面不同時會多印 `candidate識別鍵`。字串鍵不做數值換算。

## 比較值

明細：`score`、`rank`、`pr`、`percentile`。

矩陣統計：`matrix_count`、`avg_top_25`、`avg_top_50`、`avg`、`avg_bottom_50`、`avg_bottom_25`、`level_gte100`、`level_90`、`level_80`、`level_70`、`level_60`、`level_50`、`level_40`、`level_30`、`level_20`、`level_10`、`level_lt10`、`pr_88`、`pr_75`、`pr_50`、`pr_25`、`pr_12`、`std_dev_pop`。

JSON 數字保留原始字面，不轉成 `double` 或 `decimal`，也不用容差。比較時把字面正規成「有效位數＋十進位尺度」的字串，再做字串相等。因此：

- `90`、`90.0`、`90.00`、`9.0e1`、`9E+1`、`900e-1` 相同。
- `90.12345678901234567890123456781` 與末位 `...782` 不同；也與科學記號 `9.012345678901234567890123456781e1` 的同一數值相同。
- 兩邊都是 JSON `null` 視為相等。只有一邊是 `null` 記成 NULL 差異。`null` 與 `0` 是 NULL 差異，不是數值差異，也不視為相等。
- `-0`、`0`、`0.0` 視為同一個數值。

報告裡的 baseline／candidate 值是 JSON 原始字面，`null` 印成 `NULL`。

## 不去做重或 JOIN

同一個識別鍵的每一列都保留。先按鍵分組，組內依正規化後的比較值排序，排序相同再依原本順序，然後逐列配對。配對數是較短那邊的列數。每一列最多配一次，不會變成兩邊筆數相乘，也不會先合併成一筆。

- 兩邊各兩筆相同列：相等配對數是 2，不是 1，也不是 4。
- 一邊兩筆、另一邊一筆：多出來的那筆記成缺漏或 extra，不能因為其中一筆值相同就整組通過。
- 總筆數相同但學生不同：一筆缺漏、一筆多出，結果失敗。

通過條件是缺漏、多出、數值差異、NULL 差異都是 0。總筆數相同本身不會通過。重複鍵本身不單獨造成失敗；次數不同時，多出來的列會進入缺漏或多出，結果失敗。

## 差異報告

`--baseline` 比較會把報告印到標準輸出。加上 `--report 路徑` 時，同一份文字以 UTF-8（含 BOM）存檔。案例模式只在預期不符時印出報告。

摘要列固定保留各類計數與最終狀態，例如：

```text
baseline=1 candidate=1 equalPairs=0 missing=0 extra=0 valueDifferences=1 nullDifferences=0 duplicates=0 passed=False
```

有差異時才接著印明細。數值差異與 NULL 差異包含完整識別鍵、欄位、兩邊原始值、兩邊來源列與檔案行。缺漏與多出包含完整識別鍵，以及該側的來源列與檔案行。重複包含完整識別鍵、兩邊筆數，以及兩邊全部來源列與檔案行。

來源列是該側輸入 JSON 陣列的 1-based 順序。檔案行是該物件 `{` 所在的 1-based 行號。兩者都不是資料庫列號。

同一個學生、`score` 只有末位不同時，實際報告是：

```text
baseline=1 candidate=1 equalPairs=0 missing=0 extra=0 valueDifferences=1 nullDifferences=0 duplicates=0 passed=False
數值差異 1
[1]
識別鍵: school_year=113 | semester=2 | grade_year=1 | item_type=定期評量/科目成績 | ref_exam_id=15 | item_name=國文 | rank_type=年排名 | rank_name=1年級 | student_id=101
欄位: score
baseline: 90.12345678901234567890123456781
candidate: 90.12345678901234567890123456782
baseline來源列: 1
baseline檔案行: 2
candidate來源列: 1
candidate檔案行: 2
```

這次比較的行程回傳碼是 1。`baseline` 兩筆相同列、`candidate` 一筆 `90.0` 時，回傳碼也是 1，相等配對 1，缺漏是 baseline 第 2 列，重複筆數是 2 與 1：

```text
baseline=2 candidate=1 equalPairs=1 missing=1 extra=0 valueDifferences=0 nullDifferences=0 duplicates=1 passed=False
缺漏 1
[1]
識別鍵: school_year=113 | semester=2 | grade_year=1 | item_type=定期評量/科目成績 | ref_exam_id=15 | item_name=國文 | rank_type=年排名 | rank_name=1年級 | student_id=101
baseline來源列: 2
baseline檔案行: 3
重複 1
[1]
識別鍵: school_year=113 | semester=2 | grade_year=1 | item_type=定期評量/科目成績 | ref_exam_id=15 | item_name=國文 | rank_type=年排名 | rank_name=1年級 | student_id=101
baseline筆數: 2
candidate筆數: 1
baseline來源列: 1, 2
candidate來源列: 1
baseline檔案行: 2, 3
candidate檔案行: 2
```

## 離線案例

案例在 `cases/`。`defaults.json` 提供共同欄位，個別案例只覆寫差異。`std_dev_pop` 預設是 NULL，用來確認兩邊都 NULL 時不算差異。數值案例只改 `pr_88`，用來確認統計欄位有參加比較。原有 8 個案例都保留。

| 案例 | 預期 |
| --- | --- |
| 相同結果 | 通過，兩筆都相等 |
| 數值不同 | 失敗，`pr_88` 數值差異，總筆數仍相同 |
| 缺漏 | 失敗，缺 1 筆 |
| 多出 | 失敗，多 1 筆 |
| 重複次數不同 | 失敗，2 對 1，相等配對 1，缺漏 1 |
| 重複次數一致 | 通過，相等配對 2 |
| NULL 差異 | 失敗，只有 `avg` 一邊是 NULL |
| 筆數相同但鍵不同 | 失敗，缺 1 筆且多 1 筆 |
| 高精度末位不同 | 失敗，`score` 的 `...781` 與 `...782` 為數值差異 |
| 等值不同表示法 | 通過，含 `90`／`90.0`、尾端零、`0`／`0.0`、負數、`105`／`105.0`，以及同一鍵 2 筆不同寫法 |
| 科學記號 | 通過，含 `9.0e1`、`9E+1`、`900e-1`、負指數與高精度科學記號 |
| NULL 與 0 不同 | 失敗，`score` 與 `avg` 為 NULL 差異，不是數值差異 |

`--cases` 印出的 `PASS` 表示案例預期成立。高精度案例的預期就是比較失敗，所以案例結果是 `PASS`，不代表那兩個分數被判成相同。直接比較兩份完整陣列時，該差異的行程回傳碼是 1。

本次執行：

```text
PASS 相同結果
PASS 數值不同
PASS 缺漏
PASS 多出
PASS 重複次數不同
PASS 重複次數一致
PASS NULL 差異
PASS 筆數相同但鍵不同
PASS 高精度末位不同
PASS 等值不同表示法
PASS 科學記號
PASS NULL 與 0 不同
ALL PASSED 12
```

行程回傳碼是 0。

## 大量結果

讀取不再使用 `JavaScriptSerializer`。舊實作的預設 `MaxJsonLength` 是 2,097,152；實測約 3,000 列、2,115,001 字元會載入失敗。現在先把整個檔案讀成字串，再由保留數字字面的剖析器處理，沒有那個長度上限。

`--large-check` 在暫存目錄產生完整欄位的結果，比較後刪除，不把巨大檔提交進儲存庫。產生規則是至少 3,000 列，而且字串長度必須大於 2,115,001。字元數是 UTF-8 解碼後的 .NET 字串長度，與舊的 `MaxJsonLength` 同一種計數；位元組數是檔案大小。本次產生檔使用 LF。

本次結果：

```text
largeRows=3812
largeChars=2115663
largeBytes=2230023
largeRowMinimum=3000
largeCharMinimumExclusive=2115001
identicalExit=0
identicalEqualPairs=3812
mutatedExit=1
mutatedEqualPairs=3811
mutatedValueDifferences=1
mutatedField=score
mutatedBaselineValue=90.12345678901234567890123456781
mutatedCandidateValue=90.12345678901234567890123456782
mutatedBaselineSourceRow=1907
mutatedCandidateSourceRow=1907
mutatedBaselineLine=1908
mutatedCandidateLine=1908
mutatedStudentId=101906
reportRoundTrip=True
tempDeleted=True
largeCheck=PASS
```

相同的 3,812 列比較回傳 0。只改第 1,907 列 `score` 末位後，比較回傳 1，存檔報告含完整識別鍵、兩個原始分數與來源位置。`--large-check` 自己的回傳碼 0 表示這兩項檢查都成立，不表示末位不同的那一對比較成功。

## 執行

來源是 UTF-8。這台機器的 ANSI 字碼頁是 950，必須指定 `/codepage:65001`。使用 .NET Framework 4.8 的 C# 5 編譯器，不加入正式 csproj，也不再參考 `System.Web.Extensions`：

```text
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /codepage:65001 /target:exe /out:RankResultComparer.exe RankResultComparer.cs
RankResultComparer.exe --cases cases
RankResultComparer.exe --large-check
```

比較兩份完整 JSON 陣列時，每一列都要自帶全部識別欄位與比較欄位：

```text
RankResultComparer.exe --baseline baseline.json --candidate candidate.json --report report.txt
```

回傳碼：比較一致為 0。比較不一致、JSON 無法剖析、找不到檔案或缺少參數都不是 0。本次另測過高精度不一致、`90` 對 `90.0`、科學記號、`null` 對 `0`、重複 2 對 1、壞 JSON、缺少檔案與缺少參數，回傳碼分別是 1、0、0、1、1、1、1、1。

## 限制

這次只證明夾具能分辨離線案例，以及能讀過先前失敗的結果大小。沒有從資料庫讀取原版或新版排名，因此還沒有做全參與回歸。後續把查詢結果編成上述 JSON 再交給這個程式；查詢本身不在本夾具內，也不連線。

整個 JSON 會載入記憶體。本次驗證到 3,812 列、2,115,663 字元、2,230,023 位元組。沒有做成串流讀取，更大的檔案受行程記憶體限制。

數字的指數本身必須放得進 `Int64`。去掉尾端零之後的十進位尺度也必須放得進 `Int64`。超過時直接拒絕，不改用 `double` 或 `decimal` 近似。有效位數以字串保留，不會被收成 28 到 29 位。

同一個物件裡若有重複欄位名稱，採用最後一個值。來源列與檔案行只能回到這份 JSON，不能對到資料庫。整份 JSON 寫在同一行時，檔案行都是 1，仍可用來源列區分。

組內配對是排序後逐列對上，不是替每一列搜尋最佳匹配。比較值相同的重複列會對上。同一個鍵裡各列的值不完全相同時，報告裡的數值差異可能不是最接近的那一對；沒配到的列仍以缺漏或多出保留，兩邊筆數不會被消掉。
