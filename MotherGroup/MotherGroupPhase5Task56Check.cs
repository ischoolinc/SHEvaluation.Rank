using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace SHEvaluation.Rank.MotherGroup
{
    public static class MotherGroupPhase5Task56Check
    {
        public static int Main(string[] args)
        {
            try
            {
                List<string> errors = new List<string>();
                errors.AddRange(CheckSameNameIsNotConflict());
                errors.AddRange(CheckDifferentNamesStayUnresolved());
                errors.AddRange(CheckGridAndRestore());
                errors.AddRange(CheckSafety());
                if (!SemesterRankWriteGate.CanWriteRankTables())
                    errors.Add("SemesterRankWriteGate 應允許寫入");
                if (errors.Count == 0)
                {
                    Console.WriteLine("phase5-task56-check: ok");
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

        private static List<string> CheckSameNameIsNotConflict()
        {
            List<string> errors = new List<string>();
            DataTable table = Table();
            AddStudent(table, "5601", "S601", "甲", "1年級", "忠班", "甲組", "");
            AddStudent(table, "5601", "S601", "甲", "1年級", "忠班", "甲組", "");
            AddRank(table, "5601", "S601", "甲", RankKinds.Department, "普通科");
            AddRank(table, "5601", "S601", "甲", RankKinds.Department, "普通科");
            HistoryRankLoad load = HistoryRankRestore.Read("91", table, "", Tags(), new List<StudentTagLink>());
            StudentMotherGroup student = load.Students[0];
            Expect(errors, load.Restored && load.Students.Count == 1 && load.Conflicts.Count == 0, "相同母群名稱的重複列不應視為衝突");
            Expect(errors, !student.GetWorking(RankKinds.Class).HasConflict && student.GetWorking(RankKinds.Class).GroupName == "忠班" && student.GetWorking(RankKinds.Class).Participate, "重複的同一個班名應維持原母群與參與");
            Expect(errors, !student.GetWorking(RankKinds.Department).HasConflict && student.GetWorking(RankKinds.Department).GroupName == "普通科", "重複的同一個科名應維持原科別");
            return errors;
        }

        private static List<string> CheckDifferentNamesStayUnresolved()
        {
            List<string> errors = new List<string>();
            HistoryRankLoad load = ConflictLoad();
            Expect(errors, load.Restored && load.Students.Count == 1, "單一排名衝突不應讓學生從清單消失");
            StudentMotherGroup student = load.Students[0];
            RankSlot classSlot = student.GetWorking(RankKinds.Class);
            Expect(errors, classSlot.HasConflict && classSlot.ConflictNames.Contains("忠班") && classSlot.ConflictNames.Contains("孝班") && classSlot.ConflictNames.Count == 2, "不同班名應全部保留");
            Expect(errors, classSlot.GroupName == "" && !classSlot.Participate, "衝突的班排名不應選定名稱，也不參與");
            Expect(errors, student.GetWorking(RankKinds.Year).GroupName == "1年級" && student.GetWorking(RankKinds.Year).Participate && !student.GetWorking(RankKinds.Year).HasConflict, "沒有衝突的年排名應維持原母群與參與");
            Expect(errors, student.GetWorking(RankKinds.Department).GroupName == "普通科" && student.GetWorking(RankKinds.Department).Participate, "沒有衝突的科排名應維持參與");
            Expect(errors, load.Notice.IndexOf("忠班") >= 0 && load.Notice.IndexOf("孝班") >= 0, "還原提示應包含全部衝突名稱");
            return errors;
        }

        private static List<string> CheckGridAndRestore()
        {
            List<string> errors = new List<string>();
            HistoryRankLoad load = ConflictLoad();
            StudentMotherGroup student = load.Students[0];
            MotherGroupConfirmList list = new MotherGroupConfirmList();
            list.Load(new List<ConfirmStudentRow>
            {
                new ConfirmStudentRow { Group = student, ClassName = "新班", SeatNo = "9" }
            });
            Expect(errors, list.Rows.Count == 1 && student.GetWorking(RankKinds.Class).GroupName == "" && student.GetWorking(RankKinds.Year).GroupName == "1年級", "目前班級不應修復歷史班名衝突，學生仍留在確認清單");

            Form form = new Form();
            form.ShowInTaskbar = false;
            DataGridView grid = new DataGridView();
            grid.Size = new System.Drawing.Size(900, 180);
            form.Controls.Add(grid);
            form.Show();
            MotherGroupConfirmGrid.Configure(grid);
            MotherGroupConfirmGrid.Show(grid, list.Rows);
            string conflictText = "" + grid.Rows[0].Cells["colConflict"].Value;
            Expect(errors, conflictText.IndexOf("班排名") >= 0 && conflictText.IndexOf("忠班") >= 0 && conflictText.IndexOf("孝班") >= 0, "確認清單應顯示衝突排名類型與全部名稱");
            Expect(errors, ("" + grid.Rows[0].Cells[MotherGroupConfirmGrid.NameColumn(RankKinds.Class)].Value) == "", "衝突排名的母群名稱欄不應改選其中一筆");
            Expect(errors, (bool)grid.Rows[0].Cells[MotherGroupConfirmGrid.ParticipateColumn(RankKinds.Class)].Value == false, "衝突排名的參與勾選應為不參與");
            Expect(errors, ("" + grid.Rows[0].Cells[MotherGroupConfirmGrid.NameColumn(RankKinds.Year)].Value) == "1年級" && (bool)grid.Rows[0].Cells[MotherGroupConfirmGrid.ParticipateColumn(RankKinds.Year)].Value, "沒有衝突的年排名仍應顯示原名稱與參與");

            int classColumn = grid.Columns[MotherGroupConfirmGrid.ParticipateColumn(RankKinds.Class)].Index;
            grid.Rows[0].Cells[classColumn].Value = true;
            MotherGroupConfirmGrid.ApplyCellEdit(grid, list, 0, classColumn);
            Expect(errors, !student.GetWorking(RankKinds.Class).Participate && (bool)grid.Rows[0].Cells[classColumn].Value == false && student.GetWorking(RankKinds.Class).ConflictNames.Count == 2, "勾選衝突排名不應解除衝突或改為參與");

            grid.ClearSelection();
            grid.Rows[0].Selected = true;
            MotherGroupConfirmGrid.ApplyToSelected(grid, list, RankKinds.Class, true);
            Expect(errors, !student.GetWorking(RankKinds.Class).Participate && student.GetWorking(RankKinds.Year).Participate, "批次把衝突排名設為參與時仍應維持不參與，且不影響年排名");

            list.SetParticipate(new string[] { "5602" }, RankKinds.Department, false);
            Expect(errors, !student.GetWorking(RankKinds.Department).Participate && student.GetWorking(RankKinds.Department).GroupName == "普通科" && !student.GetWorking(RankKinds.Class).Participate, "沒有衝突的排名仍可調整參與，衝突項維持不參與");
            student.GetWorking(RankKinds.Class).Participate = true;
            list.Restore();
            MotherGroupConfirmGrid.RefreshParticipation(grid, list);
            Expect(errors, !student.GetWorking(RankKinds.Class).Participate && student.GetWorking(RankKinds.Class).ConflictNames.Contains("忠班") && student.GetWorking(RankKinds.Class).ConflictNames.Contains("孝班"), "還原參與不應解除未處理的衝突");
            Expect(errors, student.GetWorking(RankKinds.Department).Participate && student.GetWorking(RankKinds.Department).GroupName == "普通科", "還原應回復沒有衝突的科排名參與，且不改名稱");
            Expect(errors, ("" + grid.Rows[0].Cells["colConflict"].Value).IndexOf("孝班") >= 0, "還原後衝突名稱仍顯示在確認清單");

            MotherGroupConfirmGrid.ApplyVisibility(grid, list.VisibleStudentIds("孝班"));
            Expect(errors, grid.Rows[0].Visible && list.Rows.Count == 1, "搜尋衝突名稱時學生仍在清單");
            MotherGroupCountSummary summary = list.Summarize();
            Expect(errors, summary.StudentCount == 1 && RankCount(summary, RankKinds.Class, false) == 1 && RankCount(summary, RankKinds.Year, true) == 1, "人數統計應保留衝突學生，並只把衝突排名計為不參與");
            form.Close();
            return errors;
        }

        private static List<string> CheckSafety()
        {
            List<string> errors = new List<string>();
            string restore = File.ReadAllText("MotherGroup\\HistoryRankRestore.cs", Encoding.UTF8);
            string grid = File.ReadAllText("MotherGroup\\MotherGroupConfirmGrid.cs", Encoding.UTF8);
            string list = File.ReadAllText("MotherGroup\\MotherGroupConfirmList.cs", Encoding.UTF8);
            Expect(errors, restore.IndexOf("INSERT") < 0 && restore.IndexOf("UPDATE") < 0 && restore.IndexOf("DELETE") < 0 && restore.IndexOf("class.class_name") < 0, "衝突處理不應更新歷史批次，也不應讀目前班級來修復");
            Expect(errors, grid.IndexOf("母群衝突") >= 0 && list.IndexOf("HasConflict") >= 0, "確認清單應能標示衝突並拒絕把該項改為參與");
            string gate = File.ReadAllText("MotherGroup\\SemesterRankWriteGate.cs", Encoding.UTF8);
            Expect(errors, gate.IndexOf("return true;") >= 0 && gate.IndexOf("return false;") < 0, "SemesterRankWriteGate 應回傳 true");
            return errors;
        }

        private static HistoryRankLoad ConflictLoad()
        {
            DataTable table = Table();
            AddStudent(table, "5602", "S602", "乙", "1年級", "忠班", "", "");
            AddStudent(table, "5602", "S602", "乙", "1年級", "孝班", "", "");
            AddRank(table, "5602", "S602", "乙", RankKinds.Department, "普通科");
            return HistoryRankRestore.Read("91", table, "", Tags(), new List<StudentTagLink>());
        }

        private static int RankCount(MotherGroupCountSummary summary, string rankType, bool participate)
        {
            foreach (RankPopulationCount rank in summary.Ranks)
            {
                if (rank.RankType == rankType)
                    return participate ? rank.ParticipateCount : rank.ExcludeCount;
            }
            return -1;
        }

        private static DataTable Table()
        {
            DataTable table = new DataTable();
            table.Columns.Add("student_id");
            table.Columns.Add("student_number");
            table.Columns.Add("student_name");
            table.Columns.Add("rank_type");
            table.Columns.Add("group_name");
            table.Columns.Add("name_origin");
            return table;
        }

        private static void AddStudent(DataTable table, string id, string number, string name, string year, string className, string tag1, string tag2)
        {
            table.Rows.Add(id, number, name, RankKinds.Year, year, "student");
            table.Rows.Add(id, number, name, RankKinds.Class, className, "student");
            table.Rows.Add(id, number, name, RankKinds.Tag1, tag1, "student");
            table.Rows.Add(id, number, name, RankKinds.Tag2, tag2, "student");
        }

        private static void AddRank(DataTable table, string id, string number, string name, string rankType, string groupName)
        {
            table.Rows.Add(id, number, name, rankType, groupName, "rank");
        }

        private static List<TagDefinition> Tags()
        {
            return new List<TagDefinition>();
        }

        private static void Expect(List<string> errors, bool condition, string message)
        {
            if (!condition)
                errors.Add(message);
        }
    }
}
