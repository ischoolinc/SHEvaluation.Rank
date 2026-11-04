using FISCA.DSAClient;
using FISCA.DSAClient.HttpUtil;
using SHEvaluation.Rank.DAO;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Windows.Forms;

namespace SHEvaluation.Rank.MotherGroup
{
    public static class SemesterRankRetrievalCheck
    {
        [STAThread]
        public static int Main(string[] args)
        {
            try
            {
                List<string> errors = new List<string>();
                errors.AddRange(CheckQuerySql());
                errors.AddRange(CheckFilterAndExportScope());
                errors.AddRange(CheckFailureReport());
                errors.AddRange(CheckPrivacyRedaction());
                errors.AddRange(CheckDsaClientException());
                errors.AddRange(CheckSortInteraction());
                errors.AddRange(CheckScreenSource());
                errors.AddRange(CheckPublishBoundary());
                errors.AddRange(CheckPublishTransport());
                errors.AddRange(CheckGridTiming());
                if (errors.Count == 0)
                {
                    Console.WriteLine("semester-rank-retrieval-check: ok");
                    Console.WriteLine("database-executed: false");
                    Console.WriteLine("ischool-screen-executed: false");
                    Console.WriteLine("index-changed: false");
                    Console.WriteLine("formula-changed: false");
                    return 0;
                }
                foreach (string error in errors)
                    Console.WriteLine(error);
                return 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return 1;
            }
        }

        private static List<string> CheckQuerySql()
        {
            List<string> errors = new List<string>();
            Expect(errors, SemesterAssessmentRankQuery.OptionSql.IndexOf("SELECT DISTINCT") >= 0 && SemesterAssessmentRankQuery.OptionSql.IndexOf("ORDER BY") >= 0, "下拉選項應在資料庫先去重");
            string detail = SemesterAssessmentRankQuery.DetailSql("114", "2", "學期", "分項成績", new string[] { "1", "3" }, new string[] { "年排名", "班排名" });
            int whereAt = detail.IndexOf("WHERE rank_matrix.is_alive = true");
            Expect(errors, whereAt >= 0 && detail.IndexOf("school_year = 114") > whereAt && detail.IndexOf("semester = 2") > whereAt && detail.IndexOf("item_type LIKE '學期%'") > whereAt && detail.IndexOf("grade_year IN (1, 3)") > whereAt && detail.IndexOf("rank_type IN ('年排名', '班排名')") > whereAt, "期間、成績類型、年級與排名種類應放在同一層 WHERE");
            Expect(errors, detail.IndexOf("SELECT *") < 0 && detail.IndexOf(';') < 0 && detail.IndexOf("UNION ALL") < 0, "明細應是單一句，不再依年級與排名種類分次查詢");
            int orderAt = detail.IndexOf("ORDER BY");
            Expect(errors, orderAt > whereAt, "排序應在篩選之後");
            bool rejected = false;
            try
            {
                SemesterAssessmentRankQuery.DetailSql("114-2", "2", "學期", "分項成績", new string[] { "1" }, new string[] { "年排名" });
            }
            catch (ArgumentException)
            {
                rejected = true;
            }
            Expect(errors, rejected, "不是整數的期間不應送出查詢");
            Expect(errors, SemesterAssessmentRankQuery.DetailSql("114", "2", "學期", "分項成績", new string[0], new string[] { "年排名" }).Length == 0, "沒有年級時不查全部資料");
            string quoted = SemesterAssessmentRankQuery.DetailSql("114", "2", "學'期", "分項成績", new string[] { "3" }, new string[] { "年排名" });
            Expect(errors, quoted.IndexOf("學''期") >= 0 && quoted.IndexOf("學'期") < 0, "成績類型的單引號應跳脫");
            return errors;
        }

        private static List<string> CheckFilterAndExportScope()
        {
            List<string> errors = new List<string>();
            List<RankDataInfo> rows = new List<RankDataInfo>();
            for (int i = 0; i < 250; i++)
            {
                RankDataInfo row = new RankDataInfo();
                row.rank_matrix_id = (i + 1).ToString();
                row.item_name = i % 2 == 0 ? "學業" : "體育";
                row.rank_type = i % 5 == 0 ? "年排名" : "班排名";
                row.student_number = "210" + i.ToString("000");
                row.rank = (i + 1).ToString();
                row.score = "80.5";
                rows.Add(row);
            }
            List<RankDataInfo> all = SemesterAssessmentRankQuery.Filter(rows, "全部", "全部", "");
            List<RankDataInfo> year = SemesterAssessmentRankQuery.Filter(rows, "學業", "年排名", "210");
            Expect(errors, all.Count == 250, "匯出與檢視的篩選結果應包含全部符合列，不能只留一頁");
            Expect(errors, year.Count > 0 && year.Count < 250, "項目、排名種類與學號篩選仍有效");
            Expect(errors, (int?)SemesterAssessmentRankQuery.CellValue(rows[0], 11) == 1 && (decimal?)SemesterAssessmentRankQuery.CellValue(rows[0], 10) == 80.5m, "名次與成績維持可排序的數值");
            return errors;
        }

        private static List<string> CheckFailureReport()
        {
            List<string> errors = new List<string>();
            SemesterRankCalculationTrace trace = SemesterRankCalculationTrace.Start("114", "2", "Excel 母群清單", "");
            trace.ReadStudents(Students());
            trace.SqlBytes = 1200;
            trace.Mark("資料庫查詢失敗");
            string sql = "WITH student_list AS (SELECT '王小明' AS student_name, 99.9 AS score)";
            Exception wrapped = new InvalidOperationException("執行資料庫查詢時發生錯誤 " + sql, new InvalidOperationException("invalid input syntax for type bigint: \"排名明細筆數與成績筆數不一致 detail_count=1 score_count=2\""));
            SemesterRankFailureReport integrity = SemesterRankFailureReport.Create(trace, wrapped, true);
            Expect(errors, integrity.EvidenceKind == "integrity" && integrity.UserMessage.IndexOf("計算排名失敗") == 0 && integrity.UserMessage.IndexOf("計算完成") < 0 && integrity.UserMessage.IndexOf(trace.Id) >= 0, "完整性失敗要給識別碼，且不顯示計算完成");
            Expect(errors, integrity.UserMessage.IndexOf("人數上限") < 0 && integrity.LogText.IndexOf("王小明") < 0 && integrity.LogText.IndexOf("99.9") < 0 && integrity.LogText.IndexOf("WITH student_list") < 0, "紀錄不收學生姓名、成績或完整語句，也不先歸類為人數上限");
            Expect(errors, integrity.LogText.IndexOf("114-2") >= 0 && integrity.LogText.IndexOf("學生人數=3") >= 0 && integrity.LogText.IndexOf("1=2,3=1") >= 0 && integrity.LogText.IndexOf("資料庫查詢失敗") >= 0, "紀錄要有期間、來源人數、年級人數、階段與耗時");
            string directory = Path.Combine(Path.GetTempPath(), "semester-rank-check-" + trace.Id);
            Expect(errors, integrity.WriteFile(directory) && File.Exists(Path.Combine(directory, trace.Id + ".log")), "詳細診斷應寫入紀錄檔");
            try
            {
                Directory.Delete(directory, true);
            }
            catch (Exception)
            {
            }

            SemesterRankFailureReport timeout = SemesterRankFailureReport.Create(trace, new InvalidOperationException("SQLSTATE 57014 canceling statement due to statement timeout"), true);
            Expect(errors, timeout.EvidenceKind == "timeout" && timeout.UserMessage.IndexOf("逾時") >= 0 && timeout.UserMessage.IndexOf("不能當成人數上限") >= 0, "只有資料庫明確取消或逾時才標成逾時，且不能當成人數上限");
            SemesterRankFailureReport constraint = SemesterRankFailureReport.Create(trace, new InvalidOperationException("SQLSTATE 23505 duplicate key value violates unique constraint"), true);
            Expect(errors, constraint.EvidenceKind == "constraint" && constraint.UserMessage.IndexOf("限制衝突") >= 0, "限制衝突要單獨標出代碼");
            SemesterRankFailureReport format = SemesterRankFailureReport.Create(trace, new InvalidOperationException("SQLSTATE 22P02 invalid input syntax for type numeric"), true);
            Expect(errors, format.EvidenceKind == "format" && format.UserMessage.IndexOf("資料格式") >= 0, "沒有完整性文字的格式錯誤不要當成公布檢查");
            SemesterRankFailureReport unknown = SemesterRankFailureReport.Create(trace, new InvalidOperationException("執行資料庫查詢時發生錯誤"), true);
            Expect(errors, unknown.EvidenceKind == "unknown" && unknown.UserMessage.IndexOf("不足以分辨") >= 0 && unknown.LogText.IndexOf("不能把該次結果標成已回復") >= 0, "沒有代碼時保留未知，並註明回滾尚未在資料庫驗證");
            return errors;
        }

