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
using FISCA.UDT;
using SHEvaluation.Rank.TechnologyAssessmentRank_111;
using SHEvaluation.Rank.UDT;

namespace SHEvaluation.Rank
{
    public partial class CalculateTechnologyAssessmentRankSetSubject_111 : BaseForm
    {
        AccessHelper accessHelper = new AccessHelper();

        // 國文、英文、數學、技能領域對照
        public CalculateTechnologyAssessmentRankSetSubject_111()
        {
            InitializeComponent();
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void CalculateTechnologyAssessmentRankSetSubject_Load(object sender, EventArgs e)
        {
            LoadData();
        }

        private void LoadData()
        {
            dgData.Rows.Clear();
            // 讀取 UDT 資料
            List<udtRegistrationSubject> SubjectList = accessHelper.Select<udtRegistrationSubject>();

            Dictionary<string, udtRegistrationSubject> dataDict = new Dictionary<string, udtRegistrationSubject>();

            foreach (udtRegistrationSubject data in SubjectList)
            {
                if (!dataDict.ContainsKey(data.SubjectName))
                    dataDict.Add(data.SubjectName, data);
            }

            // 加入國英數
            List<string> sNameList = new List<string>();
            sNameList.Add("國文");
            sNameList.Add("英文");
            sNameList.Add("數學");
            sNameList.Add("技能領域");

            foreach (string name in sNameList)
            {
                int rowIdx = dgData.Rows.Add();
                dgData.Rows[rowIdx].Cells[colSubjName.Index].Value = name;

                if (dataDict.ContainsKey(name))
                {
                    dgData.Rows[rowIdx].Tag = dataDict[name];
                    dgData.Rows[rowIdx].Cells[colSubj_1a.Index].Value = dataDict[name].Subj1A;
                    dgData.Rows[rowIdx].Cells[colSubj_1b.Index].Value = dataDict[name].Subj1B;
                    dgData.Rows[rowIdx].Cells[colSubj_2a.Index].Value = dataDict[name].Subj2A;
                    dgData.Rows[rowIdx].Cells[colSubj_2b.Index].Value = dataDict[name].Subj2B;
                    dgData.Rows[rowIdx].Cells[colSubj_3a.Index].Value = dataDict[name].Subj3A;
                }
                else
                {
                    // 沒有資料新增
                    udtRegistrationSubject data = new udtRegistrationSubject();
                    data.SubjectName = name;
                    dgData.Rows[rowIdx].Tag = data;
                }
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            Save();
        }

        private void Save()
        {
            btnSave.Enabled = false;
            try
            {
                List<udtRegistrationSubject> dataList = new List<udtRegistrationSubject>();
                foreach (DataGridViewRow drv in dgData.Rows)
                {
                    udtRegistrationSubject data = drv.Tag as udtRegistrationSubject;
                    if (data != null)
                    {
                        foreach (DataGridViewCell cell in drv.Cells)
                        {
                            if (cell.ColumnIndex != colSubjName.Index)
                            {

                                if (cell.Value != null && cell.Value.ToString() != "")
                                {
                                    if (cell.ColumnIndex == colSubj_1a.Index)
                                        data.Subj1A = cell.Value.ToString().Trim();

                                    if (cell.ColumnIndex == colSubj_1b.Index)
                                        data.Subj1B = cell.Value.ToString().Trim();

                                    if (cell.ColumnIndex == colSubj_2a.Index)
                                        data.Subj2A = cell.Value.ToString().Trim();

                                    if (cell.ColumnIndex == colSubj_2b.Index)
                                        data.Subj2B = cell.Value.ToString().Trim();

                                    if (cell.ColumnIndex == colSubj_3a.Index)
                                        data.Subj3A = cell.Value.ToString().Trim();
                                }
                            }
                        }
                        dataList.Add(data);
                    }
                }

                dataList.SaveAll();
                MsgBox.Show("儲存完成。");
                this.Close();
            }
            catch (Exception ex)
            {
                MsgBox.Show("儲存失敗，" + ex.Message);
            }
            btnSave.Enabled = true;

        }

        private void btnSelectSkillDomainSubject_Click(object sender, EventArgs e)
        {
            btnSelectSkillDomainSubject.Enabled = false;
            SelectSkillDomainSubject ssds = new SelectSkillDomainSubject();

            if (ssds.ShowDialog() == DialogResult.Yes)
            {
                foreach (DataGridViewRow dr in dgData.Rows)
                {
                    if (dr.Cells[colSubjName.Index].Value.ToString() == "技能領域")
                    {
                        dr.Cells[colSubj_1a.Index].Value = ssds.strSubjectArray;
                        dr.Cells[colSubj_1b.Index].Value = ssds.strSubjectArray;
                        dr.Cells[colSubj_2a.Index].Value = ssds.strSubjectArray;
                        dr.Cells[colSubj_2b.Index].Value = ssds.strSubjectArray;
                        dr.Cells[colSubj_3a.Index].Value = ssds.strSubjectArray;
                    }
                }

            }
            btnSelectSkillDomainSubject.Enabled = true;
        }
    }
}
