using System;
using System.Collections.Generic;
using System.Text;

namespace SHEvaluation.Rank.MotherGroup
{
    public sealed class SemesterRankComparePlan
    {
        public string Scenario { get; private set; }
        public string Sql { get; private set; }

        public bool DatabaseExecuted
        {
            get { return false; }
        }

        internal SemesterRankComparePlan(string scenario, string sql)
        {
            Scenario = scenario;
            Sql = sql;
        }
    }

    public static class SemesterRankCompatibility
    {
        public static readonly string[] JoinColumns = new string[]
        {
            "student_id",
            "rank_school_year",
            "rank_semester",
            "rank_grade_year",
            "ref_exam_id",
            "item_type",
            "item_name",
            "rank_type",
            "rank_name",
            "duplicate_index"
        };

        public static readonly string[] ValueColumns = new string[]
        {
            "matrix_count",
            "score",
            "rank",
            "pr",
            "percentile",
            "avg_top_25",
            "avg_top_50",
            "avg",
            "avg_bottom_50",
            "avg_bottom_25",
            "std_dev_pop",
            "pr_88",
            "pr_75",
            "pr_50",
            "pr_25",
            "pr_12",
            "level_gte100",
            "level_90",
            "level_80",
            "level_70",
            "level_60",
            "level_50",
            "level_40",
            "level_30",
            "level_20",
            "level_10",
            "level_lt10"
        };

        public static string Compose(IList<string> studentSelects, int gradeYear, string schoolYear, string semester, string formulaFromSubjectScore)
        {
            if (studentSelects == null || studentSelects.Count == 0)
                throw new ArgumentException("學生清單是空的。");
            if (gradeYear <= 0)
                throw new ArgumentOutOfRangeException("gradeYear");
            if (formulaFromSubjectScore == null || formulaFromSubjectScore.IndexOf(", update_data AS") < 0)
                throw new ArgumentException("排名公式必須保留寫入起點，比對時才知道要從哪裡切掉。");
            if (formulaFromSubjectScore.IndexOf("unnest(xpath('/root/SemesterSubjectScoreInfo/Subject'") < 0)
                throw new ArgumentException("排名公式必須保留原本的科目展開。");

            StringBuilder sql = new StringBuilder();
            sql.Append("WITH student_list AS\n(\n");
            for (int i = 0; i < studentSelects.Count; i++)
            {
                if (i > 0)
                    sql.Append("\nUNION ALL\n");
                sql.Append(studentSelects[i]);
            }
            sql.Append("\n)\n, calc_condition AS\n(\n    SELECT\n        '");
            sql.Append(gradeYear.ToString());
            sql.Append("'::TEXT AS rank_grade_year\n        , '");
            sql.Append(Literal(schoolYear));
            sql.Append("'::TEXT AS rank_school_year\n        , '");
            sql.Append(Literal(semester));
            sql.Append("'::TEXT AS rank_semester\n        , '-1'::TEXT AS ref_exam_id\n        , '學期成績'::TEXT AS rank_exam_name\n        , ''::TEXT AS calculation_setting\n        , NULL::INT AS batch_id\n)\n");
            sql.Append(formulaFromSubjectScore);
            return sql.ToString();
        }

        public static SemesterRankComparePlan Build(string rankSql, string scenario)
        {
            if (rankSql == null)
                throw new ArgumentNullException("rankSql");
            if (string.IsNullOrEmpty(scenario))
                throw new ArgumentException("要比對的案例名稱是空的。");
            string text = rankSql.Replace("\r\n", "\n").Replace("\r", "\n");
            if (text.IndexOf("PARTITION BY rank_grade_year, subject, participate_year") >= 0)
                throw new InvalidOperationException("要比對的語句必須是套用參與旗標之前的排名 SQL。");
            int writeAt = text.IndexOf(", update_data AS");
            if (writeAt < 0)
                throw new InvalidOperationException("排名 SQL 缺少寫入起點，無法只取出計算結果。");
            string legacyBody = text.Substring(0, writeAt).Trim();
            string revised = SemesterRankSql.ApplyParticipation(text);
            int revisedWrite = revised.IndexOf(", update_data AS");
            if (revisedWrite < 0)
                throw new InvalidOperationException("套用參與旗標後找不到寫入起點。");
            string revisedBody = revised.Substring(0, revisedWrite).Trim();
            string sql = Assemble(scenario, Projection(legacyBody), Projection(revisedBody));
            EnsureReadOnly(sql);
            return new SemesterRankComparePlan(scenario, sql);
        }

        private static string Assemble(string scenario, string legacyQuery, string revisedQuery)
        {
            StringBuilder sql = new StringBuilder();
            sql.Append("/* scenario: ").Append(scenario).Append(" */\n");
            sql.Append("/* database_executed: false */\n");
            sql.Append("WITH legacy_score AS (\n/* begin-legacy */\n");
            sql.Append(legacyQuery);
            sql.Append("\n/* end-legacy */\n), revised_score AS (\n/* begin-revised */\n");
            sql.Append(revisedQuery);
            sql.Append("\n/* end-revised */\n), legacy_row AS (\n");
            sql.Append(Numbered("legacy_score"));
            sql.Append("\n), revised_row AS (\n");
            sql.Append(Numbered("revised_score"));
            sql.Append("\n)\n");
            sql.Append(DiffSelect());
            return sql.ToString();
        }

