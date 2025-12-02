# Todo-技職繁星排名_Step2_SQL效能優化_使用VALUES取代UNION ALL_1202.md

## 🎯 目標
在 `CalculateTechnologyAssessmentRankStep2_111.cs` 中：  
將 SQL CTE `student_list`、`student_sems` 目前使用的 **多個 `SELECT ... UNION ALL`** 寫法，改寫為 **單一 `VALUES`** 寫法，以降低 SQL 解析成本、提升效能。  

修改完成後請記錄於：  
`技職繁星成績排名計算調整1202.md`。

---

## ✅ Todo 1：備份檔案

**檔案：** `CalculateTechnologyAssessmentRankStep2_111.cs`

1. 建議先在 Git 建一個 branch，例如：`feature/rankstep2-values-1202`  
2. 或複製一份備份檔：  
   `CalculateTechnologyAssessmentRankStep2_111_backup_1202.cs`

---

## ✅ Todo 2：將 student_list 的 UNION ALL 改成 VALUES

**檔案：** `CalculateTechnologyAssessmentRankStep2_111.cs`

### 2-1. 尋找現有 student_list 產生程式碼

搜尋關鍵字：`WITH student_list AS` 或 `student_list AS`。  
找到類似如下的結構（實際程式可能略有差異，請以實際內容為主）：

```csharp
List<string> studentSqlList = new List<string>();

foreach (RankStudentDto dto in workerStudents)
{
    string studentId = dto.StudentID;
    string studentName = dto.StudentName;
    string rankGradeYear = dto.RankGradeYearText.Trim('年', '級');
    string deptName = dto.DeptName;
    string className = dto.ClassName;
    string tag1 = dto.Tag1;
    string tag2 = dto.Tag2;
    string regGroupName = dto.RegGroupName;
    string regGroupCode = dto.RegGroupCode;

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

    studentSqlList.Add(studentSql);
}

string studentListSql = @"
WITH student_list AS
(
    " + string.Join(@"
    UNION ALL", studentSqlList) + @"
)
";
```

### 2-2. 改寫為 VALUES 版本

將上述整段改成使用 `VALUES` 的版本：

```csharp
// 產生 student_list 用的 VALUES 列表
List<string> studentValueRows = new List<string>();

foreach (RankStudentDto dto in workerStudents)
{
    string studentId = dto.StudentID;
    string studentName = dto.StudentName;
    string rankGradeYear = dto.RankGradeYearText.Trim('年', '級');
    string deptName = dto.DeptName;
    string className = dto.ClassName;
    string tag1 = dto.Tag1;
    string tag2 = dto.Tag2;
    string regGroupName = dto.RegGroupName;
    string regGroupCode = dto.RegGroupCode;

    // 一列 VALUES：() 裡面每個欄位都有型別 cast
    string row = @"
(" + studentId + @"::BIGINT
, " + SqlLiteral(studentName) + @"::TEXT
, " + rankGradeYear + @"::INT
, " + SqlLiteral(deptName) + @"::TEXT
, " + SqlLiteral(className) + @"::TEXT
, " + SqlLiteral(tag1) + @"::TEXT
, " + SqlLiteral(tag2) + @"::TEXT
, " + SqlLiteral(regGroupName) + @"::TEXT
, " + SqlLiteral(regGroupCode) + @"::TEXT
)";

    studentValueRows.Add(row);
}

// 用 VALUES 組成 student_list CTE，並指定欄位名稱
string studentListSql = @"
WITH student_list(
    student_id,
    student_name,
    rank_grade_year,
    rank_dept_name,
    rank_class_name,
    rank_tag1,
    rank_tag2,
    rank_group_name,
    rank_group_code
) AS
(
    VALUES
    " + string.Join(@",
    ", studentValueRows) + @"
)
";
```

### 2-3. 確認後續 SQL 使用 student_list 的欄位名稱一致

1. 確認後續 SQL 中對 `student_list` 的欄位存取仍然是：

   - `student_id`
   - `student_name`
   - `rank_grade_year`
   - `rank_dept_name`
   - `rank_class_name`
   - `rank_tag1`
   - `rank_tag2`
   - `rank_group_name`
   - `rank_group_code`

2. 若名稱完全相同，則不需要其他修改。

---

## ✅ Todo 3：將 student_sems 的 UNION ALL 改成 VALUES

**檔案：** `CalculateTechnologyAssessmentRankStep2_111.cs`

### 3-1. 尋找現有 student_sems 產生程式碼

搜尋關鍵字：`student_sems AS` 或 `StudSemsSQLList`。  
找到類似如下結構：

```csharp
List<string> StudSemsSQLList = new List<string>();

foreach (string sid in RankStudentIDList)
{
    if (StudGradeYearSemsDict.ContainsKey(sid))
    {
        foreach (GradeYearSemesterInfo gs in StudGradeYearSemsDict[sid])
        {
            if (gs.GradeYear == 3 && gs.Semester == 2)
                continue;

            string qry = @"
SELECT " + sid + @" ::BIGINT AS student_id
, " + gs.GradeYear + @" ::INT AS sems_grade_year
, " + gs.Semester + @" ::INT AS sems_semester 
, " + gs.SchoolYear + @" ::INT AS sems_school_year";

            StudSemsSQLList.Add(qry);
        }
    }
}

string StudSemsSQL = @"
, student_sems AS
(
    " + string.Join(@"
    UNION ALL", StudSemsSQLList) + @"
)";
```

