using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace SHEvaluation.Rank.MotherGroup
{
    public static class MotherGroupConfirmGridAccessibilityCheck
    {
        [STAThread]
        public static int Main(string[] args)
        {
            try
            {
                using (Form form = new Form())
                using (DataGridView grid = new DevComponents.DotNetBar.Controls.DataGridViewX())
                {
                    form.ShowInTaskbar = false;
                    form.ClientSize = new Size(1000, 240);
                    grid.Dock = DockStyle.Fill;
                    form.Controls.Add(grid);
                    MotherGroupConfirmGrid.Configure(grid);
                    List<ConfirmStudentRow> rows = new List<ConfirmStudentRow>();
                    for (int i = 0; i < 2066; i++)
                    {
                        StudentMotherGroup group = new StudentMotherGroup();
                        group.StudentId = (i + 1).ToString();
                        group.StudentName = "學生" + i;
                        group.GetWorking(RankKinds.Year).GroupName = "1年級";
                        group.GetWorking(RankKinds.Year).Participate = true;
                        rows.Add(new ConfirmStudentRow { Group = group });
                    }
                    MotherGroupConfirmGrid.Show(grid, rows);
                    form.Show();
                    Application.DoEvents();

                    Require(grid.Rows.Count == 2066, "學生清單不可被截斷");
                    Require(grid.AccessibilityObject.GetChildCount() >= 2066, "保留全部資料列的輔助存取子項");
                    int yearColumn = grid.Columns[MotherGroupConfirmGrid.ParticipateColumn(RankKinds.Year)].Index;
                    AccessibleObject first = grid.Rows[0].AccessibilityObject;
                    Require(first.GetChildCount() == grid.Columns.Count, "保留儲存格子項");
                    AccessibleObject checkbox = grid.Rows[0].Cells[yearColumn].AccessibilityObject;
                    Require(!string.IsNullOrEmpty(checkbox.DefaultAction), "保留核取方塊操作");
                    Require(first.Bounds.Height > 0, "可見列須有範圍");
                    Require(grid.Rows[2065].AccessibilityObject.Bounds.IsEmpty, "視窗外最後一列須立即回傳空範圍");

                    Stopwatch watch = Stopwatch.StartNew();
                    for (int i = 0; i < grid.Rows.Count; i++)
                    {
                        Rectangle rowBounds = grid.Rows[i].AccessibilityObject.Bounds;
                        Rectangle cellBounds = grid.Rows[i].Cells[yearColumn].AccessibilityObject.Bounds;
                        Require(rowBounds.Width >= 0 && cellBounds.Width >= 0, "資料列與儲存格範圍有效");
                    }
                    watch.Stop();
                    Require(watch.ElapsedMilliseconds < 10000, "2066 列範圍走訪超過 10 秒：" + watch.ElapsedMilliseconds);
                    Console.WriteLine("2066 row/cell bounds: " + watch.ElapsedMilliseconds + "ms");

                    grid.FirstDisplayedScrollingRowIndex = 2050;
                    Application.DoEvents();
                    Require(grid.Rows[2050].AccessibilityObject.Bounds.Height > 0, "捲動後範圍須更新");
                    Require(grid.Rows[0].AccessibilityObject.Bounds.IsEmpty, "捲走的列須回傳空範圍");
                    checkbox.DoDefaultAction();
                    grid.EndEdit();
                    Require(!(bool)grid.Rows[0].Cells[yearColumn].Value, "輔助存取仍可切換核取方塊");
                    form.Close();
                }
                Console.WriteLine("confirm-grid-accessibility-check: ok");
                Console.WriteLine("database-executed: false; ischool-screen-executed: false");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return 1;
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
