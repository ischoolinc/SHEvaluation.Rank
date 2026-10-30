namespace SHEvaluation.Rank.MotherGroup
{
    public static class SemesterRankWriteGate
    {
        public const string BlockedMessage = "確認清單的參與狀態尚未套用到排名計算。本階段不寫入排名資料。";

        public static bool CanWriteRankTables()
        {
            return true;
        }
    }
}
