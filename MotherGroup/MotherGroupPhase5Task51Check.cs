using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace SHEvaluation.Rank.MotherGroup
{
    public static class MotherGroupPhase5Task51Check
    {
        public static int Main(string[] args)
        {
            try
            {
                List<string> errors = new List<string>();
                errors.AddRange(CheckCatalog());
                errors.AddRange(CheckPeriodSeparation());
                errors.AddRange(CheckSql());
                errors.AddRange(CheckForm());
                errors.AddRange(CheckLayout());
                if (!SemesterRankWriteGate.CanWriteRankTables())
                    errors.Add("SemesterRankWriteGate 應允許寫入");
                if (errors.Count == 0)
                {
                    Console.WriteLine("phase5-task51-check: ok");
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

        private static List<string> CheckCatalog()
        {
            List<string> errors = new List<string>();
            string withSnapshot = Setting("113", "2", "學期成績", "<年級>1</年級><年級>2</年級><MotherGroupSnapshot version=\"1\" />");
            string withoutSnapshot = Setting("113", "2", "學期成績", "<年級>2</年級>");
            string versionTwo = Setting("113", "2", "學期成績", "<年級>3</年級><MotherGroupSnapshot version=\"2\" />");
            DataTable table = new DataTable();
            table.Columns.Add("id");
            table.Columns.Add("school_year");
            table.Columns.Add("semester");
            table.Columns.Add("calculation_description");
            table.Columns.Add("setting");
            AddRow(table, "91", "113", "2", "第一次計算", withSnapshot);
            AddRow(table, "92", "113", "2", "重算", withoutSnapshot);
            AddRow(table, "93", "113", "2", "舊版快照", versionTwo);
            AddRow(table, "94", "114", "1", "定期", Setting("114", "1", "定期評量", "<年級>1</年級>"));
            AddRow(table, "95", "114", "1", "繁星", "<Setting 學年度=\"114\" 學期=\"1\"><年級>3</年級></Setting>");
            AddRow(table, "96", "114", "1", "其他", Setting("114", "1", "期中考", "<年級>1</年級>"));
            AddRow(table, "97", "113", "1", "壞掉", "<Setting 考試名稱=\"學期成績\"");
            AddRow(table, "", "113", "2", "沒有識別", withSnapshot);

            List<HistoryRankBatch> batches = SemesterHistoryBatchCatalog.Read(table);
            Expect(errors, batches.Count == 3, "學期排名設定只留下考試名稱為學期成績的批次");
            Expect(errors, batches[0].Id == "91" && batches[1].Id == "92" && batches[2].Id == "93", "同一學年度學期的不同學期成績批次都應保留，且維持查詢順序");
            Expect(errors, batches[0].SourceType == "學期成績" && batches[0].ExamName == "學期成績" && batches[1].ExamName == "學期成績" && batches[2].ExamName == "學期成績", "清單只接受學期成績");
            Expect(errors, batches[0].DisplayText.IndexOf("91｜學期成績｜113學年度第2學期｜學期成績｜") == 0 && batches[0].DisplayText.IndexOf("建立時間") < 0, "清單應顯示來源類型、期間與評量名稱，且不稱作建立時間");
            Expect(errors, batches[0].SchoolYear == "113" && batches[0].Semester == "2" && batches[0].Description == "第一次計算", "應顯示學年度、學期與批次說明");
            Expect(errors, batches[0].GradeYears == "1、2" && batches[0].HasVersion1Snapshot, "應顯示排名年級與版本 1 快照");
            Expect(errors, batches[1].GradeYears == "2" && !batches[1].HasVersion1Snapshot && batches[1].Description == "重算", "沒有快照的學期批次仍應列出");
            Expect(errors, !batches[2].HasVersion1Snapshot && batches[2].GradeYears == "3", "版本不是 1 的快照不應算成版本 1");
            Expect(errors, batches[0].DisplayText.IndexOf("91") >= 0 && batches[1].DisplayText.IndexOf("92") >= 0 && batches[0].DisplayText != batches[1].DisplayText, "清單文字應能區分同一學年期的不同批次");
            bool foundExcluded = false;
            foreach (HistoryRankBatch batch in batches)
            {
                if (batch.Id == "94" || batch.Id == "95" || batch.Id == "96" || batch.Id == "97")
                    foundExcluded = true;
            }
            Expect(errors, !foundExcluded, "定期評量、沒有考試名稱的技職繁星與非法 XML 不應出現");
            Expect(errors, SemesterHistoryBatchCatalog.TryRead("98", "113", "2", "空白名稱", Setting("113", "2", "", "<年級>1</年級>")) == null, "考試名稱空白不應列入");
            Expect(errors, SemesterHistoryBatchCatalog.TryRead("99", "114", "2", "期中考", Setting("114", "2", "期中考", "<年級>1</年級>")) == null, "考試名稱不是學期成績不應列入");
            return errors;
        }

        private static List<string> CheckPeriodSeparation()
        {
            List<string> errors = new List<string>();
            CalculationPeriod period = CalculationPeriod.CreateDefault("114", "2");
            period.SetCalculationPeriod("114", "2");
            period.RememberHistoryBatch("91", "113", "1");
            Expect(errors, period.SchoolYear == "114" && period.Semester == "2", "選取歷史批次不應改寫計算學年度與學期");
            Expect(errors, period.HistoryBatchId == "91" && period.HistorySchoolYear == "113" && period.HistorySemester == "1", "歷史批次的學年度、學期與識別應另存");
            Expect(errors, MotherGroupSnapshot.KindFromSourceName(CalculationPeriod.HistorySourceName) == MotherGroupSourceKind.HistorySnapshot, "歷史來源名稱應對應歷史快照種類");
            Expect(errors, MotherGroupSnapshot.KindFromSourceName(CalculationPeriod.CurrentSourceName) == MotherGroupSourceKind.Current, "目前學生來源種類應維持不變");
            period.ClearHistoryBatch();
            Expect(errors, period.HistoryBatchId == "" && period.SchoolYear == "114" && period.Semester == "2", "清除歷史批次不應改寫計算期間");
            return errors;
        }

        private static List<string> CheckSql()
        {
            List<string> errors = new List<string>();
            string sql = SemesterHistoryBatchCatalog.SelectSql;
            Expect(errors, sql.IndexOf("SELECT") >= 0 && sql.IndexOf("FROM rank_batch") >= 0, "應查詢 rank_batch");
            Expect(errors, sql.IndexOf("id") >= 0 && sql.IndexOf("school_year") >= 0 && sql.IndexOf("semester") >= 0 && sql.IndexOf("calculation_description") >= 0 && sql.IndexOf("setting") >= 0, "查詢應取出識別、學年期、說明與設定");
            Expect(errors, sql.IndexOf("INSERT") < 0 && sql.IndexOf("UPDATE") < 0 && sql.IndexOf("DELETE") < 0, "歷史批次查詢不應寫入");
            Expect(errors, sql.IndexOf("DISTINCT") < 0 && sql.IndexOf("GROUP BY") < 0 && sql.IndexOf("WHERE") < 0 && sql.IndexOf("is_alive") < 0, "查詢不應只留最新一批或排除舊的無效批次");
            Expect(errors, sql.IndexOf("XMLTABLE") < 0 && sql.IndexOf("MERGE") < 0 && sql.IndexOf(";") < 0 && sql.IndexOf("BEGIN") < 0 && sql.IndexOf("COMMIT") < 0, "查詢應相容 PostgreSQL 9.6 到 17.1");
            return errors;
        }

        private static List<string> CheckForm()
        {
            List<string> errors = new List<string>();
            string source = File.ReadAllText("CalculateSemesterAssessmentRank.cs", Encoding.UTF8);
            int next = source.IndexOf("void btnNext_Click");
            int historyLoad = source.IndexOf("BeginHistorySnapshotRestore", next < 0 ? 0 : next);
            int currentLoad = source.IndexOf("new CurrentStudentMotherGroupSource()", next < 0 ? 0 : next);
            Expect(errors, next >= 0 && historyLoad > next && currentLoad > historyLoad, "歷史來源應走快照還原，且排在目前學生載入之前");
            int loadHistory = source.IndexOf("void LoadHistoryBatches");
            int afterLoad = loadHistory < 0 ? -1 : source.IndexOf("private void btnNext_Click", loadHistory);
            string loadBody = loadHistory >= 0 && afterLoad > loadHistory ? source.Substring(loadHistory, afterLoad - loadHistory) : "";
            Expect(errors, loadBody.IndexOf("new QueryHelper().Select(SemesterHistoryBatchCatalog.SelectSql)") >= 0, "正式歷史批次查詢應使用 SemesterHistoryBatchCatalog.SelectSql");
            Expect(errors, source.IndexOf("RememberHistoryBatch") >= 0 && source.IndexOf("_nudCalcSchoolYear.Value = _nudHistorySchoolYear") < 0 && source.IndexOf("_nudCalcSchoolYear.Value = selected") < 0, "歷史學年度不應寫進計算學年度控制項");
            Expect(errors, source.IndexOf("ShowEditableCalculation()") >= 0 && source.IndexOf("_editableCalculation.SchoolYear") >= 0, "恢復的是先前可編輯的計算期間，不是歷史來源學年度");
            string gate = File.ReadAllText("MotherGroup\\SemesterRankWriteGate.cs", Encoding.UTF8);
            Expect(errors, gate.IndexOf("return true;") >= 0 && gate.IndexOf("return false;") < 0, "SemesterRankWriteGate 應回傳 true");
            string catalog = File.ReadAllText("MotherGroup\\SemesterHistoryBatchCatalog.cs", Encoding.UTF8);
            Expect(errors, catalog.IndexOf("INSERT") < 0 && catalog.IndexOf("UPDATE") < 0 && catalog.IndexOf("DELETE") < 0, "批次目錄不應含寫入語句");
            return errors;
        }

        private static List<string> CheckLayout()
        {
            List<string> errors = new List<string>();
            using (Form form = new Form())
            {
                form.ShowInTaskbar = false;
                form.ClientSize = new Size(865, 471);
                form.Show();
                Panel panel = new Panel();
                panel.Dock = DockStyle.Fill;
                form.Controls.Add(panel);
                Label yearCaption = new Label();
                yearCaption.Text = "學年度：";
                NumericUpDown schoolYear = new NumericUpDown();
                schoolYear.Minimum = 1;
                schoolYear.Maximum = 9999;
                schoolYear.Value = 114;
                Label semesterCaption = new Label();
                semesterCaption.Text = "學期：";
                ComboBox semester = new ComboBox();
                Label sourceCaption = new Label();
                sourceCaption.Text = "母群來源：";
                ComboBox source = new ComboBox();
                Label historyYearCaption = new Label();
                historyYearCaption.Text = "歷史來源學年度：";
                NumericUpDown historyYear = new NumericUpDown();
                historyYear.Minimum = 1;
                historyYear.Maximum = 9999;
                historyYear.Enabled = false;
                historyYear.Value = 1;
                Label historySemesterCaption = new Label();
                historySemesterCaption.Text = "歷史來源學期：";
                ComboBox historySemester = new ComboBox();
                historySemester.Enabled = false;
                Label batchCaption = new Label();
                batchCaption.Text = "歷史批次：";
                ComboBox batch = new ComboBox();
                Panel rankPeople = new Panel();
                Button next = new Button();
                next.SetBounds(778, 434, 75, 25);
                next.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
                panel.Controls.AddRange(new Control[]
                {
                    yearCaption, schoolYear, semesterCaption, semester, sourceCaption, source,
                    historyYearCaption, historyYear, historySemesterCaption, historySemester,
                    batchCaption, batch, rankPeople, next
                });
                SemesterRankScreenLayout.ArrangeFigure1(
                    panel, yearCaption, schoolYear, semesterCaption, semester, sourceCaption, source,
                    historyYearCaption, historyYear, historySemesterCaption, historySemester, rankPeople, next,
                    batchCaption, batch);
                Expect(errors, schoolYear.Value == 114 && !historyYear.Enabled, "排列歷史批次清單不應改計算學年度，也不啟用歷史學年度");
                Expect(errors, batch.Bottom <= rankPeople.Top && rankPeople.Bottom <= next.Top, "歷史批次清單應位於排名對象上方，且不蓋住下一步");
                Expect(errors, batch.Width > 200, "歷史批次清單應有足夠寬度顯示批次內容");
            }
            return errors;
        }

        private static string Setting(string schoolYear, string semester, string examName, string body)
        {
            return "<Setting 學年度=\"" + schoolYear + "\" 學期=\"" + semester + "\" 考試名稱=\"" + examName + "\">" + body + "</Setting>";
        }

        private static void AddRow(DataTable table, string id, string schoolYear, string semester, string description, string setting)
        {
            table.Rows.Add(id, schoolYear, semester, description, setting);
        }

        private static void Expect(List<string> errors, bool condition, string message)
        {
            if (!condition)
                errors.Add(message);
        }
    }
}
