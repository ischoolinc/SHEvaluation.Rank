using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;

namespace SHEvaluation.Rank.MotherGroup
{
    public static class MotherGroupPhase4Task42Check
    {
        public static int Main(string[] args)
        {
            try
            {
                List<string> errors = new List<string>();
                string source = Read("CalculateSemesterAssessmentRank.cs", errors);
                string formula = Formula(source, errors);
                if (formula.Length > 0)
                {
                    errors.AddRange(CheckOneGrade(formula));
                    errors.AddRange(CheckManyGrades(formula));
                    errors.AddRange(CheckBlank(formula));
                    errors.AddRange(CheckDuplicateSubjects(formula));
                    errors.AddRange(CheckInjectedFailure(formula));
                    errors.AddRange(CheckOutcomes());
                }
                errors.AddRange(CheckForm(source));
                if (!SemesterRankWriteGate.CanWriteRankTables())
                    errors.Add("SemesterRankWriteGate 應允許寫入");
                if (SemesterRankPublishSql.DatabaseExecuted)
                    errors.Add("公布組譯不應標記已執行資料庫");
                string gate = Read("MotherGroup\\SemesterRankWriteGate.cs", errors);
                Expect(errors, gate.IndexOf("return true;") >= 0 && gate.IndexOf("return false;") < 0, "SemesterRankWriteGate 應回傳 true");
                if (errors.Count == 0)
                {
                    Console.WriteLine("phase4-task42-check: ok");
                    Console.WriteLine("database-executed: false");
                    return 0;
                }
                foreach (string error in errors)
                    Console.WriteLine(error);
                return 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return 1;
            }
        }

        private static List<string> CheckOneGrade(string formula)
        {
            List<string> errors = new List<string>();
            SemesterRankPublishStatement statement = SemesterRankPublishSql.Build(
                new SemesterRankGradeStudents[] { Grade(1, Student("424211", 1, "普通科", "忠", "甲", "社團")) },
                "114",
                "1",
                "A'B",
                formula,
                false,
                true);
            errors.AddRange(CheckStatement(formula, statement, new int[] { 1 }, new string[] { "424211" }, false));
            string calc = Between(statement.Sql, ", calc_condition AS", ", subject_score AS");
            Expect(errors, calc.IndexOf("UNION ALL") < 0, "單一年級的計算條件不應再聯集其他年級");
            Expect(errors, CountOf(statement.Sql, "A''B") == 2 && statement.Sql.IndexOf("A'B") < 0, "設定文字的單引號應跳脫");
            string applied = SemesterRankSql.ApplyParticipation(statement.Sql);
            Expect(errors, applied.IndexOf("CROSS JOIN calc_condition") < 0 && applied.IndexOf("item_type LIKE '學期/%'") >= 0, "套用參與旗標後仍應保持等值連接與學期成績停用範圍");
            Expect(errors, applied.IndexOf("PARTITION BY rank_grade_year, subject, participate_year") >= 0, "公布語句仍可套用原本的參與旗標");
            return errors;
        }

        private static List<string> CheckManyGrades(string formula)
        {
            List<string> errors = new List<string>();
            SemesterRankPublishStatement statement = SemesterRankPublishSql.Build(
                new SemesterRankGradeStudents[]
                {
                    Grade(1, Student("424221", 1, "", "忠", "", "")),
                    Grade(3, Student("424223", 3, "普通科", "孝", "乙", "社團"))
                },
                "114",
                "1",
                "<Setting/>",
                formula,
                false,
                true);
            errors.AddRange(CheckStatement(formula, statement, new int[] { 1, 3 }, new string[] { "424221", "424223" }, false));
            string calc = Between(statement.Sql, ", calc_condition AS", ", subject_score AS");
            Expect(errors, CountOf(calc, "UNION ALL") == 1, "兩個年級應在同一段計算條件聯集");
            Expect(errors, CountOf(calc, "FROM\n\t\tinsert_batch_data") == 2, "每個勾選年級都應讀取同一批 RETURNING 識別");
            string students = Between(statement.Sql, "WITH student_list AS", ", insert_batch_data AS");
            Expect(errors, students.IndexOf("''::TEXT AS rank_dept_name") >= 0 && students.IndexOf("''::TEXT AS rank_tag1") >= 0, "空白科別與類別名稱應留在學生清單");
            Expect(errors, statement.Sql.IndexOf("rank_dept_name <> ''") < 0, "公布語句不應新增空白科別排除");
            return errors;
        }

