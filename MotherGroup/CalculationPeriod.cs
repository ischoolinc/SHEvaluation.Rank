namespace SHEvaluation.Rank.MotherGroup
{
    public sealed class CalculationPeriod
    {
        public const string CurrentSourceName = "依目前學生資料產生";
        public const string HistorySourceName = "歷史學期成績排名";
        public const string ExcelSourceName = "Excel 母群清單";

        public string SchoolYear { get; private set; }
        public string Semester { get; private set; }
        public string SourceName { get; private set; }
        public string HistorySchoolYear { get; private set; }
        public string HistorySemester { get; private set; }
        public string HistoryBatchId { get; private set; }

        public static CalculationPeriod CreateDefault(string systemSchoolYear, string systemSemester)
        {
            CalculationPeriod period = new CalculationPeriod();
            period.SchoolYear = systemSchoolYear ?? "";
            period.Semester = systemSemester == "2" ? "2" : "1";
            period.SourceName = CurrentSourceName;
            period.HistorySchoolYear = "";
            period.HistorySemester = "";
            period.HistoryBatchId = "";
            return period;
        }

        public void SetCalculationPeriod(string schoolYear, string semester)
        {
            SchoolYear = schoolYear ?? "";
            Semester = semester == "2" ? "2" : "1";
        }

        public void SetHistoryPeriod(string schoolYear, string semester)
        {
            HistorySchoolYear = schoolYear ?? "";
            HistorySemester = semester ?? "";
        }

        public void RememberHistoryBatch(string batchId, string schoolYear, string semester)
        {
            HistoryBatchId = batchId ?? "";
            HistorySchoolYear = schoolYear ?? "";
            HistorySemester = semester ?? "";
        }

        public void SelectHistoryBatch(string batchId)
        {
            HistoryBatchId = batchId ?? "";
        }

        public void ClearHistoryBatchSelection()
        {
            HistoryBatchId = "";
        }

        public void ClearHistoryBatch()
        {
            HistoryBatchId = "";
            HistorySchoolYear = "";
            HistorySemester = "";
        }
    }
}