        private static List<string> CheckPrivacyRedaction()
        {
            List<string> errors = new List<string>();
            string reproduced = "ERROR: null value violates not-null constraint DETAIL: Failing row contains (測試姓名, 99.9).";
            string reproducedSafe = SemesterRankFailureReport.Sanitize(reproduced);
            ExpectClean(errors, reproducedSafe, "資料列內容");
            Expect(errors, reproduced.Length < 800 && reproducedSafe.IndexOf("not-null constraint") >= 0 && reproducedSafe.IndexOf("［資料列內容已省略］") >= 0, "短訊息要靠內容遮蔽保留限制名稱，不能只靠截斷");

            SemesterRankCalculationTrace trace = SemesterRankCalculationTrace.Start("114", "2", "Excel 母群清單", "");
            trace.Mark("資料庫查詢失敗");
            SemesterRankFailureReport constraint = SemesterRankFailureReport.Create(trace, new InvalidOperationException(
                "ERROR: null value in column \"score\" violates not-null constraint \"rank_detail_score_nn\" DETAIL: Failing row contains (測試姓名, 110358, 99.9). DETAIL: Key (student_number)=(110358) already exists."), true);
            Expect(errors, constraint.EvidenceKind == "constraint" && constraint.UserMessage.IndexOf("限制衝突") >= 0, "not-null 限制仍歸類為限制衝突");
            ExpectClean(errors, constraint.LogText, "限制衝突紀錄");
            ExpectClean(errors, constraint.UserMessage, "限制衝突畫面");
            Expect(errors, constraint.LogText.IndexOf("rank_detail_score_nn") >= 0 && constraint.LogText.IndexOf("column \"score\"") >= 0 && constraint.LogText.IndexOf("student_number") >= 0 && constraint.LogText.IndexOf("InvalidOperationException") >= 0 && constraint.LogText.IndexOf("資料庫查詢失敗") >= 0, "限制名稱、欄位名稱、例外類型與階段要留下");

            SemesterRankFailureReport format = SemesterRankFailureReport.Create(trace, new InvalidOperationException(
                "SQLSTATE 22P02 invalid input syntax for type numeric: \"測試姓名\"; invalid input syntax for type numeric: \"110358\"; invalid input syntax for type numeric: 99.9; $1 = '測試姓名'; $2='110358'; $3=99.9"), true);
            Expect(errors, format.EvidenceKind == "format" && format.LogText.IndexOf("22P02") >= 0 && format.UserMessage.IndexOf("資料格式") >= 0, "引號值遮蔽後格式錯誤分類與代碼仍在");
            ExpectClean(errors, format.LogText, "格式轉換紀錄");
            ExpectClean(errors, format.UserMessage, "格式轉換畫面");

            Exception inner = new InvalidOperationException("SQLSTATE 22P02 invalid input syntax for type numeric: \"99.9\"");
            Exception middle = new InvalidOperationException("DETAIL: Failing row contains (測試姓名, 110358, 99.9).", inner);
            Exception outer = new InvalidOperationException("執行資料庫查詢時發生錯誤 WITH student_list AS (SELECT '測試姓名' AS student_name, '110358' AS student_number, 99.9 AS score)", middle);
            SemesterRankFailureReport nested = SemesterRankFailureReport.Create(trace, outer, true);
            Expect(errors, nested.EvidenceKind == "format" && nested.LogText.IndexOf("22P02") >= 0 && nested.LogText.IndexOf("［語句已省略］") >= 0 && nested.LogText.IndexOf("［資料列內容已省略］") >= 0, "多層例外仍保留格式代碼，並省略語句與資料列");
            ExpectClean(errors, nested.LogText, "多層例外紀錄");
            ExpectClean(errors, nested.UserMessage, "多層例外畫面");
            string directory = Path.Combine(Path.GetTempPath(), "semester-rank-privacy-" + nested.Id);
            Expect(errors, nested.WriteFile(directory), "遮蔽後的診斷要寫入本機紀錄");
            string fileText = File.ReadAllText(Path.Combine(directory, nested.Id + ".log"), Encoding.UTF8);
            Expect(errors, fileText == nested.LogText, "本機紀錄與準備送出的診斷文字相同");
            ExpectClean(errors, fileText, "本機紀錄檔");
            try
            {
                Directory.Delete(directory, true);
            }
            catch (Exception)
            {
            }
            string parenRow = "ERROR: null value violates not-null constraint DETAIL: Failing row contains (類別A), 測試姓名, 99.9).";
            string parenSafe = SemesterRankFailureReport.Sanitize(parenRow);
            Expect(errors, parenSafe == "ERROR: null value violates not-null constraint DETAIL: ［資料列內容已省略］", "資料值內的右括號不能讓資料列提早結束");
            ExpectClean(errors, parenSafe, "資料列內括號");
            Expect(errors, parenSafe.IndexOf("類別A") < 0, "資料列提早結束時不能留下括號後的內容");

            string keyword = "SQLSTATE 22P02 invalid input syntax for type numeric: \"測試姓名明細筆數99.9\"";
            SemesterRankFailureReport keywordReport = SemesterRankFailureReport.Create(trace, new InvalidOperationException(keyword), true);
            Expect(errors, keywordReport.EvidenceKind == "format" && keywordReport.LogText.IndexOf("22P02") >= 0, "引號裡的明細筆數不能改寫格式錯誤分類");
            Expect(errors, keywordReport.LogText.IndexOf("明細筆數") < 0 && keywordReport.LogText.IndexOf("［值已省略］") >= 0, "診斷關鍵字不能放行整段引號");
            ExpectClean(errors, keywordReport.LogText, "診斷關鍵字紀錄");
            ExpectClean(errors, keywordReport.UserMessage, "診斷關鍵字畫面");
            string keywordDir = Path.Combine(Path.GetTempPath(), "semester-rank-privacy-" + keywordReport.Id);
            Expect(errors, keywordReport.WriteFile(keywordDir), "關鍵字案例的遮蔽診斷要寫入本機紀錄");
            string keywordFile = File.ReadAllText(Path.Combine(keywordDir, keywordReport.Id + ".log"), Encoding.UTF8);
            Expect(errors, keywordFile == keywordReport.LogText, "關鍵字案例的本機紀錄與準備送出的診斷文字相同");
            ExpectClean(errors, keywordFile, "關鍵字案例本機紀錄檔");
            try
            {
                Directory.Delete(keywordDir, true);
            }
            catch (Exception)
            {
            }

            string counted = "invalid input syntax for type bigint: \"測試姓名明細筆數99.9 detail_count=1 score_count=2 missing_matrix_count=3 missing_student_count=4 student_conflict_count=5\"";
            SemesterRankFailureReport countedReport = SemesterRankFailureReport.Create(trace, new InvalidOperationException(counted), true);
            Expect(errors, countedReport.EvidenceKind == "integrity", "允許的診斷欄位仍可判斷完整性");
            Expect(errors, countedReport.LogText.IndexOf("detail_count=1") >= 0 && countedReport.LogText.IndexOf("score_count=2") >= 0 && countedReport.LogText.IndexOf("missing_matrix_count=3") >= 0 && countedReport.LogText.IndexOf("missing_student_count=4") >= 0 && countedReport.LogText.IndexOf("student_conflict_count=5") >= 0 && countedReport.LogText.IndexOf("明細筆數") < 0, "只留下允許的計數欄位與整數");
            ExpectClean(errors, countedReport.LogText, "診斷欄位抽取紀錄");
            ExpectClean(errors, countedReport.UserMessage, "診斷欄位抽取畫面");
            string decimalScore = SemesterRankFailureReport.Sanitize("invalid input syntax for type numeric: \"測試姓名 detail_count=99.9\"");
            Expect(errors, decimalScore.IndexOf("detail_count=") < 0, "允許欄位後面的小數不當成診斷計數");
            ExpectClean(errors, decimalScore, "診斷欄位小數");
            string emptyScore = SemesterRankFailureReport.Sanitize("invalid input syntax for type bigint: \"沒有符合排名條件的學期成績，未執行排名公布。母群學生=5，科目成績=0，測試姓名，99.9\"");
            Expect(errors, emptyScore.IndexOf("沒有符合排名條件的學期成績") >= 0 && emptyScore.IndexOf("母群學生=5") >= 0 && emptyScore.IndexOf("科目成績=0") >= 0 && SemesterRankFailureReport.Classify("", emptyScore) == "integrity", "沒有成績的固定診斷與計數仍可分類");
            ExpectClean(errors, emptyScore, "沒有成績診斷");
            string bareIntegrity = SemesterRankFailureReport.Sanitize("invalid input syntax for type bigint: \"完整性檢查未通過，測試姓名，99.9\"");
            Expect(errors, bareIntegrity.IndexOf("完整性檢查未通過") >= 0 && SemesterRankFailureReport.Classify("", bareIntegrity) == "integrity", "完整性句子仍可分類");
            ExpectClean(errors, bareIntegrity, "完整性句子");

            string keyParen = "ERROR: duplicate key value violates unique constraint \"rank_detail_uk\" DETAIL: Key (student_number)=(類別A), 測試姓名, 99.9) already exists.";
            string keySafe = SemesterRankFailureReport.Sanitize(keyParen);
            Expect(errors, keySafe == "ERROR: duplicate key value violates unique constraint \"rank_detail_uk\" DETAIL: Key (student_number)=(［值已省略］)", "Key 值內的右括號不能留下後面的內容");
            ExpectClean(errors, keySafe, "Key 值內括號");
            Expect(errors, keySafe.IndexOf("類別A") < 0 && keySafe.IndexOf("student_number") >= 0, "Key 欄名保留，括號後的值要省略");
            string keyNested = SemesterRankFailureReport.Sanitize("DETAIL: Key (category)=((類別A), 測試姓名) already exists.");
            Expect(errors, keyNested == "DETAIL: Key (category)=(［值已省略］) already exists.", "成對括號的 Key 值要整段省略");
            ExpectClean(errors, keyNested, "Key 成對括號");
            Expect(errors, keyNested.IndexOf("類別A") < 0, "Key 成對括號不能留下值");
            SemesterRankFailureReport keyReport = SemesterRankFailureReport.Create(trace, new InvalidOperationException(keyParen), true);
            Expect(errors, keyReport.EvidenceKind == "constraint", "Key 值遮蔽後仍歸類為限制衝突");
            ExpectClean(errors, keyReport.LogText, "Key 括號紀錄");
            ExpectClean(errors, keyReport.UserMessage, "Key 括號畫面");
            string keyDir = Path.Combine(Path.GetTempPath(), "semester-rank-privacy-" + keyReport.Id);
            Expect(errors, keyReport.WriteFile(keyDir), "Key 括號案例的遮蔽診斷要寫入本機紀錄");
            string keyFile = File.ReadAllText(Path.Combine(keyDir, keyReport.Id + ".log"), Encoding.UTF8);
            Expect(errors, keyFile == keyReport.LogText, "Key 括號案例的本機紀錄與準備送出的診斷文字相同");
            ExpectClean(errors, keyFile, "Key 括號案例本機紀錄檔");
            try
            {
                Directory.Delete(keyDir, true);
            }
            catch (Exception)
            {
            }

            string report = File.ReadAllText("MotherGroup\\SemesterRankFailureReport.cs", Encoding.UTF8);
            int assign = report.IndexOf("string text = LogText");
            int log = report.IndexOf("ApplicationLog.Log");
            int save = report.IndexOf("public void Save()");
            int saveEnd = report.IndexOf("public static string DefaultDirectory");
            string saveBody = save >= 0 && saveEnd > save ? report.Substring(save, saveEnd - save) : "";
            Expect(errors, assign > 0 && log > assign && saveBody.IndexOf("string text = LogText") >= 0 && report.IndexOf("text ?? \"\"") > log && report.IndexOf("LogText ?? \"\"") > 0, "ApplicationLog 與本機檔案都使用同一份已遮蔽文字");
            Expect(errors, saveBody.IndexOf("WriteFile") >= 0 && saveBody.IndexOf("error.Message") < 0 && saveBody.IndexOf("DetailMessage") < 0, "本機紀錄與 ApplicationLog 都只送出已組好的 LogText");
            return errors;
        }

