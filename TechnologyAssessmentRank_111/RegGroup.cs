using FISCA.Presentation.Controls;
using System;
using System.Collections;
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
    public partial class RegGroupForm : BaseForm
    {
        public RegGroupForm(Dictionary<string, string> GroupCodeDic)
        {
            InitializeComponent();
            LoadData(GroupCodeDic);
        }

        private void LoadData(Dictionary<string, string> GroupCodeDic)
        {
            lstGroupCode.Items.Clear();
            foreach (string Group in GroupCodeDic.Keys)
                {
                ListViewItem lvi = new ListViewItem(Group);
                lvi.SubItems.Add(GroupCodeDic[Group]);
                lstGroupCode.Items.Add(lvi);
            }
            lstGroupCode.ListViewItemSorter = new ListViewItemComparer(0); //依代碼排序 
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            string GroupCode = "";
            SHEvaluation.Rank.TechnologyAssessmentRank_111.CalculateTechnologyAccessmentRankSetDeptGroup111 ctrdg = ( SHEvaluation.Rank.TechnologyAssessmentRank_111.CalculateTechnologyAccessmentRankSetDeptGroup111) this.Owner;
            for (int i=0;i<lstGroupCode.Items.Count;i++)
            {
                if (lstGroupCode.Items[i].Selected == true)
                    GroupCode = lstGroupCode.Items[i].Text;
            }
            
            ctrdg.StrValue = GroupCode;//傳回報名群組代碼
            this.Close();
        }
        class ListViewItemComparer : IComparer
        {
            private int col;
            public ListViewItemComparer()
            {
                col = 0;
            }
            public ListViewItemComparer(int column)
            {
                col = column;
            }
            public int Compare(object x, object y)
            {
                return String.Compare(((ListViewItem)x).SubItems[col].Text, ((ListViewItem)y).SubItems[col].Text);
            }
        }
    }
}
