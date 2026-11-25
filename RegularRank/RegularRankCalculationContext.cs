using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;

namespace SHEvaluation.Rank.RegularRank
{
    public enum RegularRankSourceKind
    {
        Current,
        History,
        Excel
    }

    public enum RegularRankLoadState
    {
        NotLoaded,
        Loaded,
        Stale,
        Failed
    }

    internal static class RegularRankIdentifiers
    {
        internal const long MaxExamId = int.MaxValue;
        internal const long MaxBatchId = long.MaxValue;

        internal static bool TryReadExamId(string text, out string canonical, out string error)
        {
            return TryReadCanonical(text, MaxExamId, "exam ID", out canonical, out error);
        }

        internal static bool TryReadBatchId(string text, out string canonical, out string error)
        {
            return TryReadCanonical(text, MaxBatchId, "歷史批次 ID", out canonical, out error);
        }

        private static bool TryReadCanonical(string text, long maximum, string label, out string canonical, out string error)
        {
            canonical = "";
            string trimmed = (text ?? "").Trim();
            long number;
            if (trimmed.Length == 0
                || !long.TryParse(trimmed, NumberStyles.None, CultureInfo.InvariantCulture, out number)
                || number <= 0
                || number > maximum
                || number.ToString(CultureInfo.InvariantCulture) != trimmed)
            {
                error = label + "必須是 1 到 " + maximum.ToString(CultureInfo.InvariantCulture) + " 的整數，且不能有空白、正負號或前導零。";
                return false;
            }
            canonical = trimmed;
            error = "";
            return true;
        }
    }

    public sealed class RegularRankExamChoice
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public sealed class RegularRankExamResolution
    {
        public bool Resolved { get; private set; }
        public string ExamId { get; private set; }
        public string ExamName { get; private set; }
        public string Message { get; private set; }

        public static RegularRankExamResolution Resolve(string selectedId, string selectedName, IList<RegularRankExamChoice> choices)
        {
            string id = (selectedId ?? "").Trim();
            string name = (selectedName ?? "").Trim();
            if (choices == null)
                return Fail("沒有考試清單，不能選擇考試。");

            if (id.Length > 0)
            {
                string canonicalId;
                string idError;
                if (!RegularRankIdentifiers.TryReadExamId(id, out canonicalId, out idError))
                    return Fail(idError);
                RegularRankExamChoice found = null;
                int count = 0;
                foreach (RegularRankExamChoice choice in choices)
                {
                    string choiceId;
                    string ignored;
                    if (choice == null || !RegularRankIdentifiers.TryReadExamId(choice.Id, out choiceId, out ignored) || choiceId != canonicalId)
                        continue;
                    count++;
                    found = choice;
                }
                if (count == 0)
                    return Fail("指定的 exam ID 不在考試清單。");
                if (count != 1)
                    return Fail("考試清單有重複的 exam ID，不能任取一筆。");
                return Success(canonicalId, (found.Name ?? "").Trim());
            }

            if (name.Length == 0)
                return Fail("尚未指定考試。");

            RegularRankExamChoice named = null;
            int nameCount = 0;
            foreach (RegularRankExamChoice choice in choices)
            {
                if (choice == null || (choice.Name ?? "").Trim() != name)
                    continue;
                nameCount++;
                named = choice;
            }
            if (nameCount == 0)
                return Fail("考試清單沒有這個考試名稱。");
            if (nameCount != 1)
                return Fail("考試「" + name + "」對應多個 exam ID，不能任取第一筆或最後一筆。");
            string namedId;
            string namedError;
            if (!RegularRankIdentifiers.TryReadExamId(named.Id, out namedId, out namedError))
                return Fail(namedError);
            return Success(namedId, name);
        }

        private static RegularRankExamResolution Success(string examId, string examName)
        {
            RegularRankExamResolution resolution = new RegularRankExamResolution();
            resolution.Resolved = true;
            resolution.ExamId = examId ?? "";
            resolution.ExamName = examName ?? "";
            resolution.Message = "";
            return resolution;
        }

        private static RegularRankExamResolution Fail(string message)
        {
            RegularRankExamResolution resolution = new RegularRankExamResolution();
            resolution.Resolved = false;
            resolution.ExamId = "";
            resolution.ExamName = "";
            resolution.Message = message ?? "";
            return resolution;
        }
    }

