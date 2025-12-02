# Todo-技職繁星排名_Step2_XML展開重構專用_1202.md

## 🎯 目標

1. 重構 `CalculateTechnologyAssessmentRankStep2_111.cs` 內 Step2 的 SQL：  
   - 針對 `subject_score_data`、`sems_entry_Score_data` 兩段 XML 展開（`sems_subj_score` / `sems_entry_score`）做效能優化。
2. 將目前「先對整張表 XML 展開再 JOIN 學生 / 學期」的寫法，改成：  
   - **先 JOIN `student_list` + `student_sems` 篩出目標學生與五學期**
   - 再對這些限定資料做 `xmlparse + xpath + unnest`。
3. 保持原本資料結構與計算邏輯完全不變，只改變執行策略，降低 timeout 機率。  
4. 修改完成後，將此次重構紀錄寫入：`技職繁星成績排名計算調整1202.md`。

---

## ✅ Todo 1：備份與定位 SQL 位置

### 1-1. 備份檔案

- 檔案：`CalculateTechnologyAssessmentRankStep2_111.cs`
- 建議：
  - 建 Git branch：`feature/rankstep2-xml-opt-1202`
  - 或直接複製備份檔：`CalculateTechnologyAssessmentRankStep2_111_backup_1202.cs`

### 1-2. 定位 Step2 SQL 組裝區塊

- 在 C# 搜尋：`subject_score_data AS`、`sems_entry_Score_data AS`
- 確認兩段 SQL 目前大致長這樣（重點節錄）：

```sql
subject_score_data AS
(
    SELECT
         student_list.student_id
       , sems_subj_score_ext.school_year
       , sems_subj_score_ext.semester
       , sems_subj_score_ext,grade_year
       , student_list.student_name
       , ...
       , array_to_string(xpath('/root/Subject/@科目',
             xmlparse(content concat('<root>', subj_score_ele , '</root>'))), '')::TEXT As subject
       , NULLIF(array_to_string(xpath('/root/Subject/@原始成績',
             xmlparse(content concat('<root>', subj_score_ele , '</root>'))), ''),'')::DECIMAL As subject_origin_score
       , ...
    FROM 
    (
        SELECT 
            sems_subj_score.*
          , unnest(
                xpath('/root/SemesterSubjectScoreInfo/Subject',
                      xmlparse(content concat('<root>', score_info , '</root>'))
                )
            ) AS subj_score_ele
        FROM 
            sems_subj_score
    ) AS sems_subj_score_ext
    INNER JOIN student_list ...
    INNER JOIN student_sems ...
)
```

```sql
,sems_entry_Score_data AS
(
    SELECT
         student_list.student_id
       , sems_entry_score_ext.school_year
       , sems_entry_score_ext.semester
       , sems_entry_score_ext,grade_year
       , student_list.student_name
       , ...
       , '依學業原始'::TEXT AS subject
       , NULLIF(score,'')::DECIMAL AS subject_origin_score
       , '1'::DECIMAL AS subject_credit
       , ...
    FROM 
        (SELECT sems_entry_score.*
              , array_to_string(
                    xpath('//SemesterEntryScore/Entry[@分項=''學業(原始)'']/@成績',
                          xmlparse(content score_info)), ''
                )::TEXT AS score
         FROM sems_entry_score
         WHERE entry_group = 1
        ) AS sems_entry_score_ext
    INNER JOIN student_list ...
    INNER JOIN student_sems ...
)
```

> 🔴 目前兩段都是：**先對整張表 XML + xpath 展開，再 JOIN student_list / student_sems 篩。**  
> 這是 timeout 的第一主因。

---

## ✅ Todo 2：重構 subject_score_data（先 JOIN 再 XML 展開）

### 2-1. 修正明顯 typo（必要修補）

在原本 SQL 中，找到：

```sql
, sems_subj_score_ext,grade_year
```

改成：

```sql
, sems_subj_score_ext.grade_year
```

> 雖然這不是 timeout 來源，但這是 SQL 語法錯誤，必須先修。

