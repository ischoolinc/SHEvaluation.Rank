namespace SHEvaluation.Rank.RegularRank
{
    public static class RegularRankCalculationEntry
    {
        public const string NonDebugDatabaseMessage = "排名寫入僅允許資料庫名稱含 debug6 的測試環境。";
        public const string BlockedMessage = "排名資料庫結構檢查尚未通過，不能發布排名。";

        private static bool _databaseVerified;

        /// <summary>
        /// True only after a successful runtime database structure check in this session.
        /// Not tied to OpenSpec Task checkboxes. Never hard-coded true.
        /// </summary>
        public static bool ExecutionAllowed
        {
            get { return _databaseVerified; }
        }

        internal static void SetTestEvidence(RegularRankDatabaseEvidence evidence)
        {
            _databaseVerified = evidence != null
                && evidence.DatabaseName.IndexOf("debug6", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static void ClearEvidence()
        {
            _databaseVerified = false;
        }

        public static bool CanOfferCalculate(int studentCount)
        {
            return studentCount > 0;
        }

        public static string TryEnterMessage()
        {
            // The publish path checks the database again before saving.
            return "";
        }

        public static string PublishBlockedMessage()
        {
            if (!ExecutionAllowed)
                return BlockedMessage;
            return "";
        }
    }
}
