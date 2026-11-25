using Aspose.Cells;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace SHEvaluation.Rank.MotherGroup
{
    public static class SemesterMatrixRankHangCheck
    {
        private const int DetailRows = 664;

        [STAThread]
        public static int Main(string[] args)
        {
            try
            {
                List<string> errors = new List<string>();
                using (SemesterMatrixRankSelect form = new SemesterMatrixRankSelect("297", "1", "114", "2", "學期", "分項成績", "學業", "年排名", "三年級"))
                {
                    form.UseLocalDetailForCheck();
                    form.ShowInTaskbar = false;
                    form.Show();
                    Application.DoEvents();
                    form.AddLocalBatchForCheck("297", DetailTable("297", DetailRows, "A"));
                    form.AddLocalBatchForCheck("298", DetailTable("298", 40, "B"));

                    long loadMs = Time("detail-load", delegate { form.SelectBatchForCheck("297"); });
                    DataGridView grid = form.DetailGridForCheck();
                    Expect(errors, grid.Columns.Count == 17 && form.DetailCountForCheck() == DetailRows, "實際明細應載入 17 欄、664 列");
                    Expect(errors, loadMs < 8000, "明細載入超過 8 秒：" + loadMs);
                    Expect(errors, form.DetailCellForCheck(0, 0) == "297" && form.DetailCellForCheck(0, 8) == "A00000", "第一筆應是批次 297 的合成列");
                    DataGridViewRow plain = new DataGridViewRow();
                    Expect(errors, !MotherGroupConfirmGrid.IsBoundedRow(plain), "直接 new DataGridViewRow 不會套用 RowTemplate 的修正");
                    Expect(errors, MotherGroupConfirmGrid.IsBoundedRow(grid.Rows[0]), "實際建立的明細列要使用修正後的資料列");
                    Console.WriteLine("detail-row-type: " + grid.Rows[0].GetType().FullName);
                    Console.WriteLine("template-type: " + grid.RowTemplate.GetType().FullName);

                    int visibleRows = 0;
                    bool firstEmpty = true;
                    bool lastEmpty = false;
                    TimeBoxed("accessibility-all-row-bounds", delegate
                    {
                        for (int i = 0; i < grid.Rows.Count; i++)
                        {
                            if (!grid.Rows[i].AccessibilityObject.Bounds.IsEmpty)
                                visibleRows++;
                        }
                        firstEmpty = grid.Rows[0].AccessibilityObject.Bounds.IsEmpty;
                        lastEmpty = grid.Rows[DetailRows - 1].AccessibilityObject.Bounds.IsEmpty;
                        Console.WriteLine("accessibility-visible-rows: " + visibleRows);
                    });
                    Expect(errors, !firstEmpty && lastEmpty && visibleRows > 0 && visibleRows < 40, "可見列要有範圍，離屏最後一列要是空範圍");
                    int cellReads = 0;
                    TimeBoxed("accessibility-all-cell-bounds", delegate
                    {
                        for (int i = 0; i < grid.Rows.Count; i++)
                        {
                            for (int column = 0; column < grid.Columns.Count; column++)
                            {
                                Rectangle bounds = grid.Rows[i].Cells[column].AccessibilityObject.Bounds;
                                if (bounds.Width >= 0)
                                    cellReads++;
                            }
                        }
                        Console.WriteLine("accessibility-cell-reads: " + cellReads);
                    });

                    long scrollMs = Time("detail-scroll", delegate
                    {
                        form.ScrollDetailForCheck(620);
                        Application.DoEvents();
                    });
                    Expect(errors, scrollMs < 3000, "捲動超過 3 秒：" + scrollMs);
                    Expect(errors, !grid.Rows[620].AccessibilityObject.Bounds.IsEmpty && grid.Rows[0].AccessibilityObject.Bounds.IsEmpty, "捲動後範圍要跟著目前視窗");

                    long switchMs = Time("detail-switch-batch", delegate { form.SelectBatchForCheck("298"); });
                    Expect(errors, switchMs < 3000 && form.DetailCountForCheck() == 40 && form.DetailCellForCheck(0, 0) == "298" && form.DetailCellForCheck(0, 8) == "B00000", "切換批次後應換成另一批明細");
                    form.SelectBatchForCheck("297");
                    Expect(errors, form.DetailCountForCheck() == DetailRows, "切回原批次應恢復 664 列");

                    string path = Path.Combine(Path.GetTempPath(), "semester-matrix-detail-" + Guid.NewGuid().ToString("N") + ".xlsx");
                    long exportMs = Time("detail-export", delegate { form.ExportDetailForCheck(path); });
                    Expect(errors, exportMs < 8000 && File.Exists(path), "匯出應寫出全部明細：" + exportMs);
                    Workbook workbook = new Workbook(path);
                    Expect(errors, workbook.Worksheets[0].Cells.MaxDataRow == DetailRows && ("" + workbook.Worksheets[0].Cells[1, 7].Value) == "A00000", "匯出列數與第一筆學號要對上明細");
                    workbook = null;
                    try
                    {
                        File.Delete(path);
                    }
                    catch (Exception)
                    {
                    }

                    long leaveMs = Time("detail-leave", delegate { form.LeaveForCheck(); });
                    Application.DoEvents();
                    Expect(errors, leaveMs < 3000 && (form.IsDisposed || !form.Visible), "離開應關閉視窗：" + leaveMs);
                }
                if (errors.Count == 0)
                {
                    Console.WriteLine("semester-matrix-rank-hang-check: ok");
                    Console.WriteLine("database-executed: false");
                    Console.WriteLine("ischool-screen-executed: false");
                    Console.WriteLine("field-664-replaced: false");
                    return 0;
                }
                foreach (string error in errors)
                    Console.WriteLine(error);
                return 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.GetType().FullName);
                Console.WriteLine(ex.Message);
                return 1;
            }
        }

        private static DataTable DetailTable(string matrixId, int count, string mark)
        {
            DataTable table = new DataTable();
            string[] names = new string[] { "rank_matrix_id", "score_type", "score_category", "item_name", "rank_type", "rank_name", "class_name", "seat_no", "student_number", "student_name", "student_status", "score", "rank", "pr", "percentile", "school_year", "semester", "memo" };
            foreach (string name in names)
                table.Columns.Add(name);
            for (int i = 0; i < count; i++)
            {
                table.Rows.Add(matrixId, "學期", "分項成績", "學業", "年排名", "三年級", "C01", (i % 40).ToString(), mark + i.ToString("00000"), "n" + i.ToString("0000"), "一般", "80", (i + 1).ToString(), "50", "50", "114", "2", "memo");
            }
            return table;
        }

        private static long Time(string label, Action action)
        {
            Stopwatch watch = Stopwatch.StartNew();
            action();
            watch.Stop();
            Console.WriteLine(label + "-ms: " + watch.ElapsedMilliseconds);
            return watch.ElapsedMilliseconds;
        }

        private static void TimeBoxed(string label, Action action)
        {
            Thread ui = Thread.CurrentThread;
            int state = 0;
            Thread watcher = new Thread(delegate ()
            {
                Thread.Sleep(8000);
                if (Interlocked.CompareExchange(ref state, 2, 0) != 0)
                    return;
                string stack = CaptureStack(ui);
                Console.WriteLine("hang-step: " + label);
                Console.WriteLine("hang-class: " + Classify(stack));
                Console.WriteLine("UI-THREAD-STACK-BEGIN");
                Console.WriteLine(stack);
                Console.WriteLine("UI-THREAD-STACK-END");
                Environment.Exit(2);
            });
            watcher.IsBackground = true;
            watcher.Start();
            Stopwatch watch = Stopwatch.StartNew();
            action();
            watch.Stop();
            Interlocked.Exchange(ref state, 1);
            Console.WriteLine(label + "-ms: " + watch.ElapsedMilliseconds);
        }

        private static string CaptureStack(Thread ui)
        {
            try
            {
#pragma warning disable 0618
                ui.Suspend();
                try
                {
                    return new StackTrace(ui, false).ToString();
                }
                finally
                {
                    ui.Resume();
                }
#pragma warning restore 0618
            }
            catch (Exception ex)
            {
                return ex.GetType().FullName + ": " + ex.Message;
            }
        }

        private static string Classify(string stack)
        {
            string text = stack ?? "";
            if (text.IndexOf("AccessibleObject", StringComparison.Ordinal) >= 0 || text.IndexOf("get_Bounds", StringComparison.Ordinal) >= 0)
                return "ui-automation-accessibility";
            if (text.IndexOf("AutoResize", StringComparison.Ordinal) >= 0 || text.IndexOf("GetPreferredWidth", StringComparison.Ordinal) >= 0)
                return "column-width";
            if (text.IndexOf("OnPaint", StringComparison.Ordinal) >= 0)
                return "redraw";
            if (text.IndexOf("Wait", StringComparison.Ordinal) >= 0 || text.IndexOf("Join", StringComparison.Ordinal) >= 0)
                return "sync-wait";
            return "unclassified";
        }

        private static void Expect(List<string> errors, bool condition, string message)
        {
            if (!condition)
                errors.Add(message);
        }
    }
}