        private static List<string> CheckDsaClientException()
        {
            List<string> errors = new List<string>();
            SemesterRankCalculationTrace trace = SemesterRankCalculationTrace.Start("114", "2", "依目前學生資料產生", "");
            trace.ReadStudents(Students());
            trace.Mark("資料庫查詢失敗");
            string request = "<Request><Password>secret-password-value</Password><StudentName>REQUESTONLY姓名</StudentName><SQL>SELECT name, score FROM student WHERE name = '測試姓名'</SQL></Request>";
            string timeoutResponse = "<Envelope><Header><Status><Code>ServiceExecutionError</Code><Message>server</Message></Status><SecurityToken>session-token-value</SecurityToken></Header><Body><Fault><Code>57014</Code><Message>ERROR: canceling statement due to statement timeout</Message><Detail>SQLSTATE: 57014 DETAIL: Failing row contains (測試姓名, 99.9). SELECT student.name, student.score FROM student WHERE name = '測試姓名'</Detail></Fault><StudentNumber>110358</StudentNumber></Body></Envelope>";
            FISCA.DSAClient.DSAServerException timeoutClient = new FISCA.DSAClient.DSAServerException("ServiceExecutionError", "執行資料庫查詢時發生錯誤", request, timeoutResponse, null);
            FISCA.DSAUtil.DSAServerException timeoutUtil = new FISCA.DSAUtil.DSAServerException(timeoutClient);
            SemesterRankFailureReport clientReport = SemesterRankFailureReport.Create(trace, timeoutClient, true);
            SemesterRankFailureReport utilReport = SemesterRankFailureReport.Create(trace, timeoutUtil, true);
            Expect(errors, clientReport.EvidenceKind == "timeout" && utilReport.EvidenceKind == "timeout", "DSAClient 與 DSAUtil 例外都要依 SQLSTATE 57014 分成逾時");
            Expect(errors, clientReport.LogText.IndexOf("資料庫代碼=57014") >= 0 && clientReport.LogText.IndexOf("DSA狀態=ServiceExecutionError") >= 0 && clientReport.LogText.IndexOf("dsaStatus=ServiceExecutionError") >= 0 && clientReport.LogText.IndexOf("資料庫代碼=ServiceExecutionError") < 0, "DSA 狀態與 SQLSTATE 要分開記錄");
            Expect(errors, utilReport.LogText.IndexOf("資料庫代碼=57014") >= 0 && utilReport.LogText.IndexOf("DSA狀態=ServiceExecutionError") >= 0 && utilReport.LogText.IndexOf("FISCA.DSAUtil.DSAServerException") >= 0, "包在 DSAUtil 裡時仍要讀到真正的 DSAClient 狀態");
            ExpectDsaHidden(errors, clientReport.LogText, "DSAClient 逾時紀錄");
            ExpectDsaHidden(errors, clientReport.UserMessage, "DSAClient 逾時畫面");
            ExpectDsaHidden(errors, utilReport.LogText, "DSAUtil 逾時紀錄");
            Expect(errors, clientReport.LogText.IndexOf("statement timeout") >= 0 && clientReport.LogText.IndexOf("［資料列內容已省略］") >= 0, "逾時原因留下，資料列與 SQL 要省略");

            string opaque = "<Envelope><Header><Status><Code>ServiceExecutionError</Code></Status></Header><Body><Student><Name>測試姓名</Name><Score>99.9</Score><StudentNumber>110358</StudentNumber></Student></Body></Envelope>";
            FISCA.DSAClient.DSAServerException opaqueClient = new FISCA.DSAClient.DSAServerException("ServiceExecutionError", "執行資料庫查詢時發生錯誤", request, opaque, null);
            SemesterRankFailureReport opaqueReport = SemesterRankFailureReport.Create(trace, opaqueClient, true);
            Expect(errors, opaqueReport.EvidenceKind == "unknown" && opaqueReport.LogText.IndexOf("DSA狀態=ServiceExecutionError") >= 0 && opaqueReport.LogText.IndexOf("資料庫代碼=（沒有取得）") >= 0 && opaqueReport.LogText.IndexOf("［回應未含可辨識的資料庫錯誤］") >= 0, "沒有底層原因時保留 DSA 狀態，不把狀態碼當成 SQLSTATE");
            ExpectDsaHidden(errors, opaqueReport.LogText, "沒有底層原因的紀錄");
            ExpectDsaHidden(errors, opaqueReport.UserMessage, "沒有底層原因的畫面");

            FISCA.DSAClient.DSAServerException emptyClient = new FISCA.DSAClient.DSAServerException("ServiceExecutionError", "執行資料庫查詢時發生錯誤", request, "", null);
            SemesterRankFailureReport emptyReport = SemesterRankFailureReport.Create(trace, emptyClient, true);
            Expect(errors, emptyReport.LogText.IndexOf("［沒有 Response］") >= 0 && emptyReport.EvidenceKind == "unknown", "空的 Response 要明示沒有讀到內容");
            ExpectDsaHidden(errors, emptyReport.LogText, "空 Response 紀錄");

            string formatResponse = "<Envelope><Body><Fault><Code>22P02</Code><Message>SQLSTATE 22P02 invalid input syntax for type numeric: \"測試姓名明細筆數99.9\"</Message></Fault></Body></Envelope>";
            FISCA.DSAClient.DSAServerException formatClient = new FISCA.DSAClient.DSAServerException("ServiceExecutionError", "執行資料庫查詢時發生錯誤", request, formatResponse, null);
            SemesterRankFailureReport formatReport = SemesterRankFailureReport.Create(trace, formatClient, true);
            Expect(errors, formatReport.EvidenceKind == "format" && formatReport.LogText.IndexOf("資料庫代碼=22P02") >= 0 && formatReport.LogText.IndexOf("明細筆數") < 0, "Response 裡的格式錯誤仍用 SQLSTATE 分類，引號內容不整段放行");
            ExpectDsaHidden(errors, formatReport.LogText, "格式 Response 紀錄");

            string programLimit = "<Envelope><Header><Status><Code>ServiceExecutionError</Code></Status></Header><Body><Fault><Code>54001</Code><Message>ERROR: stack depth limit exceeded</Message><Detail>DETAIL: Failing row contains (測試姓名, 99.9).</Detail></Fault></Body></Envelope>";
            FISCA.DSAClient.DSAServerException programClient = new FISCA.DSAClient.DSAServerException("ServiceExecutionError", "執行資料庫查詢時發生錯誤", request, programLimit, null);
            SemesterRankFailureReport programReport = SemesterRankFailureReport.Create(trace, programClient, true);
            Expect(errors, programReport.LogText.IndexOf("資料庫代碼=54001") >= 0 && programReport.LogText.IndexOf("DSA狀態=ServiceExecutionError") >= 0 && programReport.LogText.IndexOf("資料庫代碼=ServiceExecutionError") < 0 && programReport.LogText.IndexOf("SQLSTATE") < 0, "Fault 的 54001 要直接寫入資料庫代碼，不靠固定清單或 SQLSTATE 字樣");
            Expect(errors, programReport.EvidenceKind == "unknown", "54001 仍沿用現有分類");
            ExpectDsaHidden(errors, programReport.LogText, "54001 紀錄");
            ExpectDsaHidden(errors, programReport.UserMessage, "54001 畫面");

            string notNull = "<Envelope><Header><Status><Code>ServiceExecutionError</Code></Status></Header><Body><Fault><Code>23502</Code><Message>ERROR: null value violates not-null constraint</Message><Detail>DETAIL: Failing row contains (測試姓名, 99.9).</Detail></Fault></Body></Envelope>";
            FISCA.DSAClient.DSAServerException notNullClient = new FISCA.DSAClient.DSAServerException("ServiceExecutionError", "執行資料庫查詢時發生錯誤", request, notNull, null);
            FISCA.DSAUtil.DSAServerException notNullUtil = new FISCA.DSAUtil.DSAServerException(notNullClient);
            SemesterRankFailureReport notNullReport = SemesterRankFailureReport.Create(trace, notNullClient, true);
            SemesterRankFailureReport notNullWrapped = SemesterRankFailureReport.Create(trace, notNullUtil, true);
            Expect(errors, notNullReport.LogText.IndexOf("資料庫代碼=23502") >= 0 && notNullWrapped.LogText.IndexOf("資料庫代碼=23502") >= 0 && notNullReport.LogText.IndexOf("SQLSTATE") < 0 && notNullReport.EvidenceKind == "constraint" && notNullWrapped.EvidenceKind == "constraint", "Fault 的 23502 要直接寫入資料庫代碼，並維持限制衝突分類");
            ExpectDsaHidden(errors, notNullReport.LogText, "23502 紀錄");
            ExpectDsaHidden(errors, notNullWrapped.LogText, "23502 包裝紀錄");

            string integrityResponse = "<Envelope><Body><Fault><Message>invalid input syntax for type bigint: \"測試姓名 detail_count=1 score_count=2\"</Message></Fault></Body></Envelope>";
            FISCA.DSAClient.DSAServerException integrityClient = new FISCA.DSAClient.DSAServerException("ServiceExecutionError", "執行資料庫查詢時發生錯誤", request, integrityResponse, null);
            SemesterRankFailureReport integrityReport = SemesterRankFailureReport.Create(trace, integrityClient, true);
            Expect(errors, integrityReport.EvidenceKind == "integrity" && integrityReport.LogText.IndexOf("detail_count=1") >= 0 && integrityReport.LogText.IndexOf("score_count=2") >= 0, "Response 抽出的計數欄位仍可判斷完整性");
            ExpectDsaHidden(errors, integrityReport.LogText, "完整性 Response 紀錄");

            string report = File.ReadAllText("MotherGroup\\SemesterRankFailureReport.cs", Encoding.UTF8);
            int chain = report.IndexOf("public static string ExceptionChain");
            int chainEnd = report.IndexOf("public static string Sanitize", chain);
            string chainBody = chain >= 0 && chainEnd > chain ? report.Substring(chain, chainEnd - chain) : "";
            Expect(errors, chainBody.IndexOf("ReadSafeServerDetail") >= 0 && chainBody.IndexOf(".Request") < 0 && report.IndexOf("FISCA.DSAClient.DSAServerException") >= 0 && report.IndexOf("ExtractResponse") >= 0, "例外鏈要讀 DSAClient 狀態與回應摘要，不記錄 Request");
            return errors;
        }

