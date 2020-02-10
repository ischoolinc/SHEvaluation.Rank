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
using K12.Data;
using SHEvaluation.Rank.DAO;
using FISCA.Data;
using FISCA.UDT;
using SHEvaluation.Rank.UDT;

namespace SHEvaluation.Rank
{
    public partial class CalculateTechnologyAssessmentRankStep1 : BaseForm
    {
        List<TagConfigRecord> _TagList = new List<TagConfigRecord>();
        List<StudentRecord> _StudentList = new List<StudentRecord>();
        List<string> StudentIDListAll = new List<string>();
        DataTable deptDataTable = new DataTable();
        Dictionary<string, string> StudentDeptNameDict = new Dictionary<string, string>();
        Exception bkwException = null;
        List<StudentRecord> _StudentFilterList = new List<StudentRecord>();
        BackgroundWorker _bgWorkerLoadDefaultData = new BackgroundWorker();
        BackgroundWorker _bgWorkerStep1Next = new BackgroundWorker();
        Dictionary<string, List<GradeYearSemesterInfo>> StudGradeYearSemsDict = new Dictionary<string, List<GradeYearSemesterInfo>>();
        string studentFilter = "";
        string studentTag1 = "";
        string studentTag2 = "";
        int parseNumber = 0;

        CalculateTechnologyAssessmentRankStep2 ctrr2 = new CalculateTechnologyAssessmentRankStep2();

        public CalculateTechnologyAssessmentRankStep1()
        {
            InitializeComponent();
            _bgWorkerLoadDefaultData.DoWork += _bgWorkerLoadDefaultData_DoWork;
            _bgWorkerLoadDefaultData.RunWorkerCompleted += _bgWorkerLoadDefaultData_RunWorkerCompleted;
            _bgWorkerLoadDefaultData.ProgressChanged += _bgWorkerLoadDefaultData_ProgressChanged;
            _bgWorkerLoadDefaultData.WorkerReportsProgress = true;

            _bgWorkerStep1Next.DoWork += _bgWorkerStep1Next_DoWork;
            _bgWorkerStep1Next.RunWorkerCompleted += _bgWorkerStep1Next_RunWorkerCompleted;
            _bgWorkerStep1Next.ProgressChanged += _bgWorkerStep1Next_ProgressChanged;
            _bgWorkerStep1Next.WorkerReportsProgress = true;
        }

        private void _bgWorkerStep1Next_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            FISCA.Presentation.MotherForm.SetStatusBarMessage("讀取學生清單", e.ProgressPercentage);
        }

        private void _bgWorkerStep1Next_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

            if (bkwException != null)
            {
                throw new Exception("學生列表讀取失敗", bkwException);
            }
            if (_StudentFilterList.Count == 0)
            {
                MessageBox.Show("沒有找到符合條件的學生");
                this.Visible = true;
                btnNext.Enabled = true;
                return;
            }



            #region 整理學生類別
            List<StudentTagRecord> tag1Student = new List<StudentTagRecord>();
            List<StudentTagRecord> tag2Student = new List<StudentTagRecord>();
            if (!string.IsNullOrEmpty(studentTag1))
            {
                List<string> tag1IDs = _TagList.Where(x => x.Prefix == studentTag1).Select(x => x.ID).ToList();
                if (tag1IDs.Count == 0)
                {
                    tag1IDs = _TagList.Where(x => x.Name == studentTag1).Select(x => x.ID).ToList();
                }
                tag1Student = StudentTag.SelectAll().Where(x => tag1IDs.Contains(x.RefTagID)).ToList();
            }
            if (!string.IsNullOrEmpty(studentTag2))
            {
                List<string> tag2IDs = _TagList.Where(x => x.Prefix == studentTag2).Select(x => x.ID).ToList();
                if (tag2IDs.Count == 0)
                {
                    tag2IDs = _TagList.Where(x => x.Name == studentTag2).Select(x => x.ID).ToList();
                }
                tag2Student = StudentTag.SelectAll().Where(x => tag2IDs.Contains(x.RefTagID)).ToList();
            }
            #endregion

            ctrr2.SetStudentDataList(_StudentFilterList, tag1Student, tag2Student, StudentDeptNameDict);

