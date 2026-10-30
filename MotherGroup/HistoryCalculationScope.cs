using System.Collections.Generic;
using System.Text;

namespace SHEvaluation.Rank.MotherGroup
{
    public static class HistoryCalculationScope
    {
        public static string UnparsedYearMessage(MotherGroupConfirmList list)
        {
            if (list == null)
                return "";
            StringBuilder text = new StringBuilder();
            foreach (ConfirmStudentRow row in list.Rows)
            {
                if (row == null || row.Group == null || string.IsNullOrEmpty(row.Group.StudentId))
                    continue;
                string yearName = row.Group.GetWorking(RankKinds.Year).GroupName ?? "";
                int gradeYear;
                if (SemesterRankSql.TryParseGradeYear(yearName, out gradeYear))
                    continue;
                if (text.Length == 0)
                    text.Append("以下學生的年排名名稱無法解析為年級，仍保留在確認清單。不產生該生的新排名明細，也不改用目前年級。");
                text.Append("\n");
                text.Append(row.Group.StudentId);
                text.Append("：");
                text.Append(yearName.Length == 0 ? "（空白）" : yearName);
            }
            return text.ToString();
        }

        public static bool KeepsStudentWithoutScore(MotherGroupConfirmList list, string studentId)
        {
            if (list == null || string.IsNullOrEmpty(studentId))
                return false;
            foreach (ConfirmStudentRow row in list.Rows)
            {
                if (row != null && row.Group != null && row.Group.StudentId == studentId)
                    return true;
            }
            return false;
        }
    }
}