    public enum RegularRankSubjectSet
    {
        General,
        Tag1,
        Tag2
    }

    public sealed class RegularRankSubjectGroup
    {
        private List<string> available;
        private List<string> checkedNames;
        private List<string> added;
        private List<string> removed;

        public RegularRankSubjectGroup()
        {
            available = new List<string>();
            checkedNames = new List<string>();
            added = new List<string>();
            removed = new List<string>();
        }

        public ReadOnlyCollection<string> Available
        {
            get { return available.AsReadOnly(); }
        }

        public ReadOnlyCollection<string> Checked
        {
            get { return checkedNames.AsReadOnly(); }
        }

        public ReadOnlyCollection<string> Added
        {
            get { return added.AsReadOnly(); }
        }

        public ReadOnlyCollection<string> Removed
        {
            get { return removed.AsReadOnly(); }
        }

        internal void Replace(List<string> nextAvailable, List<string> nextChecked, List<string> nextAdded, List<string> nextRemoved)
        {
            available = nextAvailable ?? new List<string>();
            checkedNames = nextChecked ?? new List<string>();
            added = nextAdded ?? new List<string>();
            removed = nextRemoved ?? new List<string>();
        }
    }

    public sealed class RegularRankConfirmRequest
    {
        public string SystemSchoolYear { get; private set; }
        public string SystemSemester { get; private set; }
        public string TargetSchoolYear { get; private set; }
        public string TargetSemester { get; private set; }
        public string TargetExamId { get; private set; }
        public string TargetExamName { get; private set; }
        public RegularRankSourceKind SourceKind { get; private set; }
        public string SourceSchoolYear { get; private set; }
        public string SourceSemester { get; private set; }
        public string SourceExamId { get; private set; }
        public string SourceExamName { get; private set; }
        public string SourceBatchId { get; private set; }
        public string ExcelIdentity { get; private set; }
        private List<int> gradeYears;
        private List<string> generalSubjects;
        private List<string> tag1Subjects;
        private List<string> tag2Subjects;

        public ReadOnlyCollection<int> GradeYears
        {
            get { return gradeYears.AsReadOnly(); }
        }

        public string ExcludedCategory { get; private set; }
        public string RankTag1 { get; private set; }
        public string RankTag2 { get; private set; }

        public ReadOnlyCollection<string> GeneralSubjects
        {
            get { return generalSubjects.AsReadOnly(); }
        }

        public ReadOnlyCollection<string> Tag1Subjects
        {
            get { return tag1Subjects.AsReadOnly(); }
        }

        public ReadOnlyCollection<string> Tag2Subjects
        {
            get { return tag2Subjects.AsReadOnly(); }
        }

        public string QuerySchoolYear
        {
            get { return TargetSchoolYear; }
        }

        public string QuerySemester
        {
            get { return TargetSemester; }
        }

        public string QueryExamId
        {
            get { return TargetExamId; }
        }

        internal static RegularRankConfirmRequest CopyFrom(RegularRankCalculationContext context)
        {
            RegularRankConfirmRequest request = new RegularRankConfirmRequest();
            request.SystemSchoolYear = context.SystemSchoolYear;
            request.SystemSemester = context.SystemSemester;
            request.TargetSchoolYear = context.TargetSchoolYear;
            request.TargetSemester = context.TargetSemester;
            request.TargetExamId = context.TargetExamId;
            request.TargetExamName = context.TargetExamName;
            request.SourceKind = context.SourceKind;
            request.SourceSchoolYear = context.SourceSchoolYear;
            request.SourceSemester = context.SourceSemester;
            request.SourceExamId = context.SourceExamId;
            request.SourceExamName = context.SourceExamName;
            request.SourceBatchId = context.SourceBatchId;
            request.ExcelIdentity = context.ExcelIdentity;
            request.gradeYears = CopyInts(context.GradeYears);
            request.ExcludedCategory = context.ExcludedCategory;
            request.RankTag1 = context.RankTag1;
            request.RankTag2 = context.RankTag2;
            request.generalSubjects = CopyStrings(context.GeneralSubjects.Checked);
            request.tag1Subjects = CopyStrings(context.Tag1Subjects.Checked);
            request.tag2Subjects = CopyStrings(context.Tag2Subjects.Checked);
            return request;
        }