            ctrr2.StartPosition = FormStartPosition.CenterScreen;
            if (ctrr2.ShowDialog() == DialogResult.Cancel)
            {
                FISCA.Presentation.MotherForm.SetStatusBarMessage("");
                ctrr2.Visible = false;
                this.Visible = true;
                this.StartPosition = FormStartPosition.CenterScreen;
                this.UserControlEnale(true);
            }
        }

        private void _bgWorkerStep1Next_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(studentFilter))
                {
                    List<string> studentFilterTagIDs = _TagList.Where(x => x.Prefix == studentFilter).Select(x => x.ID).ToList();
                    if (studentFilterTagIDs.Count == 0)
                    {
                        studentFilterTagIDs = _TagList.Where(x => x.Name == studentFilter).Select(x => x.ID).ToList();
                    }
                    List<string> studentIDList = StudentTag.SelectAll().Where(x => studentFilterTagIDs.Contains(x.RefTagID)).Select(x => x.RefStudentID).ToList();
                    _StudentFilterList = _StudentFilterList.Where(x => !studentIDList.Contains(x.ID)).ToList();
                }

                #region 取得篩選學生的科別
                string studentIDs = string.Join(",", _StudentFilterList.Select(x => x.ID).ToList());

                #region 取得科別的SQL
                string queryDeptSQL = @"
