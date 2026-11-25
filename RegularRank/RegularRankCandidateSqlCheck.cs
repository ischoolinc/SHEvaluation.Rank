using SHEvaluation.Rank.MotherGroup;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace SHEvaluation.Rank.RegularRank
{
    public static class RegularRankCandidateSqlCheck
    {
        public static int Main(string[] args)
        {
            try
            {
                RegularRankCalculationContext context = RegularRankCalculationContext.CreateDefault("114", "2");
                context.SetTargetExam(RegularRankExamResolution.Resolve("7", "", new[] { new RegularRankExamChoice { Id = "7", Name = "第一次" } }));
                context.SetGradeYears(new[] { 1, 2 });
                context.SetCurrentSource();
                context.MarkMotherGroupLoaded();
                context.ApplySubjectCandidates(new[] { "國文" }, new[] { "國文" }, new[] { "國文" }, null, null, null);
                context.ConfirmSubjects();
                MotherGroupConfirmList list = new MotherGroupConfirmList();
                list.Load(new[] { Row("41", "1年級", RankKinds.Year), Row("42", "1年級", RankKinds.Class), Row("43", "2年級", RankKinds.Class), Row("44", "無法解析", RankKinds.Year) });
                string sql = RegularRankCandidateSql.Build(context.CreateRequest(), list);
                Require(Count(sql, "WITH row AS") == 3, "參與種類與年級各自隔離計算");
                Require(Count(sql, "SELECT 41::BIGINT AS student_id") == 1 && Count(sql, "SELECT 42::BIGINT AS student_id") == 1
                    && Count(sql, "SELECT 43::BIGINT AS student_id") == 1 && !sql.Contains("SELECT 44::BIGINT"), "未參與及不可解析年級不得進入其他視窗");
                Require(Count(sql, "candidate WHERE rank_type = '年排名'") == 1 && Count(sql, "candidate WHERE rank_type = '班排名'") == 2, "只取對應種類的統計及排名");
                Require(!sql.Contains("INSERT INTO") && !sql.Contains("UPDATE"), "候選試算不得寫入");
                string publication = RegularRankAtomicPublishSql.Build(context.CreateRequest(), list, "SR20261105120000abcd", true);
                Require(publication.StartsWith("WITH regular_lock AS", StringComparison.Ordinal) && !publication.Contains("BEGIN TRANSACTION")
                    && !publication.Contains("ROLLBACK") && !publication.Contains("COMMIT;")
                    && publication.EndsWith("WHERE result_guard.ok=1", StringComparison.Ordinal), "發布為單一 WITH 查詢並回傳結果");
                Require(publication.Contains("m.item_type LIKE '定期評量/%'") && publication.Contains("matrix_dept"), "失效僅限定期且學生保存科母群");
                Require(publication.Contains("row.rank_grade_year::INT = score_list.rank_grade_year") && publication.Contains("SELECT count(*) FROM insert_detail_data) < 0"), "矩陣接合隔離年級，失敗注入依賴明細寫入");
                Require(publication.Contains("pg_try_advisory_xact_lock") && publication.Contains("pg_advisory_xact_lock")
                    && publication.Contains("count(*) FROM update_data) = (SELECT count(*) FROM previous_matrices)"),
                    "發布保留目標鎖、操作鎖及舊矩陣更新完整性檢查");
                string root = args != null && args.Length > 0 ? args[0] : Environment.CurrentDirectory;
                string original = File.ReadAllText(Path.Combine(root, "CalculateRegularAssessmentRank.cs"));
                string extracted = File.ReadAllText(Path.Combine(root, "RegularRank/RegularRankFormulaSql.cs"));
                List<string> protectedOriginal = Protected(original), protectedExtracted = Protected(extracted);
                Require(protectedOriginal.Count == protectedExtracted.Count && protectedOriginal.Count > 100, "保護公式數量一致");
                for (int i = 0; i < protectedOriginal.Count; i++) Require(protectedOriginal[i] == protectedExtracted[i], "排名與統計公式逐行一致：" + i);
                foreach (ConfirmStudentRow row in list.Rows) foreach (RankSlot slot in row.Group.Working) slot.Participate = false;
                bool rejected = false;
                try { RegularRankCandidateSql.Build(context.CreateRequest(), list); }
                catch (InvalidOperationException) { rejected = true; }
                Require(rejected, "全不參與時不產生可執行 SQL");
                Console.WriteLine("regular-rank-candidate-sql-check: ok; database-executed: false");
                return 0;
            }
            catch (Exception ex) { Console.WriteLine(ex); return 1; }
        }
        private static ConfirmStudentRow Row(string id, string year, string participating)
        {
            StudentMotherGroup group = new StudentMotherGroup { StudentId = id, StudentName = "測'試" };
            List<RankSlot> slots = new List<RankSlot>();
            foreach (string kind in RankKinds.All) slots.Add(new RankSlot { RankType = kind, GroupName = kind == RankKinds.Year ? year : "同名班", Participate = kind == participating });
            group.SetSourceSlots(slots);
            return new ConfirmStudentRow { Group = group };
        }
        private static int Count(string text, string part) { return (text.Length - text.Replace(part, "").Length) / part.Length; }
        private static List<string> Protected(string source)
        {
            List<string> lines = new List<string>();
            foreach (string line in source.Split('\n'))
                if (Regex.IsMatch(line, @"RANK\(\) OVER|ROW_NUMBER\(\) OVER|COUNT \(student_id\) OVER|FLOOR\(|AVG\(|STDDEV_POP\(|COUNT\(\*\) FILTER|THEN AVG\(|ELSE AVG\("))
                    lines.Add(line.Trim());
            return lines;
        }
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
