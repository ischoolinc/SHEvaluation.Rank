using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SHEvaluation.Rank.MotherGroup
{
    public static class MotherGroupPhase9Task91Check
    {
        public static int Main(string[] args)
        {
            try
            {
                List<string> errors = new List<string>();
                errors.AddRange(CheckMessage());
                string form = File.ReadAllText("CalculateSemesterAssessmentRank.cs", Encoding.UTF8);
                string gate = File.ReadAllText("MotherGroup\\SemesterRankWriteGate.cs", Encoding.UTF8);
                errors.AddRange(CheckForm(form, gate));
                errors.AddRange(CheckOtherRankFiles());
                if (errors.Count == 0)
                {
                    Console.WriteLine("phase9-task91-check: ok");
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

        private static List<string> CheckMessage()
        {
            List<string> errors = new List<string>();
            MotherGroupConfirmList list = new MotherGroupConfirmList();
            list.Rows.Add(Row("1", "2年級"));
            list.Rows.Add(Row("2", "高一"));
            list.Rows.Add(Row("3", "1年級"));
            string grades = RankCalculationConfirm.GradeList(list, new int[] { 1, 2 });
            Expect(errors, grades == "1年級、2年級", "停用範圍應是可解析年級，並由小到大");
            string message = RankCalculationConfirm.Message("114", "1", CalculationPeriod.CurrentSourceName, grades);
            Expect(errors, message.IndexOf("計算學年度：114") >= 0 && message.IndexOf("計算學期：1") >= 0 && message.IndexOf("母群來源：" + CalculationPeriod.CurrentSourceName) >= 0, "確認框應包含計算學年度、學期與母群來源");
            Expect(errors, message.IndexOf("本次會停用114學年度第1學期、1年級、2年級的有效學期成績排名") >= 0, "確認框應說明本次停用的有效學期成績排名");
            Expect(errors, message.IndexOf("定期評量與技職繁星的有效排名不會停用") >= 0, "確認框應排除其他排名");
            Expect(errors, message.IndexOf("擇優設定會先寫入學校組態") >= 0 && message.IndexOf("不會自動回復") >= 0, "確認框應說明擇優設定先寫入且失敗不回復");
            Expect(errors, message.IndexOf("計算完成") < 0 && message.IndexOf("INSERT") < 0 && message.IndexOf("UPDATE") < 0 && message.IndexOf("DELETE") < 0, "確認文字不表示已計算完成，也不含排名寫入語句");
            Expect(errors, RankCalculationConfirm.GradeList(null, new int[] { 1 }) == "（沒有可解析的年級）" && RankCalculationConfirm.GradeList(new MotherGroupConfirmList(), new int[] { 1 }) == "（沒有可解析的年級）", "沒有可解析年級時不宣稱會停用某個年級");
            string history = RankCalculationConfirm.Message("113", "2", CalculationPeriod.HistorySourceName, "3年級");
            string excel = RankCalculationConfirm.Message("114", "1", CalculationPeriod.ExcelSourceName, "1年級");
            Expect(errors, history.IndexOf(CalculationPeriod.HistorySourceName) >= 0 && excel.IndexOf(CalculationPeriod.ExcelSourceName) >= 0 && history.IndexOf("學年度：113") >= 0, "三種母群來源都使用同一段確認文字");
            return errors;
        }

        private static List<string> CheckForm(string form, string gate)
        {
            List<string> errors = new List<string>();
            int calculate = form.IndexOf("void btnCacluate_Click");
            int resize = form.IndexOf("void CalculateSemesterAssessmentRank_Resize");
            string body = calculate >= 0 && resize > calculate ? form.Substring(calculate, resize - calculate) : "";
            int warn = body.IndexOf("UnparsedYearMessage");
            int gateCall = body.IndexOf("CanWriteRankTables");
            int gateReturn = body.IndexOf("return;");
            int running = body.IndexOf("_rankCalculationRunning = true");
            int confirm = body.IndexOf("RankCalculationConfirm.Message");
            int cancel = body.IndexOf("_rankCalculationRunning = false");
            int disable = body.IndexOf("btnCacluate.Enabled = false");
            int save = body.IndexOf("cd.Save()");
            int build = body.IndexOf("SemesterRankSql.Build(");
            int publish = body.IndexOf("SemesterRankPublishSql.Build(");
            int apply = body.IndexOf("SemesterRankSql.ApplyParticipation");
            int select = body.IndexOf("queryHelper.Select(insertRankSql)");
            Expect(errors, running >= 0 && running < warn && warn < gateCall && gateReturn > gateCall, "執行中旗標在提示之前設下，年排名提示仍在閘門之前");
            Expect(errors, confirm > gateReturn && cancel > confirm && cancel < save && disable > confirm && disable < save, "確認框在允許寫入的路徑，取消時清除執行中旗標且尚未儲存");
            Expect(errors, save > confirm && build > save && publish > build && apply > publish && select > apply, "確認之後仍維持儲存、組句、參與旗標與唯一查詢的順序");
            Expect(errors, body.IndexOf("不會隨排名語句回滾") >= 0 && body.IndexOf("不會隨排名語句回滾") < save, "擇優設定仍先於排名查詢儲存，且註明不會隨排名語句回滾");
            int completed = body.IndexOf("bkw.RunWorkerCompleted +=");
            string done = completed >= 0 ? body.Substring(completed) : "";
            Expect(errors, done.IndexOf("_rankCalculationRunning = false") >= 0 && done.IndexOf("RestoreCalculationEditors()") > done.IndexOf("_rankCalculationRunning = false"), "完成或失敗時先清除執行中旗標並恢復計算前的控制項狀態");
            Expect(errors, body.IndexOf("HistorySchoolYear") < 0 && body.IndexOf("ExcelSourceName") < 0, "計算期間與來源讀取方式維持原程式");
            Expect(errors, body.IndexOf("Application.DoEvents") < 0, "確認流程不使用 DoEvents");
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
                if (text.IndexOf("RankCalculationConfirm") >= 0 || text.IndexOf("_rankCalculationRunning") >= 0)
                    errors.Add("定期評量或技職繁星不應套用學期成績計算確認：" + path);
            }
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
            return new ConfirmStudentRow { Group = student };
        }

        private static void Expect(List<string> errors, bool condition, string message)
        {
            if (!condition)
                errors.Add(message);
        }
    }
}
