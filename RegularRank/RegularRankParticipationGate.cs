using SHEvaluation.Rank.MotherGroup;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SHEvaluation.Rank.RegularRank
{
    public sealed class RegularRankParticipationCoverage
    {
        public bool Covered { get; private set; }
        public bool ClassWindowLacksGrade { get; private set; }
        public string Message { get; private set; }

        internal static RegularRankParticipationCoverage Ok(bool classWindowLacksGrade)
        {
            RegularRankParticipationCoverage coverage = new RegularRankParticipationCoverage();
            coverage.Covered = true;
            coverage.ClassWindowLacksGrade = classWindowLacksGrade;
            coverage.Message = "";
            return coverage;
        }

        internal static RegularRankParticipationCoverage Missing(string message)
        {
            RegularRankParticipationCoverage coverage = new RegularRankParticipationCoverage();
            coverage.Covered = false;
            coverage.ClassWindowLacksGrade = false;
            coverage.Message = message ?? "";
            return coverage;
        }
    }

    public static class RegularRankParticipationGate
    {
        public static readonly string[] ScoreSources = new string[]
        {
            "exam_score AS", "exam_score_tag1 AS", "exam_score_tag2 AS",
            "domain_score AS", "domain_score_tag1 AS", "domain_score_tag2 AS",
            "calc_sum_score AS", "calc_sum_score_tag1 AS", "calc_sum_score_tag2 AS",
            "calc_avg_score AS", "calc_avg_score_tag1 AS", "calc_avg_score_tag2 AS",
            "weight_sum_score AS", "weight_sum_score_tag1 AS", "weight_sum_score_tag2 AS",
            "weight_avg_score AS", "weight_avg_score_tag1 AS", "weight_avg_score_tag2 AS"
        };

        public static readonly string[] RankBranches = new string[]
        {
            "subject_rank_row AS", "subject_rank_row_tag1 AS", "subject_rank_row_tag2 AS",
            "domain_rank_row AS", "domain_rank_row_tag1 AS", "domain_rank_row_tag2 AS",
            "calc_sum_rank_row AS", "calc_sum_rank_row_tag1 AS", "calc_sum_rank_row_tag2 AS",
            "calc_avg_rank_row AS", "calc_avg_rank_row_tag1 AS", "calc_avg_rank_row_tag2 AS",
            "weight_sum_rank_row AS", "weight_sum_rank_row_tag1 AS", "weight_sum_rank_row_tag2 AS",
            "weight_avg_rank_row AS", "weight_avg_rank_row_tag1 AS", "weight_avg_rank_row_tag2 AS"
        };

        public static RegularRankParticipationCoverage Cover(string rankSql)
        {
            string sql = rankSql ?? "";
            if (sql.Contains("ApplyParticipation"))
                return RegularRankParticipationCoverage.Missing("定期參與閘門不能呼叫 SemesterRankSql.ApplyParticipation。");
            foreach (string marker in ScoreSources)
            {
                if (!sql.Contains(marker))
                    return RegularRankParticipationCoverage.Missing("缺少成績來源標記：" + marker);
            }
            foreach (string marker in RankBranches)
            {
                if (!sql.Contains(marker))
                    return RegularRankParticipationCoverage.Missing("缺少排名分支標記：" + marker);
            }
            if (!sql.Contains("score_list AS"))
                return RegularRankParticipationCoverage.Missing("缺少 score_list，不能把統計接到參與集合。");
            if (!sql.Contains("CASE WHEN sce_take.score = -1 THEN 0 ELSE sce_take.score END"))
                return RegularRankParticipationCoverage.Missing("缺少原始特殊分數轉換，不能改寫公式。");
            if (!sql.Contains("AND exam.id = row.ref_exam_id::INT"))
                return RegularRankParticipationCoverage.Missing("成績查詢尚未改用目標 exam ID。");
            bool classWindowLacksGrade = sql.Contains("PARTITION BY rank_class_name, subject")
                || sql.Contains("PARTITION BY rank_class_name, domain");
            return RegularRankParticipationCoverage.Ok(classWindowLacksGrade);
        }

        public static string PopulationValues(MotherGroupConfirmList list, out string error)
        {
            error = "";
            if (list == null || list.Rows.Count == 0)
            {
                error = "沒有確認清單，不能建立參與集合。";
                return "";
            }
            StringBuilder values = new StringBuilder();
            List<string> seen = new List<string>();
            foreach (ConfirmStudentRow row in list.Rows)
            {
                if (row == null || row.Group == null)
                {
                    error = "確認清單有空白學生，停止建立參與集合。";
                    return "";
                }
                string studentId = (row.Group.StudentId ?? "").Trim();
                long number;
                if (!long.TryParse(studentId, NumberStyles.None, CultureInfo.InvariantCulture, out number) || number <= 0 || number.ToString(CultureInfo.InvariantCulture) != studentId)
                {
                    error = "學生識別不是正整數，停止建立參與集合。";
                    return "";
                }
                if (seen.Contains(studentId))
                {
                    error = "參與集合有重複學生，停止。";
                    return "";
                }
                seen.Add(studentId);
                if (values.Length > 0)
                    values.Append(",");
                values.Append("(");
                values.Append(studentId);
                foreach (string rankType in RankKinds.All)
                {
                    values.Append(",");
                    values.Append(row.Group.GetWorking(rankType).Participate ? "true" : "false");
                }
                values.Append(")");
            }
            return "regular_participation(student_id, year_participate, dept_participate, class_participate, tag1_participate, tag2_participate) AS (VALUES " + values + ")";
        }
    }
}
