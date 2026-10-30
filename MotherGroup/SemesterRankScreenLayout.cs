using System;
using System.Drawing;
using System.Windows.Forms;

namespace SHEvaluation.Rank.MotherGroup
{
    public static class SemesterRankScreenLayout
    {
        public static void ArrangeFigure1(
            Control panel,
            Control yearCaption,
            NumericUpDown schoolYear,
            Control semesterCaption,
            ComboBox semester,
            Label sourceCaption,
            ComboBox source,
            Label historyYearCaption,
            NumericUpDown historyYear,
            Label historySemesterCaption,
            ComboBox historySemester,
            Control rankPeople,
            Control nextButton,
            Label historyBatchCaption = null,
            ComboBox historyBatch = null)
        {
            if (panel == null || yearCaption == null || schoolYear == null || semesterCaption == null || semester == null
                || sourceCaption == null || source == null || historyYearCaption == null || historyYear == null
                || historySemesterCaption == null || historySemester == null || rankPeople == null || nextButton == null)
                return;

            Place(schoolYear, yearCaption.Right + 8, 8, 72, 28);
            Place(semester, semesterCaption.Right + 8, 8, 56, 28);
            sourceCaption.AutoSize = false;
            Place(sourceCaption, semester.Right + 16, 12, 100, 24);
            int sourceWidth = Math.Min(220, Math.Max(140, panel.ClientSize.Width - (sourceCaption.Right + 18)));
            Place(source, sourceCaption.Right + 6, 8, sourceWidth, 28);

            int historyTop = schoolYear.Bottom + 8;
            historyYearCaption.AutoSize = false;
            Place(historyYearCaption, 12, historyTop + 2, 148, 24);
            Place(historyYear, historyYearCaption.Right + 6, historyTop, 72, 28);
            historySemesterCaption.AutoSize = false;
            Place(historySemesterCaption, historyYear.Right + 16, historyTop + 2, 124, 24);
            Place(historySemester, historySemesterCaption.Right + 6, historyTop, 56, 28);

            int groupTop = historyYear.Bottom + 8;
            if (historyBatch != null)
            {
                int batchTop = historyYear.Bottom + 8;
                int batchLeft = 12;
                if (historyBatchCaption != null)
                {
                    historyBatchCaption.AutoSize = false;
                    Place(historyBatchCaption, 12, batchTop + 2, 100, 24);
                    batchLeft = historyBatchCaption.Right + 6;
                }
                Place(historyBatch, batchLeft, batchTop, Math.Max(1, panel.ClientSize.Width - 12 - batchLeft), 28);
                groupTop = historyBatch.Bottom + 8;
            }
            int groupHeight = nextButton.Top - 8 - groupTop;
            Place(rankPeople, 12, groupTop, Math.Max(1, panel.ClientSize.Width - 24), Math.Max(160, groupHeight));
        }

        public static void ArrangeFigure2(
            Control panel,
            Control yearCaption,
            Control yearValue,
            Control semesterCaption,
            Control semesterValue,
            Label searchCaption,
            TextBox searchBox,
            Control groupCaption,
            Label fixedSummary,
            DataGridView population,
            DataGridView detail,
            DataGridView students,
            Button restore,
            Control previousButton,
            Control importButton,
            Control exportButton,
            Label addCaption = null,
            TextBox addNumber = null,
            Button addButton = null)
        {
            if (panel == null || yearCaption == null || yearValue == null || semesterCaption == null || semesterValue == null
                || searchCaption == null || searchBox == null || groupCaption == null || fixedSummary == null
                || population == null || detail == null || students == null || restore == null || previousButton == null)
                return;

            Place(yearValue, yearCaption.Right + 8, 12, 64, 24);
            Place(semesterCaption, yearValue.Right + 12, 12, 54, 24);
            Place(semesterValue, semesterCaption.Right + 8, 12, 40, 24);

            int searchWidth = 200;
            int searchLeft = panel.ClientSize.Width - 12 - searchWidth;
            int searchCaptionLeft = searchLeft - 64;
            if (searchCaptionLeft < semesterValue.Right + 16)
            {
                searchWidth = Math.Max(120, panel.ClientSize.Width - 12 - (semesterValue.Right + 16 + 64));
                searchLeft = panel.ClientSize.Width - 12 - searchWidth;
                searchCaptionLeft = searchLeft - 64;
            }
            searchCaption.AutoSize = false;
            Place(searchCaption, searchCaptionLeft, 12, 60, 24);
            Place(searchBox, searchLeft, 8, searchWidth, 28);

            Place(groupCaption, 12, 44, 88, 24);
            fixedSummary.AutoSize = false;
            Place(fixedSummary, 108, 40, Math.Max(1, panel.ClientSize.Width - 120), 24);
            Place(population, 108, fixedSummary.Bottom + 4, Math.Max(1, panel.ClientSize.Width - 120), MotherGroupConfirmGrid.PopulationAreaHeight());
            Place(detail, 12, population.Bottom + 8, Math.Max(1, panel.ClientSize.Width - 24), 96);

            int gridTop = detail.Bottom + 8;
            int gridHeight = previousButton.Top - 8 - gridTop;
            Place(students, 3, gridTop, Math.Max(1, panel.ClientSize.Width - 6), Math.Max(80, gridHeight));

            int reservedLeft = previousButton.Left;
            if (importButton != null)
                reservedLeft = Math.Min(reservedLeft, importButton.Left);
            if (exportButton != null)
                reservedLeft = Math.Min(reservedLeft, exportButton.Left);
            Place(restore, reservedLeft - 12 - 110, previousButton.Top, 110, previousButton.Height);
            if (addCaption == null || addNumber == null || addButton == null)
                return;
            int captionWidth = 48;
            int numberWidth = 110;
            int buttonWidth = 88;
            int buttonLeft = restore.Left - 8 - buttonWidth;
            int numberLeft = buttonLeft - 6 - numberWidth;
            int captionLeft = numberLeft - 4 - captionWidth;
            if (captionLeft < 8)
            {
                int deficit = 8 - captionLeft;
                numberWidth = Math.Max(60, numberWidth - deficit);
                captionLeft = 8;
                numberLeft = captionLeft + captionWidth + 4;
                buttonLeft = numberLeft + numberWidth + 6;
            }
            Place(addCaption, captionLeft, restore.Top, captionWidth, restore.Height);
            Place(addNumber, numberLeft, restore.Top, numberWidth, restore.Height);
            Place(addButton, buttonLeft, restore.Top, buttonWidth, restore.Height);
        }

        private static void Place(Control control, int x, int y, int width, int height)
        {
            control.Anchor = AnchorStyles.None;
            control.SetBounds(x, y, width, height);
        }
    }
}
