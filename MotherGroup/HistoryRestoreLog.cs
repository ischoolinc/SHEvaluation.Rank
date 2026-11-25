using System;
using System.IO;
using System.Text;

namespace SHEvaluation.Rank.MotherGroup
{
    public sealed class HistoryRestoreLog
    {
        public const string PeriodStep = "期間核對";
        public const string SettingStep = "批次設定與快照";
        public const string TagStep = "學生標籤";
        public const string StudentStep = "歷史學生";

        private readonly string _batchId;
        private readonly StringBuilder _text = new StringBuilder();

        private HistoryRestoreLog(string batchId)
        {
            _batchId = batchId ?? "";
            _text.Append("批次識別=").Append(SafeBatchId(_batchId)).Append("\n");
        }

        public static HistoryRestoreLog Start(string batchId)
        {
            return new HistoryRestoreLog(batchId);
        }

        public void Record(string step, long elapsedMs, int rowCount, string detail)
        {
            Append(step, elapsedMs, rowCount, detail, "");
        }

        public void Fail(string step, long elapsedMs, Exception ex)
        {
            string error = ex == null ? "" : ex.GetType().Name + ": " + OneLine(ex.Message);
            Append(step, elapsedMs, 0, "", error);
        }

        public void RecordStudentRead(long queryMs, long mergeMs, int pages, int rowCount)
        {
            _text.Append("步驟=").Append(StudentStep).Append("\n");
            _text.Append("查詢耗時毫秒=").Append(queryMs.ToString()).Append("\n");
            _text.Append("合併耗時毫秒=").Append(mergeMs.ToString()).Append("\n");
            _text.Append("頁數=").Append(pages.ToString()).Append("\n");
            _text.Append("回傳筆數=").Append(rowCount.ToString()).Append("\n");
            _text.Append("例外=（沒有）\n");
        }

        public string Text()
        {
            return _text.ToString();
        }

        public static string FailureMessage(string batchId, string step, string reason)
        {
            return "歷史批次 " + SafeBatchId(batchId) + " 的" + (step ?? "") + "讀取失敗。\n"
                + (reason ?? "")
                + "\n本次不會進入確認清單，也不會開始排名計算。";
        }

        public static string FailureMessage(string batchId, string step, Exception ex)
        {
            return FailureMessage(batchId, step, ex == null ? "" : ex.Message);
        }

        public void Write()
        {
            try
            {
                string directory = SemesterRankFailureReport.DefaultDirectory();
                Directory.CreateDirectory(directory);
                string name = "HR" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + "-" + SafeBatchId(_batchId) + ".log";
                File.WriteAllText(Path.Combine(directory, name), _text.ToString(), Encoding.UTF8);
            }
            catch (Exception)
            {
            }
        }

        private void Append(string step, long elapsedMs, int rowCount, string detail, string error)
        {
            _text.Append("步驟=").Append(step ?? "").Append("\n");
            _text.Append("耗時毫秒=").Append(elapsedMs.ToString()).Append("\n");
            _text.Append("回傳筆數=").Append(rowCount.ToString()).Append("\n");
            if (!string.IsNullOrEmpty(detail))
                _text.Append("補充=").Append(OneLine(detail)).Append("\n");
            _text.Append("例外=").Append(error.Length == 0 ? "（沒有）" : error).Append("\n");
        }

        private static string OneLine(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";
            string flat = text.Replace("\r", " ").Replace("\n", " ");
            if (flat.Length > 300)
                return flat.Substring(0, 300);
            return flat;
        }

        private static string SafeBatchId(string batchId)
        {
            long id;
            if (!long.TryParse(batchId == null ? "" : batchId.Trim(), out id) || id <= 0)
                return "unknown";
            return id.ToString();
        }
    }
}