        private static string Projection(string body)
        {
            StringBuilder sql = new StringBuilder();
            sql.Append(body);
            sql.Append("\nSELECT\n");
            sql.Append("    rank_school_year\n");
            sql.Append("    , rank_semester\n");
            sql.Append("    , rank_grade_year\n");
            sql.Append("    , ref_exam_id\n");
            sql.Append("    , item_type\n");
            sql.Append("    , item_name\n");
            sql.Append("    , rank_type\n");
            sql.Append("    , rank_name\n");
            foreach (string column in ValueColumns)
                sql.Append("    , ").Append(column).Append("\n");
            sql.Append("    , student_id\n");
            sql.Append("FROM\n    score_list");
            return sql.ToString();
        }

        private static string Numbered(string sourceName)
        {
            StringBuilder sql = new StringBuilder();
            sql.Append("SELECT\n    base.*\n    , ROW_NUMBER() OVER (\n        PARTITION BY ");
            bool first = true;
            foreach (string column in JoinColumns)
            {
                if (column == "duplicate_index")
                    continue;
                if (!first)
                    sql.Append(", ");
                first = false;
                sql.Append(column);
            }
            sql.Append("\n        ORDER BY ");
            first = true;
            foreach (string column in ValueColumns)
            {
                if (!first)
                    sql.Append(", ");
                first = false;
                sql.Append(column);
            }
            sql.Append("\n    ) AS duplicate_index\nFROM\n    ");
            sql.Append(sourceName).Append(" AS base");
            return sql.ToString();
        }

        private static string DiffSelect()
        {
            StringBuilder sql = new StringBuilder();
            sql.Append("SELECT\n");
            sql.Append("    CASE\n");
            sql.Append("        WHEN legacy_row.student_id IS NULL THEN 'only_revised'\n");
            sql.Append("        WHEN revised_row.student_id IS NULL THEN 'only_legacy'\n");
            sql.Append("        ELSE 'value_diff'\n");
            sql.Append("    END AS difference_kind\n");
            sql.Append("    , false AS database_executed\n");
            foreach (string column in JoinColumns)
            {
                sql.Append("    , COALESCE(legacy_row.").Append(column).Append(", revised_row.").Append(column).Append(") AS ").Append(column).Append("\n");
            }
            foreach (string column in ValueColumns)
            {
                sql.Append("    , legacy_row.").Append(column).Append(" AS legacy_").Append(column).Append("\n");
                sql.Append("    , revised_row.").Append(column).Append(" AS revised_").Append(column).Append("\n");
            }
            sql.Append("FROM\n    legacy_row\n    FULL OUTER JOIN revised_row\n        ON ");
            for (int i = 0; i < JoinColumns.Length; i++)
            {
                if (i > 0)
                    sql.Append("\n        AND ");
                sql.Append("legacy_row.").Append(JoinColumns[i]).Append(" IS NOT DISTINCT FROM revised_row.").Append(JoinColumns[i]);
            }
            sql.Append("\nWHERE\n    legacy_row.student_id IS NULL\n    OR revised_row.student_id IS NULL\n");
            foreach (string column in ValueColumns)
                sql.Append("    OR legacy_row.").Append(column).Append(" IS DISTINCT FROM revised_row.").Append(column).Append("\n");
            return sql.ToString();
        }

        private static void EnsureReadOnly(string sql)
        {
            string[] forbidden = new string[]
            {
                "INSERT", "UPDATE", "DELETE", "TRUNCATE", "MERGE", "INTO", "RETURNING", "CALL", "COPY", "ALTER", "DROP", "CREATE"
            };
            foreach (string keyword in forbidden)
            {
                if (ContainsKeyword(sql, keyword))
                    throw new InvalidOperationException("唯讀比對含有資料寫入關鍵字：" + keyword);
            }
            string compact = sql.Replace(" ", "").Replace("\n", "").Replace("\t", "");
            if (compact.IndexOf("is_alive=NULL", StringComparison.OrdinalIgnoreCase) >= 0)
                throw new InvalidOperationException("唯讀比對不應停用既有排名。");
        }

        private static bool ContainsKeyword(string sql, string keyword)
        {
            int at = 0;
            while (at <= sql.Length - keyword.Length)
            {
                at = sql.IndexOf(keyword, at, StringComparison.OrdinalIgnoreCase);
                if (at < 0)
                    return false;
                bool left = at == 0 || !IsIdent(sql[at - 1]);
                int end = at + keyword.Length;
                bool right = end >= sql.Length || !IsIdent(sql[end]);
                if (left && right)
                    return true;
                at = end;
            }
            return false;
        }

        private static bool IsIdent(char value)
        {
            return char.IsLetterOrDigit(value) || value == '_';
        }

        private static string Literal(string value)
        {
            return (value ?? "").Replace("'", "''");
        }
    }
}
