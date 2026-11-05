using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using FISCA.DSAUtil;

namespace SHEvaluation.Rank.MotherGroup
{
    public sealed class SemesterRankCalculationTrace
    {
        private readonly Stopwatch _watch = Stopwatch.StartNew();
        private readonly List<string> _stages = new List<string>();
        private long _last;

        public string Id { get; private set; }
        public string SchoolYear { get; private set; }
        public string Semester { get; private set; }
        public string SourceName { get; private set; }
        public string HistoryBatchId { get; private set; }
        public int StudentCount { get; private set; }
        public string GradeCounts { get; private set; }
        public int SkippedWithoutGrade { get; private set; }
        public int SqlBytes { get; set; }
        public bool TransportPrepared { get; set; }
        public int TimeoutMilliseconds { get; set; }
        public int MaxRetryCount { get; set; }
        public bool IsolatedManager { get; set; }
        public bool SharedManagerUntouched { get; set; }
        public bool DefaultManagerUntouched { get; set; }
        public int TripStarts { get; set; }
        public int LargeWriteTrips { get; set; }
        public int RetryEvents { get; set; }
        public string RetryEventText { get; set; }
        public DateTimeOffset StartedAt { get; private set; }
        public DateTimeOffset EndedAt { get; set; }
        public string TransportResult { get; set; }
        public int ObservedBatchCount { get; set; }
        public long ObservedMatrixCount { get; set; }
        public long ObservedAliveMatrixCount { get; set; }
        public long ObservedStudentCount { get; set; }
        public long ObservedDetailCount { get; set; }

        public static SemesterRankCalculationTrace Start(string schoolYear, string semester, string sourceName, string historyBatchId)
        {
            SemesterRankCalculationTrace trace = new SemesterRankCalculationTrace();
            trace.Id = "SR" + DateTime.Now.ToString("yyyyMMddHHmmss") + Guid.NewGuid().ToString("N").Substring(0, 4);
            trace.SchoolYear = schoolYear ?? "";
            trace.Semester = semester ?? "";
            trace.SourceName = sourceName ?? "";
            trace.HistoryBatchId = historyBatchId ?? "";
            trace.GradeCounts = "";
            trace.RetryEventText = "";
            trace.TransportResult = "";
            trace.StartedAt = DateTimeOffset.Now;
            return trace;
        }

        public void Mark(string stage)
        {
            long elapsed = _watch.ElapsedMilliseconds;
            long delta = elapsed - _last;
            _last = elapsed;
            _stages.Add((stage ?? "") + " " + delta + " 毫秒，累計 " + elapsed + " 毫秒");
        }

        public void ReadStudents(SemesterRankStudentSqlResult result)
        {
            int total = 0;
            StringBuilder text = new StringBuilder();
            if (result != null && result.Grades != null)
            {
                foreach (SemesterRankGradeStudents grade in result.Grades)
                {
                    if (grade == null)
                        continue;
                    int count = grade.Students != null ? grade.Students.Count : (grade.Selects == null ? 0 : grade.Selects.Count);
                    total += count;
                    if (text.Length > 0)
                        text.Append(',');
                    text.Append(grade.GradeYear).Append('=').Append(count);
                }
            }
            StudentCount = total;
            GradeCounts = text.ToString();
            SkippedWithoutGrade = result == null || result.SkippedWithoutGrade == null ? 0 : result.SkippedWithoutGrade.Count;
        }

        public string StageText()
        {
            return _stages.Count == 0 ? "（尚未記錄階段）" : string.Join("；", _stages.ToArray());
        }

        public void NoteTrip(long requestBytes, int largeThreshold)
        {
            lock (this)
            {
                TripStarts++;
                if (requestBytes >= largeThreshold)
                    LargeWriteTrips++;
            }
        }

        public void AddRetryEvent(long requestBytes)
        {
            lock (this)
            {
                RetryEvents++;
                if (RetryEventText == null)
                    RetryEventText = "";
                if (RetryEventText.Length > 0)
                    RetryEventText += "；";
                RetryEventText += "HTTP重試已取消 要求位元組=" + requestBytes;
            }
        }

