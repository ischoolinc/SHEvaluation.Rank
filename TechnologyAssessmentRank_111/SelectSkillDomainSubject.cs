using FISCA.Presentation.Controls;
using SHEvaluation.Rank.DAO;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SHEvaluation.Rank.TechnologyAssessmentRank_111
{
    public partial class SelectSkillDomainSubject : BaseForm
    {
        List<string> SubjectList = new List<string>();
        public string strSubjectArray = "";
        QueryData queryData = new QueryData();
        public SelectSkillDomainSubject()
        {
            InitializeComponent();
        }

        private void GetCheckedSubject()
        {
            for (int i = 0; i < dgData.Rows.Count; i++)
            {
                if (dgData.Rows[i].Cells[0].Value.ToString() == "True")
                    SubjectList.Add(dgData.Rows[i].Cells[1].Value.ToString().Trim());
            }

            strSubjectArray = String.Join(",", SubjectList.ToArray());
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            GetCheckedSubject();

        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void SelectSkillDomainSubject_Load(object sender, EventArgs e)
        {



        }

        private void btnSearh_Click(object sender, EventArgs e)
        {
            DataTable dt = queryData.GetSkillDomainSubject(iptSchoolYear.Value);
            //DataGridCell de;
            if (dt.Rows.Count > 0)
            {
                dgData.RowCount = dt.Rows.Count;

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    dgData.Rows[i].Cells[1].Value = dt.Rows[i].ItemArray[0].ToString().Trim();

                    //預設全部勾選
                    dgData.Rows[i].Cells[0].Value = true;
                }



            }
        }
    }
}
