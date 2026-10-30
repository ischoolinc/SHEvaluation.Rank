using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace SHEvaluation.Rank.MotherGroup
{
    public static class MotherGroupConfirmGrid
    {
        public static void Configure(DataGridView grid)
        {
            grid.Columns.Clear();
            grid.AutoGenerateColumns = false;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.ReadOnly = false;
            grid.MultiSelect = true;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.RowHeadersVisible = false;
            grid.EditMode = DataGridViewEditMode.EditOnEnter;
            grid.ScrollBars = ScrollBars.Both;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            AddText(grid, "colClass", "班級", 70);
            AddText(grid, "colSeat", "座號", 60);
            AddText(grid, "colNumber", "學號", 90);
            AddText(grid, "colName", "姓名", 80);
            AddText(grid, "colConflict", "母群衝突", 220);
            AddRank(grid, RankKinds.Year, "母群：年排名", "年排名參與");
            AddRank(grid, RankKinds.Department, "母群：科排名", "科排名參與");
            AddRank(grid, RankKinds.Class, "母群：班排名", "班排名參與");
            AddRank(grid, RankKinds.Tag1, "母群：類別一", "類別一參與");
            AddRank(grid, RankKinds.Tag2, "母群：類別二", "類別二參與");
        }

        public const int PopulationHeaderHeight = 24;
        public const int PopulationRowHeight = 22;

        public static int PopulationAreaHeight()
        {
            return PopulationHeaderHeight + PopulationRowHeight * RankKinds.All.Length + 4;
        }

        public static void ConfigurePopulation(DataGridView population)
        {
            population.Columns.Clear();
            population.AutoGenerateColumns = false;
            population.AllowUserToAddRows = false;
            population.AllowUserToDeleteRows = false;
            population.ReadOnly = true;
            population.MultiSelect = false;
            population.RowHeadersVisible = false;
            population.ScrollBars = ScrollBars.None;
            population.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            population.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            population.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            population.ColumnHeadersHeight = PopulationHeaderHeight;
            population.RowTemplate.Height = PopulationRowHeight;
            population.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            population.AllowUserToResizeColumns = false;
            population.AllowUserToResizeRows = false;
            AddText(population, "populationRank", "排名", 120);
            AddText(population, "populationParticipate", "參與人數", 110);
            AddText(population, "populationExclude", "排除人數", 110);
            population.Rows.Clear();
            foreach (string rankType in RankKinds.All)
            {
                int index = population.Rows.Add();
                DataGridViewRow row = population.Rows[index];
                row.Tag = rankType;
                row.Height = PopulationRowHeight;
                row.Cells["populationRank"].Value = MotherGroupConfirmList.RankCaption(rankType);
                row.Cells["populationParticipate"].Value = "0";
                row.Cells["populationExclude"].Value = "0";
            }
        }

        public static void ConfigureDetail(DataGridView detail)
        {
            detail.Columns.Clear();
            detail.AutoGenerateColumns = false;
            detail.AllowUserToAddRows = false;
            detail.AllowUserToDeleteRows = false;
            detail.ReadOnly = true;
            detail.MultiSelect = false;
            detail.RowHeadersVisible = false;
            detail.ScrollBars = ScrollBars.Both;
            detail.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            detail.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            AddText(detail, "detailRank", "排名", 100);
            AddText(detail, "detailName", "母群名稱", 180);
            AddText(detail, "detailTotal", "母群人數", 90);
            AddText(detail, "detailParticipate", "參與", 70);
            AddText(detail, "detailExclude", "排除", 70);
        }

        public static ContextMenuStrip CreateParticipationMenu(DataGridView grid, MotherGroupConfirmList list, Label summary, DataGridView population, DataGridView detail)
        {
            return CreateParticipationMenu(grid, list, summary, population, detail, null);
        }

        public static ContextMenuStrip CreateParticipationMenu(DataGridView grid, MotherGroupConfirmList list, Label summary, DataGridView population, DataGridView detail, Func<bool> canEdit)
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            foreach (string rankType in RankKinds.All)
            {
                string participateType = rankType;
                ToolStripMenuItem participate = new ToolStripMenuItem(rankType + "設為參與");
                participate.Click += delegate
                {
                    if (canEdit != null && !canEdit())
                        return;
                    ApplyToSelected(grid, list, participateType, true);
                    RefreshSummary(summary, population, detail, list);
                };
                ToolStripMenuItem exclude = new ToolStripMenuItem(rankType + "設為不參與");
                exclude.Click += delegate
                {
                    if (canEdit != null && !canEdit())
                        return;
                    ApplyToSelected(grid, list, participateType, false);
                    RefreshSummary(summary, population, detail, list);
                };
                menu.Items.Add(participate);
                menu.Items.Add(exclude);
            }
            return menu;
        }

        public static void Show(DataGridView grid, IList<ConfirmStudentRow> rows)
        {
            grid.Rows.Clear();
            if (rows == null)
                return;
            foreach (ConfirmStudentRow row in rows)
            {
                int index = grid.Rows.Add();
                DataGridViewRow gridRow = grid.Rows[index];
                gridRow.Tag = row.Group.StudentId;
                gridRow.Cells["colClass"].Value = row.ClassName ?? "";
                gridRow.Cells["colSeat"].Value = row.SeatNo ?? "";
                gridRow.Cells["colNumber"].Value = row.Group.StudentNumber ?? "";
                gridRow.Cells["colName"].Value = row.Group.StudentName ?? "";
                gridRow.Cells["colConflict"].Value = FormatConflicts(row.Group);
                if (HasConflict(row.Group))
                    gridRow.DefaultCellStyle.BackColor = Color.FromArgb(255, 249, 196);
                foreach (string rankType in RankKinds.All)
                    BindRankCells(gridRow, row.Group.GetWorking(rankType));
            }
        }

        public static string FormatConflicts(StudentMotherGroup group)
        {
            if (group == null)
                return "";
            StringBuilder text = new StringBuilder();
            foreach (string rankType in RankKinds.All)
            {
                RankSlot slot = group.GetWorking(rankType);
                if (!slot.HasConflict)
                    continue;
                if (text.Length > 0)
                    text.Append("；");
                text.Append(rankType);
                text.Append("：");
                for (int i = 0; i < slot.ConflictNames.Count; i++)
                {
                    if (i > 0)
                        text.Append("、");
                    text.Append(slot.ConflictNames[i]);
                }
            }
            return text.ToString();
        }

        public static void ApplyVisibility(DataGridView grid, IList<string> visibleStudentIds)
        {
            Dictionary<string, bool> visible = new Dictionary<string, bool>();
            if (visibleStudentIds != null)
            {
                foreach (string studentId in visibleStudentIds)
                    visible[studentId] = true;
            }
            grid.CurrentCell = null;
            foreach (DataGridViewRow row in grid.Rows)
            {
                string studentId = row.Tag as string;
                row.Visible = studentId != null && visible.ContainsKey(studentId);
            }
        }

        public static void ApplyCellEdit(DataGridView grid, MotherGroupConfirmList list, int rowIndex, int columnIndex)
        {
            if (rowIndex < 0 || columnIndex < 0)
                return;
            DataGridViewColumn column = grid.Columns[columnIndex];
            string rankType = column.Tag as string;
            if (string.IsNullOrEmpty(rankType))
                return;
            string studentId = grid.Rows[rowIndex].Tag as string;
            bool participate = false;
            object value = grid.Rows[rowIndex].Cells[columnIndex].Value;
            if (value is bool)
                participate = (bool)value;
            List<string> ids = new List<string>();
            ids.Add(studentId);
            list.SetParticipate(ids, rankType, participate);
            ConfirmStudentRow source = Find(list, studentId);
            if (source == null)
                return;
            BindRankCells(grid.Rows[rowIndex], source.Group.GetWorking(rankType));
        }

        public static void ApplyToSelected(DataGridView grid, MotherGroupConfirmList list, string rankType, bool participate)
        {
            List<string> ids = new List<string>();
            foreach (DataGridViewRow row in grid.SelectedRows)
            {
                string studentId = row.Tag as string;
                if (!string.IsNullOrEmpty(studentId))
                    ids.Add(studentId);
            }
            list.SetParticipate(ids, rankType, participate);
            foreach (DataGridViewRow row in grid.Rows)
            {
                string studentId = row.Tag as string;
                if (studentId == null || !ids.Contains(studentId))
                    continue;
                ConfirmStudentRow source = Find(list, studentId);
                if (source == null)
                    continue;
                BindRankCells(row, source.Group.GetWorking(rankType));
            }
        }

        public static void RefreshParticipation(DataGridView grid, MotherGroupConfirmList list)
        {
            foreach (DataGridViewRow gridRow in grid.Rows)
            {
                string studentId = gridRow.Tag as string;
                ConfirmStudentRow source = Find(list, studentId);
                if (source == null)
                    continue;
                gridRow.Cells["colConflict"].Value = FormatConflicts(source.Group);
                foreach (string rankType in RankKinds.All)
                    BindRankCells(gridRow, source.Group.GetWorking(rankType));
            }
        }

        private static void BindRankCells(DataGridViewRow gridRow, RankSlot slot)
        {
            gridRow.Cells[NameColumn(slot.RankType)].Value = slot.GroupName ?? "";
            DataGridViewCell participateCell = gridRow.Cells[ParticipateColumn(slot.RankType)];
            participateCell.Value = slot.Participate;
            participateCell.ReadOnly = slot.HasConflict;
        }

        private static bool HasConflict(StudentMotherGroup group)
        {
            if (group == null)
                return false;
            foreach (string rankType in RankKinds.All)
            {
                if (group.GetWorking(rankType).HasConflict)
                    return true;
            }
            return false;
        }

        public static void RefreshSummary(Label summary, DataGridView population, DataGridView detail, MotherGroupConfirmList list)
        {
            if (list == null)
                return;
            MotherGroupCountSummary counts = list.Summarize();
            if (summary != null)
                summary.Text = "學生總人數 " + counts.StudentCount;
            if (population != null)
                FillPopulation(population, counts);
            if (detail != null)
                FillNamedGroupDetails(detail, counts);
        }

        public static void FillPopulation(DataGridView population, MotherGroupCountSummary summary)
        {
            if (population == null)
                return;
            if (population.Columns.Count == 0 || population.Rows.Count != RankKinds.All.Length)
                ConfigurePopulation(population);
            Dictionary<string, RankPopulationCount> byRank = new Dictionary<string, RankPopulationCount>();
            if (summary != null && summary.Ranks != null)
            {
                foreach (RankPopulationCount rank in summary.Ranks)
                {
                    if (rank != null && !string.IsNullOrEmpty(rank.RankType))
                        byRank[rank.RankType] = rank;
                }
            }
            foreach (DataGridViewRow row in population.Rows)
            {
                string rankType = row.Tag as string;
                RankPopulationCount rank;
                int participate = 0;
                int exclude = 0;
                if (rankType != null && byRank.TryGetValue(rankType, out rank))
                {
                    participate = rank.ParticipateCount;
                    exclude = rank.ExcludeCount;
                }
                row.Cells["populationRank"].Value = MotherGroupConfirmList.RankCaption(rankType);
                row.Cells["populationParticipate"].Value = participate.ToString();
                row.Cells["populationExclude"].Value = exclude.ToString();
            }
        }

        public static void FillNamedGroupDetails(DataGridView detail, MotherGroupCountSummary summary)
        {
            detail.Rows.Clear();
            if (summary == null)
                return;
            foreach (RankPopulationCount rank in summary.Ranks)
            {
                foreach (NamedGroupCount named in rank.NamedGroups)
                {
                    int index = detail.Rows.Add();
                    DataGridViewRow row = detail.Rows[index];
                    row.Cells["detailRank"].Value = rank.RankType;
                    row.Cells["detailName"].Value = named.GroupName;
                    row.Cells["detailTotal"].Value = named.TotalCount;
                    row.Cells["detailParticipate"].Value = named.ParticipateCount;
                    row.Cells["detailExclude"].Value = named.ExcludeCount;
                }
            }
        }

        public static string NameColumn(string rankType)
        {
            return "name_" + rankType;
        }

        public static string ParticipateColumn(string rankType)
        {
            return "participate_" + rankType;
        }

        private static ConfirmStudentRow Find(MotherGroupConfirmList list, string studentId)
        {
            if (list == null)
                return null;
            foreach (ConfirmStudentRow row in list.Rows)
            {
                if (row.Group.StudentId == studentId)
                    return row;
            }
            return null;
        }

        private static void AddText(DataGridView grid, string name, string header, int width)
        {
            DataGridViewTextBoxColumn column = new DataGridViewTextBoxColumn();
            column.Name = name;
            column.HeaderText = header;
            column.ReadOnly = true;
            column.Width = width;
            column.MinimumWidth = 50;
            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            column.SortMode = DataGridViewColumnSortMode.NotSortable;
            grid.Columns.Add(column);
        }

        private static void AddRank(DataGridView grid, string rankType, string nameHeader, string participateHeader)
        {
            DataGridViewTextBoxColumn nameColumn = new DataGridViewTextBoxColumn();
            nameColumn.Name = NameColumn(rankType);
            nameColumn.HeaderText = nameHeader;
            nameColumn.ReadOnly = true;
            nameColumn.Width = 120;
            nameColumn.MinimumWidth = 80;
            nameColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            nameColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            grid.Columns.Add(nameColumn);

            DataGridViewCheckBoxColumn participateColumn = new DataGridViewCheckBoxColumn();
            participateColumn.Name = ParticipateColumn(rankType);
            participateColumn.HeaderText = participateHeader;
            participateColumn.ReadOnly = false;
            participateColumn.Width = 100;
            participateColumn.MinimumWidth = 80;
            participateColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            participateColumn.Tag = rankType;
            participateColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            grid.Columns.Add(participateColumn);
        }
    }
}