        private static void ExpectDsaHidden(List<string> errors, string text, string label)
        {
            ExpectClean(errors, text, label);
            Expect(errors, text != null && text.IndexOf("secret-password-value") < 0 && text.IndexOf("session-token-value") < 0 && text.IndexOf("REQUESTONLY姓名") < 0 && text.IndexOf("<Request") < 0 && text.IndexOf("SELECT name") < 0 && text.IndexOf("<Student>") < 0, label + "不得留下驗證資訊、Request 或完整 Response");
        }

        private static void ExpectClean(List<string> errors, string text, string label)
        {
            Expect(errors, text != null && text.IndexOf("測試姓名") < 0 && text.IndexOf("110358") < 0 && text.IndexOf("99.9") < 0, label + "不得留下姓名、學號或成績");
        }

        private static List<string> CheckSortInteraction()
        {
            List<string> errors = new List<string>();
            using (SemesterAssessmentRankSelect form = new SemesterAssessmentRankSelect())
            {
                form.LoadRowsForCheck(SampleRows());
                Expect(errors, form.SortModeForCheck("學號") == DataGridViewColumnSortMode.Programmatic && form.SortModeForCheck("排名分數") == DataGridViewColumnSortMode.Programmatic && form.SortModeForCheck("座號") == DataGridViewColumnSortMode.Programmatic && form.SortModeForCheck("PR") == DataGridViewColumnSortMode.Programmatic && form.SortModeForCheck("百分比") == DataGridViewColumnSortMode.Programmatic && form.SortModeForCheck("名次") == DataGridViewColumnSortMode.Programmatic && form.SortModeForCheck("檢視") == DataGridViewColumnSortMode.NotSortable, "可排序欄位改為程式排序，檢視按鈕不排序");
                Expect(errors, form.ColumnIndexForCheck("座號") == 7 && form.ColumnIndexForCheck("學號") == 8 && form.ColumnIndexForCheck("排名分數") == 10 && form.ColumnIndexForCheck("名次") == 11 && form.ColumnIndexForCheck("PR") == 12 && form.ColumnIndexForCheck("百分比") == 13, "排序欄位索引要對應座號、學號與成績欄");

                form.ClickHeaderForCheck("排名分數");
                form.ClickHeaderForCheck("排名分數");
                Expect(errors, form.SortGlyphForCheck("排名分數") == "Descending" && form.SortGlyphForCheck("學號") == "None", "再次點同一欄改為降冪，其他欄不顯示箭頭");
                Expect(errors, Identity(form, 0) == "MATRIX-B|STUDENT-B" && Identity(form, 1) == "MATRIX-C|STUDENT-C" && Identity(form, 2) == "MATRIX-A|STUDENT-A" && Identity(form, 3) == "MATRIX-D|STUDENT-D", "成績降冪依數值排列，缺值留在最後");
                Expect(errors, form.ShownStudentNumberForCheck(0) == "02", "排序後畫面學號仍保留前導零");

                form.FilterItemForCheck("學業");
                Expect(errors, form.ViewCountForCheck() == 3 && Identity(form, 0) == "MATRIX-B|STUDENT-B" && form.SortGlyphForCheck("排名分數") == "Descending", "篩選後沿用排序與箭頭，檢視對到篩選後的第一筆");
                ExpectSameOrder(errors, form, "篩選後匯出");

                form.FilterStudentNumberForCheck("10");
                Expect(errors, form.ViewCountForCheck() == 2 && Identity(form, 0) == "MATRIX-A|STUDENT-A" && Identity(form, 1) == "MATRIX-D|STUDENT-D", "學號篩選後仍依目前成績排序");
                ExpectSameOrder(errors, form, "學號篩選後匯出");
                form.FilterStudentNumberForCheck("");
                form.FilterItemForCheck("全部");

                form.ClickHeaderForCheck("座號");
                Expect(errors, form.SortGlyphForCheck("座號") == "Ascending" && form.SortGlyphForCheck("排名分數") == "None" && Identity(form, 0) == "MATRIX-B|STUDENT-B" && Identity(form, 1) == "MATRIX-C|STUDENT-C" && Identity(form, 2) == "MATRIX-A|STUDENT-A", "座號依數值排序，02 與 2 都在 10 前面");
                form.ClickHeaderForCheck("PR");
                Expect(errors, Identity(form, 0) == "MATRIX-B|STUDENT-B" && Identity(form, 2) == "MATRIX-A|STUDENT-A", "PR 依數值排序，不把 10 排在 2 前面");
                form.ClickHeaderForCheck("百分比");
                Expect(errors, Identity(form, 0) == "MATRIX-B|STUDENT-B" && Identity(form, 2) == "MATRIX-A|STUDENT-A", "百分比依數值排序");
                form.ClickHeaderForCheck("名次");
                Expect(errors, Identity(form, 0) == "MATRIX-B|STUDENT-B" && Identity(form, 1) == "MATRIX-C|STUDENT-C" && Identity(form, 2) == "MATRIX-A|STUDENT-A", "名次依數值排序");
                string beforeViewClick = Identity(form, 0);
                form.ClickHeaderForCheck("檢視");
                Expect(errors, Identity(form, 0) == beforeViewClick && form.SortGlyphForCheck("名次") == "Ascending", "點檢視欄不改變排序與箭頭");

                form.ClickHeaderForCheck("學號");
                Expect(errors, form.ShownStudentNumberForCheck(0) == "02" && Identity(form, 0) == "MATRIX-B|STUDENT-B" && Identity(form, 1) == "MATRIX-A|STUDENT-A" && Identity(form, 2) == "MATRIX-D|STUDENT-D" && Identity(form, 3) == "MATRIX-C|STUDENT-C", "學號依文字排序，前導零不當成數值");
                ExpectSameOrder(errors, form, "學號排序後匯出");
                form.ClickHeaderForCheck("學號");
                Expect(errors, form.SortGlyphForCheck("學號") == "Descending" && Identity(form, 0) == "MATRIX-C|STUDENT-C" && form.ShownStudentNumberForCheck(0) == "2", "學號降冪與箭頭一致");

                List<RankDataInfo> reversed = SampleRows();
                reversed.Reverse();
                form.LoadRowsForCheck(reversed);
                Expect(errors, form.SortGlyphForCheck("學號") == "Descending" && form.SortGlyphForCheck("排名分數") == "None" && Identity(form, 0) == "MATRIX-C|STUDENT-C" && Identity(form, 3) == "MATRIX-B|STUDENT-B" && form.ViewCountForCheck() == 4, "重新查詢後沿用排序，舊的載入順序不覆蓋");
                ExpectSameOrder(errors, form, "重新查詢後匯出");
            }
            return errors;
        }

