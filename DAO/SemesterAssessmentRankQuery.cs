using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;

namespace SHEvaluation.Rank.DAO
{
    public static class SemesterAssessmentRankQuery
    {
        public const string GradeYearSql = "SELECT DISTINCT class.grade_year FROM class INNER JOIN student ON student.ref_class_id = class.id WHERE student.status = 1 AND class.grade_year IS NOT NULL ORDER BY class.grade_year ASC;";

        public const string OptionSql = @"
SELECT DISTINCT
    rank_matrix.school_year
    , rank_matrix.semester
    , SUBSTRING(rank_matrix.item_type from 1 for position('/' in rank_matrix.item_type) - 1) AS score_type
    , SUBSTRING(rank_matrix.item_type from position('/' in rank_matrix.item_type) + 1 for LENGTH(rank_matrix.item_type)) AS score_category
FROM
    rank_matrix
WHERE
    is_alive = true
    AND SUBSTRING(rank_matrix.item_type from 1 for position('/' in rank_matrix.item_type) - 1) = '學期'
ORDER BY
    1, 2, 3, 4";

        public static string DetailSql(string schoolYear, string semester, string scoreType, string scoreCategory, IList<string> gradeYears, IList<string> rankTypes)
        {
            int year;
            int term;
            if (!int.TryParse(schoolYear, NumberStyles.Integer, CultureInfo.InvariantCulture, out year)
                || !int.TryParse(semester, NumberStyles.Integer, CultureInfo.InvariantCulture, out term))
                throw new ArgumentException("學年度或學期不是整數。");
            List<int> grades = Integers(gradeYears);
            List<string> ranks = Texts(rankTypes);
            if (grades.Count == 0 || ranks.Count == 0)
                return "";

            StringBuilder sql = new StringBuilder();
            sql.Append("SELECT\n");
            sql.Append("\trank_matrix.id AS rank_matrix_id\n");
            sql.Append("\t, SUBSTRING(rank_matrix.item_type from 1 for position('/' in rank_matrix.item_type) - 1) AS score_type\n");
            sql.Append("\t, SUBSTRING(rank_matrix.item_type from position('/' in rank_matrix.item_type) + 1 for LENGTH(rank_matrix.item_type)) AS score_category\n");
            sql.Append("\t, rank_matrix.item_name\n");
            sql.Append("\t, rank_matrix.rank_type\n");
            sql.Append("\t, rank_matrix.rank_name\n");
            sql.Append("\t, class.class_name\n");
            sql.Append("\t, student.seat_no\n");
            sql.Append("\t, student.student_number\n");
            sql.Append("\t, student.name AS student_name\n");
            sql.Append("\t, rank_detail.ref_student_id\n");
            sql.Append("\t, rank_detail.score\n");
            sql.Append("\t, rank_detail.rank\n");
            sql.Append("\t, rank_detail.pr\n");
            sql.Append("\t, rank_detail.percentile\n");
            sql.Append("\t, rank_matrix.school_year\n");
            sql.Append("\t, rank_matrix.semester\n");
            sql.Append("\t, rank_matrix.create_time\n");
            sql.Append("\t, rank_matrix.grade_year\n");
            sql.Append("\t, rank_matrix.item_type\n");
            sql.Append("FROM rank_matrix\n");
            sql.Append("\tLEFT OUTER JOIN rank_detail ON rank_detail.ref_matrix_id = rank_matrix.id\n");
            sql.Append("\tLEFT OUTER JOIN student ON student.id = rank_detail.ref_student_id\n");
            sql.Append("\tLEFT OUTER JOIN class ON class.id = student.ref_class_id\n");
            sql.Append("WHERE rank_matrix.is_alive = true\n");
            sql.Append("\tAND rank_matrix.school_year = ").Append(year).Append('\n');
            sql.Append("\tAND rank_matrix.semester = ").Append(term).Append('\n');
            sql.Append("\tAND rank_matrix.item_type LIKE '學期%'\n");
            sql.Append("\tAND SUBSTRING(rank_matrix.item_type from 1 for position('/' in rank_matrix.item_type) - 1) = '").Append(Literal(scoreType)).Append("'\n");
            sql.Append("\tAND SUBSTRING(rank_matrix.item_type from position('/' in rank_matrix.item_type) + 1 for LENGTH(rank_matrix.item_type)) = '").Append(Literal(scoreCategory)).Append("'\n");
            sql.Append("\tAND rank_matrix.grade_year IN (");
            for (int i = 0; i < grades.Count; i++)
            {
                if (i > 0)
                    sql.Append(", ");
                sql.Append(grades[i]);
            }
            sql.Append(")\n\tAND rank_matrix.rank_type IN (");
            for (int i = 0; i < ranks.Count; i++)
            {
                if (i > 0)
                    sql.Append(", ");
                sql.Append('\'').Append(Literal(ranks[i])).Append('\'');
            }
            sql.Append(")\nORDER BY rank_matrix.grade_year, CASE rank_matrix.rank_type WHEN '班排名' THEN 1 WHEN '科排名' THEN 2 WHEN '年排名' THEN 3 WHEN '類別1排名' THEN 4 WHEN '類別2排名' THEN 5 ELSE 6 END, rank_matrix.rank_name, rank_detail.rank, rank_matrix.create_time DESC");
            return sql.ToString();
        }

