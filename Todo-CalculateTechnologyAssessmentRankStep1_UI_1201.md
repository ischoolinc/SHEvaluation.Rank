# Todo - 修正 CalculateTechnologyAssessmentRankStep1_111.cs UI 處理問題
目標：修正 Step1 視窗在「下一步」流程中的 UI 與執行緒處理問題，避免跨執行緒操作 UI、避免重複 ShowDialog，並維持與 Step2 的參數傳遞正確。完成後請在 `技職繁星成績排名計算調整1201.md` 紀錄本次修改內容與測試結果。

---

## Cursor-ANCHOR

- 專案：技職繁星成績排名計算（Step1 / Step2 Wizard）
- 主要檔案：
  - `CalculateTechnologyAssessmentRankStep1_111.cs`
  - （僅呼叫，不修改）`CalculateTechnologyAssessmentRankStep2_111.cs`

---

## Todo 1：修正 Step1 呼叫 Step2 時的 ShowDialog 邏輯（避免重複呼叫）

**問題說明**  
目前在 Step1 內（通常在 `_bgWorkerStep1Next_RunWorkerCompleted` 或對應位置），呼叫 Step2 的程式碼邏輯為：

```csharp
ctrr2.StartPosition = FormStartPosition.CenterScreen;
if (ctrr2.ShowDialog() == DialogResult.Cancel)
{
    ...
}
else if (ctrr2.ShowDialog() == DialogResult.OK)
{
    this.Close();
}
```

上述寫法會執行兩次 `ShowDialog()`，造成 Step2 視窗被開啟兩次，且第一次回傳的 `DialogResult` 完全被忽略，是明顯的 UI 邏輯 bug。

### Todo 1-1：改成只呼叫一次 ShowDialog()

1. 找出呼叫 Step2 的程式區塊（通常在 Step1 計算完成、準備好 Step2 需要的資料後）。
2. 將程式碼改成下列模式：

```csharp
ctrr2.StartPosition = FormStartPosition.CenterScreen;
var result = ctrr2.ShowDialog();

if (result == DialogResult.Cancel)
{
    // 回到 Step1：重新啟用 Step1 的 UI
    FISCA.Presentation.MotherForm.SetStatusBarMessage(string.Empty);
    this.Visible = true;
    this.StartPosition = FormStartPosition.CenterScreen;
    this.UserControlEnale(true);   // 如果有這個方法，負責啟用按鈕/控制項
}
else if (result == DialogResult.OK)
{
    // 直接關閉 Step1，代表使用者在 Step2 完成整個流程
    this.Close();
}
```

3. 確保不再有第二次 `ShowDialog()` 呼叫。

### Todo 1-2：確認按鈕與狀態列恢復邏輯

1. 當 Step2 回傳 `DialogResult.Cancel` 時：
   - Step1 的 `btnNext` / `btnExit` 等按鈕應該恢復可用。
   - 母視窗 `StatusBarMessage` 應清空或顯示適合訊息（例如「準備就緒」）。
2. 如果現有程式已經有 `UserControlEnale(bool)` 或類似方法，請統一用同一個方法處理控制項啟用/停用，避免散落多處。

---

## Todo 2：修正 BackgroundWorker 與 Step2 的關聯呼叫位置（避免跨執行緒 UI 問題）

**問題說明**  
在 `CalculateTechnologyAssessmentRankStep1_111.cs` 的 `_bgWorkerStep1Next_DoWork` 內，目前直接呼叫 Step2 的方法，例如：

```csharp
ctrr2.SetRegistrationDept(tmpDeptDict);
ctrr2.SetRegistrationSubject(tmpSubjDict);
```

`DoWork` 在背景執行緒執行，如果這些方法未來在 Step2 中加入任何 UI 操作（例如更新控制項），會造成跨執行緒操作 UI 的問題，隱藏風險很高。

### Todo 2-1：DoWork 只負責計算與準備資料

1. 在 `_bgWorkerStep1Next_DoWork` 中：
   - 保留資料查詢與計算（如產生 `tmpDeptDict`、`tmpSubjDict`、`_StudentFilterList` 等）。
   - **不要**在 DoWork 內呼叫 `ctrr2.SetRegistrationDept(...)` 或 `ctrr2.SetRegistrationSubject(...)`。
2. 將 DoWork 需要傳回 UI thread 的資料，統一放進 `e.Result`（可用 Tuple 或自訂小型 DTO 類別，例如 `Step1NextResult`）：

   ```csharp
   e.Result = new Step1NextResult
   {
       DeptDict = tmpDeptDict,
       SubjDict = tmpSubjDict,
       StudentFilterList = _StudentFilterList,
       Tag1Student = tag1Student,
       Tag2Student = tag2Student,
       DeptNameDict = deptNameDict,
       ScoreErrIDList = scoreErrIDList,
       StudentList = _StudentList,
       StudentFilterIDList = studentFilterIDList
   };
   ```

   或使用 `Tuple<Dictionary<...>, ...>` 也可以，依現有程式風格選擇。

