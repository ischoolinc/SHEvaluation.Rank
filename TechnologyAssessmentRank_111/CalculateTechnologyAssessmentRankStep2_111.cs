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
using SHEvaluation.Rank.UDT;
using System.Xml;
using SHEvaluation.Rank.DAO;
using FISCA.Data;
using Aspose.Cells;
using System.IO;
using FISCA.UDT;

namespace SHEvaluation.Rank
{
    public partial class CalculateTechnologyAssessmentRankStep2_111 : BaseForm
    {
        // DTO 類別：用於在 UI thread 和 BackgroundWorker 之間傳遞學生資料
        private class RankStudentDto
        {
            public string StudentID { get; set; }
            public string StudentName { get; set; }
            public string RankGradeYearText { get; set; }
            public string DeptName { get; set; }
            public string ClassName { get; set; }
            public string Tag1 { get; set; }
            public string Tag2 { get; set; }
            public string RegGroupName { get; set; }
            public string RegGroupCode { get; set; }
        }

        // SQL 字串轉義方法：處理單引號避免 SQL 語法錯誤
        private string SqlLiteral(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "''";
            return "'" + value.Replace("'", "''") + "'";
        }

        // 全部被選學生
        List<StudentRecord> StudentAllList = new List<StudentRecord>();

        List<StudentRecord> StudentFilterList = new List<StudentRecord>();
        List<string> RankStudentIDList = new List<string>();
        List<StudentTagRecord> Tag1Student = new List<StudentTagRecord>();
        List<StudentTagRecord> Tag2Student = new List<StudentTagRecord>();

        // 不被列入排名清單
        Dictionary<string, string> StudentNotRankDict = new Dictionary<string, string>();


        Dictionary<string, List<GradeYearSemesterInfo>> StudGradeYearSemsDict = new Dictionary<string, List<GradeYearSemesterInfo>>();

        List<GradeYearSemesterInfo> calSemesterList = new List<GradeYearSemesterInfo>();

        Dictionary<string, Dictionary<string, string>> SubjectNameMapDict = new Dictionary<string, Dictionary<string, string>>();

        string SelStudentTag1 = "";
        string SelStudentTag2 = "";
        string SelStudentFilter = "";

        int parseNumber = 0;

        // 學生群別
        Dictionary<string, udtRegistrationDept> StudentGroupDict = new Dictionary<string, udtRegistrationDept>();

        // 群別
        Dictionary<string, udtRegistrationDept> RegistrationDeptDict = new Dictionary<string, udtRegistrationDept>();

        // 科目
        Dictionary<string, List<udtRegistrationSubjectNew>> RegistrationSubjectDict = new Dictionary<string, List<udtRegistrationSubjectNew>>();

        public CalculateTechnologyAssessmentRankStep2_111()
        {
            InitializeComponent();
        }

        private void btnPrevious_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }

        public void SetButtonEnable(bool value)
        {
            this.btnPrevious.Enabled = value;
            this.btnCacluate.Enabled = value;
        }

        public void SetParseNumber(int num)
        {
            parseNumber = num;
        }

        public void SetStudGradeYearSems(Dictionary<string, List<GradeYearSemesterInfo>> data)
        {
            StudGradeYearSemsDict = data;
        }

        public void SetRegistrationDept(Dictionary<string, udtRegistrationDept> data)
        {
            RegistrationDeptDict = data;
        }
        public void SetRegistrationSubject(Dictionary<string, List<udtRegistrationSubjectNew> > data)
        {

            RegistrationSubjectDict = data;
        }

        private string parseString1(string str)
        {
            string val = "";
            str = str.Trim();
            string[] aa = str.Split(',');
            if (aa.Count() > 0)
            {
                val = "'" + string.Join("','", aa.ToArray()) + "'";
            }

            return val;
        }


        public void SetSelStudentTag(string tag1, string tag2, string flt)
        {
            SelStudentTag1 = tag1;
            SelStudentTag2 = tag2;
            SelStudentFilter = flt;
        }

        public void SetStudentDataList(List<StudentRecord> StudList, List<StudentTagRecord> tagList1, List<StudentTagRecord> tagList2, Dictionary<string, string> deptDict, List<string> scoreErrorIDs, List<StudentRecord> studentAllList, List<string> studentFilterIDs)
        {
            StudentFilterList = StudList;
            StudentAllList = studentAllList;
            Tag1Student = tagList1;
            Tag2Student = tagList2;
            studentFilterIDs = studentFilterIDs.Distinct().ToList();

            // 沒有群別
            List<string> noGroupIDList = new List<string>();

            #region 整理學生基本資料
            var studentViewList = (from student in StudentFilterList
                                   select new
                                   {
                                       studentID = student.ID,
                                       studentClass = student.Class.Name,
                                       studentSeatNo = student.SeatNo,
                                       studentNumber = student.StudentNumber,
                                       studentName = student.Name,
                                       RankGradeYear = "" + student.Class.GradeYear + "年級",
                                       RankClassName = student.Class.Name
                                   }).ToList();
            #endregion

            #region 將資料填入dataGridView
            List<DataGridViewRow> rowList = new List<DataGridViewRow>();

            // 學生群索引
            StudentGroupDict.Clear();
            // 畫面上學生
            dgvStudentList.Rows.Clear();
            // 需要排名學生ID
            RankStudentIDList.Clear();
            // 不排名
            StudentNotRankDict.Clear();

            bool hasGroup = false;
            foreach (var student in studentViewList)
            {
                hasGroup = false;
                string tag1 = "", tag2 = "";
                DataGridViewRow row = new DataGridViewRow();
                row.CreateCells(dgvStudentList);
                row.Tag = student.studentID;
                row.Cells[colClass.Index].Value = student.studentClass;
                row.Cells[colSeatNum.Index].Value = student.studentSeatNo;
                row.Cells[colStudentNum.Index].Value = student.studentNumber;
                row.Cells[colStudentName.Index].Value = student.studentName;
                row.Cells[colSchoolRank.Index].Value = student.RankGradeYear;

                if (deptDict.ContainsKey(student.studentID))
                {

                    if (RegistrationDeptDict.ContainsKey(deptDict[student.studentID]))
                    {
                        row.Cells[colRegGroup.Index].Value = RegistrationDeptDict[deptDict[student.studentID]].RegGroupName;

                        row.Cells[colDeptName.Index].Value = RegistrationDeptDict[deptDict[student.studentID]].DeptName;
                        
                        row.Cells[colRegGroupCode.Index].Value = RegistrationDeptDict[deptDict[student.studentID]].RegGroupCode;
                        if (!string.IsNullOrEmpty(RegistrationDeptDict[deptDict[student.studentID]].RegGroupName))
                            hasGroup = true;

                        if (!StudentGroupDict.ContainsKey(student.studentID))
                            StudentGroupDict.Add(student.studentID, RegistrationDeptDict[deptDict[student.studentID]]);
                    }
                }

                row.Cells[colClassRank.Index].Value = student.RankClassName;
                if (Tag1Student.Where(x => x.RefStudentID == student.studentID).Count() > 0)
                {
                    tag1 = Tag1Student.First(x => x.RefStudentID == student.studentID).Name;
                    row.Cells[colRankType1.Index].Value = tag1;
                }
                if (Tag2Student.Where(x => x.RefStudentID == student.studentID).Count() > 0)
                {
                    tag2 = Tag2Student.First(x => x.RefStudentID == student.studentID).Name;
                    row.Cells[colRankType2.Index].Value = tag2;
                }

                // 有群別再加入
                if (hasGroup)
                {
                    RankStudentIDList.Add(student.studentID);
                    rowList.Add(row);
                }
                else
                {
                    if (!noGroupIDList.Contains(student.studentID))
                        noGroupIDList.Add(student.studentID);
                }

            }

            dgvStudentList.Rows.AddRange(rowList.ToArray());
            #endregion

            lblMsg.Text = "共 " + dgvStudentList.Rows.Count + " 位學生";

            // 整理不排名清單
            // 先暫時註解未滿5學期限制
            //foreach (string id in scoreErrorIDs)
            //{
            //    if (!StudentNotRankDict.ContainsKey(id))
            //        StudentNotRankDict.Add(id, "學期科目成績未滿5學期");
            //}
            foreach (string id in noGroupIDList)
            {
                if (!StudentNotRankDict.ContainsKey(id))
                    StudentNotRankDict.Add(id, "沒有報名群");
                else
                    StudentNotRankDict[id] += ",沒有報名群";
            }


            foreach (string id in studentFilterIDs)
            {
                if (!StudentNotRankDict.ContainsKey(id))
                    StudentNotRankDict.Add(id, "設定不排名學生");
                else
                    StudentNotRankDict[id] += ",設定不排名學生";
            }

            btnPrevious.Enabled = true;
            btnCacluate.Enabled = true;
            FISCA.Presentation.MotherForm.SetStatusBarMessage("學生列表讀取完成");
        }

        private void CalculateTechnologyAssessmentRankStep2_Load(object sender, EventArgs e)
        {
            this.StartPosition = FormStartPosition.CenterScreen;
            pbLoading.Visible = false;
            calSemesterList.Clear();
            for (int i = 1; i <= 3; i++)
            {
                for (int j = 1; j <= 2; j++)
                {
                    if (i == 3 && j == 2)
                        continue;

                    GradeYearSemesterInfo gs = new GradeYearSemesterInfo();
                    gs.GradeYear = i;
                    gs.Semester = j;
                    calSemesterList.Add(gs);
                }
            }
        }