### 2-2. 將內層「全表 XML 展開」改為「先 JOIN → 再 XML 展開」

**原本寫法概念：**

```sql
FROM 
(
    SELECT 
        sems_subj_score.*
      , unnest(
            xpath('/root/SemesterSubjectScoreInfo/Subject',
                  xmlparse(content concat('<root>', score_info , '</root>'))
            )
        ) AS subj_score_ele
    FROM 
        sems_subj_score
) AS sems_subj_score_ext
INNER JOIN student_list ...
INNER JOIN student_sems ...
```

**重構目標：**

改成先 JOIN `student_list` / `student_sems` 篩出目標學生與五學期，再對這些 row 展開 XML：

```sql
subject_score_data AS
(
    SELECT
         sl.student_id
       , s.school_year
       , s.semester
       , s.grade_year
       , sl.student_name
       , sl.rank_grade_year
       , sl.rank_dept_name
       , sl.rank_class_name
       , sl.rank_tag1
       , sl.rank_tag2
       , sl.rank_group_name
       , sl.rank_group_code
       , array_to_string(xpath('/root/Subject/@科目',
             xmlparse(content concat('<root>', subj_score_ele , '</root>'))), '')::TEXT AS subject
       , NULLIF(array_to_string(xpath('/root/Subject/@原始成績',
             xmlparse(content concat('<root>', subj_score_ele , '</root>'))), ''),'')::DECIMAL AS subject_origin_score
       , NULLIF(array_to_string(xpath('/root/Subject/@開課學分數',
             xmlparse(content concat('<root>', subj_score_ele , '</root>'))), ''),'')::DECIMAL AS subject_credit
       , NULLIF(array_to_string(xpath('/root/Subject/@開課分項類別',
             xmlparse(content concat('<root>', subj_score_ele , '</root>'))), ''),'')::TEXT AS subject_entry
       , NULLIF(array_to_string(xpath('/root/Subject/@修課必選修',
             xmlparse(content concat('<root>', subj_score_ele , '</root>'))), ''),'')::TEXT AS subject_required
       , NULLIF(array_to_string(xpath('/root/Subject/@修課校部訂',
             xmlparse(content concat('<root>', subj_score_ele , '</root>'))), ''),'')::TEXT AS subject_requiredby
       , NULLIF(array_to_string(xpath('/root/Subject/@不計學分',
             xmlparse(content concat('<root>', subj_score_ele , '</root>'))), ''),'')::TEXT AS ns1
       , NULLIF(array_to_string(xpath('/root/Subject/@不需評分',
             xmlparse(content concat('<root>', subj_score_ele , '</root>'))), ''),'')::TEXT AS ns2
    FROM
        sems_subj_score s
        INNER JOIN student_list sl
            ON s.ref_student_id = sl.student_id
        INNER JOIN student_sems ss
            ON s.ref_student_id = ss.student_id::INT
           AND s.school_year    = ss.sems_school_year::INT
           AND s.semester       = ss.sems_semester::INT
           AND s.grade_year     = ss.sems_grade_year::INT
        CROSS JOIN LATERAL
            unnest(
                xpath(
                    '/root/SemesterSubjectScoreInfo/Subject',
                    xmlparse(content concat('<root>', s.score_info , '</root>'))
                )
            ) AS subj_score_ele
)
```

**重點差異：**

1. `FROM sems_subj_score s` 先 JOIN `student_list` / `student_sems`，限制在：  
   - Step1 選到的學生（`workerStudents`）  
   - Step1 選到的五學期（`student_sems`）  
2. `CROSS JOIN LATERAL unnest(xpath(...))` 只對這些目標 rows 展開 XML。  
3. 所有原本 `SELECT` 欄位名稱與型別保持一致。

> 🔧 C# 端做法：用 `StringBuilder` 將原本 `subject_score_data` 區塊替換成上述新版本，確保整體 SQL 結構（CTE 名稱、欄位）不變。

---

## ✅ Todo 3：重構 sems_entry_Score_data（同樣先 JOIN 再 XML）

