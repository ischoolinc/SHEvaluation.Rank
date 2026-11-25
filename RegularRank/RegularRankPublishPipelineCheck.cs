using SHEvaluation.Rank.MotherGroup;
using System;
using System.Collections.Generic;
using System.Data;

namespace SHEvaluation.Rank.RegularRank
{
    public static class RegularRankPublishPipelineCheck
    {
        public static int Main(string[] args)
        {
            List<string> errors = new List<string>();
            try
            {
                RegularRankCalculationEntry.ClearEvidence();
                errors.AddRange(CheckPlanAndGuard());
                errors.AddRange(CheckSubstitutePipeline());
                errors.AddRange(CheckDatabaseFailureStops());
                errors.AddRange(CheckMissingResponseReconciles());
            }
            catch (Exception ex)
            {
                errors.Add(ex.ToString());
            }
            finally
            {
                RegularRankCalculationEntry.ClearEvidence();
            }
            if (errors.Count == 0)
            {
                Console.WriteLine("RegularRankPublishPipelineCheck: ok");
                return 0;
            }
            Console.WriteLine("RegularRankPublishPipelineCheck: FAIL");
            foreach (string error in errors) Console.WriteLine(" - " + error);
            return 1;
        }

        private static List<string> CheckPlanAndGuard()
        {
            List<string> errors = new List<string>();
            RegularRankPublishPlan ok = RegularRankPublishPlan.Create(new RegularRankGradeOutcome[]
            {
                new RegularRankGradeOutcome { GradeYear = 1, StudentCount = 10, HasNewDetail = true }
            }, true);
            Expect(errors, ok.ExecutionAllowed && ok.SingleStatement && !ok.PerGradeCommit && ok.BlockReason.Length == 0, "有效計畫允許執行且禁止逐年級提交");

            RegularRankPublishPlan zero = RegularRankPublishPlan.Create(new RegularRankGradeOutcome[]
            {
                new RegularRankGradeOutcome { GradeYear = 1, StudentCount = 10, HasNewDetail = false }
            }, false);
            Expect(errors, !zero.ExecutionAllowed && zero.WholeBatchFails, "零明細年級整次失敗");

            RegularRankCalculationGuard guard = new RegularRankCalculationGuard();
            Expect(errors, guard.TryEnter() && guard.Running && !guard.EditorsEnabled, "清單就緒時可進入防重入");
            Expect(errors, !guard.TryEnter(), "進行中不可重入");
            guard.FailAndRestore();
            Expect(errors, !guard.Running && guard.EditorsEnabled, "失敗後可恢復編輯");
            Expect(errors, RegularRankCalculationEntry.CanOfferCalculate(1) && !RegularRankCalculationEntry.CanOfferCalculate(0), "計算按鈕依清單人數");
            Expect(errors, !RegularRankCalculationEntry.ExecutionAllowed, "未通過結構檢查前 ExecutionAllowed 仍為 false");
            Expect(errors, RegularRankCalculationEntry.BlockedMessage.Contains("資料庫結構"), "阻擋訊息指出結構檢查");
            return errors;
        }

        private static List<string> CheckSubstitutePipeline()
        {
            List<string> errors = new List<string>();
            RegularRankConfirmRequest request = SampleRequest();
            MotherGroupConfirmList list = SampleList();
            SubstituteRegularRankSqlExecutor executor = new SubstituteRegularRankSqlExecutor(delegate(string sql, int index)
            {
                if (sql.Contains("pg_attribute"))
                    return StructureTable("1admin_debug6");
                if (sql.Contains("pg_constraint") || sql.Contains("pg_index"))
                    return ConstraintsTable();
                if (sql.StartsWith("WITH regular_lock AS", StringComparison.Ordinal))
                    return PublishedTable(501, 12, 4);
                throw new InvalidOperationException("未預期 SQL：" + (sql.Length > 80 ? sql.Substring(0, 80) : sql));
            });

            string operationId;
            DataTable published = RegularRankPublicationService.PublishForVerification(request, list, false, executor, out operationId);
            Expect(errors, RegularRankPublicationService.IsPublishedResult(published), "替代執行器可完成發布呼叫鏈");
            Expect(errors, RegularRankCalculationEntry.ExecutionAllowed, "結構檢查後 ExecutionAllowed 為 true");
            Expect(errors, !string.IsNullOrEmpty(operationId) && executor.ExecutedSql.Count == 3
                && executor.ExecutedSql[2].Contains(operationId), "只執行兩次結構查詢及一次含操作識別的 WITH 發布");
            return errors;
        }

        private static List<string> CheckDatabaseFailureStops()
        {
            List<string> errors = new List<string>();
            RegularRankCalculationEntry.ClearEvidence();
            SubstituteRegularRankSqlExecutor failing = new SubstituteRegularRankSqlExecutor(delegate(string sql, int index)
            {
                if (sql.Contains("pg_attribute"))
                    return StructureTable("1admin_debug6");
                throw new InvalidOperationException("DSA 504 simulated");
            });
            bool stopped = false;
            try
            {
                string operationId;
                RegularRankPublicationService.PublishForVerification(SampleRequest(), SampleList(), false, failing, out operationId);
            }
            catch (InvalidOperationException error)
            {
                stopped = true;
                Expect(errors, error.Message.Contains("排名限制與索引"), "保留結構查詢的失敗原因");
            }
            Expect(errors, stopped && !RegularRankCalculationEntry.ExecutionAllowed, "結構查詢失敗停止發布");
            Expect(errors, failing.ExecutedSql.Count == 2, "結構檢查失敗後未送出排名發布 SQL");
            return errors;
        }

