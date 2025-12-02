# Todo-方案B_Step1效能優化_技職繁星成績排名計算1202.md

## 🎯 目標
優化 `CalculateTechnologyAssessmentRankStep1_111.cs` 效能。  
將「整理學生類別」的邏輯，從 UI Thread (`RunWorkerCompleted`) 移到 Background Thread (`DoWork`)，  
避免 UI 卡頓與多次 `StudentTag.SelectAll()` 造成效能極差。

修改完成後請記錄於：  
`技職繁星成績排名計算調整1202.md`

---

## ✅ Todo 1：在 Step1NextResult 擴充欄位（加入 Tag1/Tag2 結果）

**檔案：** `CalculateTechnologyAssessmentRankStep1_111.cs`

1. 搜尋：

   ```csharp
   private class Step1NextResult
   ```

2. 修改為（新增最後兩個欄位）：

   ```csharp
   private class Step1NextResult
   {
       public Dictionary<string, udtRegistrationDept> DeptDict { get; set; }
       public Dictionary<string, List<udtRegistrationSubjectNew>> SubjDict { get; set; }
       public Dictionary<string, string> DeptNameDict { get; set; }

       // 新增：把 Tag 過濾的 StudentTagRecord 也放進 Result
       public List<StudentTagRecord> Tag1Student { get; set; }
       public List<StudentTagRecord> Tag2Student { get; set; }
   }
   ```

---

## ✅ Todo 2：移除 RunWorkerCompleted 中舊的「整理學生類別」邏輯

**檔案：** `CalculateTechnologyAssessmentRankStep1_111.cs`

1. 在 `_bgWorkerStep1Next_RunWorkerCompleted` 方法中，找到區塊：

   ```csharp
   #region 整理學生類別
   List<StudentTagRecord> tag1Student = new List<StudentTagRecord>();
   List<StudentTagRecord> tag2Student = new List<StudentTagRecord>();
   ...
   #endregion
   ```

2. 將整段 **註解或刪除**，之後改用 DoWork 裡算好的結果。

---

## ✅ Todo 3：在 DoWork 中新增「整理學生類別（背景執行）」邏輯

**檔案：** `CalculateTechnologyAssessmentRankStep1_111.cs`  
**方法：** `_bgWorkerStep1Next_DoWork`

1. 在 `e.Result = new Step1NextResult { ... }` 之前，插入以下程式碼：

   ```csharp
   // ===================== 整理學生類別（背景執行） =====================
   List<StudentTagRecord> tag1Student = new List<StudentTagRecord>();
   List<StudentTagRecord> tag2Student = new List<StudentTagRecord>();

   // 一次抓全部 StudentTag
   List<StudentTagRecord> allStudentTags = StudentTag.SelectAll();
   var tagIdToStudentTags = allStudentTags.ToLookup(t => t.RefTagID);

   // 共用方法：依 Prefix/Name 找 TagID
   Func<string, List<string>> getTagIdsByPrefixOrName = (tagName) =>
   {
       if (string.IsNullOrEmpty(tagName))
           return new List<string>();

       var ids = _TagList
           .Where(x => x.Prefix == tagName)
           .Select(x => x.ID)
           .ToList();

       if (ids.Count == 0)
       {
           ids = _TagList
               .Where(x => x.Name == tagName)
               .Select(x => x.ID)
               .ToList();
       }

       return ids;
   };

   if (!string.IsNullOrEmpty(studentTag1))
   {
       var tag1IDs = new HashSet<string>(getTagIdsByPrefixOrName(studentTag1));
       tag1Student = tag1IDs
           .SelectMany(id => tagIdToStudentTags[id])
           .ToList();
   }

   if (!string.IsNullOrEmpty(studentTag2))
   {
       var tag2IDs = new HashSet<string>(getTagIdsByPrefixOrName(studentTag2));
       tag2Student = tag2IDs
           .SelectMany(id => tagIdToStudentTags[id])
           .ToList();
   }
   // ===================== 整理學生類別（結束） =====================
   ```

---

## ✅ Todo 4：DoWork 的 e.Result 帶上 Tag1 / Tag2

1. 原本的程式碼：

   ```csharp
   e.Result = new Step1NextResult
   {
       DeptDict = tmpDeptDict,
       SubjDict = tmpSubjDict,
       DeptNameDict = StudentDeptNameDict
   };
   ```

2. 修改為：

   ```csharp
   e.Result = new Step1NextResult
   {
       DeptDict = tmpDeptDict,
       SubjDict = tmpSubjDict,
       DeptNameDict = StudentDeptNameDict,

       // 新增這兩個回傳值
       Tag1Student = tag1Student,
       Tag2Student = tag2Student
   };
   ```

---

## ✅ Todo 5：RunWorkerCompleted 使用 DoWork 結果，不再呼叫 SelectAll()

**檔案：** `CalculateTechnologyAssessmentRankStep1_111.cs`  
**方法：** `_bgWorkerStep1Next_RunWorkerCompleted`

1. 在取得 `Step1NextResult result = e.Result as Step1NextResult;` 後，加入：

   ```csharp
   List<StudentTagRecord> tag1Student = result.Tag1Student ?? new List<StudentTagRecord>();
   List<StudentTagRecord> tag2Student = result.Tag2Student ?? new List<StudentTagRecord>();
   ```

2. 確認呼叫 Step2 的地方使用這兩個變數：

   ```csharp
   ctrr2.SetStudentDataList(
       _StudentFilterList,
       tag1Student,
       tag2Student,
       result.DeptNameDict,
       scoreErrIDList,
       _StudentList,
       studentFilterIDList
   );
   ```

3. 確保 RunWorkerCompleted 中 **不再有** `StudentTag.SelectAll()`。

---

## ✅ Todo 6：基本測試建議（給自己在本機驗證）

1. 在修改前先備份檔案或建立 Git branch。
2. 使用相同學生資料與相同條件（同一批 studentFilter / studentTag1 / studentTag2），分別執行：  
   - 改版前程式  
   - 改版後程式  
3. 在 `SetStudentDataList` 內，暫時加入 debug log 或 MessageBox（建議 log 到檔案），比對：  
   - tag1Student 所對應學生 ID 集合是否相同  
   - tag2Student 所對應學生 ID 集合是否相同

若兩邊結果一致，即代表邏輯未變，只是效能提升。

---

## ✅ Todo 7：完成後撰寫調整紀錄

**檔案：** `技職繁星成績排名計算調整1202.md`

建議新增內容：

```markdown
# 技職繁星成績排名計算調整 1202

## 2025/12/02 Step1 效能優化（方案 B）

1. 將整理學生類別（StudentTag 分類）邏輯從 RunWorkerCompleted 移到 DoWork 背景執行。
2. StudentTag.SelectAll() 從原本最多 3 次減為 1 次，避免重複撈取大量 Tag 資料。
3. 使用 Lookup + HashSet 取代 LINQ Contains 多重迭代，提高查詢效能。
4. RunWorkerCompleted 不再阻塞 UI Thread，Step2 視窗顯示速度大幅改善。
5. 整體效能依學生數不同，預估提升約 40%~70%。
```

---

## 備註

- 方案 B 僅調整「執行位置與效能」，不變更原本 Tag 過濾邏輯。
- `studentTag1` / `studentTag2` 與 `_TagList` 的使用方式與舊版相同。