        private static List<string> CheckBlank(string formula)
        {
            List<string> errors = new List<string>();
            ExpectRejected(errors, delegate { SemesterRankPublishSql.Build(null, "114", "1", "", formula, false); }, "沒有勾選年級", "空白年級清單");
            ExpectRejected(errors, delegate { SemesterRankPublishSql.Build(new SemesterRankGradeStudents[0], "114", "1", "", formula, false); }, "批次識別 0", "空的年級清單");
            SemesterRankGradeStudents empty = new SemesterRankGradeStudents();
            empty.GradeYear = 1;
            empty.Selects = new List<string>();
            ExpectRejected(errors, delegate { SemesterRankPublishSql.Build(new SemesterRankGradeStudents[] { empty }, "114", "1", "", formula, false); }, "沒有學生", "沒有學生的年級");
            ExpectRejected(errors, delegate { SemesterRankPublishSql.Build(new SemesterRankGradeStudents[] { Grade(1, "   ") }, "114", "1", "", formula, false); }, "空白學生列", "空白學生列");
            ExpectRejected(errors, delegate { SemesterRankPublishSql.Build(new SemesterRankGradeStudents[] { Grade(1, Student("1", 1, "", "", "", "")), Grade(1, Student("2", 1, "", "", "", "")) }, "114", "1", "", formula, false); }, "年級重複", "重複年級");
            ExpectRejected(errors, delegate { SemesterRankPublishSql.Build(new SemesterRankGradeStudents[] { Grade(1, Student("1", 1, "", "", "", "")) }, "一一四", "1", "", formula, false); }, "學年度不是整數", "非整數學年度");
            string zero = formula.Replace("SELECT count(*) FROM score_list", "SELECT 0 AS batch_id FROM score_list");
            ExpectRejected(errors, delegate { SemesterRankPublishSql.Build(new SemesterRankGradeStudents[] { Grade(1, Student("1", 1, "", "", "", "")) }, "114", "1", "", zero, false); }, "批次識別 0", "公式裡的批次識別 0");
            return errors;
        }

        private static List<string> CheckDuplicateSubjects(string formula)
        {
            List<string> errors = new List<string>();
            SemesterRankPublishStatement statement = SemesterRankPublishSql.Build(
                new SemesterRankGradeStudents[]
                {
                    Grade(2, Student("424231", 2, "普通科", "仁", "甲", "社團"), Student("424232", 2, "普通科", "仁", "甲", "社團"))
                },
                "114",
                "1",
                "",
                formula,
                false,
                true);
            string before = Between(Normalize(formula), ", subject_score AS", ", update_data AS");
            string after = Between(statement.Sql, ", subject_score AS", ", update_data AS");
            Expect(errors, before == after, "科目、分項與排名公式應原樣保留");
            Expect(errors, CountOf(before, "unnest(xpath('/root/SemesterSubjectScoreInfo/Subject'") == 1, "重複科目仍使用原本的 unnest");
            Expect(errors, CountOf(after, "DISTINCT") == 0 && CountOf(after, "unnest(xpath('/root/SemesterSubjectScoreInfo/Subject'") == 1, "重複科目展開不應被 DISTINCT 移除");
            string students = Between(statement.Sql, ", insert_batch_student_data AS", ", insert_detail_data AS");
            Expect(errors, students.IndexOf("identity_rows = 1") >= 0 && students.IndexOf("PARTITION BY calc_condition.batch_id, score_list.student_id") >= 0 && students.IndexOf("min(") < 0 && students.IndexOf("unnest") < 0 && students.IndexOf("item_name") < 0 && students.IndexOf("subject_origin_score") < 0 && students.IndexOf("score_list.score") < 0, "同一批次與學生只留一種母群身分，衝突時不取第一筆，也不折掉科目");
            string detail = Between(statement.Sql, ", insert_detail_data AS", "SELECT CASE");
            Expect(errors, detail.IndexOf("SELECT DISTINCT") < 0 && detail.IndexOf("count(DISTINCT") < 0 && detail.IndexOf("score_list.score") >= 0, "排名明細保留每一筆成績，包含重複科目");
            string list = Between(statement.Sql, "WITH student_list AS", ", insert_batch_data AS");
            Expect(errors, CountOf(list, "'424231'::BIGINT") == 1 && CountOf(list, "'424232'::BIGINT") == 1, "同一年級的兩名學生應各出現一次");
            Expect(errors, CountOf(statement.Sql, "score_list.rank_grade_year = calc_condition.rank_grade_year::INT") == 2, "母群與學生寫入都應依年級等值連接");
            return errors;
        }

