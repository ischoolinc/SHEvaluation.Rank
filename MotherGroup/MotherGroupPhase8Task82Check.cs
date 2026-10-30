using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SHEvaluation.Rank.MotherGroup
{
    public static class MotherGroupPhase8Task82Check
    {
        public static int Main(string[] args)
        {
            try
            {
                List<string> errors = new List<string>();
                string form = File.ReadAllText("CalculateSemesterAssessmentRank.cs", Encoding.UTF8);
                string designer = File.ReadAllText("CalculateSemesterAssessmentRank.Designer.cs", Encoding.UTF8);
                string gate = File.ReadAllText("MotherGroup\\SemesterRankWriteGate.cs", Encoding.UTF8);
                errors.AddRange(CheckStages(form, designer));
                errors.AddRange(CheckReadsStayOnUiThread(form));
                errors.AddRange(CheckRules(form, gate));
                errors.AddRange(CheckOtherRankFiles());
                if (errors.Count == 0)
                {
                    Console.WriteLine("phase8-task82-check: ok");
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

        private static List<string> CheckStages(string form, string designer)
        {
            List<string> errors = new List<string>();
            string[] stages = new string[]
            {
                "正在初始化畫面…",
                "正在載入學校設定…",
                "正在載入學生類別…",
                "正在載入學生清單…",
                "正在載入年級清單…"
            };
            int previous = -1;
            foreach (string stage in stages)
            {
                int at = form.IndexOf("labelX6.Text = \"" + stage + "\"");
                Expect(errors, at > previous, "labelX6 應依序顯示：" + stage);
                previous = at;
            }
            Expect(errors, form.IndexOf("Application.DoEvents") < 0, "載入階段不使用 Application.DoEvents()");
            Expect(errors, form.IndexOf("new Form(") < 0 && form.IndexOf("ShowDialog()") >= 0, "不為載入階段新增視窗");
            Expect(errors, designer.IndexOf("說明：班、科、年排名會自動計算。分項成績擇優項目請參考成績計算規則。") >= 0, "Designer 應保留原本說明文字");
            string running = Between(form, "public CalculateSemesterAssessmentRank()", "void FinishScreenLoad");
            Expect(errors, running.IndexOf("說明：班、科、年排名會自動計算。分項成績擇優項目請參考成績計算規則。") < 0, "五個載入階段進行中不恢復原本說明");
            string load = Between(form, "protected override void OnShown", "void cboStudentFilter_SelectedIndexChanged");
            Expect(errors, CountOf(load, "BeginInvoke(new MethodInvoker(") == 5, "五段讀取前都應以 BeginInvoke 交回訊息迴圈");
            Expect(errors, load.IndexOf("BackgroundWorker") < 0, "初次載入不使用 BackgroundWorker");
            Expect(errors, load.IndexOf("%") < 0, "載入階段不顯示百分比");
            return errors;
        }

        private static List<string> CheckReadsStayOnUiThread(string form)
        {
            List<string> errors = new List<string>();
            string constructor = Between(form, "public CalculateSemesterAssessmentRank()", "protected override void OnShown");
            Expect(errors, constructor.IndexOf("btnNext.Enabled = false") >= 0, "載入期間下一步應停用");
            Expect(errors, constructor.IndexOf("btnNext.Enabled = true") < 0, "建構函式不應在載入前啟用下一步");
            Expect(errors, constructor.IndexOf("SelectAll") < 0 && constructor.IndexOf("SelectByCategory") < 0 && constructor.IndexOf("DefaultSchoolYear") < 0 && constructor.IndexOf("DefaultSemester") < 0 && constructor.IndexOf("Configuration[") < 0, "建構函式不應再讀取學校設定、學生類別或學生清單");
            Expect(errors, constructor.IndexOf("正在初始化畫面…") >= 0 && constructor.IndexOf("plStudentView.Visible = false") >= 0, "顯示前應先切到圖1並寫入初始化階段");

            string shown = Between(form, "protected override void OnShown", "void LoadScreenInitialization");
            Expect(errors, shown.IndexOf("PaintLoadStage()") >= 0 && shown.IndexOf("PaintLoadStage()") < shown.IndexOf("BeginInvoke(new MethodInvoker(LoadScreenInitialization))"), "初始化文字應先畫出，再排入下一段");

            string screen = Between(form, "void LoadScreenInitialization", "void LoadSchoolSettings");
            Expect(errors, screen.IndexOf("EnsureFigureControls()") >= 0 && screen.IndexOf("PaintLoadStage()") > screen.IndexOf("正在載入學校設定…") && screen.IndexOf("BeginInvoke(new MethodInvoker(LoadSchoolSettings))") > screen.IndexOf("PaintLoadStage()"), "學校設定文字應先畫出，再排入訊息迴圈");

            string school = Between(form, "void LoadSchoolSettings", "void LoadStudentCategories");
            Expect(errors, school.IndexOf("School.DefaultSchoolYear") >= 0 && school.IndexOf("School.DefaultSemester") >= 0, "預設學年度與學期留在 UI 階段方法");
            Expect(errors, school.IndexOf("Configuration[\"固定排名_學期成績排名計算\"][\"設定檔\"]") >= 0, "擇優設定檔留在學校設定階段");
            Expect(errors, school.IndexOf("PaintLoadStage()") > school.IndexOf("正在載入學生類別…") && school.IndexOf("BeginInvoke(new MethodInvoker(LoadStudentCategories))") > school.IndexOf("PaintLoadStage()"), "學生類別文字應先畫出，再排入訊息迴圈");

            string tags = Between(form, "void LoadStudentCategories", "void LoadStudentList");
            Expect(errors, tags.IndexOf("TagConfig.SelectByCategory(TagCategory.Student)") >= 0, "學生類別留在 UI 階段方法");
            Expect(errors, tags.IndexOf("PaintLoadStage()") > tags.IndexOf("正在載入學生清單…") && tags.IndexOf("BeginInvoke(new MethodInvoker(LoadStudentList))") > tags.IndexOf("PaintLoadStage()"), "學生清單文字應先畫出，再排入訊息迴圈");

            string students = Between(form, "void LoadStudentList", "void LoadGradeYears");
            Expect(errors, students.IndexOf("Student.SelectAll()") >= 0 && students.IndexOf("x.Class.GradeYear != null") >= 0, "學生清單與班級年級條件留在同一個 UI 階段方法");
            Expect(errors, students.IndexOf("PaintLoadStage()") > students.IndexOf("正在載入年級清單…") && students.IndexOf("BeginInvoke(new MethodInvoker(LoadGradeYears))") > students.IndexOf("PaintLoadStage()"), "年級清單文字應先畫出，再排入訊息迴圈");

            string grades = Between(form, "void LoadGradeYears", "void CalculateSemesterAssessmentRank_Load");
            Expect(errors, grades.IndexOf("x.Class.GradeYear") >= 0 && grades.IndexOf("listGradeYear.Items.Add") >= 0, "年級清單仍由已載入學生的 Class.GradeYear 整理");
            Expect(errors, grades.IndexOf("BackgroundWorker") < 0 && grades.IndexOf("btnNext.Enabled = true") < 0, "年級階段不啟用下一步，也不改走背景工作");
            return errors;
        }

        private static List<string> CheckRules(string form, string gate)
        {
            List<string> errors = new List<string>();
            string students = Between(form, "void LoadStudentList", "void LoadGradeYears");
            Expect(errors, students.IndexOf("x.Status == StudentRecord.StudentStatus.一般") >= 0 && students.IndexOf("!string.IsNullOrEmpty(x.RefClassID)") >= 0 && students.IndexOf("x.Class.GradeYear != null") >= 0, "學生篩選仍是一般、有班級且年級有值");
            string school = Between(form, "void LoadSchoolSettings", "void LoadStudentCategories");
            int empty = school.IndexOf("if (configString != \"\")");
            int clear = school.IndexOf("chbMakeUp.Checked = chbRetake.Checked = chbSYAdj.Checked = chbHandAdj.Checked = false");
            Expect(errors, empty >= 0 && clear > empty && school.IndexOf("補考成績") > clear && school.IndexOf("重修成績") > clear && school.IndexOf("學年調整成績") > clear && school.IndexOf("手動調整成績") > clear, "設定檔有內容時才先取消擇優項目再依檔案勾回");
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
                if (text.IndexOf("正在載入學校設定…") >= 0 || text.IndexOf("LoadSchoolSettings") >= 0)
                    errors.Add("定期評量或技職繁星不應套用學期成績載入階段：" + path);
            }
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
