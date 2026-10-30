using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SHEvaluation.Rank.MotherGroup
{
    public static class MotherGroupPhase4Task41Check
    {
        public static int Main(string[] args)
        {
            try
            {
                List<string> errors = new List<string>();
                errors.AddRange(CheckSql());
                errors.AddRange(CheckDecisions());
                errors.AddRange(CheckPublishScope());
                if (!SemesterRankWriteGate.CanWriteRankTables())
                    errors.Add("SemesterRankWriteGate 應允許寫入");
                if (SemesterRankCatalog.DatabaseExecuted)
                    errors.Add("目錄查詢不應標記為已在資料庫執行");
                string gateSource = File.ReadAllText("MotherGroup\\SemesterRankWriteGate.cs", Encoding.UTF8);
                Expect(errors, gateSource.IndexOf("return true;") >= 0 && gateSource.IndexOf("return false;") < 0, "SemesterRankWriteGate 應回傳 true");
                if (errors.Count == 0)
                {
                    Console.WriteLine("phase4-task41-check: ok");
                    Console.WriteLine("database-executed: false");
                    return 0;
                }
                foreach (string error in errors)
                    Console.WriteLine(error);
                return 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return 1;
            }
        }

        private static List<string> CheckSql()
        {
            List<string> errors = new List<string>();
            string sql = SemesterRankCatalog.ReadOnlySql;
            Expect(errors, !ContainsWrite(sql), "目錄 SQL 不應含寫入或建立物件的關鍵字");
            Expect(errors, sql.IndexOf("FROM rank_") < 0 && sql.IndexOf("JOIN rank_") < 0, "目錄 SQL 不應讀取排名資料列");
            foreach (string tableName in SemesterRankCatalog.TableNames)
                Expect(errors, sql.IndexOf("'" + tableName + "'") >= 0, "目錄 SQL 應包含 " + tableName);
            Expect(errors, sql.IndexOf("pg_constraint") >= 0 && sql.IndexOf("pg_index") >= 0 && sql.IndexOf("pg_attribute") >= 0, "目錄 SQL 應查詢欄位、限制與索引");
            Expect(errors, sql.IndexOf("'primary key'") >= 0 && sql.IndexOf("'unique'") >= 0 && sql.IndexOf("'foreign key'") >= 0 && sql.IndexOf("'check'") >= 0, "目錄 SQL 應區分主鍵、唯一限制、外部鍵與 CHECK");
            Expect(errors, sql.IndexOf("is_alive") >= 0 && sql.IndexOf("indisunique") >= 0 && sql.IndexOf("indpred") >= 0, "目錄 SQL 應包含 is_alive、唯一索引與部分索引條件");
            Expect(errors, sql.IndexOf("generate_subscripts") < 0 && sql.IndexOf("indnkeyatts") < 0 && sql.IndexOf("indnullsnotdistinct") < 0, "目錄 SQL 不應使用 PostgreSQL 9.6 沒有或不需要的索引欄位");
            Expect(errors, sql.IndexOf("WITH ORDINALITY") >= 0 && sql.IndexOf("pg_get_indexdef") >= 0 && sql.IndexOf("pg_get_constraintdef") >= 0, "欄位順序與限制定義應使用 9.6 已有的函式");
            Expect(errors, sql.IndexOf("XMLTABLE") < 0 && sql.IndexOf("MERGE") < 0 && sql.IndexOf("ON CONFLICT") < 0, "目錄 SQL 不應使用 PostgreSQL 9.6 沒有的語法");
            return errors;
        }

        private static List<string> CheckDecisions()
        {
            List<string> errors = new List<string>();
            CatalogAssessment empty = SemesterRankCatalog.Assess(new List<CatalogFact>());
            Expect(errors, empty.Verdict == "not-verified" && empty.DatabaseExecuted == false, "沒有目錄列時不得認定新舊列可以並存");

            CatalogAssessment coexist = SemesterRankCatalog.Assess(BaseFacts());
            Expect(errors, coexist.Verdict == "coexist" && coexist.DatabaseExecuted == false, "只有主鍵時，傳入列應解釋為資料庫定義不禁止並存，同時標記尚未執行");

            List<CatalogFact> business = BaseFacts();
            business.Add(Unique("rank_matrix", "rank_matrix_business_key", "school_year,semester,grade_year,item_type,ref_exam_id,item_name,rank_type,rank_name", false, ""));
            CatalogAssessment businessResult = SemesterRankCatalog.Assess(business);
            string businessText = Join(businessResult.Reasons);
            Expect(errors, businessResult.Verdict == "not-verified" && businessText.IndexOf("實際插入鍵") >= 0 && businessText.IndexOf("必然") >= 0, "業務鍵唯一限制不得只靠欄位名稱認定插入失敗");

            List<CatalogFact> withAlive = BaseFacts();
            withAlive.Add(Unique("rank_matrix", "rank_matrix_alive_key", "school_year,semester,grade_year,item_type,ref_exam_id,item_name,rank_type,rank_name,is_alive", false, ""));
            Expect(errors, SemesterRankCatalog.Assess(withAlive).Verdict == "coexist", "唯一鍵含 is_alive 時，NULL 與 true 不是同一個鍵");

            List<CatalogFact> partial = BaseFacts();
            partial.Add(Unique("rank_matrix", "rank_matrix_alive_partial", "school_year,semester,grade_year,item_type,ref_exam_id,item_name,rank_type,rank_name", true, "CREATE UNIQUE INDEX rank_matrix_alive_partial ON rank_matrix (school_year, semester) WHERE is_alive = true"));
            Expect(errors, SemesterRankCatalog.Assess(partial).Verdict == "coexist", "只收錄 is_alive = true 的部分唯一索引應允許舊的 NULL 列留下");

            List<CatalogFact> opaque = BaseFacts();
            opaque.Add(Unique("rank_matrix", "rank_matrix_partial_unknown", "school_year,semester,item_name", true, "CREATE UNIQUE INDEX rank_matrix_partial_unknown ON rank_matrix (school_year, item_name) WHERE school_year > 0"));
            Expect(errors, SemesterRankCatalog.Assess(opaque).Verdict == "not-verified", "看不懂的部分唯一索引不得當成可以並存");

            List<CatalogFact> notNull = BaseFacts();
            notNull.Add(Column("rank_matrix", "is_alive", true));
            Expect(errors, SemesterRankCatalog.Assess(notNull).Verdict == "stop", "is_alive 不允許 NULL 時，現有停用語句無法執行");

            List<CatalogFact> checkNotNull = BaseFacts();
            checkNotNull.Add(Check("rank_matrix", "rank_matrix_alive_not_null", "CHECK ((is_alive IS NOT NULL))"));
            Expect(errors, SemesterRankCatalog.Assess(checkNotNull).Verdict == "stop", "CHECK 不允許 is_alive 為 NULL 時應停止");

            List<CatalogFact> checkBool = BaseFacts();
            checkBool.Add(Check("rank_matrix", "rank_matrix_alive_bool", "CHECK ((is_alive = true) OR (is_alive = false))"));
            Expect(errors, SemesterRankCatalog.Assess(checkBool).Verdict == "coexist", "一般 CHECK 對 NULL 視為通過，不因此停止");

            List<CatalogFact> batch = BaseFacts();
            batch.Add(Unique("rank_batch", "rank_batch_period_key", "school_year,semester", false, ""));
            CatalogAssessment batchResult = SemesterRankCatalog.Assess(batch);
            Expect(errors, batchResult.Verdict == "not-verified" && Join(batchResult.Reasons).IndexOf("已經有批次") >= 0, "學年度加學期的唯一限制不得直接認定再次插入失敗");

            List<CatalogFact> students = BaseFacts();
            students.Add(Unique("rank_batch_student", "rank_batch_student_student_key", "ref_batch_id,ref_student_id", false, ""));
            CatalogAssessment studentResult = SemesterRankCatalog.Assess(students);
            string studentText = Join(studentResult.Reasons);
            Expect(errors, studentResult.Verdict == "not-verified" && studentText.IndexOf("一批次一學生") >= 0 && studentText.IndexOf("只有一列時不會") >= 0, "一批次一學生要看實際成績列數，不能直接停止");

            List<CatalogFact> studentWithId = BaseFacts();
            studentWithId.Add(Unique("rank_batch_student", "rank_batch_student_with_id", "ref_batch_id,ref_student_id,id", false, ""));
            Expect(errors, SemesterRankCatalog.Assess(studentWithId).Verdict == "coexist", "唯一鍵含 id 時，每一筆新列都有不同識別");

            List<CatalogFact> details = BaseFacts();
            details.Add(Unique("rank_detail", "rank_detail_student_key", "ref_matrix_id,ref_student_id", false, ""));
            CatalogAssessment detailResult = SemesterRankCatalog.Assess(details);
            string detailText = Join(detailResult.Reasons);
            Expect(errors, detailResult.Verdict == "not-verified" && detailText.IndexOf("不同科目") >= 0 && detailText.IndexOf("重複展開") >= 0, "重複科目要看同一矩陣是否被展開多次");

            List<CatalogFact> detailStudentOnly = BaseFacts();
            detailStudentOnly.Add(Unique("rank_detail", "rank_detail_student_only", "ref_student_id", false, ""));
            CatalogAssessment detailStudentResult = SemesterRankCatalog.Assess(detailStudentOnly);
            Expect(errors, detailStudentResult.Verdict == "not-verified" && Join(detailStudentResult.Reasons).IndexOf("不能因此認定插入必然失敗") >= 0, "只有學生識別的明細唯一限制仍要看實際列數");

            List<CatalogFact> detailWithScore = BaseFacts();
            detailWithScore.Add(Unique("rank_detail", "rank_detail_score_key", "ref_matrix_id,ref_student_id,score", false, ""));
            Expect(errors, SemesterRankCatalog.Assess(detailWithScore).Verdict == "not-verified", "相同科目但分數不同時，含分數的唯一鍵不一定衝突");

            List<CatalogFact> byBatch = BaseFacts();
            byBatch.Add(Unique("rank_matrix", "rank_matrix_batch_key", "ref_batch_id,item_type,item_name,rank_type,rank_name", false, ""));
            CatalogAssessment batchKeyResult = SemesterRankCatalog.Assess(byBatch);
            Expect(errors, batchKeyResult.Verdict == "not-verified" && Join(batchKeyResult.Reasons).IndexOf("實際批次識別") >= 0, "ref_batch_id 要等實際批次識別才能判斷是否與舊列不同");

            List<CatalogFact> nonUniqueAlive = BaseFacts();
            nonUniqueAlive.Add(new CatalogFact
            {
                Section = "index",
                TableName = "rank_matrix",
                SchemaName = "public",
                ObjectKind = "index",
                ObjectName = "rank_matrix_alive_lookup",
                ColumnName = "is_alive",
                Definition = "CREATE INDEX rank_matrix_alive_lookup ON rank_matrix (is_alive)",
                MentionsIsAlive = true
            });
            Expect(errors, SemesterRankCatalog.Assess(nonUniqueAlive).Verdict == "coexist", "非唯一的 is_alive 索引不構成並存衝突");

            List<CatalogFact> missing = BaseFacts();
            missing.Add(new CatalogFact { TableName = "rank_matrix", SchemaName = "archive", ObjectKind = "table" });
            Expect(errors, SemesterRankCatalog.Assess(missing).Verdict == "not-verified", "同名表出現在多個 schema 時不得選定其中一個");
            return errors;
        }

        private static List<string> CheckPublishScope()
        {
            List<string> errors = new List<string>();
            string path = "CalculateSemesterAssessmentRank.cs";
            if (!File.Exists(path))
            {
                errors.Add("找不到排名表單：" + path);
                return errors;
            }
            string source = File.ReadAllText(path, Encoding.UTF8);
            PublishScopeAssessment scope = SemesterRankCatalog.InspectPublishScope(source);
            Expect(errors, !scope.SeparateBatchInsert && !scope.GradeLoopUsesSeparateSelect && !scope.BatchIdStartsAtZero, "公布不應再先插入批次、依年級各執行一次，或使用批次識別 0");
            Expect(errors, scope.UpdateOmitsItemType, "表單範本的停用條件仍由公布組譯器補上 item_type");
            Expect(errors, scope.NoBeginOrCommit && scope.AllGradesInOneStatement && scope.DatabaseExecuted == false, "單一語句尚未對資料庫執行，也沒有 BEGIN/COMMIT");
            Expect(errors, source.IndexOf("SemesterRankCatalog") < 0, "Task 4.1 不應把目錄查詢接進計算按鈕");
            Expect(errors, source.IndexOf("GREATEST") >= 0 && source.IndexOf("is_alive = NULL") >= 0, "這次不應改動排名公式或原有停用語句");
            int gate = source.IndexOf("CanWriteRankTables");
            int select = source.IndexOf("queryHelper.Select(insertRankSql)");
            Expect(errors, gate >= 0 && select > gate, "寫入閘門仍位於排名 SQL 執行之前");
            return errors;
        }

        private static List<CatalogFact> BaseFacts()
        {
            List<CatalogFact> facts = new List<CatalogFact>();
            foreach (string tableName in SemesterRankCatalog.TableNames)
            {
                facts.Add(new CatalogFact
                {
                    Section = "table",
                    TableName = tableName,
                    SchemaName = "public",
                    ObjectKind = "table"
                });
                facts.Add(Column(tableName, "id", true));
                facts.Add(Unique(tableName, tableName + "_pkey", "id", false, "PRIMARY KEY (id)"));
            }
            facts.Add(Column("rank_matrix", "is_alive", false));
            facts.Add(Column("rank_matrix", "school_year", true));
            facts.Add(Column("rank_matrix", "rank_name", false));
            return facts;
        }

        private static CatalogFact Column(string tableName, string columnName, bool notNull)
        {
            return new CatalogFact
            {
                Section = "column",
                TableName = tableName,
                SchemaName = "public",
                ObjectKind = "column",
                ObjectName = columnName,
                ColumnName = columnName,
                ColumnNotNull = notNull,
                MentionsIsAlive = columnName == "is_alive"
            };
        }

        private static CatalogFact Unique(string tableName, string objectName, string columns, bool partial, string definition)
        {
            return new CatalogFact
            {
                Section = "index",
                TableName = tableName,
                SchemaName = "public",
                ObjectKind = partial ? "unique index" : "unique",
                ObjectName = objectName,
                ColumnName = columns,
                Definition = definition,
                IsUnique = true,
                IsPartial = partial,
                MentionsIsAlive = (columns ?? "").IndexOf("is_alive") >= 0 || (definition ?? "").IndexOf("is_alive") >= 0
            };
        }

        private static CatalogFact Check(string tableName, string objectName, string definition)
        {
            return new CatalogFact
            {
                Section = "constraint",
                TableName = tableName,
                SchemaName = "public",
                ObjectKind = "check",
                ObjectName = objectName,
                Definition = definition,
                MentionsIsAlive = true
            };
        }

        private static bool ContainsWrite(string sql)
        {
            string[] forbidden = new string[]
            {
                "INSERT", "UPDATE", "DELETE", "TRUNCATE", "MERGE", "INTO", "RETURNING", "CALL", "COPY", "ALTER", "DROP", "CREATE"
            };
            foreach (string keyword in forbidden)
            {
                if (ContainsKeyword(sql, keyword))
                    return true;
            }
            return false;
        }

        private static bool ContainsKeyword(string sql, string keyword)
        {
            int at = 0;
            while (at <= sql.Length - keyword.Length)
            {
                at = sql.IndexOf(keyword, at, StringComparison.OrdinalIgnoreCase);
                if (at < 0)
                    return false;
                bool left = at == 0 || !IsIdent(sql[at - 1]);
                int end = at + keyword.Length;
                bool right = end >= sql.Length || !IsIdent(sql[end]);
                if (left && right)
                    return true;
                at = end;
            }
            return false;
        }

        private static bool IsIdent(char value)
        {
            return char.IsLetterOrDigit(value) || value == '_';
        }

        private static string Join(List<string> reasons)
        {
            StringBuilder text = new StringBuilder();
            foreach (string reason in reasons)
                text.Append(reason).Append('\n');
            return text.ToString();
        }

        private static void Expect(List<string> errors, bool condition, string message)
        {
            if (!condition)
                errors.Add(message);
        }
    }
}