SELECT
student.id AS student_id
, COALESCE(dept.name, '') AS deptname
FROM
student
LEFT OUTER JOIN class
ON class.id = student.ref_class_id
LEFT OUTER JOIN dept
ON dept.id = COALESCE(student.ref_dept_id, class.ref_dept_id)
WHERE
student.id IN
(
" + studentIDs + @"
)
";
                #endregion
                if (studentIDs.Count() > 0)
                {
                    StudentDeptNameDict.Clear();
                    QueryHelper queryHelper = new QueryHelper();
                    deptDataTable = queryHelper.Select(queryDeptSQL);
                    foreach (DataRow dr in deptDataTable.Rows)
                    {
                        string sid = dr["student_id"].ToString();
                        if (!StudentDeptNameDict.ContainsKey(sid))
                        {
                            StudentDeptNameDict.Add(sid, dr["deptname"].ToString());
                        }
                    }
                }


                #endregion

                AccessHelper accessHelper = new AccessHelper();

                Dictionary<string, udtRegistrationDept> tmpDeptDict = new Dictionary<string, udtRegistrationDept>();
                // 設定群別 key: DeptName
                List<udtRegistrationDept> udtDeptList = accessHelper.Select<udtRegistrationDept>();
                foreach (udtRegistrationDept data in udtDeptList)
                {
                    if (!tmpDeptDict.ContainsKey(data.DeptName))
                        tmpDeptDict.Add(data.DeptName, data);
                }
                ctrr2.SetRegistrationDept(tmpDeptDict);

                // 設定科目 key: SubjectName
                Dictionary<string, udtRegistrationSubject> tmpSubjDict = new Dictionary<string, udtRegistrationSubject>();

                List<udtRegistrationSubject> udtSubjList = accessHelper.Select<udtRegistrationSubject>();

                foreach (udtRegistrationSubject data in udtSubjList)
                {
                    if (!tmpSubjDict.ContainsKey(data.SubjectName))
                        tmpSubjDict.Add(data.SubjectName, data);
                }
                ctrr2.SetRegistrationSubject(tmpSubjDict);
            }
            catch (Exception ex)
            {
                bkwException = ex;
            }
        }

        private void _bgWorkerLoadDefaultData_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            FISCA.Presentation.MotherForm.SetStatusBarMessage("預設資料讀取中...", e.ProgressPercentage);
        }

        private void _bgWorkerLoadDefaultData_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            cboStudentFilter.Items.Clear();
            cboStudentTag1.Items.Clear();
            cboStudentTag2.Items.Clear();
            cboStudentFilter.Items.Add("");
            cboStudentTag1.Items.Add("");
            cboStudentTag2.Items.Add("");
            foreach (string tagName in _TagList.Select(x => x.Prefix).Distinct().ToList())
            {
                if (!string.IsNullOrEmpty(tagName))
                {
                    cboStudentFilter.Items.Add("[" + tagName + "]");
                    cboStudentTag1.Items.Add("[" + tagName + "]");
                    cboStudentTag2.Items.Add("[" + tagName + "]");
                }
            }
            foreach (string tagName in _TagList.Where(x => string.IsNullOrEmpty(x.Prefix)).Select(x => x.Name).ToList())
            {
                cboStudentFilter.Items.Add(tagName);
                cboStudentTag1.Items.Add(tagName);
                cboStudentTag2.Items.Add(tagName);
            }

            UserControlEnale(true);
            FISCA.Presentation.MotherForm.SetStatusBarMessage("");
        }

        private void _bgWorkerLoadDefaultData_DoWork(object sender, DoWorkEventArgs e)
        {
            LoadDefaultData();
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            this.Visible = false;
            btnNext.Enabled = false;
            FISCA.Presentation.MotherForm.SetStatusBarMessage("資料處理中...");

            studentFilter = cboStudentFilter.Text.Trim('[', ']');
            studentTag1 = cboStudentTag1.Text.Trim('[', ']');
            studentTag2 = cboStudentTag2.Text.Trim('[', ']');

            ctrr2.SetButtonEnable(false);
            ctrr2.SetSelStudentTag(studentTag1, studentTag2, studentFilter);
            ctrr2.SetParseNumber(parseNumber);
            ctrr2.SetStudGradeYearSems(StudGradeYearSemsDict);

            _bgWorkerStep1Next.RunWorkerAsync();


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
            btnSetSubject.Enabled = false;
            CalculateTechnologyAssessmentRankSetSubject ctrss = new CalculateTechnologyAssessmentRankSetSubject();
            ctrss.ShowDialog();
            btnSetSubject.Enabled = true;
        }

        private void CalculateTechnologyAssessmentRankStep1_Load(object sender, EventArgs e)
        {
            this.MaximumSize = this.MinimumSize = this.Size;
            this.StartPosition = FormStartPosition.CenterScreen;
            UserControlEnale(false);

            _bgWorkerLoadDefaultData.RunWorkerAsync();



        }

        private void LoadDefaultData()
        {
            _bgWorkerLoadDefaultData.ReportProgress(1);
            StudentIDListAll.Clear();
            // 三年級一般生
            _StudentList = K12.Data.Student.SelectAll().Where(x => (x.Status == StudentRecord.StudentStatus.一般) && !string.IsNullOrEmpty(x.RefClassID) && x.Class.GradeYear != null && x.Class.GradeYear.Value == 3).ToList();
            _bgWorkerLoadDefaultData.ReportProgress(30);
            _TagList = K12.Data.TagConfig.SelectByCategory(TagCategory.Student);

            _bgWorkerLoadDefaultData.ReportProgress(50);
            foreach (StudentRecord rec in _StudentList)
                StudentIDListAll.Add(rec.ID);

            QueryData qd = new QueryData();

            // 取得學生成績年級學生
            StudGradeYearSemsDict = qd.GetScoreGradeSemesterByGradeYear(StudentIDListAll);

            _bgWorkerLoadDefaultData.ReportProgress(70);

            // 過濾不排名名單與成績不滿5學期
            _StudentFilterList.Clear();
            List<string> scoreErrIDList = new List<string>();

            // 學期成績未滿5學期不能使用
            foreach (string id in StudGradeYearSemsDict.Keys)
            {
                if (StudGradeYearSemsDict[id].Count < 5)
                    scoreErrIDList.Add(id);
            }

            foreach (StudentRecord rec in _StudentList)
            {
                if (!scoreErrIDList.Contains(rec.ID))
                    _StudentFilterList.Add(rec);
            }

            _bgWorkerLoadDefaultData.ReportProgress(100);

        }

        private void UserControlEnale(bool value)
        {
            btnNext.Enabled = value;
            btnSetDeptGroup.Enabled = value;
            btnSetSubject.Enabled = value;
            cboStudentFilter.Enabled = value;
            cboStudentTag1.Enabled = value;
            cboStudentTag2.Enabled = value;

        }

        private void cboStudentFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboStudentFilter.Text == cboStudentTag1.Text || cboStudentFilter.Text == cboStudentTag2.Text)
            {
                cboStudentFilter.Text = "";
            }
        }

        private void cboStudentTag1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboStudentTag1.Text == cboStudentFilter.Text || cboStudentTag1.Text == cboStudentTag2.Text)
            {
                cboStudentTag1.Text = "";
            }
            if (cboStudentTag1.Text != "")
            {
                cboStudentTag2.Enabled = true;
            }
            else
            {
                cboStudentTag2.Text = "";
                cboStudentTag2.Enabled = false;
            }
        }

        private void cboStudentTag2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboStudentTag2.Text == cboStudentFilter.Text || cboStudentTag2.Text == cboStudentTag1.Text)
            {
                cboStudentTag2.Text = "";
            }
        }

        private void iptParseNum_ValueChanged(object sender, EventArgs e)
        {
            parseNumber = iptParseNum.Value;
        }
    }
}