        public string TransportText()
        {
            if (!TransportPrepared && TripStarts == 0 && RetryEvents == 0)
                return "";
            StringBuilder text = new StringBuilder();
            text.Append("操作識別=").Append(Id);
            text.Append("\n有效逾時毫秒=").Append(TimeoutMilliseconds);
            text.Append("\n有效重試上限=").Append(MaxRetryCount);
            text.Append("\n獨立HttpManager=").Append(IsolatedManager ? "true" : "false");
            text.Append("\n共用連線HttpManager未改=").Append(SharedManagerUntouched ? "true" : "false");
            text.Append("\n全域DefaultHttpManager未改=").Append(DefaultManagerUntouched ? "true" : "false");
            text.Append("\n行程開始=").Append(TripStarts);
            text.Append("\n大型寫入行程=").Append(LargeWriteTrips);
            text.Append("\nHTTP重試事件=").Append(RetryEvents);
            text.Append("\n重試事件內容=").Append(string.IsNullOrEmpty(RetryEventText) ? "（無）" : RetryEventText);
            text.Append("\n傳輸開始=").Append(StartedAt.ToString("yyyy-MM-dd HH:mm:ss.fff zzz"));
            text.Append("\n傳輸結束=").Append(EndedAt == default(DateTimeOffset) ? "（尚未結束）" : EndedAt.ToString("yyyy-MM-dd HH:mm:ss.fff zzz"));
            text.Append("\n傳輸結果=").Append(string.IsNullOrEmpty(TransportResult) ? "（尚未判定）" : TransportResult);
            text.Append("\n核對批次數=").Append(ObservedBatchCount);
            text.Append("\n核對矩陣=").Append(ObservedMatrixCount);
            text.Append("\n核對有效矩陣=").Append(ObservedAliveMatrixCount);
            text.Append("\n核對學生=").Append(ObservedStudentCount);
            text.Append("\n核對明細=").Append(ObservedDetailCount);
            return text.ToString();
        }
    }

    public sealed class SemesterRankFailureReport
    {
        public string Id { get; private set; }
        public string UserMessage { get; private set; }
        public string LogText { get; private set; }
        public string EvidenceKind { get; private set; }

        public static SemesterRankFailureReport Create(SemesterRankCalculationTrace trace, Exception error)
        {
            return Create(trace, error, true);
        }

        public static SemesterRankFailureReport Create(SemesterRankCalculationTrace trace, Exception error, bool warnBeforeRetry)
        {
            if (trace == null)
                trace = SemesterRankCalculationTrace.Start("", "", "", "");
            string chain = ExceptionChain(error);
            string code = FirstSqlState(error, chain);
            string dsaStatus = FirstDsaStatus(error);
            string kind = Classify(code, chain);
            StringBuilder user = new StringBuilder();
            user.Append(warnBeforeRetry ? "計算排名失敗。" : "排名資料讀取失敗。");
            user.Append("\n").Append(UserHint(kind, code, chain));
            user.Append("\n錯誤識別碼：").Append(trace.Id);
            if (warnBeforeRetry)
                user.Append("\n請把識別碼交給維護人員。確認沒有新的有效排名之前不要重算，也不要刪除批次。");
            else
                user.Append("\n請把識別碼交給維護人員。可以關閉視窗後再查詢，這次沒有改排名資料。");

            StringBuilder log = new StringBuilder();
            log.Append("錯誤識別碼=").Append(trace.Id);
            log.Append("\n證據分類=").Append(kind);
            log.Append("\n資料庫代碼=").Append(code.Length == 0 ? "（沒有取得）" : code);
            log.Append("\nDSA狀態=").Append(dsaStatus.Length == 0 ? "（沒有取得）" : dsaStatus);
            log.Append("\n計算期間=").Append(trace.SchoolYear).Append('-').Append(trace.Semester);
            log.Append("\n來源=").Append(trace.SourceName);
            log.Append("\n歷史批次識別=").Append(trace.HistoryBatchId.Length == 0 ? "（無）" : trace.HistoryBatchId);
            log.Append("\n學生人數=").Append(trace.StudentCount);
            log.Append("\n各年級人數=").Append(trace.GradeCounts.Length == 0 ? "（無）" : trace.GradeCounts);
            log.Append("\n無法解析年級人數=").Append(trace.SkippedWithoutGrade);
            log.Append("\n語句位元組=").Append(trace.SqlBytes);
            string transport = trace.TransportText();
            if (transport.Length > 0)
                log.Append("\n").Append(transport);
            log.Append("\n階段=").Append(trace.StageText());
            log.Append("\n例外=\n").Append(chain);
            log.Append("\n").Append(StaticBoundary());

            SemesterRankFailureReport report = new SemesterRankFailureReport();
            report.Id = trace.Id;
            report.EvidenceKind = kind;
            report.UserMessage = user.ToString();
            report.LogText = log.ToString();
            return report;
        }