        private static void ExpectSameOrder(List<string> errors, SemesterAssessmentRankSelect form, string label)
        {
            List<string> lines = form.ExportLinesForCheck();
            Expect(errors, lines.Count == form.ViewCountForCheck() && lines.Count > 1, label + "要包含全部篩選結果");
            for (int i = 0; i < lines.Count; i++)
            {
                RankDataInfo row = form.ViewRowForCheck(i);
                string number = row.student_number ?? "";
                Expect(errors, lines[i].IndexOf("\t" + number + "\t") >= 0 && lines[i].IndexOf(row.rank_matrix_id ?? "") < 0, label + "第 " + (i + 1) + " 列要對應畫面那一筆，且不因隱藏欄位改變對象");
            }
        }

        private static string Identity(SemesterAssessmentRankSelect form, int rowIndex)
        {
            RankDataInfo row = form.ViewRowForCheck(rowIndex);
            if (row == null)
                return "";
            return (row.rank_matrix_id ?? "") + "|" + (row.ref_student_id ?? "");
        }

        private static List<RankDataInfo> SampleRows()
        {
            return new List<RankDataInfo>
            {
                Sample("MATRIX-A", "STUDENT-A", "學業", "10", "10", "70", "3", "10", "100"),
                Sample("MATRIX-B", "STUDENT-B", "學業", "02", "2", "90", "1", "2", "20"),
                Sample("MATRIX-C", "STUDENT-C", "體育", "2", "02", "80.5", "2", "8", "50"),
                Sample("MATRIX-D", "STUDENT-D", "學業", "100", "", "", "", "", "")
            };
        }