        private static List<string> CheckInjectedFailure(string formula)
        {
            List<string> errors = new List<string>();
            SemesterRankPublishStatement statement = SemesterRankPublishSql.Build(
                new SemesterRankGradeStudents[]
                {
                    Grade(1, Student("424241", 1, "普通科", "忠", "甲", "社團")),
                    Grade(2, Student("424242", 2, "普通科", "孝", "乙", "社團"))
                },
                "114",
                "1",
                "",
                formula,
                true,
                true);
            errors.AddRange(CheckStatement(formula, statement, new int[] { 1, 2 }, new string[] { "424241", "424242" }, true));
            Expect(errors, statement.Sql.IndexOf("AND detail_count = detail_count + 1 THEN detail_count") >= 0 && statement.Sql.IndexOf("::TEXT || 'x')::BIGINT") >= 0 && statement.Sql.IndexOf("1/0") < 0, "多年級故意失敗應依賴執行期筆數，不使用常數除以零");
            Expect(errors, CountOf(statement.Sql, "SELECT count(*) FROM insert_detail_data") == 1, "失敗案例應讀取明細筆數");
            int failure = statement.Sql.IndexOf("::TEXT || 'x')::BIGINT");
            int batch = statement.Sql.IndexOf("INSERT INTO rank_batch");
            int update = statement.Sql.IndexOf("is_alive = NULL");
            Expect(errors, batch >= 0 && update > batch && failure > update, "失敗運算應排在批次插入與舊排名停用之後，且仍在同一句");
            return errors;
        }

