# Todo - CalculateTechnologyAssessmentRankStep2_111.cs 全面修正（UI＋執行緒＋SQL＋變數命名）
目標：
1. 修正 Step2 在「計算」流程中的 UI 與執行緒處理問題（避免跨執行緒操作 UI、視窗卡死）。  
2. 修正 BackgroundWorker 錯誤處理邏輯（DoWork 不直接操作 UI、不吞例外）。  
3. 修正 SQL 字串處理（避免單引號造成語法錯誤）。  
4. 修正 lambda 內變數命名衝突（`e` 變數）。  
5. 完成後記錄在 `技職繁星成績排名計算調整1201.md`。

---

## Cursor-ANCHOR

- 專案：技職繁星成績排名計算（Step1 / Step2 Wizard）
- 主要檔案：
  - `CalculateTechnologyAssessmentRankStep2_111.cs`
- 相關檔案（不用修改，但行為相依）：
  - `CalculateTechnologyAssessmentRankStep1_111.cs`（負責呼叫 Step2、傳入學生清單與設定）

---

## Todo 1：修正 lambda 內「e」變數命名衝突（編譯錯誤修正）

### 問題說明

在 `bkw.RunWorkerCompleted += (sender, e) => { ... }` 裡，目前有以下程式碼：

```csharp
if (strstr.Any())
{
    string data = "";
    foreach (string name in strstr)
        data += name + " ";
    
    var e = new Exception("下列學生科目設定重複:" + data);
    MessageBox.Show("下列學生科目設定重複:" + data);
    return;
}
```

- 外層 lambda 參數已經使用 `e`：`(sender, e)`。
- 內層再宣告 `var e = new Exception(...)`，造成 C# 編譯錯誤：  
  `A local or parameter named 'e' cannot be declared in this scope...`

### Todo 1-1：改變內部變數名稱或移除

1. 搜尋這段程式碼，將 `var e = new Exception(...)` 改名或刪除：

   ```csharp
   // 方案一：改名（若真的要保留 Exception 物件）
   var ex = new Exception("下列學生科目設定重複:" + data);
   MessageBox.Show("下列學生科目設定重複:" + data);
   return;

   // 方案二：不需要變數，直接顯示訊息即可（建議）
   MessageBox.Show("下列學生科目設定重複:" + data);
   return;
   ```

2. 全檔確認：**不要再在 `(sender, e)` 的 lambda 內宣告任何新的變數名稱為 `e`**，也不要在同一層使用 `catch (Exception e)`。統一用 `ex` 當 Exception 變數名。

---

## Todo 2：避免在 BackgroundWorker.DoWork 操作 UI（dgvStudentList 等）

目前版本已經將 `dgvStudentList` 內容先轉成 DTO list 再傳入 BackgroundWorker，請確認並保持以下設計：

### Todo 2-1：在 btnCacluate_Click 內收集 DataGridView 資料

1. 在 `btnCacluate_Click` 中，定義 DTO 類別（可放在 form 類別裡）：

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

2. 在 `btnCacluate_Click` 內，將 `dgvStudentList` 內容轉為 `List<RankStudentDto>`：

   ```csharp
   var rankStudents = new List<RankStudentDto>();

   foreach (DataGridViewRow row in dgvStudentList.Rows)
   {
       if (row.IsNewRow) continue;

       var dto = new RankStudentDto
       {
           StudentID         = "" + row.Tag,
           StudentName       = "" + row.Cells[colStudentName.Index].Value,
           RankGradeYearText = "" + row.Cells[colSchoolRank.Index].Value,
           DeptName          = "" + row.Cells[colDeptName.Index].Value,
           ClassName         = "" + row.Cells[colClassRank.Index].Value,
           Tag1              = "" + row.Cells[colRankType1.Index].Value,
           Tag2              = "" + row.Cells[colRankType2.Index].Value,
           RegGroupName      = "" + row.Cells[colRegGroup.Index].Value,
           RegGroupCode      = "" + row.Cells[colRegGroupCode.Index].Value
       };

       rankStudents.Add(dto);
   }
   ```

3. 檢查無學生時的行為（見 Todo 3），最後以 `rankStudents` 作為 `RunWorkerAsync` 參數：

   ```csharp
   if (rankStudents.Count == 0)
   {
       MsgBox.Show("沒有可供計算的學生。");
       return;
   }

   // 開始背景計算
   pbLoading.Visible = true;
   btnCacluate.Enabled = false;
   btnPrevious.Enabled = false;

   bkw.RunWorkerAsync(rankStudents);
   ```