        private void btnCacluate_Click(object sender, EventArgs e)
        {
            btnCacluate.Enabled = false;
            btnPrevious.Enabled = false;
            
            // Todo 1-1 & 3-1: 在 UI thread 先將 dgvStudentList 的資料複製為 DTO list
            var rankStudents = new List<RankStudentDto>();
            foreach (DataGridViewRow row in dgvStudentList.Rows)
            {
                if (row.IsNewRow) continue;

                var dto = new RankStudentDto
                {
                    StudentID = "" + row.Tag,
                    StudentName = "" + row.Cells[colStudentName.Index].Value,
                    RankGradeYearText = "" + row.Cells[colSchoolRank.Index].Value,
                    DeptName = "" + row.Cells[colDeptName.Index].Value,
                    ClassName = "" + row.Cells[colClassRank.Index].Value,
                    Tag1 = "" + row.Cells[colRankType1.Index].Value,
                    Tag2 = "" + row.Cells[colRankType2.Index].Value,
                    RegGroupName = "" + row.Cells[colRegGroup.Index].Value,
                    RegGroupCode = "" + row.Cells[colRegGroupCode.Index].Value
                };

                rankStudents.Add(dto);
            }

            // Todo 3-1: 檢查是否有學生可計算
            if (rankStudents.Count == 0)
            {
                MsgBox.Show("沒有可供計算的學生。");
                btnCacluate.Enabled = true;
                btnPrevious.Enabled = true;
                return;
            }

            Dictionary<string, Dictionary<string, List<string>>> RegGroupSubjMapDic =new Dictionary<string, Dictionary<string, List<string>>>();
            Dictionary<string, Dictionary<string, List<string>>> RegGroupCalcMapDic = new Dictionary<string, Dictionary<string, List<string>>>();
            BackgroundWorker bkw = new BackgroundWorker();
            bkw.WorkerReportsProgress = true;
            pbLoading.Visible = true;
            
            bkw.ProgressChanged += delegate (object obj, ProgressChangedEventArgs eventArgs)
            {
                FISCA.Presentation.MotherForm.SetStatusBarMessage("計算排名中", eventArgs.ProgressPercentage);
            };
            // 讀取 科目設定UDT 資料
            AccessHelper accessHelper = new AccessHelper();
            List<udtRegistrationSubjectNew> SubjectList = new List<udtRegistrationSubjectNew>();
            SubjectList = accessHelper.Select<udtRegistrationSubjectNew>();
            Dictionary<string, Dictionary<string, List<udtRegistrationSubjectNew>>> DataDict = new Dictionary<string, Dictionary<string, List<udtRegistrationSubjectNew>>>();
            foreach (udtRegistrationSubjectNew data in SubjectList)
            {
                if (!DataDict.ContainsKey(data.RegGroupName))
                {
                    DataDict.Add(data.RegGroupName, new Dictionary<string, List<udtRegistrationSubjectNew>>());
                    if (!DataDict[data.RegGroupName].ContainsKey(data.CalcName))
                        {
                        DataDict[data.RegGroupName].Add(data.CalcName, new List<udtRegistrationSubjectNew>());
                        DataDict[data.RegGroupName][data.CalcName].Add(data);
                    }
                    else
                        DataDict[data.RegGroupName][data.CalcName].Add(data);

                }
                else
                {
                    if (!DataDict[data.RegGroupName].ContainsKey(data.CalcName))
                        {
                        DataDict[data.RegGroupName].Add(data.CalcName, new List<udtRegistrationSubjectNew>());
                        DataDict[data.RegGroupName][data.CalcName].Add(data);
                    }
                    else
                        DataDict[data.RegGroupName][data.CalcName].Add(data);

                }
            }
            

            bkw.DoWork += delegate (object bwSender, DoWorkEventArgs bwE)
            {
                // Todo 1-2 & 3-2: 從 bwE.Argument 取得 DTO list，並檢查是否有學生
                var workerStudents = bwE.Argument as List<RankStudentDto>;
                if (workerStudents == null || workerStudents.Count == 0)
                {
                    bwE.Result = new InvalidOperationException("沒有可供計算的學生");
                    return;
                }

                try
                {
                    bkw.ReportProgress(1);
                    #region 產生學生清單的SQL
                    // 產生 student_list 用的 VALUES 列表
                    List<string> studentValueRows = new List<string>();
                    foreach (RankStudentDto dto in workerStudents)
                    {
                  
                        #region 單筆學生資料的SQL
                        string studentId = dto.StudentID;
                        string studentName = dto.StudentName;
                        string rankGradeYear = dto.RankGradeYearText.Trim('年', '級');
                        string deptName = dto.DeptName;
                        string className = dto.ClassName;
                        string tag1 = dto.Tag1;
                        string tag2 = dto.Tag2;
                        string regGroupName = dto.RegGroupName;
                        string regGroupCode = dto.RegGroupCode;

                        // 一列 VALUES：() 裡面每個欄位都有型別 cast
                        string row = @"
(" + studentId + @"::BIGINT
, " + SqlLiteral(studentName) + @"::TEXT
, " + rankGradeYear + @"::INT
, " + SqlLiteral(deptName) + @"::TEXT
, " + SqlLiteral(className) + @"::TEXT
, " + SqlLiteral(tag1) + @"::TEXT
, " + SqlLiteral(tag2) + @"::TEXT
, " + SqlLiteral(regGroupName) + @"::TEXT
, " + SqlLiteral(regGroupCode) + @"::TEXT
)";
                        #endregion
                        //將各群組學生加入科目對照表
                        if (!RegGroupSubjMapDic.ContainsKey(regGroupName))
                    {
                        RegGroupSubjMapDic.Add(regGroupName, new Dictionary<string, List<string>>());
                        RegGroupSubjMapDic[regGroupName].Add("學業", new List<string>());
                        RegGroupSubjMapDic[regGroupName]["學業"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[regGroupName]["學業"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[regGroupName]["學業"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[regGroupName]["學業"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[regGroupName]["學業"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[regGroupName].Add("專業", new List<string>());
                        RegGroupSubjMapDic[regGroupName]["專業"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[regGroupName]["專業"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[regGroupName]["專業"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[regGroupName]["專業"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[regGroupName]["專業"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[regGroupName].Add("技能", new List<string>());
                        RegGroupSubjMapDic[regGroupName]["技能"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[regGroupName]["技能"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[regGroupName]["技能"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[regGroupName]["技能"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[regGroupName]["技能"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[regGroupName].Add("英文", new List<string>());
                        RegGroupSubjMapDic[regGroupName]["英文"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[regGroupName]["英文"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[regGroupName]["英文"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[regGroupName]["英文"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[regGroupName]["英文"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[regGroupName].Add("國文", new List<string>());
                        RegGroupSubjMapDic[regGroupName]["國文"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[regGroupName]["國文"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[regGroupName]["國文"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[regGroupName]["國文"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[regGroupName]["國文"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[regGroupName].Add("數學", new List<string>());
                        RegGroupSubjMapDic[regGroupName]["數學"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[regGroupName]["數學"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[regGroupName]["數學"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[regGroupName]["數學"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[regGroupName]["數學"].Add("'沒有設定科目'");
                    }
                    //將各群組學生加入成綪計算方式對照表
                    if (!RegGroupCalcMapDic.ContainsKey(regGroupName))
                    {
                        RegGroupCalcMapDic.Add(regGroupName, new Dictionary<string, List<string>>());
                        RegGroupCalcMapDic[regGroupName].Add("學業", new List<string>());
                        RegGroupCalcMapDic[regGroupName]["學業"].Add("");
                        RegGroupCalcMapDic[regGroupName]["學業"].Add("");
                        RegGroupCalcMapDic[regGroupName].Add("專業", new List<string>());
                        RegGroupCalcMapDic[regGroupName]["專業"].Add("");
                        RegGroupCalcMapDic[regGroupName]["專業"].Add("");
                        RegGroupCalcMapDic[regGroupName].Add("技能", new List<string>());
                        RegGroupCalcMapDic[regGroupName]["技能"].Add("");
                        RegGroupCalcMapDic[regGroupName]["技能"].Add("");
                        RegGroupCalcMapDic[regGroupName].Add("英文", new List<string>());
                        RegGroupCalcMapDic[regGroupName]["英文"].Add("");
                        RegGroupCalcMapDic[regGroupName]["英文"].Add("");
                        RegGroupCalcMapDic[regGroupName].Add("國文", new List<string>());
                        RegGroupCalcMapDic[regGroupName]["國文"].Add("");
                        RegGroupCalcMapDic[regGroupName]["國文"].Add("");
                        RegGroupCalcMapDic[regGroupName].Add("數學", new List<string>());
                        RegGroupCalcMapDic[regGroupName]["數學"].Add("");
                        RegGroupCalcMapDic[regGroupName]["數學"].Add("");

                    }

                    //把單筆學生資料的 VALUES row 加入到List                    
                    studentValueRows.Add(row);
                }
                //填入群組計算方式
                List<udtRegistrationCalc> GroupCalc = new List<udtRegistrationCalc>();

                GroupCalc = accessHelper.Select<udtRegistrationCalc>();
                foreach (udtRegistrationCalc Calc in GroupCalc)
                { 
                    if (!RegGroupCalcMapDic.ContainsKey(Calc.RegGroupName))
                    {
                        RegGroupCalcMapDic.Add(Calc.RegGroupName, new Dictionary<string, List<string>>());
                        RegGroupCalcMapDic[Calc.RegGroupName].Add("學業", new List<string>());
                        RegGroupCalcMapDic[Calc.RegGroupName]["學業"].Add("");
                        RegGroupCalcMapDic[Calc.RegGroupName]["學業"].Add("");
                        RegGroupCalcMapDic[Calc.RegGroupName].Add("專業", new List<string>());
                        RegGroupCalcMapDic[Calc.RegGroupName]["專業"].Add("");
                        RegGroupCalcMapDic[Calc.RegGroupName]["專業"].Add("");
                        RegGroupCalcMapDic[Calc.RegGroupName].Add("技能", new List<string>());
                        RegGroupCalcMapDic[Calc.RegGroupName]["技能"].Add("");
                        RegGroupCalcMapDic[Calc.RegGroupName]["技能"].Add("");
                        RegGroupCalcMapDic[Calc.RegGroupName].Add("英文", new List<string>());
                        RegGroupCalcMapDic[Calc.RegGroupName]["英文"].Add("");
                        RegGroupCalcMapDic[Calc.RegGroupName]["英文"].Add("");
                        RegGroupCalcMapDic[Calc.RegGroupName].Add("國文", new List<string>());
                        RegGroupCalcMapDic[Calc.RegGroupName]["國文"].Add("");
                        RegGroupCalcMapDic[Calc.RegGroupName]["國文"].Add("");
                        RegGroupCalcMapDic[Calc.RegGroupName].Add("數學", new List<string>());
                        RegGroupCalcMapDic[Calc.RegGroupName]["數學"].Add("");
                        RegGroupCalcMapDic[Calc.RegGroupName]["數學"].Add("");
                    }
                    RegGroupCalcMapDic[Calc.RegGroupName][Calc.CalcName][0] = Calc.RegCalcKind;
                    RegGroupCalcMapDic[Calc.RegGroupName][Calc.CalcName][1] = Calc.RegCalcItem;
                    //若該群組只有設定學業原始，則只會有學業成績
                    if (RegGroupCalcMapDic[Calc.RegGroupName]["學業"][1] == "依學業原始")
                    {
                        if (!RegGroupSubjMapDic.ContainsKey(Calc.RegGroupName))
                        {
                            RegGroupSubjMapDic.Add(Calc.RegGroupName, new Dictionary<string, List<string>>());
                            RegGroupSubjMapDic[Calc.RegGroupName].Add("學業", new List<string>());
                            RegGroupSubjMapDic[Calc.RegGroupName]["學業"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[Calc.RegGroupName]["學業"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[Calc.RegGroupName]["學業"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[Calc.RegGroupName]["學業"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[Calc.RegGroupName]["學業"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[Calc.RegGroupName].Add("專業", new List<string>());
                            RegGroupSubjMapDic[Calc.RegGroupName]["專業"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[Calc.RegGroupName]["專業"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[Calc.RegGroupName]["專業"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[Calc.RegGroupName]["專業"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[Calc.RegGroupName]["專業"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[Calc.RegGroupName].Add("技能", new List<string>());
                            RegGroupSubjMapDic[Calc.RegGroupName]["技能"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[Calc.RegGroupName]["技能"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[Calc.RegGroupName]["技能"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[Calc.RegGroupName]["技能"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[Calc.RegGroupName]["技能"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[Calc.RegGroupName].Add("英文", new List<string>());
                            RegGroupSubjMapDic[Calc.RegGroupName]["英文"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[Calc.RegGroupName]["英文"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[Calc.RegGroupName]["英文"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[Calc.RegGroupName]["英文"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[Calc.RegGroupName]["英文"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[Calc.RegGroupName].Add("國文", new List<string>());
                            RegGroupSubjMapDic[Calc.RegGroupName]["國文"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[Calc.RegGroupName]["國文"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[Calc.RegGroupName]["國文"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[Calc.RegGroupName]["國文"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[Calc.RegGroupName]["國文"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[Calc.RegGroupName].Add("數學", new List<string>());
                            RegGroupSubjMapDic[Calc.RegGroupName]["數學"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[Calc.RegGroupName]["數學"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[Calc.RegGroupName]["數學"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[Calc.RegGroupName]["數學"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[Calc.RegGroupName]["數學"].Add("'沒有設定科目'");
                        }
                        RegGroupSubjMapDic[Calc.RegGroupName]["學業"][0] = "'依學業原始'";
                        RegGroupSubjMapDic[Calc.RegGroupName]["學業"][1] = "'依學業原始'";
                        RegGroupSubjMapDic[Calc.RegGroupName]["學業"][2] = "'依學業原始'";
                        RegGroupSubjMapDic[Calc.RegGroupName]["學業"][3] = "'依學業原始'";
                        RegGroupSubjMapDic[Calc.RegGroupName]["學業"][4] = "'依學業原始'";
                    }
                }
                //填入科目
                List<string> SubjectC = new List<string>();
                SubjectC.Add("'沒有設定科目',");
                SubjectC.Add("'沒有設定科目',");
                SubjectC.Add("'沒有設定科目',");
                SubjectC.Add("'沒有設定科目',");
                SubjectC.Add("'沒有設定科目',");
                //依群組填入各項目科目
                foreach (string group in DataDict.Keys)
                {
                    if (!RegGroupCalcMapDic.ContainsKey(group))
                    {
                        RegGroupCalcMapDic.Add(group, new Dictionary<string, List<string>>());
                        RegGroupCalcMapDic[group].Add("學業", new List<string>());
                        RegGroupCalcMapDic[group]["學業"].Add("");
                        RegGroupCalcMapDic[group]["學業"].Add("");
                        RegGroupCalcMapDic[group].Add("專業", new List<string>());
                        RegGroupCalcMapDic[group]["專業"].Add("");
                        RegGroupCalcMapDic[group]["專業"].Add("");
                        RegGroupCalcMapDic[group].Add("技能", new List<string>());
                        RegGroupCalcMapDic[group]["技能"].Add("");
                        RegGroupCalcMapDic[group]["技能"].Add("");
                        RegGroupCalcMapDic[group].Add("英文", new List<string>());
                        RegGroupCalcMapDic[group]["英文"].Add("");
                        RegGroupCalcMapDic[group]["英文"].Add("");
                        RegGroupCalcMapDic[group].Add("國文", new List<string>());
                        RegGroupCalcMapDic[group]["國文"].Add("");
                        RegGroupCalcMapDic[group]["國文"].Add("");
                        RegGroupCalcMapDic[group].Add("數學", new List<string>());
                        RegGroupCalcMapDic[group]["數學"].Add("");
                        RegGroupCalcMapDic[group]["數學"].Add("");
                    }
                    if (!RegGroupSubjMapDic.ContainsKey(group))
                    {
                        RegGroupSubjMapDic.Add(group, new Dictionary<string, List<string>>());
                        
                        if (RegGroupCalcMapDic[group]["學業"][1] == "依學業原始")
                        {
                            RegGroupSubjMapDic[group].Add("學業", new List<string>());
                            RegGroupSubjMapDic[group]["學業"].Add("'依學業原始'");
                            RegGroupSubjMapDic[group]["學業"].Add("'依學業原始'");
                            RegGroupSubjMapDic[group]["學業"].Add("'依學業原始'");
                            RegGroupSubjMapDic[group]["學業"].Add("'依學業原始'");
                            RegGroupSubjMapDic[group]["學業"].Add("'依學業原始'");
                        }
                        else
                        {
                            RegGroupSubjMapDic[group].Add("學業", new List<string>());
                            RegGroupSubjMapDic[group]["學業"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[group]["學業"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[group]["學業"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[group]["學業"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[group]["學業"].Add("'沒有設定科目'");
                        }
                        RegGroupSubjMapDic[group].Add("專業", new List<string>());
                        RegGroupSubjMapDic[group]["專業"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[group]["專業"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[group]["專業"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[group]["專業"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[group]["專業"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[group].Add("技能", new List<string>());
                        RegGroupSubjMapDic[group]["技能"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[group]["技能"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[group]["技能"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[group]["技能"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[group]["技能"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[group].Add("英文", new List<string>());
                        RegGroupSubjMapDic[group]["英文"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[group]["英文"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[group]["英文"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[group]["英文"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[group]["英文"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[group].Add("國文", new List<string>());
                        RegGroupSubjMapDic[group]["國文"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[group]["國文"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[group]["國文"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[group]["國文"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[group]["國文"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[group].Add("數學", new List<string>());
                        RegGroupSubjMapDic[group]["數學"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[group]["數學"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[group]["數學"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[group]["數學"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[group]["數學"].Add("'沒有設定科目'");
                    }
                    List<string> CalcString = new List<string>();
                    //CalcString.Add("學業");
                    //CalcString.Add("專業");
                    //CalcString.Add("技能");
                    //CalcString.Add("英文");
                    //CalcString.Add("國文");
                    //CalcString.Add("數學");
                    //依計算項目加入科目
                    foreach (string Calc in DataDict[group].Keys)
                    {
                        if (Calc == "學業" && RegGroupCalcMapDic[group]["學業"][1] == "依學業原始")
                        {
                            RegGroupSubjMapDic[group][Calc][0] = "'依學業原始'";
                            RegGroupSubjMapDic[group][Calc][1] = "'依學業原始'";
                            RegGroupSubjMapDic[group][Calc][2] = "'依學業原始'";
                            RegGroupSubjMapDic[group][Calc][3] = "'依學業原始'";
                            RegGroupSubjMapDic[group][Calc][4] = "'依學業原始'";
                        }
                        else
                        {
                            //組合科目名稱
                            if (DataDict[group].ContainsKey(Calc))
                            { 
                                foreach (udtRegistrationSubjectNew data in DataDict[group][Calc])
                                {
                                    SubjectC[((int.Parse(data.GradeYear) - 1) * 2 + int.Parse(data.Semester)) - 1] = SubjectC[((int.Parse(data.GradeYear) - 1) * 2 + int.Parse(data.Semester)) - 1] + "'" + data.SubjectName + "',";
                                }
                            }
                            //放入5學期科目
                            RegGroupSubjMapDic[group][Calc][0] = SubjectC[0].Substring(0, SubjectC[0].Length - 1);
                            RegGroupSubjMapDic[group][Calc][1] = SubjectC[1].Substring(0, SubjectC[1].Length - 1);
                            RegGroupSubjMapDic[group][Calc][2] = SubjectC[2].Substring(0, SubjectC[2].Length - 1);
                            RegGroupSubjMapDic[group][Calc][3] = SubjectC[3].Substring(0, SubjectC[3].Length - 1);
                            RegGroupSubjMapDic[group][Calc][4] = SubjectC[4].Substring(0, SubjectC[4].Length - 1);
                        }
                        SubjectC[0]="'沒有設定科目',";
                        SubjectC[1] = "'沒有設定科目',";
                        SubjectC[2] = "'沒有設定科目',";
                        SubjectC[3] = "'沒有設定科目',";
                        SubjectC[4] = "'沒有設定科目',";
                   }
                }
                        //把剛剛組好的學生資料的 VALUES 組成 CTE
                        #region 所有學生資料的SQL
                        string studentListSql;
                        if (studentValueRows.Count == 0)
                        {
                            // 若沒有任何學生，就讓 student_list 產生一個空結果 CTE
                            studentListSql = @"
WITH student_list(
    student_id,
    student_name,
    rank_grade_year,
    rank_dept_name,
    rank_class_name,
    rank_tag1,
    rank_tag2,
    rank_group_name,
    rank_group_code
) AS
(
    SELECT
        NULL::BIGINT AS student_id,
        NULL::TEXT AS student_name,
        NULL::INT AS rank_grade_year,
        NULL::TEXT AS rank_dept_name,
        NULL::TEXT AS rank_class_name,
        NULL::TEXT AS rank_tag1,
        NULL::TEXT AS rank_tag2,
        NULL::TEXT AS rank_group_name,
        NULL::TEXT AS rank_group_code
    WHERE 1 = 0
)
";
                        }
                        else
                        {
                            // 用 VALUES 組成 student_list CTE，並指定欄位名稱
                            studentListSql = @"
WITH student_list(
    student_id,
    student_name,
    rank_grade_year,
    rank_dept_name,
    rank_class_name,
    rank_tag1,
    rank_tag2,
    rank_group_name,
    rank_group_code
) AS
(
    VALUES
    " + string.Join(@",
    ", studentValueRows) + @"
)
";
                        }
                #endregion
                #endregion

                //// debug 
                //string fiPath = Application.StartupPath + @"\sql1.txt";
                //using (System.IO.StreamWriter fi = new System.IO.StreamWriter(fiPath))
                //{
                //    fi.WriteLine(studentListSql);
                //}

                //< Subject 不計學分 = "否" 不需評分 = "否" 修課必選修 = "必修" 修課校部訂 = "校訂" 原始成績 = "82" 學年調整成績 = "" 擇優採計成績 = "" 是否取得學分 = "是" 是否補修成績 = "否" 科目 = "英文文法" 科目級別 = "2" 補考成績 = "" 重修學年度 = "" 重修學期 = "" 重修成績 = "" 開課分項類別 = "學業" 開課學分數 = "1" />

                string calculationSetting = "";

                #region 產生計算規則的SQL
                #region 產生要儲存到rank_batch的setting的Xml
                XmlDocument xdoc = new XmlDocument();
                var settingEle = xdoc.CreateElement("Setting");
                settingEle.SetAttribute("不排名學生類別", SelStudentFilter);
                settingEle.SetAttribute("類別一", SelStudentTag1);
                //settingEle.SetAttribute("類別二", SelStudentTag2);
                settingEle.SetAttribute("年級", "3");
                settingEle.SetAttribute("成績四捨五入小數位數", parseNumber.ToString());

                calculationSetting = settingEle.OuterXml;
                #endregion
                #endregion

                #region 產生學生學期成績對照表
                // 產生 student_sems 用的 VALUES 列表
                List<string> studSemsValueRows = new List<string>();

                foreach (string sid in RankStudentIDList)
                {
                    if (StudGradeYearSemsDict.ContainsKey(sid))
                    {
                        foreach (GradeYearSemesterInfo gs in StudGradeYearSemsDict[sid])
                        {
                            // 取五學期，3 年級第 2 學期不取
                            if (gs.GradeYear == 3 && gs.Semester == 2)
                                continue;
                            
                            // todo 處理重讀--Cyn

                            string row = @"
(" + sid + @"::BIGINT
, " + gs.GradeYear + @"::INT
, " + gs.Semester + @"::INT
, " + gs.SchoolYear + @"::INT
)";
                            studSemsValueRows.Add(row);
                        }
                    }
                }

                string StudSemsSQL;
                if (studSemsValueRows.Count == 0)
                {
                    // 若沒有任何學期資料，就讓 student_sems 產生一個空結果 CTE
                    StudSemsSQL = @"
, student_sems(
    student_id,
    sems_grade_year,
    sems_semester,
    sems_school_year
) AS
(
    SELECT
        NULL::BIGINT AS student_id,
        NULL::INT AS sems_grade_year,
        NULL::INT AS sems_semester,
        NULL::INT AS sems_school_year
    WHERE 1 = 0
)";
                }
                else
                {
                    StudSemsSQL = @"
, student_sems(
    student_id,
    sems_grade_year,
    sems_semester,
    sems_school_year
) AS
(
    VALUES
    " + string.Join(@",
    ", studSemsValueRows) + @"
)";
                }

                string insertRankSql = @"
" + studentListSql + @"
" + StudSemsSQL + @"
,parse_number AS
(
    SELECT " + parseNumber + @" ::INT AS parse_number
)              
, subject_score_data AS
(
	SELECT
		 sl.student_id
        , s.school_year
        , s.semester
        , s.grade_year
		, sl.student_name
		, sl.rank_grade_year
		, sl.rank_dept_name
		, sl.rank_class_name
		, sl.rank_tag1
		, sl.rank_tag2
		, sl.rank_group_name
        , sl.rank_group_code
		, array_to_string(xpath('/root/Subject/@科目', xmlparse(content concat('<root>', subj_score_ele , '</root>'))), '')::TEXT As subject
		, NULLIF(array_to_string(xpath('/root/Subject/@原始成績', xmlparse(content concat('<root>', subj_score_ele , '</root>'))), ''),'')::DECIMAL As subject_origin_score
		, NULLIF(array_to_string(xpath('/root/Subject/@開課學分數', xmlparse(content concat('<root>', subj_score_ele , '</root>'))), ''),'')::DECIMAL As subject_credit
		, NULLIF(array_to_string(xpath('/root/Subject/@開課分項類別', xmlparse(content concat('<root>', subj_score_ele , '</root>'))), ''),'')::TEXT As subject_entry
        , NULLIF(array_to_string(xpath('/root/Subject/@修課必選修', xmlparse(content concat('<root>', subj_score_ele , '</root>'))), ''),'')::TEXT As subject_required
        , NULLIF(array_to_string(xpath('/root/Subject/@修課校部訂', xmlparse(content concat('<root>', subj_score_ele , '</root>'))), ''),'')::TEXT As subject_requiredby
         , NULLIF(array_to_string(xpath('/root/Subject/@不計學分', xmlparse(content concat('<root>', subj_score_ele , '</root>'))), ''),'')::TEXT As ns1
         , NULLIF(array_to_string(xpath('/root/Subject/@不需評分', xmlparse(content concat('<root>', subj_score_ele , '</root>'))), ''),'')::TEXT As ns2
	FROM
		sems_subj_score s
		INNER JOIN student_list sl
			ON s.ref_student_id = sl.student_id
		INNER JOIN student_sems ss
			ON s.ref_student_id = ss.student_id::INT
		   AND s.school_year = ss.sems_school_year::INT
		   AND s.semester = ss.sems_semester::INT
		   AND s.grade_year = ss.sems_grade_year::INT
		CROSS JOIN LATERAL
			unnest(
				xpath(
					'/root/SemesterSubjectScoreInfo/Subject',
					xmlparse(content concat('<root>', s.score_info , '</root>'))
				)
			) AS subj_score_ele
)
,sems_entry_Score_data AS
(
	SELECT
		 sl.student_id
        , s.school_year
        , s.semester
        , s.grade_year
		, sl.student_name
		, sl.rank_grade_year
		, sl.rank_dept_name
		, sl.rank_class_name
		, sl.rank_tag1
		, sl.rank_tag2
		, sl.rank_group_name
        , sl.rank_group_code
		, '依學業原始'::TEXT As subject
		, NULLIF(x.score_text,'')::DECIMAL AS subject_origin_score
		, '1'::DECIMAL As subject_credit
		, '學業(原始)'::TEXT As subject_entry
        , NULL::TEXT As subject_required
        , NULL::TEXT As subject_requiredby
        , '否'::TEXT As ns1
        , '否'::TEXT As ns2
	FROM
		sems_entry_score s
		INNER JOIN student_list sl
			ON s.ref_student_id = sl.student_id
		INNER JOIN student_sems ss
			ON s.ref_student_id = ss.student_id::INT
		   AND s.school_year = ss.sems_school_year::INT
		   AND s.semester = ss.sems_semester::INT
		   AND s.grade_year = ss.sems_grade_year::INT
		CROSS JOIN LATERAL
		(
			SELECT array_to_string(
					   xpath('//SemesterEntryScore/Entry[@分項=''學業(原始)'']/@成績',
							 xmlparse(content s.score_info)), ''
				   )::TEXT AS score_text
		) AS x
	WHERE
		s.entry_group = 1
)
,subject_score AS
(
    SELECT * FROM subject_score_data WHERE ns1 = '否' AND ns2 ='否' 
    UNION ALL
    SELECT * FROM sems_entry_score_data
)
,subject_entry_score_base AS
(
    SELECT
        s.student_id,
        sl.student_name,
        sl.rank_grade_year,
        sl.rank_dept_name,
        sl.rank_class_name,
        sl.rank_tag1,
        sl.rank_tag2,
        sl.rank_group_name,
        sl.rank_group_code,
        s.school_year,
        s.semester,
        s.grade_year,
        s.subject,
        s.subject_origin_score,
        s.subject_credit
    FROM subject_score s
    INNER JOIN student_list sl
        ON s.student_id = sl.student_id
)
"; // 過濾不需評分與不計學分，並建立 base CTE 一次掃描 subject_score 

    // 多學期學業加權平均 - 使用 bucket_key 重構（效能優化：減少 subject_score 掃描次數）
    StringBuilder sbSubjectEntryBucket = new StringBuilder();
    StringBuilder sbSubjectEntryCaseWhen = new StringBuilder();
    string semstr = "";
    bool useWeightedAvg = false;
    
    // 建立 bucket_key 的 CASE WHEN 語句
    sbSubjectEntryCaseWhen.AppendLine("        CASE");
    foreach (string REGGroupName in RegGroupSubjMapDic.Keys)
    {
        if (RegGroupCalcMapDic[REGGroupName]["學業"][0] == "加權平均")
            useWeightedAvg = true;
        else
            useWeightedAvg = false;
            
        foreach (GradeYearSemesterInfo gs in calSemesterList)
        {
            int subjectNum = (gs.GradeYear - 1) * 2 + gs.Semester - 1;
            string subjectList = RegGroupSubjMapDic[REGGroupName]["學業"][subjectNum];
            
            sbSubjectEntryCaseWhen.AppendLine("            WHEN b.rank_group_name = " + SqlLiteral(REGGroupName) + 
                " AND b.grade_year = " + gs.GradeYear + 
                " AND b.semester = " + gs.Semester + 
                " AND b.subject IN (" + subjectList + ")");
            sbSubjectEntryCaseWhen.AppendLine("                THEN " + SqlLiteral("學業_" + REGGroupName + "_" + gs.GradeYear + (gs.Semester == 1 ? "a" : "b")));
        }
    }
    sbSubjectEntryCaseWhen.AppendLine("            ELSE NULL");
    sbSubjectEntryCaseWhen.AppendLine("        END AS bucket_key");
    
    // 建立 bucket CTE
    sbSubjectEntryBucket.AppendLine(@",subject_entry_score_bucket AS
(
    SELECT
        b.*,");
    sbSubjectEntryBucket.Append(sbSubjectEntryCaseWhen.ToString());
    sbSubjectEntryBucket.AppendLine(@"
    FROM subject_entry_score_base b
)");
    
    // 判斷是否使用加權平均（取第一個群組的設定作為代表，因為通常所有群組使用相同計算方式）
    if (RegGroupSubjMapDic.Count > 0)
    {
        string firstGroup = RegGroupSubjMapDic.Keys.First();
        if (RegGroupCalcMapDic[firstGroup]["學業"][0] == "加權平均")
            semstr = ",SUM(subject_origin_score * subject_credit) AS sum_score, SUM(subject_credit) AS sum_credit";
        else
            semstr = ",SUM(subject_origin_score) AS sum_score, COUNT(subject_credit) AS sum_credit";
    }
    else
    {
        semstr = ",SUM(subject_origin_score * subject_credit) AS sum_score, SUM(subject_credit) AS sum_credit";
    }
    
    // 舊的 sbSubjectEntrySore 不再使用，改為使用 bucket CTE
    StringBuilder sbSubjectEntrySore = new StringBuilder();
    sbSubjectEntrySore.Append(sbSubjectEntryBucket.ToString());
                       
                string Strsubject_entry_score_avg_list = @"
,subject_entry_score_avg_list AS
(
    SELECT
        student_id,
        student_name,
        rank_grade_year,
        rank_dept_name,
        rank_class_name,
        rank_tag1,
        rank_tag2,
        rank_group_name,
        rank_group_code,
        round(sum_score_s/sum_credit_s, parse_number) AS avg_score
    FROM
    (
        SELECT
            student_id,
            student_name,
            rank_grade_year,
            rank_dept_name,
            rank_class_name,
            rank_tag1,
            rank_tag2,
            rank_group_name,
            rank_group_code,
            SUM(sum_score) AS sum_score_s,
            SUM(sum_credit) AS sum_credit_s,
            (SELECT parse_number FROM parse_number)::INT AS parse_number
        FROM
        (
            SELECT
                student_id,
                student_name,
                rank_grade_year,
                rank_dept_name,
                rank_class_name,
                rank_tag1,
                rank_tag2,
                rank_group_name,
                rank_group_code" + semstr + @"
            FROM subject_entry_score_bucket
            WHERE bucket_key IS NOT NULL
            GROUP BY
                student_id,
                student_name,
                rank_grade_year,
                rank_dept_name,
                rank_class_name,
                rank_tag1,
                rank_tag2,
                rank_group_name,
                rank_group_code,
                bucket_key
        ) AS subject_entry_score_avg5
        GROUP BY
            student_id,
            student_name,
            rank_grade_year,
            rank_dept_name,
            rank_class_name,
            rank_tag1,
            rank_tag2,
            rank_group_name,
            rank_group_code
    ) AS subject_entry_score_avg_round
)
,subject_entry_score_rank_list AS
(
    SELECT
         student_id
        ,student_name
        ,rank_grade_year
        ,rank_dept_name
        ,'學業'::TEXT AS item_name
        ,rank_class_name
        ,rank_tag1
        ,rank_tag2
        ,rank_group_name
        ,rank_group_code
        , avg_score
        , RANK() OVER(PARTITION BY rank_grade_year ORDER BY avg_score DESC) AS grade_rank
        , RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name ORDER BY avg_score DESC) AS dept_rank
        , RANK() OVER(PARTITION BY rank_class_name ORDER BY avg_score DESC) AS class_rank
        , RANK() OVER(PARTITION BY rank_grade_year, rank_tag1 ORDER BY avg_score DESC) AS tag1_rank
        , RANK() OVER(PARTITION BY rank_grade_year, rank_tag2 ORDER BY avg_score DESC) AS tag2_rank
        , RANK() OVER(PARTITION BY rank_grade_year, rank_group_code ORDER BY avg_score DESC) AS group_rank
        , RANK() OVER(PARTITION BY rank_grade_year ORDER BY avg_score ASC) AS grade_rank_reverse
        , RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name ORDER BY avg_score ASC) AS dept_rank_reverse
        , RANK() OVER(PARTITION BY rank_class_name ORDER BY avg_score ASC) AS class_rank_reverse
        , RANK() OVER(PARTITION BY rank_grade_year, rank_tag1 ORDER BY avg_score ASC) AS tag1_rank_reverse
        , RANK() OVER(PARTITION BY rank_grade_year, rank_tag2 ORDER BY avg_score ASC) AS tag2_rank_reverse
        , RANK() OVER(PARTITION BY rank_grade_year, rank_group_code ORDER BY avg_score ASC) AS group_rank_reverse
        , COUNT(student_id) OVER(PARTITION BY rank_grade_year) AS grade_count
        , COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_dept_name) AS dept_count
        , COUNT(student_id) OVER(PARTITION BY rank_class_name) AS class_count
        , COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_tag1) AS tag1_count
        , COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_tag2) AS tag2_count
        , COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_group_code) AS group_count
    FROM
        subject_entry_score_avg_list
    WHERE       
        avg_score IS NOT NULL
)
, subject_entry_score_avg_expand AS
(
    SELECT
        subject_entry_score_rank_list.*
        , FLOOR((grade_rank::DECIMAL - 1)*100::DECIMAL / grade_count) + 1 AS graderank_percentage
        , FLOOR((dept_rank::DECIMAL - 1)*100::DECIMAL / dept_count) + 1 AS deptrank_percentage
        , FLOOR((class_rank::DECIMAL - 1)*100::DECIMAL / class_count) + 1 AS classrank_percentage
        , FLOOR((tag1_rank::DECIMAL - 1)*100::DECIMAL / tag1_count) + 1 AS tag1rank_percentage
        , FLOOR((tag2_rank::DECIMAL - 1)*100::DECIMAL / tag2_count) + 1 AS tag2rank_percentage
        , FLOOR((group_rank::DECIMAL - 1)*100::DECIMAL / group_count) + 1 AS grouprank_percentage
        , FLOOR((grade_rank_reverse::DECIMAL-1)*100::DECIMAL/grade_count) AS graderank_pr
        , FLOOR((class_rank_reverse::DECIMAL-1)*100::DECIMAL/class_count) AS classrank_pr
        , FLOOR((dept_rank_reverse::DECIMAL-1)*100::DECIMAL/dept_count) AS deptrank_pr
        , FLOOR((tag1_rank_reverse::DECIMAL-1)*100::DECIMAL/tag1_count) AS tag1ank_pr
        , FLOOR((tag2_rank_reverse::DECIMAL-1)*100::DECIMAL/tag2_count) AS tag2rank_pr
        , FLOOR((group_rank_reverse::DECIMAL-1)*100::DECIMAL/group_count) AS grouprank_pr
    FROM
        subject_entry_score_rank_list
)

";

                    // 多學期專業及實習加權平均 - 使用 bucket_key 重構（效能優化）
                StringBuilder sbSubjectProBucket = new StringBuilder();
                StringBuilder sbSubjectProCaseWhen = new StringBuilder();
                string semstrPro = "";
                
                // 建立 bucket_key 的 CASE WHEN 語句
                sbSubjectProCaseWhen.AppendLine("        CASE");
                foreach (string REGGroupName in RegGroupSubjMapDic.Keys)
                {
                    foreach (GradeYearSemesterInfo gs in calSemesterList)
                    {
                        int subjectNum = (gs.GradeYear - 1) * 2 + gs.Semester - 1;
                        string subjectList = RegGroupSubjMapDic[REGGroupName]["專業"][subjectNum];
                        
                        sbSubjectProCaseWhen.AppendLine("            WHEN b.rank_group_name = " + SqlLiteral(REGGroupName) + 
                            " AND b.grade_year = " + gs.GradeYear + 
                            " AND b.semester = " + gs.Semester + 
                            " AND b.subject IN (" + subjectList + ")");
                        sbSubjectProCaseWhen.AppendLine("                THEN " + SqlLiteral("專業_" + REGGroupName + "_" + gs.GradeYear + (gs.Semester == 1 ? "a" : "b")));
                    }
                }
                sbSubjectProCaseWhen.AppendLine("            ELSE NULL");
                sbSubjectProCaseWhen.AppendLine("        END AS bucket_key");
                
                // 建立 bucket CTE
                sbSubjectProBucket.AppendLine(@",subject_score_pro_bucket AS
(
    SELECT
        b.*,");
                sbSubjectProBucket.Append(sbSubjectProCaseWhen.ToString());
                sbSubjectProBucket.AppendLine(@"
    FROM subject_entry_score_base b
)");
                
                // 判斷是否使用加權平均
                if (RegGroupSubjMapDic.Count > 0)
                {
                    string firstGroup = RegGroupSubjMapDic.Keys.First();
                    if (RegGroupCalcMapDic[firstGroup]["專業"][0] == "加權平均")
                        semstrPro = ",SUM(subject_origin_score * subject_credit) AS sum_score, SUM(subject_credit) AS sum_credit";
                    else
                        semstrPro = ",SUM(subject_origin_score) AS sum_score, COUNT(subject_credit) AS sum_credit";
                }
                else
                {
                    semstrPro = ",SUM(subject_origin_score * subject_credit) AS sum_score, SUM(subject_credit) AS sum_credit";
                }
                
                StringBuilder sbSubjectProSore = new StringBuilder();
                sbSubjectProSore.Append(sbSubjectProBucket.ToString());
                    
                    string Strsubject_score_pro_avg_list = @"
,subject_score_pro_avg_list AS
(
    SELECT
        student_id,
        student_name,
        rank_grade_year,
        rank_dept_name,
        rank_class_name,
        rank_tag1,
        rank_tag2,
        rank_group_name,
        rank_group_code,
        round(sum_score_s/sum_credit_s, parse_number) AS avg_score
    FROM
    (
        SELECT
            student_id,
            student_name,
            rank_grade_year,
            rank_dept_name,
            rank_class_name,
            rank_tag1,
            rank_tag2,
            rank_group_name,
            rank_group_code,
            SUM(sum_score) AS sum_score_s,
            SUM(sum_credit) AS sum_credit_s,
            (SELECT parse_number FROM parse_number)::INT AS parse_number
        FROM
        (
            SELECT
                student_id,
                student_name,
                rank_grade_year,
                rank_dept_name,
                rank_class_name,
                rank_tag1,
                rank_tag2,
                rank_group_name,
                rank_group_code" + semstrPro + @"
            FROM subject_score_pro_bucket
            WHERE bucket_key IS NOT NULL
            GROUP BY
                student_id,
                student_name,
                rank_grade_year,
                rank_dept_name,
                rank_class_name,
                rank_tag1,
                rank_tag2,
                rank_group_name,
                rank_group_code,
                bucket_key
        ) AS subject_score_pro_avg5
        GROUP BY
            student_id,
            student_name,
            rank_grade_year,
            rank_dept_name,
            rank_class_name,
            rank_tag1,
            rank_tag2,
            rank_group_name,
            rank_group_code
    ) AS subject_score_pro_avg_round
)
,subject_score_pro_rank_list AS
(
    SELECT
         student_id
        ,student_name
        ,rank_grade_year
        ,rank_dept_name
        ,'專業及實習'::TEXT AS item_name
        ,rank_class_name
        ,rank_tag1
        ,rank_tag2
        ,rank_group_name
        ,rank_group_code
        , avg_score
        , RANK() OVER(PARTITION BY rank_grade_year ORDER BY avg_score DESC) AS grade_rank
        , RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name ORDER BY avg_score DESC) AS dept_rank
        , RANK() OVER(PARTITION BY rank_class_name ORDER BY avg_score DESC) AS class_rank
        , RANK() OVER(PARTITION BY rank_grade_year, rank_tag1 ORDER BY avg_score DESC) AS tag1_rank
        , RANK() OVER(PARTITION BY rank_grade_year, rank_tag2 ORDER BY avg_score DESC) AS tag2_rank
        , RANK() OVER(PARTITION BY rank_grade_year, rank_group_code ORDER BY avg_score DESC) AS group_rank
        , RANK() OVER(PARTITION BY rank_grade_year ORDER BY avg_score ASC) AS grade_rank_reverse
        , RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name ORDER BY avg_score ASC) AS dept_rank_reverse
        , RANK() OVER(PARTITION BY rank_class_name ORDER BY avg_score ASC) AS class_rank_reverse
        , RANK() OVER(PARTITION BY rank_grade_year, rank_tag1 ORDER BY avg_score ASC) AS tag1_rank_reverse
        , RANK() OVER(PARTITION BY rank_grade_year, rank_tag2 ORDER BY avg_score ASC) AS tag2_rank_reverse
        , RANK() OVER(PARTITION BY rank_grade_year, rank_group_code ORDER BY avg_score ASC) AS group_rank_reverse
        , COUNT(student_id) OVER(PARTITION BY rank_grade_year) AS grade_count
        , COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_dept_name) AS dept_count
        , COUNT(student_id) OVER(PARTITION BY rank_class_name) AS class_count
        , COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_tag1) AS tag1_count
        , COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_tag2) AS tag2_count
        , COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_group_code) AS group_count
    FROM
        subject_score_pro_avg_list
    WHERE       
        avg_score IS NOT NULL
)
, subject_score_pro_avg_expand AS
(
    SELECT
        subject_score_pro_rank_list.*
        , FLOOR((grade_rank::DECIMAL - 1)*100::DECIMAL / grade_count) + 1 AS graderank_percentage
        , FLOOR((dept_rank::DECIMAL - 1)*100::DECIMAL / dept_count) + 1 AS deptrank_percentage
        , FLOOR((class_rank::DECIMAL - 1)*100::DECIMAL / class_count) + 1 AS classrank_percentage
        , FLOOR((tag1_rank::DECIMAL - 1)*100::DECIMAL / tag1_count) + 1 AS tag1rank_percentage
        , FLOOR((tag2_rank::DECIMAL - 1)*100::DECIMAL / tag2_count) + 1 AS tag2rank_percentage
        , FLOOR((group_rank::DECIMAL - 1)*100::DECIMAL / group_count) + 1 AS grouprank_percentage
        , FLOOR((grade_rank_reverse::DECIMAL-1)*100::DECIMAL/grade_count) AS graderank_pr
        , FLOOR((class_rank_reverse::DECIMAL-1)*100::DECIMAL/class_count) AS classrank_pr
        , FLOOR((dept_rank_reverse::DECIMAL-1)*100::DECIMAL/dept_count) AS deptrank_pr
        , FLOOR((tag1_rank_reverse::DECIMAL-1)*100::DECIMAL/tag1_count) AS tag1ank_pr
        , FLOOR((tag2_rank_reverse::DECIMAL-1)*100::DECIMAL/tag2_count) AS tag2rank_pr
        , FLOOR((group_rank_reverse::DECIMAL-1)*100::DECIMAL/group_count) AS grouprank_pr
    FROM
        subject_score_pro_rank_list
)
";
                // 多學期技能領域 - 使用 bucket_key 重構（效能優化）
                StringBuilder sbSubjectSkillBucket = new StringBuilder();
                StringBuilder sbSubjectSkillCaseWhen = new StringBuilder();
                string semstrSkill = "";
                
                // 建立 bucket_key 的 CASE WHEN 語句
                sbSubjectSkillCaseWhen.AppendLine("        CASE");
                foreach (string REGGroupName in RegGroupSubjMapDic.Keys)
                {
                    foreach (GradeYearSemesterInfo gs in calSemesterList)
                    {
                        int subjectNum = (gs.GradeYear - 1) * 2 + gs.Semester - 1;
                        string subjectList = RegGroupSubjMapDic[REGGroupName]["技能"][subjectNum];
                        
                        sbSubjectSkillCaseWhen.AppendLine("            WHEN b.rank_group_name = " + SqlLiteral(REGGroupName) + 
                            " AND b.grade_year = " + gs.GradeYear + 
                            " AND b.semester = " + gs.Semester + 
                            " AND b.subject IN (" + subjectList + ")");
                        sbSubjectSkillCaseWhen.AppendLine("                THEN " + SqlLiteral("技能_" + REGGroupName + "_" + gs.GradeYear + (gs.Semester == 1 ? "a" : "b")));
                    }
                }
                sbSubjectSkillCaseWhen.AppendLine("            ELSE NULL");
                sbSubjectSkillCaseWhen.AppendLine("        END AS bucket_key");
                
                // 建立 bucket CTE
                sbSubjectSkillBucket.AppendLine(@",subject_score_SkillDomain_bucket AS
(
    SELECT
        b.*,");
                sbSubjectSkillBucket.Append(sbSubjectSkillCaseWhen.ToString());
                sbSubjectSkillBucket.AppendLine(@"
    FROM subject_entry_score_base b
)");
                
                // 判斷是否使用加權平均
                if (RegGroupSubjMapDic.Count > 0)
                {
                    string firstGroup = RegGroupSubjMapDic.Keys.First();
                    if (RegGroupCalcMapDic[firstGroup]["技能"][0] == "加權平均")
                        semstrSkill = ",SUM(subject_origin_score * subject_credit) AS sum_score, SUM(subject_credit) AS sum_credit";
                    else
                        semstrSkill = ",SUM(subject_origin_score) AS sum_score, COUNT(subject_credit) AS sum_credit";
                }
                else
                {
                    semstrSkill = ",SUM(subject_origin_score * subject_credit) AS sum_score, SUM(subject_credit) AS sum_credit";
                }
                
                StringBuilder sbSubjectSoreSkill = new StringBuilder();
                sbSubjectSoreSkill.Append(sbSubjectSkillBucket.ToString());
                    
                    string Strsubject_score_SkillDomain_avg_list = @"
,subject_score_SkillDomain_avg_list AS
(
    SELECT
        student_id,
        student_name,
        rank_grade_year,
        rank_dept_name,
        rank_class_name,
        rank_tag1,
        rank_tag2,
        rank_group_name,
        rank_group_code,
        round(sum_score_s/sum_credit_s, parse_number) AS avg_score
    FROM
    (
        SELECT
            student_id,
            student_name,
            rank_grade_year,
            rank_dept_name,
            rank_class_name,
            rank_tag1,
            rank_tag2,
            rank_group_name,
            rank_group_code,
            SUM(sum_score) AS sum_score_s,
            SUM(sum_credit) AS sum_credit_s,
            (SELECT parse_number FROM parse_number)::INT AS parse_number
        FROM
        (
            SELECT
                student_id,
                student_name,
                rank_grade_year,
                rank_dept_name,
                rank_class_name,
                rank_tag1,
                rank_tag2,
                rank_group_name,
                rank_group_code" + semstrSkill + @"
            FROM subject_score_SkillDomain_bucket
            WHERE bucket_key IS NOT NULL
            GROUP BY
                student_id,
                student_name,
                rank_grade_year,
                rank_dept_name,
                rank_class_name,
                rank_tag1,
                rank_tag2,
                rank_group_name,
                rank_group_code,
                bucket_key
        ) AS subject_SkillDomain_avg_avg5
        GROUP BY
            student_id,
            student_name,
            rank_grade_year,
            rank_dept_name,
            rank_class_name,
            rank_tag1,
            rank_tag2,
            rank_group_name,
            rank_group_code
    ) AS subject_score_SkillDomain_avg_round
)
,subject_score_SkillDomain_rank_list AS
(
    SELECT
         student_id
        ,student_name
        ,rank_grade_year
        ,rank_dept_name
        ,'技能領域'::TEXT AS item_name
        ,rank_class_name
        ,rank_tag1
        ,rank_tag2
        ,rank_group_name
        ,rank_group_code
        , avg_score
        , RANK() OVER(PARTITION BY rank_grade_year ORDER BY avg_score DESC) AS grade_rank
        , RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name ORDER BY avg_score DESC) AS dept_rank
        , RANK() OVER(PARTITION BY rank_class_name ORDER BY avg_score DESC) AS class_rank
        , RANK() OVER(PARTITION BY rank_grade_year, rank_tag1 ORDER BY avg_score DESC) AS tag1_rank
        , RANK() OVER(PARTITION BY rank_grade_year, rank_tag2 ORDER BY avg_score DESC) AS tag2_rank
        , RANK() OVER(PARTITION BY rank_grade_year,rank_group_code ORDER BY avg_score DESC) AS group_rank
        , RANK() OVER(PARTITION BY rank_grade_year ORDER BY avg_score ASC) AS grade_rank_reverse
        , RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name ORDER BY avg_score ASC) AS dept_rank_reverse
        , RANK() OVER(PARTITION BY rank_class_name ORDER BY avg_score ASC) AS class_rank_reverse
        , RANK() OVER(PARTITION BY rank_grade_year, rank_tag1 ORDER BY avg_score ASC) AS tag1_rank_reverse
        , RANK() OVER(PARTITION BY rank_grade_year, rank_tag2 ORDER BY avg_score ASC) AS tag2_rank_reverse
        , RANK() OVER(PARTITION BY rank_grade_year,rank_group_code ORDER BY avg_score ASC) AS group_rank_reverse
        , COUNT(student_id) OVER(PARTITION BY rank_grade_year) AS grade_count
        , COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_dept_name) AS dept_count
        , COUNT(student_id) OVER(PARTITION BY rank_class_name) AS class_count
        , COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_tag1) AS tag1_count
        , COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_tag2) AS tag2_count
        , COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_group_code) AS group_count
    FROM
        subject_score_SkillDomain_avg_list
    WHERE       
        avg_score IS NOT NULL
)
, subject_score_SkillDomain_avg_expand AS
(
    SELECT
        subject_score_SkillDomain_rank_list.*
        , FLOOR((grade_rank::DECIMAL - 1)*100::DECIMAL / grade_count) + 1 AS graderank_percentage
        , FLOOR((dept_rank::DECIMAL - 1)*100::DECIMAL / dept_count) + 1 AS deptrank_percentage
        , FLOOR((class_rank::DECIMAL - 1)*100::DECIMAL / class_count) + 1 AS classrank_percentage
        , FLOOR((tag1_rank::DECIMAL - 1)*100::DECIMAL / tag1_count) + 1 AS tag1rank_percentage
        , FLOOR((tag2_rank::DECIMAL - 1)*100::DECIMAL / tag2_count) + 1 AS tag2rank_percentage
        , FLOOR((group_rank::DECIMAL - 1)*100::DECIMAL / group_count) + 1 AS grouprank_percentage
        , FLOOR((grade_rank_reverse::DECIMAL-1)*100::DECIMAL/grade_count) AS graderank_pr
        , FLOOR((class_rank_reverse::DECIMAL-1)*100::DECIMAL/class_count) AS classrank_pr
        , FLOOR((dept_rank_reverse::DECIMAL-1)*100::DECIMAL/dept_count) AS deptrank_pr
        , FLOOR((tag1_rank_reverse::DECIMAL-1)*100::DECIMAL/tag1_count) AS tag1ank_pr
        , FLOOR((tag2_rank_reverse::DECIMAL-1)*100::DECIMAL/tag2_count) AS tag2rank_pr
        , FLOOR((group_rank_reverse::DECIMAL-1)*100::DECIMAL/group_count) AS grouprank_pr
    FROM
        subject_score_SkillDomain_rank_list
)
";

                // 處理國英數動態對照算成績
                // 多學期國文平均 - 使用 bucket_key 重構（效能優化）
                StringBuilder sbSubjectChineseBucket = new StringBuilder();
                StringBuilder sbSubjectChineseCaseWhen = new StringBuilder();
                string semstrChinese = "";
                
                // 建立 bucket_key 的 CASE WHEN 語句
                sbSubjectChineseCaseWhen.AppendLine("        CASE");
                foreach (string REGGroupName in RegGroupSubjMapDic.Keys)
                {
                    foreach (GradeYearSemesterInfo gs in calSemesterList)
                    {
                        int subjectNum = (gs.GradeYear - 1) * 2 + gs.Semester - 1;
                        string subjectList = RegGroupSubjMapDic[REGGroupName]["國文"][subjectNum];
                        
                        sbSubjectChineseCaseWhen.AppendLine("            WHEN b.rank_group_name = " + SqlLiteral(REGGroupName) + 
                            " AND b.grade_year = " + gs.GradeYear + 
                            " AND b.semester = " + gs.Semester + 
                            " AND b.subject IN (" + subjectList + ")");
                        sbSubjectChineseCaseWhen.AppendLine("                THEN " + SqlLiteral("國文_" + REGGroupName + "_" + gs.GradeYear + (gs.Semester == 1 ? "a" : "b")));
                    }
                }
                sbSubjectChineseCaseWhen.AppendLine("            ELSE NULL");
                sbSubjectChineseCaseWhen.AppendLine("        END AS bucket_key");
                
                // 建立 bucket CTE
                sbSubjectChineseBucket.AppendLine(@",subject_score_chinese_bucket AS
(
    SELECT
        b.*,");
                sbSubjectChineseBucket.Append(sbSubjectChineseCaseWhen.ToString());
                sbSubjectChineseBucket.AppendLine(@"
    FROM subject_entry_score_base b
)");
                
                // 判斷是否使用加權平均
                if (RegGroupSubjMapDic.Count > 0)
                {
                    string firstGroup = RegGroupSubjMapDic.Keys.First();
                    if (RegGroupCalcMapDic[firstGroup]["國文"][0] == "加權平均")
                        semstrChinese = ",SUM(subject_origin_score * subject_credit) AS sum_score, SUM(subject_credit) AS sum_credit";
                    else
                        semstrChinese = ",SUM(subject_origin_score) AS sum_score, COUNT(subject_credit) AS sum_credit";
                }
                else
                {
                    semstrChinese = ",SUM(subject_origin_score * subject_credit) AS sum_score, SUM(subject_credit) AS sum_credit";
                }
                
                StringBuilder sbSubjectChineseScore = new StringBuilder();
                sbSubjectChineseScore.Append(sbSubjectChineseBucket.ToString());

                    string Strsubject_score_chinese_avg_list = @"
,subject_score_chinese_avg_list AS
(
    SELECT
        student_id,
        student_name,
        rank_grade_year,
        rank_dept_name,
        rank_class_name,
        rank_tag1,
        rank_tag2,
        rank_group_name,
        rank_group_code,
        round(sum_score_s/sum_credit_s, parse_number) AS avg_score
    FROM
    (
        SELECT
            student_id,
            student_name,
            rank_grade_year,
            rank_dept_name,
            rank_class_name,
            rank_tag1,
            rank_tag2,
            rank_group_name,
            rank_group_code,
            SUM(sum_score) AS sum_score_s,
            SUM(sum_credit) AS sum_credit_s,
            (SELECT parse_number FROM parse_number)::INT AS parse_number
        FROM
        (
            SELECT
                student_id,
                student_name,
                rank_grade_year,
                rank_dept_name,
                rank_class_name,
                rank_tag1,
                rank_tag2,
                rank_group_name,
                rank_group_code" + semstrChinese + @"
            FROM subject_score_chinese_bucket
            WHERE bucket_key IS NOT NULL
            GROUP BY
                student_id,
                student_name,
                rank_grade_year,
                rank_dept_name,
                rank_class_name,
                rank_tag1,
                rank_tag2,
                rank_group_name,
                rank_group_code,
                bucket_key
        ) AS subject_score_chinese_avg5
        GROUP BY
            student_id,
            student_name,
            rank_grade_year,
            rank_dept_name,
            rank_class_name,
            rank_tag1,
            rank_tag2,
            rank_group_name,
            rank_group_code
    ) AS subject_score_chinese_avg_round
)
,subject_score_chinese_rank_list AS
(
    SELECT
         student_id
        ,student_name
        ,rank_grade_year
        ,rank_dept_name
        ,'國文'::TEXT AS item_name
        ,rank_class_name
        ,rank_tag1
        ,rank_tag2
        ,rank_group_name
        ,rank_group_code
        , avg_score
        , RANK() OVER(PARTITION BY rank_grade_year ORDER BY avg_score DESC) AS grade_rank
        , RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name ORDER BY avg_score DESC) AS dept_rank
        , RANK() OVER(PARTITION BY rank_class_name ORDER BY avg_score DESC) AS class_rank
        , RANK() OVER(PARTITION BY rank_grade_year, rank_tag1 ORDER BY avg_score DESC) AS tag1_rank
        , RANK() OVER(PARTITION BY rank_grade_year, rank_tag2 ORDER BY avg_score DESC) AS tag2_rank
        , RANK() OVER(PARTITION BY rank_grade_year, rank_group_code ORDER BY avg_score DESC) AS group_rank
        , RANK() OVER(PARTITION BY rank_grade_year ORDER BY avg_score ASC) AS grade_rank_reverse
        , RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name ORDER BY avg_score ASC) AS dept_rank_reverse
        , RANK() OVER(PARTITION BY rank_class_name ORDER BY avg_score ASC) AS class_rank_reverse
        , RANK() OVER(PARTITION BY rank_grade_year, rank_tag1 ORDER BY avg_score ASC) AS tag1_rank_reverse
        , RANK() OVER(PARTITION BY rank_grade_year, rank_tag2 ORDER BY avg_score ASC) AS tag2_rank_reverse
        , RANK() OVER(PARTITION BY rank_grade_year,rank_group_code ORDER BY avg_score ASC) AS group_rank_reverse
        , COUNT(student_id) OVER(PARTITION BY rank_grade_year) AS grade_count
        , COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_dept_name) AS dept_count
        , COUNT(student_id) OVER(PARTITION BY rank_class_name) AS class_count
        , COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_tag1) AS tag1_count
        , COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_tag2) AS tag2_count
        , COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_group_code) AS group_count
    FROM
        subject_score_chinese_avg_list
    WHERE       
        avg_score IS NOT NULL
)
, subject_score_chinese_avg_expand AS
(
    SELECT
        subject_score_chinese_rank_list.*
        , FLOOR((grade_rank::DECIMAL - 1)*100::DECIMAL / grade_count) + 1 AS graderank_percentage
        , FLOOR((dept_rank::DECIMAL - 1)*100::DECIMAL / dept_count) + 1 AS deptrank_percentage
        , FLOOR((class_rank::DECIMAL - 1)*100::DECIMAL / class_count) + 1 AS classrank_percentage
        , FLOOR((tag1_rank::DECIMAL - 1)*100::DECIMAL / tag1_count) + 1 AS tag1rank_percentage
        , FLOOR((tag2_rank::DECIMAL - 1)*100::DECIMAL / tag2_count) + 1 AS tag2rank_percentage
        , FLOOR((group_rank::DECIMAL - 1)*100::DECIMAL / group_count) + 1 AS grouprank_percentage
        , FLOOR((grade_rank_reverse::DECIMAL-1)*100::DECIMAL/grade_count) AS graderank_pr
        , FLOOR((class_rank_reverse::DECIMAL-1)*100::DECIMAL/class_count) AS classrank_pr
        , FLOOR((dept_rank_reverse::DECIMAL-1)*100::DECIMAL/dept_count) AS deptrank_pr
        , FLOOR((tag1_rank_reverse::DECIMAL-1)*100::DECIMAL/tag1_count) AS tag1ank_pr
        , FLOOR((tag2_rank_reverse::DECIMAL-1)*100::DECIMAL/tag2_count) AS tag2rank_pr
        , FLOOR((group_rank_reverse::DECIMAL-1)*100::DECIMAL/group_count) AS grouprank_pr
    FROM
        subject_score_chinese_rank_list
)

";
 
                // 多學期英文 - 使用 bucket_key 重構（效能優化）
                StringBuilder sbSubjectEnglishBucket = new StringBuilder();
                StringBuilder sbSubjectEnglishCaseWhen = new StringBuilder();
                string semstrEnglish = "";
                
                // 建立 bucket_key 的 CASE WHEN 語句
                sbSubjectEnglishCaseWhen.AppendLine("        CASE");
                foreach (string REGGroupName in RegGroupSubjMapDic.Keys)
                {
                    foreach (GradeYearSemesterInfo gs in calSemesterList)
                    {
                        int subjectNum = (gs.GradeYear - 1) * 2 + gs.Semester - 1;
                        string subjectList = RegGroupSubjMapDic[REGGroupName]["英文"][subjectNum];
                        
                        sbSubjectEnglishCaseWhen.AppendLine("            WHEN b.rank_group_name = " + SqlLiteral(REGGroupName) + 
                            " AND b.grade_year = " + gs.GradeYear + 
                            " AND b.semester = " + gs.Semester + 
                            " AND b.subject IN (" + subjectList + ")");
                        sbSubjectEnglishCaseWhen.AppendLine("                THEN " + SqlLiteral("英文_" + REGGroupName + "_" + gs.GradeYear + (gs.Semester == 1 ? "a" : "b")));
                    }
                }
                sbSubjectEnglishCaseWhen.AppendLine("            ELSE NULL");
                sbSubjectEnglishCaseWhen.AppendLine("        END AS bucket_key");
                
                // 建立 bucket CTE
                sbSubjectEnglishBucket.AppendLine(@",subject_score_english_bucket AS
(
    SELECT
        b.*,");
                sbSubjectEnglishBucket.Append(sbSubjectEnglishCaseWhen.ToString());
                sbSubjectEnglishBucket.AppendLine(@"
    FROM subject_entry_score_base b
)");
                
                // 判斷是否使用加權平均
                if (RegGroupSubjMapDic.Count > 0)
                {
                    string firstGroup = RegGroupSubjMapDic.Keys.First();
                    if (RegGroupCalcMapDic[firstGroup]["英文"][0] == "加權平均")
                        semstrEnglish = ",SUM(subject_origin_score * subject_credit) AS sum_score, SUM(subject_credit) AS sum_credit";
                    else
                        semstrEnglish = ",SUM(subject_origin_score) AS sum_score, COUNT(subject_credit) AS sum_credit";
                }
                else
                {
                    semstrEnglish = ",SUM(subject_origin_score * subject_credit) AS sum_score, SUM(subject_credit) AS sum_credit";
                }
                
                StringBuilder sbSubjectEnglishScore = new StringBuilder();
                sbSubjectEnglishScore.Append(sbSubjectEnglishBucket.ToString());

                    string Strsubject_score_english_avg_list = @"
,subject_score_english_avg_list AS
(
    SELECT
        student_id,
        student_name,
        rank_grade_year,
        rank_dept_name,
        rank_class_name,
        rank_tag1,
        rank_tag2,
        rank_group_name,
        rank_group_code,
        round(sum_score_s/sum_credit_s, parse_number) AS avg_score
    FROM
    (
        SELECT
            student_id,
            student_name,
            rank_grade_year,
            rank_dept_name,
            rank_class_name,
            rank_tag1,
            rank_tag2,
            rank_group_name,
            rank_group_code,
            SUM(sum_score) AS sum_score_s,
            SUM(sum_credit) AS sum_credit_s,
            (SELECT parse_number FROM parse_number)::INT AS parse_number
        FROM
        (
            SELECT
                student_id,
                student_name,
                rank_grade_year,
                rank_dept_name,
                rank_class_name,
                rank_tag1,
                rank_tag2,
                rank_group_name,
                rank_group_code" + semstrEnglish + @"
            FROM subject_score_english_bucket
            WHERE bucket_key IS NOT NULL
            GROUP BY
                student_id,
                student_name,
                rank_grade_year,
                rank_dept_name,
                rank_class_name,
                rank_tag1,
                rank_tag2,
                rank_group_name,
                rank_group_code,
                bucket_key
        ) AS subject_score_english_avg5
        GROUP BY
            student_id,
            student_name,
            rank_grade_year,
            rank_dept_name,
            rank_class_name,
            rank_tag1,
            rank_tag2,
            rank_group_name,
            rank_group_code
    ) AS subject_score_english_avg_round
)
,subject_score_english_rank_list AS
(
    SELECT
         student_id
        ,student_name
        ,rank_grade_year
        ,rank_dept_name
        ,'英文'::TEXT AS item_name
        ,rank_class_name
        ,rank_tag1
        ,rank_tag2
        ,rank_group_name
        ,rank_group_code        
        , avg_score
        , RANK() OVER(PARTITION BY rank_grade_year ORDER BY avg_score DESC) AS grade_rank
        , RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name ORDER BY avg_score DESC) AS dept_rank
        , RANK() OVER(PARTITION BY rank_class_name ORDER BY avg_score DESC) AS class_rank
        , RANK() OVER(PARTITION BY rank_grade_year, rank_tag1 ORDER BY avg_score DESC) AS tag1_rank
        , RANK() OVER(PARTITION BY rank_grade_year, rank_tag2 ORDER BY avg_score DESC) AS tag2_rank
        , RANK() OVER(PARTITION BY rank_grade_year, rank_group_code ORDER BY avg_score DESC) AS group_rank
        , RANK() OVER(PARTITION BY rank_grade_year ORDER BY avg_score ASC) AS grade_rank_reverse
        , RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name ORDER BY avg_score ASC) AS dept_rank_reverse
        , RANK() OVER(PARTITION BY rank_class_name ORDER BY avg_score ASC) AS class_rank_reverse
        , RANK() OVER(PARTITION BY rank_grade_year, rank_tag1 ORDER BY avg_score ASC) AS tag1_rank_reverse
        , RANK() OVER(PARTITION BY rank_grade_year, rank_tag2 ORDER BY avg_score ASC) AS tag2_rank_reverse
        , RANK() OVER(PARTITION BY rank_grade_year, rank_group_code ORDER BY avg_score ASC) AS group_rank_reverse
        , COUNT(student_id) OVER(PARTITION BY rank_grade_year) AS grade_count
        , COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_dept_name) AS dept_count
        , COUNT(student_id) OVER(PARTITION BY rank_class_name) AS class_count
        , COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_tag1) AS tag1_count
        , COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_tag2) AS tag2_count
        , COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_group_code) AS group_count
    FROM
        subject_score_english_avg_list
    WHERE       
        avg_score IS NOT NULL
)
, subject_score_english_avg_expand AS
(
    SELECT
        subject_score_english_rank_list.*
        , FLOOR((grade_rank::DECIMAL - 1)*100::DECIMAL / grade_count) + 1 AS graderank_percentage
        , FLOOR((dept_rank::DECIMAL - 1)*100::DECIMAL / dept_count) + 1 AS deptrank_percentage
        , FLOOR((class_rank::DECIMAL - 1)*100::DECIMAL / class_count) + 1 AS classrank_percentage
        , FLOOR((tag1_rank::DECIMAL - 1)*100::DECIMAL / tag1_count) + 1 AS tag1rank_percentage
        , FLOOR((tag2_rank::DECIMAL - 1)*100::DECIMAL / tag2_count) + 1 AS tag2rank_percentage
        , FLOOR((group_rank::DECIMAL - 1)*100::DECIMAL / group_count) + 1 AS grouprank_percentage
        , FLOOR((grade_rank_reverse::DECIMAL-1)*100::DECIMAL/grade_count) AS graderank_pr
        , FLOOR((class_rank_reverse::DECIMAL-1)*100::DECIMAL/class_count) AS classrank_pr
        , FLOOR((dept_rank_reverse::DECIMAL-1)*100::DECIMAL/dept_count) AS deptrank_pr
        , FLOOR((tag1_rank_reverse::DECIMAL-1)*100::DECIMAL/tag1_count) AS tag1ank_pr
        , FLOOR((tag2_rank_reverse::DECIMAL-1)*100::DECIMAL/tag2_count) AS tag2rank_pr
        , FLOOR((group_rank_reverse::DECIMAL-1)*100::DECIMAL/group_count) AS grouprank_pr
    FROM
        subject_score_english_rank_list
)

";

                // 多學期數學 - 使用 bucket_key 重構（效能優化）
                StringBuilder sbSubjectMathBucket = new StringBuilder();
                StringBuilder sbSubjectMathCaseWhen = new StringBuilder();
                string semstrMath = "";
                
                // 建立 bucket_key 的 CASE WHEN 語句
                sbSubjectMathCaseWhen.AppendLine("        CASE");
                foreach (string REGGroupName in RegGroupSubjMapDic.Keys)
                {
                    foreach (GradeYearSemesterInfo gs in calSemesterList)
                    {
                        int subjectNum = (gs.GradeYear - 1) * 2 + gs.Semester - 1;
                        string subjectList = RegGroupSubjMapDic[REGGroupName]["數學"][subjectNum];
                        
                        sbSubjectMathCaseWhen.AppendLine("            WHEN b.rank_group_name = " + SqlLiteral(REGGroupName) + 
                            " AND b.grade_year = " + gs.GradeYear + 
                            " AND b.semester = " + gs.Semester + 
                            " AND b.subject IN (" + subjectList + ")");
                        sbSubjectMathCaseWhen.AppendLine("                THEN " + SqlLiteral("數學_" + REGGroupName + "_" + gs.GradeYear + (gs.Semester == 1 ? "a" : "b")));
                    }
                }
                sbSubjectMathCaseWhen.AppendLine("            ELSE NULL");
                sbSubjectMathCaseWhen.AppendLine("        END AS bucket_key");
                
                // 建立 bucket CTE
                sbSubjectMathBucket.AppendLine(@",subject_score_math_bucket AS
(
    SELECT
        b.*,");
                sbSubjectMathBucket.Append(sbSubjectMathCaseWhen.ToString());
                sbSubjectMathBucket.AppendLine(@"
    FROM subject_entry_score_base b
)");
                
                // 判斷是否使用加權平均
                if (RegGroupSubjMapDic.Count > 0)
                {
                    string firstGroup = RegGroupSubjMapDic.Keys.First();
                    if (RegGroupCalcMapDic[firstGroup]["數學"][0] == "加權平均")
                        semstrMath = ",SUM(subject_origin_score * subject_credit) AS sum_score, SUM(subject_credit) AS sum_credit";
                    else
                        semstrMath = ",SUM(subject_origin_score) AS sum_score, COUNT(subject_credit) AS sum_credit";
                }
                else
                {
                    semstrMath = ",SUM(subject_origin_score * subject_credit) AS sum_score, SUM(subject_credit) AS sum_credit";
                }
                
                StringBuilder sbSubjectMathScore = new StringBuilder();
                sbSubjectMathScore.Append(sbSubjectMathBucket.ToString());

                    string Strsubject_score_math_avg_list = @"
,subject_score_math_avg_list AS
(
    SELECT
        student_id,
        student_name,
        rank_grade_year,
        rank_dept_name,
        rank_class_name,
        rank_tag1,
        rank_tag2,
        rank_group_name,
        rank_group_code,
        round(sum_score_s/sum_credit_s, parse_number) AS avg_score
    FROM
    (
        SELECT
            student_id,
            student_name,
            rank_grade_year,
            rank_dept_name,
            rank_class_name,
            rank_tag1,
            rank_tag2,
            rank_group_name,
            rank_group_code,
            SUM(sum_score) AS sum_score_s,
            SUM(sum_credit) AS sum_credit_s,
            (SELECT parse_number FROM parse_number)::INT AS parse_number
        FROM
        (
            SELECT
                student_id,
                student_name,
                rank_grade_year,
                rank_dept_name,
                rank_class_name,
                rank_tag1,
                rank_tag2,
                rank_group_name,
                rank_group_code" + semstrMath + @"
            FROM subject_score_math_bucket
            WHERE bucket_key IS NOT NULL
            GROUP BY
                student_id,
                student_name,
                rank_grade_year,
                rank_dept_name,
                rank_class_name,
                rank_tag1,
                rank_tag2,
                rank_group_name,
                rank_group_code,
                bucket_key
        ) AS subject_score_math_avg5
        GROUP BY
            student_id,
            student_name,
            rank_grade_year,
            rank_dept_name,
            rank_class_name,
            rank_tag1,
            rank_tag2,
            rank_group_name,
            rank_group_code
    ) AS subject_score_math_avg_round
)
,subject_score_math_rank_list AS
(
    SELECT
         student_id
        ,student_name
        ,rank_grade_year
        ,rank_dept_name
        ,'數學'::TEXT AS item_name
        ,rank_class_name
        ,rank_tag1
        ,rank_tag2
        ,rank_group_name
        ,rank_group_code
        , avg_score
        , RANK() OVER(PARTITION BY rank_grade_year ORDER BY avg_score DESC) AS grade_rank
        , RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name ORDER BY avg_score DESC) AS dept_rank
        , RANK() OVER(PARTITION BY rank_class_name ORDER BY avg_score DESC) AS class_rank
        , RANK() OVER(PARTITION BY rank_grade_year, rank_tag1 ORDER BY avg_score DESC) AS tag1_rank
        , RANK() OVER(PARTITION BY rank_grade_year, rank_tag2 ORDER BY avg_score DESC) AS tag2_rank
        , RANK() OVER(PARTITION BY rank_grade_year, rank_group_code ORDER BY avg_score DESC) AS group_rank
        , RANK() OVER(PARTITION BY rank_grade_year ORDER BY avg_score ASC) AS grade_rank_reverse
        , RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name ORDER BY avg_score ASC) AS dept_rank_reverse
        , RANK() OVER(PARTITION BY rank_class_name ORDER BY avg_score ASC) AS class_rank_reverse
        , RANK() OVER(PARTITION BY rank_grade_year, rank_tag1 ORDER BY avg_score ASC) AS tag1_rank_reverse
        , RANK() OVER(PARTITION BY rank_grade_year, rank_tag2 ORDER BY avg_score ASC) AS tag2_rank_reverse
        , RANK() OVER(PARTITION BY rank_grade_year, rank_group_code ORDER BY avg_score ASC) AS group_rank_reverse
        , COUNT(student_id) OVER(PARTITION BY rank_grade_year) AS grade_count
        , COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_dept_name) AS dept_count
        , COUNT(student_id) OVER(PARTITION BY rank_class_name) AS class_count
        , COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_tag1) AS tag1_count
        , COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_tag2) AS tag2_count
        , COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_group_code) AS group_count
    FROM
        subject_score_math_avg_list
    WHERE       
        avg_score IS NOT NULL
)
, subject_score_math_avg_expand AS
(
    SELECT
        subject_score_math_rank_list.*
        , FLOOR((grade_rank::DECIMAL - 1)*100::DECIMAL / grade_count) + 1 AS graderank_percentage
        , FLOOR((dept_rank::DECIMAL - 1)*100::DECIMAL / dept_count) + 1 AS deptrank_percentage
        , FLOOR((class_rank::DECIMAL - 1)*100::DECIMAL / class_count) + 1 AS classrank_percentage
        , FLOOR((tag1_rank::DECIMAL - 1)*100::DECIMAL / tag1_count) + 1 AS tag1rank_percentage
        , FLOOR((tag2_rank::DECIMAL - 1)*100::DECIMAL / tag2_count) + 1 AS tag2rank_percentage
        , FLOOR((group_rank::DECIMAL - 1)*100::DECIMAL / group_count) + 1 AS grouprank_percentage
        , FLOOR((grade_rank_reverse::DECIMAL-1)*100::DECIMAL/grade_count) AS graderank_pr
        , FLOOR((class_rank_reverse::DECIMAL-1)*100::DECIMAL/class_count) AS classrank_pr
        , FLOOR((dept_rank_reverse::DECIMAL-1)*100::DECIMAL/dept_count) AS deptrank_pr
        , FLOOR((tag1_rank_reverse::DECIMAL-1)*100::DECIMAL/tag1_count) AS tag1ank_pr
        , FLOOR((tag2_rank_reverse::DECIMAL-1)*100::DECIMAL/tag2_count) AS tag2rank_pr
        , FLOOR((group_rank_reverse::DECIMAL-1)*100::DECIMAL/group_count) AS grouprank_pr
    FROM
        subject_score_math_rank_list
)

";

                StringBuilder sbScoreListSQL = new StringBuilder();
                List<string> scNameList = new List<string>();
                scNameList.Add("subject_entry_score_avg_expand");  //學業
                scNameList.Add("subject_score_pro_avg_expand");    //專業/實習科目
                scNameList.Add("subject_score_SkillDomain_avg_expand");    //技能領域            
                scNameList.Add("subject_score_chinese_avg_expand");//國文
                scNameList.Add("subject_score_english_avg_expand");//英文
                scNameList.Add("subject_score_math_avg_expand");   //數學             

                sbScoreListSQL.AppendLine(", score_list AS(");
                int sbScoreItemNum = 1;
                foreach (string name in scNameList)
                    {
                        // 成績總表
                        string scoreListSQL = @"
    SELECT
		-1 :: INT AS rank_school_year
		, -1 :: INT AS rank_semester
		, rank_grade_year
		, '5學期/技職繁星比序(111年學年度適用)'::TEXT AS item_type
		, -1 :: INT AS ref_exam_id
		, item_name
		, '年排名'::TEXT AS rank_type
		, '' || rank_grade_year || '年級'::TEXT AS rank_name
		, true AS is_alive
		, grade_count AS matrix_count		
		, student_id
		, avg_score AS score
		, grade_rank AS rank
		, graderank_percentage AS percentile
        , graderank_pr AS pr
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
        , rank_group_name
        , rank_group_code
	FROM
		" + name + @"
     UNION ALL
    SELECT
		-1 :: INT AS rank_school_year
		, -1 :: INT AS rank_semester
		, rank_grade_year
		, '5學期/技職繁星比序(111年學年度適用)'::TEXT AS item_type
		, -1 :: INT AS ref_exam_id
		, item_name
		, '科排名'::TEXT AS rank_type
		, rank_dept_name::TEXT AS rank_name
		, true AS is_alive
		, dept_count AS matrix_count		
		, student_id
		, avg_score AS score
		, dept_rank AS rank
		, deptrank_percentage AS percentile
        , deptrank_pr AS pr
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
        , rank_group_name
        ,rank_group_code
	FROM
		" + name + @"
UNION ALL
    SELECT
		-1 :: INT AS rank_school_year
		, -1 :: INT AS rank_semester
		, rank_grade_year
		, '5學期/技職繁星比序(111年學年度適用)'::TEXT AS item_type
		, -1 :: INT AS ref_exam_id
		, item_name
		, '班排名'::TEXT AS rank_type
		, rank_class_name::TEXT AS rank_name
		, true AS is_alive
		, class_count AS matrix_count		
		, student_id
		, avg_score AS score
		, class_rank AS rank
		, classrank_percentage AS percentile
        , classrank_pr AS pr
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
        , rank_group_name
        , rank_group_code
	FROM
		" + name + @"
UNION ALL
    SELECT
		-1 :: INT AS rank_school_year
		, -1 :: INT AS rank_semester
		, rank_grade_year
		, '5學期/技職繁星比序(111年學年度適用)'::TEXT AS item_type
		, -1 :: INT AS ref_exam_id
		, item_name
		, '類別1排名'::TEXT AS rank_type
		, rank_tag1::TEXT AS rank_name
		, true AS is_alive
		, tag1_count AS matrix_count		
		, student_id
		, avg_score AS score
		, tag1_rank AS rank
		, tag1rank_percentage AS percentile
        , tag1ank_pr AS pr
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
        , rank_group_name
        , rank_group_code
	FROM
		" + name + @"
	WHERE
		rank_tag1 IS NOT NULL
		AND rank_tag1 <> ''

 UNION ALL
    SELECT
		-1 :: INT AS rank_school_year
		, -1 :: INT AS rank_semester
		, rank_grade_year
		, '5學期/技職繁星比序(111年學年度適用)'::TEXT AS item_type
		, -1 :: INT AS ref_exam_id
		, item_name
		, '學群排名'::TEXT AS rank_type
		, rank_group_code::TEXT AS rank_name
		, true AS is_alive
		, group_count AS matrix_count		
		, student_id
		, avg_score AS score
		, group_rank AS rank
		, grouprank_percentage AS percentile
        , grouprank_pr AS pr
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
        , rank_group_name
        , rank_group_code
	FROM
		" + name + @"
";
                        sbScoreListSQL.AppendLine(scoreListSQL);

                        if (sbScoreItemNum > 0 && sbScoreItemNum <6)  //6個比序
                            sbScoreListSQL.AppendLine(" UNION ALL");
                        sbScoreItemNum++;

                    }
                    sbScoreListSQL.AppendLine(")");
                    //sbScoreListSQL.AppendLine("select * from score_list");


                    //select * from score_list
                    //";






                    string insertUpdateSQL = @"
, update_data AS
(
	UPDATE
		rank_matrix
	SET
		is_alive = NULL	
	WHERE
		rank_matrix.is_alive = true
		AND rank_matrix.school_year = -1
		AND rank_matrix.semester = -1
		AND rank_matrix.grade_year = 3		
		AND rank_matrix.item_type = '5學期/技職繁星比序(111年學年度適用)'

	RETURNING rank_matrix.*
), insert_batch_data AS
(
	INSERT INTO
		rank_batch
		(
			school_year
			, semester
			, calculation_description
			, setting
		)
		VALUES
        (" + K12.Data.School.DefaultSchoolYear + @"
            ," + K12.Data.School.DefaultSemester + @"
            ,'" + K12.Data.School.DefaultSchoolYear + "學年度 第" + K12.Data.School.DefaultSemester + @"學期 技職繁星比序(111年學年度適用)'
            ,'" + calculationSetting + @"'
        )

	RETURNING *
)
, insert_matrix_data AS
(
	INSERT INTO
		rank_matrix
		(
			ref_batch_id
			, school_year
			, semester
			, grade_year
			, item_type
			, ref_exam_id
			, item_name
			, rank_type
			, rank_name
			, is_alive
			, matrix_count			
		)
		SELECT
			insert_batch_data.id AS ref_batch_id
			, score_list.rank_school_year::INT
			, score_list.rank_semester::INT
			, score_list.rank_grade_year::INT
			, score_list.item_type
			, score_list.ref_exam_id
			, score_list.item_name
			, score_list.rank_type
			, score_list.rank_name
			, score_list.is_alive
			, score_list.matrix_count			
		FROM
			score_list
			LEFT OUTER JOIN update_data
				ON update_data.id  < 0 --永遠為false，只是為了讓insert等待update執行完
			CROSS JOIN insert_batch_data
		GROUP BY
			insert_batch_data.id
			, score_list.rank_school_year
			, score_list.rank_semester
			, score_list.rank_grade_year
			, score_list.item_type
			, score_list.ref_exam_id
			, score_list.item_name
			, score_list.rank_type
			, score_list.rank_name
			, score_list.is_alive
			, score_list.matrix_count			

	RETURNING *
)
, insert_batch_student_data AS
(
	INSERT INTO
		rank_batch_student
		(
			ref_batch_id
			, ref_student_id
			, grade_year
			, matrix_grade
			, matrix_class
            , matrix_dept
			, matrix_tag1
			, matrix_tag2
		)
		SELECT
			insert_batch_data.id AS ref_batch_id
			, score_list.student_id
			, score_list.rank_grade_year::INT
			, score_list.rank_grade_year||'年級' AS matrix_grade
			, score_list.rank_class_name
            , score_list.rank_dept_name
			, score_list.rank_tag1 
            , score_list.rank_group_code
		FROM
			score_list
			CROSS JOIN insert_batch_data
)
, insert_detail_data AS
(
	INSERT INTO
		rank_detail
		(
			ref_matrix_id
			, ref_student_id
			, score
			, rank
            ,pr
			, percentile
		)
		SELECT
			insert_matrix_data.id AS ref_matrix_id
			, score_list.student_id AS ref_student_id
			, score_list.score AS score
			, score_list.rank AS rank
            , score_list.pr AS pr
			, score_list.percentile AS percentile
		FROM
			score_list
			LEFT OUTER JOIN
				insert_matrix_data
					ON insert_matrix_data.school_year = score_list.rank_school_year::INT
					AND insert_matrix_data.semester = score_list.rank_semester::INT
					AND insert_matrix_data.grade_year = score_list.rank_grade_year::INT
					AND insert_matrix_data.item_type = score_list.item_type
					AND insert_matrix_data.ref_exam_id = score_list.ref_exam_id
					AND insert_matrix_data.item_name = score_list.item_name
					AND insert_matrix_data.rank_type = score_list.rank_type
					AND insert_matrix_data.rank_name = score_list.rank_name
)
SELECT * from score_list limit 1
	

                ";

                insertRankSql += sbSubjectEntrySore.ToString() + Strsubject_entry_score_avg_list   //學業成績             
                + sbSubjectProSore.ToString() + Strsubject_score_pro_avg_list +//專業成績   
                sbSubjectSoreSkill.ToString()+ Strsubject_score_SkillDomain_avg_list + //技能領域
                 sbSubjectChineseScore.ToString() + Strsubject_score_chinese_avg_list//國文成績               
                +sbSubjectEnglishScore.ToString() + Strsubject_score_english_avg_list +//英文成績                
               sbSubjectMathScore.ToString() + Strsubject_score_math_avg_list +//數學成績
                
               sbScoreListSQL.ToString() + insertUpdateSQL;


                ////debug
                //string fiPath = Application.StartupPath + @"\sql1.sql";
                //using (System.IO.StreamWriter fi = new System.IO.StreamWriter(fiPath))
                //{
                //    fi.WriteLine(insertRankSql);
                //}

                #endregion


                bkw.ReportProgress(50);

                // Todo 2-1: 在 DoWork 中執行 SQL，發生例外時讓 BackgroundWorker 自動設定 bwE.Error
                QueryHelper queryHelper = new QueryHelper();
                queryHelper.Select(insertRankSql);

                bkw.ReportProgress(100);
                }
                catch (Exception ex)
                {
                    // Todo 2-1: 將例外傳遞給 RunWorkerCompleted 處理
                    bwE.Result = ex;
                }
            };

            bkw.RunWorkerCompleted += delegate (object bwSender, RunWorkerCompletedEventArgs bwE)
            {
                pbLoading.Visible = false;
                btnCacluate.Enabled = true;
                btnPrevious.Enabled = true;

                // Todo 2-2: 在 RunWorkerCompleted（UI thread）內統一處理成功 / 失敗
                if (bwE.Error != null || bwE.Result is Exception)
                {
                    var ex = bwE.Error ?? (Exception)bwE.Result;
                    MsgBox.Show("計算失敗：" + ex.Message);
                    FISCA.Presentation.MotherForm.SetStatusBarMessage("");
                    return;
                }

                MessageBox.Show("計算完成");
                FISCA.Presentation.MotherForm.SetStatusBarMessage("");
                this.DialogResult = DialogResult.OK;
            };

            // Todo 1-1: 將 rankStudents 透過 BackgroundWorker 傳入 DoWork
            bkw.RunWorkerAsync(rankStudents);
        }

        private void CalculateTechnologyAssessmentRankStep2_Resize(object sender, EventArgs e)
        {
            pbLoading.Location = new Point(this.Width / 2 - 20, this.Height / 2 - 20);
        }

        private void CalculateTechnologyAssessmentRankStep2_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Todo 4-1: 禁止計算中關閉視窗，避免跨執行緒操作已 Dispose 的控制項
            if (pbLoading.Visible)
            {
                MsgBox.Show("正在計算中，請稍候計算完成再關閉視窗。");
                e.Cancel = true;
            }
        }

        private void btnExportNoRank_Click(object sender, EventArgs e)
        {
            if (StudentNotRankDict.Count > 0)
            {
                try
                {
                    btnExportNoRank.Enabled = false;


                    Workbook wb = new Workbook();
                    wb.Worksheets[0].Name = "不排名學生";
                    wb.Worksheets[0].Cells[0, 0].PutValue("學號");
                    wb.Worksheets[0].Cells[0, 1].PutValue("班級");
                    wb.Worksheets[0].Cells[0, 2].PutValue("座號");
                    wb.Worksheets[0].Cells[0, 3].PutValue("姓名");
                    wb.Worksheets[0].Cells[0, 4].PutValue("不排名原因");
                    int rIdx = 1;
                    foreach (StudentRecord studRec in StudentAllList)
                    {
                        if (StudentNotRankDict.ContainsKey(studRec.ID))
                        {
                            wb.Worksheets[0].Cells[rIdx, 0].PutValue(studRec.StudentNumber);
                            if (studRec.Class != null)
                                wb.Worksheets[0].Cells[rIdx, 1].PutValue(studRec.Class.Name);
                            if (studRec.SeatNo.HasValue)
                                wb.Worksheets[0].Cells[rIdx, 2].PutValue(studRec.SeatNo.Value);
                            wb.Worksheets[0].Cells[rIdx, 3].PutValue(studRec.Name);
                            wb.Worksheets[0].Cells[rIdx, 4].PutValue(StudentNotRankDict[studRec.ID]);
                            rIdx++;
                        }
                    }

                    if (wb != null)
                    {
                        #region 儲存檔案
                        string reportName = "技職繁星不排名學生";

                        string path = Path.Combine(System.Windows.Forms.Application.StartupPath, "Reports");
                        if (!Directory.Exists(path))
                            Directory.CreateDirectory(path);
                        path = Path.Combine(path, reportName + ".xls");

                        try
                        {


                            if (File.Exists(path))
                            {
                                int i = 1;
                                while (true)
                                {
                                    string newPath = Path.GetDirectoryName(path) + "\\" + Path.GetFileNameWithoutExtension(path) + (i++) + Path.GetExtension(path);
                                    if (!File.Exists(newPath))
                                    {
                                        path = newPath;
                                        break;
                                    }
                                }
                            }
                            wb.Save(path, SaveFormat.Excel97To2003);
                            System.Diagnostics.Process.Start(path);
                        }
                        catch
                        {
                            System.Windows.Forms.SaveFileDialog sd = new System.Windows.Forms.SaveFileDialog();
                            sd.Title = "另存新檔";
                            sd.FileName = reportName + ".xls";
                            sd.Filter = "Excel檔案 (*.xls)|*.xls|所有檔案 (*.*)|*.*";
                            if (sd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                            {
                                try
                                {
                                    wb.Save(sd.FileName, SaveFormat.Excel97To2003);

                                }
                                catch
                                {
                                    FISCA.Presentation.Controls.MsgBox.Show("指定路徑無法存取。", "建立檔案失敗", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                                    return;
                                }
                            }
                        }
                        #endregion
                    }
                    else
                    {
                        MsgBox.Show("Excel 檔案無法產生。");
                    }
                }
                catch (Exception ex)
                {
                    MsgBox.Show("產生不排名學生錯誤," + ex.Message);
                    btnExportNoRank.Enabled = true;
                }


                btnExportNoRank.Enabled = true;
            }
        }
    }
}