        private static List<string> CheckStatement(string formula, SemesterRankPublishStatement statement, int[] grades, string[] studentIds, bool failure)
        {
            List<string> errors = new List<string>();
            string sql = statement.Sql;
            Expect(errors, statement.DatabaseExecuted == false && statement.InjectsFailure == failure, "公布語句不應標記成已執行");
            Expect(errors, sql.IndexOf(';') < 0, "公布內容必須是單一 PostgreSQL 語句");
            Expect(errors, sql.IndexOf("CROSS JOIN calc_condition") < 0 && sql.IndexOf("0 AS batch_id") < 0 && sql.IndexOf("batchID") < 0, "不應再跨年級配對或使用批次識別 0");
            Expect(errors, CountOf(sql, "INSERT INTO rank_batch") == 1 && CountOf(sql, "INSERT INTO\n\t\trank_matrix") == 1 && CountOf(sql, "INSERT INTO\n\t\trank_batch_student") == 1 && CountOf(sql, "INSERT INTO\n\t\trank_detail") == 1, "四張表都應在同一句寫入");
            Expect(errors, CountOf(sql, "XMLTABLE") == 0 && CountOf(sql, "MERGE") == 0 && CountOf(sql, "PROCEDURE") == 0 && CountOf(sql, "ON CONFLICT") == 0, "不應引入 PostgreSQL 9.6 沒有的語法");
            Expect(errors, CountOf(sql, "GREATEST") == CountOf(formula, "GREATEST") && CountOf(sql, "subject_origin_score IS NOT NULL") == CountOf(formula, "subject_origin_score IS NOT NULL") && CountOf(sql, "entry_score IS NOT NULL") == CountOf(formula, "entry_score IS NOT NULL"), "分數判斷與擇優公式不應改變");
            Expect(errors, CountOf(sql, "unnest(xpath('/root/SemesterSubjectScoreInfo/Subject'") == CountOf(formula, "unnest(xpath('/root/SemesterSubjectScoreInfo/Subject'"), "科目展開次數應與原公式相同");
            string update = Between(sql, ", update_data AS", ", insert_matrix_data AS");
            Expect(errors, update.IndexOf("is_alive = NULL") >= 0 && update.IndexOf("rank_matrix.is_alive = true") >= 0, "停用仍只把目前有效列改成無效");
            Expect(errors, update.IndexOf("rank_matrix.school_year = calc_condition.rank_school_year::INT") >= 0 && update.IndexOf("rank_matrix.semester = calc_condition.rank_semester::INT") >= 0 && update.IndexOf("rank_matrix.grade_year = calc_condition.rank_grade_year::INT") >= 0, "停用範圍應含本次學年度、學期與勾選年級");
            Expect(errors, CountOf(update, "item_type LIKE '學期/%'") == 1 && update.IndexOf("定期評量") < 0 && update.IndexOf("5學期") < 0, "停用只限學期成績，不包含定期評量或技職繁星");
            string calc = Between(sql, ", calc_condition AS", ", subject_score AS");
            foreach (int grade in grades)
                Expect(errors, calc.IndexOf("'" + grade + "'::TEXT AS rank_grade_year") >= 0, "計算條件應包含年級 " + grade);
            Expect(errors, CountOf(calc, "insert_batch_data.id AS batch_id") == grades.Length, "每個年級都應使用 RETURNING 的批次識別");
            string list = Between(sql, "WITH student_list AS", ", insert_batch_data AS");
            foreach (string studentId in studentIds)
                Expect(errors, CountOf(list, "'" + studentId + "'::BIGINT") == 1, "學生 " + studentId + " 不應在清單重複");
            Expect(errors, list.IndexOf("CROSS JOIN") < 0, "學生清單不應與年級交叉配對");
            int batchAt = sql.IndexOf(", insert_batch_data AS");
            int calcAt = sql.IndexOf(", calc_condition AS");
            int updateAt = sql.IndexOf(", update_data AS");
            int matrixAt = sql.IndexOf(", insert_matrix_data AS");
            int studentAt = sql.IndexOf(", insert_batch_student_data AS");
            int detailAt = sql.IndexOf(", insert_detail_data AS");
            int finalAt = sql.LastIndexOf("AS publish_check");
            Expect(errors, batchAt >= 0 && calcAt > batchAt && updateAt > calcAt && matrixAt > updateAt && studentAt > matrixAt && detailAt > studentAt && finalAt > detailAt, "資料修改應依 RETURNING 的讀取順序串接");
            Expect(errors, Between(sql, ", insert_batch_data AS", ", calc_condition AS").IndexOf("RETURNING *") >= 0, "rank_batch 應 RETURNING");
            Expect(errors, Between(sql, ", insert_matrix_data AS", ", insert_batch_student_data AS").IndexOf("RETURNING *") >= 0 && Between(sql, ", insert_matrix_data AS", ", insert_batch_student_data AS").IndexOf("score_list.rank_grade_year = calc_condition.rank_grade_year::INT") >= 0, "rank_matrix 應 RETURNING，並依年級讀取計算條件");
            string studentWrite = Between(sql, ", insert_batch_student_data AS", ", insert_detail_data AS");
            Expect(errors, studentWrite.IndexOf("RETURNING *") >= 0 && studentWrite.IndexOf("LEFT OUTER JOIN insert_matrix_data") >= 0 && studentWrite.IndexOf("insert_matrix_data.id < 0") >= 0, "學生寫入應以不成立的 LEFT JOIN 等待 rank_matrix，且不刪除學生列");
            string detailWrite = Between(sql, ", insert_detail_data AS", "SELECT CASE");
            Expect(errors, detailWrite.IndexOf("RETURNING *") >= 0 && detailWrite.IndexOf("insert_matrix_data.id AS ref_matrix_id") >= 0 && detailWrite.IndexOf("INNER JOIN") >= 0 && detailWrite.IndexOf("LEFT OUTER JOIN") < 0, "明細應以 INNER JOIN 取得 ref_matrix_id");
            Expect(errors, detailWrite.IndexOf("insert_batch_student_data.ref_student_id = score_list.student_id") >= 0 && detailWrite.IndexOf("insert_batch_student_data.grade_year = score_list.rank_grade_year::INT") >= 0 && detailWrite.IndexOf("matrix_class IS NOT DISTINCT FROM score_list.rank_class_name") >= 0 && detailWrite.IndexOf("matrix_tag1 IS NOT DISTINCT FROM score_list.rank_tag1") >= 0 && detailWrite.IndexOf("matrix_tag2 IS NOT DISTINCT FROM score_list.rank_tag2") >= 0, "明細應同時對上學生、年級、班級與類別，避免學生列把明細放大");
            Expect(errors, detailWrite.IndexOf("ref_student_id < 0") < 0 && detailWrite.IndexOf("1 = 0") < 0 && detailWrite.IndexOf("1/0") < 0, "明細不使用恒不成立的連接，也不使用常數除以零");
            Expect(errors, sql.IndexOf("WHEN detail_count > 0 AND detail_count = score_count AND missing_matrix_count = 0 AND missing_student_count = 0 AND student_conflict_count = 0") >= 0 && sql.IndexOf("THEN detail_count") >= 0 && sql.IndexOf("NOT EXISTS") >= 0 && sql.IndexOf("insert_matrix_data.rank_name = score_list.rank_name") >= 0, "成功時回傳明細筆數；缺矩陣、缺學生、筆數不符或身分衝突都留在同一句的失敗條件");
            Expect(errors, failure == (sql.IndexOf("detail_count = detail_count + 1") >= 0), "只有故意失敗案例加上執行期無法成立的筆數條件");
            Expect(errors, Between(sql, ", insert_matrix_data AS", ", insert_batch_student_data AS").IndexOf("update_data.id") >= 0, "母群寫入應等待舊排名停用");
            return errors;
        }