### Todo 2-2：DoWork 內只用 DTO，不碰任何 UI 控制項

1. 在 `bkw.DoWork` 內：

   ```csharp
   bkw.DoWork += (sender, e) =>
   {
       var rankStudents = e.Argument as List<RankStudentDto>;

       if (rankStudents == null || rankStudents.Count == 0)
       {
           e.Result = new InvalidOperationException("沒有可供計算的學生");
           return;
       }

       // 以下所有 SQL 組合都只能用 rankStudents，不要使用 dgvStudentList 或畫面控制項
       ...
   };
   ```

2. 確認 DoWork 裡不再有任何 `dgvStudentList`、`pbLoading`、`btn...` 相關的存取。

---

## Todo 3：BackgroundWorker 錯誤處理與 UI 邏輯

### Todo 3-1：DoWork 中捕捉 Exception，透過 e.Result / e.Error 回傳

1. 在 DoWork 內，將所有對 DB 執行 SQL、複雜計算包在 try/catch 內：

   ```csharp
   bkw.DoWork += (sender, e) =>
   {
       var rankStudents = e.Argument as List<RankStudentDto>;
       if (rankStudents == null || rankStudents.Count == 0)
       {
           e.Result = new InvalidOperationException("沒有可供計算的學生");
           return;
       }

       try
       {
           // 1. 讀取 UDT Subject 資料
           // 2. 依學群組出科目與計算設定
           // 3. 組成 student_list / subject_score_data / 各種 CTE
           // 4. 呼叫 QueryHelper 執行 insert / update
       }
       catch (Exception ex)
       {
           e.Result = ex;
       }
   };
   ```

2. **禁止**在 DoWork 中出現：`MsgBox.Show`、`MessageBox.Show`、`pbLoading.Visible = ...` 等 UI 操作。

### Todo 3-2：RunWorkerCompleted 中統一處理成功 / 失敗與 UI 更新

1. 在 `bkw.RunWorkerCompleted` 內，統一負責：

   ```csharp
   bkw.RunWorkerCompleted += (sender, e) =>
   {
       pbLoading.Visible = false;
       btnCacluate.Enabled = true;
       btnPrevious.Enabled = true;

       // 判斷是否有錯
       if (e.Error != null || e.Result is Exception)
       {
           var ex = e.Error ?? (Exception)e.Result;
           MsgBox.Show("計算失敗：" + ex.Message);
           FISCA.Presentation.MotherForm.SetStatusBarMessage("");
           return;
       }

       // 正常完成
       MessageBox.Show("計算完成");
       FISCA.Presentation.MotherForm.SetStatusBarMessage("");
       this.DialogResult = DialogResult.OK;
   };
   ```

2. 若有「學生科目設定重複」等邏輯錯誤，需要中止計算，也應在此函式中處理（如 Todo 1 的 `strstr.Any()` 判斷），而非在 DoWork 中直接彈出訊息。

---

## Todo 4：避免計算中關閉視窗造成例外（FormClosing）

### 問題說明

- 當 BackgroundWorker 正在運作時，使用者可能按右上角「X」關閉 Step2。
- 若允許視窗被關閉，BackgroundWorker 完成時呼叫 `pbLoading.Visible = false`、`btnCacluate.Enabled = true` 有機會操作已 Dispose 的控制項，導致例外。

### Todo 4-1：簡單防護（禁止計算中關閉視窗）

1. 在 `CalculateTechnologyAssessmentRankStep2_111` 的 `FormClosing` 事件中加入：

   ```csharp
   private void CalculateTechnologyAssessmentRankStep2_111_FormClosing(object sender, FormClosingEventArgs e)
   {
       if (pbLoading.Visible)
       {
           MsgBox.Show("正在計算中，請稍候計算完成再關閉視窗。");
           e.Cancel = true;
       }
   }
   ```

2. 此方式簡單直接，可避免大多數「中途關閉造成例外」情況。

> 若要更進階的取消機制，可以把 BackgroundWorker 宣告成欄位 `_bkw`，並支援 `CancellationPending` / `CancelAsync`，但在本次 Todo 中不強制。

---

## Todo 5：SQL 字串中的單引號處理（SqlLiteral）

### Todo 5-1：統一使用 SqlLiteral

1. 在 `CalculateTechnologyAssessmentRankStep2_111.cs` 類別內已新增：

   ```csharp
   private string SqlLiteral(string value)
   {
       if (string.IsNullOrEmpty(value))
           return "''";
       return "'" + value.Replace("'", "''") + "'";
   }
   ```

