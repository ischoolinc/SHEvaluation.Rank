# Todo-技職繁星排名_Step2_SQL效能重構_bucket_key_1202.md

## 🎯 目標

1. 修正 `CalculateTechnologyAssessmentRankStep2_111.cs` / `q01.sql` 中 **Step2 大型 SQL** 的效能問題。
2. 將目前對 `subject_score` 的「多個 CTE 重複掃描」改為：  
   - **一次掃描 subject_score** → 先做 base 資料集  
   - 使用 `CASE WHEN` / `bucket_key` 進行分類  
   - 再從這個集中結果計算各類成績（學業、專業及實習、技能、國英數等）。
3. 保持原本所有排名邏輯、分群條件、科目清單完全不變，只改善效能。  
4. 完成後，在 `技職繁星成績排名計算調整1202.md` 紀錄調整內容與影響。

---

## ✅ Todo 1：備份與定位

### 1-1. 備份檔案

- 檔案：`CalculateTechnologyAssessmentRankStep2_111.cs`
- 建議：
  - 建 Git branch：`feature/rankstep2-sql-opt-1202`
  - 或複製備份檔：`CalculateTechnologyAssessmentRankStep2_111_backup_1202.cs`

### 1-2. 定位組 SQL 的程式碼區塊

- 方法：搜尋 `bkw.DoWork +=` 或 `insertRankSql =`。
- 確認目前 SQL 組法為：

  ```csharp
  string insertRankSql = @"
  " + studentListSql + @"
  " + StudSemsSQL + @"
  ,parse_number AS (...)
  , subject_score_data AS (...)
  , subject_score AS (...)
  ";
  
  // 之後用 StringBuilder 拼接：
  insertRankSql +=
      sbSubjectEntrySore.ToString() + Strsubject_entry_score_avg_list
    + sbSubjectProSore.ToString() + Strsubject_score_pro_avg_list
    + sbSubjectSoreSkill.ToString() + Strsubject_score_SkillDomain_avg_list
    + sbSubjectChineseScore.ToString() + Strsubject_score_chinese_avg_list
    + sbSubjectEnglishScore.ToString() + Strsubject_score_english_avg_list
    + sbSubjectMathScore.ToString() + Strsubject_score_math_avg_list
    + sbScoreListSQL.ToString()
    + insertUpdateSQL;
  ```

- 這裡是效能問題主要來源。

---

## ✅ Todo 2：分析目前效能問題（給未來自己或團隊參考）

> **這步驟主要是紀錄，不一定要改程式。**

在 `技職繁星成績排名計算調整1202.md` 或程式註解中，簡短描述：

1. `subject_score` CTE 定義後，被以下多組 CTE 重複使用：
   - 學業 5 學期：`sbSubjectEntrySore` / `Strsubject_entry_score_avg_list`
   - 專業及實習：`sbSubjectProSore` / `Strsubject_score_pro_avg_list`
   - 技能領域：`sbSubjectSoreSkill` / `Strsubject_score_SkillDomain_avg_list`
   - 國文、英文、數學：`sbSubjectChineseScore`、`sbSubjectEnglishScore`、`sbSubjectMathScore`
2. 每一組 CTE 內都包含：

   ```sql
   FROM subject_score
   INNER JOIN student_list ...
   WHERE grade_year = ...
     AND semester = ...
     AND rank_group_name = ...
     AND subject IN (...)
   GROUP BY ...
   ```

3. 因為每個群別 × 每個年級/學期 × 每種成績類型，都各自掃一次 `subject_score`，導致：
   - `subject_score` 被掃描數十次甚至上百次。
   - Postgres 在規劃與執行此 SQL 時負擔極重。

---

## ✅ Todo 3：新增 subject_score 的「一次掃描 base CTE」

### 3-1. 在 SQL 中新增 base CTE

在 `insertRankSql` 中，`subject_score` CTE 之後，新增一個 base CTE（以「學業」為先導案例）：

