using FISCA.Authentication;
using FISCA.Data;
using FISCA.DSAClient;
using FISCA.DSAClient.HttpUtil;
using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace SHEvaluation.Rank.MotherGroup
{
    public sealed class SemesterRankBatchObservation
    {
        public int BatchCount { get; set; }
        public long MatrixCount { get; set; }
        public long AliveMatrixCount { get; set; }
        public long StudentCount { get; set; }
        public long DetailCount { get; set; }
        public bool Complete { get; set; }
    }

    /// <summary>
    /// 排名公布寫入使用獨立連線。AsContract 以 MemberwiseClone 回傳新物件；若與登入連線相同就中止。
    /// </summary>
    public static class SemesterRankPublishTransport
    {
        public const int TimeoutMilliseconds = 900000;
        public const int LargeWriteBytes = 32768;

        public static HttpManager CreateIsolatedManager(HttpManager baseline)
        {
            if (baseline == null)
                throw new ArgumentNullException("baseline");
            HttpSetup source = baseline.HttpSetup ?? new HttpSetup();
            HttpSetup setup = new HttpSetup();
            setup.DataBlockSize = source.DataBlockSize;
            setup.KeepAlive = source.KeepAlive;
            setup.Method = source.Method;
            setup.ContentType = source.ContentType;
            setup.WebProxy = source.WebProxy;
            setup.AllowCompression = source.AllowCompression;
            setup.Timeout = TimeoutMilliseconds;
            HttpManager dedicated = new HttpManager();
            dedicated.HttpSetup = setup;
            dedicated.MaxRetryCount = 0;
            dedicated.TripRetryOccurring += delegate (object sender, HttpTripRetryEventArgs args)
            {
                if (args != null)
                    args.Cancel = true;
            };
            return dedicated;
        }

        public static QueryHelper OpenWriter(SemesterRankCalculationTrace trace)
        {
            if (trace == null)
                throw new ArgumentNullException("trace");
            trace.TransportPrepared = true;
            Connection shared = DSAServices.DefaultDataSource;
            if (shared == null || !shared.IsConnected)
                throw new InvalidOperationException("尚未登入，不能公布排名。");

            HttpManager sharedManager = shared.HttpManager;
            HttpManager baseline = sharedManager ?? Connection.DefaultHttpManager;
            int defaultRetry = Connection.DefaultHttpManager.MaxRetryCount;
            int defaultTimeout = Connection.DefaultHttpManager.HttpSetup.Timeout;
            int baselineRetry = baseline.MaxRetryCount;
            int baselineTimeout = baseline.HttpSetup.Timeout;

            Connection isolated = shared.AsContract(shared.TargetContract);
            trace.SharedManagerUntouched = object.ReferenceEquals(shared.HttpManager, sharedManager);
            trace.DefaultManagerUntouched = Connection.DefaultHttpManager.MaxRetryCount == defaultRetry
                && Connection.DefaultHttpManager.HttpSetup.Timeout == defaultTimeout;
            if (object.ReferenceEquals(isolated, shared))
                throw new InvalidOperationException("AsContract 沒有回傳獨立連線，已中止公布。");
            if (!trace.SharedManagerUntouched || !trace.DefaultManagerUntouched)
                throw new InvalidOperationException("準備公布連線時改到共用傳輸設定，已中止公布。");

            HttpManager dedicated = CreateIsolatedManager(baseline);
            dedicated.TripStarting += delegate (object sender, HttpTripStartingEventArgs args)
            {
                trace.NoteTrip(RequestBytes(args == null ? null : args.Trip), LargeWriteBytes);
            };
            dedicated.TripRetryOccurring += delegate (object sender, HttpTripRetryEventArgs args)
            {
                if (args != null)
                    args.Cancel = true;
                trace.AddRetryEvent(RequestBytes(args == null ? null : args.Trip));
            };
            isolated.HttpManager = dedicated;

            trace.TimeoutMilliseconds = dedicated.HttpSetup.Timeout;
            trace.MaxRetryCount = dedicated.MaxRetryCount;
            trace.IsolatedManager = !object.ReferenceEquals(dedicated, Connection.DefaultHttpManager)
                && !object.ReferenceEquals(dedicated, sharedManager);
            trace.SharedManagerUntouched = object.ReferenceEquals(shared.HttpManager, sharedManager);
            trace.DefaultManagerUntouched = Connection.DefaultHttpManager.MaxRetryCount == defaultRetry
                && Connection.DefaultHttpManager.HttpSetup.Timeout == defaultTimeout
                && baseline.MaxRetryCount == baselineRetry
                && baseline.HttpSetup.Timeout == baselineTimeout;
            if (dedicated.MaxRetryCount != 0 || dedicated.HttpSetup.Timeout != TimeoutMilliseconds)
                throw new InvalidOperationException("公布連線仍會自動重送或逾時不是這次的有限設定，已中止公布。");
            if (!trace.IsolatedManager || !trace.SharedManagerUntouched || !trace.DefaultManagerUntouched)
                throw new InvalidOperationException("公布連線沒有和共用傳輸設定分開，已中止公布。");
            return new QueryHelper(isolated);
        }

        public static bool IsUnconfirmed(Exception error)
        {
            bool transport = false;
            Exception current = error;
            int depth = 0;
            while (current != null && depth < 8)
            {
                if (current is DSAServerException || current is FISCA.DSAUtil.DSAServerException)
                    return false;
                if (current is WebException || current is IOException || current is TimeoutException || current is TripCancelledException || (current.GetType().FullName == "FISCA.DSAClient.HttpUtil.RetryRequiredException"))
                    transport = true;
                current = current.InnerException;
                depth++;
            }
            return transport;
        }

        public static string WaitSql(string operationId)
        {
            return "SELECT 1 AS waited FROM (SELECT " + SemesterRankPublishSql.AdvisoryLockCall(operationId) + " AS locked) AS publish_lock";
        }

        public static string LookupSql(string operationId)
        {
            string token = SemesterRankPublishSql.OperationAttribute(operationId).Replace("'", "''");
            return "SELECT rank_batch.id::text AS batch_id"
                + ", (SELECT count(*) FROM rank_matrix WHERE rank_matrix.ref_batch_id = rank_batch.id) AS matrix_count"
                + ", (SELECT count(*) FROM rank_matrix WHERE rank_matrix.ref_batch_id = rank_batch.id AND rank_matrix.is_alive = true) AS alive_matrix_count"
                + ", (SELECT count(*) FROM rank_batch_student WHERE rank_batch_student.ref_batch_id = rank_batch.id) AS student_count"
                + ", (SELECT count(*) FROM rank_detail INNER JOIN rank_matrix ON rank_matrix.id = rank_detail.ref_matrix_id WHERE rank_matrix.ref_batch_id = rank_batch.id) AS detail_count"
                + " FROM rank_batch WHERE strpos(rank_batch.setting, '" + token + "') > 0";
        }

        public static SemesterRankBatchObservation ReadObservation(DataTable table)
        {
            SemesterRankBatchObservation observation = new SemesterRankBatchObservation();
            if (table == null)
                return observation;
            observation.BatchCount = table.Rows.Count;
            if (table.Rows.Count != 1)
                return observation;
            DataRow row = table.Rows[0];
            observation.MatrixCount = ReadLong(row, "matrix_count");
            observation.AliveMatrixCount = ReadLong(row, "alive_matrix_count");
            observation.StudentCount = ReadLong(row, "student_count");
            observation.DetailCount = ReadLong(row, "detail_count");
            observation.Complete = observation.MatrixCount > 0
                && observation.AliveMatrixCount == observation.MatrixCount
                && observation.StudentCount > 0
                && observation.DetailCount > 0
                && observation.DetailCount <= int.MaxValue;
            return observation;
        }

        public static DataTable Reconcile(QueryHelper helper, SemesterRankCalculationTrace trace)
        {
            if (helper == null)
                throw new ArgumentNullException("helper");
            if (trace == null)
                throw new ArgumentNullException("trace");
            helper.Select(WaitSql(trace.Id));
            trace.Mark("等待公布鎖");
            DataTable found = helper.Select(LookupSql(trace.Id));
            trace.Mark("核對批次");
            SemesterRankBatchObservation observation = ReadObservation(found);
            trace.ObservedBatchCount = observation.BatchCount;
            trace.ObservedMatrixCount = observation.MatrixCount;
            trace.ObservedAliveMatrixCount = observation.AliveMatrixCount;
            trace.ObservedStudentCount = observation.StudentCount;
            trace.ObservedDetailCount = observation.DetailCount;
            if (!observation.Complete)
                return null;
            DataTable result = new DataTable();
            result.Columns.Add("published_count");
            result.Rows.Add(observation.DetailCount.ToString(CultureInfo.InvariantCulture));
            return result;
        }

        public static string UnconfirmedMessage(SemesterRankCalculationTrace trace)
        {
            int batches = trace == null ? 0 : trace.ObservedBatchCount;
            long matrix = trace == null ? 0 : trace.ObservedMatrixCount;
            long alive = trace == null ? 0 : trace.ObservedAliveMatrixCount;
            long students = trace == null ? 0 : trace.ObservedStudentCount;
            long details = trace == null ? 0 : trace.ObservedDetailCount;
            int writes = trace == null ? 0 : trace.LargeWriteTrips;
            return "結果未確認。公布要求的回應中斷，已用操作識別等待鎖定後核對，沒有重新送出寫入。"
                + " 大型寫入行程=" + writes
                + " 可見批次=" + batches
                + " 矩陣=" + matrix
                + " 有效矩陣=" + alive
                + " 明細=" + details
                + " 學生=" + students
                + "。學生人數相同不足以認定成功。";
        }

        public static void WriteLog(SemesterRankCalculationTrace trace)
        {
            if (trace == null)
                return;
            try
            {
                trace.EndedAt = DateTimeOffset.Now;
                string text = trace.TransportText();
                if (text.Length == 0)
                    return;
                string directory = SemesterRankFailureReport.DefaultDirectory();
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, trace.Id + ".transport.log"), text, Encoding.UTF8);
                string id = trace.Id;
                ThreadPool.QueueUserWorkItem(delegate
                {
                    try
                    {
                        FISCA.LogAgent.ApplicationLog.Log("學期成績排名", "排名公布傳輸", "semester-rank", id, text);
                    }
                    catch (Exception)
                    {
                    }
                });
            }
            catch (Exception)
            {
            }
        }

        private static long RequestBytes(HttpTrip trip)
        {
            if (trip == null || trip.Request == null)
                return 0;
            return trip.TotalBytesToSend;
        }

        private static long ReadLong(DataRow row, string column)
        {
            if (row == null || row.Table == null || !row.Table.Columns.Contains(column))
                return -1;
            long value;
            if (!long.TryParse(Convert.ToString(row[column]), NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                return -1;
            return value;
        }
    }
}
