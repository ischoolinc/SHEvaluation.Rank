using SHEvaluation.Rank.DAO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace SHEvaluation.Rank.MotherGroup
{
    public static class SemesterRankRetrievalHangCheck
    {
        private const int RowCount = 7193;

        [STAThread]
        public static int Main(string[] args)
        {
            try
            {
                List<string> errors = new List<string>();
                using (SemesterAssessmentRankSelect form = new SemesterAssessmentRankSelect())
                {
                    form.UseLocalRowsForCheck();
                    form.ShowInTaskbar = false;
                    form.Show();
                    Application.DoEvents();
                    Expect(errors, form.ColumnCountForCheck() == 17, "檢索表格應有 17 欄");

                    List<RankDataInfo> rows = SyntheticRows();
                    long loadMs = Time("manual-load", delegate { form.LoadRowsForCheck(rows); });
                    long paintMs = Time("manual-paint", delegate
                    {
                        form.Refresh();
                        Application.DoEvents();
                    });
                    Expect(errors, form.ViewCountForCheck() == RowCount, "載入後應顯示全部 7193 筆");
                    Expect(errors, loadMs < 3000 && paintMs < 3000, "手動載入與重繪應在 3 秒內完成：" + loadMs + "/" + paintMs);

                    long sortMs = Time("manual-sort", delegate
                    {
                        form.ClickHeaderForCheck("排名分數");
                        form.ClickHeaderForCheck("排名分數");
                    });
                    Expect(errors, sortMs < 3000 && form.SortGlyphForCheck("排名分數") == "Descending", "手動排序雙向切換應在 3 秒內完成：" + sortMs);
                    RankDataInfo sorted = form.ViewRowForCheck(0);
                    Expect(errors, sorted != null && sorted.score == "99.5" && sorted.rank_matrix_id == "M99", "降冪後檢視應對到分數最高的那一筆");

                    long filterMs = Time("manual-filter", delegate
                    {
                        form.FilterStudentNumberForCheck("00100");
                        form.FilterStudentNumberForCheck("");
                        form.FilterItemForCheck("學業");
                        form.FilterRankTypeForCheck("年排名");
                    });
                    Expect(errors, filterMs < 3000 && form.ViewCountForCheck() > 0 && form.ViewCountForCheck() < RowCount, "學號、項目與母群篩選應在 3 秒內完成：" + filterMs);
                    RankDataInfo filtered = form.ViewRowForCheck(0);
                    Expect(errors, filtered != null && filtered.item_name == "學業" && filtered.rank_type == "年排名", "篩選後第一筆應屬於選定項目與母群");
                    List<string> exported = form.ExportLinesForCheck();
                    Expect(errors, exported.Count == form.ViewCountForCheck(), "匯出應包含全部篩選結果");
                    form.FilterRankTypeForCheck("全部");
                    form.FilterItemForCheck("全部");
                    Expect(errors, form.ViewCountForCheck() == RowCount, "清除篩選後應回到全部資料");

                    long closeMs = Time("manual-close-prepare", delegate { Application.DoEvents(); });
                    Console.WriteLine("manual-responsive: true");
                    Console.WriteLine("manual-close-prepare-ms: " + closeMs);

                    AccessibleObject grid = form.RetrievalGridForCheck().AccessibilityObject;
                    TimeBoxed("accessibility-child-count", delegate
                    {
                        int children = grid.GetChildCount();
                        Console.WriteLine("accessibility-child-count: " + children);
                    });
                    DataGridView view = form.RetrievalGridForCheck();
                    bool firstEmpty = true;
                    bool lastEmpty = false;
                    int visibleRows = 0;
                    TimeBoxed("accessibility-first-row-bounds", delegate
                    {
                        Rectangle bounds = view.Rows[0].AccessibilityObject.Bounds;
                        firstEmpty = bounds.IsEmpty;
                        Console.WriteLine("accessibility-first-row-empty: " + bounds.IsEmpty);
                    });
                    TimeBoxed("accessibility-last-row-bounds", delegate
                    {
                        Rectangle bounds = view.Rows[RowCount - 1].AccessibilityObject.Bounds;
                        lastEmpty = bounds.IsEmpty;
                        Console.WriteLine("accessibility-last-row-empty: " + bounds.IsEmpty);
                    });
                    TimeBoxed("accessibility-all-row-bounds", delegate
                    {
                        for (int i = 0; i < view.Rows.Count; i++)
                        {
                            if (!view.Rows[i].AccessibilityObject.Bounds.IsEmpty)
                                visibleRows++;
                        }
                        Console.WriteLine("accessibility-visible-rows: " + visibleRows);
                    });
                    Expect(errors, !firstEmpty && lastEmpty, "可見列要有範圍，視窗外最後一列要立即回傳空範圍");
                    Expect(errors, visibleRows > 0 && visibleRows < 80, "輔助存取只應看到目前視窗內的列：" + visibleRows);
                    TimeBoxed("accessibility-visible-cell-bounds", delegate
                    {
                        int row = view.FirstDisplayedScrollingRowIndex;
                        if (row < 0)
                            row = 0;
                        int seen = 0;
                        for (int i = row; i < view.Rows.Count && i < row + view.DisplayedRowCount(true); i++)
                        {
                            for (int column = 0; column < view.Columns.Count; column++)
                            {
                                Rectangle bounds = view.Rows[i].Cells[column].AccessibilityObject.Bounds;
                                if (bounds.Width >= 0)
                                    seen++;
                            }
                        }
                        Console.WriteLine("accessibility-visible-cells: " + seen);
                    });
                    form.Close();
                    Application.DoEvents();
                    Expect(errors, form.IsDisposed || !form.Visible, "視窗應可關閉");
                }
                if (errors.Count == 0)
                {
                    Console.WriteLine("semester-rank-retrieval-hang-check: ok");
                    Console.WriteLine("database-executed: false");
                    Console.WriteLine("ischool-screen-executed: false");
                    Console.WriteLine("field-7193-replaced: false");
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

        private static List<RankDataInfo> SyntheticRows()
        {
            List<RankDataInfo> rows = new List<RankDataInfo>();
            for (int i = 0; i < RowCount; i++)
            {
                RankDataInfo row = new RankDataInfo();
                row.rank_matrix_id = "M" + (i % 100).ToString("00");
                row.ref_student_id = "S" + i.ToString("0000");
                row.score_type = "學期";
                row.score_category = "分項成績";
                row.item_name = i % 2 == 0 ? "學業" : "體育";
                row.rank_type = i % 4 == 0 ? "年排名" : "班排名";
                row.rank_name = "合成母群";
                row.class_name = "C" + (i % 20).ToString("00");
                row.seat_no = (i % 40).ToString();
                row.student_number = i.ToString("00000");
                row.student_name = "n" + i.ToString("0000");
                row.score = ((i % 100) + 0.5m).ToString(CultureInfo.InvariantCulture);
                row.rank = ((i % 50) + 1).ToString();
                row.pr = (i % 100).ToString();
                row.percentile = (i % 100).ToString();
                row.school_year = "114";
                row.semester = "2";
                rows.Add(row);
            }
            return rows;
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
            if (text.IndexOf("AccessibleObject", StringComparison.Ordinal) >= 0
                || text.IndexOf("get_Bounds", StringComparison.Ordinal) >= 0)
                return "ui-automation-accessibility";
            if (text.IndexOf("SortRows", StringComparison.Ordinal) >= 0 || text.IndexOf("Array.Sort", StringComparison.Ordinal) >= 0)
                return "sort";
            if (text.IndexOf("AutoResize", StringComparison.Ordinal) >= 0
                || text.IndexOf("GetPreferredWidth", StringComparison.Ordinal) >= 0
                || text.IndexOf("DisplayedCells", StringComparison.Ordinal) >= 0)
                return "column-width";
            if (text.IndexOf("OnPaint", StringComparison.Ordinal) >= 0 || text.IndexOf("Paint", StringComparison.Ordinal) >= 0)
                return "redraw";
            if (text.IndexOf("DataGridViewX", StringComparison.Ordinal) >= 0)
                return "datagridviewx";
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