### 3-1. 修正 typo（必要修補）

找到原本：

```sql
,sems_entry_score_ext,grade_year
```

改為：

```sql
,sems_entry_score_ext.grade_year
```

### 3-2. 將 XML 展開改為先 JOIN 再解析

**原本寫法概念：**

```sql
,sems_entry_Score_data AS
(
    SELECT
         student_list.student_id
       , sems_entry_score_ext.school_year
       , sems_entry_score_ext.semester
       , sems_entry_score_ext,grade_year
       , student_list.student_name
       , ...
       , '依學業原始'::TEXT AS subject
       , NULLIF(score,'')::DECIMAL AS subject_origin_score
       , '1'::DECIMAL AS subject_credit
       , ...
    FROM 
        (SELECT sems_entry_score.*
              , array_to_string(
                    xpath('//SemesterEntryScore/Entry[@分項=''學業(原始)'']/@成績',
                          xmlparse(content score_info)), ''
                )::TEXT AS score
         FROM sems_entry_score
         WHERE entry_group = 1
        ) AS sems_entry_score_ext
    INNER JOIN student_list ...
    INNER JOIN student_sems ...
)
```

**重構目標寫法：**

```sql
,sems_entry_Score_data AS
(
    SELECT
         sl.student_id
       , s.school_year
       , s.semester
       , s.grade_year
       , sl.student_name
       , sl.rank_grade_year
       , sl.rank_dept_name
       , sl.rank_class_name
       , sl.rank_tag1
       , sl.rank_tag2
       , sl.rank_group_name
       , sl.rank_group_code
       , '依學業原始'::TEXT AS subject
       , NULLIF(x.score_text,'')::DECIMAL AS subject_origin_score
       , '1'::DECIMAL AS subject_credit
       , '學業(原始)'::TEXT AS subject_entry
       , NULL::TEXT AS subject_required
       , NULL::TEXT AS subject_requiredby
       , '否'::TEXT AS ns1
       , '否'::TEXT AS ns2
    FROM
        sems_entry_score s
        INNER JOIN student_list sl
            ON s.ref_student_id = sl.student_id
        INNER JOIN student_sems ss
            ON s.ref_student_id = ss.student_id::INT
           AND s.school_year    = ss.sems_school_year::INT
           AND s.semester       = ss.sems_semester::INT
           AND s.grade_year     = ss.sems_grade_year::INT
        CROSS JOIN LATERAL
        (
            SELECT array_to_string(
                       xpath('//SemesterEntryScore/Entry[@分項=''學業(原始)'']/@成績',
                             xmlparse(content s.score_info)), ''
                   )::TEXT AS score_text
        ) AS x
    WHERE
        s.entry_group = 1
)
```

**重點：**

1. 同樣「先 JOIN student_list + student_sems」再做 XML 解析。  
2. `score_text` 透過 `CROSS JOIN LATERAL` 取得，**只對目標 rows 解析 XML**。  
3. 跟原本一樣，只產出 **學業(原始)** 的 entry，不改業務邏輯。  
4. 最終輸出欄位名稱與型別要與原本保持一致。

---

## ✅ Todo 4：確認 subject_score CTE 與後續邏輯不變

`subject_score` CTE 目前長相大致如下（請依實際程式對照）：

```sql
,subject_score AS
(
    SELECT * FROM subject_score_data WHERE ns1 = '否' AND ns2 = '否' 
    UNION ALL
    SELECT * FROM sems_entry_Score_data
)
```

### 檢查重點：

1. 確認 `subject_score_data` 新版輸出欄位順序與舊版完全一致：  
   - student_id, school_year, semester, grade_year, student_name, rank_grade_year, rank_dept_name, rank_class_name, rank_tag1, rank_tag2, rank_group_name, rank_group_code, subject, subject_origin_score, subject_credit, subject_entry, subject_required, subject_requiredby, ns1, ns2
