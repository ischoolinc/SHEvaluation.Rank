# Todo - 修正 CalculateTechnologyAssessmentRankStep2_111.cs UI 處理問題
目標：修正 Step2 視窗在「計算」流程中的 UI 與執行緒處理問題，避免跨執行緒操作 UI、避免錯誤處理失效與視窗卡死，並維持與 Step1 傳入參數的行為一致。完成後請在 `技職繁星成績排名計算調整1201.md` 紀錄本次修改內容與測試結果。

---

## Cursor-ANCHOR

- 專案：技職繁星成績排名計算（Step1 / Step2 Wizard）
- 主要檔案：
  - `CalculateTechnologyAssessmentRankStep2_111.cs`
- 相關檔案（不一定要修改）：
  - `CalculateTechnologyAssessmentRankStep1_111.cs`（負責呼叫 Step2 與傳遞資料）

---

## Todo 1：避免在 BackgroundWorker.DoWork 內直接存取 UI 控制項（dgvStudentList）

**問題說明**  
目前 Step2 在按下「開始計算」按鈕（例如 `btnCacluate_Click`）後，BackgroundWorker.DoWork 內直接透過：

```csharp
foreach (DataGridViewRow row in dgvStudentList.Rows)
{
    // 使用 row.Tag、row.Cells[colStudentName.Index].Value ...
}
```

來組 SQL。  
這會造成：

- `DoWork` 在背景執行緒執行，但 WinForms UI 控制項（`dgvStudentList` 及其 Cells、Tag）只能在 UI thread 操作。
- 容易觸發 `Cross-thread operation not valid` 例外，或在不同 .NET 版本下出現不穩定行為。

### Todo 1-1：在 UI thread 先把需要的資料複製成 DTO list

1. 在 `btnCacluate_Click` 事件中，在呼叫 `bkw.RunWorkerAsync()` **之前**，將 `dgvStudentList` 中需要的欄位轉成一個 List of DTO，範例：

   ```csharp
   private class RankStudentDto
   {
       public string StudentID { get; set; }
       public string StudentName { get; set; }
       public string RankGradeYearText { get; set; }
       public string DeptName { get; set; }
       public string ClassName { get; set; }
       public string Tag1 { get; set; }
       public string Tag2 { get; set; }
       public string RegGroupName { get; set; }
       public string RegGroupCode { get; set; }
   }
   ```

2. 在 `btnCacluate_Click` 內建立這個 List：

   ```csharp
   var rankStudents = new List<RankStudentDto>();

   foreach (DataGridViewRow row in dgvStudentList.Rows)
   {
       if (row.IsNewRow) continue;

       var dto = new RankStudentDto
       {
           StudentID        = "" + row.Tag,
           StudentName      = "" + row.Cells[colStudentName.Index].Value,
           RankGradeYearText= "" + row.Cells[colSchoolRank.Index].Value,
           DeptName         = "" + row.Cells[colDeptName.Index].Value,
           ClassName        = "" + row.Cells[colClassRank.Index].Value,
           Tag1             = "" + row.Cells[colRankType1.Index].Value,
           Tag2             = "" + row.Cells[colRankType2.Index].Value,
           RegGroupName     = "" + row.Cells[colRegGroup.Index].Value,
           RegGroupCode     = "" + row.Cells[colRegGroupCode.Index].Value
       };

       rankStudents.Add(dto);
   }
   ```

3. 將 `rankStudents` 透過 BackgroundWorker 傳入 DoWork：

   ```csharp
   bkw.RunWorkerAsync(rankStudents);
   ```

### Todo 1-2：在 DoWork 透過 e.Argument 使用 DTO，不再碰 UI 控制項

1. 修改 `bkw.DoWork` 事件簽名：

   ```csharp
   bkw.DoWork += (sender, e) =>
   {
       var rankStudents = e.Argument as List<RankStudentDto>;
       // 使用 rankStudents 來組 SQL，不再使用 dgvStudentList
   };
   ```

2. 所有原本拿 `row.Tag`、`row.Cells[...]` 來組 `studentSql` 的地方，改成使用 `rankStudents` 中對應欄位。

---

## Todo 2：修正 BackgroundWorker 的錯誤處理與 UI 更新位置

**問題說明**  
目前程式在 DoWork 中對 SQL 執行採用類似以下模式：

```csharp
try
{
    queryHelper.Select(insertRankSql);
}
catch
{
    MsgBox.Show("計算失敗");
}
```

並在 RunWorkerCompleted 中無條件顯示「計算完成」，造成：

