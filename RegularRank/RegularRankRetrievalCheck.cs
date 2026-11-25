using SHEvaluation.Rank.DAO;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;

namespace SHEvaluation.Rank.RegularRank
{
    public static class RegularRankRetrievalCheck
    {
        public static int Main(string[] args)
        {
            List<string> errors = new List<string>();
            try
            {
                errors.AddRange(CheckQueryContracts());
                errors.AddRange(CheckSelectSources());
                errors.AddRange(CheckExistingLookup());
                errors.AddRange(CheckFilterIsolation());
            }
            catch (Exception ex)
            {
                errors.Add(ex.ToString());
            }
            if (errors.Count == 0)
            {
                Console.WriteLine("RegularRankRetrievalCheck: ok");
                return 0;
            }
            Console.WriteLine("RegularRankRetrievalCheck: FAIL");
            foreach (string error in errors) Console.WriteLine(" - " + error);
            return 1;
        }

        private static List<string> CheckQueryContracts()
        {
            List<string> errors = new List<string>();
            string options = RegularAssessmentRankQuery.AliveFilterOptionsSql();
            Expect(errors, options.Contains("定期評量") && options.Contains("is_alive = true") && !options.Contains("學期成績"), "選項只含有效定期評量");
            string detail = RegularAssessmentRankQuery.DetailSql("114", "2", "成績", "3", "年排名");
            Expect(errors, detail.Contains("item_type LIKE '定期評量/%'") && detail.Contains("school_year = 114") && detail.Contains("semester = 2")
                && detail.Contains("ref_exam_id") && detail.Contains("ref_batch_id") && !detail.Contains("學期/%"), "明細查詢含期間、exam、batch，且只限定期");
            string matrix = RegularAssessmentRankQuery.MatrixBatchJoinSql(99, 15);
            Expect(errors, matrix.Contains("ref_exam_id") && matrix.Contains("ref_batch_id") && matrix.Contains("定期評量/%")
                && matrix.Contains("source.id = 99") && matrix.Contains("ref_student_id = 15"), "矩陣接合含 exam／batch／定期限定");
            bool threw = false;
            try { RegularAssessmentRankQuery.DetailSql("x", "2", "成績", "3", "年排名"); }
            catch (ArgumentException) { threw = true; }
            Expect(errors, threw, "非法期間應拒絕");
            return errors;
        }

        private static List<string> CheckSelectSources()
        {
            List<string> errors = new List<string>();
            string root = FindRoot();
            string select = File.ReadAllText(Path.Combine(root, "RegularAssessmentRankSelect.cs"), Encoding.UTF8);
            Expect(errors, select.Contains("RegularAssessmentRankQuery.AliveFilterOptionsSql")
                && select.Contains("RegularAssessmentRankQuery.DetailSql")
                && select.Contains("RegularAssessmentRankQuery.ScoreType")
                && !select.Contains("學期成績"), "RegularAssessmentRankSelect 經定期轉接查詢，不混學期");
            Expect(errors, RegularAssessmentRankQuery.DetailSql("114", "2", "成績", "1", "年排名").Contains("exam.id=rank_matrix.ref_exam_id"), "結果檢索接合 exam ID");
            string matrix = File.ReadAllText(Path.Combine(root, "RegularMatrixRankSelect.cs"), Encoding.UTF8);
            Expect(errors, matrix.Contains("ref_batch_id") && matrix.Contains("ref_exam_id") && matrix.Contains("定期評量/%") && matrix.Contains("btnExportToExcel_Click"), "矩陣頁含 batch／exam／定期限定與匯出");
            Expect(errors, matrix.Contains("catch (Exception") || matrix.Contains("catch(Exception"), "矩陣查詢錯誤可恢復");
            Expect(errors, matrix.Contains("找不到對應的定期評量矩陣資料"), "空矩陣選取可恢復提示");
            return errors;
        }

        private static List<string> CheckExistingLookup()
        {
            List<string> errors = new List<string>();
            string sql = RegularRankExistingRankLookup.AliveMatricesSql("114", "2", "5", new int[] { 1, 3 });
            Expect(errors, sql.Contains("item_type LIKE '定期評量/%'") && sql.Contains("ref_exam_id") && sql.Contains("grade_year IN (1,3)")
                && !sql.Contains("學期"), "既有有效排名檢索限定期、exam、年級");
            Expect(errors, RegularRankExistingRankLookup.Describe().Contains("不混入學期成績"), "說明不混學期");
            return errors;
        }

        private static List<string> CheckFilterIsolation()
        {
            List<string> errors = new List<string>();
            List<RankDataInfo> source = new List<RankDataInfo>
            {
                new RankDataInfo { exam_name = "期末考", item_name = "國文", rank_type = "年排名", student_number = "001" },
                new RankDataInfo { exam_name = "期末考", item_name = "英文", rank_type = "班排名", student_number = "002" },
                new RankDataInfo { exam_name = "期中考", item_name = "國文", rank_type = "年排名", student_number = "0012" }
            };
            List<RankDataInfo> byExamItem = RegularAssessmentRankQuery.Filter(source, "期末考", "國文", "年排名", "");
            Expect(errors, byExamItem.Count == 1 && byExamItem[0].student_number == "001", "考試／科目／種類篩選");
            List<RankDataInfo> byNumber = RegularAssessmentRankQuery.Filter(source, "全部", "全部", "全部", "001");
            Expect(errors, byNumber.Count == 2 && byNumber[0].student_number == "001" && byNumber[1].student_number == "0012", "篩選保留前導零字串比對");
            Expect(errors, RegularAssessmentRankQuery.Filter(source, "全部", "全部", "全部", "").Count == 3, "全部不縮小");
            Expect(errors, RegularAssessmentRankQuery.Filter(source, "不存在", "全部", "全部", "").Count == 0, "空結果不例外");
            return errors;
        }

        private static string FindRoot()
        {
            string dir = AppDomain.CurrentDomain.BaseDirectory;
            for (int i = 0; i < 8; i++)
            {
                if (File.Exists(Path.Combine(dir, "SHEvaluation.Rank.csproj"))) return dir;
                dir = Path.GetFullPath(Path.Combine(dir, ".."));
            }
            return Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\"));
        }

        private static void Expect(List<string> errors, bool condition, string message)
        {
            if (!condition) errors.Add(message);
        }
    }
}