        private static RankDataInfo Sample(string matrixId, string studentId, string item, string studentNumber, string seat, string score, string rank, string pr, string percentile)
        {
            RankDataInfo row = new RankDataInfo();
            row.rank_matrix_id = matrixId;
            row.ref_student_id = studentId;
            row.item_name = item;
            row.rank_type = "年排名";
            row.student_number = studentNumber;
            row.seat_no = seat;
            row.score = score;
            row.rank = rank;
            row.pr = pr;
            row.percentile = percentile;
            row.student_name = "學生";
            row.school_year = "114";
            row.semester = "2";
            return row;
        }

        private static SemesterRankStudentSqlResult Students()
        {
            SemesterRankStudentSqlResult result = new SemesterRankStudentSqlResult();
            SemesterRankGradeStudents first = new SemesterRankGradeStudents();
            first.GradeYear = 1;
            first.Students = new List<SemesterRankStudentInput>();
            first.Students.Add(new SemesterRankStudentInput());
            first.Students.Add(new SemesterRankStudentInput());
            SemesterRankGradeStudents third = new SemesterRankGradeStudents();
            third.GradeYear = 3;
            third.Students = new List<SemesterRankStudentInput>();
            third.Students.Add(new SemesterRankStudentInput());
            result.Grades.Add(first);
            result.Grades.Add(third);
            return result;
        }

        private static List<string> CheckScreenSource()
        {
            List<string> errors = new List<string>();
            string select = File.ReadAllText("SemesterAssessmentRankSelect.cs", Encoding.UTF8);
            string calculate = File.ReadAllText("CalculateSemesterAssessmentRank.cs", Encoding.UTF8);
            Expect(errors, select.IndexOf("Application.DoEvents") < 0, "檢索畫面不使用 DoEvents");
            Expect(errors, select.IndexOf("throw new Exception(\"資料讀取失敗\"") < 0, "查詢完成事件不再拋出未處理例外");
            Expect(errors, select.IndexOf("SemesterAssessmentRankQuery.DetailSql") >= 0 && select.IndexOf("VirtualMode = true") >= 0, "明細改為一次查詢，並用虛擬模式建立畫面");
            int load = select.IndexOf("void SemesterAssessmentRankSelect_Load");
            int loadEnd = select.IndexOf("void LoadRowData");
            string loadBody = load >= 0 && loadEnd > load ? select.Substring(load, loadEnd - load) : "";
            int selectAt = loadBody.IndexOf("queryHelper.Select");
            int workerAt = loadBody.IndexOf("RunWorkerAsync()");
            Expect(errors, selectAt > loadBody.IndexOf("DoWork") && workerAt > selectAt, "初始化的資料庫查詢應在背景工作，而不是畫面事件同步執行");
            Expect(errors, select.IndexOf("_detailGeneration") >= 0 && select.IndexOf("generation != _detailGeneration") >= 0, "舊查詢完成時不得覆蓋新條件");
            Expect(errors, select.IndexOf("_viewRows.Count") >= 0 && select.IndexOf("for (int rowIndex = 0; rowIndex < _viewRows.Count") >= 0, "匯出走完整篩選結果");
            Expect(errors, select.IndexOf("DataGridViewColumnSortMode.Programmatic") >= 0 && select.IndexOf("ColumnHeaderMouseClick") >= 0 && select.IndexOf("SemesterAssessmentRankQuery.SortRows") >= 0 && select.IndexOf("dgvScoreRank.Sort(") < 0, "虛擬表格由程式排序資料，不呼叫內建排序");
            Expect(errors, calculate.IndexOf("queryHelper.Select(insertRankSql)") >= 0 && Count(calculate, "queryHelper.Select(insertRankSql)") == 1, "公布仍是一次查詢");
            Expect(errors, calculate.IndexOf("BEGIN") < 0 && calculate.IndexOf("COMMIT") < 0, "產品路徑仍不送交易控制指令");
            Expect(errors, calculate.IndexOf("SemesterRankPublishOutcome.PendingTrace = rankTrace") >= 0 && calculate.IndexOf("SemesterRankPublishOutcome.From(bkwException, publishResult)") >= 0, "計算失敗要把階段紀錄送進既有結果，且維持一次查詢的呼叫形狀");
            return errors;
        }

