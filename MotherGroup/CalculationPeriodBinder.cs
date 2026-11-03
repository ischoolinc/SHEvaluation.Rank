using System.Windows.Forms;

namespace SHEvaluation.Rank.MotherGroup
{
    public static class CalculationPeriodBinder
    {
        public static void Apply(
            NumericUpDown schoolYear,
            ComboBox semester,
            ComboBox source,
            NumericUpDown historySchoolYear,
            ComboBox historySemester,
            CalculationPeriod period)
        {
            schoolYear.Minimum = 1;
            schoolYear.Maximum = 9999;
            schoolYear.DecimalPlaces = 0;
            int yearNumber;
            if (!int.TryParse(period.SchoolYear, out yearNumber) || yearNumber < 1 || yearNumber > 9999)
                yearNumber = 1;
            schoolYear.Value = yearNumber;

            semester.DropDownStyle = ComboBoxStyle.DropDownList;
            semester.Items.Clear();
            semester.Items.Add("1");
            semester.Items.Add("2");
            semester.SelectedItem = period.Semester;

            source.DropDownStyle = ComboBoxStyle.DropDownList;
            source.Items.Clear();
            source.Items.Add(CalculationPeriod.CurrentSourceName);
            source.Items.Add(CalculationPeriod.HistorySourceName);
            source.Items.Add(CalculationPeriod.ExcelSourceName);
            source.SelectedIndex = 0;

            historySchoolYear.Enabled = false;
            historySchoolYear.Minimum = 1;
            historySchoolYear.Maximum = 9999;
            historySchoolYear.Value = yearNumber;
            historySemester.Enabled = false;
            historySemester.DropDownStyle = ComboBoxStyle.DropDownList;
            historySemester.Items.Clear();
            historySemester.Items.Add("1");
            historySemester.Items.Add("2");
            historySemester.SelectedItem = period.Semester;
            period.SetHistoryPeriod(yearNumber.ToString(), period.Semester);
        }

        public static void ReadCalculation(NumericUpDown schoolYear, ComboBox semester, CalculationPeriod period)
        {
            string selectedSemester = semester.SelectedItem == null ? "1" : semester.SelectedItem.ToString();
            period.SetCalculationPeriod(decimal.ToInt32(schoolYear.Value).ToString(), selectedSemester);
        }

        public static void RestoreSystemPeriod(NumericUpDown schoolYear, ComboBox semester, CalculationPeriod period, string systemSchoolYear, string systemSemester)
        {
            string semesterText = systemSemester == "2" ? "2" : "1";
            if (period != null)
                period.SetCalculationPeriod(systemSchoolYear, semesterText);
            if (schoolYear != null)
            {
                int yearNumber;
                if (!int.TryParse(systemSchoolYear, out yearNumber) || yearNumber < schoolYear.Minimum || yearNumber > schoolYear.Maximum)
                    yearNumber = decimal.ToInt32(schoolYear.Minimum);
                schoolYear.Value = yearNumber;
            }
            if (semester != null && semester.Items.Contains(semesterText))
                semester.SelectedItem = semesterText;
        }
    }
}
