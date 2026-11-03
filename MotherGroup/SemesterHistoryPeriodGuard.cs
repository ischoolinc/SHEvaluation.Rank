using System;
using System.Data;

namespace SHEvaluation.Rank.MotherGroup
{
    public static class SemesterHistoryPeriodGuard
    {
        public const string MismatchMessage = "所選批次的學年度、學期與畫面上的歷史期間不一致，不會進入確認清單，也不會讀取學生。";

        public static string SelectSql(string batchId)
        {
            long id;
            if (!long.TryParse(batchId == null ? "" : batchId.Trim(), out id) || id <= 0 || id.ToString() != batchId.Trim())
                throw new ArgumentException("歷史批次識別不是有效的數字，不查詢期間。");
            return "SELECT\n\tid\n\t, school_year\n\t, semester\nFROM rank_batch\nWHERE id = " + id.ToString();
        }

        public static bool Matches(DataTable table, string batchId, string historyYear, string historySemester)
        {
            if (table == null || table.Rows.Count != 1)
                return false;
            if (!table.Columns.Contains("id") || !table.Columns.Contains("school_year") || !table.Columns.Contains("semester"))
                return false;
            string id = Cell(table.Rows[0], "id").Trim();
            if (id != (batchId == null ? "" : batchId.Trim()))
                return false;
            string year = Cell(table.Rows[0], "school_year").Trim();
            string term = Cell(table.Rows[0], "semester").Trim();
            return year == (historyYear == null ? "" : historyYear.Trim())
                && term == (historySemester == null ? "" : historySemester.Trim());
        }

        private static string Cell(DataRow row, string column)
        {
            object value = row[column];
            if (value == null || value == DBNull.Value)
                return "";
            return value.ToString();
        }
    }
}