### 3-2. 改寫為 VALUES 版本

將上述整段改成：

```csharp
// 產生 student_sems 用的 VALUES 列表
List<string> studSemsValueRows = new List<string>();

foreach (string sid in RankStudentIDList)
{
    if (StudGradeYearSemsDict.ContainsKey(sid))
    {
        foreach (GradeYearSemesterInfo gs in StudGradeYearSemsDict[sid])
        {
            // 取五學期，3 年級第 2 學期不取
            if (gs.GradeYear == 3 && gs.Semester == 2)
                continue;

            string row = @"
(" + sid + @"::BIGINT
, " + gs.GradeYear + @"::INT
, " + gs.Semester + @"::INT
, " + gs.SchoolYear + @"::INT
)";
            studSemsValueRows.Add(row);
        }
    }
}

string StudSemsSQL = @"
, student_sems(
    student_id,
    sems_grade_year,
    sems_semester,
    sems_school_year
) AS
(
    VALUES
    " + string.Join(@",
    ", studSemsValueRows) + @"
)";
```

### 3-3. 確認後續 SQL 使用 student_sems 欄位名稱一致

1. 確認後續 SQL 中對 `student_sems` 的欄位存取仍然是：

   - `student_id`
   - `sems_grade_year`
   - `sems_semester`
   - `sems_school_year`

2. 若名稱完全相同，則不需額外修改。

---

## ✅ Todo 4：避免 VALUES 空集合造成語法錯誤（選擇性強化）

**說明：**  
若 `workerStudents` 或 `StudGradeYearSemsDict` 為空，有可能組出：

```sql
VALUES
  -- 沒有 row
```

這在 SQL 中會造成語法錯誤。建議加保護：

### 4-1. student_list 保護（可選）

在建立 `studentListSql` 前檢查：

```csharp
if (studentValueRows.Count == 0)
{
    // 若沒有任何學生，就讓 student_list 產生一個空結果 CTE
    studentListSql = @"
WITH student_list(
    student_id,
    student_name,
    rank_grade_year,
    rank_dept_name,
    rank_class_name,
    rank_tag1,
    rank_tag2,
    rank_group_name,
    rank_group_code
) AS
(
    SELECT
        NULL::BIGINT AS student_id,
        NULL::TEXT AS student_name,
        NULL::INT AS rank_grade_year,
        NULL::TEXT AS rank_dept_name,
        NULL::TEXT AS rank_class_name,
        NULL::TEXT AS rank_tag1,
        NULL::TEXT AS rank_tag2,
        NULL::TEXT AS rank_group_name,
        NULL::TEXT AS rank_group_code
    WHERE 1 = 0
)
";
}
else
{
    studentListSql = @"
WITH student_list(
        student_id,
        student_name,
        rank_grade_year,
        rank_dept_name,
        rank_class_name,
        rank_tag1,
        rank_tag2,
        rank_group_name,
        rank_group_code
    ) AS
    (
        VALUES
        " + string.Join(@",
        ", studentValueRows) + @"
    )
";
}
```

### 4-2. student_sems 保護（可選）

同樣方式處理 `studSemsValueRows.Count == 0` 的情況。

---

## ✅ Todo 5：基本測試建議

1. 準備一組固定的測試資料（同一所學校、相同學生群組）。  
2. 在改寫前，把產生出來的 SQL（包含 `student_list` / `student_sems`）輸出到 log 檔或 Debug 視窗。  
3. 改寫後，再輸出一次 SQL；或直接在 DB 中跑：  
   - 比較 `student_list` 的內容是否相同（student_id / rank_grade_year / dept / class / tag 等）。  
   - 比較 `student_sems` 的內容是否相同（student_id / sems_grade_year / sems_semester / sems_school_year）。  
4. 確認 Step2 計算出來的：  
   - 排名人數  
   - 每位學生排名結果  
   與改寫前完全一致。

---

## ✅ Todo 6：完成後撰寫調整紀錄

**檔案：** `技職繁星成績排名計算調整1202.md`

建議新增內容：

```markdown
# 技職繁星成績排名計算調整 1202

## 2025/12/02 Step2 SQL 效能優化（student_list / student_sems 改用 VALUES）

1. 將 CalculateTechnologyAssessmentRankStep2_111.cs 中 CTE `student_list` 由多個 `SELECT ... UNION ALL` 改為使用 `VALUES` 建立資料列。
2. 將 CTE `student_sems` 由多個 `SELECT ... UNION ALL` 改為使用 `VALUES` 建立資料列。
3. 保留原有欄位名稱與型別（student_id、rank_grade_year、sems_grade_year 等），不影響後續 SQL 計算與排名結果。
4. 預期效能改善：降低 Postgres SQL 解析與規劃 `UNION ALL` 的負擔，對大量學生（上千人）計算時，整體 Step2 執行時間可明顯縮短。
5. 已以測試資料比對改寫前後 `student_list` / `student_sems` 的內容與最終排名結果，確認無差異。
```

---

## 備註

- 此次修正**僅調整 SQL 的資料建構方式**，不改變任何欄位值與後續邏輯。  
- 所有排名計算邏輯、過濾規則、群組方式維持不變。