```sql
, subject_entry_score_base AS
(
    SELECT
        s.student_id,
        sl.student_name,
        sl.rank_grade_year,
        sl.rank_dept_name,
        sl.rank_class_name,
        sl.rank_tag1,
        sl.rank_tag2,
        sl.rank_group_name,
        sl.rank_group_code,
        s.school_year,
        s.semester,
        s.grade_year,
        s.subject,
        s.subject_origin_score,
        s.subject_credit
    FROM subject_score s
    INNER JOIN student_list sl
        ON s.student_id = sl.student_id
)
```

> ⚠ 注意：這裡只做 **一次 join subject_score + student_list**，不加任何群別/年級/科目條件，讓這個 base CTE 成為後續各成績類型的統一來源。

### 3-2. C# 端實作

- 用 StringBuilder（例如 `sbSubjectEntryBase`）插入上述 CTE。
- 插入位置：在舊的 `subject_entry_score_avg_...` 相關 CTE 之前。

---

## ✅ Todo 4：將「學業」多 CTE 改為 bucket_key 分類（第一階段重構）

> 先以「學業」計算為 prototype，確認正確並有顯著效能改善，再套用到其他成績類型。

### 4-1. 找出原本「學業」相關的 CTE 組法

搜尋變數：

- `sbSubjectEntrySore`
- `Strsubject_entry_score_avg_list`

原本應該是在迴圈裡針對 `RegGroupSubjMapDic[REGGroupName]["學業"]` 及 `calSemesterList`，產生多個：

```sql
, subject_entry_score_avg_商管1a AS (... FROM subject_score ...)
, subject_entry_score_avg_商管1b AS (... FROM subject_score ...)
, subject_entry_score_avg_商管2a AS (... FROM subject_score ...)
...
```

### 4-2. 用 bucket_key 重構「學業 base CTE」

將原本「很多個 `subject_entry_score_avg_XXX`」的邏輯，改為在一個 CTE 中，用 `CASE WHEN` 將資料分類：

```sql
, subject_entry_score_bucket AS
(
    SELECT
        b.*,
        CASE
            -- 這裡使用原本的條件：群別 + 年級 + 學期 + 科目清單
            WHEN b.rank_group_name = '商業與管理群'
             AND b.grade_year = 1
             AND b.semester = 1
             AND b.subject IN (/* 原本 1a 用的學業科目清單 */)
                THEN '學業_商管_1a'
            WHEN b.rank_group_name = '商業與管理群'
             AND b.grade_year = 1
             AND b.semester = 2
             AND b.subject IN (/* 原本 1b 用的學業科目清單 */)
                THEN '學業_商管_1b'
            -- TODO: 依 RegGroupSubjMapDic，補齊其他群別與年級/學期對應的 CASE WHEN
            ELSE NULL
        END AS bucket_key
    FROM subject_entry_score_base b
)
```

### 4-3. 學業平均成績 CTE（取代多個 CTE）

新增一個聚合 CTE：

```sql
, subject_entry_score_avg_list AS
(
    SELECT
        student_id,
        student_name,
        rank_grade_year,
        rank_dept_name,
        rank_class_name,
        rank_tag1,
        rank_tag2,
        rank_group_name,
        rank_group_code,
        bucket_key,
        SUM(subject_origin_score * subject_credit) AS sum_score,
        SUM(subject_credit) AS sum_credit
    FROM subject_entry_score_bucket
    WHERE bucket_key IS NOT NULL
    GROUP BY
        student_id,
        student_name,
        rank_grade_year,
        rank_dept_name,
        rank_class_name,
        rank_tag1,
        rank_tag2,
        rank_group_name,
        rank_group_code,
        bucket_key
)
```

### 4-4. 移除舊的「多個 subject_entry_score_avg_*」 CTE 組法

- 刪除或註解原本在 C# 中動態產生的：

  ```csharp
  ,subject_entry_score_avg_商管1a AS (... FROM subject_score ...)
  ```

- 改為只產生上述 `subject_entry_score_bucket` / `subject_entry_score_avg_list` 兩個 CTE。

### 4-5. 保持 score_list 使用介面不變

