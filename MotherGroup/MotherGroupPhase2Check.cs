using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SHEvaluation.Rank.MotherGroup
{
    public static class MotherGroupPhase2Check
    {
        [STAThread]
        public static int Main(string[] args)
        {
            try
            {
            List<string> errors = new List<string>();
            errors.AddRange(CheckCalculationPeriod());
            errors.AddRange(CheckConfirmList());
            errors.AddRange(CheckGrid());
            errors.AddRange(CheckLayout());
            if (!SemesterRankWriteGate.CanWriteRankTables())
                errors.Add("SemesterRankWriteGate 應允許寫入");
            if (errors.Count == 0)
            {
                Console.WriteLine("phase2-check: ok");
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

        public static List<string> CheckCalculationPeriod()
        {
            List<string> errors = new List<string>();
            CalculationPeriod period = CalculationPeriod.CreateDefault("114", "2");
            NumericUpDown year = new NumericUpDown();
            ComboBox semester = new ComboBox();
            ComboBox source = new ComboBox();
            NumericUpDown historyYear = new NumericUpDown();
            ComboBox historySemester = new ComboBox();
            CalculationPeriodBinder.Apply(year, semester, source, historyYear, historySemester, period);

            Expect(errors, year.Value == 114, "計算學年度應預設系統目前值");
            Expect(errors, semester.Items.Count == 2 && semester.Items[0].ToString() == "1" && semester.Items[1].ToString() == "2", "學期只能選擇 1 或 2");
            Expect(errors, semester.SelectedItem.ToString() == "2", "計算學期應預設系統目前值");
            Expect(errors, source.SelectedItem.ToString() == CalculationPeriod.CurrentSourceName, "母群來源應預設依目前學生資料產生");
            Expect(errors, source.Items.Count == 3 && source.Items[1].ToString() == CalculationPeriod.HistorySourceName && source.Items[2].ToString() == CalculationPeriod.ExcelSourceName, "母群來源應可改選歷史學期成績排名與 Excel 母群清單");
            Expect(errors, !historyYear.Enabled && !historySemester.Enabled, "目前學生來源初始應停用歷史期間");
            Expect(errors, historyYear.Value == 114 && (string)historySemester.SelectedItem == "2", "歷史期間應預設系統學年度與學期，不應寫死為 1");
            Expect(errors, period.HistorySchoolYear == "114" && period.HistorySemester == "2", "歷史期間模型應與初始化欄位一致");

            historyYear.Value = 113;
            historySemester.SelectedItem = "1";
            period.SetHistoryPeriod("113", "1");
            Expect(errors, year.Value == 114 && semester.SelectedItem.ToString() == "2", "歷史來源期間控制項不應改寫計算學年度與學期");
            Expect(errors, period.SchoolYear == "114" && period.Semester == "2", "歷史來源期間不應改寫計算期間");
            Expect(errors, period.HistorySchoolYear == "113" && period.HistorySemester == "1", "歷史來源期間應另存");

            CalculationPeriod first = CalculationPeriod.CreateDefault("113", "1");
            Expect(errors, first.Semester == "1" && first.SourceName == CalculationPeriod.CurrentSourceName, "系統學期 1 時應維持第 1 學期");
            return errors;
        }

        public static List<string> CheckConfirmList()
        {
            List<string> errors = new List<string>();
            StudentMotherGroup excluded = BuildCurrent("11", "S11", "李", "忠", true, "普通科", "第一", "");
            StudentMotherGroup included = BuildCurrent("10", "S10", "王", "忠", false, "普通科", "第二", "社團");
            MotherGroupConfirmList list = new MotherGroupConfirmList();
            list.Load(new ConfirmStudentRow[]
            {
                new ConfirmStudentRow { Group = excluded, ClassName = "忠", SeatNo = "1" },
                new ConfirmStudentRow { Group = included, ClassName = "忠", SeatNo = "2" }
            });

            Expect(errors, list.Rows.Count == 2, "不排名學生應留在確認清單");
            foreach (string rankType in RankKinds.All)
                Expect(errors, !excluded.GetWorking(rankType).Participate, "不排名學生的" + rankType + "應預設不參與");
            Expect(errors, excluded.GetWorking(RankKinds.Year).GroupName == "1年級", "不排名學生的年排名名稱應保留");
            Expect(errors, included.GetWorking(RankKinds.Tag1).GroupName == "第二", "類別應沿用 First() 的第一筆");

            list.SetParticipate(new string[] { "11" }, RankKinds.Year, true);
            Expect(errors, excluded.GetWorking(RankKinds.Year).Participate && excluded.GetWorking(RankKinds.Year).GroupName == "1年級", "個別改為參與後應保留母群名稱");
            Expect(errors, !excluded.GetWorking(RankKinds.Class).Participate, "個別調整不應改到其他排名");

            list.SetParticipate(new string[] { "10", "11" }, RankKinds.Class, false);
            Expect(errors, !included.GetWorking(RankKinds.Class).Participate && included.GetWorking(RankKinds.Year).Participate, "多選只應改班排名");
            Expect(errors, included.GetWorking(RankKinds.Class).GroupName == "忠", "多選不應改母群名稱");

            list.Restore();
            Expect(errors, !excluded.GetWorking(RankKinds.Year).Participate, "還原後不排名學生的年排名應回到初始不參與");
            Expect(errors, included.GetWorking(RankKinds.Class).Participate && included.GetWorking(RankKinds.Class).GroupName == "忠", "還原後班排名應回到進入清單時的參與與名稱");

            MotherGroupCountSummary beforeSearch = list.Summarize();
            List<string> visible = list.VisibleStudentIds("S10");
            MotherGroupCountSummary afterSearch = list.Summarize();
            Expect(errors, visible.Count == 1 && visible[0] == "10", "搜尋應只挑出符合的學生");
            Expect(errors, list.Rows.Count == 2 && beforeSearch.StudentCount == 2 && afterSearch.StudentCount == 2, "搜尋不應改變完整清單或學生總人數");

            RankPopulationCount classRank = afterSearch.Ranks[2];
            Expect(errors, classRank.RankType == RankKinds.Class, "第三種排名應是班排名");
            Expect(errors, classRank.NamedGroups.Count == 1 && classRank.NamedGroups[0].GroupName == "忠", "同名班母群應合併");
            Expect(errors, classRank.NamedGroups[0].TotalCount == 2 && classRank.NamedGroups[0].ParticipateCount == 1 && classRank.NamedGroups[0].ExcludeCount == 1, "同名母群總人數應包含不參與者");

            included.GetWorking(RankKinds.Tag2).Participate = false;
            MotherGroupCountSummary tagSummary = list.Summarize();
            RankPopulationCount tag2 = tagSummary.Ranks[4];
            Expect(errors, tag2.NamedGroups.Count == 1 && tag2.NamedGroups[0].GroupName == "社團", "空白類別名稱不應計入具名母群");
            Expect(errors, tag2.NamedGroups[0].TotalCount == 1 && tag2.ParticipateCount == 0 && tag2.ExcludeCount == 2, "排名參與與排除人數應以完整工作副本計算");
            string fixedSummary = MotherGroupConfirmList.FormatFixedSummary(tagSummary);
            ExpectRankLines(errors, fixedSummary);
            Expect(errors, fixedSummary.IndexOf("母群 ") < 0, "固定摘要不放入各母群名稱人數");

            MotherGroupCountSummary big = new MotherGroupCountSummary();
            big.StudentCount = 1235;
            big.Ranks = new List<RankPopulationCount>();
            foreach (string rankType in RankKinds.All)
            {
                RankPopulationCount rank = new RankPopulationCount();
                rank.RankType = rankType;
                rank.ParticipateCount = rankType == RankKinds.Year ? 1200 : 8;
                rank.ExcludeCount = rankType == RankKinds.Year ? 35 : 1227;
                rank.NamedGroups = new List<NamedGroupCount>();
                big.Ranks.Add(rank);
            }
            string bigText = MotherGroupConfirmList.FormatFixedSummary(big);
            ExpectRankLines(errors, bigText);
            Expect(errors, bigText.Contains("學生總人數 1235") && bigText.Contains("年排名 參與 1200／排除 35") && bigText.Contains("類別二 參與 8／排除 1227"), "人數變大時仍應完整顯示每一位數字");
            using (Form form = new Form())
            {
                form.ShowInTaskbar = false;
                form.Show();
                DataGridView population = new DataGridView();
                population.Size = new Size(400, MotherGroupConfirmGrid.PopulationAreaHeight());
                form.Controls.Add(population);
                MotherGroupConfirmGrid.ConfigurePopulation(population);
                MotherGroupConfirmGrid.FillPopulation(population, big);
                Expect(errors, population.Rows.Count == 5, "五種排名應各占表格一列");
                Expect(errors, PopulationValue(population, RankKinds.Year, "populationParticipate") == "1200" && PopulationValue(population, RankKinds.Year, "populationExclude") == "35", "年排名表格應顯示完整的參與與排除人數");
                Expect(errors, PopulationValue(population, RankKinds.Tag2, "populationExclude") == "1227", "類別二排除人數變大時不應被截斷");
                form.Close();
            }
            return errors;
        }

        public static List<string> CheckGrid()
        {
            List<string> errors = new List<string>();
            using (Form form = new Form())
            {
                form.ShowInTaskbar = false;
                form.Show();
                DataGridView grid = new DataGridView();
                grid.Size = new System.Drawing.Size(900, 240);
                form.Controls.Add(grid);
                Label summary = new Label();
                summary.AutoSize = false;
                summary.Size = new System.Drawing.Size(700, 24);
                form.Controls.Add(summary);
                DataGridView population = new DataGridView();
                population.Size = new System.Drawing.Size(400, MotherGroupConfirmGrid.PopulationAreaHeight());
                form.Controls.Add(population);
                MotherGroupConfirmGrid.ConfigurePopulation(population);
                DataGridView detail = new DataGridView();
                detail.Size = new System.Drawing.Size(700, 96);
                form.Controls.Add(detail);
                MotherGroupConfirmGrid.ConfigureDetail(detail);

                StudentMotherGroup excluded = BuildCurrent("11", "S11", "李", "忠", true, "普通科", "第一", "");
                StudentMotherGroup included = BuildCurrent("10", "S10", "王", "忠", false, "普通科", "第二", "社團");
                MotherGroupConfirmList list = new MotherGroupConfirmList();
                list.Load(new ConfirmStudentRow[]
                {
                    new ConfirmStudentRow { Group = excluded, ClassName = "忠", SeatNo = "1" },
                    new ConfirmStudentRow { Group = included, ClassName = "忠", SeatNo = "2" }
                });
                MotherGroupConfirmGrid.Configure(grid);
                MotherGroupConfirmGrid.Show(grid, list.Rows);
                MotherGroupConfirmGrid.RefreshSummary(summary, population, detail, list);

                Expect(errors, summary.Text == "學生總人數 2", "學生總人數單獨顯示，不與五種排名放在同一行");
                Expect(errors, population.Rows.Count == 5 && PopulationValue(population, RankKinds.Year, "populationParticipate") == "1", "五種排名各有一列參與與排除人數");
                Expect(errors, summary.Text.IndexOf("母群 ") < 0, "固定摘要不放入各母群名稱人數");
                Expect(errors, detail.ScrollBars == ScrollBars.Both && detail.Rows.Count > 0, "各母群人數放在可捲動明細");
                int columnWidth = 0;
                foreach (DataGridViewColumn column in grid.Columns)
                    columnWidth += column.Width;
                Expect(errors, columnWidth > grid.ClientSize.Width, "學生表格欄寬超過可見寬度，應可水平捲動");
                Expect(errors, grid.Columns[MotherGroupConfirmGrid.ParticipateColumn(RankKinds.Year)] is DataGridViewCheckBoxColumn, "年排名參與應是 CheckBox");

                Expect(errors, grid.Columns[MotherGroupConfirmGrid.NameColumn(RankKinds.Year)].ReadOnly, "年排名母群名稱應唯讀");
                Expect(errors, !grid.Columns[MotherGroupConfirmGrid.ParticipateColumn(RankKinds.Year)].ReadOnly, "年排名參與應可修改");
                Expect(errors, grid.Rows.Count == 2, "畫面應列出含不排名學生的兩列");
                Expect(errors, (bool)grid.Rows[0].Cells[MotherGroupConfirmGrid.ParticipateColumn(RankKinds.Year)].Value == false, "畫面上的不排名學生應顯示不參與");

                int yearColumn = grid.Columns[MotherGroupConfirmGrid.ParticipateColumn(RankKinds.Year)].Index;
                grid.Rows[0].Cells[yearColumn].Value = true;
                MotherGroupConfirmGrid.ApplyCellEdit(grid, list, 0, yearColumn);
                MotherGroupConfirmGrid.RefreshSummary(summary, population, detail, list);
                Expect(errors, excluded.GetWorking(RankKinds.Year).Participate && excluded.GetOriginal(RankKinds.Year).GroupName == "1年級", "勾選參與應只改工作副本");
                Expect(errors, summary.Text == "學生總人數 2" && PopulationValue(population, RankKinds.Year, "populationParticipate") == "2" && PopulationValue(population, RankKinds.Year, "populationExclude") == "0", "個別勾選後五種人數應同步");
                Expect(errors, DetailValue(detail, RankKinds.Year, "1年級", "detailParticipate") == "2", "個別勾選後母群明細應同步");

                grid.ClearSelection();
                grid.Rows[0].Selected = true;
                grid.Rows[1].Selected = true;
                MotherGroupConfirmGrid.ApplyToSelected(grid, list, RankKinds.Department, false);
                MotherGroupConfirmGrid.RefreshSummary(summary, population, detail, list);
                string summaryBeforeSearch = summary.Text;
                string yearParticipateBeforeSearch = PopulationValue(population, RankKinds.Year, "populationParticipate");
                int detailCountBeforeSearch = detail.Rows.Count;
                Expect(errors, !excluded.GetWorking(RankKinds.Department).Participate && !included.GetWorking(RankKinds.Department).Participate, "右鍵批次應改多選學生的科排名");
                Expect(errors, included.GetWorking(RankKinds.Year).Participate, "批次設定科排名不應改年排名");
                Expect(errors, (bool)grid.Rows[1].Cells[MotherGroupConfirmGrid.ParticipateColumn(RankKinds.Department)].Value == false, "批次設定應回寫畫面核取方塊");

                MotherGroupConfirmGrid.ApplyVisibility(grid, list.VisibleStudentIds("S10"));
                Expect(errors, !grid.Rows[0].Visible && grid.Rows[1].Visible, "搜尋應只隱藏不符合的畫面列");
                Expect(errors, list.Rows.Count == 2 && summary.Text == summaryBeforeSearch && detail.Rows.Count == detailCountBeforeSearch && PopulationValue(population, RankKinds.Year, "populationParticipate") == yearParticipateBeforeSearch, "搜尋不應重算或改寫完整人數");

                list.Restore();
                MotherGroupConfirmGrid.RefreshParticipation(grid, list);
                Expect(errors, (bool)grid.Rows[0].Cells[yearColumn].Value == false, "還原後畫面應回到初始不參與");
                Expect(errors, grid.Rows[0].Cells[MotherGroupConfirmGrid.NameColumn(RankKinds.Year)].Value.ToString() == "1年級", "還原不應改母群名稱");
                form.Close();
            }
            return errors;
        }

        public static List<string> CheckLayout()
        {
            List<string> errors = new List<string>();
            bool nullArrange = true;
            try
            {
                SemesterRankScreenLayout.ArrangeFigure1(null, null, null, null, null, null, null, null, null, null, null, null, null);
                SemesterRankScreenLayout.ArrangeFigure2(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null);
            }
            catch (Exception ex)
            {
                nullArrange = false;
                errors.Add("控制項尚未建立時排列擲出例外：" + ex.GetType().Name);
            }
            Expect(errors, nullArrange, "初始化期間排列應略過尚未建立的控制項");

            using (Form form = new Form())
            {
                form.ShowInTaskbar = false;
                form.ClientSize = new Size(865, 471);
                form.Show();

                Panel figure1 = new Panel();
                figure1.Dock = DockStyle.Fill;
                form.Controls.Add(figure1);
                Label yearCaption = NamedLabel("yearCaption", 12, 12, 69, 24);
                NumericUpDown schoolYear = new NumericUpDown();
                schoolYear.Name = "schoolYear";
                Label semesterCaption = NamedLabel("semesterCaption", 188, 12, 54, 24);
                ComboBox semester = new ComboBox();
                semester.Name = "semester";
                Label sourceCaption = new Label();
                sourceCaption.Name = "sourceCaption";
                sourceCaption.Text = "母群來源：";
                ComboBox source = new ComboBox();
                source.Name = "source";
                Label historyYearCaption = new Label();
                historyYearCaption.Name = "historyYearCaption";
                historyYearCaption.Text = "歷史來源學年度：";
                NumericUpDown historyYear = new NumericUpDown();
                historyYear.Name = "historyYear";
                Label historySemesterCaption = new Label();
                historySemesterCaption.Name = "historySemesterCaption";
                historySemesterCaption.Text = "歷史來源學期：";
                ComboBox historySemester = new ComboBox();
                historySemester.Name = "historySemester";
                Panel rankPeople = new Panel();
                rankPeople.Name = "rankPeople";
                rankPeople.SetBounds(12, 53, 841, 375);
                Button next = NamedButton("next", 778, 434, 75, 25);
                next.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
                figure1.Controls.AddRange(new Control[]
                {
                    yearCaption, schoolYear, semesterCaption, semester, sourceCaption, source,
                    historyYearCaption, historyYear, historySemesterCaption, historySemester, rankPeople, next
                });
                SemesterRankScreenLayout.ArrangeFigure1(
                    figure1, yearCaption, schoolYear, semesterCaption, semester, sourceCaption, source,
                    historyYearCaption, historyYear, historySemesterCaption, historySemester, rankPeople, next);
                CollectOverlaps(errors, figure1, "圖1");
                Expect(errors, rankPeople.Bottom <= next.Top, "圖1排名對象應位於下一步按鈕上方");
                Expect(errors, rankPeople.Anchor == AnchorStyles.None && schoolYear.Anchor == AnchorStyles.None, "圖1動態控制項不使用 Anchor 拉伸");
                Rectangle rankAtDesign = rankPeople.Bounds;
                form.ClientSize = new Size(1200, 760);
                SemesterRankScreenLayout.ArrangeFigure1(
                    figure1, yearCaption, schoolYear, semesterCaption, semester, sourceCaption, source,
                    historyYearCaption, historyYear, historySemesterCaption, historySemester, rankPeople, next);
                CollectOverlaps(errors, figure1, "圖1放大");
                Expect(errors, rankPeople.Width > rankAtDesign.Width && rankPeople.Bottom <= next.Top, "圖1放大後排名對象應變寬且留在下一步上方");
                form.ClientSize = new Size(865, 471);
                SemesterRankScreenLayout.ArrangeFigure1(
                    figure1, yearCaption, schoolYear, semesterCaption, semester, sourceCaption, source,
                    historyYearCaption, historyYear, historySemesterCaption, historySemester, rankPeople, next);
                CollectOverlaps(errors, figure1, "圖1縮回");
                Expect(errors, rankPeople.Bounds == rankAtDesign, "圖1縮回原尺寸後排列應回到原位置");

                form.Controls.Remove(figure1);
                Panel figure2 = new Panel();
                figure2.Dock = DockStyle.Fill;
                form.Controls.Add(figure2);
                Label schoolYearValue = NamedLabel("schoolYearValue", 80, 12, 54, 24);
                Label semesterValue = NamedLabel("semesterValue", 199, 12, 38, 24);
                Label searchCaption = new Label();
                searchCaption.Name = "searchCaption";
                searchCaption.Text = "搜尋：";
                TextBox searchBox = new TextBox();
                searchBox.Name = "searchBox";
                Label groupCaption = NamedLabel("groupCaption", 12, 40, 85, 24);
                Label summary = new Label();
                summary.Name = "summary";
                DataGridView population = new DataGridView();
                population.Name = "population";
                MotherGroupConfirmGrid.ConfigurePopulation(population);
                DataGridView detail = new DataGridView();
                detail.Name = "detail";
                DataGridView students = new DataGridView();
                students.Name = "students";
                MotherGroupConfirmGrid.Configure(students);
                Button restore = NamedButton("restore", 0, 0, 110, 25);
                Button previous = NamedButton("previous", 681, 434, 75, 25);
                Button import = NamedButton("import", 590, 434, 75, 25);
                Button export = NamedButton("export", 499, 434, 75, 25);
                Button calculate = NamedButton("calculate", 772, 434, 81, 25);
                import.Visible = false;
                export.Visible = false;
                previous.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
                import.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
                export.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
                calculate.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
                Label yearCaption2 = NamedLabel("yearCaption2", 12, 12, 69, 24);
                Label semesterCaption2 = NamedLabel("semesterCaption2", 146, 12, 54, 24);
                figure2.Controls.AddRange(new Control[]
                {
                    yearCaption2, schoolYearValue, semesterCaption2, semesterValue,
                    searchCaption, searchBox, groupCaption, summary, population, detail, students,
                    restore, previous, import, export, calculate
                });
                students.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                SemesterRankScreenLayout.ArrangeFigure2(
                    figure2, yearCaption2, schoolYearValue, semesterCaption2, semesterValue,
                    searchCaption, searchBox, groupCaption, summary, population, detail, students,
                    restore, previous, import, export);
                CollectOverlaps(errors, figure2, "圖2");
                Expect(errors, summary.Height == 24, "學生總人數只占一行");
                Expect(errors, population.Rows.Count == 5 && population.Height == MotherGroupConfirmGrid.PopulationAreaHeight() && population.ScrollBars == ScrollBars.None, "五種排名人數使用固定高度表格且不需捲動");
                Expect(errors, summary.Bottom <= population.Top && population.Bottom <= detail.Top && detail.Bottom <= students.Top, "人數表不遮蓋母群明細或學生清單");
                Expect(errors, students.Bottom <= previous.Top, "圖2表格應位於底部按鈕上方");
                Expect(errors, restore.Right <= import.Left, "還原按鈕不應與匯入按鈕重疊");
                Expect(errors, students.Anchor == AnchorStyles.None && population.Anchor == AnchorStyles.None && restore.Anchor == AnchorStyles.None && searchBox.Anchor == AnchorStyles.None, "圖2動態控制項不使用 Anchor 拉伸");
                Expect(errors, previous.Anchor == (AnchorStyles.Bottom | AnchorStyles.Right), "既有底部按鈕維持原有 Anchor");
                int needed = population.ColumnHeadersHeight;
                foreach (DataGridViewRow row in population.Rows)
                    needed += row.Height;
                Expect(errors, population.ClientSize.Height >= needed, "五種排名列應完整落在人數表可見範圍");
                Rectangle populationAtDesign = population.Bounds;
                Rectangle studentsAtDesign = students.Bounds;
                int widthBefore = students.Width;
                int heightBefore = students.Height;
                form.ClientSize = new Size(1200, 760);
                SemesterRankScreenLayout.ArrangeFigure2(
                    figure2, yearCaption2, schoolYearValue, semesterCaption2, semesterValue,
                    searchCaption, searchBox, groupCaption, summary, population, detail, students,
                    restore, previous, import, export);
                CollectOverlaps(errors, figure2, "圖2放大");
                Expect(errors, students.Width > widthBefore && students.Height > heightBefore, "視窗放大後學生表格應變大");
                Expect(errors, population.Height == populationAtDesign.Height && population.Bottom <= detail.Top && detail.Bottom <= students.Top, "放大後人數表高度不變且不遮蓋下方清單");
                Expect(errors, students.Bottom <= previous.Top && previous.Right <= figure2.ClientSize.Width && calculate.Right <= figure2.ClientSize.Width, "放大後按鈕應留在視窗內且不壓到表格");
                int columnWidth = 0;
                foreach (DataGridViewColumn column in students.Columns)
                    columnWidth += column.Width;
                Expect(errors, columnWidth > students.ClientSize.Width, "放大後學生表格仍應保留水平捲動");
                form.ClientSize = new Size(865, 471);
                SemesterRankScreenLayout.ArrangeFigure2(
                    figure2, yearCaption2, schoolYearValue, semesterCaption2, semesterValue,
                    searchCaption, searchBox, groupCaption, summary, population, detail, students,
                    restore, previous, import, export);
                CollectOverlaps(errors, figure2, "圖2縮回");
                Expect(errors, population.Bounds == populationAtDesign && students.Bounds == studentsAtDesign, "圖2縮回原尺寸後排列應回到原位置");
                form.Close();
            }
            return errors;
        }

        private static void ExpectRankLines(List<string> errors, string text)
        {
            string[] lines = text.Replace("\r", "").Split('\n');
            Expect(errors, lines.Length == 6, "學生總人數與五種排名應各占一行");
            string[] captions = new string[] { "年排名", "科排名", "班排名", "類別一", "類別二" };
            Expect(errors, lines.Length > 0 && lines[0].StartsWith("學生總人數") && lines[0].IndexOf("年排名") < 0, "第一行只顯示學生總人數");
            if (lines.Length < 6)
                return;
            for (int i = 0; i < captions.Length; i++)
            {
                int hits = 0;
                foreach (string caption in captions)
                {
                    if (lines[i + 1].Contains(caption))
                        hits++;
                }
                Expect(errors, lines[i + 1].StartsWith(captions[i] + " 參與") && hits == 1, "每一種排名應獨占一行：" + captions[i]);
            }
        }

        private static string PopulationValue(DataGridView population, string rankType, string column)
        {
            foreach (DataGridViewRow row in population.Rows)
            {
                if ((row.Tag as string) == rankType)
                    return row.Cells[column].Value + "";
            }
            return "";
        }

        private static string DetailValue(DataGridView detail, string rank, string name, string column)
        {
            foreach (DataGridViewRow row in detail.Rows)
            {
                if ((row.Cells["detailRank"].Value + "") == rank && (row.Cells["detailName"].Value + "") == name)
                    return row.Cells[column].Value + "";
            }
            return "";
        }

        private static void CollectOverlaps(List<string> errors, Control parent, string figure)
        {
            List<Control> visible = new List<Control>();
            foreach (Control control in parent.Controls)
            {
                if (!control.Visible || control.Width <= 0 || control.Height <= 0)
                    continue;
                visible.Add(control);
                if (control.Left < 0 || control.Top < 0 || control.Right > parent.ClientSize.Width + 1 || control.Bottom > parent.ClientSize.Height + 1)
                    errors.Add(figure + " 控制項超出面板：" + control.Name + " " + control.Bounds);
            }
            for (int i = 0; i < visible.Count; i++)
            {
                for (int j = i + 1; j < visible.Count; j++)
                {
                    if (visible[i].Bounds.IntersectsWith(visible[j].Bounds))
                        errors.Add(figure + " 控制項重疊：" + visible[i].Name + " " + visible[i].Bounds + " 與 " + visible[j].Name + " " + visible[j].Bounds);
                }
            }
        }

        private static Label NamedLabel(string name, int x, int y, int width, int height)
        {
            Label label = new Label();
            label.Name = name;
            label.AutoSize = false;
            label.SetBounds(x, y, width, height);
            return label;
        }

        private static Button NamedButton(string name, int x, int y, int width, int height)
        {
            Button button = new Button();
            button.Name = name;
            button.SetBounds(x, y, width, height);
            return button;
        }

        private static StudentMotherGroup BuildCurrent(string id, string number, string name, string className, bool excluded, string dept, string tag1, string tag2)
        {
            List<StudentTagLink> links = new List<StudentTagLink>();
            List<TagDefinition> tags = new List<TagDefinition>();
            if (excluded)
            {
                tags.Add(new TagDefinition { Id = "ex", Prefix = "不排名", Name = "休學" });
                links.Add(new StudentTagLink { StudentId = id, TagId = "ex", Name = "休學" });
            }
            tags.Add(new TagDefinition { Id = "b", Prefix = "學程", Name = "第二" });
            tags.Add(new TagDefinition { Id = "a", Prefix = "學程", Name = "第一" });
            tags.Add(new TagDefinition { Id = "c", Prefix = "", Name = "社團" });
            if (tag1 == "第二")
            {
                links.Add(new StudentTagLink { StudentId = id, TagId = "b", Name = "第二" });
                links.Add(new StudentTagLink { StudentId = id, TagId = "a", Name = "第一" });
            }
            else if (tag1 == "第一")
            {
                links.Add(new StudentTagLink { StudentId = id, TagId = "a", Name = "第一" });
            }
            if (tag2 == "社團")
                links.Add(new StudentTagLink { StudentId = id, TagId = "c", Name = "社團" });

            List<StudentMotherGroup> groups = CurrentStudentMotherGroupBuilder.Build(
                new CurrentStudentRow[]
                {
                    new CurrentStudentRow { StudentId = id, StudentNumber = number, StudentName = name, IsRegular = true, RefClassId = "c1", GradeYear = 1, ClassName = className }
                },
                tags,
                links,
                new int[] { 1 },
                "不排名",
                "學程",
                "社團",
                new Dictionary<string, string> { { id, dept } });
            MotherGroupInitialParticipation.Apply(MotherGroupSourceKind.Current, groups[0], groups[0].MatchesExcludedCategory);
            return groups[0];
        }

        private static void Expect(List<string> errors, bool condition, string message)
        {
            if (!condition)
                errors.Add(message);
        }
    }
}
