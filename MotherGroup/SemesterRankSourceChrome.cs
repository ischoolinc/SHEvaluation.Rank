namespace SHEvaluation.Rank.MotherGroup
{
    public static class SemesterRankSourceChrome
    {
        public const string CalcPeriodHint = "此期間用於讀取學期成績與產生排名。";
        public const string CurrentGradeHint = "依學生目前班級的年級篩選";
        public const string HistoryGradeHint = "依歷史母群的「年排名母群」判定年級，再套用勾選範圍";
        public const string ExcelGradeHint = "依 Excel 的「年排名母群」判定年級，再套用勾選範圍";
        public const string ExcludedCategoryHint = "符合這個類別的學生，五項排名預設不參與，可在確認頁調整";
        public const string ExcludedRowHint = "符合不排名學生類別，預設不參與";
        public const string NextText = "下一步";
        public const string ExcelNextText = "選擇 Excel 檔案";
        public const string NoSemesterBatchText = "此期間無學期成績批次";
    }

    public sealed class EditableCalculationPeriodMemory
    {
        public string SchoolYear { get; private set; }
        public string Semester { get; private set; }
        public bool HasValue { get; private set; }

        public EditableCalculationPeriodMemory()
        {
            SchoolYear = "";
            Semester = "";
        }

        public void Remember(string schoolYear, string semester)
        {
            SchoolYear = schoolYear ?? "";
            Semester = semester == "2" ? "2" : "1";
            HasValue = SchoolYear.Length > 0;
        }
    }
}