        public static List<RankDataInfo> ReadRows(DataTable table)
        {
            List<RankDataInfo> rows = new List<RankDataInfo>();
            if (table == null)
                return rows;
            foreach (DataRow dataRow in table.Rows)
            {
                RankDataInfo row = new RankDataInfo();
                row.rank_matrix_id = Text(dataRow, "rank_matrix_id");
                row.score_type = Text(dataRow, "score_type");
                row.score_category = Text(dataRow, "score_category");
                row.item_name = Text(dataRow, "item_name");
                row.rank_type = Text(dataRow, "rank_type");
                row.rank_name = Text(dataRow, "rank_name");
                row.class_name = Text(dataRow, "class_name");
                row.seat_no = Text(dataRow, "seat_no");
                row.student_number = Text(dataRow, "student_number");
                row.student_name = Text(dataRow, "student_name");
                row.ref_student_id = Text(dataRow, "ref_student_id");
                row.score = Text(dataRow, "score");
                row.rank = Text(dataRow, "rank");
                row.pr = Text(dataRow, "pr");
                row.percentile = Text(dataRow, "percentile");
                row.school_year = Text(dataRow, "school_year");
                row.semester = Text(dataRow, "semester");
                row.create_time = Text(dataRow, "create_time");
                row.grade_year = Text(dataRow, "grade_year");
                row.item_type = Text(dataRow, "item_type");
                rows.Add(row);
            }
            return rows;
        }

        public static List<RankDataInfo> Filter(IList<RankDataInfo> rows, string itemName, string rankType, string studentNumber)
        {
            List<RankDataInfo> matched = new List<RankDataInfo>();
            if (rows == null)
                return matched;
            foreach (RankDataInfo row in rows)
            {
                if (row == null)
                    continue;
                if (!string.IsNullOrEmpty(itemName) && itemName != "全部" && itemName != (row.item_name ?? ""))
                    continue;
                if (!string.IsNullOrEmpty(rankType) && rankType != "全部" && rankType != (row.rank_type ?? ""))
                    continue;
                if (!string.IsNullOrEmpty(studentNumber) && (row.student_number ?? "").IndexOf(studentNumber) < 0)
                    continue;
                matched.Add(row);
            }
            return matched;
        }

