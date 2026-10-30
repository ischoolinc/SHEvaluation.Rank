using System.Collections.Generic;
using System.Text;

namespace SHEvaluation.Rank.MotherGroup
{
    public static class RankCalculationConfirm
    {
        public static string GradeList(MotherGroupConfirmList list, IList<int> selectedGradeYears)
        {
            List<int> grades = GradeYears(list, selectedGradeYears);
            if (grades.Count == 0)
                return "（沒有可解析的年級）";
            StringBuilder text = new StringBuilder();
            for (int i = 0; i < grades.Count; i++)
            {
                if (i > 0)
                    text.Append("、");
                text.Append(grades[i]);
                text.Append("年級");
            }
            return text.ToString();
        }

        public static List<int> GradeYears(MotherGroupConfirmList list, IList<int> selectedGradeYears)
        {
            Dictionary<int, bool> selected = SelectedSet(selectedGradeYears);
            List<int> grades = new List<int>();
            if (list == null)
                return grades;
            foreach (ConfirmStudentRow row in list.Rows)
            {
                if (row == null || row.Group == null || string.IsNullOrEmpty(row.Group.StudentId))
                    continue;
                int gradeYear;
                if (!SemesterRankSql.TryParseGradeYear(row.Group.GetWorking(RankKinds.Year).GroupName, out gradeYear))
                    continue;
                if (!selected.ContainsKey(gradeYear) || grades.Contains(gradeYear))
                    continue;
                grades.Add(gradeYear);
            }
            grades.Sort();
            return grades;
        }

        public static List<SemesterRankGradeStudents> LimitToSelectedGrades(IList<SemesterRankGradeStudents> grades, IList<int> selectedGradeYears)
        {
            Dictionary<int, bool> selected = SelectedSet(selectedGradeYears);
            List<SemesterRankGradeStudents> kept = new List<SemesterRankGradeStudents>();
            if (grades == null)
                return kept;
            foreach (SemesterRankGradeStudents grade in grades)
            {
                if (grade == null || !selected.ContainsKey(grade.GradeYear))
                    continue;
                kept.Add(grade);
            }
            kept.Sort(delegate(SemesterRankGradeStudents left, SemesterRankGradeStudents right)
            {
                return left.GradeYear.CompareTo(right.GradeYear);
            });
            return kept;
        }

        public static string Message(string schoolYear, string semester, string sourceName, string gradeList)
        {
            return "即將計算學期成績固定排名。\n\n"
                + "計算學年度：" + schoolYear + "\n"
                + "計算學期：" + semester + "\n"
                + "母群來源：" + sourceName + "\n\n"       
                + "確定要繼續計算嗎？";
        }

        private static Dictionary<int, bool> SelectedSet(IList<int> selectedGradeYears)
        {
            Dictionary<int, bool> selected = new Dictionary<int, bool>();
            if (selectedGradeYears == null)
                return selected;
            foreach (int gradeYear in selectedGradeYears)
            {
                if (!selected.ContainsKey(gradeYear))
                    selected.Add(gradeYear, true);
            }
            return selected;
        }
    }
}