- 真正發生錯誤時，錯誤被吃掉，背景執行緒卻直接彈出 `MsgBox`（跨執行緒 UI 問題）。
- 使用者仍會看到「計算完成」訊息，與實際結果不符。

### Todo 2-1：改用 BackgroundWorker 的 e.Error 或 e.Result 回傳例外

1. 將 DoWork 中對 DB 的執行修改為：

   ```csharp
   bkw.DoWork += (sender, e) =>
   {
       try
       {
           // 組好 insertRankSql 之後
           QueryHelper queryHelper = new QueryHelper();
           queryHelper.Select(insertRankSql);
       }
       catch (Exception ex)
       {
           e.Result = ex;   // 或者直接 throw，交給 e.Error 處理
       }
   };
   ```

2. 不要在 DoWork 直接呼叫 `MsgBox.Show` 或任何 UI 元件。

### Todo 2-2：在 RunWorkerCompleted（UI thread）內統一處理成功 / 失敗

1. 在 `bkw.RunWorkerCompleted` 中：

   ```csharp
   bkw.RunWorkerCompleted += (sender, e) =>
   {
       pbLoading.Visible = false;
       btnCacluate.Enabled = true;
       btnPrevious.Enabled = true;

       if (e.Error != null || e.Result is Exception)
       {
           var ex = e.Error ?? (Exception)e.Result;
           MsgBox.Show("計算失敗：" + ex.Message);
           FISCA.Presentation.MotherForm.SetStatusBarMessage("");
           return;
       }

       MessageBox.Show("計算完成");
       FISCA.Presentation.MotherForm.SetStatusBarMessage("");
       this.DialogResult = DialogResult.OK;
   };
   ```

2. 確保：
   - 所有 `MessageBox` / `MsgBox` / `pbLoading.Visible` / `btnCacluate.Enabled` 等 UI 操作，都只在 RunWorkerCompleted 或 UI 事件（如 Click）中執行。
   - DoWork 專注在處理資料與 SQL，發生例外透過 `e.Error` 或 `e.Result` 回傳。

---

## Todo 3：處理「沒有任何學生可計算」的 UI 流程

**問題說明**  
目前如果 `dgvStudentList` 沒有任何要排名的學生，或 DTO list 為空，仍可能進入組 SQL 的流程，產生空的 CTE / 無效 SQL，或執行沒有意義的運算。

### Todo 3-1：在 UI thread 先檢查是否有學生

1. 在 `btnCacluate_Click` 裡，建立 `rankStudents` 之後，先做檢查：

   ```csharp
   if (rankStudents.Count == 0)
   {
       MsgBox.Show("沒有可供計算的學生。");
       return;
   }
   ```

2. 僅在有學生時才呼叫 `bkw.RunWorkerAsync(rankStudents)`。

### Todo 3-2：DoWork 中再加防護（避免未來修改漏掉）

1. 在 DoWork 開頭再做一次保險檢查：

   ```csharp
   var rankStudents = e.Argument as List<RankStudentDto>;
   if (rankStudents == null || rankStudents.Count == 0)
   {
       e.Result = new InvalidOperationException("沒有可供計算的學生");
       return;
   }
   ```

2. 讓 RunWorkerCompleted 能夠顯示適當錯誤訊息，而不是執行到無效 SQL。

---

## Todo 4：SQL 字串中處理學生姓名 / 群組名稱的單引號（避免 SQL 壞掉）

**問題說明**  
目前組 `studentSql` 的時候，直接用字串插入學生姓名、科別名稱、學群名稱：

```csharp
, '" + "" + row.Cells[colStudentName.Index].Value + @"'::TEXT AS student_name
...
, '" + "" + row.Cells[colDeptName.Index].Value + @"'::TEXT AS rank_dept_name
...
, '" + "" + row.Cells[colRegGroup.Index].Value + @"'::TEXT AS rank_group_name
```

若學生姓名或群組名稱含有 `'`（如「O'Neil」），會導致 SQL 變成：

```sql
, 'O'Neil'::TEXT AS student_name
```

造成語法錯誤，甚至潛在 SQL injection 風險。

### Todo 4-1：實作一個共用的 SQL 字串轉義方法

1. 在 `CalculateTechnologyAssessmentRankStep2_111.cs` 類別中加入：

   ```csharp
   private string SqlLiteral(string value)
   {
       if (string.IsNullOrEmpty(value))
           return "''";
       return "'" + value.Replace("'", "''") + "'";
   }
   ```

### Todo 4-2：所有字串欄位都改用 SqlLiteral