        private static List<string> CopyStrings(IList<string> values)
        {
            List<string> copy = new List<string>();
            if (values == null)
                return copy;
            foreach (string value in values)
                copy.Add(value);
            return copy;
        }

        private static List<int> CopyInts(IList<int> values)
        {
            List<int> copy = new List<int>();
            if (values == null)
                return copy;
            foreach (int value in values)
                copy.Add(value);
            return copy;
        }
    }

    public sealed class RegularRankCalculationContext
    {
        public string SystemSchoolYear { get; private set; }
        public string SystemSemester { get; private set; }
        public string TargetSchoolYear { get; private set; }
        public string TargetSemester { get; private set; }
        public string TargetExamId { get; private set; }
        public string TargetExamName { get; private set; }
        public RegularRankSourceKind SourceKind { get; private set; }
        public string SourceSchoolYear { get; private set; }
        public string SourceSemester { get; private set; }
        public string SourceExamId { get; private set; }
        public string SourceExamName { get; private set; }
        public string SourceBatchId { get; private set; }
        public string ExcelIdentity { get; private set; }
        private List<int> gradeYears;
        public ReadOnlyCollection<int> GradeYears
        {
            get { return gradeYears.AsReadOnly(); }
        }
        public string ExcludedCategory { get; private set; }
        public string RankTag1 { get; private set; }
        public string RankTag2 { get; private set; }
        public RegularRankSubjectGroup GeneralSubjects { get; private set; }
        public RegularRankSubjectGroup Tag1Subjects { get; private set; }
        public RegularRankSubjectGroup Tag2Subjects { get; private set; }
        public RegularRankLoadState LoadState { get; private set; }
        public bool SubjectsUsable { get; private set; }
        public bool SubjectsConfirmed { get; private set; }

        public static RegularRankCalculationContext CreateDefault(string systemSchoolYear, string systemSemester)
        {
            RegularRankCalculationContext context = new RegularRankCalculationContext();
            context.SystemSchoolYear = (systemSchoolYear ?? "").Trim();
            context.SystemSemester = (systemSemester ?? "").Trim();
            context.TargetSchoolYear = "";
            context.TargetSemester = "";
            context.TargetExamId = "";
            context.TargetExamName = "";
            context.SourceKind = RegularRankSourceKind.Current;
            context.ClearSourceIdentity();
            context.gradeYears = new List<int>();
            context.ExcludedCategory = "";
            context.RankTag1 = "";
            context.RankTag2 = "";
            context.GeneralSubjects = new RegularRankSubjectGroup();
            context.Tag1Subjects = new RegularRankSubjectGroup();
            context.Tag2Subjects = new RegularRankSubjectGroup();
            context.LoadState = RegularRankLoadState.NotLoaded;
            context.SubjectsUsable = false;
            context.SubjectsConfirmed = false;
            string year;
            string semester;
            string periodError;
            if (TryReadPeriod(context.SystemSchoolYear, context.SystemSemester, out year, out semester, out periodError))
            {
                context.TargetSchoolYear = year;
                context.TargetSemester = semester;
            }
            return context;
        }

        public string QuerySchoolYear
        {
            get { return TargetSchoolYear; }
        }

        public string QuerySemester
        {
            get { return TargetSemester; }
        }

        public string QueryExamId
        {
            get { return TargetExamId; }
        }

        public string SetTargetPeriod(string schoolYear, string semester)
        {
            string year;
            string normalizedSemester;
            string error;
            if (!TryReadPeriod(schoolYear, semester, out year, out normalizedSemester, out error))
                return error;
            if (year == TargetSchoolYear && normalizedSemester == TargetSemester)
                return "";
            TargetSchoolYear = year;
            TargetSemester = normalizedSemester;
            TouchCondition();
            return "";
        }