2. `sems_entry_Score_data` 亦輸出同樣欄位順序與型別。  
3. `subject_score` 仍為上述兩者 UNION ALL 結果，不加新條件（保持原始邏輯）。  
4. 後續所有 CTE：`subject_entry_score_base`、各種 bucket_*、*_avg_list、score_list 等，不需要任何欄位名稱調整。

---

## ✅ Todo 5：測試與驗證（避免邏輯跑掉 + 確認 timeout 缓解）

### 5-1. 準備測試資料與 baseline

1. 選擇一間實際會 timeout 的學校作為測試對象。  
2. 用「尚未重構（舊版）」的程式：
   - 執行 Step2 排名計算一次（如果一定會 timeout，可先找一間較小的學校作 baseline）。  
   - 匯出 / 查詢下列資料，保存為 baseline：  
     - `rank_batch` 最新一筆（同一個 `calc_name`）。  
     - 該 `batch` 的 `rank_matrix`、`rank_detail` 全部資料。  

> 若舊版必然 timeout 無法完成，可改找「資料量中等、可順利跑完」的學校做 baseline。

### 5-2. 套用重構後程式，重新執行 Step2

1. 將上述 XML 重構後的 `CalculateTechnologyAssessmentRankStep2_111.cs` 編譯部署到測試環境。  
2. 針對同一所學校、同一批學生、同一個 `calc_name`，重新執行 Step2。  
3. 觀察：
   - 是否仍然 timeout？（若不 timeout，記錄實際執行時間）  
   - 若資料有產生：抓取 `rank_batch`、`rank_matrix`、`rank_detail` 新版結果。

### 5-3. 比對舊版 vs 新版結果一致性

以 `student_id + rank_type + rank_name` 為 key，比對：

- `rank_matrix`：
  - score, score2, percentile, pr_value, alive 是否完全一致。  
- `rank_detail`：
  - seq、count、avg、pr 等是否一致。  

> 若有差異，優先檢查欄位對應、XML 解析是否漏掉某些學期或某些分項。

---

## ✅ Todo 6：記錄本次重構於技職繁星成績排名計算調整1202.md

在 `技職繁星成績排名計算調整1202.md` 新增一節：

```markdown
## 2025/12/02 Step2 XML 展開效能重構（subject_score_data / sems_entry_Score_data）

1. 原始問題：
   - Step2 排名計算在部分學校發生 DB timeout。
   - 分析 SQL 後發現 `subject_score_data`、`sems_entry_Score_data` 皆先對整張表 (`sems_subj_score` / `sems_entry_score`) 做 XML 展開，再 JOIN `student_list` / `student_sems` 篩選學生與五學期，導致 CPU 與 I/O 負荷極大。

2. 重構作法：
   - 將 `subject_score_data` 改為：先 `JOIN student_list` + `JOIN student_sems` 限縮資料範圍，再使用 `CROSS JOIN LATERAL + xpath + unnest` 針對目標學生與五學期展開 XML。
   - 將 `sems_entry_Score_data` 同樣改寫為先 JOIN 後 XML 解析，僅對需要的學生與學期解析「學業(原始)」分項。
   - 修正 SQL typo：`sems_subj_score_ext,grade_year` / `sems_entry_score_ext,grade_year` 改為使用 `.`，避免潛在語法錯誤。

3. 結果：
   - 在測試學校中，重構後 Step2 執行時間由原本約（X 秒 / timeout）降至約（Y 秒，以實測為準）。
   - 使用同一批學生資料比較重構前後 `rank_matrix`、`rank_detail`，各學生之 `score`、`rank`、`percentile`、`pr_value` 皆完全一致，確認僅為效能優化，不影響計算邏輯。
```

---

## 備註

- 本 Todo 僅針對 **XML 展開順序** 做重構，保持：
  - 原本字段名稱與型別不變；
  - 原本成績計算與排名規則不變；
  - 原本寫入 `rank_*` 表的結構不變。
- 如後續仍有效能瓶頸，再進行第二階段優化：
  - 將多個 `*_bucket` CTE 合併為單一 `subject_score_bucket`，減少掃描 `subject_entry_score_base` 次數。