        private static List<string> CheckPublishBoundary()
        {
            List<string> errors = new List<string>();
            string formula = File.ReadAllText("MotherGroup\\SemesterRankFormulaSql.cs", Encoding.UTF8);
            Expect(errors, formula.IndexOf("unnest(xpath('/root/SemesterSubjectScoreInfo/Subject'") >= 0 && formula.IndexOf("std_dev_pop") >= 0 && formula.IndexOf("每百") < 0, "排名公式仍在 SemesterRankFormulaSql，保留原本科目展開、標準差，且沒有依每百人拆開排名");
            string calculateFormula = File.ReadAllText("CalculateSemesterAssessmentRank.cs", Encoding.UTF8);
            Expect(errors, calculateFormula.IndexOf("unnest(xpath('/root/SemesterSubjectScoreInfo/Subject'") < 0, "畫面檔不再內嵌舊的科目公式");
            string publish = File.ReadAllText("MotherGroup\\SemesterRankPublishSql.cs", Encoding.UTF8);
            Expect(errors, publish.IndexOf("detail_count = detail_count + 1") >= 0 && publish.IndexOf("::BIGINT") >= 0 && publish.IndexOf("THEN detail_count") >= 0, "公布完整性失敗仍讓同一句失敗，維持原本回復方式");
            string history = File.ReadAllText("MotherGroup\\CalculationPeriod.cs", Encoding.UTF8);
            Expect(errors, history.IndexOf("HistorySchoolYear") >= 0, "歷史期間欄位仍在，這次沒有改寫歷史來源條件");
            return errors;
        }

        private static List<string> CheckPublishTransport()
        {
            List<string> errors = new List<string>();
            string calculate = File.ReadAllText("CalculateSemesterAssessmentRank.cs", Encoding.UTF8);
            string transport = File.ReadAllText("MotherGroup\\SemesterRankPublishTransport.cs", Encoding.UTF8);
            string diag = File.ReadAllText("MotherGroup\\SemesterRankField687Diag.cs", Encoding.UTF8);
            Expect(errors, calculate.IndexOf("SemesterRankPublishTransport.OpenWriter(rankTrace)") >= 0 && calculate.IndexOf("rankTrace.Id") >= 0 && Count(calculate, "queryHelper.Select(insertRankSql)") == 1, "公布仍只送出一次寫入，並帶這次操作識別");
            Expect(errors, calculate.IndexOf("SemesterRankPublishTransport.Reconcile") >= 0 && calculate.IndexOf("結果未確認") >= 0, "回應中斷時改為核對已公布批次");
            Expect(errors, transport.IndexOf("dedicated.MaxRetryCount = 0") >= 0 && transport.IndexOf("TimeoutMilliseconds") >= 0 && transport.IndexOf("AsContract") >= 0 && transport.IndexOf("ReferenceEquals") >= 0, "公布傳輸要關閉重送，並確認 AsContract 回傳另一個物件");
            Expect(errors, transport.IndexOf("Connection.DefaultHttpManager =") < 0 && transport.IndexOf("shared.HttpManager =") < 0, "不可改全域或登入連線的 HttpManager");
            Expect(errors, diag.IndexOf("class.class_name") >= 0 && diag.IndexOf("class.name") < 0 && diag.IndexOf("tag_student") >= 0 && diag.IndexOf("student_tag") < 0, "診斷要讀 class_name 與 tag_student");

            int defaultRetry = Connection.DefaultHttpManager.MaxRetryCount;
            int defaultTimeout = Connection.DefaultHttpManager.HttpSetup.Timeout;
            HttpManager baseline = new HttpManager();
            baseline.MaxRetryCount = 3;
            baseline.HttpSetup.Timeout = 100000;
            HttpSetup baselineSetup = baseline.HttpSetup;
            HttpManager isolated = SemesterRankPublishTransport.CreateIsolatedManager(baseline);
            Expect(errors, isolated.MaxRetryCount == 0 && isolated.HttpSetup.Timeout == SemesterRankPublishTransport.TimeoutMilliseconds, "獨立 HttpManager 的有效設定是不重送與有限逾時");
            Expect(errors, baseline.MaxRetryCount == 3 && baseline.HttpSetup.Timeout == 100000 && object.ReferenceEquals(baseline.HttpSetup, baselineSetup), "建立公布連線時不改原來的 HttpManager");
            Expect(errors, !object.ReferenceEquals(isolated, baseline) && !object.ReferenceEquals(isolated.HttpSetup, baseline.HttpSetup) && object.ReferenceEquals(isolated.HttpSetup.WebProxy, baseline.HttpSetup.WebProxy), "沿用 Proxy，但逾時設定是另一份");
            Expect(errors, Connection.DefaultHttpManager.MaxRetryCount == defaultRetry && Connection.DefaultHttpManager.HttpSetup.Timeout == defaultTimeout, "檢查本身也不改全域 DefaultHttpManager");
            Expect(errors, SemesterRankPublishTransport.TimeoutMilliseconds > 100000 && SemesterRankPublishTransport.TimeoutMilliseconds < int.MaxValue, "逾時必須長於安裝組件的 100 秒，而且是有限值");

            Expect(errors, SemesterRankPublishTransport.IsUnconfirmed(new WebException("The operation has timed out.", WebExceptionStatus.Timeout)), "HTTP 逾時要當成結果未確認");
            Expect(errors, SemesterRankPublishTransport.IsUnconfirmed(new IOException("連線中斷")), "連線中斷要當成結果未確認");
            DSAServerException explicitFailure = new DSAServerException("ServiceExecutionError", "執行資料庫查詢時發生錯誤", "<Request />", "<Envelope><Body><Fault><Code>23505</Code><Message>duplicate key</Message></Fault></Body></Envelope>", null);
            Expect(errors, !SemesterRankPublishTransport.IsUnconfirmed(explicitFailure), "資料庫明確失敗不進入未確認核對成功");
            Expect(errors, !SemesterRankPublishTransport.IsUnconfirmed(new InvalidOperationException("結果未確認", explicitFailure)), "內層已有資料庫錯誤時保持明確失敗");

            string operationId = "SR2026110420072800ab";
            string formula = SemesterRankFormulaSql.BuildOriginOnly();
            SemesterRankGradeStudents grade = new SemesterRankGradeStudents();
            grade.GradeYear = 3;
            grade.Students = new List<SemesterRankStudentInput>();
            grade.Students.Add(new SemesterRankStudentInput { StudentId = 1001, StudentName = "s", GradeYear = 3, DeptName = "", ClassName = "", Tag1 = "", Tag2 = "" });
            SemesterRankPublishStatement publish = SemesterRankPublishSql.Build(new SemesterRankGradeStudents[] { grade }, "114", "2", "<Setting />", "[]", formula, false, false, operationId);
            string without = SemesterRankPublishSql.Build(new SemesterRankGradeStudents[] { grade }, "114", "2", "<Setting />", "[]", formula, false).Sql;
            string lockCall = SemesterRankPublishSql.AdvisoryLockCall(operationId);
            Expect(errors, publish.Sql.IndexOf(lockCall) >= 0 && Count(publish.Sql, "pg_advisory_xact_lock") == 1 && publish.Sql.IndexOf(SemesterRankPublishSql.OperationAttribute(operationId)) >= 0 && publish.Sql.IndexOf(';') < 0, "公布語句只加一次交易鎖與操作識別，仍是單一敘述");
            Expect(errors, without.IndexOf("pg_advisory_xact_lock") < 0, "沒有操作識別的既有組句不加入鎖定");
            string publishSource = File.ReadAllText("MotherGroup\\SemesterRankPublishSql.cs", Encoding.UTF8);
            Expect(errors, lockCall.IndexOf("hashtextextended") < 0 && publishSource.IndexOf("hashtextextended") < 0 && publishSource.IndexOf(".GetHashCode(") < 0 && publishSource.IndexOf("SHA256.Create()") >= 0, "鎖定鍵用固定 SHA-256，不用 hashtextextended 或字串雜湊碼");
            int key1;
            int key2;
            SemesterRankPublishSql.AdvisoryLockKeys(operationId, out key1, out key2);
            int again1;
            int again2;
            SemesterRankPublishSql.AdvisoryLockKeys(operationId, out again1, out again2);
            Expect(errors, key1 == again1 && key2 == again2 && lockCall == SemesterRankPublishSql.AdvisoryLockCall(operationId), "相同操作識別固定產生同一組鎖定鍵");
            bool rejected = false;
            try
            {
                SemesterRankPublishSql.Build(new SemesterRankGradeStudents[] { grade }, "114", "2", "<Setting />", "[]", formula, false, false, "批次305");
            }
            catch (InvalidOperationException)
            {
                rejected = true;
            }
            Expect(errors, rejected, "不合法的操作識別不能進入公布語句");
            string wait = SemesterRankPublishTransport.WaitSql(operationId);
            string lookup = SemesterRankPublishTransport.LookupSql(operationId);
            Expect(errors, wait.IndexOf(lockCall) >= 0 && wait.IndexOf(';') < 0 && lookup.IndexOf(';') < 0 && lookup.IndexOf("NOT EXISTS") < 0 && lookup.IndexOf(operationId) >= 0, "核對先等待同一把鎖，不用 NOT EXISTS，也不重送公布語句");

            DataTable studentsOnly = ObservationRow(0, 0, 664, 0);
            SemesterRankBatchObservation students = SemesterRankPublishTransport.ReadObservation(studentsOnly);
            Expect(errors, !students.Complete && students.StudentCount == 664, "只有學生人數不能當成公布成功");
            DataTable partial = ObservationRow(10, 0, 664, 5);
            Expect(errors, !SemesterRankPublishTransport.ReadObservation(partial).Complete, "有效矩陣與矩陣數不一致不能當成成功");
            DataTable complete = ObservationRow(2138, 2138, 664, 116918);
            SemesterRankBatchObservation done = SemesterRankPublishTransport.ReadObservation(complete);
            Expect(errors, done.Complete && done.DetailCount == 116918, "同一操作識別的完整矩陣與明細才算核對成功");
            complete.Rows.Add("2138", "2138", "664", "116918");
            Expect(errors, !SemesterRankPublishTransport.ReadObservation(complete).Complete, "同一個操作識別出現兩筆批次不能當成成功");

            SemesterRankCalculationTrace trace = SemesterRankCalculationTrace.Start("114", "2", "依目前學生資料產生", "");
            SemesterRankFailureReport report = SemesterRankFailureReport.Create(trace, new InvalidOperationException(SemesterRankPublishTransport.UnconfirmedMessage(trace), new WebException("The operation has timed out.", WebExceptionStatus.Timeout)), true);
            Expect(errors, report.EvidenceKind == "unconfirmed" && report.UserMessage.IndexOf("沒有重新送出寫入") >= 0 && report.UserMessage.IndexOf("計算完成") < 0 && report.LogText.IndexOf("The operation has timed out") >= 0, "未確認的畫面不顯示成功，紀錄仍保留 HTTP 逾時");
            return errors;
        }