        public string SetTargetExam(RegularRankExamResolution resolution)
        {
            if (resolution == null || !resolution.Resolved)
                return resolution == null ? "尚未指定目標考試。" : resolution.Message;
            string canonical;
            string idError;
            if (!RegularRankIdentifiers.TryReadExamId(resolution.ExamId, out canonical, out idError))
                return idError;
            if (canonical == TargetExamId && (resolution.ExamName ?? "") == TargetExamName)
                return "";
            TargetExamId = canonical;
            TargetExamName = resolution.ExamName ?? "";
            TouchCondition();
            return "";
        }

        public string SetCurrentSource()
        {
            if (SourceKind == RegularRankSourceKind.Current
                && SourceBatchId.Length == 0
                && SourceExamId.Length == 0
                && ExcelIdentity.Length == 0
                && SourceSchoolYear.Length == 0
                && SourceSemester.Length == 0)
                return "";
            SourceKind = RegularRankSourceKind.Current;
            ClearSourceIdentity();
            TouchCondition();
            return "";
        }

        public string SetHistorySource(string batchId, string schoolYear, string semester, RegularRankExamResolution sourceExam)
        {
            string canonicalBatch;
            string batchError;
            if (!RegularRankIdentifiers.TryReadBatchId(batchId, out canonicalBatch, out batchError))
                return batchError;
            string year;
            string normalizedSemester;
            string periodError;
            if (!TryReadPeriod(schoolYear, semester, out year, out normalizedSemester, out periodError))
                return periodError;
            if (sourceExam == null || !sourceExam.Resolved)
                return sourceExam == null ? "歷史來源的考試無法唯一識別。" : sourceExam.Message;
            string canonicalExam;
            string examError;
            if (!RegularRankIdentifiers.TryReadExamId(sourceExam.ExamId, out canonicalExam, out examError))
                return examError;
            if (SourceKind == RegularRankSourceKind.History
                && SourceBatchId == canonicalBatch
                && SourceSchoolYear == year
                && SourceSemester == normalizedSemester
                && SourceExamId == canonicalExam
                && SourceExamName == (sourceExam.ExamName ?? "")
                && ExcelIdentity.Length == 0)
                return "";
            SourceKind = RegularRankSourceKind.History;
            SourceBatchId = canonicalBatch;
            SourceSchoolYear = year;
            SourceSemester = normalizedSemester;
            SourceExamId = canonicalExam;
            SourceExamName = sourceExam.ExamName ?? "";
            ExcelIdentity = "";
            TouchCondition();
            return "";
        }

        public string SetExcelSource(string excelIdentity)
        {
            string identity = (excelIdentity ?? "").Trim();
            if (identity.Length == 0)
                return "Excel 來源必須指定匯入識別。";
            if (SourceKind == RegularRankSourceKind.Excel
                && ExcelIdentity == identity
                && SourceBatchId.Length == 0
                && SourceExamId.Length == 0)
                return "";
            SourceKind = RegularRankSourceKind.Excel;
            ExcelIdentity = identity;
            SourceBatchId = "";
            SourceSchoolYear = "";
            SourceSemester = "";
            SourceExamId = "";
            SourceExamName = "";
            TouchCondition();
            return "";
        }

        public string SetGradeYears(IList<int> selectedGrades)
        {
            List<int> copy = new List<int>();
            if (selectedGrades != null)
            {
                foreach (int gradeYear in selectedGrades)
                {
                    if (gradeYear <= 0)
                        return "年級必須是正整數。";
                    if (!copy.Contains(gradeYear))
                        copy.Add(gradeYear);
                }
            }
            if (SameInts(gradeYears, copy))
                return "";
            gradeYears = copy;
            TouchCondition();
            return "";
        }

        public string SetCheckedSubjects(RegularRankSubjectSet subjectSet, IList<string> checkedNames)
        {
            if (LoadState != RegularRankLoadState.Loaded || !SubjectsUsable)
                return "科目尚未成功更新，不能調整勾選。";
            RegularRankSubjectGroup group = SubjectGroup(subjectSet);
            List<string> requested = new List<string>();
            if (checkedNames != null)
            {
                foreach (string name in checkedNames)
                {
                    string trimmed = (name ?? "").Trim();
                    if (trimmed.Length == 0)
                        return "勾選科目不能是空白。";
                    if (requested.Contains(trimmed))
                        return "勾選科目「" + trimmed + "」重複。";
                    if (!ContainsText(group.Available, trimmed))
                        return "勾選科目「" + trimmed + "」不在這組候選。";
                    requested.Add(trimmed);
                }
            }
            List<string> ordered = new List<string>();
            foreach (string name in group.Available)
            {
                if (requested.Contains(name))
                    ordered.Add(name);
            }
            if (SameStrings(group.Checked, ordered))
                return "";
            group.Replace(CopyStrings(group.Available), ordered, CopyStrings(group.Added), CopyStrings(group.Removed));
            SubjectsConfirmed = false;
            return "";
        }