        private static List<string> CheckOutcomes()
        {
            List<string> errors = new List<string>();
            SemesterRankPublishOutcome buildFailure = SemesterRankPublishOutcome.From(new InvalidOperationException("沒有勾選年級，不建立公布語句。"), null);
            ExpectOutcome(errors, buildFailure, false, "SQL 建立失敗");
            SemesterRankPublishOutcome executeFailure = SemesterRankPublishOutcome.From(new InvalidOperationException("查詢失敗"), Published(4));
            ExpectOutcome(errors, executeFailure, false, "SQL 執行例外");
            ExpectOutcome(errors, SemesterRankPublishOutcome.From(null, null), false, "沒有回傳結果");
            ExpectOutcome(errors, SemesterRankPublishOutcome.From(null, new DataTable()), false, "無排名明細");
            ExpectOutcome(errors, SemesterRankPublishOutcome.From(null, Published(0)), false, "明細筆數為零");
            DataTable partial = new DataTable();
            partial.Columns.Add("published_count", typeof(int));
            partial.Rows.Add(1);
            partial.Rows.Add(1);
            ExpectOutcome(errors, SemesterRankPublishOutcome.From(null, partial), false, "部分結果");
            SemesterRankPublishOutcome success = SemesterRankPublishOutcome.From(null, Published(6));
            Expect(errors, success.Succeeded && success.DialogMessage == "計算完成" && success.StatusMessage == "排名計算完成", "完整明細筆數才顯示計算完成");
            Expect(errors, success.DialogMessage.IndexOf("計算排名失敗") < 0, "成功訊息不應同時帶失敗");
            return errors;
        }