1. 在組 `studentSql` 時，改成：

   ```csharp
   string studentSql = @"
   SELECT
       " + studentId + @"::BIGINT AS student_id
       , " + SqlLiteral(studentName) + @"::TEXT AS student_name
       , " + rankGradeYear + @"::INT AS rank_grade_year
       , " + SqlLiteral(deptName) + @"::TEXT AS rank_dept_name
       , " + SqlLiteral(className) + @"::TEXT AS rank_class_name
       , " + SqlLiteral(tag1) + @"::TEXT AS rank_tag1
       , " + SqlLiteral(tag2) + @"::TEXT AS rank_tag2
       , " + SqlLiteral(regGroupName) + @"::TEXT AS rank_group_name
       , " + SqlLiteral(regGroupCode) + @"::TEXT AS rank_group_code
   ";
   ```

2. 確認所有用於 SQL 的字串欄位都透過 `SqlLiteral` 處理。

---

## Todo 5（可選）：優化 Tag 查詢與其它小效能

> 此段屬於邏輯/效能優化，與 UI 直接關聯較小，可視專案時間安排決定是否本次一併處理。

### Todo 5-1：避免對同一學生在 Tag 清單上多次搜尋

目前邏輯可能是：

```csharp
if (Tag1Student.Where(x => x.RefStudentID == studentID).Count() > 0)
{
    tag1 = Tag1Student.First(x => x.RefStudentID == studentID).Name;
}
```

同一個學生會對 List 做兩次搜尋。建議改為：

```csharp
var t1 = Tag1Student.FirstOrDefault(x => x.RefStudentID == studentID);
if (t1 != null)
{
    tag1 = t1.Name;
}
```

此外也可以將 Tag 資料預先轉成 `Dictionary<string, string>`（Key = StudentID, Value = TagName），以 `TryGetValue` 方式取得，效能會更好。

---

## Todo 6：完成修改後的紀錄（技職繁星成績排名計算調整1201.md）

1. 新增或開啟檔案：`技職繁星成績排名計算調整1201.md`。
2. 在檔案中加入一個新章節，例如：

   ```markdown
   ## 2025-12-01 Step2 UI / 計算流程調整

   - 調整 `CalculateTechnologyAssessmentRankStep2_111.cs`：
     - 在 UI thread 先將 `dgvStudentList` 的學生資料複製為 DTO list，透過 BackgroundWorker 傳入 DoWork，避免跨執行緒操作 DataGridView。
     - 調整 BackgroundWorker 的錯誤處理方式，利用 `e.Error` / `e.Result` 統一在 `RunWorkerCompleted` 處理，避免 DoWork 內直接顯示訊息視窗。
     - 加入「沒有可計算學生」的檢查，避免產生無效 SQL。
     - 新增 `SqlLiteral` 方法，對學生姓名、科別名稱、學群名稱等字串欄位做單引號轉義，避免 SQL 語法錯誤。
   - 測試項目：
     - 一般情境：有多名學生、含 Tag 與學群資料，按「計算」可正常完成並顯示「計算完成」，資料寫入正確。
     - 錯誤情境：故意讓 SQL 失敗（例如關閉資料庫或修改 Connection），確認會顯示「計算失敗」，不會誤顯示「計算完成」，且視窗不會卡死。
     - 特殊字串：建立一位學生姓名含 `'`（如 O'Neil），確認計算過程不會拋出 SQL 語法錯誤。
     - 無學生情境：清空學生列表後按「計算」，顯示適當提示訊息，不執行 SQL。
   ```

3. 儲存 `技職繁星成績排名計算調整1201.md`，確保未來可以追蹤這次 UI / 執行緒 / SQL 防護修正的目的與影響範圍。

---

## Cursor-RUN 建議順序

1. 打開 `CalculateTechnologyAssessmentRankStep2_111.cs`。
2. 依 **Todo 1** 實作 `RankStudentDto`，並改成先在 `btnCacluate_Click` 收集資料、`RunWorkerAsync(dtoList)`。
3. 依 **Todo 2** 改寫 BackgroundWorker.DoWork / RunWorkerCompleted 的錯誤處理與 UI 更新方式。
4. 依 **Todo 3** 增加「沒有學生」的檢查。
5. 依 **Todo 4** 加入 `SqlLiteral` 並套用到所有 SQL 用字串欄位。
6. 視時間決定是否先處理 **Todo 5** 中的 Tag 查詢優化。
7. 編譯並在測試環境實際操作 Step2（搭配 Step1），確認流程與訊息都正確。
8. 編輯並更新 `技職繁星成績排名計算調整1201.md`，記錄這次修改。