        public string SetRankCategories(string excludedCategory, string rankTag1, string rankTag2)
        {
            string excluded = excludedCategory ?? "";
            string tag1 = rankTag1 ?? "";
            string tag2 = rankTag2 ?? "";
            if (excluded == ExcludedCategory && tag1 == RankTag1 && tag2 == RankTag2)
                return "";
            ExcludedCategory = excluded;
            RankTag1 = tag1;
            RankTag2 = tag2;
            TouchCondition();
            return "";
        }

        public string ClearTargetExam()
        {
            if ((TargetExamId ?? "").Length == 0 && (TargetExamName ?? "").Length == 0)
                return "";
            TargetExamId = "";
            TargetExamName = "";
            TouchCondition();
            return "";
        }

        public string SetSourceKindWithoutIdentity(RegularRankSourceKind kind)
        {
            bool identityEmpty = (SourceSchoolYear ?? "").Length == 0
                && (SourceSemester ?? "").Length == 0
                && (SourceExamId ?? "").Length == 0
                && (SourceExamName ?? "").Length == 0
                && (SourceBatchId ?? "").Length == 0
                && (ExcelIdentity ?? "").Length == 0;
            if (SourceKind == kind && identityEmpty)
                return "";
            SourceKind = kind;
            ClearSourceIdentity();
            TouchCondition();
            return "";
        }

        public string MarkMotherGroupLoaded()
        {
            string gate = GateMessage(true);
            if (gate.Length > 0)
                return gate;
            LoadState = RegularRankLoadState.Loaded;
            SubjectsUsable = false;
            SubjectsConfirmed = false;
            return "";
        }

        public string MarkSubjectLoadFailed()
        {
            LoadState = RegularRankLoadState.Failed;
            SubjectsUsable = false;
            SubjectsConfirmed = false;
            return "";
        }

        public string ApplySubjectCandidates(
            IList<string> generalAvailable,
            IList<string> tag1Available,
            IList<string> tag2Available,
            IList<string> generalUncounted,
            IList<string> tag1Uncounted,
            IList<string> tag2Uncounted)
        {
            if (LoadState != RegularRankLoadState.Loaded)
                return "必須先成功載入母群，才能更新科目。";
            SubjectsConfirmed = false;
            SubjectsUsable = false;
            List<string> generalAvailableNext;
            List<string> generalCheckedNext;
            List<string> generalAdded;
            List<string> generalRemoved;
            List<string> tag1AvailableNext;
            List<string> tag1CheckedNext;
            List<string> tag1Added;
            List<string> tag1Removed;
            List<string> tag2AvailableNext;
            List<string> tag2CheckedNext;
            List<string> tag2Added;
            List<string> tag2Removed;
            string error;
            if (!TryPlanGroup(GeneralSubjects, generalAvailable, generalUncounted, out generalAvailableNext, out generalCheckedNext, out generalAdded, out generalRemoved, out error))
                return error;
            if (!TryPlanGroup(Tag1Subjects, tag1Available, tag1Uncounted, out tag1AvailableNext, out tag1CheckedNext, out tag1Added, out tag1Removed, out error))
                return error;
            if (!TryPlanGroup(Tag2Subjects, tag2Available, tag2Uncounted, out tag2AvailableNext, out tag2CheckedNext, out tag2Added, out tag2Removed, out error))
                return error;
            GeneralSubjects.Replace(generalAvailableNext, generalCheckedNext, generalAdded, generalRemoved);
            Tag1Subjects.Replace(tag1AvailableNext, tag1CheckedNext, tag1Added, tag1Removed);
            Tag2Subjects.Replace(tag2AvailableNext, tag2CheckedNext, tag2Added, tag2Removed);
            SubjectsConfirmed = false;
            SubjectsUsable = GeneralSubjects.Available.Count + Tag1Subjects.Available.Count + Tag2Subjects.Available.Count > 0;
            if (!SubjectsUsable)
                return "沒有可用科目，不能進入確認。";
            return "";
        }

