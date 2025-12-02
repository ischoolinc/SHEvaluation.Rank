# Todo - Step2_命名衝突修正專用 Todo_v2
目標：只處理 `CalculateTechnologyAssessmentRankStep2_111.cs` 內「變數 / 參數命名衝突」造成的編譯錯誤：  
- `sender` / `e` 在 lambda / delegate 內與外層衝突  
- `rankStudents` 在外層 method 與 DoWork 內再次宣告衝突  

> **本 Todo 不動商業邏輯、不調整 SQL，只改名字讓程式能乾淨編譯。**

---

## Cursor-ANCHOR

- 檔案：`CalculateTechnologyAssessmentRankStep2_111.cs`
- 範圍：
  - `btnCacluate_Click` 事件內建立 `BackgroundWorker` 的區塊
  - 其內的 `DoWork` 與 `RunWorkerCompleted` 兩個委派
  - 任何 `catch (Exception e)` 或重新宣告 `e` 的區域變數

---

## Todo 1：統一 BackgroundWorker 委派參數名稱，避免與外層 `(sender, e)` 衝突

### 問題說明

在 `btnCacluate_Click(object sender, EventArgs e)` 方法內，你目前用了類似寫法：

```csharp
BackgroundWorker bkw = new BackgroundWorker();

bkw.DoWork += delegate (object sender, DoWorkEventArgs e)
{
    ...
};

bkw.RunWorkerCompleted += delegate (object sender, RunWorkerCompletedEventArgs e)
{
    ...
};
```

- 外層 method 已經有 `(object sender, EventArgs e)`。
- 內層 delegate 又宣告 `(object sender, DoWorkEventArgs e)` / `(object sender, RunWorkerCompletedEventArgs e)`。
- 造成 C# 編譯錯誤：  
  `A local or parameter named 'sender' (或 'e') cannot be declared in this scope...`

### Todo 1-1：改名 DoWork 參數

1. 在 `btnCacluate_Click` 內，找到：

   ```csharp
   bkw.DoWork += delegate (object sender, DoWorkEventArgs e)
   {
       ...
   };
   ```

2. 將參數名稱統一改成不會和外層衝突的命名，例如：

   ```csharp
   bkw.DoWork += delegate (object bwSender, DoWorkEventArgs bwE)
   {
       // 下面所有原本使用 e.<...> 的地方，改用 bwE.<...>
   };
   ```

3. 在此 delegate 區塊中：
   - 將 `e.Argument` 改成 `bwE.Argument`
   - 將 `e.Result = ...` 改成 `bwE.Result = ...`
   - 若有其他地方使用 `e`（例如傳遞到其他方法），一併改成 `bwE`。

### Todo 1-2：改名 RunWorkerCompleted 參數

1. 找到：

   ```csharp
   bkw.RunWorkerCompleted += delegate (object sender, RunWorkerCompletedEventArgs e)
   {
       ...
   };
   ```

2. 改成：

   ```csharp
   bkw.RunWorkerCompleted += delegate (object bwSender, RunWorkerCompletedEventArgs bwE)
   {
       // 下面所有原本使用 e.<...> 的地方，改用 bwE.<...>
   };
   ```

3. 在此 delegate 區塊中：
   - 將 `e.Error` 改成 `bwE.Error`
   - 將 `e.Result` 改成 `bwE.Result`
   - 若有用到 `e.Cancelled` 之類屬性，也一併改成 `bwE.Cancelled`。

> 目的：**整個檔案內，只允許最外層 method 使用 `sender` / `e`，內部所有 BackgroundWorker 相關委派，一律改用 `bwSender` / `bwE`，避免作用域命名衝突。**

---

## Todo 2：避免在 DoWork 內再次宣告 `rankStudents` 名稱

### 問題說明

在 `btnCacluate_Click` 內：

```csharp
var rankStudents = new List<RankStudentDto>();
// 填入資料後
bkw.RunWorkerAsync(rankStudents);
```

在 `DoWork` 內可能有：

```csharp
bkw.DoWork += delegate (object bwSender, DoWorkEventArgs bwE)
{
    var rankStudents = bwE.Argument as List<RankStudentDto>;
    ...
};
```

