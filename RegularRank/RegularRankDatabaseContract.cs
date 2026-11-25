using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace SHEvaluation.Rank.RegularRank
{
    public sealed class RegularRankDatabaseEvidence
    {
        public string DatabaseName { get; internal set; }
        public string ServerVersion { get; internal set; }
        public string Report { get; internal set; }
    }

    public static class RegularRankDatabaseContract
    {
        public const string ConstraintsSql = @"
SELECT 'constraint'::TEXT AS kind, c.conrelid::regclass::TEXT AS relation, c.conname::TEXT AS name, pg_get_constraintdef(c.oid)::TEXT AS definition
FROM pg_constraint c WHERE c.conrelid IN ('rank_batch'::regclass,'rank_batch_student'::regclass,'rank_matrix'::regclass,'rank_detail'::regclass)
UNION ALL SELECT 'index',t.relname::TEXT,i.relname::TEXT,pg_get_indexdef(x.indexrelid)::TEXT
FROM pg_index x INNER JOIN pg_class t ON t.oid=x.indrelid INNER JOIN pg_class i ON i.oid=x.indexrelid
WHERE t.oid IN ('rank_batch'::regclass,'rank_batch_student'::regclass,'rank_matrix'::regclass,'rank_detail'::regclass)
ORDER BY kind,relation,name";
        public const string ProbeSql = @"
SELECT current_database()::TEXT AS database_name, current_setting('server_version_num')::TEXT AS server_version,
       c.relname::TEXT AS table_name, a.attname::TEXT AS column_name,
       format_type(a.atttypid,a.atttypmod)::TEXT AS data_type,
       CASE WHEN a.attnotnull THEN 'NO' ELSE 'YES' END AS is_nullable
FROM pg_attribute a INNER JOIN pg_class c ON c.oid=a.attrelid
WHERE c.oid IN ('rank_batch'::regclass,'rank_batch_student'::regclass,'rank_matrix'::regclass,'rank_detail'::regclass)
  AND a.attnum>0 AND NOT a.attisdropped ORDER BY c.relname,a.attnum";

        public static RegularRankDatabaseEvidence Verify(DataTable table)
        {
            if (table == null || table.Rows.Count == 0) throw new InvalidOperationException("無法讀取排名資料表結構。");
            Dictionary<string, HashSet<string>> columns = new Dictionary<string, HashSet<string>>();
            StringBuilder report = new StringBuilder();
            string database = Convert.ToString(table.Rows[0]["database_name"]);
            string version = Convert.ToString(table.Rows[0]["server_version"]);
            report.AppendLine("database=" + database + "; server_version=" + version);
            foreach (DataRow row in table.Rows)
            {
                string name = Convert.ToString(row["table_name"]);
                if (!columns.ContainsKey(name)) columns.Add(name, new HashSet<string>());
                string column = Convert.ToString(row["column_name"]);
                columns[name].Add(column);
                report.AppendLine(name + "." + column + ": " + row["data_type"] + "; nullable=" + row["is_nullable"]);
            }
            Require(columns, "rank_batch", "id", "school_year", "semester", "setting", "extension", "calculation_description");
            Require(columns, "rank_batch_student", "id", "ref_batch_id", "ref_student_id", "grade_year", "matrix_grade", "matrix_dept", "matrix_class", "matrix_tag1", "matrix_tag2");
            Require(columns, "rank_matrix", "id", "ref_batch_id", "school_year", "semester", "grade_year", "item_type", "ref_exam_id", "item_name", "rank_type", "rank_name", "is_alive", "matrix_count");
            Require(columns, "rank_detail", "ref_matrix_id", "ref_student_id", "score", "rank", "pr", "percentile");
            return new RegularRankDatabaseEvidence { DatabaseName = database, ServerVersion = version, Report = report.ToString() };
        }

        private static void Require(Dictionary<string, HashSet<string>> columns, string table, params string[] names)
        {
            if (!columns.ContainsKey(table)) throw new InvalidOperationException("缺少資料表 " + table);
            foreach (string name in names) if (!columns[table].Contains(name)) throw new InvalidOperationException("缺少欄位 " + table + "." + name + "，不能開放計算。");
        }
    }
}
