using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using SHEvaluation.Rank.MotherGroup;

namespace SHEvaluation.Rank.RegularRank
{
    public static class RegularRankDesignerStructureCheck
    {
        [STAThread]
        public static int Main(string[] args)
        {
            string root = args != null && args.Length > 0 ? args[0] : @"C:\ischoolProject\SHEvaluation-Regular";
            List<string> errors = new List<string>();
            try
            {
                errors.AddRange(CheckDesignerSources(root));
                errors.AddRange(CheckSourceBatchDropDownWidth());
                errors.AddRange(CheckConfirmFormLifecycle());
                errors.AddRange(CheckCalculationStillBlocked());
            }
            catch (Exception ex)
            {
                errors.Add(ex.ToString());
            }

            string reportPath = Path.Combine(root, @"bin\Debug\regular-rank-designer-structure-check.txt");
            List<string> report = new List<string>();
            if (errors.Count == 0)
                report.Add("regular-rank-designer-structure-check: ok");
            else
                report.AddRange(errors);
            report.Add("vs2022-designer-open-save-reopen: not-executed");
            report.Add("ischool-screen-executed: false");
            report.Add("database-executed: false");
            File.WriteAllLines(reportPath, report.ToArray());
            foreach (string line in report)
                Console.WriteLine(line);
            return errors.Count == 0 ? 0 : 1;
        }

        private static List<string> CheckDesignerSources(string root)
        {
            List<string> errors = new List<string>();
            string mainDesigner = File.ReadAllText(Path.Combine(root, "CalculateRegularAssessmentRank.Designer.cs"));
            string confirmDesigner = File.ReadAllText(Path.Combine(root, @"RegularRank\RegularRankMotherGroupConfirmForm.Designer.cs"));
            string host = File.ReadAllText(Path.Combine(root, "CalculateRegularAssessmentRank.cs"));

            string[] forbidden = new string[]
            {
                "plStudentView", "dgvStudentList", "btnCacluate", "btnPrevious", "pbLoading",
                "btnExport", "btnImport", "lbExam", "lbSchoolYear", "lbSemester"
            };
            foreach (string name in forbidden)
            {
                if (mainDesigner.Contains("this." + name) || ContainsField(mainDesigner, name))
                    errors.Add("條件 Form Designer 仍有舊學生頁控制項：" + name);
                if (ContainsLiveSymbol(host, name))
                    errors.Add("條件 Form 程式仍引用舊學生頁控制項：" + name);
            }

            Expect(errors, mainDesigner.Contains("cboMotherGroupSource") && mainDesigner.Contains("btnLoadMotherGroup") && mainDesigner.Contains("btnNext") && !mainDesigner.Contains("btnConfirmSubjects"), "條件 Designer 應含來源、載入與下一步，確認科目併入下一步");
            Expect(errors, !host.Contains("btnConfirmSubjects"), "條件程式不得再綁定確認三組科目按鈕");
            Expect(errors, host.Contains("SetCheckedSubjects") && host.Contains("ConfirmSubjects") && host.Contains("CanCreateRequest"), "下一步應套用三組勾選、確認科目並通過 request 檢查");
            Expect(errors, mainDesigner.Contains("cboSourceBatch.Location = new System.Drawing.Point(656, 72)") && mainDesigner.Contains("cboSourceBatch.Size = new System.Drawing.Size(184, 25)") && mainDesigner.Contains("cboSourceBatch.DropDownWidth = 300"), "來源批次收合欄位維持原位與原寬，展開基準為 300");
            Expect(errors, host.Contains("TextRenderer.MeasureText") && host.Contains("DropDownWidth"), "來源批次展開寬度應依實際字型量測");
            Expect(errors, !confirmDesigner.Contains("btnVerify"), "確認頁 Designer 應移除核對試算按鈕");
            Expect(errors, mainDesigner.Contains("this.Controls.Add(this.plSetting)") && !mainDesigner.Contains("Controls.Add(this.plStudentView)"), "條件 Form 只能承載 plSetting");
            Expect(errors, mainDesigner.Contains("plSetting.Dock = System.Windows.Forms.DockStyle.Fill"), "條件頁 plSetting 應 Dock Fill");
            Expect(errors, host.Contains("LicenseManager.UsageMode == LicenseUsageMode.Designtime"), "條件 Form 預設建構子設計時期不得查資料庫");
            Expect(errors, host.Contains("TryCalculateFromConfirm") && host.Contains("OpenMotherGroupConfirm") && host.Contains("ReturnFromMotherGroupConfirm"), "計算與往返必須銜接確認頁");

            Expect(errors, confirmDesigner.Contains("dgvStudents") && confirmDesigner.Contains("dgvPopulation") && confirmDesigner.Contains("dgvDetail"), "確認 Designer 應有三個 Grid");
            Expect(errors, confirmDesigner.Contains("btnRestore") && confirmDesigner.Contains("btnExport") && confirmDesigner.Contains("btnPrevious") && confirmDesigner.Contains("btnCalculate"), "確認 Designer 應有恢復、匯出、上一步與計算");
            Expect(errors, confirmDesigner.Contains("txtSearch") && confirmDesigner.Contains("#region Windows Form Designer generated code"), "確認 Designer 應有搜尋與 Designer region");
            Expect(errors, !confirmDesigner.Contains("MaximumSize"), "確認 Form 不得鎖死 MaximumSize");
            return errors;
        }

