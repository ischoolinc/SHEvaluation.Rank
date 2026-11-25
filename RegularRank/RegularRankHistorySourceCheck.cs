using SHEvaluation.Rank.MotherGroup;
using System;
using System.Collections.Generic;
using System.Data;

namespace SHEvaluation.Rank.RegularRank
{
    public static class RegularRankHistorySourceCheck
    {
        public static int Main(string[] args)
        {
            try
            {
                RegularRankExamChoice[] exams = { new RegularRankExamChoice { Id = "7", Name = "第一次" }, new RegularRankExamChoice { Id = "8", Name = "第一次" } };
                DataTable catalog = new DataTable();
                foreach (string column in new string[] { "id", "school_year", "semester", "setting", "extension", "source_exam_count", "source_exam_id" }) catalog.Columns.Add(column);
                catalog.Rows.Add("9", "114", "2", "<Setting 考試名稱='第一次' ref_exam_id='7' />", "", "1", "7");
                RegularRankHistoryBatch batch = RegularRankHistorySource.ReadCatalog(catalog, exams)[0];
                Require(batch.Error == "" && batch.Exam.ExamId == "7", "同名考試由 ID 識別");
                StudentMotherGroup student = new StudentMotherGroup { StudentId = "42", StudentNumber = "00123", StudentName = "歷史學生" };
                List<RankSlot> slots = new List<RankSlot>();
                foreach (string kind in RankKinds.All) slots.Add(new RankSlot { RankType = kind, GroupName = kind == RankKinds.Year ? "1年級" : "歷史母群", Participate = kind != RankKinds.Class });
                student.SetSourceSlots(slots);
                MotherGroupInitialParticipation.Apply(MotherGroupSourceKind.Current, student, false);
                MotherGroupConfirmList list = new MotherGroupConfirmList();
                list.Load(new[] { new ConfirmStudentRow { Group = student } });
                batch.Extension = MotherGroupSnapshotJson.Compose("[]", list, MotherGroupSourceKind.Current, null);
                string notice;
                List<ConfirmStudentRow> restored = RegularRankHistorySource.Restore(batch,
                    delegate(string sql) { throw new Exception("有快照不應讀降級資料"); }, "", null, null, out notice);
                Require(restored.Count == 1 && restored[0].Group.StudentNumber == "00123" && !restored[0].Group.GetWorking(RankKinds.Class).Participate, "還原完整旗標及前導零");
                batch.Extension = "[broken";
                bool rejected = false;
                try { RegularRankHistorySource.Restore(batch, delegate(string sql) { throw new Exception("損壞快照不可降級"); }, "", null, null, out notice); }
                catch (InvalidOperationException) { rejected = true; }
                Require(rejected, "壞 JSON 停止");
                catalog.Rows[0]["setting"] = "<Setting 考試名稱='第一次' />";
                Require(RegularRankHistorySource.ReadCatalog(catalog, exams)[0].Exam.ExamId == "7", "舊設定無 ID 時以唯一矩陣 ID 識別，不任取同名考試");
                catalog.Rows[0]["source_exam_count"] = "2";
                Require(RegularRankHistorySource.ReadCatalog(catalog, exams)[0].Error.Length > 0, "多個矩陣考試 ID 阻擋");
                Require(RegularRankHistorySource.CatalogSql("114", "2").Contains("ORDER BY b.id DESC"), "目錄 ID 降冪且不依有效旗標排除歷史");
                Console.WriteLine("regular-rank-history-source-check: ok; database-executed: false");
                return 0;
            }
            catch (Exception ex) { Console.WriteLine(ex); return 1; }
        }
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
