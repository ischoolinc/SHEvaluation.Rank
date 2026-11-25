using Aspose.Cells;
using SHEvaluation.Rank.MotherGroup;
using System;
using System.Collections.Generic;
using System.Data;

namespace SHEvaluation.Rank.RegularRank
{
    public static class RegularRankExcelSourceCheck
    {
        public static int Main(string[] args)
        {
            try
            {
                StudentMotherGroup group = new StudentMotherGroup { StudentId = "42", StudentNumber = "00123", StudentName = "原姓名", Status = 2 };
                List<RankSlot> slots = new List<RankSlot>();
                foreach (string kind in RankKinds.All)
                    slots.Add(new RankSlot { RankType = kind, GroupName = kind == RankKinds.Year ? "1年級" : "測試", Participate = kind != RankKinds.Class });
                group.SetSourceSlots(slots);
                MotherGroupInitialParticipation.Apply(MotherGroupSourceKind.Excel, group, false);
                MotherGroupConfirmList list = new MotherGroupConfirmList();
                list.Load(new List<ConfirmStudentRow> { new ConfirmStudentRow { Group = group } });
                Workbook workbook = MotherGroupExcelExchange.Create(list, null);
                DataTable table = new DataTable();
                foreach (string column in new string[] { "student_id", "student_number", "student_name", "student_status" }) table.Columns.Add(column);
                table.Rows.Add("42", "00123", "主檔姓名", "2");
                string warning;
                string captured = "";
                List<ConfirmStudentRow> loaded = RegularRankExcelSource.Load(workbook,
                    delegate(string sql) { captured = sql; return table; }, "", null, null, out warning);
                Require(captured.Contains("'00123'") && loaded.Count == 1, "前導零學號及身分查詢");
                Require(loaded[0].Group.StudentNumber == "00123" && loaded[0].Group.StudentName == "主檔姓名" && warning.Length > 0, "姓名差異以主檔為準並警告");
                Require(!loaded[0].Group.GetWorking(RankKinds.Class).Participate, "保留不參與旗標");
                table.Rows.Add("43", "00123", "重複姓名", "2");
                bool rejected = false;
                try { RegularRankExcelSource.Load(workbook, delegate(string sql) { return table; }, "", null, null, out warning); }
                catch (InvalidOperationException) { rejected = true; }
                Require(rejected, "多筆身分必須整份拒絕");
                table.Rows.RemoveAt(1);
                workbook.Worksheets[0].Cells[1, 8].PutValue("錯誤旗標");
                rejected = false;
                try { RegularRankExcelSource.Load(workbook, delegate(string sql) { return table; }, "", null, null, out warning); }
                catch (InvalidOperationException ex) { rejected = ex.Message.Contains("2"); }
                Require(rejected, "無效旗標整份拒絕且有列號");
                Console.WriteLine("regular-rank-excel-source-check: ok; database-executed: false");
                return 0;
            }
            catch (Exception ex) { Console.WriteLine(ex); return 1; }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