        private static List<string> CheckSourceBatchDropDownWidth()
        {
            List<string> errors = new List<string>();
            using (Font font = new Font(FontFamily.GenericSansSerif, 9.75f, FontStyle.Regular, GraphicsUnit.Point))
            {
                string longText = "123456｜113-2｜第一次定期評量加長考試名稱（ID 9876543210）";
                int measured = TextRenderer.MeasureText(
                    longText,
                    font,
                    new Size(short.MaxValue, short.MaxValue),
                    TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding).Width;
                int scrollbar = SystemInformation.VerticalScrollBarWidth;
                int expected = measured + scrollbar + 16;
                Expect(errors, expected > 320, "測試文字應寬於 320，才能驗證螢幕寬度截斷");
                int open = CalculateRegularAssessmentRank.SourceBatchDropDownWidth(
                    font,
                    new string[] { "1｜113-1｜期中（ID 1）", longText },
                    184,
                    96,
                    20000);
                Expect(errors, open == expected, "展開寬度為最長文字、垂直捲軸與左右留白");
                int baseline = CalculateRegularAssessmentRank.SourceBatchDropDownWidth(font, new string[] { "1" }, 184, 96, 20000);
                Expect(errors, baseline == 300, "96 DPI 的短文字以 300 為展開基準");
                int scaled = CalculateRegularAssessmentRank.SourceBatchDropDownWidth(font, new string[] { "1" }, 184, 192, 20000);
                Expect(errors, scaled == 600, "192 DPI 將 300 基準放大為 600");
                int clamped = CalculateRegularAssessmentRank.SourceBatchDropDownWidth(font, new string[] { longText }, 184, 96, 320);
                Expect(errors, clamped == 320, "展開寬度不超過可用螢幕寬度");
                int floor = CalculateRegularAssessmentRank.SourceBatchDropDownWidth(font, new string[] { "1" }, 420, 96, 20000);
                Expect(errors, floor == 420, "短文字仍不窄於收合欄位");
            }
            return errors;
        }

