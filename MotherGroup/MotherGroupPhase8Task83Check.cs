using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SHEvaluation.Rank.MotherGroup
{
    public static class MotherGroupPhase8Task83Check
    {
        const string ReadyText = "說明：班、科、年排名會自動計算。分項成績擇優項目請參考成績計算規則。";

        public static int Main(string[] args)
        {
            try
            {
                List<string> errors = new List<string>();
                string form = File.ReadAllText("CalculateSemesterAssessmentRank.cs", Encoding.UTF8);
                string designer = File.ReadAllText("CalculateSemesterAssessmentRank.Designer.cs", Encoding.UTF8);
                string gate = File.ReadAllText("MotherGroup\\SemesterRankWriteGate.cs", Encoding.UTF8);
                errors.AddRange(CheckSuccess(form, designer));
                errors.AddRange(CheckFailure(form));
                errors.AddRange(CheckScheduling(form, gate));
                errors.AddRange(CheckOtherRankFiles());
                errors.AddRange(CheckNextStep(form));
                if (errors.Count == 0)
                {
                    Console.WriteLine("phase8-task83-check: ok");
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

        private static List<string> CheckSuccess(string form, string designer)
        {
            List<string> errors = new List<string>();
            string finish = Between(form, "void FinishScreenLoad", "void FailScreenLoad");
            Expect(errors, finish.IndexOf("if (_screenLoadSettled || IsDisposed)") >= 0 && finish.IndexOf("_screenLoadSettled = true") > finish.IndexOf("IsDisposed"), "成功路徑只執行一次，且關閉後不更新控制項");
            Expect(errors, finish.IndexOf("labelX6.Text = ScreenLoadReadyText") >= 0 && form.IndexOf(ReadyText) >= 0, "五段成功後應恢復原本說明");
            Expect(errors, finish.IndexOf("btnNext.Enabled = true") >= 0 && CountOf(finish, "btnNext.Enabled = true") == 1, "畫面載入只有全部成功才在該方法啟用下一步");
            Expect(errors, finish.IndexOf("Items.Clear") < 0 && finish.IndexOf("_TagList =") < 0 && finish.IndexOf("_StudentList =") < 0, "成功時保留類別、年級與擇優結果");
            string grades = Between(form, "void LoadGradeYears", "void CalculateSemesterAssessmentRank_Load");
            Expect(errors, grades.IndexOf("listViewItem.Checked = true") >= 0 && grades.IndexOf("FinishScreenLoad()") > grades.IndexOf("listGradeYear.Items.Add"), "年級勾選完成後才進入成功路徑");
            Expect(errors, designer.IndexOf("new System.Drawing.Point(26, 436)") >= 0 && designer.IndexOf("new System.Drawing.Size(671, 23)") >= 0, "labelX6 維持原位置與大小");
            return errors;
        }

        private static List<string> CheckFailure(string form)
        {
            List<string> errors = new List<string>();
            string fail = Between(form, "void FailScreenLoad", "void cboStudentFilter_SelectedIndexChanged");
            Expect(errors, fail.IndexOf("if (_screenLoadSettled)") >= 0 && fail.IndexOf("_screenLoadSettled = true") > fail.IndexOf("if (_screenLoadSettled)"), "失敗路徑只執行一次");
            int disposed = fail.IndexOf("if (IsDisposed)");
            int clear = fail.IndexOf("cboStudentFilter.Items.Clear()");
            Expect(errors, disposed >= 0 && clear > disposed, "表單關閉後失敗路徑不更新控制項");
            Expect(errors, fail.IndexOf("_TagList = new List<TagConfigRecord>()") >= 0 && fail.IndexOf("_StudentList = new List<StudentRecord>()") >= 0 && fail.IndexOf("_GradeYearList = new List<int>()") >= 0, "失敗時清空暫存學生、類別與年級清單");
            Expect(errors, fail.IndexOf("cboStudentTag1.Items.Clear()") >= 0 && fail.IndexOf("cboStudentTag2.Items.Clear()") >= 0 && fail.IndexOf("listGradeYear.Items.Clear()") >= 0, "失敗時清空畫面上的類別與年級");
            Expect(errors, fail.IndexOf("btnNext.Enabled = false") >= 0 && fail.IndexOf("btnNext.Enabled = true") < 0, "失敗時下一步維持停用");
            Expect(errors, fail.IndexOf("\"載入\" + stage + \"失敗：\"") >= 0 && fail.IndexOf(ReadyText) < 0 && fail.IndexOf("ScreenLoadReadyText") < 0, "失敗時顯示階段與錯誤，不恢復原本說明");
            Expect(errors, fail.IndexOf("_screenLoadErrorTip.SetToolTip(labelX6, message)") >= 0, "超出標籤寬度時，完整錯誤留在不改配置的工具提示");
            string[] stages = new string[] { "畫面初始化", "學校設定", "學生類別", "學生清單", "年級清單" };
            string[] methods = new string[] { "void LoadScreenInitialization", "void LoadSchoolSettings", "void LoadStudentCategories", "void LoadStudentList", "void LoadGradeYears" };
            for (int i = 0; i < stages.Length; i++)
            {
                string body = Between(form, methods[i], i + 1 < methods.Length ? methods[i + 1] : "void CalculateSemesterAssessmentRank_Load");
                Expect(errors, body.IndexOf("catch (Exception ex)") >= 0 && body.IndexOf("FailScreenLoad(\"" + stages[i] + "\", ex)") >= 0, stages[i] + " 應捕捉例外並標示該階段");
            }
            string next = Between(form, "void btnNext_Click", "CalculationPeriodBinder.ReadCalculation");
            Expect(errors, next.IndexOf("if (!_screenLoadSettled || !btnNext.Enabled)") >= 0 && next.IndexOf("return;") > next.IndexOf("!_screenLoadSettled"), "未成功載入時不得進入確認母群");
            return errors;
        }

        private static List<string> CheckScheduling(string form, string gate)
        {
            List<string> errors = new List<string>();
            string load = Between(form, "protected override void OnShown", "void cboStudentFilter_SelectedIndexChanged");
            Expect(errors, CountOf(load, "BeginInvoke(new MethodInvoker(") == 5, "應保留五次 BeginInvoke 階段排程");
            Expect(errors, load.IndexOf("BackgroundWorker") < 0 && form.IndexOf("Application.DoEvents") < 0, "初次載入不使用背景工作或 DoEvents");
            string students = Between(form, "void LoadStudentList", "void LoadGradeYears");
            Expect(errors, students.IndexOf("x.Status == StudentRecord.StudentStatus.一般") >= 0 && students.IndexOf("!string.IsNullOrEmpty(x.RefClassID)") >= 0 && students.IndexOf("x.Class.GradeYear != null") >= 0, "學生篩選維持一般、有班級且年級有值");
            string school = Between(form, "void LoadSchoolSettings", "void LoadStudentCategories");
            int empty = school.IndexOf("if (configString != \"\")");
            int reset = school.IndexOf("chbMakeUp.Checked = chbRetake.Checked = chbSYAdj.Checked = chbHandAdj.Checked = false");
            Expect(errors, empty >= 0 && reset > empty, "擇優設定仍只在設定檔有內容時先取消再勾回");
            Expect(errors, gate.IndexOf("return true;") >= 0 && gate.IndexOf("return false;") < 0, "SemesterRankWriteGate 應回傳 true");
            return errors;
        }

        private static List<string> CheckOtherRankFiles()
        {
            List<string> errors = new List<string>();
            foreach (string path in Directory.GetFiles(".", "*.cs", SearchOption.AllDirectories))
            {
                if (path.IndexOf("\\bin\\") >= 0 || path.IndexOf("\\obj\\") >= 0 || path.IndexOf("\\MotherGroup\\") >= 0)
                    continue;
                string name = Path.GetFileName(path);
                if (name.IndexOf("Regular") < 0 && name.IndexOf("Technology") < 0)
                    continue;
                string text = File.ReadAllText(path, Encoding.UTF8);
                if (text.IndexOf("FailScreenLoad") >= 0 || text.IndexOf("FinishScreenLoad") >= 0)
                    errors.Add("定期評量或技職繁星不應套用學期成績載入結果：" + path);
            }
            return errors;
        }

        private static List<string> CheckNextStep(string form)
        {
            List<string> errors = new List<string>();
            int next = form.IndexOf("void btnNext_Click");
            int history = form.IndexOf("void BeginHistorySnapshotRestore");
            string nextBody = next >= 0 && history > next ? form.Substring(next, history - next) : "";
            Expect(errors, nextBody.IndexOf("if (_nextStepRunning)") >= 0 && nextBody.IndexOf("_nextStepRunning = true") > nextBody.IndexOf("if (_nextStepRunning)") && nextBody.IndexOf("btnNext.Enabled = false") > nextBody.IndexOf("_nextStepRunning = true"), "下一步應先擋住重入，再停用按鈕");
            Expect(errors, nextBody.IndexOf("正在載入目前學生母群資料...") >= 0 && nextBody.IndexOf("正在讀取歷史批次及母群資料...") >= 0 && nextBody.IndexOf("正在匯入 Excel 母群資料...") >= 0, "三種來源應立刻顯示處理提示");
            int excel = form.IndexOf("void BeginExcelImport");
            int excelEnd = form.IndexOf("bool IsHistorySourceSelected", excel);
            string excelBody = excel >= 0 && excelEnd > excel ? form.Substring(excel, excelEnd - excel) : "";
            int dialog = excelBody.IndexOf("dialog.ShowDialog()");
            int worker = excelBody.IndexOf("BackgroundWorker");
            int open = excelBody.IndexOf("MotherGroupExcelExchange.Open");
            Expect(errors, dialog >= 0 && dialog < worker && open > worker && excelBody.IndexOf("ReleaseNextStep(true)") >= 0 && excelBody.IndexOf("ShowConfirmList") > excelBody.IndexOf("!validation.IsValid"), "Excel 選檔留在畫面執行緒，解析在背景，失敗要恢復下一步");
            string historyBody = history >= 0 ? form.Substring(history, form.IndexOf("void ShowConfirmList") - history) : "";
            Expect(errors, historyBody.IndexOf("HistorySnapshotRestore.Load") >= 0 && historyBody.IndexOf("HistoryRankRestore.ReadBatchStudentTable") > historyBody.IndexOf("HistorySnapshotRestore.Load") && historyBody.IndexOf("SelectBatchStudentSql") < 0, "歷史還原順序維持 JSON、XML，然後分頁讀排名學生");
            Expect(errors, CountOf(historyBody, "ReleaseNextStep(true)") >= 4 && historyBody.IndexOf("RunWorkerCompleted") >= 0, "歷史完成事件的失敗分支都要恢復下一步");
            int confirm = form.IndexOf("void ShowConfirmList");
            int previous = form.IndexOf("void btnPrevious_Click");
            string confirmBody = confirm >= 0 && previous > confirm ? form.Substring(confirm, previous - confirm) : "";
            Expect(errors, confirmBody.IndexOf("ReleaseNextStep(false)") >= 0 && confirmBody.IndexOf("ShowConfirmForm()") < confirmBody.IndexOf("ReleaseNextStep(false)"), "第二頁開啟後解除旗標，且不在這裡重新啟用下一步");
            string previousBody = previous >= 0 ? form.Substring(previous, form.IndexOf("void ExportMotherGroup") - previous) : "";
            Expect(errors, previousBody.IndexOf("ReleaseNextStep(true)") >= 0, "回到第一頁時恢復下一步");
            return errors;
        }

        private static string Between(string text, string start, string end)
        {
            int at = text.IndexOf(start);
            int to = at < 0 ? -1 : text.IndexOf(end, at + start.Length);
            if (at < 0 || to < 0)
                return "";
            return text.Substring(at, to - at);
        }

        private static int CountOf(string text, string token)
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

        private static void Expect(List<string> errors, bool condition, string message)
        {
            if (!condition)
                errors.Add(message);
        }
    }
}
