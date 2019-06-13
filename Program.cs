using FISCA;
using FISCA.Presentation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SHEvaluation.Rank
{
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [MainMethod()]
        public static void Main()
        {
            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);
            //Application.Run(new CalculateRegularAssessmentRank());

            RibbonBarItem regularSchoolYearRank = MotherForm.RibbonBarItems["教務作業", "批次作業/檢視"];
            regularSchoolYearRank["成績排名"]["計算定期評量排名"].Enable = true;
            regularSchoolYearRank["成績排名"]["計算定期評量排名"].Click += delegate
            {
                CalculateRegularAssessmentRank cacluateRegularAssessmentRank = new CalculateRegularAssessmentRank();
                cacluateRegularAssessmentRank.ShowDialog();
            };

            RibbonBarItem semesterAssessmentRank = MotherForm.RibbonBarItems["教務作業", "批次作業/檢視"];
            semesterAssessmentRank["成績排名"]["計算學期成績固定排名"].Enable = true;
            semesterAssessmentRank["成績排名"]["計算學期成績固定排名"].Click += delegate
            {
                CalculateSemesterAssessmentRank calculateSemesterAssessmentRank = new CalculateSemesterAssessmentRank();
                calculateSemesterAssessmentRank.ShowDialog();
            };

            RibbonBarItem regularRank = FISCA.Presentation.MotherForm.RibbonBarItems["教務作業", "批次作業/檢視"];
            regularRank["成績排名"]["定期評量排名資料檢索"].Enable = true;
            regularRank["成績排名"]["定期評量排名資料檢索"].Click += delegate
            {
                RegularAssessmentRankSelect rankselect = new RegularAssessmentRankSelect();
                rankselect.ShowDialog();
            };

            RibbonBarItem semesterAssessmentSelect = FISCA.Presentation.MotherForm.RibbonBarItems["教務作業", "批次作業/檢視"];
            regularRank["成績排名"]["學期成績排名資料檢索"].Enable = true;
            regularRank["成績排名"]["學期成績排名資料檢索"].Click += delegate
            {
                SemesterAssessmentRankSelect semesterAssessmentRankSelect = new SemesterAssessmentRankSelect();
                semesterAssessmentRankSelect.ShowDialog();
            };
        }
    }
}