2. 請確認所有組成 SQL 的「文字欄位」均使用 `SqlLiteral`：

   - 學生姓名 (`student_name`)
   - 科別名稱 (`rank_dept_name`)
   - 班級名稱 (`rank_class_name`)
   - 類別一 (`rank_tag1`)
   - 類別二 (`rank_tag2`)
   - 學群名稱 (`rank_group_name`)
   - 學群代碼 (`rank_group_code`)

   範例：

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

3. 確保沒有殘留 `'" + studentName + @"'` 這種沒有透過 `SqlLiteral` 的寫法。

---

## Todo 6（可選）：TAG 查詢與效能整理

> 此部分屬於效能與可讀性優化，與 UI 穩定性無強烈依賴，可視時間決定是否本次處理。

### Todo 6-1：避免對 Tag List 重複搜尋

如果目前有模式如下：

```csharp
if (Tag1Student.Where(x => x.RefStudentID == id).Count() > 0)
{
    tag1 = Tag1Student.First(x => x.RefStudentID == id).Name;
}
```

改寫為：

```csharp
var t1 = Tag1Student.FirstOrDefault(x => x.RefStudentID == id);
if (t1 != null)
    tag1 = t1.Name;
```

或在載入 Tag 時就轉成 Dictionary：

```csharp
Dictionary<string, string> Tag1Dict = Tag1Student
    .GroupBy(x => x.RefStudentID)
    .ToDictionary(g => g.Key, g => g.First().Name);
```

後面直接 `Tag1Dict.TryGetValue(studentId, out tag1)` 即可。

---

## Todo 7：修改完成後紀錄在「技職繁星成績排名計算調整1201.md」

1. 打開或建立 `技職繁星成績排名計算調整1201.md`。
2. 增加一節紀錄 Step2 的此次調整：

   ```markdown
   ## 2025-12-01 Step2 UI / 執行緒 / SQL 修正

   - 調整 `CalculateTechnologyAssessmentRankStep2_111.cs`：
     - 修正 RunWorkerCompleted 內 lambda 使用 `e` 名稱衝突問題，避免編譯錯誤。
     - 將 DataGridView 學生資料先在 UI thread 收集為 DTO，透過 BackgroundWorker 傳入 DoWork，避免跨執行緒操作 UI。
     - 調整 BackgroundWorker 錯誤處理：DoWork 不再直接顯示訊息視窗，透過 `e.Result` / `e.Error` 回傳例外，RunWorkerCompleted 統一處理成功/失敗訊息與按鈕狀態。
     - 新增/統一使用 `SqlLiteral` 方法，處理學生姓名、科別名稱、學群名稱中的單引號，避免 SQL 語法錯誤。
     - 在 FormClosing 中防止計算過程中關閉視窗導致例外。
   - 測試項目：
     - 有多名學生（含不同群別、Tag）的正常計算流程，確認結果寫入正確，顯示「計算完成」。
     - 故意關閉資料庫或改錯 Connection，確認會顯示「計算失敗」，不會顯示「計算完成」，且視窗不會卡死。
     - 建立姓名含 `'` 的學生（例如 O'Neil），確認計算過程無 SQL 語法錯誤。
     - 計算進行中嘗試關閉視窗，確認有提示並且不會造成例外。
   ```

3. 保存檔案，作為日後查詢這次調整的依據。

---

## Cursor-RUN 建議順序

1. 開啟 `CalculateTechnologyAssessmentRankStep2_111.cs`。
2. 依 **Todo 1** 修正 RunWorkerCompleted 內 `var e = ...` 命名衝突（改成 ex 或移除）。
3. 依 **Todo 2** 核對 DataGridView → DTO → RunWorkerAsync 流程是否正確，DoWork 裡不再使用任何 UI 控制項。
4. 依 **Todo 3** 確認 BackgroundWorker 錯誤處理與 UI 更新邏輯一致（DoWork 只算資料／丟錯，RunWorkerCompleted 更新 UI）。
5. 依 **Todo 4** 加入 FormClosing 防護，禁止計算中關閉視窗。
6. 依 **Todo 5** 檢查所有 SQL 字串都使用 `SqlLiteral` 處理文字欄位。
7. 視時間再處理 **Todo 6**（Tag 查詢效能優化）。
8. 編譯專案並實際測試 Step1→Step2 完整流程。
9. 最後更新 `技職繁星成績排名計算調整1201.md`，記錄本次修正與測試結果。

