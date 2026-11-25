using FISCA.Data;
using SHEvaluation.Rank.MotherGroup;
using System;
using System.Data;
using System.IO;
using System.Text;

namespace SHEvaluation.Rank.RegularRank
{
    public static class RegularRankPublicationService
    {
        public static string ValidationFailure(Exception error)
        {
            SemesterRankFailureReport report = SemesterRankFailureReport.Create(null, error, false);
            string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ischool", "RankVerification");
            report.WriteFile(directory);
            return report.UserMessage;
        }

        public static RegularRankDatabaseEvidence VerifyTestDatabase(QueryHelper helper)
        {
            return VerifyTestDatabase(new QueryHelperRegularRankSqlExecutor(helper));
        }

        public static RegularRankDatabaseEvidence VerifyTestDatabase(IRegularRankSqlExecutor executor)
        {
            if (executor == null) throw new ArgumentNullException("executor");
            string evidenceDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ischool", "RankVerification");
            Directory.CreateDirectory(evidenceDirectory);
            try
            {
                RegularRankDatabaseEvidence evidence = RegularRankPublishPipeline.VerifyDatabase(executor);
                File.WriteAllText(Path.Combine(evidenceDirectory, "regular-rank-contract.txt"), evidence.Report, Encoding.UTF8);
                RegularRankCalculationEntry.SetTestEvidence(evidence);
                return evidence;
            }
            catch (Exception error)
            {
                RegularRankCalculationEntry.ClearEvidence();
                WriteDatabaseCheckFailure(evidenceDirectory, error);
                throw;
            }
        }

        private static void WriteDatabaseCheckFailure(string directory, Exception error)
        {
            File.AppendAllText(Path.Combine(directory, "regular-rank-transport-probes.txt"),
                DateTimeOffset.Now.ToString("o") + " probe=database-structure\n"
                + SemesterRankFailureReport.ExceptionChain(error) + "\n\n", Encoding.UTF8);
        }

        public static DataTable PublishForVerification(RegularRankConfirmRequest request,
            MotherGroupConfirmList population, bool injectFailure, out string operationId)
        {
            return PublishForVerification(request, population, injectFailure, null, out operationId);
        }

        public static DataTable PublishForVerification(RegularRankConfirmRequest request,
            MotherGroupConfirmList population, bool injectFailure, IRegularRankSqlExecutor injected, out string operationId)
        {
            SemesterRankCalculationTrace trace = SemesterRankCalculationTrace.Start(request.TargetSchoolYear,
                request.TargetSemester, "定期評量/" + RegularRankPeriodSourceEditor.SourceText(request.SourceKind), request.SourceBatchId);
            operationId = trace.Id;
            QueryHelper writer = null;
            IRegularRankSqlExecutor executor = injected;
            if (executor == null)
            {
                writer = SemesterRankPublishTransport.OpenWriter(trace);
                executor = new QueryHelperRegularRankSqlExecutor(writer);
            }
            try
            {
                RegularRankDatabaseEvidence evidence = VerifyTestDatabase(executor);
                string sql = RegularRankPublishPipeline.ComposePublishSql(request, population, evidence, trace.Id, injectFailure);
                trace.SqlBytes = Encoding.UTF8.GetByteCount(sql);
                trace.Mark("排名語句");
                DataTable result = RegularRankPublishPipeline.ExecutePublish(executor, sql,
                    writer == null ? (Func<DataTable>)null : delegate { return Reconcile(writer, trace); },
                    injectFailure);
                trace.Mark("資料庫查詢");
                trace.TransportResult = "成功";
                return result;
            }
            catch (Exception error)
            {
                trace.TransportResult = "失敗或未確認";
                if (writer != null)
                    SemesterRankFailureReport.Create(trace, error, true).WriteFile(SemesterRankFailureReport.DefaultDirectory());
                throw;
            }
            finally
            {
                if (writer != null) SemesterRankPublishTransport.WriteLog(trace);
            }
        }

        private static DataTable Reconcile(QueryHelper writer, SemesterRankCalculationTrace trace)
        {
            writer.Select(SemesterRankPublishTransport.WaitSql(trace.Id));
            trace.Mark("等待公布鎖");
            DataTable found = writer.Select(SemesterRankPublishTransport.LookupSql(trace.Id));
            trace.Mark("核對批次");
            SemesterRankBatchObservation observation = SemesterRankPublishTransport.ReadObservation(found);
            trace.ObservedBatchCount = observation.BatchCount;
            trace.ObservedMatrixCount = observation.MatrixCount;
            trace.ObservedAliveMatrixCount = observation.AliveMatrixCount;
            trace.ObservedStudentCount = observation.StudentCount;
            trace.ObservedDetailCount = observation.DetailCount;
            return observation.Complete ? found : null;
        }

        public static bool IsPublishedResult(DataTable table)
        {
            long batch, detail, matrix;
            return table != null && table.Rows.Count == 1 && table.Columns.Contains("batch_id")
                && table.Columns.Contains("detail_count") && table.Columns.Contains("matrix_count")
                && long.TryParse(Convert.ToString(table.Rows[0]["batch_id"]), out batch) && batch > 0
                && long.TryParse(Convert.ToString(table.Rows[0]["detail_count"]), out detail) && detail > 0
                && long.TryParse(Convert.ToString(table.Rows[0]["matrix_count"]), out matrix) && matrix > 0;
        }
    }
}