        private static void ExpectOutcome(List<string> errors, SemesterRankPublishOutcome outcome, bool succeeded, string name)
        {
            Expect(errors, outcome.Succeeded == succeeded, name + " 的成功標記不對");
            Expect(errors, outcome.DialogMessage.IndexOf("計算排名失敗") == 0 && outcome.DialogMessage.IndexOf("計算完成") < 0, name + " 不應顯示計算完成");
            Expect(errors, outcome.StatusMessage == "排名計算失敗", name + " 的狀態列應是失敗");
        }

        private static DataTable Published(int count)
        {
            DataTable table = new DataTable();
            table.Columns.Add("published_count", typeof(int));
            table.Rows.Add(count);
            return table;
        }

        private static List<string> CheckForm(string source)
        {
            List<string> errors = new List<string>();
            int gate = source.IndexOf("CanWriteRankTables");
            int build = source.IndexOf("SemesterRankPublishSql.Build(");
            int apply = source.IndexOf("SemesterRankSql.ApplyParticipation");
            int select = source.IndexOf("queryHelper.Select(insertRankSql)");
            Expect(errors, gate >= 0 && build > gate && apply > build && select > apply, "寫入閘門、組句、參與旗標與唯一一次查詢應依序排列");
            Expect(errors, CountOf(source, "queryHelper.Select(insertRankSql)") == 1 && source.IndexOf("queryHelper.Select(insertRankBatchSql)") < 0, "所有勾選年級只能有一次 QueryHelper.Select");
            Expect(errors, source.IndexOf("int batchID = 0;") < 0 && source.IndexOf("// 依年級分批計算") < 0, "表單不應再初始化批次識別 0 或依年級分批執行");
            Expect(errors, source.IndexOf("BEGIN") < 0 && source.IndexOf("COMMIT") < 0, "排名語句不使用 BEGIN/COMMIT");
            Expect(errors, source.IndexOf("MotherGroupSnapshotJson.Compose") >= 0 && source.IndexOf("MotherGroupSnapshot.Append") < 0 && source.IndexOf("UPDATE rank_batch") < 0, "快照寫在新批次的 extension，不另外更新既有批次");
            int saveAt = source.IndexOf("cd.Save()");
            Expect(errors, saveAt >= 0 && saveAt < select && source.IndexOf("不會隨排名語句回滾") >= 0 && source.IndexOf("不會隨排名語句回滾") < saveAt, "排名失敗時，先前儲存的擇優設定不會被排名語句回滾");
            Expect(errors, source.IndexOf("SemesterRankFormulaSql.Build(gradeSelectCopy)") >= 0 && source.IndexOf("string insertRankSql = @\"") < 0, "表單應呼叫共用公式組句，不再內嵌 verbatim SQL");
            Expect(errors, CountOf(SemesterRankFormulaSql.BuildOriginOnly(), "CROSS JOIN calc_condition") == 2, "共用公式仍有兩處跨年級配對交給組譯器");
            int worker = source.LastIndexOf("bkw.DoWork +=");
            int completed = source.LastIndexOf("bkw.RunWorkerCompleted +=");
            Expect(errors, worker >= 0 && completed > worker, "計算應在背景工作裡執行");
            if (worker < 0 || completed <= worker)
                return errors;
            string work = source.Substring(worker, completed - worker);
            int selectAt = work.IndexOf("queryHelper.Select(insertRankSql)");
            int caught = work.IndexOf("bkwException = ex");
            Expect(errors, work.IndexOf("Console.WriteLine(ex.Message)") < 0 && selectAt >= 0 && caught > selectAt, "組句、套用旗標或查詢失敗都應進入 bkwException，不可只寫主控台");
            Expect(errors, work.IndexOf("HasPublishedDetails(publishResult)") >= 0 && work.IndexOf("排名明細沒有寫入。") > selectAt, "零筆明細不應繼續當成成功");
            string done = source.Substring(completed);
            int dialog = done.IndexOf("MessageBox.Show(outcome.DialogMessage)");
            Expect(errors, done.IndexOf("SemesterRankPublishOutcome.From(bkwException, publishResult)") >= 0 && done.IndexOf("pbLoading.Visible = false") >= 0 && done.IndexOf("RestoreCalculationEditors()") >= 0 && done.IndexOf("SetStatusBarMessage(outcome.StatusMessage)") >= 0 && dialog > done.IndexOf("pbLoading.Visible = false"), "失敗時應先恢復計算前的控制項、進度條與狀態列，再顯示結果訊息");
            Expect(errors, done.IndexOf("throw new Exception(\"計算排名失敗\"") < 0 && done.IndexOf("MessageBox.Show(\"計算完成\")") < 0, "完成事件不應在失敗路徑繼續拋出或固定顯示計算完成");
            return errors;
        }