### Todo 2-2：在 RunWorkerCompleted（UI thread）再呼叫 Step2 設定資料

1. 在 `_bgWorkerStep1Next_RunWorkerCompleted` 中：
   - 先從 `e.Result` 取回剛才 DoWork 計算好的資料。
   - 再呼叫 Step2 的 setter：

   ```csharp
   var result = (Step1NextResult)e.Result;

   ctrr2.SetRegistrationDept(result.DeptDict);
   ctrr2.SetRegistrationSubject(result.SubjDict);

   ctrr2.SetStudentDataList(
       result.StudentFilterList,
       result.Tag1Student,
       result.Tag2Student,
       result.DeptNameDict,
       result.ScoreErrIDList,
       result.StudentList,
       result.StudentFilterIDList
   );
   ```

2. 確認上述呼叫都只在 UI thread 執行（`RunWorkerCompleted`），避免未來 Step2 內部如果調整 UI 會出現跨執行緒問題。

---

## Todo 3：整理 Step1 按鈕與視窗顯示流程（Wizard 使用體驗）

### Todo 3-1：btnNext_Click 事件流程檢查

1. 檢查 `btnNext_Click` 實作：
   - 按下「下一步」時，應：
     - 停用 Step1 的主要按鈕（`btnNext.Enabled = false`、如果有 `btnExit` 或其他需要鎖定的控制項一併停用）。
     - 顯示狀態列訊息（例如：「資料處理中…」）。
     - 只啟動 `_bgWorkerStep1Next.RunWorkerAsync()`，**不要在這裡直接 Show Step2**。
2. 確認不會在 `btnNext_Click` 中與 `RunWorkerCompleted` 重複顯示 Step2。

### Todo 3-2：RunWorkerCompleted 成功與失敗兩種情境

1. 在 `_bgWorkerStep1Next_RunWorkerCompleted`：
   - **成功**情況：
     - 設定 Step2 所需資料（`SetRegistrationDept`、`SetRegistrationSubject`、`SetStudentDataList` 等）。
     - 隱藏 Step1 視窗（`this.Visible = false;`，如有需要）。
     - 呼叫一次 `ctrr2.ShowDialog()` 並依回傳結果處理（對應 Todo 1-1）。
   - **失敗**情況（DoWork 內若有例外或狀態失敗）：
     - 顯示錯誤訊息（`MsgBox.Show(...)`）。
     - 還原 Step1 按鈕狀態（`btnNext.Enabled = true` 等）。
     - 清空或更新狀態列訊息。

> 如果目前 DoWork 內直接 `catch` 例外並吞掉，建議改成把例外帶回 `RunWorkerCompleted` 統一處理，風格可以參考 Step2 之後要改的方式。

---

## Todo 4：完成修改後的紀錄（技職繁星成績排名計算調整1201.md）

1. 新增或開啟檔案：`技職繁星成績排名計算調整1201.md`。
2. 在檔案中加入一個新章節，例如：

   ```markdown
   ## 2025-12-01 Step1 UI / Wizard 流程調整

   - 調整 `CalculateTechnologyAssessmentRankStep1_111.cs`：
     - 修正 Step2 `ShowDialog()` 只呼叫一次，避免重複開啟與邏輯錯誤。
     - 將對 Step2 的 `SetRegistrationDept`、`SetRegistrationSubject` 等呼叫，移至 `RunWorkerCompleted`（UI thread）執行，避免跨執行緒操作 UI。
     - 重新整理「下一步」流程中按鈕啟用/停用與狀態列訊息顯示。
   - 測試項目：
     - 一般情境：三年級一般生、無錯誤學生，按「下一步」→ 正常顯示 Step2、只出現一次。
     - Step2 按「取消」：可順利回到 Step1，按鈕與狀態列恢復正常。
     - 若 Step1 DataLoad 發生錯誤：顯示錯誤訊息，停留在 Step1，不會開啟 Step2。
   ```

3. 儲存 `技職繁星成績排名計算調整1201.md`，確保未來可以追蹤這次 UI 修正目的與影響範圍。

---

## Cursor-RUN 建議順序

1. 打開 `CalculateTechnologyAssessmentRankStep1_111.cs`。
2. 依 **Todo 1** 修正 Step2 `ShowDialog()` 呼叫邏輯。
3. 依 **Todo 2** 調整 BackgroundWorker DoWork / RunWorkerCompleted 的職責分工。
4. 依 **Todo 3** 檢查與微調按鈕啟用、StatusBar 訊息與 Step1/Step2 顯示流程。
5. 編譯並在測試環境實際操作 Step1 → Step2 Wizard，確認行為符合預期。
6. 編輯並更新 `技職繁星成績排名計算調整1201.md`，記錄這次修改。

