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
using SHEvaluation.Rank.DAO;
using SHEvaluation.Rank.UDT;

namespace SHEvaluation.Rank
{
    public partial class CalculateTechnologyAssessmentRankSetDeptGroup : BaseForm
    {
        QueryData qd = new QueryData();
        Dictionary<string, string> DeptNameIDDict = new Dictionary<string, string>();
        Dictionary<string, string> DeptIDNameDict = new Dictionary<string, string>();
        List<udtRegistrationDept> RegistrationDeptList = new List<udtRegistrationDept>();
        AccessHelper accessHelper = new AccessHelper();

        public CalculateTechnologyAssessmentRankSetDeptGroup()
        {
            InitializeComponent();
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void CalculateTechnologyAssessmentRankSetDeptGroup_Load(object sender, EventArgs e)
        {
            LoadData();
        }

        private void LoadData()
        {
            btnSave.Enabled = dgData.Enabled = false;

            DeptIDNameDict.Clear();
            DeptNameIDDict.Clear();
            RegistrationDeptList.Clear();

            // 取得資料庫內目前科別
            DataTable dt = qd.GetDeptInfoDT();

            foreach (DataRow dr in dt.Rows)
            {
                string id = dr["id"].ToString();
                string name = dr["name"].ToString();

                if (!DeptNameIDDict.ContainsKey(name))
                    DeptNameIDDict.Add(name, id);

                if (!DeptIDNameDict.ContainsKey(id))
                    DeptIDNameDict.Add(id, name);
            }

            // 取得UDT 資料
            RegistrationDeptList = accessHelper.Select<udtRegistrationDept>();
            List<string> tmpID = new List<string>();

            foreach (udtRegistrationDept data in RegistrationDeptList)
                tmpID.Add(data.RefDeptID);

            // 多於刪除
            foreach (udtRegistrationDept data in RegistrationDeptList)
            {
                if (!DeptIDNameDict.ContainsKey(data.RefDeptID))
                    data.Deleted = true;
            }

            // 沒有資料加入
            foreach (string key in DeptIDNameDict.Keys)
            {
                if (!tmpID.Contains(key))
                {
                    udtRegistrationDept data = new udtRegistrationDept();
                    data.DeptName = DeptIDNameDict[key];
                    data.RefDeptID = key;
                    RegistrationDeptList.Add(data);
                }
            }

            RegistrationDeptList.SaveAll();

            // 載入畫面
            dgData.Rows.Clear();
            foreach (udtRegistrationDept data in RegistrationDeptList)
            {
                int rowIdx = dgData.Rows.Add();
                dgData.Rows[rowIdx].Tag = data;
                dgData.Rows[rowIdx].Cells[colDeptName.Index].Value = data.DeptName;
                dgData.Rows[rowIdx].Cells[colRefDeptName.Index].Value = data.RegDeptName;
                dgData.Rows[rowIdx].Cells[colGroupCode.Index].Value = data.RegGroupCode;
                dgData.Rows[rowIdx].Cells[colGroupName.Index].Value = data.RegGroupName;
            }

            lblMsg.Text = "共 " + dgData.Rows.Count + " 筆";
            btnSave.Enabled = dgData.Enabled = true;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            List<udtRegistrationDept> dataList = new List<udtRegistrationDept>();
            try
            {
                foreach (DataGridViewRow drv in dgData.Rows)
                {
                    udtRegistrationDept data = drv.Tag as udtRegistrationDept;
                    if (drv.Cells[colDeptName.Index].Value == null)
                    {
                        data.DeptName = "";
                    }
                    else
                    {
                        data.DeptName = drv.Cells[colDeptName.Index].Value.ToString();
                    }

                    if (drv.Cells[colRefDeptName.Index].Value == null)
                    {
                        data.RegDeptName = "";
                    }
                    else
                    {
                        data.RegDeptName = drv.Cells[colRefDeptName.Index].Value.ToString();
                    }

                    if (drv.Cells[colGroupCode.Index].Value == null)
                    {
                        data.RegGroupCode = "";
                    }
                    else
                    {
                        data.RegGroupCode = drv.Cells[colGroupCode.Index].Value.ToString();
                    }

                    if (drv.Cells[colGroupName.Index].Value == null)
                    {
                        data.RegGroupName = "";
                    }
                    else
                    {
                        data.RegGroupName = drv.Cells[colGroupName.Index].Value.ToString();
                    }

                    dataList.Add(data);
                }

                dataList.SaveAll();

                MsgBox.Show("儲存完成");
                this.Close();
            }
            catch (Exception ex)
            {
                MsgBox.Show("儲存資料發生錯誤," + ex.Message);
            }


        }

        /// <summary>
        /// 檢查資料
        /// </summary>
        /// <returns></returns>
        private bool CheckData()
        {
            bool value = true;
            foreach (DataGridViewRow drv in dgData.Rows)
            {
                foreach (DataGridViewCell cell in drv.Cells)
                {
                    if (cell.Value == null)
                    {
                        MsgBox.Show("");
                        return false;
                    }
                }
            }

            return value;
        }
    }
}