        public string ConfirmSubjects()
        {
            if (LoadState != RegularRankLoadState.Loaded || !SubjectsUsable)
                return "沒有可用科目，不能進入確認。";
            SubjectsConfirmed = true;
            return "";
        }

        public string CanCreateRequest()
        {
            string gate = GateMessage(true);
            if (gate.Length > 0)
                return gate;
            if (LoadState == RegularRankLoadState.Failed)
                return "科目載入失敗，不能用上一份科目搭配新條件。";
            if (LoadState != RegularRankLoadState.Loaded)
                return "條件已變更，必須重新載入母群與科目。";
            if (!SubjectsUsable)
                return "沒有可用科目，不能進入確認。";
            if (!SubjectsConfirmed)
                return "尚未確認三組科目。";
            return "";
        }

        public RegularRankConfirmRequest CreateRequest()
        {
            string error = CanCreateRequest();
            if (error.Length > 0)
                throw new InvalidOperationException(error);
            return RegularRankConfirmRequest.CopyFrom(this);
        }

        public RegularRankCalculationContext CreateWorkCopy()
        {
            string error = CanCreateRequest();
            if (error.Length > 0)
                throw new InvalidOperationException(error);
            RegularRankCalculationContext copy = CreateDefault(SystemSchoolYear, SystemSemester);
            copy.TargetSchoolYear = TargetSchoolYear;
            copy.TargetSemester = TargetSemester;
            copy.TargetExamId = TargetExamId;
            copy.TargetExamName = TargetExamName;
            copy.SourceKind = SourceKind;
            copy.SourceSchoolYear = SourceSchoolYear;
            copy.SourceSemester = SourceSemester;
            copy.SourceExamId = SourceExamId;
            copy.SourceExamName = SourceExamName;
            copy.SourceBatchId = SourceBatchId;
            copy.ExcelIdentity = ExcelIdentity;
            copy.gradeYears = CopyInts(gradeYears);
            copy.ExcludedCategory = ExcludedCategory;
            copy.RankTag1 = RankTag1;
            copy.RankTag2 = RankTag2;
            copy.GeneralSubjects = CopyGroup(GeneralSubjects);
            copy.Tag1Subjects = CopyGroup(Tag1Subjects);
            copy.Tag2Subjects = CopyGroup(Tag2Subjects);
            copy.LoadState = LoadState;
            copy.SubjectsUsable = SubjectsUsable;
            copy.SubjectsConfirmed = SubjectsConfirmed;
            return copy;
        }

        private string GateMessage(bool requireGrades)
        {
            string ignored;
            string year;
            string semester;
            if (!TryReadPeriod(TargetSchoolYear, TargetSemester, out year, out semester, out ignored))
                return "目標期間不完整。";
            if ((TargetExamId ?? "").Length == 0)
                return "尚未指定目標考試。";
            if (requireGrades && (gradeYears == null || gradeYears.Count == 0))
                return "尚未選擇年級。";
            if (SourceKind == RegularRankSourceKind.History)
            {
                if ((SourceBatchId ?? "").Length == 0 || (SourceExamId ?? "").Length == 0)
                    return "歷史來源的考試無法唯一識別。";
                if (!TryReadPeriod(SourceSchoolYear, SourceSemester, out year, out semester, out ignored))
                    return "歷史來源期間不完整。";
            }
            if (SourceKind == RegularRankSourceKind.Excel && (ExcelIdentity ?? "").Length == 0)
                return "Excel 來源必須指定匯入識別。";
            return "";
        }

        private void TouchCondition()
        {
            if (LoadState == RegularRankLoadState.NotLoaded)
                return;
            LoadState = RegularRankLoadState.Stale;
            SubjectsUsable = false;
            SubjectsConfirmed = false;
        }

        private void ClearSourceIdentity()
        {
            SourceSchoolYear = "";
            SourceSemester = "";
            SourceExamId = "";
            SourceExamName = "";
            SourceBatchId = "";
            ExcelIdentity = "";
        }