- 外層已經有 `rankStudents` 這個 local 變數。  
- 內層又宣告 `var rankStudents = ...`，導致：  
  `A local or parameter named 'rankStudents' cannot be declared in this scope...`

### Todo 2-1：修改 DoWork 內部變數名稱

1. 在 DoWork delegate 內，把：

   ```csharp
   var rankStudents = bwE.Argument as List<RankStudentDto>;
   ```

   改成例如：

   ```csharp
   var workerStudents = bwE.Argument as List<RankStudentDto>;
   ```

2. 下方所有使用 `rankStudents` 的地方全部改為 `workerStudents`：

   ```csharp
   if (workerStudents == null || workerStudents.Count == 0)
   {
       bwE.Result = new InvalidOperationException("沒有可供計算的學生");
       return;
   }

   foreach (RankStudentDto dto in workerStudents)
   {
       ...
   }
   ```

> 只要內外層名稱不再重複，這個錯誤就會消失。你可以選擇改內層（建議），也可以改外層名稱成 `rankStudentList`、`rankStudentDtos` 等。

---

## Todo 3：清掉所有 lambda 內「重新宣告 e / sender」的寫法

### 問題說明

除了委派參數 `(object sender, EventArgs e)` 外，還有可能在 lambda/委派內重新宣告：

- `var e = new Exception(...)`
- `catch (Exception e)`
- `foreach (var e in someList)`

只要這些寫在 `(sender, e)` scope 裡，就會引發同樣的錯誤。

### Todo 3-1：檢查並修正 Exception 變數命名

1. 在整個 `CalculateTechnologyAssessmentRankStep2_111.cs` 檔案內搜尋：

   ```text
   catch (Exception e)
   ```

2. 將所有 `catch (Exception e)` 改成：

   ```csharp
   catch (Exception ex)
   ```

3. 將區塊內使用 `e` 的地方改成 `ex`。

### Todo 3-2：檢查 lambda 內是否有 `var e = ...`

1. 搜尋：

   ```text
   var e =
   ```

2. 若這一行出現在 `(sender, e)` 的委派 / lambda 內，立刻改名為：

   ```csharp
   var ex = ...
   ```

   或者刪除該變數（如果只需要顯示訊息，不需要保留 Exception 物件）。

### Todo 3-3：確保沒有在委派內使用 `sender` 當作其它變數名稱

1. 搜尋整檔：

   ```text
   var sender =
   ```

2. 若該行位於 `delegate (object sender, ...)` 或 `(..., ..., ...) =>` scope 內，改名成其他，例如 `srcSender`、`obj` 等。

> 原則：**在同一個 `{ ... }` 區塊，`sender` / `e` / `rankStudents` 這些名稱都不能被「重新宣告」成新的變數或參數。**

---

## Todo 4：編譯檢查與最小驗證

1. 完成上述命名修正後，重新編譯專案。  
2. 確認下列錯誤訊息消失：

   - `A local or parameter named 'sender' cannot be declared in this scope...`
   - `A local or parameter named 'e' cannot be declared in this scope...`
   - `A local or parameter named 'rankStudents' cannot be declared in this scope...`

3. 執行實際操作：  
   - 由 Step1 進入 Step2，載入學生清單。  
   - 在 Step2 按「計算」，確定畫面沒有立即拋出例外、背景執行能正常跑完（或至少能看到現在的其他錯誤訊息）。

> 這份 Todo 只處理「命名衝突」的編譯錯誤，若後續還有邏輯 / SQL 例外，再另外整理。

---

## Cursor-RUN 建議順序

1. 打開 `CalculateTechnologyAssessmentRankStep2_111.cs`。
2. 依 **Todo 1** 改寫 `bkw.DoWork`、`bkw.RunWorkerCompleted` 的委派參數名稱為 `bwSender` / `bwE` / `bwECompleted`（或類似命名），並同步修改內部對 `e` 的存取。
3. 依 **Todo 2**，在 DoWork 內將 `var rankStudents = ...` 改為 `var workerStudents = ...`，並同步修改使用處。
4. 依 **Todo 3**，全檔搜尋修正 `catch (Exception e)`、`var e =`、`var sender =` 等可能衝突命名。
5. 重新編譯，確保所有命名衝突錯誤已清除。