        public static void SortRows(List<RankDataInfo> rows, int columnIndex, bool descending)
        {
            if (rows == null || rows.Count < 2 || columnIndex < 0)
                return;
            int count = rows.Count;
            int[] index = new int[count];
            RankDataInfo[] source = new RankDataInfo[count];
            for (int i = 0; i < count; i++)
            {
                index[i] = i;
                source[i] = rows[i];
            }
            Array.Sort<int>(index, delegate (int left, int right)
            {
                int compared = CompareRow(source[left], source[right], columnIndex, descending);
                return compared != 0 ? compared : left.CompareTo(right);
            });
            for (int i = 0; i < count; i++)
                rows[i] = source[index[i]];
        }

        private static int CompareRow(RankDataInfo left, RankDataInfo right, int columnIndex, bool descending)
        {
            if (columnIndex == 7 || columnIndex == 10 || columnIndex == 11 || columnIndex == 12 || columnIndex == 13)
            {
                decimal leftNumber;
                decimal rightNumber;
                bool leftOk = TryNumber(left, columnIndex, out leftNumber);
                bool rightOk = TryNumber(right, columnIndex, out rightNumber);
                if (leftOk != rightOk)
                    return leftOk ? -1 : 1;
                if (!leftOk)
                    return 0;
                int compared = leftNumber.CompareTo(rightNumber);
                return descending ? -compared : compared;
            }
            string leftText = Convert.ToString(CellValue(left, columnIndex) ?? "");
            string rightText = Convert.ToString(CellValue(right, columnIndex) ?? "");
            int textCompared = string.Compare(leftText, rightText, StringComparison.Ordinal);
            return descending ? -textCompared : textCompared;
        }

        private static bool TryNumber(RankDataInfo row, int columnIndex, out decimal number)
        {
            number = 0m;
            string text = null;
            if (row != null)
            {
                switch (columnIndex)
                {
                    case 7: text = row.seat_no; break;
                    case 10: text = row.score; break;
                    case 11: text = row.rank; break;
                    case 12: text = row.pr; break;
                    case 13: text = row.percentile; break;
                }
            }
            if (string.IsNullOrWhiteSpace(text))
                return false;
            return decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out number)
                || decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out number);
        }

        public static object CellValue(RankDataInfo row, int columnIndex)
        {
            if (row == null)
                return null;
            int number;
            decimal score;
            switch (columnIndex)
            {
                case 0: return row.rank_matrix_id ?? "";
                case 1: return row.score_type ?? "";
                case 2: return row.score_category ?? "";
                case 3: return row.item_name ?? "";
                case 4: return row.rank_type ?? "";
                case 5: return row.rank_name ?? "";
                case 6: return row.class_name ?? "";
                case 7: return int.TryParse(row.seat_no, out number) ? (int?)number : null;
                case 8: return row.student_number ?? "";
                case 9: return row.student_name ?? "";
                case 10: return decimal.TryParse(row.score, out score) ? (decimal?)score : null;
                case 11: return int.TryParse(row.rank, out number) ? (int?)number : null;
                case 12: return int.TryParse(row.pr, out number) ? (int?)number : null;
                case 13: return int.TryParse(row.percentile, out number) ? (int?)number : null;
                case 14: return "檢視";
                case 15: return row.school_year ?? "";
                case 16: return row.semester ?? "";
                default: return null;
            }
        }

        private static List<int> Integers(IList<string> values)
        {
            List<int> numbers = new List<int>();
            if (values == null)
                return numbers;
            foreach (string value in values)
            {
                int number;
                if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out number))
                    numbers.Add(number);
            }
            return numbers;
        }

        private static List<string> Texts(IList<string> values)
        {
            List<string> texts = new List<string>();
            if (values == null)
                return texts;
            foreach (string value in values)
            {
                if (!string.IsNullOrEmpty(value))
                    texts.Add(value);
            }
            return texts;
        }

        private static string Text(DataRow row, string column)
        {
            if (row == null || row.Table == null || !row.Table.Columns.Contains(column) || row.IsNull(column))
                return "";
            return Convert.ToString(row[column]);
        }

        private static string Literal(string value)
        {
            return (value ?? "").Replace("'", "''");
        }
    }
}