        private static DataTable ObservationRow(long matrix, long alive, long students, long details)
        {
            DataTable table = new DataTable();
            table.Columns.Add("matrix_count");
            table.Columns.Add("alive_matrix_count");
            table.Columns.Add("student_count");
            table.Columns.Add("detail_count");
            table.Rows.Add(matrix.ToString(), alive.ToString(), students.ToString(), details.ToString());
            return table;
        }

        private static List<string> CheckGridTiming()
        {
            List<string> errors = new List<string>();
            const int count = 5400;
            List<RankDataInfo> rows = new List<RankDataInfo>();
            for (int i = 0; i < count; i++)
            {
                RankDataInfo row = new RankDataInfo();
                row.rank_matrix_id = (i + 1).ToString();
                row.item_name = "學業";
                row.rank_type = "年排名";
                row.rank_name = "1年級";
                row.student_number = i.ToString();
                row.student_name = "s" + i;
                row.score = "90";
                row.rank = "1";
                row.pr = "50";
                row.percentile = "50";
                row.school_year = "114";
                row.semester = "2";
                rows.Add(row);
            }
            using (Form form = new Form())
            using (DataGridView grid = new DevComponents.DotNetBar.Controls.DataGridViewX())
            {
                form.ShowInTaskbar = false;
                form.ClientSize = new Size(900, 240);
                grid.Dock = DockStyle.Fill;
                grid.AllowUserToAddRows = false;
                grid.ReadOnly = true;
                grid.Columns.Add("id", "id");
                grid.Columns.Add("score", "score");
                grid.VirtualMode = true;
                int index = 0;
                grid.CellValueNeeded += delegate (object sender, DataGridViewCellValueEventArgs e)
                {
                    if (e.RowIndex < 0 || e.RowIndex >= rows.Count)
                        return;
                    e.Value = e.ColumnIndex == 0 ? rows[e.RowIndex].rank_matrix_id : rows[e.RowIndex].score;
                };
                form.Controls.Add(grid);
                form.Show();
                Stopwatch virtualWatch = Stopwatch.StartNew();
                grid.RowCount = rows.Count;
                object first = grid.Rows[0].Cells[0].Value;
                object last = grid.Rows[count - 1].Cells[1].Value;
                virtualWatch.Stop();
                Expect(errors, (string)first == "1" && (string)last == "90", "虛擬模式應讀到第一列與最後一列");
                Console.WriteLine("virtual " + count + " rows: " + virtualWatch.ElapsedMilliseconds + "ms");

                Stopwatch rowWatch = Stopwatch.StartNew();
                List<DataGridViewRow> created = new List<DataGridViewRow>();
                for (int i = 0; i < count; i++)
                {
                    DataGridViewRow gridRow = new DataGridViewRow();
                    gridRow.CreateCells(grid);
                    gridRow.Cells[0].Value = rows[i].rank_matrix_id;
                    gridRow.Cells[1].Value = rows[i].score;
                    created.Add(gridRow);
                    index++;
                }
                rowWatch.Stop();
                Console.WriteLine("materialized " + count + " rows: " + rowWatch.ElapsedMilliseconds + "ms; created=" + index);
                Expect(errors, virtualWatch.ElapsedMilliseconds < 2000, "虛擬模式繫結超過 2 秒：" + virtualWatch.ElapsedMilliseconds);
                form.Close();
            }
            return errors;
        }

        private static int Count(string text, string value)
        {
            int count = 0;
            int from = 0;
            while (from < text.Length)
            {
                int at = text.IndexOf(value, from);
                if (at < 0)
                    break;
                count++;
                from = at + value.Length;
            }
            return count;
        }

        private static void Expect(List<string> errors, bool condition, string message)
        {
            if (!condition)
                errors.Add(message);
        }
    }
}