        private static void ExpectRejected(List<string> errors, Action action, string messagePart, string name)
        {
            try
            {
                action();
                errors.Add(name + " 應拒絕建立語句");
            }
            catch (InvalidOperationException ex)
            {
                Expect(errors, ex.Message.IndexOf(messagePart) >= 0 && ex.Message.IndexOf("INSERT INTO") < 0, name + " 應拒絕且不產生寫入語句");
            }
        }

        private static SemesterRankGradeStudents Grade(int gradeYear, params string[] selects)
        {
            SemesterRankGradeStudents grade = new SemesterRankGradeStudents();
            grade.GradeYear = gradeYear;
            grade.Selects = new List<string>(selects);
            return grade;
        }

        private static string Student(string id, int gradeYear, string dept, string className, string tag1, string tag2)
        {
            return "SELECT\n'" + id + "'::BIGINT AS student_id\n, '學生'::TEXT AS student_name\n, '" + gradeYear + "'::INT AS rank_grade_year\n, '" + dept + "'::TEXT AS rank_dept_name\n, '" + className + "'::TEXT AS rank_class_name\n, '" + tag1 + "'::TEXT AS rank_tag1\n, '" + tag2 + "'::TEXT AS rank_tag2\n, true AS participate_year\n, true AS participate_dept\n, true AS participate_class\n, true AS participate_tag1\n, true AS participate_tag2\n";
        }

        private static string Formula(string source, List<string> errors)
        {
            string formula = SemesterRankFormulaSql.BuildOriginOnly();
            if (formula.IndexOf(", subject_score AS") < 0 || formula.IndexOf("SELECT count(*) FROM score_list") < 0)
            {
                errors.Add("應能從共用公式組句取得排名公式");
                return "";
            }
            Expect(errors, source.IndexOf("SemesterRankFormulaSql.Build(gradeSelectCopy)") >= 0, "產品畫面應改呼叫共用公式組句");
            return formula;
        }

        private static string Read(string path, List<string> errors)
        {
            if (!File.Exists(path))
            {
                errors.Add("找不到檔案：" + path);
                return "";
            }
            return File.ReadAllText(path, Encoding.UTF8);
        }

        private static string Normalize(string text)
        {
            return (text ?? "").Replace("\r\n", "\n").Replace("\r", "\n");
        }

        private static string Between(string text, string start, string end)
        {
            int from = text.IndexOf(start);
            int to = text.IndexOf(end, from < 0 ? 0 : from + start.Length);
            if (from < 0 || to < from)
                return "";
            return text.Substring(from, to - from);
        }

        private static int CountOf(string text, string token)
        {
            int count = 0;
            int at = 0;
            while (text != null && at >= 0 && at < text.Length)
            {
                at = text.IndexOf(token, at);
                if (at < 0)
                    break;
                count++;
                at += token.Length;
            }
            return count;
        }

        private static void Expect(List<string> errors, bool condition, string message)
        {
            if (!condition)
                errors.Add(message);
        }
    }
}