        private static bool TryPlanGroup(
            RegularRankSubjectGroup group,
            IList<string> available,
            IList<string> uncounted,
            out List<string> nextAvailable,
            out List<string> nextChecked,
            out List<string> added,
            out List<string> removed,
            out string error)
        {
            nextAvailable = new List<string>();
            nextChecked = new List<string>();
            added = new List<string>();
            removed = new List<string>();
            error = "";
            nextAvailable = new List<string>();
            if (available != null)
            {
                foreach (string name in available)
                {
                    string trimmed = (name ?? "").Trim();
                    if (trimmed.Length == 0)
                    {
                        error = "科目名稱不能是空白。";
                        return false;
                    }
                    if (nextAvailable.Contains(trimmed))
                    {
                        error = "科目「" + trimmed + "」重複，不能任取一筆。";
                        return false;
                    }
                    nextAvailable.Add(trimmed);
                }
            }

            foreach (string oldName in group.Available)
            {
                if (!nextAvailable.Contains(oldName))
                    removed.Add(oldName);
            }
            foreach (string name in nextAvailable)
            {
                bool isNew = !group.Available.Contains(name);
                bool check = isNew ? !ContainsText(uncounted, name) : group.Checked.Contains(name);
                if (check)
                    nextChecked.Add(name);
                if (isNew)
                    added.Add(name);
            }
            return true;
        }

        private RegularRankSubjectGroup SubjectGroup(RegularRankSubjectSet subjectSet)
        {
            if (subjectSet == RegularRankSubjectSet.Tag1)
                return Tag1Subjects;
            if (subjectSet == RegularRankSubjectSet.Tag2)
                return Tag2Subjects;
            return GeneralSubjects;
        }

        private static bool ContainsText(IList<string> values, string name)
        {
            if (values == null)
                return false;
            foreach (string value in values)
            {
                if ((value ?? "").Trim() == name)
                    return true;
            }
            return false;
        }

        private static RegularRankSubjectGroup CopyGroup(RegularRankSubjectGroup group)
        {
            RegularRankSubjectGroup copy = new RegularRankSubjectGroup();
            copy.Replace(CopyStrings(group.Available), CopyStrings(group.Checked), CopyStrings(group.Added), CopyStrings(group.Removed));
            return copy;
        }

        private static List<string> CopyStrings(IList<string> values)
        {
            List<string> copy = new List<string>();
            if (values == null)
                return copy;
            foreach (string value in values)
                copy.Add(value);
            return copy;
        }

        private static List<int> CopyInts(IList<int> values)
        {
            List<int> copy = new List<int>();
            if (values == null)
                return copy;
            foreach (int value in values)
                copy.Add(value);
            return copy;
        }

        private static bool SameStrings(IList<string> left, IList<string> right)
        {
            if (left == null || right == null)
                return left == right;
            if (left.Count != right.Count)
                return false;
            for (int i = 0; i < left.Count; i++)
            {
                if (left[i] != right[i])
                    return false;
            }
            return true;
        }

        private static bool SameInts(IList<int> left, IList<int> right)
        {
            if (left == null || right == null)
                return left == right;
            if (left.Count != right.Count)
                return false;
            for (int i = 0; i < left.Count; i++)
            {
                if (left[i] != right[i])
                    return false;
            }
            return true;
        }

        private static bool TryReadPeriod(string schoolYear, string semester, out string year, out string normalizedSemester, out string error)
        {
            year = "";
            normalizedSemester = "";
            error = "";
            string yearText = (schoolYear ?? "").Trim();
            string semesterText = (semester ?? "").Trim();
            int yearNumber;
            if (!int.TryParse(yearText, NumberStyles.None, CultureInfo.InvariantCulture, out yearNumber) || yearNumber <= 0 || yearNumber.ToString(CultureInfo.InvariantCulture) != yearText)
            {
                error = "學年度必須是正整數。";
                return false;
            }
            if (semesterText != "1" && semesterText != "2")
            {
                error = "學期必須是 1 或 2。";
                return false;
            }
            year = yearText;
            normalizedSemester = semesterText;
            return true;
        }
    }
}