原本使用「學業成績」的地方如果是：

```sql
FROM subject_entry_score_avg_expand
```

或類似方式，需確認：

1. `subject_entry_score_avg_expand` 所依賴的 CTE 改為 `subject_entry_score_avg_list`。
2. bucket_key 可在 expand 階段展開為不同欄位或 rank_type / rank_name，不改變最終輸出格式。

> 若需要，可在 `subject_entry_score_avg_list` 基礎上再建立一個 `subject_entry_score_avg_expand` CTE，用原本欄位名稱包裝，使後續 SQL 不用大改。

---

## ✅ Todo 5：將相同模式套用到「專業及實習 / 技能 / 國文 / 英文 / 數學」（第二階段重構）

在確認「學業」重構後計算結果正確且效能有顯著改善，再對以下部分重複同樣模式：

1. 專業及實習：使用 `sbSubjectProSore` / `Strsubject_score_pro_avg_list`
2. 技能領域：使用 `sbSubjectSoreSkill` / `Strsubject_score_SkillDomain_avg_list`
3. 國文：`sbSubjectChineseScore` / `Strsubject_score_chinese_avg_list`
4. 英文：`sbSubjectEnglishScore` / `Strsubject_score_english_avg_list`
5. 數學：`sbSubjectMathScore` / `Strsubject_score_math_avg_list`

重構方式與學業相同：

- 共同 base：`subject_score` → base CTE（可視需求拆成多個 base CTE 或共用同一個）
- CASE WHEN 分類：各自建立 `*_bucket` CTE，加上適當 bucket_key
- 聚合：各自建立 `*_avg_list` CTE，取代原本大量 CTE + UNION ALL 的寫法

---

## ✅ Todo 6：測試與驗證

1. 準備固定測試資料（同一學校、同一批學生）。
2. 在改寫前，執行原始版本 Step2：
   - 記錄執行時間（可在 C# 端用 `Stopwatch` 紀錄 bkw.DoWork 的 SQL 執行時間）。
   - 匯出最終 rank 結果（例如 rank_matrix / rank_detail），保存成檔案。
3. 在改寫後，執行新版本 Step2：
   - 再次記錄執行時間。
   - 匯出同一批學生的 rank 結果，與舊版逐筆比對：
     - 是否同樣的學生數
     - 每位學生的各類排名（名次、級距、百分比等）是否一致
4. 若結果完全一致且執行時間明顯縮短，即確認重構成功。

---

## ✅ Todo 7：撰寫調整紀錄

**檔案：** `技職繁星成績排名計算調整1202.md`

建議新增紀錄內容：

```markdown
# 技職繁星成績排名計算調整 1202

## 2025/12/02 Step2 SQL 效能重構（subject_score 掃描合併 / bucket_key）

1. 分析原始 Step2 SQL，發現 `subject_score` 被多個 CTE 重複掃描（依群別、年級、學期、成績類型各自建 CTE），導致效能極差。
2. 新增 `subject_entry_score_base` 等 base CTE，將 subject_score 與 student_list 的 JOIN 只執行一次。
3. 導入 `bucket_key` 概念，使用 CASE WHEN 依群別、年級、學期、科目清單進行分類，取代原本多個 `subject_entry_score_avg_*` CTE。
4. 以學業成績為 prototype 完成重構，並逐步套用到專業及實習、技能領域、國文、英文、數學等成績類型。
5. 重構前後，使用同一批學生資料比對最終 rank 結果，確認名次與成績完全一致，僅執行時間顯著改善（約 xx% ~ yy%，視資料量而定）。
```

---

## 備註

- 此次改動為 **效能重構**，目標是「減少 subject_score 的掃描次數」，**不改變原始業務邏輯與成績判斷規則**。
- 所有關於「學期採計、群別、科目清單、比序項目」的設定，仍由既有的 `RegGroupSubjMapDic`、`calSemesterList` 等結構控制。
- 若未來新增或修改群別/科目，只需更新 mapping 資料，不需要再新增大量 CTE。
