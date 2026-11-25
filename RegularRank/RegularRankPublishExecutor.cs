using FISCA.Data;
using SHEvaluation.Rank.MotherGroup;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace SHEvaluation.Rank.RegularRank
{
    /// <summary>
    /// Separates SQL composition from execution so local checks can inject a substitute
    /// without pretending a real database commit or lock succeeded.
    /// </summary>
    public interface IRegularRankSqlExecutor
    {
        DataTable Select(string sql);
    }

    public sealed class QueryHelperRegularRankSqlExecutor : IRegularRankSqlExecutor
    {
        private readonly QueryHelper _helper;

        public QueryHelperRegularRankSqlExecutor(QueryHelper helper)
        {
            if (helper == null) throw new ArgumentNullException("helper");
            _helper = helper;
        }

        public DataTable Select(string sql)
        {
            return _helper.Select(sql);
        }
    }

    /// <summary>
    /// Local substitute. Records SQL and returns programmed tables; never claims
    /// PostgreSQL rollback or advisory-lock semantics were observed remotely.
    /// </summary>
    public sealed class SubstituteRegularRankSqlExecutor : IRegularRankSqlExecutor
    {
        private readonly List<string> _executed = new List<string>();
        private readonly Func<string, int, DataTable> _handler;

        public SubstituteRegularRankSqlExecutor(Func<string, int, DataTable> handler)
        {
            if (handler == null) throw new ArgumentNullException("handler");
            _handler = handler;
        }

        public IList<string> ExecutedSql
        {
            get { return _executed.AsReadOnly(); }
        }

        public DataTable Select(string sql)
        {
            _executed.Add(sql ?? "");
            return _handler(sql, _executed.Count);
        }
    }

    public static class RegularRankPublishPipeline
    {
        public static RegularRankDatabaseEvidence VerifyDatabase(IRegularRankSqlExecutor executor)
        {
            if (executor == null) throw new ArgumentNullException("executor");
            RegularRankDatabaseEvidence evidence;
            try { evidence = RegularRankDatabaseContract.Verify(executor.Select(RegularRankDatabaseContract.ProbeSql)); }
            catch (Exception ex) { throw new InvalidOperationException("排名結構欄位查詢／核對失敗。", ex); }
            if (evidence.DatabaseName.IndexOf("debug6", StringComparison.OrdinalIgnoreCase) < 0)
                throw new InvalidOperationException(RegularRankCalculationEntry.NonDebugDatabaseMessage);
            StringBuilder metadata = new StringBuilder(evidence.Report);
            try
            {
                foreach (DataRow row in executor.Select(RegularRankDatabaseContract.ConstraintsSql).Rows)
                    metadata.AppendLine(row["kind"] + ": " + row["relation"] + "." + row["name"] + " " + row["definition"]);
            }
            catch (Exception ex) { throw new InvalidOperationException("排名限制與索引查詢失敗。", ex); }
            evidence.Report = metadata.ToString();

            return evidence;
        }

        public static string ComposePublishSql(RegularRankConfirmRequest request, MotherGroupConfirmList population,
            RegularRankDatabaseEvidence evidence, string operationId, bool injectFailure)
        {
            if (evidence == null)
                throw new InvalidOperationException(RegularRankCalculationEntry.BlockedMessage);
            string sql = RegularRankAtomicPublishSql.Build(request, population, operationId, injectFailure);
            if (Encoding.UTF8.GetByteCount(sql) > 7 * 1024 * 1024)
                throw new InvalidOperationException("整次排名 SQL 超過已知 7MB 驗證範圍，停止發布；不得改成逐年級提交。");
            return sql;
        }

        public static DataTable ExecutePublish(IRegularRankSqlExecutor executor, string sql,
            Func<DataTable> reconcile, bool injectFailure)
        {
            if (executor == null) throw new ArgumentNullException("executor");
            Exception publishError = null;
            try
            {
                DataTable result = executor.Select(sql);
                if (RegularRankPublicationService.IsPublishedResult(result))
                    return result;
            }
            catch (Exception error)
            {
                if (injectFailure) throw;
                publishError = error;
            }

            if (reconcile != null && !injectFailure)
            {
                try
                {
                    DataTable observed = reconcile();
                    if (RegularRankPublicationService.IsPublishedResult(observed))
                        return observed;
                }
                catch (Exception error)
                {
                    publishError = publishError == null ? error : new AggregateException(publishError, error);
                }
            }
            throw new InvalidOperationException("發布結果尚未確認；請依操作識別核對批次，不可直接重送。", publishError);
        }
    }
}
