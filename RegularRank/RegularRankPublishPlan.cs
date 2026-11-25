using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SHEvaluation.Rank.RegularRank
{
    public sealed class RegularRankGradeOutcome
    {
        public int GradeYear { get; set; }
        public int StudentCount { get; set; }
        public bool? HasNewDetail { get; set; }
    }

    public sealed class RegularRankPublishPlan
    {
        public bool ExecutionAllowed { get; private set; }
        public bool SingleStatement { get; private set; }
        public bool PerGradeCommit { get; private set; }
        public bool IsolateClassWindowsByGrade { get; private set; }
        public bool WholeBatchFails { get; private set; }
        public string BlockReason { get; private set; }
        private List<int> replacementGrades;

        public ReadOnlyCollection<int> ReplacementGrades
        {
            get { return replacementGrades.AsReadOnly(); }
        }

        public static RegularRankPublishPlan Create(IList<RegularRankGradeOutcome> grades, bool classWindowLacksGrade)
        {
            RegularRankPublishPlan plan = new RegularRankPublishPlan();
            plan.SingleStatement = true;
            plan.PerGradeCommit = false;
            plan.IsolateClassWindowsByGrade = classWindowLacksGrade;
            plan.replacementGrades = new List<int>();
            plan.WholeBatchFails = false;
            if (grades != null)
            {
                foreach (RegularRankGradeOutcome grade in grades)
                {
                    if (grade == null || grade.StudentCount <= 0)
                        continue;
                    plan.replacementGrades.Add(grade.GradeYear);
                    if (grade.HasNewDetail.HasValue && !grade.HasNewDetail.Value)
                        plan.WholeBatchFails = true;
                }
            }
            if (plan.replacementGrades.Count == 0)
            {
                plan.ExecutionAllowed = false;
                plan.BlockReason = "沒有實際計算年級，不發布排名。";
            }
            else if (plan.WholeBatchFails)
            {
                plan.ExecutionAllowed = false;
                plan.BlockReason = "有實際年級沒有新明細，整次必須失敗並保留全部舊有效排名。";
            }
            else
            {
                plan.ExecutionAllowed = true;
                plan.BlockReason = "";
            }
            return plan;
        }
    }

    public sealed class RegularRankCalculationGuard
    {
        private bool running;
        private bool editorsEnabled;

        public bool Running
        {
            get { return running; }
        }

        public bool EditorsEnabled
        {
            get { return editorsEnabled; }
        }

        public RegularRankCalculationGuard()
        {
            editorsEnabled = true;
        }

        public bool TryEnter()
        {
            if (running)
                return false;
            running = true;
            editorsEnabled = false;
            return true;
        }

        public void FailAndRestore()
        {
            running = false;
            editorsEnabled = true;
        }
    }

    public static class RegularRankExistingRankLookup
    {
        public static string AliveMatricesSql(string schoolYear, string semester, string examId, IList<int> grades)
        {
            if (string.IsNullOrEmpty(schoolYear) || string.IsNullOrEmpty(semester) || string.IsNullOrEmpty(examId))
                throw new ArgumentException("目標期間與 exam ID 不可空白。");
            System.Text.StringBuilder sql = new System.Text.StringBuilder();
            sql.Append("SELECT rank_matrix.id, rank_matrix.ref_batch_id, rank_matrix.grade_year, rank_matrix.item_type, rank_matrix.ref_exam_id, rank_matrix.is_alive");
            sql.Append(" FROM rank_matrix WHERE rank_matrix.is_alive = true");
            sql.Append(" AND rank_matrix.school_year = ").Append(RegularRankCandidateSql.Literal(schoolYear)).Append("::INT");
            sql.Append(" AND rank_matrix.semester = ").Append(RegularRankCandidateSql.Literal(semester)).Append("::INT");
            sql.Append(" AND rank_matrix.ref_exam_id = ").Append(RegularRankCandidateSql.Literal(examId)).Append("::INT");
            sql.Append(" AND rank_matrix.item_type LIKE '定期評量/%'");
            if (grades != null && grades.Count > 0)
            {
                sql.Append(" AND rank_matrix.grade_year IN (");
                for (int i = 0; i < grades.Count; i++)
                {
                    if (i > 0) sql.Append(",");
                    sql.Append(grades[i].ToString(System.Globalization.CultureInfo.InvariantCulture));
                }
                sql.Append(")");
            }
            sql.Append(" ORDER BY rank_matrix.id");
            return sql.ToString();
        }

        public static string Describe()
        {
            return "既有有效定期排名以目標學年期、exam ID、實際年級及 item_type LIKE '定期評量/%' 檢索；不混入學期成績。";
        }
    }
}
