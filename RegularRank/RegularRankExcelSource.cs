using Aspose.Cells;
using SHEvaluation.Rank.MotherGroup;
using System;
using System.Collections.Generic;
using System.Data;

namespace SHEvaluation.Rank.RegularRank
{
    // Database access is supplied by the caller so identity validation can be
    // tested without a school connection. No partial workbook is ever returned.
    public static class RegularRankExcelSource
    {
        public static List<ConfirmStudentRow> Load(Workbook workbook,
            Func<string, DataTable> query, string excludedCategory,
            IList<TagDefinition> definitions, IList<StudentTagLink> links,
            out string warning)
        {
            if (query == null) throw new ArgumentNullException("query");
            warning = "";
            List<MotherGroupExcelIdentity> keys = MotherGroupExcelImport.IdentityKeys(workbook);
            DataTable identities = keys.Count == 0 ? new DataTable() : query(MotherGroupExcelImport.SelectSql(keys));
            MotherGroupExcelValidation validation = MotherGroupExcelImport.Validate(workbook, identities);
            if (!validation.IsValid)
                throw new InvalidOperationException(MotherGroupExcelApply.IssueText(validation));
            List<StudentMotherGroup> groups = MotherGroupExcelApply.CreateGroups(validation,
                RegularRankCurrentStudentSource.NormalizeCategory(excludedCategory), definitions, links);
            if (groups.Count == 0) throw new InvalidOperationException("Excel 沒有學生資料。");
            List<ConfirmStudentRow> result = new List<ConfirmStudentRow>();
            foreach (StudentMotherGroup group in groups)
                result.Add(new ConfirmStudentRow { Group = group, ClassName = "", SeatNo = "" });
            warning = MotherGroupExcelApply.WarningText(validation);
            return result;
        }
    }
}