        private static List<string> CheckMissingResponseReconciles()
        {
            List<string> errors = new List<string>();
            var executor = new SubstituteRegularRankSqlExecutor(
                delegate { throw new InvalidOperationException("DSA 504 simulated"); });
            int lookups = 0;
            DataTable result = RegularRankPublishPipeline.ExecutePublish(executor, "WITH test AS (SELECT 1) SELECT * FROM test",
                delegate { lookups++; return PublishedTable(501, 12, 4); }, false);
            Expect(errors, RegularRankPublicationService.IsPublishedResult(result)
                && lookups == 1 && executor.ExecutedSql.Count == 1, "發布回應失敗時只核對一次，不重送寫入");
            return errors;
        }

        private static RegularRankConfirmRequest SampleRequest()
        {
            RegularRankCalculationContext context = RegularRankCalculationContext.CreateDefault("115", "1");
            context.SetTargetPeriod("114", "2");
            context.SetCurrentSource();
            context.SetTargetExam(RegularRankExamResolution.Resolve("5", "", new RegularRankExamChoice[]
            {
                new RegularRankExamChoice { Id = "5", Name = "期末考" }
            }));
            context.SetGradeYears(new int[] { 1 });
            context.MarkMotherGroupLoaded();
            context.ApplySubjectCandidates(new string[] { "國文" }, new string[] { "國文" }, new string[] { "國文" }, new string[0], new string[0], new string[0]);
            context.ConfirmSubjects();
            return context.CreateRequest();
        }

        private static MotherGroupConfirmList SampleList()
        {
            MotherGroupConfirmList list = new MotherGroupConfirmList();
            StudentMotherGroup group = new StudentMotherGroup();
            group.StudentId = "15";
            group.StudentNumber = "0015";
            group.StudentName = "測";
            group.Status = 1;
            group.SetSourceSlots(new RankSlot[]
            {
                new RankSlot { RankType = RankKinds.Year, GroupName = "1年級", Participate = true },
                new RankSlot { RankType = RankKinds.Department, GroupName = "普通科", Participate = true },
                new RankSlot { RankType = RankKinds.Class, GroupName = "101", Participate = true },
                new RankSlot { RankType = RankKinds.Tag1, GroupName = "", Participate = false },
                new RankSlot { RankType = RankKinds.Tag2, GroupName = "", Participate = false }
            });
            list.Load(new ConfirmStudentRow[] { new ConfirmStudentRow { Group = group } });
            return list;
        }

        private static DataTable StructureTable(string database)
        {
            DataTable table = new DataTable();
            table.Columns.Add("database_name");
            table.Columns.Add("server_version");
            table.Columns.Add("table_name");
            table.Columns.Add("column_name");
            table.Columns.Add("data_type");
            table.Columns.Add("is_nullable");
            string[][] columns = new string[][]
            {
                new string[] { "rank_batch", "id" }, new string[] { "rank_batch", "school_year" }, new string[] { "rank_batch", "semester" },
                new string[] { "rank_batch", "setting" }, new string[] { "rank_batch", "extension" }, new string[] { "rank_batch", "calculation_description" },
                new string[] { "rank_batch_student", "id" }, new string[] { "rank_batch_student", "ref_batch_id" }, new string[] { "rank_batch_student", "ref_student_id" },
                new string[] { "rank_batch_student", "grade_year" }, new string[] { "rank_batch_student", "matrix_grade" }, new string[] { "rank_batch_student", "matrix_dept" },
                new string[] { "rank_batch_student", "matrix_class" }, new string[] { "rank_batch_student", "matrix_tag1" }, new string[] { "rank_batch_student", "matrix_tag2" },
                new string[] { "rank_matrix", "id" }, new string[] { "rank_matrix", "ref_batch_id" }, new string[] { "rank_matrix", "school_year" },
                new string[] { "rank_matrix", "semester" }, new string[] { "rank_matrix", "grade_year" }, new string[] { "rank_matrix", "item_type" },
                new string[] { "rank_matrix", "ref_exam_id" }, new string[] { "rank_matrix", "item_name" }, new string[] { "rank_matrix", "rank_type" },
                new string[] { "rank_matrix", "rank_name" }, new string[] { "rank_matrix", "is_alive" }, new string[] { "rank_matrix", "matrix_count" },
                new string[] { "rank_detail", "ref_matrix_id" }, new string[] { "rank_detail", "ref_student_id" }, new string[] { "rank_detail", "score" },
                new string[] { "rank_detail", "rank" }, new string[] { "rank_detail", "pr" }, new string[] { "rank_detail", "percentile" }
            };
            foreach (string[] column in columns)
                table.Rows.Add(database, "170011", column[0], column[1], "text", "YES");
            return table;
        }

        private static DataTable ConstraintsTable()
        {
            DataTable table = new DataTable();
            table.Columns.Add("kind");
            table.Columns.Add("relation");
            table.Columns.Add("name");
            table.Columns.Add("definition");
            table.Rows.Add("constraint", "rank_matrix", "rank_matrix_pkey", "PRIMARY KEY (id)");
            return table;
        }

        private static DataTable PublishedTable(long batch, long detail, long matrix)
        {
            DataTable table = new DataTable();
            table.Columns.Add("batch_id");
            table.Columns.Add("detail_count");
            table.Columns.Add("matrix_count");
            table.Rows.Add(batch, detail, matrix);
            return table;
        }

        private static void Expect(List<string> errors, bool condition, string message)
        {
            if (!condition) errors.Add(message);
        }
    }
}