        private static List<string> CheckConfirmFormLifecycle()
        {
            List<string> errors = new List<string>();
            using (RegularRankMotherGroupConfirmForm first = new RegularRankMotherGroupConfirmForm())
            using (RegularRankMotherGroupConfirmForm second = new RegularRankMotherGroupConfirmForm())
            {
                Expect(errors, first != null && second != null, "預設建構子可建立兩個確認 Form，不查資料庫");
                first.PrepareProtectedGrids();
                List<ConfirmStudentRow> rows = new List<ConfirmStudentRow>();
                for (int i = 0; i < 3; i++)
                {
                    StudentMotherGroup group = new StudentMotherGroup();
                    group.StudentId = (i + 1).ToString();
                    group.StudentNumber = group.StudentId;
                    group.StudentName = "測" + i;
                    group.Status = 1;
                    group.SetSourceSlots(new RankSlot[]
                    {
                        new RankSlot { RankType = RankKinds.Year, GroupName = "1年級", Participate = true },
                        new RankSlot { RankType = RankKinds.Department, GroupName = "普通科", Participate = true },
                        new RankSlot { RankType = RankKinds.Class, GroupName = "1班", Participate = true },
                        new RankSlot { RankType = RankKinds.Tag1, GroupName = "", Participate = false },
                        new RankSlot { RankType = RankKinds.Tag2, GroupName = "", Participate = false }
                    });
                    rows.Add(new ConfirmStudentRow { Group = group, ClassName = "現班", SeatNo = "1" });
                }
                RegularRankCalculationContext context = RegularRankCalculationContext.CreateDefault("115", "1");
                context.SetTargetPeriod("113", "2");
                context.SetCurrentSource();
                context.SetTargetExam(RegularRankExamResolution.Resolve("30", "", new RegularRankExamChoice[]
                {
                    new RegularRankExamChoice { Id = "30", Name = "第一次定期評量" }
                }));
                context.SetGradeYears(new int[] { 1 });
                context.MarkMotherGroupLoaded();
                context.ApplySubjectCandidates(new string[] { "國文" }, new string[] { "國文" }, new string[] { "國文" }, new string[0], new string[0], new string[0]);
                context.ConfirmSubjects();
                first.ShowStudents(context.CreateRequest(), rows);
                Expect(errors, first.StudentGrid.Rows.Count == 3 && first.List.Rows.Count == 3, "確認頁填列走受保護 Grid");
                Expect(errors, MotherGroupConfirmGrid.IsBoundedRow(first.StudentGrid.Rows[0]), "確認頁學生列受保護");
                Expect(errors, first.CalculateButtonEnabled(), "有確認清單時計算按鈕可按，寫入前仍做傳輸檢查");
                Expect(errors, first.Controls.Find("btnVerify", true).Length == 0, "確認頁沒有核對試算按鈕");
                first.HideForHost();
                Expect(errors, !first.Visible, "上一步／關閉後確認頁可隱藏，不銷毀宿主條件");
            }
            return errors;
        }

        private static List<string> CheckCalculationStillBlocked()
        {
            List<string> errors = new List<string>();
            RegularRankCalculationEntry.ClearEvidence();
            Expect(errors, !RegularRankCalculationEntry.ExecutionAllowed, "未通過傳輸前不可寫入");
            Expect(errors, RegularRankCalculationEntry.TryEnterMessage().Length == 0, "清單就緒不因 Task 勾選永久停用按鈕");
            Expect(errors, RegularRankCalculationEntry.CanOfferCalculate(3) && !RegularRankCalculationEntry.CanOfferCalculate(0), "計算按鈕依確認人數");
            RegularRankCalculationGuard guard = new RegularRankCalculationGuard();
            Expect(errors, guard.TryEnter() && !guard.EditorsEnabled, "防重入可進入；寫入成功仍取決於傳輸檢查");
            guard.FailAndRestore();
            Expect(errors, guard.EditorsEnabled, "失敗後可恢復編輯");
            return errors;
        }

        private static bool ContainsField(string designer, string name)
        {
            return designer.Contains(" " + name + ";");
        }

        private static bool ContainsLiveSymbol(string source, string name)
        {
            using (StringReader reader = new StringReader(source))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string trimmed = line.Trim();
                    if (trimmed.StartsWith("//"))
                        continue;
                    if (trimmed.Contains("考試名稱 = \"lbExam.Text\""))
                        continue;
                    if (trimmed.Contains("不是舊 plStudentView"))
                        continue;
                    if (line.Contains(name))
                        return true;
                }
            }
            return false;
        }

        private static void Expect(List<string> errors, bool condition, string message)
        {
            if (!condition)
                errors.Add(message);
        }
    }
}
