using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SHEvaluation.Rank.MotherGroup
{
    public static class MotherGroupPhase9Task93Check
    {
        public static int Main(string[] args)
        {
            try
            {
                List<string> errors = new List<string>();
                errors.AddRange(CheckCopy());
                string form = File.ReadAllText("CalculateSemesterAssessmentRank.cs", Encoding.UTF8);
                string gate = File.ReadAllText("MotherGroup\\SemesterRankWriteGate.cs", Encoding.UTF8);
                errors.AddRange(CheckForm(form, gate));
                if (errors.Count == 0)
                {
                    Console.WriteLine("phase9-task93-check: ok");
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

        private static List<string> CheckCopy()
        {
            List<string> errors = new List<string>();
            MotherGroupConfirmList source = new MotherGroupConfirmList();
            ConfirmStudentRow row = new ConfirmStudentRow();
            row.Group = Student("s1", "1001", "王小明", true);
            source.Rows.Add(row);
            StudentMotherGroup original = source.Rows[0].Group;
            original.GetWorking(RankKinds.Year).Participate = true;
            original.GetWorking(RankKinds.Department).Participate = false;
            original.GetWorking(RankKinds.Class).Participate = true;
            original.GetWorking(RankKinds.Tag1).Participate = false;
            original.GetWorking(RankKinds.Tag2).Participate = true;
            original.GetWorking(RankKinds.Class).ConflictNames = new List<string>();
            original.GetWorking(RankKinds.Class).ConflictNames.Add("甲班");
            original.GetWorking(RankKinds.Class).ConflictNames.Add("乙班");

            MotherGroupConfirmList copy = source.CopyWorking();
            StudentMotherGroup cloned = copy.Rows[0].Group;
            Expect(errors, cloned != original && cloned.Working != original.Working, "工作副本不與畫面學生共用物件");
            Expect(errors, cloned.StudentId == "s1" && cloned.StudentNumber == "1001" && cloned.StudentName == "王小明", "工作副本保留學生 ID、學號與姓名");
            foreach (string rankType in RankKinds.All)
            {
                RankSlot left = original.GetWorking(rankType);
                RankSlot right = cloned.GetWorking(rankType);
                Expect(errors, right != left && right.GroupName == left.GroupName && right.Participate == left.Participate, rankType + " 的名稱與參與旗標要複製成新物件");
            }
            RankSlot copiedClass = cloned.GetWorking(RankKinds.Class);
            Expect(errors, copiedClass.ConflictNames != original.GetWorking(RankKinds.Class).ConflictNames && copiedClass.ConflictNames.Count == 2 && copiedClass.HasConflict, "衝突名稱集合要深層複製");

            original.GetWorking(RankKinds.Year).Participate = false;
            original.GetWorking(RankKinds.Year).GroupName = "改掉";
            original.GetWorking(RankKinds.Class).ConflictNames.Add("丙班");
            Expect(errors, cloned.GetWorking(RankKinds.Year).Participate && cloned.GetWorking(RankKinds.Year).GroupName == "1年級" && cloned.GetWorking(RankKinds.Class).ConflictNames.Count == 2, "修改原清單不影響已建立的工作副本");
            return errors;
        }

        private static List<string> CheckForm(string form, string gate)
        {
            List<string> errors = new List<string>();
            int click = form.IndexOf("void btnCacluate_Click");
            int resize = form.IndexOf("void CalculateSemesterAssessmentRank_Resize");
            string body = click >= 0 && resize > click ? form.Substring(click, resize - click) : "";
            int gateCall = body.IndexOf("CanWriteRankTables");
            int gateReturn = body.IndexOf("return;");
            int schoolYear = body.IndexOf("string schoolYear = lbSchoolYear.Text");
            int empty = body.IndexOf("沒有可計算的年級");
            int confirm = body.IndexOf("RankCalculationConfirm.Message");
            int save = body.IndexOf("cd.Save()");
            int build = body.IndexOf("SemesterRankSql.Build(");
            int publish = body.IndexOf("SemesterRankPublishSql.Build(");
            int apply = body.IndexOf("SemesterRankSql.ApplyParticipation");
            int select = body.IndexOf("queryHelper.Select(insertRankSql)");
            Expect(errors, gateCall >= 0 && gateReturn > gateCall && gateReturn < schoolYear, "寫入閘門的返回仍是第一個返回，且在讀取學年度之前");
            Expect(errors, empty > gateReturn && empty < confirm && confirm < save, "空年級提示在閘門返回之後、確認框之前");
            string emptySlice = gateReturn >= 0 && confirm > gateReturn ? body.Substring(gateReturn, confirm - gateReturn) : "";
            Expect(errors, emptySlice.IndexOf("cd.Save()") < 0 && emptySlice.IndexOf("SemesterRankSql.Build(") < 0 && emptySlice.IndexOf("queryHelper.Select") < 0, "沒有可計算年級時不儲存組態、不產生排名 SQL");
            Expect(errors, body.IndexOf("CommitPendingParticipationEdit()") >= 0 && body.IndexOf("CommitPendingParticipationEdit()") < body.IndexOf("CopyWorking()") && body.IndexOf("CopyWorking()") < empty, "先提交未完成編輯，再建立工作副本，然後才判斷空年級");
            Expect(errors, body.IndexOf("_capturingCalculationEdit = true") < 0, "提交未完成編輯時使用的旗標不在計算方法裡誤擋");
            Expect(errors, form.IndexOf("_capturingCalculationEdit = true") >= 0 && form.IndexOf("CalculationEditAllowed()") >= 0, "執行中仍允許這一次把未提交編輯寫入確認清單");
            Expect(errors, save > confirm && build > save && body.IndexOf("SemesterRankSql.Build(calculationList)") > save && publish > build && apply > publish && select > apply, "儲存、組句、公布、參與旗標與唯一查詢的順序維持不變");
            Expect(errors, body.IndexOf("RankCalculationConfirm.GradeList(calculationList, _GradeYearList)") > gateReturn && body.IndexOf("MotherGroupSnapshotJson.Compose(\"[]\", calculationList") > save && body.IndexOf("foreach (var item in gradeSelectCopy)") > confirm && body.IndexOf("gradeSelectCopy.Contains(") > save, "確認框、快照、擇優 XML 與 SQL 擇優片段使用同一份副本");

            int worker = body.IndexOf("bkw.DoWork +=");
            int completed = body.IndexOf("bkw.RunWorkerCompleted +=");
            string work = worker >= 0 && completed > worker ? body.Substring(worker, completed - worker) : "";
            Expect(errors, work.IndexOf("_confirmList") < 0 && work.IndexOf("_GradeSelectList") < 0, "背景工作不直接讀取畫面確認清單或擇優清單");
            Expect(errors, form.IndexOf("if (!CalculationEditAllowed()") >= 0 && form.IndexOf("CreateParticipationMenu(dgvStudentList, _confirmList, _lblMotherGroupSummary, _dgvRankPopulation, _dgvGroupCounts, CalculationEditAllowed)") >= 0, "參與核取、還原、加入學生與右鍵批次都檢查執行中旗標");
            string restore = Method(form, "void RestoreCalculationEditors()");
            Expect(errors, restore.IndexOf("btnCacluate.Enabled = _calculationUiState.CalculateEnabled") >= 0 && restore.IndexOf("btnCacluate.Enabled = true") < 0 && restore.IndexOf(".Visible = _calculationUiState.AddVisible") >= 0, "恢復控制項時使用原本的 Enabled 與 Visible，不無條件啟用");
            Expect(errors, body.IndexOf("btnCacluate.Enabled = false") > confirm && body.IndexOf("_btnAddHistoryStudent.Visible = true") < 0, "確定後才停用計算，且不把加入學生改成可見");
            Expect(errors, gate.IndexOf("return true;") >= 0 && gate.IndexOf("return false;") < 0, "SemesterRankWriteGate 應回傳 true");
            return errors;
        }

        private static StudentMotherGroup Student(string id, string number, string name, bool participate)
        {
            StudentMotherGroup student = new StudentMotherGroup();
            student.StudentId = id;
            student.StudentNumber = number;
            student.StudentName = name;
            List<RankSlot> slots = new List<RankSlot>();
            foreach (string rankType in RankKinds.All)
            {
                slots.Add(new RankSlot { RankType = rankType, GroupName = rankType == RankKinds.Year ? "1年級" : rankType, Participate = participate });
            }
            student.SetSourceSlots(slots);
            return student;
        }

        private static string Method(string form, string signature)
        {
            int start = form.IndexOf(signature);
            int end = start < 0 ? -1 : form.IndexOf("private void", start + signature.Length);
            if (start < 0 || end <= start)
                return "";
            return form.Substring(start, end - start);
        }

        private static void Expect(List<string> errors, bool condition, string message)
        {
            if (!condition)
                errors.Add(message);
        }
    }
}
