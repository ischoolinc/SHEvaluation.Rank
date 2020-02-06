using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using FISCA.Presentation.Controls;

namespace SHEvaluation.Rank
{
    public partial class CalculateTechnologyAssessmentRankStep1 : BaseForm
    {
        public CalculateTechnologyAssessmentRankStep1()
        {
            InitializeComponent();
        }

        private void btnNext_Click(object sender, EventArgs e)
        {

        }

        private void btnSetDeptGroup_Click(object sender, EventArgs e)
        {
            btnSetDeptGroup.Enabled = false;
            CalculateTechnologyAssessmentRankSetDeptGroup ctrdg = new CalculateTechnologyAssessmentRankSetDeptGroup();
            ctrdg.ShowDialog();
            btnSetDeptGroup.Enabled = true;
        }

        private void btnSetSubject_Click(object sender, EventArgs e)
        {

        }

        private void CalculateTechnologyAssessmentRankStep1_Load(object sender, EventArgs e)
        {
            this.MaximumSize = this.MinimumSize = this.Size;
        }
    }
}
