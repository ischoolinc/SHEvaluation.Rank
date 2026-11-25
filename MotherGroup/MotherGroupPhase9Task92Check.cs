using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SHEvaluation.Rank.MotherGroup
{
    public static class MotherGroupPhase9Task92Check
    {
        public static int Main(string[] args)
        {
            try
            {
                List<string> errors = new List<string>();
                errors.AddRange(CheckSelectedGrades());
                string form = File.ReadAllText("CalculateSemesterAssessmentRank.cs", Encoding.UTF8);
                string gate = File.ReadAllText("MotherGroup\\SemesterRankWriteGate.cs", Encoding.UTF8);
                errors.AddRange(CheckForm(form, gate));
                if (errors.Count == 0)
                {
                    Console.WriteLine("phase9-task92-check: ok");
                    Console.WriteLine("database-executed: false");
                    Console.WriteLine("school-ui-opened: false");
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

        private static List<string> CheckSelectedGrades()
        {
            List<string> errors = new List<string>();
            MotherGroupConfirmList list = new MotherGroupConfirmList();
            list.Rows.Add(Row("1", "1年級"));
            list.Rows.Add(Row("2", "2年級"));
            list.Rows.Add(Row("3", "高一"));
            list.Rows.Add(Row("", "1年級"));
            int[] onlyFirst = new int[] { 1 };
            string grades = RankCalculationConfirm.GradeList(list, onlyFirst);
            Expect(errors, grades == "1年級" && grades.IndexOf("2年級") < 0 && grades.IndexOf("高一") < 0, "圖1只勾選一年級時，確認框不可顯示其他年級");
            string message = RankCalculationConfirm.Message("114", "1", CalculationPeriod.CurrentSourceName, grades);
            Expect(errors, message.IndexOf("本次會停用114學年度第1學期、1年級的有效學期成績排名") >= 0 && message.IndexOf("2年級") < 0, "確認框的停用範圍應只有圖1勾選且可解析的年級");

            SemesterRankStudentSqlResult built = SemesterRankSql.Build(list);
            List<SemesterRankGradeStudents> limited = RankCalculationConfirm.LimitToSelectedGrades(built.Grades, onlyFirst);
            List<int> shown = RankCalculationConfirm.GradeYears(list, onlyFirst);
            Expect(errors, shown.Count == 1 && shown[0] == 1 && limited.Count == 1 && limited[0].GradeYear == 1, "確認框年級應與進入 calc_condition 的年級相同");
            string formula = Formula(File.ReadAllText("CalculateSemesterAssessmentRank.cs", Encoding.UTF8), errors);
            if (formula.Length > 0 && limited.Count == 1)
            {
                SemesterRankPublishStatement statement = SemesterRankPublishSql.Build(limited, "114", "1", "", formula, false);
                string calc = Between(statement.Sql, ", calc_condition AS", ", subject_score AS");
                string studentList = Between(statement.Sql, "WITH student_list AS", ", insert_batch_data AS");
                Expect(errors, calc.IndexOf("'1'::TEXT AS rank_grade_year") >= 0 && calc.IndexOf("'2'::TEXT AS rank_grade_year") < 0 && CountOf(calc, "AS rank_grade_year") == 1, "最終 calc_condition 只含圖1勾選的一年級");
                Expect(errors, studentList.IndexOf("jsonb_to_recordset") >= 0 && studentList.IndexOf("UNION ALL") < 0 && studentList.IndexOf("\"student_id\":2") < 0, "未勾選年級不進入 JSONB 學生清單");
            }

            string widerSelection = RankCalculationConfirm.GradeList(list, new int[] { 1, 3 });
            List<SemesterRankGradeStudents> wider = RankCalculationConfirm.LimitToSelectedGrades(built.Grades, new int[] { 1, 3 });
            Expect(errors, widerSelection == "1年級" && wider.Count == 1 && wider[0].GradeYear == 1, "圖1多勾的年級若確認清單沒有，不進入停用範圍");
            Expect(errors, RankCalculationConfirm.GradeList(list, new int[] { }) == "（沒有可解析的年級）" && RankCalculationConfirm.LimitToSelectedGrades(built.Grades, new int[] { }).Count == 0, "沒有交集時不宣稱停用其他年級");

            string history = RankCalculationConfirm.GradeList(list, onlyFirst);
            string excel = RankCalculationConfirm.GradeList(list, onlyFirst);
            Expect(errors, history == grades && excel == grades, "三種母群來源使用同一套年級交集");

            SemesterRankPublishOutcome failed = SemesterRankPublishOutcome.From(new InvalidOperationException("排名語句失敗"), null);
            Expect(errors, failed.Succeeded == false && failed.DialogMessage.IndexOf("計算排名失敗") == 0 && failed.DialogMessage.IndexOf("計算完成") < 0, "實際失敗不得顯示計算完成");
            return errors;
        }

        private static List<string> CheckForm(string form, string gate)
        {
            List<string> errors = new List<string>();
            int click = form.IndexOf("void btnCacluate_Click");
            int run = form.IndexOf("void RunRankCalculation");
            int resize = form.IndexOf("void CalculateSemesterAssessmentRank_Resize");
            string clickBody = click >= 0 && run > click ? form.Substring(click, run - click) : "";
            string body = click >= 0 && resize > click ? form.Substring(click, resize - click) : "";
            Expect(errors, clickBody.IndexOf("if (!_rankCalculationRunning)") >= 0 && clickBody.IndexOf("return;") < 0, "重複進入應在進入計算前擋下，且點擊方法本身不返回");

            int running = body.IndexOf("_rankCalculationRunning = true");
            int warn = body.IndexOf("UnparsedYearMessage");
            int gateCall = body.IndexOf("CanWriteRankTables");
            int gateReturn = body.IndexOf("return;");
            int confirm = body.IndexOf("RankCalculationConfirm.Message");
            int disable = body.IndexOf("btnCacluate.Enabled = false");
            int save = body.IndexOf("cd.Save()");
            int build = body.IndexOf("SemesterRankSql.Build(");
            int limit = body.IndexOf("LimitToSelectedGrades");
            int publish = body.IndexOf("SemesterRankPublishSql.Build(");
            int apply = body.IndexOf("SemesterRankSql.ApplyParticipation");
            int select = body.IndexOf("queryHelper.Select(insertRankSql)");
            Expect(errors, running >= 0 && running < warn && warn < gateCall && gateReturn > gateCall, "執行中旗標必須在年排名提示、阻擋訊息與確認框之前設下");
            Expect(errors, confirm > gateReturn && body.IndexOf("RankCalculationConfirm.GradeList(calculationList, _GradeYearList)") > gateReturn, "確認框年級使用圖1勾選年級與工作副本的交集");
            int cancelStart = body.IndexOf("!= DialogResult.OK");
            int cancelReturn = cancelStart >= 0 ? body.IndexOf("return;", cancelStart) : -1;
            string cancelSlice = cancelStart >= 0 && cancelReturn > cancelStart ? body.Substring(cancelStart, cancelReturn - cancelStart) : "";
            Expect(errors, cancelSlice.IndexOf("_rankCalculationRunning = false") >= 0 && cancelSlice.IndexOf("cd.Save()") < 0 && cancelSlice.IndexOf("queryHelper.Select") < 0, "確認框取消時不儲存、不送出排名 SQL");
            Expect(errors, disable > cancelReturn && disable < save && cancelSlice.IndexOf("btnCacluate.Enabled = false") < 0, "確認後才停用計算，取消路徑不停用");

            Expect(errors, save > confirm && build > save && limit > build && publish > limit && apply > publish && select > apply, "儲存、組句、年級交集、公布語句、參與旗標與唯一查詢的順序維持不變");
            Expect(errors, CountOf(body, "queryHelper.Select(insertRankSql)") == 1, "排名查詢仍只有一次");
            Expect(errors, body.IndexOf("SemesterRankPublishSql.Build(rankStudents.Grades") >= 0, "公布語句使用交集後的年級");

            int worker = body.IndexOf("bkw.RunWorkerAsync()");
            int started = body.IndexOf("rankWorkerStarted = true");
            int flowCatch = body.IndexOf("catch (Exception rankFlowException)");
            int flowFinally = body.IndexOf("if (!rankWorkerStarted)", flowCatch < 0 ? 0 : flowCatch);
            Expect(errors, worker >= 0 && started > worker && flowCatch > worker && flowFinally > flowCatch, "背景工作成功送出後才保留執行中旗標");
            string flow = flowCatch >= 0 && flowFinally > flowCatch ? body.Substring(flowCatch, flowFinally - flowCatch) : "";
            Expect(errors, flow.IndexOf("計算排名失敗") >= 0 && flow.IndexOf("計算完成") < 0 && flow.IndexOf("RestoreCalculationEditors()") >= 0, "確認框之後的例外應恢復計算前的控制項，且不得顯示計算完成");
            Expect(errors, flowFinally >= 0 && body.IndexOf("_rankCalculationRunning = false", flowFinally) >= 0, "未送出背景工作時應清除執行中旗標");

            int completed = body.IndexOf("bkw.RunWorkerCompleted +=");
            string done = completed >= 0 ? body.Substring(completed, worker > completed ? worker - completed : body.Length - completed) : "";
            Expect(errors, done.IndexOf("finally") >= 0 && done.IndexOf("_rankCalculationRunning = false") >= 0 && done.IndexOf("RestoreCalculationEditors()") >= 0, "背景工作完成事件發生例外時也要清除旗標並恢復計算前的控制項");

            Expect(errors, CountOf(body, "RankCalculationConfirm.Message") == 1 && CountOf(form, "void RunRankCalculation") == 1, "三種母群來源共用同一條計算路徑");
            Expect(errors, body.IndexOf("HistorySchoolYear") < 0 && body.IndexOf("ExcelSourceName") < 0, "共用路徑不改歷史期間或 Excel 來源讀法");
            Expect(errors, gate.IndexOf("return true;") >= 0 && gate.IndexOf("return false;") < 0, "SemesterRankWriteGate 應回傳 true");
            return errors;
        }

        private static ConfirmStudentRow Row(string id, string year)
        {
            StudentMotherGroup student = new StudentMotherGroup();
            student.StudentId = id;
            student.StudentName = "學生";
            student.SetSourceSlots(new List<RankSlot>
            {
                new RankSlot { RankType = RankKinds.Year, GroupName = year, Participate = true },
                new RankSlot { RankType = RankKinds.Department, GroupName = "", Participate = false },
                new RankSlot { RankType = RankKinds.Class, GroupName = "", Participate = false },
                new RankSlot { RankType = RankKinds.Tag1, GroupName = "", Participate = false },
                new RankSlot { RankType = RankKinds.Tag2, GroupName = "", Participate = false }
            });
            ConfirmStudentRow row = new ConfirmStudentRow();
            row.Group = student;
            return row;
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
            while (text != null && at >= 0)
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