        public bool WriteFile(string directory)
        {
            try
            {
                if (string.IsNullOrEmpty(directory))
                    directory = DefaultDirectory();
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, Id + ".log"), LogText ?? "", Encoding.UTF8);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void Save()
        {
            bool wrote = WriteFile(DefaultDirectory());
            if (!wrote && UserMessage.IndexOf("本機紀錄寫入失敗") < 0)
                UserMessage += "\n本機紀錄寫入失敗，請仍提供錯誤識別碼。";
            string id = Id;
            string text = LogText;
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    FISCA.LogAgent.ApplicationLog.Log("學期成績排名", "計算或檢索失敗", "semester-rank", id, text ?? "");
                }
                catch (Exception)
                {
                }
            });
        }

        public static string DefaultDirectory()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log", "SemesterRank");
        }

        public static string StaticBoundary()
        {
            return "靜態路徑，不是這次錯誤的結論：公布是單一 PostgreSQL 語句，產品路徑沒有另外送交易控制指令。"
                + "完整性沒通過時，同一句把說明轉成數值使整句失敗；資料庫若在提交前失敗，這句寫入會一起回復。"
                + "擇優設定在排名語句之前寫入，不會跟著回復。"
                + "若閘道在資料庫已提交後才讓前端收到錯誤，畫面仍會顯示失敗，批次可能已經存在，重算可能再新增一批。"
                + "1.0.0.53 失敗後看得到新批次，還不足以證明部分公布。這份紀錄沒有連線資料庫，不能把該次結果標成已回復或已部分公布。"
                + "不要刪除批次。排名公式、五標、平均、標準差、PR、百分比與整體母群沒有改，也沒有依人數拆開排名。"
                + "公布寫入使用獨立連線與獨立 HttpManager，重試上限為 0，不改全域 DefaultHttpManager，也不改登入連線的 HttpManager。"
                + "這次公布的逾時是有限時間。回應中斷時用操作識別等待鎖定後核對，不重送寫入。";
        }

        private static string UserHint(string kind, string code, string chain)
        {
            if (kind == "integrity")
                return "公布完整性檢查沒有通過。" + ShortExtract(chain) + " 詳細代碼在紀錄，畫面不列出學生姓名或成績。";
            if (kind == "timeout")
                return "資料庫回報查詢被取消或逾時" + CodeSuffix(code) + "。這不能當成人數上限。";
            if (kind == "resource")
                return chain.IndexOf("Java heap space", StringComparison.OrdinalIgnoreCase) >= 0 || chain.IndexOf("OutOfMemoryError", StringComparison.OrdinalIgnoreCase) >= 0
                    ? "排名查詢服務記憶體不足，資料未完整讀取。請縮小查詢條件後重新查詢，並提供錯誤識別碼給維護人員。"
                    : "資料庫回報資源不足" + CodeSuffix(code) + "。這不能當成人數上限。";
            if (kind == "constraint")
                return "資料庫回報限制衝突" + CodeSuffix(code) + "。";
            if (kind == "format")
                return "資料庫回報資料格式無法轉換" + CodeSuffix(code) + "。尚未能判斷是哪一筆成績。";
            if (kind == "unconfirmed")
                return "公布要求的回應中斷，資料庫沒有明確回報這次語句失敗。已用操作識別等待鎖定後核對；沒有把學生人數相同當成成功，也沒有重新送出寫入。請用錯誤識別碼核對排名批次。確認沒有這個識別的新批次之前不要重算，也不要刪除批次。";
            return "資料庫查詢沒有完成。目前取得的內容不足以分辨逾時、成績格式、限制衝突或公布完整性檢查。";
        }

        private static string CodeSuffix(string code)
        {
            return code.Length == 0 ? "" : "（代碼 " + code + "）";
        }

        private static string ShortExtract(string chain)
        {
            if (string.IsNullOrEmpty(chain))
                return "";
            int at = chain.IndexOf("沒有符合排名條件");
            if (at < 0)
                at = chain.IndexOf("明細筆數");
            if (at < 0)
                at = chain.IndexOf("母群矩陣");
            if (at < 0)
                at = chain.IndexOf("批次學生");
            if (at < 0)
                at = chain.IndexOf("排名身分");
            if (at < 0)
                at = IndexOfCountField(chain);
            if (at < 0)
                return "";
            int length = Math.Min(120, chain.Length - at);
            return " 摘錄：" + chain.Substring(at, length).Replace("\r", " ").Replace("\n", " ");
        }

        public static string Classify(string code, string chain)
        {
            string text = chain ?? "";
            if (text.IndexOf("沒有符合排名條件的學期成績") >= 0
                || text.IndexOf("明細筆數") >= 0
                || text.IndexOf("missing_matrix_count") >= 0
                || text.IndexOf("母群矩陣缺少") >= 0
                || text.IndexOf("批次學生缺少") >= 0
                || text.IndexOf("排名身分") >= 0
                || text.IndexOf("完整性檢查未通過") >= 0
                || IndexOfCountField(text) >= 0)
                return "integrity";
            if (code == "57014" || text.IndexOf("statement timeout") >= 0 || text.IndexOf("canceling statement") >= 0 || text.IndexOf("查詢逾時") >= 0)
                return "timeout";
            if (code == "53200" || code == "53100" || text.IndexOf("out of memory") >= 0 || text.IndexOf("disk full") >= 0
                || text.IndexOf("Java heap space", StringComparison.OrdinalIgnoreCase) >= 0
                || text.IndexOf("OutOfMemoryError", StringComparison.OrdinalIgnoreCase) >= 0)
                return "resource";
            if ((!string.IsNullOrEmpty(code) && code.StartsWith("23"))
                || text.IndexOf("duplicate key") >= 0
                || text.IndexOf("unique constraint") >= 0
                || text.IndexOf("not-null constraint") >= 0
                || text.IndexOf("foreign key constraint") >= 0
                || text.IndexOf("check constraint") >= 0)
                return "constraint";
            if (code == "22P02" || code == "22003" || code == "22P03" || text.IndexOf("invalid input syntax") >= 0)
                return "format";
            if (text.IndexOf("結果未確認") >= 0)
                return "unconfirmed";
            return "unknown";
        }

        private static string FirstSqlState(Exception error, string chain)
        {
            Exception current = error;
            int depth = 0;
            while (current != null && depth < 8)
            {
                string code = ReadCode(current);
                if (code.Length > 0)
                    return code;
                current = current.InnerException;
                depth++;
            }
            string fromResponse = ReadResponseSqlState(error);
            if (fromResponse.Length > 0)
                return fromResponse;
            Match match = Regex.Match(chain ?? "", @"SQLSTATE[:\s=]+([0-9A-Z]{5})", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value.ToUpperInvariant() : "";
        }

        private static string ReadResponseSqlState(Exception error)
        {
            Exception current = error;
            int depth = 0;
            while (current != null && depth < 8)
            {
                string code = SqlStateFromExceptionResponse(current);
                if (code.Length > 0)
                    return code;
                current = current.InnerException;
                depth++;
            }
            return "";
        }

        private static string SqlStateFromExceptionResponse(Exception error)
        {
            DSAServerException util = error as DSAServerException;
            FISCA.DSAClient.DSAServerException client = error as FISCA.DSAClient.DSAServerException;
            if (util != null)
            {
                FISCA.DSAClient.DSAServerException warped = TryWarpedError(util);
                if (warped != null)
                    client = warped;
            }
            if (client == null || string.IsNullOrEmpty(client.Response))
                return "";
            return SqlStateFromResponse(client.Response);
        }

        private static string SqlStateFromResponse(string response)
        {
            if (string.IsNullOrEmpty(response))
                return "";
            XmlDocument document = new XmlDocument();
            try
            {
                document.LoadXml(response);
            }
            catch (XmlException)
            {
                return "";
            }
            XmlNodeList nodes = document.GetElementsByTagName("*");
            for (int i = 0; i < nodes.Count; i++)
            {
                XmlElement element = nodes[i] as XmlElement;
                if (element == null || !IsDatabaseFault(element))
                    continue;
                string code = SqlStateFromFault(element);
                if (code.Length > 0)
                    return code;
            }
            return "";
        }

        private static bool IsDatabaseFault(XmlElement element)
        {
            string name = element.LocalName;
            return name == "Fault" || name == "InnerFault";
        }

        private static string SqlStateFromFault(XmlElement fault)
        {
            for (XmlNode node = fault.FirstChild; node != null; node = node.NextSibling)
            {
                XmlElement child = node as XmlElement;
                if (child == null || !HasOnlyText(child))
                    continue;
                string name = child.LocalName;
                if (name != "Code" && name != "SqlState")
                    continue;
                string value = child.InnerText.Trim();
                if (LooksLikeSqlState(value))
                    return value.ToUpperInvariant();
            }
            return "";
        }

        private static string ReadCode(Exception error)
        {
            DSAFaultException fault = error as DSAFaultException;
            if (fault != null && LooksLikeSqlState(fault.Code))
                return fault.Code.ToUpperInvariant();
            string[] names = new string[] { "SqlState", "SqlStateCode", "Code" };
            foreach (string name in names)
            {
                PropertyInfo property = error.GetType().GetProperty(name);
                if (property == null || property.PropertyType != typeof(string))
                    continue;
                string value = property.GetValue(error, null) as string;
                if (LooksLikeSqlState(value))
                    return value.ToUpperInvariant();
            }
            return "";
        }

        private static bool LooksLikeSqlState(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length != 5)
                return false;
            for (int i = 0; i < value.Length; i++)
            {
                char ch = value[i];
                bool digit = ch >= '0' && ch <= '9';
                bool upper = ch >= 'A' && ch <= 'Z';
                bool lower = ch >= 'a' && ch <= 'z';
                if (!digit && !upper && !lower)
                    return false;
            }
            return true;
        }

        private static string FirstDsaStatus(Exception error)
        {
            Exception current = error;
            int depth = 0;
            while (current != null && depth < 8)
            {
                string status = ReadDsaStatus(current);
                if (status.Length > 0)
                    return status;
                current = current.InnerException;
                depth++;
            }
            return "";
        }

        private static string ReadDsaStatus(Exception error)
        {
            DSAServerException util = error as DSAServerException;
            if (util != null)
            {
                string fromClient = ReadClientStatus(TryWarpedError(util));
                if (fromClient.Length > 0)
                    return fromClient;
                string status = TryServerStatus(util);
                if (status.Length > 0 && status != "Unknow")
                    return status;
            }
            return ReadClientStatus(error as FISCA.DSAClient.DSAServerException);
        }

        private static string ReadClientStatus(FISCA.DSAClient.DSAServerException client)
        {
            if (client == null || string.IsNullOrEmpty(client.Status))
                return "";
            return client.Status.Trim();
        }

        private static string TryServerStatus(DSAServerException error)
        {
            try
            {
                return error.ServerStatus.ToString();
            }
            catch (NotSupportedException)
            {
                return "";
            }
        }

        private static FISCA.DSAClient.DSAServerException TryWarpedError(DSAServerException error)
        {
            try
            {
                return error.WarpedError;
            }
            catch (NotSupportedException)
            {
                return null;
            }
        }

        private static string ReadSafeServerDetail(Exception error)
        {
            StringBuilder text = new StringBuilder();
            DSAServerException util = error as DSAServerException;
            FISCA.DSAClient.DSAServerException client = error as FISCA.DSAClient.DSAServerException;
            if (util != null)
            {
                AppendSafeFragment(text, TryDetailMessage(util));
                FISCA.DSAClient.DSAServerException warped = TryWarpedError(util);
                if (warped != null)
                    client = warped;
            }
            if (client != null)
            {
                AppendSafeFragment(text, client.AccessPoint);
                if (string.IsNullOrEmpty(client.Response))
                    text.Append("［沒有 Response］");
                else
                    text.Append(ExtractResponse(client.Response));
            }
            return text.ToString().Trim();
        }

        private static string TryDetailMessage(DSAServerException error)
        {
            try
            {
                return error.DetailMessage;
            }
            catch (NotSupportedException)
            {
                return "";
            }
        }

        private static void AppendSafeFragment(StringBuilder text, string value)
        {
            string safe = SafeFragment(value);
            if (safe.Length == 0)
                return;
            if (text.Length > 0)
                text.Append('\n');
            text.Append(safe);
        }

        private static string SafeFragment(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";
            if (value.IndexOf('<') >= 0 || IndexOfSql(value) >= 0 || value.Length > 500)
                return ExtractResponse(value);
            return Sanitize(value);
        }

        private static string ExtractResponse(string response)
        {
            if (string.IsNullOrEmpty(response))
                return "";
            string masked = MaskSecrets(response);
            StringBuilder picked = new StringBuilder();
            XmlDocument document = new XmlDocument();
            bool xml = false;
            try
            {
                document.LoadXml(masked);
                xml = true;
            }
            catch (XmlException)
            {
            }
            if (xml)
            {
                XmlNodeList nodes = document.GetElementsByTagName("*");
                for (int i = 0; i < nodes.Count; i++)
                {
                    XmlElement element = nodes[i] as XmlElement;
                    if (element == null || !HasOnlyText(element))
                        continue;
                    string name = element.LocalName;
                    if (name != "Code" && name != "Message" && name != "Detail" && name != "Source")
                        continue;
                    ConsiderDiagnostic(picked, element.InnerText);
                }
            }
            else
                ConsiderDiagnostic(picked, masked);
            string text = Sanitize(picked.ToString());
            return text.Length == 0 ? "［回應未含可辨識的資料庫錯誤］" : text;
        }

        private static void ConsiderDiagnostic(StringBuilder picked, string value)
        {
            if (string.IsNullOrEmpty(value))
                return;
            string trimmed = value.Trim();
            if (IsShortCode(trimmed))
            {
                if (LooksLikeSqlState(trimmed))
                    picked.Append(trimmed).Append('\n');
                return;
            }
            if (!ContainsDiagnostic(trimmed))
                return;
            string[] lines = trimmed.Replace('\r', '\n').Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length > 0 && (IsShortCode(line) || ContainsDiagnostic(line)))
                    picked.Append(line).Append('\n');
            }
        }

        private static bool HasOnlyText(XmlElement element)
        {
            if (element == null || !element.HasChildNodes)
                return false;
            for (XmlNode node = element.FirstChild; node != null; node = node.NextSibling)
            {
                if (node.NodeType == XmlNodeType.Element)
                    return false;
                if (node.NodeType != XmlNodeType.Text && node.NodeType != XmlNodeType.CDATA && node.NodeType != XmlNodeType.Whitespace && node.NodeType != XmlNodeType.SignificantWhitespace)
                    return false;
            }
            return true;
        }

        private static bool IsShortCode(string value)
        {
            if (LooksLikeSqlState(value))
                return true;
            if (string.IsNullOrEmpty(value) || value.Length < 8 || value.Length > 40)
                return false;
            for (int i = 0; i < value.Length; i++)
            {
                char ch = value[i];
                bool ok = (ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z') || ch == '_';
                if (!ok)
                    return false;
            }
            return true;
        }

        private static bool ContainsDiagnostic(string text)
        {
            string[] keys = new string[] { "ERROR:", "SQLSTATE", "DETAIL:", "HINT:", "canceling statement", "statement timeout", "duplicate key", "not-null", "invalid input syntax", "out of memory", "unique constraint", "check constraint", "foreign key", "沒有符合排名條件", "明細筆數", "missing_matrix_count", "完整性檢查未通過" };
            for (int i = 0; i < keys.Length; i++)
            {
                if (text.IndexOf(keys[i], StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }

        private static string MaskSecrets(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";
            string word = @"(?<![A-Za-z])(password|passwd|pwd|token|session(?:id|token)?|authorization|cookie|passport|credential|secret|securitytoken)(?![A-Za-z])";
            text = Regex.Replace(text, "(?i)" + word + @"\s*=\s*(?:""[^""]*""|'[^']*'|[^\s<>&]+)", "$1=［驗證已省略］");
            text = Regex.Replace(text, "(?i)" + word + @"\s*:\s*\S+", "$1:［驗證已省略］");
            text = Regex.Replace(text, "(?i)<(password|passwd|pwd|token|session[^> ]*|authorization|cookie|passport|credential|secret|securitytoken)(\\s[^>]*)?>[\\s\\S]*?</\\1>", "<$1>［驗證已省略］</$1>");
            text = Regex.Replace(text, "(?i)(https?://)[^/\\s:@]+:[^@\\s]+@", "$1［驗證已省略］@");
            return text;
        }

        public static string ExceptionChain(Exception error)
        {
            StringBuilder text = new StringBuilder();
            int depth = 0;
            while (error != null && depth < 8)
            {
                if (text.Length > 0)
                    text.Append("\n----\n");
                text.Append(error.GetType().FullName);
                string code = ReadCode(error);
                if (code.Length == 0)
                    code = SqlStateFromExceptionResponse(error);
                if (code.Length > 0)
                    text.Append(" sqlState=").Append(code);
                string dsaStatus = ReadDsaStatus(error);
                if (dsaStatus.Length > 0)
                    text.Append(" dsaStatus=").Append(dsaStatus);
                text.Append("\n").Append(Sanitize(error.Message));
                string detail = ReadSafeServerDetail(error);
                if (detail.Length > 0)
                    text.Append("\n").Append(detail);
                DSAFaultException fault = error as DSAFaultException;
                if (fault != null)
                    text.Append("\n").Append(Sanitize(fault.Detail));
                error = error.InnerException;
                depth++;
            }
            if (text.Length == 0)
                text.Append("（沒有例外物件）");
            return text.ToString();
        }

        public static string Sanitize(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";
            string text = value.Replace("\r\n", "\n").Replace('\r', '\n');
            text = MaskSecrets(text);
            text = RemoveMarkedTuple(text, "Failing row contains");
            text = RemoveKeyValues(text);
            text = MaskParameters(text);
            text = MaskQuotes(text);
            text = MaskUnquotedSyntaxValue(text);
            int sql = IndexOfSql(text);
            if (sql >= 0)
                text = text.Substring(0, sql) + "［語句已省略］";
            if (text.Length > 4000)
                text = text.Substring(0, 4000) + "［紀錄過長已截斷］";
            return text;
        }

        private static string RemoveMarkedTuple(string text, string marker)
        {
            StringBuilder result = new StringBuilder();
            int cursor = 0;
            while (cursor < text.Length)
            {
                int at = text.IndexOf(marker, cursor, StringComparison.OrdinalIgnoreCase);
                if (at < 0)
                {
                    result.Append(text, cursor, text.Length - cursor);
                    break;
                }
                result.Append(text, cursor, at - cursor);
                result.Append("［資料列內容已省略］");
                // 資料值本身可含右括號，不能把第一個配對括號當成資料列結尾。
                int next = NextKeyDetail(text, at + marker.Length);
                if (next < 0)
                    return result.ToString();
                if (text[next] != '\n')
                    result.Append(' ');
                cursor = next;
            }
            return result.ToString();
        }

        private static string RemoveKeyValues(string text)
        {
            StringBuilder result = new StringBuilder();
            int cursor = 0;
            while (cursor < text.Length)
            {
                int at = text.IndexOf("Key (", cursor, StringComparison.OrdinalIgnoreCase);
                if (at < 0)
                {
                    result.Append(text, cursor, text.Length - cursor);
                    break;
                }
                int columns = at + 4;
                int columnsEnd = EndBalanced(text, columns);
                if (columnsEnd < 0)
                {
                    result.Append(text, cursor, at - cursor);
                    result.Append("Key (［值已省略］)");
                    return result.ToString();
                }
                int equals = columnsEnd + 1;
                while (equals < text.Length && char.IsWhiteSpace(text[equals]))
                    equals++;
                if (equals >= text.Length || text[equals] != '=')
                {
                    result.Append(text, cursor, at - cursor);
                    result.Append("Key ").Append(text, columns, columnsEnd + 1 - columns).Append("=(［值已省略］)");
                    return result.ToString();
                }
                int values = equals + 1;
                while (values < text.Length && char.IsWhiteSpace(text[values]))
                    values++;
                if (values >= text.Length || text[values] != '(')
                {
                    result.Append(text, cursor, at - cursor);
                    result.Append("Key ").Append(text, columns, columnsEnd + 1 - columns).Append("=(［值已省略］)");
                    return result.ToString();
                }
                int valuesEnd = EndBalanced(text, values);
                if (valuesEnd < 0 || !IsSafeKeySuffix(text, valuesEnd + 1))
                {
                    result.Append(text, cursor, at - cursor);
                    result.Append("Key ").Append(text, columns, columnsEnd + 1 - columns).Append("=(［值已省略］)");
                    return result.ToString();
                }
                result.Append(text, cursor, at - cursor);
                result.Append("Key ").Append(text, columns, columnsEnd + 1 - columns).Append("=(［值已省略］)");
                cursor = valuesEnd + 1;
            }
            return result.ToString();
        }

        private static int NextKeyDetail(string text, int start)
        {
            int from = start;
            while (from < text.Length)
            {
                int at = text.IndexOf("DETAIL:", from, StringComparison.OrdinalIgnoreCase);
                if (at < 0)
                    return -1;
                int i = at + 7;
                while (i < text.Length && char.IsWhiteSpace(text[i]))
                    i++;
                if (i + 4 <= text.Length && string.Compare(text, i, "Key (", 0, 4, StringComparison.OrdinalIgnoreCase) == 0)
                    return at;
                from = at + 7;
            }
            return -1;
        }

        private static bool IsSafeKeySuffix(string text, int afterClose)
        {
            int i = afterClose;
            while (i < text.Length && char.IsWhiteSpace(text[i]))
                i++;
            if (i >= text.Length)
                return true;
            string[] suffixes = new string[] { "already exists", "is not present in table", "is still referenced from table" };
            foreach (string suffix in suffixes)
            {
                if (i + suffix.Length > text.Length)
                    continue;
                if (string.Compare(text, i, suffix, 0, suffix.Length, StringComparison.OrdinalIgnoreCase) != 0)
                    continue;
                int after = i + suffix.Length;
                if (after < text.Length && char.IsLetterOrDigit(text[after]))
                    continue;
                return RestAfterKeySuffix(text, after);
            }
            if (text[i] == '.')
                return RestAfterKeySuffix(text, i + 1);
            return StartsWithSectionMarker(text, i);
        }

        private static bool RestAfterKeySuffix(string text, int start)
        {
            int i = start;
            while (i < text.Length && (char.IsWhiteSpace(text[i]) || text[i] == '.'))
                i++;
            if (i >= text.Length)
                return true;
            if (text[i] == '"' || text[i] == '\'')
            {
                int end = EndQuote(text, i, text[i]);
                if (end < 0)
                    return false;
                return RestAfterKeySuffix(text, end + 1);
            }
            return StartsWithSectionMarker(text, i);
        }

        private static bool StartsWithSectionMarker(string text, int start)
        {
            string[] markers = new string[] { "DETAIL:", "HINT:", "CONTEXT:", "STATEMENT:", "ERROR:", "WARNING:", "QUERY:", "LOCATION:" };
            foreach (string marker in markers)
            {
                if (start + marker.Length <= text.Length && string.Compare(text, start, marker, 0, marker.Length, StringComparison.OrdinalIgnoreCase) == 0)
                    return true;
            }
            return false;
        }

        private static int EndBalanced(string text, int open)
        {
            if (open < 0 || open >= text.Length || text[open] != '(')
                return -1;
            int depth = 0;
            for (int i = open; i < text.Length; i++)
            {
                if (text[i] == '(')
                    depth++;
                else if (text[i] == ')')
                {
                    depth--;
                    if (depth == 0)
                        return i;
                }
            }
            return -1;
        }

        private static string MaskParameters(string text)
        {
            return Regex.Replace(text, @"\$(\d+)\s*=\s*(?:'(?:''|[^'])*'|""[^""]*""|\S+)", "$$$1=［值已省略］");
        }

        private static string MaskQuotes(string text)
        {
            StringBuilder result = new StringBuilder();
            int i = 0;
            while (i < text.Length)
            {
                char quote = text[i];
                if (quote != '\'' && quote != '"')
                {
                    result.Append(text[i]);
                    i++;
                    continue;
                }
                int end = EndQuote(text, i, quote);
                if (end < 0)
                {
                    result.Append(text[i]);
                    i++;
                    continue;
                }
                string inner = text.Substring(i + 1, end - i - 1).Replace("''", "'");
                if (quote == '"' && PrecededBySchemaKeyword(text, i))
                    result.Append(text, i, end + 1 - i);
                else
                {
                    result.Append(quote).Append("［值已省略］").Append(quote);
                    string diagnostic = ExtractDiagnostics(inner);
                    if (diagnostic.Length > 0)
                        result.Append(' ').Append(diagnostic);
                }
                i = end + 1;
            }
            return result.ToString();
        }

        private static int EndQuote(string text, int start, char quote)
        {
            for (int i = start + 1; i < text.Length; i++)
            {
                if (text[i] != quote)
                    continue;
                if (quote == '\'' && i + 1 < text.Length && text[i + 1] == '\'')
                {
                    i++;
                    continue;
                }
                return i;
            }
            return -1;
        }

        private static bool PrecededBySchemaKeyword(string text, int quoteAt)
        {
            int end = quoteAt;
            while (end > 0 && char.IsWhiteSpace(text[end - 1]))
                end--;
            string[] words = new string[] { "constraint", "column", "relation", "index", "table" };
            foreach (string word in words)
            {
                if (end < word.Length)
                    continue;
                int start = end - word.Length;
                if (string.Compare(text, start, word, 0, word.Length, StringComparison.OrdinalIgnoreCase) != 0)
                    continue;
                if (start > 0 && char.IsLetterOrDigit(text[start - 1]))
                    continue;
                return true;
            }
            return false;
        }

        private static int IndexOfCountField(string text)
        {
            if (string.IsNullOrEmpty(text))
                return -1;
            for (int i = 0; i < text.Length; i++)
            {
                if (MatchCountField(text, i) != null)
                    return i;
            }
            return -1;
        }

        private static string ExtractDiagnostics(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";
            string[] phrases = new string[] { "沒有符合排名條件的學期成績", "完整性檢查未通過" };
            StringBuilder found = new StringBuilder();
            int i = 0;
            while (i < text.Length)
            {
                string field = MatchCountField(text, i);
                if (field != null)
                {
                    if (found.Length > 0)
                        found.Append(' ');
                    found.Append(field);
                    i += field.Length;
                    continue;
                }
                string phrase = MatchAt(text, i, phrases);
                if (phrase != null)
                {
                    if (found.Length > 0)
                        found.Append(' ');
                    found.Append(phrase);
                    i += phrase.Length;
                    continue;
                }
                i++;
            }
            return found.ToString();
        }

        private static string MatchCountField(string text, int index)
        {
            if (!IsTokenStart(text, index))
                return null;
            string name = MatchAt(text, index, CountFields());
            if (name == null)
                return null;
            int eq = index + name.Length;
            if (eq >= text.Length || text[eq] != '=')
                return null;
            int end = eq + 1;
            int num = end;
            while (end < text.Length && text[end] >= '0' && text[end] <= '9')
                end++;
            if (end == num)
                return null;
            if (end + 1 < text.Length && text[end] == '.' && text[end + 1] >= '0' && text[end + 1] <= '9')
                return null;
            if (end < text.Length && (text[end] == '_' || char.IsLetter(text[end])))
                return null;
            return text.Substring(index, end - index);
        }

        private static string[] CountFields()
        {
            return new string[]
            {
                "missing_matrix_count",
                "missing_student_count",
                "student_conflict_count",
                "detail_count",
                "score_count",
                "五種排名皆不參與",
                "可排名科目",
                "可排名分項",
                "母群學生",
                "科目成績",
                "分項成績"
            };
        }

        private static string MatchAt(string text, int index, string[] tokens)
        {
            string found = null;
            foreach (string token in tokens)
            {
                if (token == null || index + token.Length > text.Length)
                    continue;
                if (string.Compare(text, index, token, 0, token.Length, StringComparison.Ordinal) != 0)
                    continue;
                if (found == null || token.Length > found.Length)
                    found = token;
            }
            return found;
        }

        private static bool IsTokenStart(string text, int index)
        {
            if (index <= 0)
                return true;
            char prev = text[index - 1];
            return !char.IsLetterOrDigit(prev) && prev != '_';
        }

        private static string MaskUnquotedSyntaxValue(string text)
        {
            return Regex.Replace(text, @"(?i)(invalid input syntax for type\s+[^:\n]{1,80}:\s*)([+-]?\d+(?:\.\d+)?)", "$1［值已省略］");
        }

        private static int IndexOfSql(string text)
        {
            string[] markers = new string[] { "WITH student_list", "jsonb_to_recordset", "INSERT INTO", "unnest(xpath", "WITH ", "SELECT ", "UPDATE ", "DELETE FROM", "(SELECT " };
            int found = -1;
            foreach (string marker in markers)
            {
                int at = text.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                if (at >= 0 && (found < 0 || at < found))
                    found = at;
            }
            return found;
        }
    }
}
