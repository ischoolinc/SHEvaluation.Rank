using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace SHEvaluation.Rank.MotherGroup
{
    public sealed class SemesterRankPublishStatement
    {
        public string Sql { get; private set; }
        public bool InjectsFailure { get; private set; }

        public bool DatabaseExecuted
        {
            get { return false; }
        }

        internal SemesterRankPublishStatement(string sql, bool injectsFailure)
        {
            Sql = sql;
            InjectsFailure = injectsFailure;
        }
    }

    public static class SemesterRankPublishSql
    {
        public static bool DatabaseExecuted
        {
            get { return false; }
        }

        public static SemesterRankPublishStatement Build(IList<SemesterRankGradeStudents> grades, string schoolYear, string semester, string calculationSetting, string formulaFromSubjectScore, bool injectFailure, bool legacyStudentSelects = false)
        {
            return Build(grades, schoolYear, semester, calculationSetting, null, formulaFromSubjectScore, injectFailure, legacyStudentSelects);
        }

        public static SemesterRankPublishStatement Build(IList<SemesterRankGradeStudents> grades, string schoolYear, string semester, string calculationSetting, string extensionJson, string formulaFromSubjectScore, bool injectFailure, bool legacyStudentSelects = false)
        {
            int yearNumber = RequirePeriod(schoolYear, "學年度");
            int semesterNumber = RequirePeriod(semester, "學期");
            List<SemesterRankGradeStudents> ordered = RequireGrades(grades);
            string formula = RequireFormula(formulaFromSubjectScore);
            if (formula.IndexOf("0 AS batch_id") >= 0)
                throw new InvalidOperationException("排名公式已帶批次識別 0，不建立公布語句。");

            formula = ReplaceExactly(
                formula,
                "CROSS JOIN calc_condition",
                "INNER JOIN calc_condition\n\t\t\t\tON score_list.rank_grade_year = calc_condition.rank_grade_year::INT",
                2,
                "排名公式應有兩處 CROSS JOIN calc_condition。");
            formula = ReplaceExactly(
                formula,
                "AND rank_matrix.ref_exam_id = calc_condition.ref_exam_id::INT",
                "AND rank_matrix.ref_exam_id = calc_condition.ref_exam_id::INT\n\t\tAND rank_matrix.item_type LIKE '學期/%'",
                1,
                "停用條件缺少 ref_exam_id，無法只限縮學期成績。");
            formula = ReplaceStudentInsert(formula);
            formula = RequireMatrixMatch(formula);
            formula = InsertBeforeClosing(
                formula,
                ", insert_detail_data AS",
                "SELECT count(*) FROM score_list",
                "\t\t\tINNER JOIN insert_batch_student_data\n\t\t\t\tON insert_batch_student_data.ref_student_id = score_list.student_id\n\t\t\t\tAND insert_batch_student_data.grade_year = score_list.rank_grade_year::INT\n\t\t\t\tAND insert_batch_student_data.matrix_class IS NOT DISTINCT FROM score_list.rank_class_name\n\t\t\t\tAND insert_batch_student_data.matrix_tag1 IS NOT DISTINCT FROM score_list.rank_tag1\n\t\t\t\tAND insert_batch_student_data.matrix_tag2 IS NOT DISTINCT FROM score_list.rank_tag2\n\tRETURNING *\n",
                "rank_detail");
            string finalSelect = ResultSelect(injectFailure);
            formula = ReplaceExactly(
                formula,
                "SELECT count(*) FROM score_list",
                finalSelect,
                1,
                "排名公式的結尾必須是 SELECT count(*) FROM score_list。");

            string sql = Header(ordered, yearNumber, semesterNumber, calculationSetting ?? "", extensionJson, legacyStudentSelects) + "\n" + formula;
            if (sql.IndexOf("CROSS JOIN calc_condition") >= 0 || sql.IndexOf("0 AS batch_id") >= 0 || sql.IndexOf(';') >= 0 || sql.IndexOf("1/0") >= 0)
                throw new InvalidOperationException("公布語句仍有跨年級配對、批次識別 0、多個敘述或常數除以零。");
            return new SemesterRankPublishStatement(sql, injectFailure);
        }

        public static bool HasPublishedDetails(DataTable result)
        {
            if (result == null || result.Rows.Count != 1 || result.Columns.Count == 0)
                return false;
            object value = result.Rows[0][0];
            if (value == null || value == DBNull.Value)
                return false;
            int count;
            if (!int.TryParse(Convert.ToString(value), out count))
                return false;
            return count > 0;
        }

        private static string ResultSelect(bool injectFailure)
        {
            string guard = "detail_count > 0 AND detail_count = score_count AND missing_matrix_count = 0 AND missing_student_count = 0 AND student_conflict_count = 0";
            if (injectFailure)
                guard += " AND detail_count = detail_count + 1";
            return "SELECT CASE\n\tWHEN " + guard + " THEN detail_count\n\tELSE ((" + FailureText() + ")::TEXT || 'x')::BIGINT\nEND AS published_count\nFROM (\n\tSELECT\n\t\t(SELECT count(*) FROM insert_detail_data) AS detail_count\n\t\t, (SELECT count(*) FROM score_list) AS score_count\n\t\t, (SELECT count(*) FROM score_list WHERE NOT EXISTS (\n\t\t\tSELECT 1 FROM insert_matrix_data\n\t\t\tWHERE insert_matrix_data.school_year = score_list.rank_school_year::INT\n\t\t\t\tAND insert_matrix_data.semester = score_list.rank_semester::INT\n\t\t\t\tAND insert_matrix_data.grade_year = score_list.rank_grade_year::INT\n\t\t\t\tAND insert_matrix_data.item_type = score_list.item_type\n\t\t\t\tAND insert_matrix_data.ref_exam_id = score_list.ref_exam_id\n\t\t\t\tAND insert_matrix_data.item_name = score_list.item_name\n\t\t\t\tAND insert_matrix_data.rank_type = score_list.rank_type\n\t\t\t\tAND insert_matrix_data.rank_name = score_list.rank_name\n\t\t)) AS missing_matrix_count\n\t\t, (SELECT count(*) FROM score_list WHERE NOT EXISTS (\n\t\t\tSELECT 1 FROM insert_batch_student_data\n\t\t\tWHERE insert_batch_student_data.ref_student_id = score_list.student_id\n\t\t\t\tAND insert_batch_student_data.grade_year = score_list.rank_grade_year::INT\n\t\t\t\tAND insert_batch_student_data.matrix_class IS NOT DISTINCT FROM score_list.rank_class_name\n\t\t\t\tAND insert_batch_student_data.matrix_tag1 IS NOT DISTINCT FROM score_list.rank_tag1\n\t\t\t\tAND insert_batch_student_data.matrix_tag2 IS NOT DISTINCT FROM score_list.rank_tag2\n\t\t)) AS missing_student_count\n\t\t, (SELECT count(*) FROM (\n\t\t\tSELECT score_list.student_id\n\t\t\tFROM score_list\n\t\t\tGROUP BY score_list.student_id\n\t\t\tHAVING count(DISTINCT coalesce(score_list.rank_grade_year::TEXT, '') || chr(1) || coalesce(score_list.rank_class_name, '') || chr(1) || coalesce(score_list.rank_tag1, '') || chr(1) || coalesce(score_list.rank_tag2, '')) > 1\n\t\t) AS student_conflict) AS student_conflict_count\n) AS publish_check";
        }

        private static string FailureText()
        {
            return "CASE\n"
                + "\t\tWHEN score_count = 0 THEN '沒有符合排名條件的學期成績，未執行排名公布。母群學生=' || (SELECT count(*) FROM student_list)::TEXT"
                + " || '，科目成績=' || (SELECT count(*) FROM subject_score)::TEXT"
                + " || '，分項成績=' || (SELECT count(*) FROM entry_score_list)::TEXT"
                + " || '，可排名科目=' || (SELECT count(*) FROM subj_origin_rank)::TEXT"
                + " || '，可排名分項=' || (SELECT count(*) FROM entry_rank_list)::TEXT"
                + " || '，五種排名皆不參與=' || (SELECT count(*) FROM student_list WHERE participate_year = false AND participate_dept = false AND participate_class = false AND participate_tag1 = false AND participate_tag2 = false)::TEXT\n"
                + "\t\tWHEN detail_count <> score_count THEN '排名明細筆數與成績筆數不一致 detail_count=' || detail_count::TEXT || ' score_count=' || score_count::TEXT\n"
                + "\t\tWHEN missing_matrix_count > 0 THEN '母群矩陣缺少對應列 missing_matrix_count=' || missing_matrix_count::TEXT\n"
                + "\t\tWHEN missing_student_count > 0 THEN '批次學生缺少對應列 missing_student_count=' || missing_student_count::TEXT\n"
                + "\t\tWHEN student_conflict_count > 0 THEN '同一學生有多個排名身分 student_conflict_count=' || student_conflict_count::TEXT\n"
                + "\t\tELSE '完整性檢查未通過'\n"
                + "\tEND";
        }

        private static string ReplaceStudentInsert(string sql)
        {
            int start = sql.IndexOf(", insert_batch_student_data AS");
            int end = sql.IndexOf(", insert_detail_data AS", start < 0 ? 0 : start);
            if (start < 0 || end <= start)
                throw new InvalidOperationException("rank_batch_student 寫入區段不完整。");
            string body = sql.Substring(start, end - start);
            int select = body.IndexOf("SELECT");
            int close = body.LastIndexOf(')');
            if (select < 0 || close < select || body.IndexOf("score_list.student_id") < 0)
                throw new InvalidOperationException("rank_batch_student 缺少學生欄位。");
            string query = "SELECT\n\t\t\tstudent_identity.ref_batch_id\n\t\t\t, student_identity.ref_student_id\n\t\t\t, student_identity.grade_year\n\t\t\t, student_identity.matrix_grade\n\t\t\t, student_identity.matrix_class\n\t\t\t, student_identity.matrix_tag1\n\t\t\t, student_identity.matrix_tag2\n\t\tFROM (\n\t\t\tSELECT\n\t\t\t\tcalc_condition.batch_id AS ref_batch_id\n\t\t\t\t, score_list.student_id AS ref_student_id\n\t\t\t\t, score_list.rank_grade_year::INT AS grade_year\n\t\t\t\t, score_list.rank_grade_year || '年級' AS matrix_grade\n\t\t\t\t, score_list.rank_class_name AS matrix_class\n\t\t\t\t, score_list.rank_tag1 AS matrix_tag1\n\t\t\t\t, score_list.rank_tag2 AS matrix_tag2\n\t\t\t\t, count(*) OVER (PARTITION BY calc_condition.batch_id, score_list.student_id) AS identity_rows\n\t\t\tFROM\n\t\t\t\tscore_list\n\t\t\t\tINNER JOIN calc_condition\n\t\t\t\t\tON score_list.rank_grade_year = calc_condition.rank_grade_year::INT\n\t\t\t\tLEFT OUTER JOIN insert_matrix_data\n\t\t\t\t\tON insert_matrix_data.id < 0\n\t\t\tGROUP BY\n\t\t\t\tcalc_condition.batch_id\n\t\t\t\t, score_list.student_id\n\t\t\t\t, score_list.rank_grade_year\n\t\t\t\t, score_list.rank_grade_year || '年級'\n\t\t\t\t, score_list.rank_class_name\n\t\t\t\t, score_list.rank_tag1\n\t\t\t\t, score_list.rank_tag2\n\t\t) AS student_identity\n\t\tWHERE\n\t\t\tstudent_identity.identity_rows = 1\n\tRETURNING *\n";
            body = body.Substring(0, select) + query + body.Substring(close);
            return sql.Substring(0, start) + body + sql.Substring(end);
        }

        private static string RequireMatrixMatch(string sql)
        {
            int start = sql.IndexOf(", insert_detail_data AS");
            int end = sql.IndexOf("SELECT count(*) FROM score_list", start < 0 ? 0 : start);
            if (start < 0 || end <= start)
                throw new InvalidOperationException("rank_detail 寫入區段不完整。");
            string body = sql.Substring(start, end - start);
            const string left = "LEFT OUTER JOIN";
            int at = body.IndexOf(left);
            int matrix = at < 0 ? -1 : body.IndexOf("insert_matrix_data", at);
            if (at < 0 || matrix < 0 || body.IndexOf("insert_matrix_data.id AS ref_matrix_id") < 0)
                throw new InvalidOperationException("明細寫入缺少矩陣連接，無法取得 ref_matrix_id。");
            body = body.Substring(0, at) + "INNER JOIN" + body.Substring(at + left.Length);
            return sql.Substring(0, start) + body + sql.Substring(end);
        }

        private static void AppendStudentList(StringBuilder sql, List<SemesterRankGradeStudents> grades, bool legacyStudentSelects)
        {
            string json = SemesterRankStudentJson.TryCompose(grades);
            if (json != null)
            {
                sql.Append("WITH student_list AS\n(\n");
                sql.Append("\tSELECT\n");
                sql.Append("\t\ts.student_id\n");
                sql.Append("\t\t, s.student_name\n");
                sql.Append("\t\t, s.rank_grade_year\n");
                sql.Append("\t\t, s.rank_dept_name\n");
                sql.Append("\t\t, s.rank_class_name\n");
                sql.Append("\t\t, s.rank_tag1\n");
                sql.Append("\t\t, s.rank_tag2\n");
                sql.Append("\t\t, s.participate_year\n");
                sql.Append("\t\t, s.participate_dept\n");
                sql.Append("\t\t, s.participate_class\n");
                sql.Append("\t\t, s.participate_tag1\n");
                sql.Append("\t\t, s.participate_tag2\n");
                sql.Append("\tFROM jsonb_to_recordset('");
                sql.Append(Literal(json));
                sql.Append("'::jsonb) AS s (\n");
                sql.Append("\t\tstudent_id BIGINT\n");
                sql.Append("\t\t, student_name TEXT\n");
                sql.Append("\t\t, rank_grade_year INT\n");
                sql.Append("\t\t, rank_dept_name TEXT\n");
                sql.Append("\t\t, rank_class_name TEXT\n");
                sql.Append("\t\t, rank_tag1 TEXT\n");
                sql.Append("\t\t, rank_tag2 TEXT\n");
                sql.Append("\t\t, participate_year BOOLEAN\n");
                sql.Append("\t\t, participate_dept BOOLEAN\n");
                sql.Append("\t\t, participate_class BOOLEAN\n");
                sql.Append("\t\t, participate_tag1 BOOLEAN\n");
                sql.Append("\t\t, participate_tag2 BOOLEAN\n");
                sql.Append("\t)\n)\n");
                return;
            }
            if (!legacyStudentSelects)
                throw new InvalidOperationException("正式排名必須使用 JSONB 學生清單，不建立每位學生的 UNION ALL。");

            sql.Append("WITH student_list AS\n(\n");
            bool firstStudent = true;
            foreach (SemesterRankGradeStudents grade in grades)
            {
                if (grade.Selects == null)
                    continue;
                foreach (string select in grade.Selects)
                {
                    if (!firstStudent)
                        sql.Append("\nUNION ALL\n");
                    sql.Append(select.Trim());
                    firstStudent = false;
                }
            }
            sql.Append("\n)\n");
        }

        private static string Header(List<SemesterRankGradeStudents> grades, int schoolYear, int semester, string calculationSetting, string extensionJson, bool legacyStudentSelects)
        {
            StringBuilder sql = new StringBuilder();
            AppendStudentList(sql, grades, legacyStudentSelects);
            sql.Append(", insert_batch_data AS\n(\n");
            sql.Append("\tINSERT INTO rank_batch\n");
            sql.Append("\t(\n\t\tschool_year\n\t\t, semester\n\t\t, calculation_description\n\t\t, setting\n");
            if (extensionJson != null)
                sql.Append("\t\t, extension\n");
            sql.Append("\t)\n");
            sql.Append("\tSELECT\n\t\t'");
            sql.Append(schoolYear);
            sql.Append("'::INT\n\t\t, '");
            sql.Append(semester);
            sql.Append("'::INT\n\t\t, '");
            sql.Append(schoolYear);
            sql.Append("' || ' ' || '");
            sql.Append(semester);
            sql.Append("' || ' 計算學期成績排名'\n\t\t, '");
            sql.Append(Literal(calculationSetting));
            sql.Append("'::TEXT\n");
            if (extensionJson != null)
            {
                sql.Append("\t\t, '");
                sql.Append(Literal(extensionJson));
                sql.Append("'::jsonb\n");
            }
            sql.Append("\tRETURNING *\n)\n, calc_condition AS\n(\n");
            for (int i = 0; i < grades.Count; i++)
            {
                if (i > 0)
                    sql.Append("\n\tUNION ALL\n");
                sql.Append("\tSELECT\n\t\t'");
                sql.Append(grades[i].GradeYear);
                sql.Append("'::TEXT AS rank_grade_year\n\t\t, '");
                sql.Append(schoolYear);
                sql.Append("'::TEXT AS rank_school_year\n\t\t, '");
                sql.Append(semester);
                sql.Append("'::TEXT AS rank_semester\n\t\t, '-1'::TEXT AS ref_exam_id\n\t\t, '學期成績'::TEXT AS rank_exam_name\n\t\t, '");
                sql.Append(Literal(calculationSetting));
                sql.Append("'::TEXT AS calculation_setting\n\t\t, insert_batch_data.id AS batch_id\n\tFROM\n\t\tinsert_batch_data");
            }
            sql.Append("\n)");
            return sql.ToString();
        }

        private static List<SemesterRankGradeStudents> RequireGrades(IList<SemesterRankGradeStudents> grades)
        {
            if (grades == null || grades.Count == 0)
                throw new InvalidOperationException("沒有勾選年級，不建立公布語句，也不使用批次識別 0。");
            List<SemesterRankGradeStudents> ordered = new List<SemesterRankGradeStudents>();
            Dictionary<int, bool> seen = new Dictionary<int, bool>();
            foreach (SemesterRankGradeStudents grade in grades)
            {
                if (grade == null)
                    throw new InvalidOperationException("勾選年級是空白，不建立公布語句。");
                if (grade.GradeYear <= 0)
                    throw new InvalidOperationException("年級不是正整數，不建立公布語句。");
                if (seen.ContainsKey(grade.GradeYear))
                    throw new InvalidOperationException("勾選年級重複，不建立公布語句。");
                bool hasStudents = grade.Students != null && grade.Students.Count > 0;
                bool hasSelects = grade.Selects != null && grade.Selects.Count > 0;
                if (!hasStudents && !hasSelects)
                    throw new InvalidOperationException("年級 " + grade.GradeYear + " 沒有學生，不建立公布語句。");
                if (hasSelects)
                {
                    foreach (string select in grade.Selects)
                    {
                        if (select == null || select.Trim().Length == 0)
                            throw new InvalidOperationException("年級 " + grade.GradeYear + " 有空白學生列，不建立公布語句。");
                    }
                }
                seen.Add(grade.GradeYear, true);
                ordered.Add(grade);
            }
            return ordered;
        }

        private static int RequirePeriod(string value, string name)
        {
            int parsed;
            if (value == null || !int.TryParse(value.Trim(), out parsed))
                throw new InvalidOperationException(name + "不是整數，不建立公布語句。");
            return parsed;
        }

        private static string RequireFormula(string formulaFromSubjectScore)
        {
            if (formulaFromSubjectScore == null)
                throw new ArgumentNullException("formulaFromSubjectScore");
            string formula = formulaFromSubjectScore.Replace("\r\n", "\n").Replace("\r", "\n").Trim();
            if (!formula.StartsWith(", subject_score AS"))
                throw new InvalidOperationException("排名公式必須從 subject_score 開始，學生清單與計算條件由公布語句產生。");
            if (formula.IndexOf(", update_data AS") < 0 || formula.IndexOf(", insert_matrix_data AS") < 0 || formula.IndexOf(", insert_batch_student_data AS") < 0 || formula.IndexOf(", insert_detail_data AS") < 0)
                throw new InvalidOperationException("排名公式缺少停用、母群、學生或明細寫入。");
            return formula;
        }

        private static string InsertBeforeClosing(string sql, string startMark, string endMark, string insertion, string name)
        {
            int start = sql.IndexOf(startMark);
            int end = sql.IndexOf(endMark, start < 0 ? 0 : start + startMark.Length);
            if (start < 0 || end <= start)
                throw new InvalidOperationException(name + " 寫入區段不完整。");
            string body = sql.Substring(start, end - start);
            if (body.IndexOf("RETURNING") >= 0)
                throw new InvalidOperationException(name + " 已經有 RETURNING，不重複加入。");
            int close = body.LastIndexOf(')');
            if (close < 0)
                throw new InvalidOperationException(name + " 缺少結尾。");
            return sql.Substring(0, start) + body.Substring(0, close) + insertion + body.Substring(close) + sql.Substring(end);
        }

        private static string ReplaceExactly(string text, string token, string replacement, int expected, string message)
        {
            if (Count(text, token) != expected)
                throw new InvalidOperationException(message);
            return text.Replace(token, replacement);
        }

        private static int Count(string text, string token)
        {
            int count = 0;
            int at = 0;
            while (at >= 0 && at < text.Length)
            {
                at = text.IndexOf(token, at);
                if (at < 0)
                    break;
                count++;
                at += token.Length;
            }
            return count;
        }

        private static string Literal(string value)
        {
            return (value ?? "").Replace("'", "''");
        }
    }

    public sealed class SemesterRankPublishOutcome
    {
        public bool Succeeded { get; private set; }
        public string DialogMessage { get; private set; }
        public string StatusMessage { get; private set; }

        private SemesterRankPublishOutcome(bool succeeded, string dialogMessage, string statusMessage)
        {
            Succeeded = succeeded;
            DialogMessage = dialogMessage;
            StatusMessage = statusMessage;
        }

        public static SemesterRankPublishOutcome From(Exception error, DataTable result)
        {
            if (error == null && SemesterRankPublishSql.HasPublishedDetails(result))
                return new SemesterRankPublishOutcome(true, "計算完成", "排名計算完成");
            string detail = error == null ? "排名明細沒有寫入。" : error.Message;
            if (detail != null && detail.IndexOf("沒有符合排名條件的學期成績，未執行排名公布") >= 0)
                detail = "沒有成績";
            return new SemesterRankPublishOutcome(false, "計算排名失敗。\n" + detail, "排名計算失敗");
        }
    }
}
