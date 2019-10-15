using FISCA.Presentation.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using K12.Data;
using FISCA.Data;
using FISCA.Presentation;
using System.Xml;

namespace SHEvaluation.Rank
{
    public partial class CalculateRegularAssessmentRank : BaseForm
    {
        string _DefaultSchoolYear = "";
        string _DefaultSemester = "";
        List<ExamRecord> _ExamList = new List<ExamRecord>();
        List<TagConfigRecord> _TagConfigRecord = new List<TagConfigRecord>();
        List<StudentRecord> _StudentRecord = new List<StudentRecord>();
        List<CheckBox> _CheckBoxList = new List<CheckBox>();
        List<StudentRecord> _FilterStudentList = new List<StudentRecord>();

        XmlElement _ConfigElement = null;
        List<string> _FilteredSubject = new List<string>();
        List<string> _Tag1FilteredSubject = new List<string>();
        List<string> _Tag2FilteredSubject = new List<string>();

        public CalculateRegularAssessmentRank()
        {
            InitializeComponent();
            try
            {
                _DefaultSchoolYear = K12.Data.School.DefaultSchoolYear;
                _DefaultSemester = K12.Data.School.DefaultSemester;
                _ExamList = K12.Data.Exam.SelectAll();
                _TagConfigRecord = K12.Data.TagConfig.SelectByCategory(TagCategory.Student);
                _StudentRecord = K12.Data.Student.SelectAll().ToList();
            }
            catch (Exception ex)
            {
                throw new Exception("資料讀取失敗", ex);
            }

            string configString = K12.Data.School.Configuration["固定排名_定期評量排名計算"]["設定檔"];
            if (configString != "")
            {
                //< Setting 考試名稱 = "lbExam.Text" 不排名學生類別 = "cboStudentFilter.Text" 類別一 = "cboStudentTag1.Text" 類別二 = "cboStudentTag2.Text" >
                //    < 不採計科目 > OO </ 不採計科目 >
                //    < 不採計科目 > OO </ 不採計科目 >
                //    < 不採計科目 > OO </ 不採計科目 >
                //    < 類別一不採計科目 > OO </ 類別一不採計科目 >
                //    < 類別一不採計科目 > OO </ 類別一不採計科目 >
                //    < 類別二不採計科目 > OO </ 類別二不採計科目 >
                //    < 類別二不採計科目 > OO </ 類別二不採計科目 >
                //</ Setting >
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(configString);
                _ConfigElement = doc.DocumentElement;
                foreach (XmlElement item in _ConfigElement.SelectNodes("不採計科目"))
                {
                    _FilteredSubject.Add(item.InnerText);
                }
                foreach (XmlElement item in _ConfigElement.SelectNodes("類別一不採計科目"))
                {
                    _Tag1FilteredSubject.Add(item.InnerText);
                }
                foreach (XmlElement item in _ConfigElement.SelectNodes("類別二不採計科目"))
                {
                    _Tag2FilteredSubject.Add(item.InnerText);
                }
            }
        }

        private void CacluateRegularAssessmentRank_Load(object sender, EventArgs e)
        {
            #region 讓Form回到起始狀態
            plSetting.Visible = true;
            plStudentView.Visible = false;
            this.CalculateRegularAssessmentRank_Resize(null, null);
            #endregion

            #region 篩選學生資料
            _StudentRecord = _StudentRecord.Where(
                x => !string.IsNullOrEmpty(x.RefClassID)
                && (x.Status == StudentRecord.StudentStatus.一般 || x.Status == StudentRecord.StudentStatus.延修)
                && x.Class.GradeYear != null
            ).ToList();
            #endregion

            #region 動態產生年級的CheckBox
            //整理年級的清單
            List<int> gradeList = _StudentRecord.Select(x => Convert.ToInt32(x.Class.GradeYear)).Distinct().OrderBy(x => x).ToList();
            for (int i = 0; i < gradeList.Count; i++)
            {
                CheckBox checkBox = new CheckBox();
                checkBox.AutoSize = true;
                checkBox.Name = "ch" + gradeList[i];
                checkBox.TabIndex = 7 + i;
                checkBox.Text = "" + gradeList[i] + "年級";
                checkBox.UseVisualStyleBackColor = true;
                checkBox.Checked = true;
                checkBox.CheckedChanged += reloadSubject;
                flowLayoutPanel1.Controls.Add(checkBox);
            }
            #endregion

            #region 填資料進ComboBox
            //因為目前只提供計算預設學年度學期的排名，所以暫時先註解起來
            //cboSchoolYear.Items.Clear();
            //cboSemester.Items.Clear();
            cboExamType.Items.Clear();
            cboStudentFilter.Items.Clear();
            cboStudentTag1.Items.Clear();
            cboStudentTag2.Items.Clear();

            lbCalcSchoolYear.Text = _DefaultSchoolYear;
            lbCalcSemester.Text = _DefaultSemester;
            //cboSchoolYear.Items.Add(_DefaultSchoolYear);//加入預設的學年度
            //cboSemester.Items.Add(_DefaultSemester);//加入預設的學年度
            //foreach (DataRow row in _SchoolYearTable.Rows)
            //{
            //    #region 現階段先不用匯入其他學年度及學期
            //    //現階段先不用匯入其他學年度
            //    //if (!string.IsNullOrEmpty("" + row["school_year"]) && !cboSchoolYear.Items.Contains("" + row["school_year"]))
            //    //{
            //    //    cboSchoolYear.Items.Add("" + row["school_year"]);
            //    //} 

            //    //if (!string.IsNullOrEmpty("" + row["semester"]) && !cboSemester.Items.Contains("" + row["semester"]))
            //    //{
            //    //    cboSemester.Items.Add("" + row["semester"]);
            //    //}
            //    #endregion
            //}
            //cboSchoolYear.SelectedIndex = 0;

            //if (cboSemester.Items.Contains(_DefaultSemester))
            //{
            //    cboSemester.SelectedIndex = cboSemester.Items.IndexOf(_DefaultSemester);
            //}
            //else
            //{
            //    cboSemester.SelectedIndex = 0;
            //}

            cboStudentFilter.Items.Add("");
            cboStudentTag1.Items.Add("");
            cboStudentTag2.Items.Add("");

            int cboStudentFilterIndex = 0;
            int cboStudentTag1Index = 0;
            int cboStudentTag2Index = 0;
            int cboExamTypeIndex = 0;
            foreach (var item in _TagConfigRecord.Select(x => x.Prefix).Distinct())
            {
                if (!string.IsNullOrEmpty(item))
                {
                    var index1 = cboStudentFilter.Items.Add("[" + item + "]");
                    var index2 = cboStudentTag1.Items.Add("[" + item + "]");
                    var index3 = cboStudentTag2.Items.Add("[" + item + "]");
                    //< Setting 考試名稱 = "cboExamType.Text" 不排名學生類別 = "cboStudentFilter.Text" 類別一 = "cboStudentTag1.Text" 類別二 = "cboStudentTag2.Text" >
                    //    < 不採計科目 > OO </ 不採計科目 >
                    //    < 不採計科目 > OO </ 不採計科目 >
                    //    < 不採計科目 > OO </ 不採計科目 >
                    //    < 類別一不採計科目 > OO </ 類別一不採計科目 >
                    //    < 類別一不採計科目 > OO </ 類別一不採計科目 >
                    //    < 類別二不採計科目 > OO </ 類別二不採計科目 >
                    //    < 類別二不採計科目 > OO </ 類別二不採計科目 >
                    //</ Setting >
                    if (_ConfigElement != null && _ConfigElement.GetAttribute("不排名學生類別") == "[" + item + "]")
                        cboStudentFilterIndex = index1;
                    if (_ConfigElement != null && _ConfigElement.GetAttribute("類別一") == "[" + item + "]")
                        cboStudentTag1Index = index2;
                    if (_ConfigElement != null && _ConfigElement.GetAttribute("類別二") == "[" + item + "]")
                        cboStudentTag2Index = index3;
                }
            }
            foreach (string tagName in _TagConfigRecord.Where(x => string.IsNullOrEmpty(x.Prefix)).Select(x => x.Name).ToList())
            {
                var index1 = cboStudentFilter.Items.Add(tagName);
                var index2 = cboStudentTag1.Items.Add(tagName);
                var index3 = cboStudentTag2.Items.Add(tagName);
                if (_ConfigElement != null && _ConfigElement.GetAttribute("不排名學生類別") == tagName)
                    cboStudentFilterIndex = index1;
                if (_ConfigElement != null && _ConfigElement.GetAttribute("類別一") == tagName)
                    cboStudentTag1Index = index2;
                if (_ConfigElement != null && _ConfigElement.GetAttribute("類別二") == tagName)
                    cboStudentTag2Index = index3;
            }
            cboStudentFilter.SelectedIndex = cboStudentFilterIndex;
            cboStudentTag1.SelectedIndex = cboStudentTag1Index;
            cboStudentTag2.SelectedIndex = cboStudentTag2Index;

            foreach (var item in _ExamList.Select(x => x.Name).Distinct())
            {
                int index = cboExamType.Items.Add(item);
                if (_ConfigElement != null && _ConfigElement.GetAttribute("考試名稱") == item)
                    cboExamTypeIndex = index;
            }
            cboExamType.SelectedIndex = cboExamTypeIndex;
            #endregion
        }

        private void reloadSubject(object eander, EventArgs e)
        {

            lvCalcSubject.Items.Clear();
            lvCalcSubjectTag1.Items.Clear();
            lvCalcSubjectTag2.Items.Clear();
            #region 取得科目
            List<string> gradeList = new List<string>();
            gradeList.Add("" + int.MinValue);
            foreach (CheckBox checkBox in flowLayoutPanel1.Controls.OfType<CheckBox>())
            {
                if (checkBox.Checked == true)
                {
                    gradeList.Add(checkBox.Text.Trim('年', '級'));
                }
            }
            var examID = "-1";
            foreach (var item in _ExamList)
            {
                if (item.Name == "" + cboExamType.SelectedItem)
                    examID = item.ID;
            }
            var subjectTable = new QueryHelper().Select(@"

SELECT
	subject
FROM (
	SELECT
		subject
		, row_number() OVER () as subject_order
	FROM (
		SELECT DISTINCT
			course.credit
			, course.period
			, course.subject
		FROM
			student
			INNER JOIN class
				ON class.id = student.ref_class_id
				AND class.grade_year IN (" + string.Join(", ", gradeList) + @")
			INNER JOIN sc_attend
				ON sc_attend.ref_student_id = student.id
			INNER JOIN course
				ON course.id = sc_attend.ref_course_id
				AND course.school_year = " + _DefaultSchoolYear + @"
				AND course.semester = " + _DefaultSemester + @"
			INNER JOIN sce_take
				ON sce_take.ref_sc_attend_id = sc_attend.id
				AND sce_take.ref_exam_id = " + examID + @"
		ORDER BY
			course.credit DESC
			, course.period DESC
			, course.subject
	) AS s
) AS s
GROUP BY
	subject
ORDER BY
	MIN(subject_order)");
            #endregion
            foreach (DataRow row in subjectTable.Rows)
            {
                lvCalcSubject.Items.Add("" + row["subject"]).Checked = !_FilteredSubject.Contains("" + row["subject"]);
                lvCalcSubjectTag1.Items.Add("" + row["subject"]).Checked = !_Tag1FilteredSubject.Contains("" + row["subject"]);
                lvCalcSubjectTag2.Items.Add("" + row["subject"]).Checked = !_Tag2FilteredSubject.Contains("" + row["subject"]);
            }
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            plSetting.Visible = false;
            plStudentView.Visible = true;
            lbExam.Text = cboExamType.Text;

            //因為目前只提供計算預設學年度學期的排名，所以暫時先註解起來
            lbSchoolYear.Text = lbCalcSchoolYear.Text; //cboSemester.Text;
            lbSemester.Text = lbCalcSemester.Text; //cboSchoolYear.Text;

            #region 依據勾選的項目動態產生CheckBox
            int checkBoxCount = 0;
            foreach (CheckBox checkBox in flowLayoutPanel1.Controls.OfType<CheckBox>())
            {
                if (checkBox.Checked == true)
                {
                    CheckBox newCheckBox = new CheckBox();
                    newCheckBox.Location = new System.Drawing.Point(65 + (97 * (checkBoxCount % 4)), 44 + (27 * (checkBoxCount / 4)));//第一個checkBox的位置X=65, y=44，兩個checkBox的X差距97，兩個checkBox的Y差距27
                    newCheckBox.Name = "new" + checkBox.Name;
                    newCheckBox.Size = new System.Drawing.Size(91, 21);
                    newCheckBox.TabIndex = 26 + checkBoxCount;
                    newCheckBox.Text = checkBox.Text;
                    newCheckBox.UseVisualStyleBackColor = true;
                    newCheckBox.Enabled = false;
                    newCheckBox.Checked = true;
                    newCheckBox.Visible = false;//因為不需要且_CheckBoxList後面用的到，所以保留此功能但不顯示

                    plStudentView.Controls.Add(newCheckBox);
                    _CheckBoxList.Add(newCheckBox);
                    checkBoxCount++;
                }
            }
            #endregion

            #region 讀取學生清單
            _FilterStudentList = new List<StudentRecord>();
            string studentFilter = cboStudentFilter.Text.Trim('[', ']');
            string studentTag1 = cboStudentTag1.Text.Trim('[', ']');
            string studentTag2 = cboStudentTag2.Text.Trim('[', ']');
            Exception bkwException = null;
            DataTable deptDataTable = new DataTable();
            BackgroundWorker bkw = new BackgroundWorker();
            bkw.WorkerReportsProgress = true;

            btnPrevious.Enabled = false;

            bkw.ProgressChanged += delegate (object s1, ProgressChangedEventArgs e1)
            {
                MotherForm.SetStatusBarMessage("資料載入中", e1.ProgressPercentage);
            };

            bkw.DoWork += delegate
            {
                try
                {
                    #region 依據條件篩選學生
                    bkw.ReportProgress(1);
                    foreach (string gradeYear in _CheckBoxList.Select(x => x.Text))
                    {
                        _FilterStudentList.AddRange(_StudentRecord.Where(x => x.Class.GradeYear == Convert.ToInt32(gradeYear.Trim('年', '級'))).ToList());
                    }

                    bkw.ReportProgress(50);
                    if (!string.IsNullOrEmpty(studentFilter))
                    {
                        List<string> studentFilterTagIDs = _TagConfigRecord.Where(x => x.Prefix == studentFilter).Select(x => x.ID).ToList();
                        if (studentFilterTagIDs.Count == 0)
                        {
                            studentFilterTagIDs = _TagConfigRecord.Where(x => x.Name == studentFilter).Select(x => x.ID).ToList();
                        }
                        List<string> filterStudentID = K12.Data.StudentTag.SelectAll().Where(x => studentFilterTagIDs.Contains(x.RefTagID)).Select(x => x.RefStudentID).ToList();
                        _FilterStudentList = _FilterStudentList.Where(x => !filterStudentID.Contains(x.ID)).ToList();
                    }

                    bkw.ReportProgress(70);
                    #region 取得篩選學生的科別
                    string studentIDs = string.Join(",", _FilterStudentList.Select(x => x.ID).ToList());

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

                    QueryHelper queryHelper = new QueryHelper();
                    deptDataTable = queryHelper.Select(queryDeptSQL);

                    #endregion
                    bkw.ReportProgress(100);
                    #endregion
                }
                catch (Exception ex)
                {
                    bkwException = ex;
                }
            };

            bkw.RunWorkerCompleted += delegate
            {
                if (bkwException != null)
                {
                    throw new Exception("資料讀取失敗", bkwException);
                }

                if (_FilterStudentList.Count == 0)
                {
                    btnCacluate.Enabled = false;
                    MessageBox.Show("沒有找到符合條件的學生");
                    btnPrevious.Enabled = true;
                    return;
                }

                #region 將學生清單顯示在dataGridView上
                var studentView = (from s in _FilterStudentList
                                   select new
                                   {
                                       studentId = s.ID,
                                       ClassName = s.Class.Name,
                                       s.SeatNo,
                                       s.StudentNumber,
                                       s.Name,
                                       RankGradeYear = "" + s.Class.GradeYear + "年級",
                                       RankClassName = s.Class.Name,
                                   }).ToList();

                #region 取得符合類別的學生
                List<string> tag1IDs = new List<string>();
                List<string> tag2IDs = new List<string>();
                if (!string.IsNullOrEmpty(studentTag1))
                {
                    tag1IDs = _TagConfigRecord.Where(x => x.Prefix == studentTag1).Select(x => x.ID).ToList();
                    if (tag1IDs.Count == 0)
                    {
                        tag1IDs = _TagConfigRecord.Where(x => x.Name == studentTag1).Select(x => x.ID).ToList();
                    }
                }
                if (!string.IsNullOrEmpty(studentTag2))
                {
                    tag2IDs = _TagConfigRecord.Where(x => x.Prefix == studentTag2).Select(x => x.ID).ToList();
                    if (tag2IDs.Count == 0)
                    {
                        tag2IDs = _TagConfigRecord.Where(x => x.Name == studentTag2).Select(x => x.ID).ToList();
                    }
                }
                List<StudentTagRecord> studentTag1List = K12.Data.StudentTag.SelectAll().Where(x => tag1IDs.Contains(x.RefTagID)).ToList();
                List<StudentTagRecord> studentTag2List = K12.Data.StudentTag.SelectAll().Where(x => tag2IDs.Contains(x.RefTagID)).ToList();
                #endregion

                List<DataGridViewRow> rowList = new List<DataGridViewRow>();
                for (int rowIndex = 0; rowIndex < studentView.Count; rowIndex++)
                {
                    string tag1 = "", tag2 = "";
                    DataGridViewRow row = new DataGridViewRow();
                    row.CreateCells(dgvStudentList);
                    row.Tag = studentView[rowIndex].studentId;
                    row.Cells[0].Value = studentView[rowIndex].ClassName;
                    row.Cells[1].Value = studentView[rowIndex].SeatNo;
                    row.Cells[2].Value = studentView[rowIndex].StudentNumber;
                    row.Cells[3].Value = studentView[rowIndex].Name;
                    row.Cells[4].Value = studentView[rowIndex].RankGradeYear;
                    DataRow[] dataRow = deptDataTable.Select("student_id = '" + studentView[rowIndex].studentId + "'");
                    row.Cells[5].Value = dataRow.First()["deptname"];
                    row.Cells[6].Value = studentView[rowIndex].RankClassName;
                    if (studentTag1List.Select(x => x.RefStudentID).Contains(studentView[rowIndex].studentId))
                    {
                        tag1 = studentTag1List.First(x => x.RefStudentID == studentView[rowIndex].studentId).Name;
                        row.Cells[7].Value = tag1;
                    }
                    if (studentTag2List.Select(x => x.RefStudentID).Contains(studentView[rowIndex].studentId))
                    {
                        tag2 = studentTag2List.First(x => x.RefStudentID == studentView[rowIndex].studentId).Name;
                        row.Cells[8].Value = tag2;
                    }

                    rowList.Add(row);
                }
                dgvStudentList.Rows.AddRange(rowList.ToArray());
                btnPrevious.Enabled = true;
                #endregion
            };

            bkw.RunWorkerAsync();
            #endregion
        }

        private void btnCacluate_Click(object sender, EventArgs e)
        {
            #region 儲存設定
            {
                XmlDocument document = new XmlDocument();
                XmlElement configEle = document.CreateElement("Setting");
                configEle.SetAttribute("考試名稱", lbExam.Text);
                configEle.SetAttribute("不排名學生類別", cboStudentFilter.Text);
                configEle.SetAttribute("類別一", cboStudentTag1.Text);
                configEle.SetAttribute("類別二", cboStudentTag2.Text);
                foreach (ListViewItem item in lvCalcSubject.Items)
                {
                    if (item.Checked)
                    {
                        if (_FilteredSubject.Contains(item.Text))
                            _FilteredSubject.Remove(item.Text);
                    }
                    else
                    {
                        if (!_FilteredSubject.Contains(item.Text))
                            _FilteredSubject.Add(item.Text);
                    }
                }
                foreach (ListViewItem item in lvCalcSubjectTag1.Items)
                {
                    if (item.Checked)
                    {
                        if (_Tag1FilteredSubject.Contains(item.Text))
                            _Tag1FilteredSubject.Remove(item.Text);
                    }
                    else
                    {
                        if (!_Tag1FilteredSubject.Contains(item.Text))
                            _Tag1FilteredSubject.Add(item.Text);
                    }
                }
                foreach (ListViewItem item in lvCalcSubjectTag2.Items)
                {
                    if (item.Checked)
                    {
                        if (_Tag2FilteredSubject.Contains(item.Text))
                            _Tag2FilteredSubject.Remove(item.Text);
                    }
                    else
                    {
                        if (!_Tag2FilteredSubject.Contains(item.Text))
                            _Tag2FilteredSubject.Add(item.Text);
                    }
                }
                foreach (var item in _FilteredSubject)
                {
                    var ele = document.CreateElement("不採計科目");
                    ele.InnerText = item;
                    configEle.AppendChild(ele);
                }
                foreach (var item in _Tag1FilteredSubject)
                {
                    var ele = document.CreateElement("類別一不採計科目");
                    ele.InnerText = item;
                    configEle.AppendChild(ele);
                }
                foreach (var item in _Tag2FilteredSubject)
                {
                    var ele = document.CreateElement("類別二不採計科目");
                    ele.InnerText = item;
                    configEle.AppendChild(ele);
                }

                var cd = K12.Data.School.Configuration["固定排名_定期評量排名計算"];
                cd["設定檔"] = configEle.OuterXml;
                cd.Save();
            }
            #endregion

            List<string> studentSqlList = new List<string>();
            foreach (DataGridViewRow row in dgvStudentList.Rows)
            {
                //每一筆學生先組好先加進List裡
                studentSqlList.Add(@"
    SELECT
        '" + row.Tag + @"'::BIGINT AS student_id
        ,'" + row.Cells[3].Value + @"'::TEXT AS student_name
        ,'" + ("" + row.Cells[4].Value).Trim('年', '級') + @"'::INT AS rank_grade_year
        ,'" + "" + row.Cells[5].Value + @"'::TEXT AS rank_dept_name
        ,'" + "" + row.Cells[6].Value + @"'::TEXT AS rank_class_name
        ,'" + "" + row.Cells[7].Value + @"'::TEXT AS rank_tag1
        ,'" + "" + row.Cells[8].Value + @"'::TEXT AS rank_tag2
    ");
            }

            btnCacluate.Enabled = false;
            btnPrevious.Enabled = false;
            string schoolYear = lbSchoolYear.Text;
            string semester = lbSemester.Text;
            string examName = lbExam.Text;
            string examId = _ExamList.First(x => x.Name == examName).ID;
            string tag1 = cboStudentTag1.Text.Trim('[', ']');
            string tag2 = cboStudentTag2.Text.Trim('[', ']');
            string studentFilter = cboStudentFilter.Text.Trim('[', ']');
            List<int> gradeYearList = new List<int>();
            foreach (CheckBox checkBox in _CheckBoxList)
            {
                gradeYearList.Add(Convert.ToInt32(checkBox.Text.Trim('年', '級')));
            }

            #region 產生計算設定的字串
            XmlDocument doc = new XmlDocument();
            var settingEle = doc.CreateElement("Setting");
            settingEle.SetAttribute("學年度", "" + schoolYear);
            settingEle.SetAttribute("學期", "" + semester);
            settingEle.SetAttribute("考試名稱", "" + examName);
            settingEle.SetAttribute("不排名學生類別", "" + studentFilter);
            settingEle.SetAttribute("類別一", "" + tag1);
            settingEle.SetAttribute("類別二", "" + tag2);
            foreach (var gradeYear in gradeYearList)
            {
                var gradeYearEle = doc.CreateElement("年級");
                gradeYearEle.InnerText = "" + gradeYear;
                settingEle.AppendChild(gradeYearEle);
            }
            foreach (ListViewItem item in lvCalcSubject.Items)
            {
                if (item.Checked)
                {
                    var ele = doc.CreateElement("採計科目");
                    ele.InnerText = item.Text;
                    settingEle.AppendChild(ele);
                }
            }
            foreach (ListViewItem item in lvCalcSubjectTag1.Items)
            {
                if (item.Checked)
                {
                    var ele = doc.CreateElement("類別一採計科目");
                    ele.InnerText = item.Text;
                    settingEle.AppendChild(ele);
                }
            }
            foreach (ListViewItem item in lvCalcSubjectTag2.Items)
            {
                if (item.Checked)
                {
                    var ele = doc.CreateElement("類別二採計科目");
                    ele.InnerText = item.Text;
                    settingEle.AppendChild(ele);
                }
            }
            #endregion

            Exception bkwException = null;
            BackgroundWorker bkw = new BackgroundWorker();
            bkw.WorkerReportsProgress = true;
            pbLoading.Visible = true;

            bkw.ProgressChanged += delegate (object s1, ProgressChangedEventArgs e1)
            {
                MotherForm.SetStatusBarMessage("計算排名中", e1.ProgressPercentage);
            };

            bkw.DoWork += delegate
            {
                try
                {
                    bkw.ReportProgress(1);
                    List<string> rowSqlList = new List<string>();

                    for (int index = 0; index < gradeYearList.Count; index++)
                    {
                        //每一筆row(包含GradeYear, SchoolYear, Semester, ExamName)先組好加進List
                        rowSqlList.Add(@"
	SELECT
		'" + gradeYearList[index] + @"'::TEXT  AS rank_grade_year
		, '" + schoolYear + @"'::TEXT AS rank_school_year
		, '" + semester + @"'::TEXT AS rank_semester
        , '" + examId + @"'::TEXT AS ref_exam_id
		, '" + examName + @"'::TEXT AS rank_exam_name
        , '" + settingEle.OuterXml.Replace("'", "''") + @"'::TEXT AS calculation_setting
");
                    }

                    bkw.ReportProgress(20);

                    #region 計算排名的SQL
                    string insertRankSql = @"
WITH row AS (
" + string.Join(@"
    UNION ALL
", rowSqlList) + @"
), student_row AS (
" + string.Join(@"
    UNION ALL
", studentSqlList) + @"
), calc_subject AS ( --採計科目
    SELECT
        array_to_string(xpath('./text()', eleSubject), '')::TEXT as subject
    FROM (
        SELECT
            unnest(xpath('/Setting/採計科目', xmlparse(content calculation_setting))) AS eleSubject
        FROM
            row
    ) as ele
), calc_subject_tag1 AS ( --類別一採計科目
    SELECT
        array_to_string(xpath('./text()', eleSubject), '')::TEXT as subject
    FROM (
        SELECT
            unnest(xpath('/Setting/類別一採計科目', xmlparse(content calculation_setting))) AS eleSubject
        FROM
            row
    ) as ele
), calc_subject_tag2 AS ( --類別二採計科目
    SELECT
        array_to_string(xpath('./text()', eleSubject), '')::TEXT as subject
    FROM (
        SELECT
            unnest(xpath('/Setting/類別二採計科目', xmlparse(content calculation_setting))) AS eleSubject
        FROM
            row
    ) as ele
), score_detail_row AS (--取得學生的定期評量成績
	SELECT
		student_row.student_id
   		, student_row.student_name
		, sc_attend.id AS sc_attend_id
		, course.course_name
		, course.school_year AS rank_school_year
		, course.semester AS rank_semester
		, course.subject
		, course.domain
		, course.credit
		, exam.id AS exam_id
		, exam.exam_name
		, student_row.rank_class_name
		, student_row.rank_grade_year
        , student_row.rank_dept_name
		, student_row.rank_tag1
		, student_row.rank_tag2
		, sce_take.score
	FROM  
        sce_take
		LEFT JOIN sc_attend 
			ON ref_sc_attend_id = sc_attend.id
		LEFT JOIN exam 
			ON ref_exam_id = exam.id
		LEFT JOIN course 
			ON sc_attend.ref_course_id = course.id
            AND course.subject IN (
                SELECT subject FROM calc_subject
            )
		INNER JOIN student_row
			ON sc_attend.ref_student_id = student_row.student_id
		INNER JOIN row
			ON course.school_year = row.rank_school_year::int
			AND course.semester = row.rank_semester::int
			AND student_row.rank_grade_year = row.rank_grade_year::int
			AND exam.exam_name= row.rank_exam_name
	WHERE 
        sce_take.score IS NOT NULL

    --2.1 科目成績 年排名
    --2.2 科目成績 班排名
    --2.5 科目成績 科排名
), exam_score AS (-------結算定期評量總成績
	SELECT	
		score_detail_row.*
	FROM 
		score_detail_row
), subject_rank_row AS (--------計算科目排名
	SELECT
		student_id
		, rank_tag1
		, rank_tag2
		, '定期評量/科目成績'::TEXT AS item_type
		, subject AS item_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, rank_dept_name
		, exam_id
		, score
		, RANK() OVER(PARTITION BY rank_grade_year,subject ORDER BY score DESC) AS grade_rank
		, RANK() OVER(PARTITION BY rank_class_name ,subject ORDER BY score DESC) AS class_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name ,subject ORDER BY score DESC) AS dept_rank
		, RANK() OVER(PARTITION BY rank_grade_year,subject ORDER BY score ASC) AS grade_rank_reverse
		, RANK() OVER(PARTITION BY rank_class_name ,subject ORDER BY score ASC) AS class_rank_reverse
		, RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name ,subject ORDER BY score ASC) AS dept_rank_reverse
		, COUNT (student_id) OVER(PARTITION BY rank_grade_year,subject ) AS grade_count
		, COUNT (student_id) OVER(PARTITION BY rank_class_name, subject) AS class_count
		, COUNT (student_id) OVER(PARTITION BY rank_grade_year, rank_dept_name, subject) AS dept_count
        , subject
	FROM 
        exam_score
	WHERE 
        subject IS NOT NULL
), subject_rank AS (-----------計算科目排名排名百分比及PR
	SELECT  
		s1.*
		, FLOOR((grade_rank::DECIMAL-1)*100::DECIMAL/grade_count)+1 AS graderank_percentage
		, FLOOR((class_rank::DECIMAL-1)*100::DECIMAL/class_count)+1 AS classrank_percentage
		, FLOOR((dept_rank::DECIMAL-1)*100::DECIMAL/dept_count)+1 AS deptrank_percentage
        , FLOOR((grade_rank_reverse::DECIMAL-1)*100::DECIMAL/grade_count) AS graderank_pr
        , FLOOR((class_rank_reverse::DECIMAL-1)*100::DECIMAL/class_count) AS classrank_pr
        , FLOOR((dept_rank_reverse::DECIMAL-1)*100::DECIMAL/dept_count) AS deptrank_pr
	FROM 
		subject_rank_row AS s1



    --2.3 科目成績 類別1排名
), exam_score_tag1 AS (-------結算定期評量總成績
	SELECT	
		*
	FROM 
		exam_score
	WHERE 
	    subject IN (
            SELECT subject
            FROM calc_subject_tag1
        )
), subject_rank_row_tag1 AS (--------計算科目排名
	SELECT
		student_id
		, rank_tag1
		, rank_tag2
		, '定期評量/科目成績'::TEXT AS item_type
		, subject AS item_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, score
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag1, subject ORDER BY score DESC) AS tag1_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag1, subject ORDER BY score ASC) AS tag1_rank_reverse
		, COUNT (student_id) OVER(PARTITION BY rank_grade_year, rank_tag1, subject) AS tag1_count
        , subject
	FROM 
        exam_score_tag1
	WHERE 
        subject IS NOT NULL
), subject_rank_tag1 AS (-----------計算科目排名排名百分比及PR
	SELECT  
		s1.*
		, FLOOR((tag1_rank::DECIMAL-1)*100::DECIMAL/tag1_count)+1 AS tag1rank_percentage
        , FLOOR((tag1_rank_reverse::DECIMAL-1)*100::DECIMAL/tag1_count) AS tag1rank_pr
	FROM 
		subject_rank_row_tag1 AS s1



    --2.4 科目成績 類別2排名
), exam_score_tag2 AS (-------結算定期評量總成績
	SELECT	
		*
	FROM 
		exam_score
	WHERE 
	    subject IN (
            SELECT subject
            FROM calc_subject_tag2
        )
), subject_rank_row_tag2 AS (--------計算科目排名
	SELECT
		student_id
		, rank_tag1
		, rank_tag2
		, '定期評量/科目成績'::TEXT AS item_type
		, subject AS item_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, score
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag2, subject ORDER BY score DESC) AS tag2_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag2, subject ORDER BY score ASC) AS tag2_rank_reverse
		, COUNT (student_id) OVER(PARTITION BY rank_grade_year, rank_tag2, subject) AS tag2_count
        , subject
	FROM 
        exam_score_tag2
	WHERE 
        subject IS NOT NULL
), subject_rank_tag2 AS (-----------計算科目排名排名百分比及PR
	SELECT  
		s1.*
		, FLOOR((tag2_rank::DECIMAL-1)*100::DECIMAL/tag2_count)+1 AS tag2rank_percentage
        , FLOOR((tag2_rank_reverse::DECIMAL-1)*100::DECIMAL/tag2_count) AS tag2rank_pr
	FROM 
		subject_rank_row_tag2 AS s1



    --1.1 領域成績 年排名
    --1.2 領域成績 班排名
), domain_score AS (-----結算領域成績
	SELECT 
		student_id
		, student_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, domain
		, rank_tag1
		, rank_tag2
		, CASE 
            WHEN SUM(credit) IS NULL THEN NULL::DECIMAL
            WHEN SUM(credit) = 0 THEN 0::DECIMAL
            ELSE SUM(score::DECIMAL * credit::DECIMAL) / SUM(credit)
            END AS score
	FROM  
		exam_score
	WHERE
		score IS NOT NULL
		AND credit IS NOT NULL
	GROUP BY
		domain
        , rank_school_year
        , rank_semester
        , rank_grade_year
        , rank_class_name
        , exam_id
        , student_id
        , student_name
        , rank_tag1
        , rank_tag2
), domain_rank_row AS (-------計算領域排名
	SELECT
		student_id
		, rank_tag1
		, rank_tag2
		, '定期評量/領域成績'::TEXT AS item_type
		, domain::TEXT AS item_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, score
		, RANK() OVER(PARTITION BY rank_grade_year ,domain ORDER BY score DESC) AS grade_rank
		, RANK() OVER(PARTITION BY rank_class_name ,domain ORDER BY score DESC) AS class_rank
		, RANK() OVER(PARTITION BY rank_grade_year ,domain ORDER BY score ASC) AS grade_rank_reverse
		, RANK() OVER(PARTITION BY rank_class_name ,domain ORDER BY score ASC) AS class_rank_reverse
		, COUNT (student_id) OVER(PARTITION BY rank_grade_year ,domain) AS grade_count
		, COUNT (student_id) OVER(PARTITION BY rank_class_name, domain) AS class_count
        , domain
	FROM 
		domain_score
	WHERE
		domain IS NOT NULL
), domain_rank AS (-----------計算領域排名排名百分比及PR
	SELECT  
		s1.*
		, FLOOR((grade_rank::DECIMAL-1)*100::DECIMAL/grade_count)+1 AS graderank_percentage
		, FLOOR((class_rank::DECIMAL-1)*100::DECIMAL/class_count)+1 AS classrank_percentage
        , FLOOR((grade_rank_reverse::DECIMAL-1)*100::DECIMAL/grade_count) AS graderank_pr
        , FLOOR((class_rank_reverse::DECIMAL-1)*100::DECIMAL/class_count) AS classrank_pr
	FROM 
		domain_rank_row AS s1



    --1.3 領域成績 類別1排名
), domain_score_tag1 AS (-----結算領域成績
	SELECT 
		student_id
		, student_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, domain
		, rank_tag1
		, rank_tag2
		, CASE 
            WHEN SUM(credit) IS NULL THEN NULL::DECIMAL
            WHEN SUM(credit) = 0 THEN 0::DECIMAL
            ELSE SUM(score::DECIMAL * credit::DECIMAL) / SUM(credit)
            END AS score
	FROM  
		exam_score
	WHERE
		score IS NOT NULL
		AND credit IS NOT NULL
	    AND subject IN (
            SELECT subject
            FROM calc_subject_tag1
        )
	GROUP BY
		domain
        , rank_school_year
        , rank_semester
        , rank_grade_year
        , rank_class_name
        , exam_id
        , student_id
        , student_name
        , rank_tag1
        , rank_tag2
), domain_rank_row_tag1 AS (-------計算領域排名
	SELECT
		student_id
		, rank_tag1
		, rank_tag2
		, '定期評量/領域成績'::TEXT AS item_type
		, domain::TEXT AS item_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, score
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag1, domain ORDER BY score DESC) AS tag1_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag1, domain ORDER BY score ASC) AS tag1_rank_reverse
		, COUNT (student_id) OVER(PARTITION BY rank_grade_year, rank_tag1, domain) AS tag1_count
        , domain
	FROM 
		domain_score_tag1
	WHERE
		domain IS NOT NULL
), domain_rank_tag1 AS (-----------計算領域排名排名百分比及PR
	SELECT  
		s1.*
		, FLOOR((tag1_rank::DECIMAL-1)*100::DECIMAL/tag1_count)+1 AS tag1rank_percentage
        , FLOOR((tag1_rank_reverse::DECIMAL-1)*100::DECIMAL/tag1_count) AS tag1rank_pr
	FROM 
		domain_rank_row_tag1 AS s1



    --1.4 領域成績 類別2排名
), domain_score_tag2 AS (-----結算領域成績
	SELECT 
		student_id
		, student_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, domain
		, rank_tag1
		, rank_tag2
		, CASE 
            WHEN SUM(credit) IS NULL THEN NULL::DECIMAL
            WHEN SUM(credit) = 0 THEN 0::DECIMAL
            ELSE SUM(score::DECIMAL * credit::DECIMAL) / SUM(credit)
            END AS score
	FROM  
		exam_score
	WHERE
		score IS NOT NULL
		AND credit IS NOT NULL
	    AND subject IN (
            SELECT subject
            FROM calc_subject_tag2
        )
	GROUP BY
		domain
        , rank_school_year
        , rank_semester
        , rank_grade_year
        , rank_class_name
        , exam_id
        , student_id
        , student_name
        , rank_tag1
        , rank_tag2
), domain_rank_row_tag2 AS (-------計算領域排名
	SELECT
		student_id
		, rank_tag1
		, rank_tag2
		, '定期評量/領域成績'::TEXT AS item_type
		, domain::TEXT AS item_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, score
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag2, domain ORDER BY score DESC) AS tag2_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag2, domain ORDER BY score ASC) AS tag2_rank_reverse
		, COUNT (student_id) OVER(PARTITION BY rank_grade_year, rank_tag2, domain) AS tag2_count
        , domain
	FROM 
		domain_score_tag2
	WHERE
		domain IS NOT NULL
), domain_rank_tag2 AS (-----------計算領域排名排名百分比及PR
	SELECT  
		s1.*
		, FLOOR((tag2_rank::DECIMAL-1)*100::DECIMAL/tag2_count)+1 AS tag2rank_percentage
        , FLOOR((tag2_rank_reverse::DECIMAL-1)*100::DECIMAL/tag2_count) AS tag2rank_pr
	FROM 
		domain_rank_row_tag2 AS s1



    --3.1 總計成績 總分 年排名
    --3.2 總計成績 總分 班排名
), calc_sum_score AS (------算數總分排名所需成績
	SELECT
		student_id
		, student_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, rank_tag1
		, rank_tag2
		, SUM( score::decimal ) AS score
	FROM 
		exam_score
	WHERE
		score IS NOT NULL
		AND credit IS NOT NULL
	GROUP BY 
		student_id
        , student_name
        , rank_school_year
        , rank_semester
        , rank_grade_year
        , rank_class_name
        , exam_id
        , rank_tag1
        , rank_tag2
), calc_sum_rank_row AS (-----------計算總分排名
	SELECT 
		student_id
		, rank_tag1
		, rank_tag2
		, '定期評量/總計成績'::text AS item_type
		, '總分'::TEXT As item_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, score
		, RANK() OVER(PARTITION BY rank_grade_year ORDER BY score DESC) AS grade_rank
		, RANK() OVER(PARTITION BY rank_class_name ORDER BY score DESC) AS class_rank
		, RANK() OVER(PARTITION BY rank_grade_year ORDER BY score ASC) AS grade_rank_reverse
		, RANK() OVER(PARTITION BY rank_class_name ORDER BY score ASC) AS class_rank_reverse
		, COUNT (*) OVER(PARTITION BY rank_grade_year) AS grade_count
		, COUNT (*) OVER(PARTITION BY rank_class_name) AS class_count
	FROM 
		calc_sum_score
), calc_sum_rank AS (-----------計算總分排名百分比及PR
	SELECT  
		s1.*
		, FLOOR((grade_rank::DECIMAL-1)*100::DECIMAL/grade_count)+1 AS graderank_percentage
		, FLOOR((class_rank::DECIMAL-1)*100::DECIMAL/class_count)+1 AS classrank_percentage
        , FLOOR((grade_rank_reverse::DECIMAL-1)*100::DECIMAL/grade_count) AS graderank_pr
        , FLOOR((class_rank_reverse::DECIMAL-1)*100::DECIMAL/class_count) AS classrank_pr
	FROM 
		calc_sum_rank_row AS s1



    --3.3 總計成績 總分 類別1排名
), calc_sum_score_tag1 AS (------算數總分排名所需成績
	SELECT
		student_id
		, student_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, rank_tag1
		, rank_tag2
		, SUM( score::decimal ) AS score
	FROM 
		exam_score
	WHERE
		score IS NOT NULL
		AND credit IS NOT NULL
	    AND subject IN (
            SELECT subject
            FROM calc_subject_tag1
        )
	GROUP BY 
		student_id
        , student_name
        , rank_school_year
        , rank_semester
        , rank_grade_year
        , rank_class_name
        , exam_id
        , rank_tag1
        , rank_tag2
), calc_sum_rank_row_tag1 AS (-----------計算總分排名
	SELECT 
		student_id
		, rank_tag1
		, rank_tag2
		, '定期評量/總計成績'::text AS item_type
		, '總分'::TEXT As item_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, score
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag1 ORDER BY score DESC) AS tag1_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag1 ORDER BY score ASC) AS tag1_rank_reverse
		, COUNT (*) OVER(PARTITION BY rank_grade_year, rank_tag1) AS tag1_count
	FROM 
		calc_sum_score_tag1
), calc_sum_rank_tag1 AS (-----------計算總分排名百分比及PR
	SELECT  
		s1.*
		, FLOOR((tag1_rank::DECIMAL-1)*100::DECIMAL/tag1_count)+1 AS tag1rank_percentage
        , FLOOR((tag1_rank_reverse::DECIMAL-1)*100::DECIMAL/tag1_count) AS tag1rank_pr
	FROM 
		calc_sum_rank_row_tag1 AS s1



    --3.4 總計成績 總分 類別2排名
), calc_sum_score_tag2 AS (------算數總分排名所需成績
	SELECT
		student_id
		, student_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, rank_tag1
		, rank_tag2
		, SUM( score::decimal ) AS score
	FROM 
		exam_score
	WHERE
		score IS NOT NULL
		AND credit IS NOT NULL
	    AND subject IN (
            SELECT subject
            FROM calc_subject_tag2
        )
	GROUP BY 
		student_id
        , student_name
        , rank_school_year
        , rank_semester
        , rank_grade_year
        , rank_class_name
        , exam_id
        , rank_tag1
        , rank_tag2
), calc_sum_rank_row_tag2 AS (-----------計算總分排名
	SELECT 
		student_id
		, rank_tag1
		, rank_tag2
		, '定期評量/總計成績'::text AS item_type
		, '總分'::TEXT As item_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, score
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag2 ORDER BY score DESC) AS tag2_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag2 ORDER BY score ASC) AS tag2_rank_reverse
		, COUNT (*) OVER(PARTITION BY rank_grade_year, rank_tag2) AS tag2_count
	FROM 
		calc_sum_score_tag2
), calc_sum_rank_tag2 AS (-----------計算總分排名百分比及PR
	SELECT  
		s1.*
		, FLOOR((tag2_rank::DECIMAL-1)*100::DECIMAL/tag2_count)+1 AS tag2rank_percentage
        , FLOOR((tag2_rank_reverse::DECIMAL-1)*100::DECIMAL/tag2_count) AS tag2rank_pr
	FROM 
		calc_sum_rank_row_tag2 AS s1



    --4.1 總計成績 平均 年排名
    --4.2 總計成績 平均 班排名
), calc_avg_score AS (------算數平均排名所需成績
	SELECT
		student_id
		, student_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, rank_tag1
		, rank_tag2
		, AVG( score::decimal ) AS score
	FROM 
		exam_score
	WHERE
		score IS NOT NULL
		AND credit IS NOT NULL
	GROUP BY 
		student_id
        , student_name
        , rank_school_year
        , rank_semester
        , rank_grade_year
        , rank_class_name
        , exam_id
        , rank_tag1
        , rank_tag2
), calc_avg_rank_row AS (-----------計算平均排名
	SELECT 
		student_id
		, rank_tag1
		, rank_tag2
		, '定期評量/總計成績'::text AS item_type
		, '平均'::TEXT As item_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, score
		, RANK() OVER(PARTITION BY rank_grade_year ORDER BY score DESC) AS grade_rank
		, RANK() OVER(PARTITION BY rank_class_name ORDER BY score DESC) AS class_rank
		, RANK() OVER(PARTITION BY rank_grade_year ORDER BY score ASC) AS grade_rank_reverse
		, RANK() OVER(PARTITION BY rank_class_name ORDER BY score ASC) AS class_rank_reverse
		, COUNT (*) OVER(PARTITION BY rank_grade_year) AS grade_count
		, COUNT (*) OVER(PARTITION BY rank_class_name) AS class_count
	FROM 
		calc_avg_score
), calc_avg_rank AS (-----------計算平均排名百分比及PR
	SELECT  
		s1.*
		, FLOOR((grade_rank::DECIMAL-1)*100::DECIMAL/grade_count)+1 AS graderank_percentage
		, FLOOR((class_rank::DECIMAL-1)*100::DECIMAL/class_count)+1 AS classrank_percentage
        , FLOOR((grade_rank_reverse::DECIMAL-1)*100::DECIMAL/grade_count) AS graderank_pr
        , FLOOR((class_rank_reverse::DECIMAL-1)*100::DECIMAL/class_count) AS classrank_pr
	FROM 
		calc_avg_rank_row AS s1



    --4.3 總計成績 平均 類別1排名
), calc_avg_score_tag1 AS (------算數平均排名所需成績
	SELECT
		student_id
		, student_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, rank_tag1
		, rank_tag2
		, AVG( score::decimal ) AS score
	FROM 
		exam_score
	WHERE
		score IS NOT NULL
		AND credit IS NOT NULL
	    AND subject IN (
            SELECT subject
            FROM calc_subject_tag1
        )
	GROUP BY 
		student_id
        , student_name
        , rank_school_year
        , rank_semester
        , rank_grade_year
        , rank_class_name
        , exam_id
        , rank_tag1
        , rank_tag2
), calc_avg_rank_row_tag1 AS (-----------計算平均排名
	SELECT 
		student_id
		, rank_tag1
		, rank_tag2
		, '定期評量/總計成績'::text AS item_type
		, '平均'::TEXT As item_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, score
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag1 ORDER BY score DESC) AS tag1_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag1 ORDER BY score ASC) AS tag1_rank_reverse
		, COUNT (*) OVER(PARTITION BY rank_grade_year, rank_tag1) AS tag1_count
	FROM 
		calc_avg_score_tag1 AS calc_avg_score
), calc_avg_rank_tag1 AS (-----------計算平均排名百分比及PR
	SELECT  
		s1.*
		, FLOOR((tag1_rank::DECIMAL-1)*100::DECIMAL/tag1_count)+1 AS tag1rank_percentage
        , FLOOR((tag1_rank_reverse::DECIMAL-1)*100::DECIMAL/tag1_count) AS tag1rank_pr
	FROM 
		calc_avg_rank_row_tag1 AS s1



    --4.4 總計成績 平均 類別2排名
), calc_avg_score_tag2 AS (------算數平均排名所需成績
	SELECT
		student_id
		, student_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, rank_tag1
		, rank_tag2
		, AVG( score::decimal ) AS score
	FROM 
		exam_score
	WHERE
		score IS NOT NULL
		AND credit IS NOT NULL
	    AND subject IN (
            SELECT subject
            FROM calc_subject_tag2
        )
	GROUP BY 
		student_id
        , student_name
        , rank_school_year
        , rank_semester
        , rank_grade_year
        , rank_class_name
        , exam_id
        , rank_tag1
        , rank_tag2
), calc_avg_rank_row_tag2 AS (-----------計算平均排名
	SELECT 
		student_id
		, rank_tag1
		, rank_tag2
		, '定期評量/總計成績'::text AS item_type
		, '平均'::TEXT As item_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, score
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag2 ORDER BY score DESC) AS tag2_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag2 ORDER BY score ASC) AS tag2_rank_reverse
		, COUNT (*) OVER(PARTITION BY rank_grade_year, rank_tag2) AS tag2_count
	FROM 
		calc_avg_score_tag2 AS calc_avg_score
), calc_avg_rank_tag2 AS (-----------計算平均排名百分比及PR
	SELECT  
		s1.*
		, FLOOR((tag2_rank::DECIMAL-1)*100::DECIMAL/tag2_count)+1 AS tag2rank_percentage
        , FLOOR((tag2_rank_reverse::DECIMAL-1)*100::DECIMAL/tag2_count) AS tag2rank_pr
	FROM 
		calc_avg_rank_row_tag2 AS s1




    --5.1 總計成績 加權總分 年排名
    --5.2 總計成績 加權總分 班排名
), weight_sum_score AS (------加權總分排名所需成績
	SELECT
		student_id
		, student_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, rank_tag1
		, rank_tag2
		, SUM( score::decimal * credit::decimal ) AS score
	FROM 
		exam_score
	WHERE
		score IS NOT NULL
		AND credit IS NOT NULL
	GROUP BY 
		student_id
        , student_name
        , rank_school_year
        , rank_semester
        , rank_grade_year
        , rank_class_name
        , exam_id
        , rank_tag1
        , rank_tag2
), weight_sum_rank_row AS (-----------計算加權總分排名
	SELECT 
		student_id
		, rank_tag1
		, rank_tag2
		, '定期評量/總計成績'::text AS item_type
		, '加權總分'::TEXT As item_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, score
		, RANK() OVER(PARTITION BY rank_grade_year ORDER BY score DESC) AS grade_rank
		, RANK() OVER(PARTITION BY rank_class_name ORDER BY score DESC) AS class_rank
		, RANK() OVER(PARTITION BY rank_grade_year ORDER BY score ASC) AS grade_rank_reverse
		, RANK() OVER(PARTITION BY rank_class_name ORDER BY score ASC) AS class_rank_reverse
		, COUNT (*) OVER(PARTITION BY rank_grade_year) AS grade_count
		, COUNT (*) OVER(PARTITION BY rank_class_name) AS class_count
	FROM 
		weight_sum_score
), weight_sum_rank AS (-----------計算加權總分排名百分比及PR
	SELECT  
		s1.*
		, FLOOR((grade_rank::DECIMAL-1)*100::DECIMAL/grade_count)+1 AS graderank_percentage
		, FLOOR((class_rank::DECIMAL-1)*100::DECIMAL/class_count)+1 AS classrank_percentage
        , FLOOR((grade_rank_reverse::DECIMAL-1)*100::DECIMAL/grade_count) AS graderank_pr
        , FLOOR((class_rank_reverse::DECIMAL-1)*100::DECIMAL/class_count) AS classrank_pr
	FROM 
		weight_sum_rank_row AS s1




    --5.3 總計成績 加權總分 類別1排名
), weight_sum_score_tag1 AS (------加權總分排名所需成績
	SELECT
		student_id
		, student_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, rank_tag1
		, rank_tag2
		, SUM( score::decimal * credit::decimal ) AS score
	FROM 
		exam_score
	WHERE
		score IS NOT NULL
		AND credit IS NOT NULL
	    AND subject IN (
            SELECT subject
            FROM calc_subject_tag1
        )
	GROUP BY 
		student_id
        , student_name
        , rank_school_year
        , rank_semester
        , rank_grade_year
        , rank_class_name
        , exam_id
        , rank_tag1
        , rank_tag2
), weight_sum_rank_row_tag1 AS (-----------計算加權總分排名
	SELECT 
		student_id
		, rank_tag1
		, rank_tag2
		, '定期評量/總計成績'::text AS item_type
		, '加權總分'::TEXT As item_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, score
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag1 ORDER BY score DESC) AS tag1_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag1 ORDER BY score ASC) AS tag1_rank_reverse
		, COUNT (*) OVER(PARTITION BY rank_grade_year, rank_tag1) AS tag1_count
	FROM 
		weight_sum_score_tag1 AS weight_sum_score
), weight_sum_rank_tag1 AS (-----------計算加權總分排名百分比及PR
	SELECT  
		s1.*
		, FLOOR((tag1_rank::DECIMAL-1)*100::DECIMAL/tag1_count)+1 AS tag1rank_percentage
        , FLOOR((tag1_rank_reverse::DECIMAL-1)*100::DECIMAL/tag1_count) AS tag1rank_pr
	FROM 
		weight_sum_rank_row_tag1 AS s1




    --5.4 總計成績 加權總分 類別2排名
), weight_sum_score_tag2 AS (------加權總分排名所需成績
	SELECT
		student_id
		, student_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, rank_tag1
		, rank_tag2
		, SUM( score::decimal * credit::decimal ) AS score
	FROM 
		exam_score
	WHERE
		score IS NOT NULL
		AND credit IS NOT NULL
	    AND subject IN (
            SELECT subject
            FROM calc_subject_tag2
        )
	GROUP BY 
		student_id
        , student_name
        , rank_school_year
        , rank_semester
        , rank_grade_year
        , rank_class_name
        , exam_id
        , rank_tag1
        , rank_tag2
), weight_sum_rank_row_tag2 AS (-----------計算加權總分排名
	SELECT 
		student_id
		, rank_tag1
		, rank_tag2
		, '定期評量/總計成績'::text AS item_type
		, '加權總分'::TEXT As item_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, score
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag2 ORDER BY score DESC) AS tag2_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag2 ORDER BY score ASC) AS tag2_rank_reverse
		, COUNT (*) OVER(PARTITION BY rank_grade_year, rank_tag2) AS tag2_count
	FROM 
		weight_sum_score_tag2 AS weight_sum_score
), weight_sum_rank_tag2 AS (-----------計算加權總分排名百分比及PR
	SELECT  
		s1.*
		, FLOOR((tag2_rank::DECIMAL-1)*100::DECIMAL/tag2_count)+1 AS tag2rank_percentage
        , FLOOR((tag2_rank_reverse::DECIMAL-1)*100::DECIMAL/tag2_count) AS tag2rank_pr
	FROM 
		weight_sum_rank_row_tag2 AS s1




    --6.1 總計成績 加權平均 年排名
    --6.2 總計成績 加權平均 班排名
), weight_avg_score AS (------加權平均排名所需成績
	SELECT
		student_id
		, student_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, rank_tag1
		, rank_tag2
		, CASE 
            WHEN SUM(credit) IS NULL THEN NULL::DECIMAL
            WHEN SUM(credit) = 0 THEN 0::DECIMAL
            ELSE SUM(score::DECIMAL * credit::DECIMAL) / SUM(credit)
            END AS score
	FROM 
		exam_score
	WHERE
		score IS NOT NULL
		AND credit IS NOT NULL
	GROUP BY 
		student_id
        , student_name
        , rank_school_year
        , rank_semester
        , rank_grade_year
        , rank_class_name
        , exam_id
        , rank_tag1
        , rank_tag2
), weight_avg_rank_row AS (-----------計算加權平均排名
	SELECT 
		student_id
		, rank_tag1
		, rank_tag2
		, '定期評量/總計成績'::text AS item_type
		, '加權平均'::TEXT As item_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, score
		, RANK() OVER(PARTITION BY rank_grade_year ORDER BY score DESC) AS grade_rank
		, RANK() OVER(PARTITION BY rank_class_name ORDER BY score DESC) AS class_rank
		, RANK() OVER(PARTITION BY rank_grade_year ORDER BY score ASC) AS grade_rank_reverse
		, RANK() OVER(PARTITION BY rank_class_name ORDER BY score ASC) AS class_rank_reverse
		, COUNT (*) OVER(PARTITION BY rank_grade_year) AS grade_count
		, COUNT (*) OVER(PARTITION BY rank_class_name) AS class_count
	FROM 
		weight_avg_score
), weight_avg_rank AS (-----------計算加權平均排名百分比及PR
	SELECT  
		s1.*
		, FLOOR((grade_rank::DECIMAL-1)*100::DECIMAL/grade_count)+1 AS graderank_percentage
		, FLOOR((class_rank::DECIMAL-1)*100::DECIMAL/class_count)+1 AS classrank_percentage
        , FLOOR((grade_rank_reverse::DECIMAL-1)*100::DECIMAL/grade_count) AS graderank_pr
        , FLOOR((class_rank_reverse::DECIMAL-1)*100::DECIMAL/class_count) AS classrank_pr
	FROM 
		weight_avg_rank_row AS s1




    --6.3 總計成績 加權平均 類別1排名
), weight_avg_score_tag1 AS (------加權平均排名所需成績
	SELECT
		student_id
		, student_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, rank_tag1
		, rank_tag2
		, CASE 
            WHEN SUM(credit) IS NULL THEN NULL::DECIMAL
            WHEN SUM(credit) = 0 THEN 0::DECIMAL
            ELSE SUM(score::DECIMAL * credit::DECIMAL) / SUM(credit)
            END AS score
	FROM 
		exam_score
	WHERE
		score IS NOT NULL
		AND credit IS NOT NULL
	    AND subject IN (
            SELECT subject
            FROM calc_subject_tag1
        )
	GROUP BY 
		student_id
        , student_name
        , rank_school_year
        , rank_semester
        , rank_grade_year
        , rank_class_name
        , exam_id
        , rank_tag1
        , rank_tag2
), weight_avg_rank_row_tag1 AS (-----------計算加權平均排名
	SELECT 
		student_id
		, rank_tag1
		, rank_tag2
		, '定期評量/總計成績'::text AS item_type
		, '加權平均'::TEXT As item_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, score
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag1 ORDER BY score DESC) AS tag1_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag1 ORDER BY score ASC) AS tag1_rank_reverse
		, COUNT (*) OVER(PARTITION BY rank_grade_year, rank_tag1) AS tag1_count
	FROM 
		weight_avg_score_tag1 AS weight_avg_score
), weight_avg_rank_tag1 AS (-----------計算加權平均排名百分比及PR
	SELECT  
		s1.*
		, FLOOR((tag1_rank::DECIMAL-1)*100::DECIMAL/tag1_count)+1 AS tag1rank_percentage
        , FLOOR((tag1_rank_reverse::DECIMAL-1)*100::DECIMAL/tag1_count) AS tag1rank_pr
	FROM 
		weight_avg_rank_row_tag1 AS s1




    --6.4 總計成績 加權平均 類別2排名
), weight_avg_score_tag2 AS (------加權平均排名所需成績
	SELECT
		student_id
		, student_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, rank_tag1
		, rank_tag2
		, CASE 
            WHEN SUM(credit) IS NULL THEN NULL::DECIMAL
            WHEN SUM(credit) = 0 THEN 0::DECIMAL
            ELSE SUM(score::DECIMAL * credit::DECIMAL) / SUM(credit)
            END AS score
	FROM 
		exam_score
	WHERE
		score IS NOT NULL
		AND credit IS NOT NULL
	    AND subject IN (
            SELECT subject
            FROM calc_subject_tag2
        )
	GROUP BY 
		student_id
        , student_name
        , rank_school_year
        , rank_semester
        , rank_grade_year
        , rank_class_name
        , exam_id
        , rank_tag1
        , rank_tag2
), weight_avg_rank_row_tag2 AS (-----------計算加權平均排名
	SELECT 
		student_id
		, rank_tag1
		, rank_tag2
		, '定期評量/總計成績'::text AS item_type
		, '加權平均'::TEXT As item_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, score
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag2 ORDER BY score DESC) AS tag2_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag2 ORDER BY score ASC) AS tag2_rank_reverse
		, COUNT (*) OVER(PARTITION BY rank_grade_year, rank_tag2) AS tag2_count
	FROM 
		weight_avg_score_tag2 AS weight_avg_score
), weight_avg_rank_tag2 AS (-----------計算加權平均排名百分比及PR
	SELECT  
		s1.*
		, FLOOR((tag2_rank::DECIMAL-1)*100::DECIMAL/tag2_count)+1 AS tag2rank_percentage
        , FLOOR((tag2_rank_reverse::DECIMAL-1)*100::DECIMAL/tag2_count) AS tag2rank_pr
	FROM 
		weight_avg_rank_row_tag2 AS s1





), score_list AS (
    --1.1 領域成績 年排名
    --1.2 領域成績 班排名
    --1.3 領域成績 類別1排名
    --1.4 領域成績 類別2排名
    --2.1 科目成績 年排名
    --2.2 科目成績 班排名
    --2.3 科目成績 類別1排名
    --2.4 科目成績 類別2排名
    --2.5 科目成績 科排名
    --3.1 總計成績 總分 年排名
    --3.2 總計成績 總分 班排名
    --3.3 總計成績 總分 類別1排名
    --3.4 總計成績 總分 類別2排名
    --4.1 總計成績 平均 年排名
    --4.2 總計成績 平均 班排名
    --4.3 總計成績 平均 類別1排名
    --4.4 總計成績 平均 類別2排名
    --5.1 總計成績 加權總分 年排名
    --5.2 總計成績 加權總分 班排名
    --5.3 總計成績 加權總分 類別1排名
    --5.4 總計成績 加權總分 類別2排名
    --6.1 總計成績 加權平均 年排名
    --6.2 總計成績 加權平均 班排名
    --6.3 總計成績 加權平均 類別1排名
    --6.4 總計成績 加權平均 類別2排名


--     --1.1 領域成績 年排名
-- 	SELECT
-- 		rank_school_year 
-- 		, rank_semester
-- 		, rank_grade_year
-- 		, item_type
-- 		, exam_id AS ref_exam_id
-- 		, item_name
-- 		, '年排名'::TEXT AS rank_type
-- 		, '' || rank_grade_year || '年級'::TEXT AS rank_name
-- 		, true AS is_alive
-- 		, grade_count AS matrix_count
-- 		, AVG(Score::DECIMAL) FILTER(WHERE grade_rank <= TRUNC(grade_count * 0.25)) OVER(PARTITION BY rank_grade_year, item_name) AS avg_top_25
-- 		, AVG(Score::DECIMAL) FILTER(WHERE grade_rank <= TRUNC(grade_count * 0.5)) OVER(PARTITION BY rank_grade_year, item_name) AS avg_top_50
-- 		, AVG(Score::DECIMAL) OVER(PARTITION BY rank_grade_year,item_name) AS avg
-- 		, AVG(Score::DECIMAL) FILTER(WHERE grade_rank >= TRUNC(grade_count * 0.5)) OVER(PARTITION BY rank_grade_year, item_name) AS avg_bottom_50
-- 		, AVG(Score::DECIMAL) FILTER(WHERE grade_rank >= TRUNC(grade_count * 0.75)) OVER(PARTITION BY rank_grade_year, item_name) AS avg_bottom_25
-- 		, COUNT(*) FILTER (WHERE 100::DECIMAL<=score::DECIMAL ) OVER(PARTITION BY rank_grade_year,item_name)AS level_gte100 
-- 		, COUNT(*) FILTER (WHERE 90::DECIMAL<=score AND score <100::DECIMAL) OVER(PARTITION BY rank_grade_year,item_name) AS level_90
-- 		, COUNT(*) FILTER (WHERE 80::DECIMAL<=score AND score <90::DECIMAL)  OVER(PARTITION BY rank_grade_year,item_name) AS level_80
-- 		, COUNT(*) FILTER (WHERE 70::DECIMAL<=score AND score <80::DECIMAL)  OVER(PARTITION BY rank_grade_year,item_name)AS level_70
-- 		, COUNT(*) FILTER (WHERE 60::DECIMAL<=score AND score <70::DECIMAL)  OVER(PARTITION BY rank_grade_year,item_name) AS level_60
-- 		, COUNT(*) FILTER (WHERE 50::DECIMAL<=score AND score <60::DECIMAL)  OVER(PARTITION BY rank_grade_year,item_name)AS level_50
-- 		, COUNT(*) FILTER (WHERE 40::DECIMAL<=score AND score <50::DECIMAL)  OVER(PARTITION BY rank_grade_year,item_name)AS level_40
-- 		, COUNT(*) FILTER (WHERE 30::DECIMAL<=score AND score <40::DECIMAL)  OVER(PARTITION BY rank_grade_year,item_name)AS level_30
-- 		, COUNT(*) FILTER (WHERE 20::DECIMAL<=score AND score <30::DECIMAL)  OVER(PARTITION BY rank_grade_year,item_name) AS level_20
-- 		, COUNT(*) FILTER (WHERE 10::DECIMAL<=score AND score <20::DECIMAL)  OVER(PARTITION BY rank_grade_year,item_name)AS level_10
-- 		, COUNT(*) FILTER (WHERE score<10::DECIMAL) OVER (PARTITION BY rank_grade_year,item_name)AS level_lt10 
-- 		, student_id
-- 		, score
-- 		, grade_rank AS rank
-- 		, graderank_percentage AS percentile
-- 		, graderank_pr AS pr
-- 		, rank_class_name
-- 		, rank_tag1
-- 		, rank_tag2
-- 	FROM
-- 		domain_rank
-- 	UNION ALL
--     --1.2 領域成績 班排名
-- 	SELECT
-- 		rank_school_year 
-- 		, rank_semester
-- 		, rank_grade_year
-- 		, item_type
-- 		, exam_id AS ref_exam_id
-- 		, item_name
-- 		, '班排名'::TEXT AS rank_type
-- 		, rank_class_name AS rank_name
-- 		, true AS is_alive
-- 		, class_count AS matrix_count
-- 		, AVG(Score::DECIMAL) FILTER(WHERE class_rank <= TRUNC(class_count * 0.25)) OVER(PARTITION BY rank_class_name,item_name) AS avg_top_25
-- 		, AVG(Score::DECIMAL) FILTER(WHERE class_rank <= TRUNC(class_count * 0.5)) OVER(PARTITION BY rank_class_name,item_name) AS avg_top_50
-- 		, AVG(Score::DECIMAL) OVER(PARTITION BY rank_class_name,item_name) AS avg
-- 		, AVG(Score::DECIMAL) FILTER(WHERE class_rank >= TRUNC(class_count * 0.5)) OVER(PARTITION BY rank_class_name,item_name) AS avg_bottom_50
-- 		, AVG(Score::DECIMAL) FILTER(WHERE class_rank >= TRUNC(class_count * 0.75)) OVER(PARTITION BY rank_class_name,item_name) AS avg_bottom_25
-- 		, COUNT(*) FILTER (WHERE 100::DECIMAL<=score::DECIMAL ) OVER(PARTITION BY rank_class_name,item_name)AS level_gte100 
-- 		, COUNT(*) FILTER (WHERE 90::DECIMAL<=score AND score <100::DECIMAL)OVER (PARTITION BY rank_class_name,item_name) AS level_90
-- 		, COUNT(*) FILTER (WHERE 80::DECIMAL<=score AND score <90::DECIMAL) OVER (PARTITION BY rank_class_name,item_name) AS level_80
-- 		, COUNT(*) FILTER (WHERE 70::DECIMAL<=score AND score <80::DECIMAL) OVER (PARTITION BY rank_class_name,item_name)AS level_70
-- 		, COUNT(*) FILTER (WHERE 60::DECIMAL<=score AND score <70::DECIMAL) OVER (PARTITION BY rank_class_name,item_name) AS level_60
-- 		, COUNT(*) FILTER (WHERE 50::DECIMAL<=score AND score <60::DECIMAL) OVER (PARTITION BY rank_class_name,item_name)AS level_50
-- 		, COUNT(*) FILTER (WHERE 40::DECIMAL<=score AND score <50::DECIMAL) OVER (PARTITION BY rank_class_name,item_name)AS level_40
-- 		, COUNT(*) FILTER (WHERE 30::DECIMAL<=score AND score <40::DECIMAL) OVER (PARTITION BY rank_class_name,item_name)AS level_30
-- 		, COUNT(*) FILTER (WHERE 20::DECIMAL<=score AND score <30::DECIMAL) OVER (PARTITION BY rank_class_name,item_name) AS level_20
-- 		, COUNT(*) FILTER (WHERE 10::DECIMAL<=score AND score <20::DECIMAL) OVER (PARTITION BY rank_class_name,item_name)AS level_10
-- 		, COUNT(*) FILTER (WHERE score<10::DECIMAL) OVER (PARTITION BY rank_class_name,item_name)AS level_lt10 
-- 		, student_id
-- 		, score
-- 		, class_rank AS rank
-- 		, classrank_percentage AS percentile
-- 		, classrank_pr AS pr
-- 		, rank_class_name
-- 		, rank_tag1
-- 		, rank_tag2
-- 	FROM
-- 		domain_rank
-- 	UNION ALL
--     --1.3 領域成績 類別1排名
-- 	SELECT
-- 		rank_school_year 
-- 		, rank_semester
-- 		, rank_grade_year
-- 		, item_type
-- 		, exam_id AS ref_exam_id
-- 		, item_name
-- 		, '類別1排名'::TEXT AS rank_type
-- 		, rank_tag1 AS rank_name
-- 		, true AS is_alive
-- 		, tag1_count AS matrix_count
-- 		, AVG(Score::DECIMAL)FILTER(WHERE tag1_rank <= TRUNC(tag1_count * 0.25)) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_top_25
-- 		, AVG(Score::DECIMAL)FILTER(WHERE tag1_rank <= TRUNC(tag1_count * 0.5)) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_top_50
-- 		, AVG(Score::DECIMAL)OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg
-- 		, AVG(Score::DECIMAL)FILTER(WHERE tag1_rank >= TRUNC(tag1_count * 0.5)) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_bottom_50
-- 		, AVG(Score::DECIMAL)FILTER(WHERE tag1_rank >= TRUNC(tag1_count * 0.75)) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_bottom_25
-- 		, COUNT(*) FILTER (WHERE 100::DECIMAL<=score::DECIMAL ) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)AS level_gte100 
-- 		, COUNT(*) FILTER (WHERE 90::DECIMAL<=score AND score <100::DECIMAL)OVER (PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_90
-- 		, COUNT(*) FILTER (WHERE 80::DECIMAL<=score AND score <90::DECIMAL) OVER (PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_80
-- 		, COUNT(*) FILTER (WHERE 70::DECIMAL<=score AND score <80::DECIMAL) OVER (PARTITION BY rank_grade_year, rank_tag1, item_name)AS level_70
-- 		, COUNT(*) FILTER (WHERE 60::DECIMAL<=score AND score <70::DECIMAL) OVER (PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_60
-- 		, COUNT(*) FILTER (WHERE 50::DECIMAL<=score AND score <60::DECIMAL) OVER (PARTITION BY rank_grade_year, rank_tag1, item_name)AS level_50
-- 		, COUNT(*) FILTER (WHERE 40::DECIMAL<=score AND score <50::DECIMAL) OVER (PARTITION BY rank_grade_year, rank_tag1, item_name)AS level_40
-- 		, COUNT(*) FILTER (WHERE 30::DECIMAL<=score AND score <40::DECIMAL) OVER (PARTITION BY rank_grade_year, rank_tag1, item_name)AS level_30
-- 		, COUNT(*) FILTER (WHERE 20::DECIMAL<=score AND score <30::DECIMAL) OVER (PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_20
-- 		, COUNT(*) FILTER (WHERE 10::DECIMAL<=score AND score <20::DECIMAL) OVER (PARTITION BY rank_grade_year, rank_tag1, item_name)AS level_10
-- 		, COUNT(*) FILTER (WHERE score<10::DECIMAL) OVER (PARTITION BY rank_grade_year, rank_tag1, item_name)AS level_lt10 
-- 		, student_id
-- 		, score
-- 		, tag1_rank AS rank
-- 		, tag1rank_percentage AS percentile
-- 		, tag1rank_pr AS pr
-- 		, rank_class_name
-- 		, rank_tag1
-- 		, rank_tag2
-- 	FROM
-- 		domain_rank_tag1 AS domain_rank
-- 	WHERE 
--         rank_tag1 IS NOT NULL
-- 	    AND rank_tag1 <> ''
-- 	UNION ALL
--     --1.4 領域成績 類別2排名
-- 	SELECT
-- 		rank_school_year 
-- 		, rank_semester
-- 		, rank_grade_year
-- 		, item_type
-- 		, exam_id AS ref_exam_id
-- 		, item_name
-- 		, '類別2排名'::TEXT AS rank_type
-- 		, rank_tag2 AS rank_name
-- 		, true AS is_alive
-- 		, tag2_count AS matrix_count
-- 		, AVG(Score::DECIMAL)FILTER(WHERE tag2_rank <= TRUNC(tag2_count * 0.25)) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_top_25
-- 		, AVG(Score::DECIMAL)FILTER(WHERE tag2_rank <= TRUNC(tag2_count * 0.5)) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_top_50
-- 		, AVG(Score::DECIMAL)OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg
-- 		, AVG(Score::DECIMAL)FILTER(WHERE tag2_rank >= TRUNC(tag2_count * 0.5)) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_bottom_50
-- 		, AVG(Score::DECIMAL)FILTER(WHERE tag2_rank >= TRUNC(tag2_count * 0.75)) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_bottom_25
-- 		, COUNT(*) FILTER (WHERE 100::DECIMAL<=score::DECIMAL ) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)AS level_gte100 
-- 		, COUNT(*) FILTER (WHERE 90::DECIMAL<=score AND score <100::DECIMAL)OVER (PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_90
-- 		, COUNT(*) FILTER (WHERE 80::DECIMAL<=score AND score <90::DECIMAL) OVER (PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_80
-- 		, COUNT(*) FILTER (WHERE 70::DECIMAL<=score AND score <80::DECIMAL) OVER (PARTITION BY rank_grade_year, rank_tag2, item_name)AS level_70
-- 		, COUNT(*) FILTER (WHERE 60::DECIMAL<=score AND score <70::DECIMAL) OVER (PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_60
-- 		, COUNT(*) FILTER (WHERE 50::DECIMAL<=score AND score <60::DECIMAL) OVER (PARTITION BY rank_grade_year, rank_tag2, item_name)AS level_50
-- 		, COUNT(*) FILTER (WHERE 40::DECIMAL<=score AND score <50::DECIMAL) OVER (PARTITION BY rank_grade_year, rank_tag2, item_name)AS level_40
-- 		, COUNT(*) FILTER (WHERE 30::DECIMAL<=score AND score <40::DECIMAL) OVER (PARTITION BY rank_grade_year, rank_tag2, item_name)AS level_30
-- 		, COUNT(*) FILTER (WHERE 20::DECIMAL<=score AND score <30::DECIMAL) OVER (PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_20
-- 		, COUNT(*) FILTER (WHERE 10::DECIMAL<=score AND score <20::DECIMAL) OVER (PARTITION BY rank_grade_year, rank_tag2, item_name)AS level_10
-- 		, COUNT(*) FILTER (WHERE score<10::DECIMAL) OVER (PARTITION BY rank_grade_year, rank_tag2, item_name)AS level_lt10 
-- 		, student_id
-- 		, score
-- 		, tag2_rank AS rank
-- 		, tag2rank_percentage AS percentile
-- 		, tag2rank_pr AS pr
-- 		, rank_class_name
-- 		, rank_tag1
-- 		, rank_tag2
-- 	FROM
-- 		domain_rank_tag2 AS domain_rank
-- 	WHERE 
-- 		rank_tag2 IS NOT NULL
-- 	    AND rank_tag2 <> ''
-- 	UNION ALL
    --2.1 科目成績 年排名
	SELECT
		rank_school_year 
		, rank_semester
		, rank_grade_year
		, item_type
		, exam_id AS ref_exam_id
		, item_name
		, '年排名'::TEXT AS rank_type
		, '' || rank_grade_year || '年級'::TEXT AS rank_name
		, true AS is_alive
		, grade_count AS matrix_count
		, AVG(Score::DECIMAL)FILTER(WHERE grade_rank <= TRUNC(grade_count * 0.25)) OVER(PARTITION BY rank_grade_year,item_name) AS avg_top_25
		, AVG(Score::DECIMAL)FILTER(WHERE grade_rank <= TRUNC(grade_count * 0.5)) OVER(PARTITION BY rank_grade_year,item_name) AS avg_top_50
		, AVG(Score::DECIMAL)OVER(PARTITION BY rank_grade_year,item_name) AS avg
		, AVG(Score::DECIMAL)FILTER(WHERE grade_rank >= TRUNC(grade_count * 0.5)) OVER(PARTITION BY rank_grade_year,item_name) AS avg_bottom_50
		, AVG(Score::DECIMAL)FILTER(WHERE grade_rank >= TRUNC(grade_count * 0.75)) OVER(PARTITION BY rank_grade_year,item_name) AS avg_bottom_25
		, COUNT(*) FILTER (WHERE 100::DECIMAL<=score::DECIMAL ) OVER(PARTITION BY rank_grade_year,item_name)AS level_gte100 
		, COUNT(*) FILTER (WHERE 90::DECIMAL<=score AND score <100::DECIMAL)  OVER(PARTITION BY rank_grade_year,item_name) AS level_90
		, COUNT(*) FILTER (WHERE 80::DECIMAL<=score AND score <90::DECIMAL)  OVER(PARTITION BY rank_grade_year,item_name) AS level_80
		, COUNT(*) FILTER (WHERE 70::DECIMAL<=score AND  score <80::DECIMAL)  OVER(PARTITION BY rank_grade_year,item_name)AS level_70
		, COUNT(*) FILTER (WHERE 60::DECIMAL<=score AND score <70::DECIMAL) OVER(PARTITION BY rank_grade_year,item_name) AS level_60
		, COUNT(*) FILTER (WHERE 50::DECIMAL<=score AND score <60::DECIMAL)  OVER (PARTITION BY rank_grade_year,item_name)AS level_50
		, COUNT(*) FILTER (WHERE 40::DECIMAL<=score AND score <50::DECIMAL) OVER (PARTITION BY rank_grade_year,item_name)AS level_40
		, COUNT(*) FILTER (WHERE 30::DECIMAL<=score AND score <40::DECIMAL)   OVER (PARTITION BY rank_grade_year,item_name)AS level_30
		, COUNT(*) FILTER (WHERE 20::DECIMAL<=score AND score <30::DECIMAL)  OVER (PARTITION BY rank_grade_year,item_name) AS level_20
		, COUNT(*) FILTER (WHERE 10::DECIMAL<=score AND score <20::DECIMAL)   OVER (PARTITION BY rank_grade_year,item_name)AS level_10
		, COUNT(*) FILTER (WHERE score<10::DECIMAL) OVER (PARTITION BY rank_grade_year,item_name)AS level_lt10
		, student_id
		, score
		, grade_rank AS rank
		, graderank_percentage AS percentile
		, graderank_pr AS pr
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		subject_rank
	UNION ALL
    --2.2 科目成績 班排名
	SELECT
		rank_school_year 
		, rank_semester
		, rank_grade_year
		, item_type
		, exam_id AS ref_exam_id
		, item_name
		, '班排名'::TEXT AS rank_type
		, rank_class_name AS rank_name
		, true AS is_alive
		, class_count AS matrix_count
		, AVG(Score::DECIMAL)FILTER(WHERE class_rank <= TRUNC(class_count * 0.25)) OVER(PARTITION BY rank_class_name,item_name) AS avg_top_25
		, AVG(Score::DECIMAL)FILTER(WHERE class_rank <= TRUNC(class_count * 0.5)) OVER(PARTITION BY rank_class_name,item_name) AS avg_top_50
		, AVG(Score::DECIMAL)OVER(PARTITION BY rank_class_name,item_name) AS avg
		, AVG(Score::DECIMAL)FILTER(WHERE class_rank >= TRUNC(class_count * 0.5)) OVER(PARTITION BY rank_class_name,item_name) AS avg_bottom_50
		, AVG(Score::DECIMAL)FILTER(WHERE class_rank >= TRUNC(class_count * 0.75)) OVER(PARTITION BY rank_class_name,item_name) AS avg_bottom_25
		, COUNT(*) FILTER (WHERE 100::DECIMAL<=score::DECIMAL ) OVER(PARTITION BY rank_class_name,item_name)AS level_gte100 
		, COUNT(*) FILTER (WHERE 90::DECIMAL<=score AND score <100::DECIMAL)  OVER(PARTITION BY rank_class_name,item_name) AS level_90
		, COUNT(*) FILTER (WHERE 80::DECIMAL<=score AND score <90::DECIMAL)  OVER(PARTITION BY rank_class_name,item_name) AS level_80
		, COUNT(*) FILTER (WHERE 70::DECIMAL<=score AND score <80::DECIMAL)  OVER(PARTITION BY rank_class_name,item_name)AS level_70
		, COUNT(*) FILTER (WHERE 60::DECIMAL<=score AND score <70::DECIMAL) OVER(PARTITION BY rank_class_name,item_name) AS level_60
		, COUNT(*) FILTER (WHERE 50::DECIMAL<=score AND score <60::DECIMAL)  OVER (PARTITION BY rank_class_name,item_name)AS level_50
		, COUNT(*) FILTER (WHERE 40::DECIMAL<=score AND score <50::DECIMAL) OVER (PARTITION BY rank_class_name,item_name)AS level_40
		, COUNT(*) FILTER (WHERE 30::DECIMAL<=score AND score <40::DECIMAL)   OVER (PARTITION BY rank_class_name,item_name)AS level_30
		, COUNT(*) FILTER (WHERE 20::DECIMAL<=score AND score <30::DECIMAL)  OVER (PARTITION BY rank_class_name,item_name) AS level_20
		, COUNT(*) FILTER (WHERE 10::DECIMAL<=score AND score <20::DECIMAL)   OVER (PARTITION BY rank_class_name,item_name)AS level_10
		, COUNT(*) FILTER (WHERE score<10::DECIMAL) OVER (PARTITION BY rank_class_name,item_name)AS level_lt10 
		, student_id
		, score
		, class_rank AS rank
		, classrank_percentage AS percentile
		, classrank_pr AS pr
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		subject_rank
	UNION ALL
    --2.3 科目成績 類別1排名
	SELECT
		rank_school_year 
		, rank_semester
		, rank_grade_year
		, item_type
		, exam_id AS ref_exam_id
		, item_name
		, '類別1排名'::TEXT AS rank_type
		, rank_tag1 AS rank_name
		, true AS is_alive
		, tag1_count AS matrix_count
		, AVG(Score::DECIMAL)FILTER(WHERE tag1_rank <= TRUNC(tag1_count * 0.25)) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_top_25
		, AVG(Score::DECIMAL)FILTER(WHERE tag1_rank <= TRUNC(tag1_count * 0.5)) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_top_50
		, AVG(Score::DECIMAL)OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg
		, AVG(Score::DECIMAL)FILTER(WHERE tag1_rank >= TRUNC(tag1_count * 0.5)) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_bottom_50
		, AVG(Score::DECIMAL)FILTER(WHERE tag1_rank >= TRUNC(tag1_count * 0.75)) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_bottom_25
		, COUNT(*) FILTER (WHERE 100::DECIMAL<=score::DECIMAL ) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)AS level_gte100 
		, COUNT(*) FILTER (WHERE 90::DECIMAL<=score AND score <100::DECIMAL)OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_90
		, COUNT(*) FILTER (WHERE 80::DECIMAL<=score AND score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_80
		, COUNT(*) FILTER (WHERE 70::DECIMAL<=score AND score <80::DECIMAL)OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)AS level_70
		, COUNT(*) FILTER (WHERE 60::DECIMAL<=score AND score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_60
		, COUNT(*) FILTER (WHERE 50::DECIMAL<=score AND score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)AS level_50
		, COUNT(*) FILTER (WHERE 40::DECIMAL<=score AND score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)AS level_40
		, COUNT(*) FILTER (WHERE 30::DECIMAL<=score AND score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)AS level_30
		, COUNT(*) FILTER (WHERE 20::DECIMAL<=score AND score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_20
		, COUNT(*) FILTER (WHERE 10::DECIMAL<=score AND score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)AS level_10
		, COUNT(*) FILTER (WHERE score<10::DECIMAL) OVER (PARTITION BY rank_grade_year, rank_tag1, item_name)AS level_lt10
		, student_id
		, score
		, tag1_rank AS rank
		, tag1rank_percentage AS percentile
		, tag1rank_pr AS pr
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		subject_rank_tag1 AS subject_rank
	WHERE
		rank_tag1 IS NOT NULL
		AND rank_tag1 <> ''
	UNION ALL
    --2.4 科目成績 類別2排名
	SELECT
		rank_school_year 
		, rank_semester
		, rank_grade_year
		, item_type
		, exam_id AS ref_exam_id
		, item_name
		, '類別2排名'::TEXT AS rank_type
		, rank_tag2 AS rank_name
		, true AS is_alive
		, tag2_count AS matrix_count
		, AVG(Score::DECIMAL)FILTER(WHERE tag2_rank <= TRUNC(tag2_count * 0.25)) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_top_25
		, AVG(Score::DECIMAL)FILTER(WHERE tag2_rank <= TRUNC(tag2_count * 0.5)) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_top_50
		, AVG(Score::DECIMAL)OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg
		, AVG(Score::DECIMAL)FILTER(WHERE tag2_rank >= TRUNC(tag2_count * 0.5)) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_bottom_50
		, AVG(Score::DECIMAL)FILTER(WHERE tag2_rank >= TRUNC(tag2_count * 0.75)) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_bottom_25
		, COUNT(*) FILTER (WHERE 100::DECIMAL<=score::DECIMAL ) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)AS level_gte100 
		, COUNT(*) FILTER (WHERE 90::DECIMAL<=score AND score <100::DECIMAL)OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_90
		, COUNT(*) FILTER (WHERE 80::DECIMAL<=score AND score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_80
		, COUNT(*) FILTER (WHERE 70::DECIMAL<=score AND score <80::DECIMAL)OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)AS level_70
		, COUNT(*) FILTER (WHERE 60::DECIMAL<=score AND score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_60
		, COUNT(*) FILTER (WHERE 50::DECIMAL<=score AND score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)AS level_50
		, COUNT(*) FILTER (WHERE 40::DECIMAL<=score AND score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)AS level_40
		, COUNT(*) FILTER (WHERE 30::DECIMAL<=score AND score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)AS level_30
		, COUNT(*) FILTER (WHERE 20::DECIMAL<=score AND score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_20
		, COUNT(*) FILTER (WHERE 10::DECIMAL<=score AND score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)AS level_10
		, COUNT(*) FILTER (WHERE score<10::DECIMAL) OVER (PARTITION BY rank_grade_year, rank_tag2, item_name)AS level_lt10
		, student_id
		, score
		, tag2_rank AS rank
		, tag2rank_percentage AS percentile
		, tag2rank_pr AS pr
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		subject_rank_tag2 AS subject_rank
	WHERE
		rank_tag2 IS NOT NULL
		AND rank_tag2 <> ''
	UNION ALL
    --2.5 科目成績 科排名
	SELECT
		rank_school_year 
		, rank_semester
		, rank_grade_year
		, item_type
		, exam_id AS ref_exam_id
		, item_name
		, '科排名'::TEXT AS rank_type
		, rank_dept_name AS rank_name
		, true AS is_alive
		, dept_count AS matrix_count
		, AVG(score::DECIMAL)FILTER(WHERE class_rank <= TRUNC(dept_count * 0.25)) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg_top_25
		, AVG(score::DECIMAL)FILTER(WHERE class_rank <= TRUNC(dept_count * 0.5)) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg_top_50
		, AVG(score::DECIMAL)OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg
		, AVG(score::DECIMAL)FILTER(WHERE class_rank >= TRUNC(dept_count * 0.5)) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg_bottom_50
		, AVG(score::DECIMAL)FILTER(WHERE class_rank >= TRUNC(dept_count * 0.75)) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg_bottom_25
		, COUNT(*) FILTER (WHERE 100::DECIMAL<=score::DECIMAL ) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)AS level_gte100 
		, COUNT(*) FILTER (WHERE 90::DECIMAL<=score AND score <100::DECIMAL)  OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_90
		, COUNT(*) FILTER (WHERE 80::DECIMAL<=score AND score <90::DECIMAL)  OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_80
		, COUNT(*) FILTER (WHERE 70::DECIMAL<=score AND score <80::DECIMAL)  OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)AS level_70
		, COUNT(*) FILTER (WHERE 60::DECIMAL<=score AND score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_60
		, COUNT(*) FILTER (WHERE 50::DECIMAL<=score AND score <60::DECIMAL)  OVER (PARTITION BY rank_grade_year, rank_dept_name, item_name)AS level_50
		, COUNT(*) FILTER (WHERE 40::DECIMAL<=score AND score <50::DECIMAL) OVER (PARTITION BY rank_grade_year, rank_dept_name, item_name)AS level_40
		, COUNT(*) FILTER (WHERE 30::DECIMAL<=score AND score <40::DECIMAL)   OVER (PARTITION BY rank_grade_year, rank_dept_name, item_name)AS level_30
		, COUNT(*) FILTER (WHERE 20::DECIMAL<=score AND score <30::DECIMAL)  OVER (PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_20
		, COUNT(*) FILTER (WHERE 10::DECIMAL<=score AND score <20::DECIMAL)   OVER (PARTITION BY rank_grade_year, rank_dept_name, item_name)AS level_10
		, COUNT(*) FILTER (WHERE score<10::DECIMAL) OVER (PARTITION BY rank_grade_year, rank_dept_name, item_name)AS level_lt10 
		, student_id
		, score
		, class_rank AS rank
		, classrank_percentage AS percentile
		, classrank_pr AS pr
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		subject_rank
	UNION ALL
    --3.1 總計成績 總分 年排名
	SELECT
		rank_school_year 
		, rank_semester
		, rank_grade_year
		, item_type
		, exam_id AS ref_exam_id
		, item_name
		, '年排名'::TEXT AS rank_type
		, '' || rank_grade_year || '年級'::TEXT AS rank_name
		, true AS is_alive
		, grade_count AS matrix_count
		, AVG(Score::DECIMAL)FILTER(WHERE grade_rank <= TRUNC(grade_count * 0.25)) OVER(PARTITION BY rank_grade_year) AS avg_top_25
		, AVG(Score::DECIMAL)FILTER(WHERE grade_rank <= TRUNC(grade_count * 0.5)) OVER(PARTITION BY rank_grade_year) AS avg_top_50
		, AVG(Score::DECIMAL)OVER(PARTITION BY rank_grade_year) AS avg
		, AVG(Score::DECIMAL)FILTER(WHERE grade_rank >= TRUNC(grade_count * 0.5)) OVER(PARTITION BY rank_grade_year) AS avg_bottom_50
		, AVG(Score::DECIMAL)FILTER(WHERE grade_rank >= TRUNC(grade_count * 0.75)) OVER(PARTITION BY rank_grade_year) AS avg_bottom_25
		, COUNT(*) FILTER (WHERE 100::DECIMAL<=score::DECIMAL ) OVER(PARTITION BY rank_grade_year)AS level_gte100 
		, COUNT(*) FILTER (WHERE 90::DECIMAL<=score AND score <100::DECIMAL)  OVER(PARTITION BY rank_grade_year) AS level_90
		, COUNT(*) FILTER (WHERE 80::DECIMAL<=score AND score <90::DECIMAL)  OVER(PARTITION BY rank_grade_year) AS level_80
		, COUNT(*) FILTER (WHERE 70::DECIMAL<=score AND  score <80::DECIMAL)  OVER(PARTITION BY rank_grade_year)AS level_70
		, COUNT(*) FILTER (WHERE 60::DECIMAL<=score AND score <70::DECIMAL) OVER(PARTITION BY rank_grade_year) AS level_60
		, COUNT(*) FILTER (WHERE 50::DECIMAL<=score AND score <60::DECIMAL)  OVER (PARTITION BY rank_grade_year)AS level_50
		, COUNT(*) FILTER (WHERE 40::DECIMAL<=score AND score <50::DECIMAL) OVER (PARTITION BY rank_grade_year)AS level_40
		, COUNT(*) FILTER (WHERE 30::DECIMAL<=score AND score <40::DECIMAL)   OVER (PARTITION BY rank_grade_year)AS level_30
		, COUNT(*) FILTER (WHERE 20::DECIMAL<=score AND score <30::DECIMAL)  OVER (PARTITION BY rank_grade_year) AS level_20
		, COUNT(*) FILTER (WHERE 10::DECIMAL<=score AND score <20::DECIMAL)   OVER (PARTITION BY rank_grade_year)AS level_10
		, COUNT(*) FILTER (WHERE score<10::DECIMAL) OVER (PARTITION BY rank_grade_year)AS level_lt10
		, student_id
		, score
		, grade_rank AS rank
		, graderank_percentage AS percentile
		, graderank_pr AS pr
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		calc_sum_rank
	UNION ALL
    --3.2 總計成績 總分 班排名
	SELECT
		rank_school_year 
		, rank_semester
		, rank_grade_year
		, item_type
		, exam_id AS ref_exam_id
		, item_name
		, '班排名'::TEXT AS rank_type
		, rank_class_name AS rank_name
		, true AS is_alive
		, class_count AS matrix_count
		, AVG(score::DECIMAL)FILTER(WHERE class_rank <= TRUNC(class_count * 0.25)) OVER(PARTITION BY rank_class_name) AS avg_top_25
		, AVG(score::DECIMAL)FILTER(WHERE class_rank <= TRUNC(class_count * 0.5)) OVER(PARTITION BY rank_class_name) AS avg_top_50
		, AVG(score::DECIMAL)OVER(PARTITION BY rank_class_name) AS avg
		, AVG(score::DECIMAL)FILTER(WHERE class_rank >= TRUNC(class_count * 0.5)) OVER(PARTITION BY rank_class_name) AS avg_bottom_50
		, AVG(score::DECIMAL)FILTER(WHERE class_rank >= TRUNC(class_count * 0.75)) OVER(PARTITION BY rank_class_name) AS avg_bottom_25
		, COUNT(*) FILTER (WHERE 100::DECIMAL<=score::DECIMAL ) OVER(PARTITION BY rank_class_name)AS level_gte100 
		, COUNT(*) FILTER (WHERE 90::DECIMAL<=score AND score <100::DECIMAL) OVER(PARTITION BY rank_class_name) AS level_90
		, COUNT(*) FILTER (WHERE 80::DECIMAL<=score AND score <90::DECIMAL) OVER(PARTITION BY rank_class_name) AS level_80
		, COUNT(*) FILTER (WHERE 70::DECIMAL<=score AND score <80::DECIMAL) OVER(PARTITION BY rank_class_name)AS level_70
		, COUNT(*) FILTER (WHERE 60::DECIMAL<=score AND score <70::DECIMAL) OVER(PARTITION BY rank_class_name) AS level_60
		, COUNT(*) FILTER (WHERE 50::DECIMAL<=score AND score <60::DECIMAL) OVER(PARTITION BY rank_class_name)AS level_50
		, COUNT(*) FILTER (WHERE 40::DECIMAL<=score AND score <50::DECIMAL) OVER(PARTITION BY rank_class_name)AS level_40
		, COUNT(*) FILTER (WHERE 30::DECIMAL<=score AND score <40::DECIMAL) OVER(PARTITION BY rank_class_name)AS level_30
		, COUNT(*) FILTER (WHERE 20::DECIMAL<=score AND score <30::DECIMAL) OVER(PARTITION BY rank_class_name) AS level_20
		, COUNT(*) FILTER (WHERE 10::DECIMAL<=score AND score <20::DECIMAL) OVER(PARTITION BY rank_class_name)AS level_10
		, COUNT(*) FILTER (WHERE score<10::DECIMAL) OVER (PARTITION BY rank_class_name)AS level_lt10 
		, student_id
		, score
		, class_rank AS rank
		, classrank_percentage AS percentile
		, classrank_pr AS pr
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		calc_sum_rank
	UNION ALL
    --3.3 總計成績 總分 類別1排名
	SELECT
		rank_school_year 
		, rank_semester
		, rank_grade_year
		, item_type
		, exam_id AS ref_exam_id
		, item_name
		, '類別1排名'::TEXT AS rank_type
		, rank_tag1 AS rank_name
		, true AS is_alive
		, tag1_count AS matrix_count
		, AVG(Score::DECIMAL)FILTER(WHERE tag1_rank <= TRUNC(tag1_count * 0.25)) OVER(PARTITION BY rank_grade_year, rank_tag1) AS avg_top_25
		, AVG(Score::DECIMAL)FILTER(WHERE tag1_rank <= TRUNC(tag1_count * 0.5)) OVER(PARTITION BY rank_grade_year, rank_tag1) AS avg_top_50
		, AVG(Score::DECIMAL)OVER(PARTITION BY rank_grade_year, rank_tag1) AS avg
		, AVG(Score::DECIMAL)FILTER(WHERE tag1_rank >= TRUNC(tag1_count * 0.5)) OVER(PARTITION BY rank_grade_year, rank_tag1) AS avg_bottom_50
		, AVG(Score::DECIMAL)FILTER(WHERE tag1_rank >= TRUNC(tag1_count * 0.75)) OVER(PARTITION BY rank_grade_year, rank_tag1) AS avg_bottom_25
		, COUNT(*) FILTER (WHERE 100::DECIMAL<=score::DECIMAL ) OVER(PARTITION BY rank_grade_year, rank_tag1)AS level_gte100 
		, COUNT(*) FILTER (WHERE 90::DECIMAL<=score AND score <100::DECIMAL)OVER(PARTITION BY rank_grade_year, rank_tag1) AS level_90
		, COUNT(*) FILTER (WHERE 80::DECIMAL<=score AND score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1) AS level_80
		, COUNT(*) FILTER (WHERE 70::DECIMAL<=score AND score <80::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1)AS level_70
		, COUNT(*) FILTER (WHERE 60::DECIMAL<=score AND score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1) AS level_60
		, COUNT(*) FILTER (WHERE 50::DECIMAL<=score AND score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1)AS level_50
		, COUNT(*) FILTER (WHERE 40::DECIMAL<=score AND score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1)AS level_40
		, COUNT(*) FILTER (WHERE 30::DECIMAL<=score AND score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1)AS level_30
		, COUNT(*) FILTER (WHERE 20::DECIMAL<=score AND score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1) AS level_20
		, COUNT(*) FILTER (WHERE 10::DECIMAL<=score AND score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1)AS level_10
		, COUNT(*) FILTER (WHERE score<10::DECIMAL) OVER (PARTITION BY rank_grade_year, rank_tag1)AS level_lt10
		, student_id
		, score
		, tag1_rank AS rank
		, tag1rank_percentage AS percentile
		, tag1rank_pr AS pr
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		calc_sum_rank_tag1 AS calc_sum_rank
	WHERE
		rank_tag1 IS NOT NULL
		AND rank_tag1 <> ''
	UNION ALL
    --3.4 總計成績 總分 類別2排名
	SELECT
		rank_school_year 
		, rank_semester
		, rank_grade_year
		, item_type
		, exam_id AS ref_exam_id
		, item_name
		, '類別2排名'::TEXT AS rank_type
		, rank_tag2 AS rank_name
		, true AS is_alive
		, tag2_count AS matrix_count
		, AVG(Score::DECIMAL)FILTER(WHERE tag2_rank <= TRUNC(tag2_count * 0.25)) OVER(PARTITION BY rank_grade_year, rank_tag2) AS avg_top_25
		, AVG(Score::DECIMAL)FILTER(WHERE tag2_rank <= TRUNC(tag2_count * 0.5)) OVER(PARTITION BY rank_grade_year, rank_tag2) AS avg_top_50
		, AVG(Score::DECIMAL)OVER(PARTITION BY rank_grade_year, rank_tag2) AS avg
		, AVG(Score::DECIMAL)FILTER(WHERE tag2_rank >= TRUNC(tag2_count * 0.5)) OVER(PARTITION BY rank_grade_year, rank_tag2) AS avg_bottom_50
		, AVG(Score::DECIMAL)FILTER(WHERE tag2_rank >= TRUNC(tag2_count * 0.75)) OVER(PARTITION BY rank_grade_year, rank_tag2) AS avg_bottom_25
		, COUNT(*) FILTER (WHERE 100::DECIMAL<=score::DECIMAL ) OVER(PARTITION BY rank_grade_year, rank_tag2)AS level_gte100 
		, COUNT(*) FILTER (WHERE 90::DECIMAL<=score AND score <100::DECIMAL)OVER(PARTITION BY rank_grade_year, rank_tag2) AS level_90
		, COUNT(*) FILTER (WHERE 80::DECIMAL<=score AND score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2) AS level_80
		, COUNT(*) FILTER (WHERE 70::DECIMAL<=score AND score <80::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2)AS level_70
		, COUNT(*) FILTER (WHERE 60::DECIMAL<=score AND score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2) AS level_60
		, COUNT(*) FILTER (WHERE 50::DECIMAL<=score AND score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2)AS level_50
		, COUNT(*) FILTER (WHERE 40::DECIMAL<=score AND score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2)AS level_40
		, COUNT(*) FILTER (WHERE 30::DECIMAL<=score AND score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2)AS level_30
		, COUNT(*) FILTER (WHERE 20::DECIMAL<=score AND score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2) AS level_20
		, COUNT(*) FILTER (WHERE 10::DECIMAL<=score AND score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2)AS level_10
		, COUNT(*) FILTER (WHERE score<10::DECIMAL) OVER (PARTITION BY rank_grade_year, rank_tag2)AS level_lt10
		, student_id
		, score
		, tag2_rank AS rank
		, tag2rank_percentage AS percentile
		, tag2rank_pr AS pr
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		calc_sum_rank_tag2 AS calc_sum_rank
	WHERE
		rank_tag2 IS NOT NULL
		AND rank_tag2 <> ''
	UNION ALL
    --4.1 總計成績 平均 年排名
	SELECT
		rank_school_year 
		, rank_semester
		, rank_grade_year
		, item_type
		, exam_id AS ref_exam_id
		, item_name
		, '年排名'::TEXT AS rank_type
		, '' || rank_grade_year || '年級'::TEXT AS rank_name
		, true AS is_alive
		, grade_count AS matrix_count
		, AVG(Score::DECIMAL)FILTER(WHERE grade_rank <= TRUNC(grade_count * 0.25)) OVER(PARTITION BY rank_grade_year) AS avg_top_25
		, AVG(Score::DECIMAL)FILTER(WHERE grade_rank <= TRUNC(grade_count * 0.5)) OVER(PARTITION BY rank_grade_year) AS avg_top_50
		, AVG(Score::DECIMAL)OVER(PARTITION BY rank_grade_year) AS avg
		, AVG(Score::DECIMAL)FILTER(WHERE grade_rank >= TRUNC(grade_count * 0.5)) OVER(PARTITION BY rank_grade_year) AS avg_bottom_50
		, AVG(Score::DECIMAL)FILTER(WHERE grade_rank >= TRUNC(grade_count * 0.75)) OVER(PARTITION BY rank_grade_year) AS avg_bottom_25
		, COUNT(*) FILTER (WHERE 100::DECIMAL<=score::DECIMAL ) OVER(PARTITION BY rank_grade_year)AS level_gte100 
		, COUNT(*) FILTER (WHERE 90::DECIMAL<=score AND score <100::DECIMAL)  OVER(PARTITION BY rank_grade_year) AS level_90
		, COUNT(*) FILTER (WHERE 80::DECIMAL<=score AND score <90::DECIMAL)  OVER(PARTITION BY rank_grade_year) AS level_80
		, COUNT(*) FILTER (WHERE 70::DECIMAL<=score AND  score <80::DECIMAL)  OVER(PARTITION BY rank_grade_year)AS level_70
		, COUNT(*) FILTER (WHERE 60::DECIMAL<=score AND score <70::DECIMAL) OVER(PARTITION BY rank_grade_year) AS level_60
		, COUNT(*) FILTER (WHERE 50::DECIMAL<=score AND score <60::DECIMAL)  OVER (PARTITION BY rank_grade_year)AS level_50
		, COUNT(*) FILTER (WHERE 40::DECIMAL<=score AND score <50::DECIMAL) OVER (PARTITION BY rank_grade_year)AS level_40
		, COUNT(*) FILTER (WHERE 30::DECIMAL<=score AND score <40::DECIMAL)   OVER (PARTITION BY rank_grade_year)AS level_30
		, COUNT(*) FILTER (WHERE 20::DECIMAL<=score AND score <30::DECIMAL)  OVER (PARTITION BY rank_grade_year) AS level_20
		, COUNT(*) FILTER (WHERE 10::DECIMAL<=score AND score <20::DECIMAL)   OVER (PARTITION BY rank_grade_year)AS level_10
		, COUNT(*) FILTER (WHERE score<10::DECIMAL) OVER (PARTITION BY rank_grade_year)AS level_lt10
		, student_id
		, score
		, grade_rank AS rank
		, graderank_percentage AS percentile
		, graderank_pr AS pr
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		calc_avg_rank
	UNION ALL
    --4.2 總計成績 平均 班排名
	SELECT
		rank_school_year 
		, rank_semester
		, rank_grade_year
		, item_type
		, exam_id AS ref_exam_id
		, item_name
		, '班排名'::TEXT AS rank_type
		, rank_class_name AS rank_name
		, true AS is_alive
		, class_count AS matrix_count
		, AVG(score::DECIMAL)FILTER(WHERE class_rank <= TRUNC(class_count * 0.25)) OVER(PARTITION BY rank_class_name) AS avg_top_25
		, AVG(score::DECIMAL)FILTER(WHERE class_rank <= TRUNC(class_count * 0.5)) OVER(PARTITION BY rank_class_name) AS avg_top_50
		, AVG(score::DECIMAL)OVER(PARTITION BY rank_class_name) AS avg
		, AVG(score::DECIMAL)FILTER(WHERE class_rank >= TRUNC(class_count * 0.5)) OVER(PARTITION BY rank_class_name) AS avg_bottom_50
		, AVG(score::DECIMAL)FILTER(WHERE class_rank >= TRUNC(class_count * 0.75)) OVER(PARTITION BY rank_class_name) AS avg_bottom_25
		, COUNT(*) FILTER (WHERE 100::DECIMAL<=score::DECIMAL ) OVER(PARTITION BY rank_class_name)AS level_gte100 
		, COUNT(*) FILTER (WHERE 90::DECIMAL<=score AND score <100::DECIMAL) OVER(PARTITION BY rank_class_name) AS level_90
		, COUNT(*) FILTER (WHERE 80::DECIMAL<=score AND score <90::DECIMAL) OVER(PARTITION BY rank_class_name) AS level_80
		, COUNT(*) FILTER (WHERE 70::DECIMAL<=score AND score <80::DECIMAL) OVER(PARTITION BY rank_class_name)AS level_70
		, COUNT(*) FILTER (WHERE 60::DECIMAL<=score AND score <70::DECIMAL) OVER(PARTITION BY rank_class_name) AS level_60
		, COUNT(*) FILTER (WHERE 50::DECIMAL<=score AND score <60::DECIMAL) OVER(PARTITION BY rank_class_name)AS level_50
		, COUNT(*) FILTER (WHERE 40::DECIMAL<=score AND score <50::DECIMAL) OVER(PARTITION BY rank_class_name)AS level_40
		, COUNT(*) FILTER (WHERE 30::DECIMAL<=score AND score <40::DECIMAL) OVER(PARTITION BY rank_class_name)AS level_30
		, COUNT(*) FILTER (WHERE 20::DECIMAL<=score AND score <30::DECIMAL) OVER(PARTITION BY rank_class_name) AS level_20
		, COUNT(*) FILTER (WHERE 10::DECIMAL<=score AND score <20::DECIMAL) OVER(PARTITION BY rank_class_name)AS level_10
		, COUNT(*) FILTER (WHERE score<10::DECIMAL) OVER (PARTITION BY rank_class_name)AS level_lt10 
		, student_id
		, score
		, class_rank AS rank
		, classrank_percentage AS percentile
		, classrank_pr AS pr
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		calc_avg_rank
	UNION ALL
    --4.3 總計成績 平均 類別1排名
	SELECT
		rank_school_year 
		, rank_semester
		, rank_grade_year
		, item_type
		, exam_id AS ref_exam_id
		, item_name
		, '類別1排名'::TEXT AS rank_type
		, rank_tag1 AS rank_name
		, true AS is_alive
		, tag1_count AS matrix_count
		, AVG(Score::DECIMAL)FILTER(WHERE tag1_rank <= TRUNC(tag1_count * 0.25)) OVER(PARTITION BY rank_grade_year, rank_tag1) AS avg_top_25
		, AVG(Score::DECIMAL)FILTER(WHERE tag1_rank <= TRUNC(tag1_count * 0.5)) OVER(PARTITION BY rank_grade_year, rank_tag1) AS avg_top_50
		, AVG(Score::DECIMAL)OVER(PARTITION BY rank_grade_year, rank_tag1) AS avg
		, AVG(Score::DECIMAL)FILTER(WHERE tag1_rank >= TRUNC(tag1_count * 0.5)) OVER(PARTITION BY rank_grade_year, rank_tag1) AS avg_bottom_50
		, AVG(Score::DECIMAL)FILTER(WHERE tag1_rank >= TRUNC(tag1_count * 0.75)) OVER(PARTITION BY rank_grade_year, rank_tag1) AS avg_bottom_25
		, COUNT(*) FILTER (WHERE 100::DECIMAL<=score::DECIMAL ) OVER(PARTITION BY rank_grade_year, rank_tag1)AS level_gte100 
		, COUNT(*) FILTER (WHERE 90::DECIMAL<=score AND score <100::DECIMAL)OVER(PARTITION BY rank_grade_year, rank_tag1) AS level_90
		, COUNT(*) FILTER (WHERE 80::DECIMAL<=score AND score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1) AS level_80
		, COUNT(*) FILTER (WHERE 70::DECIMAL<=score AND score <80::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1)AS level_70
		, COUNT(*) FILTER (WHERE 60::DECIMAL<=score AND score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1) AS level_60
		, COUNT(*) FILTER (WHERE 50::DECIMAL<=score AND score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1)AS level_50
		, COUNT(*) FILTER (WHERE 40::DECIMAL<=score AND score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1)AS level_40
		, COUNT(*) FILTER (WHERE 30::DECIMAL<=score AND score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1)AS level_30
		, COUNT(*) FILTER (WHERE 20::DECIMAL<=score AND score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1) AS level_20
		, COUNT(*) FILTER (WHERE 10::DECIMAL<=score AND score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1)AS level_10
		, COUNT(*) FILTER (WHERE score<10::DECIMAL) OVER (PARTITION BY rank_grade_year, rank_tag1)AS level_lt10
		, student_id
		, score
		, tag1_rank AS rank
		, tag1rank_percentage AS percentile
		, tag1rank_pr AS pr
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		calc_avg_rank_tag1 AS calc_avg_rank
	WHERE
		rank_tag1 IS NOT NULL
		AND rank_tag1 <> ''
	UNION ALL
    --4.4 總計成績 平均 類別2排名
	SELECT
		rank_school_year 
		, rank_semester
		, rank_grade_year
		, item_type
		, exam_id AS ref_exam_id
		, item_name
		, '類別2排名'::TEXT AS rank_type
		, rank_tag2 AS rank_name
		, true AS is_alive
		, tag2_count AS matrix_count
		, AVG(Score::DECIMAL)FILTER(WHERE tag2_rank <= TRUNC(tag2_count * 0.25)) OVER(PARTITION BY rank_grade_year, rank_tag2) AS avg_top_25
		, AVG(Score::DECIMAL)FILTER(WHERE tag2_rank <= TRUNC(tag2_count * 0.5)) OVER(PARTITION BY rank_grade_year, rank_tag2) AS avg_top_50
		, AVG(Score::DECIMAL)OVER(PARTITION BY rank_grade_year, rank_tag2) AS avg
		, AVG(Score::DECIMAL)FILTER(WHERE tag2_rank >= TRUNC(tag2_count * 0.5)) OVER(PARTITION BY rank_grade_year, rank_tag2) AS avg_bottom_50
		, AVG(Score::DECIMAL)FILTER(WHERE tag2_rank >= TRUNC(tag2_count * 0.75)) OVER(PARTITION BY rank_grade_year, rank_tag2) AS avg_bottom_25
		, COUNT(*) FILTER (WHERE 100::DECIMAL<=score::DECIMAL ) OVER(PARTITION BY rank_grade_year, rank_tag2)AS level_gte100 
		, COUNT(*) FILTER (WHERE 90::DECIMAL<=score AND score <100::DECIMAL)OVER(PARTITION BY rank_grade_year, rank_tag2) AS level_90
		, COUNT(*) FILTER (WHERE 80::DECIMAL<=score AND score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2) AS level_80
		, COUNT(*) FILTER (WHERE 70::DECIMAL<=score AND score <80::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2)AS level_70
		, COUNT(*) FILTER (WHERE 60::DECIMAL<=score AND score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2) AS level_60
		, COUNT(*) FILTER (WHERE 50::DECIMAL<=score AND score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2)AS level_50
		, COUNT(*) FILTER (WHERE 40::DECIMAL<=score AND score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2)AS level_40
		, COUNT(*) FILTER (WHERE 30::DECIMAL<=score AND score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2)AS level_30
		, COUNT(*) FILTER (WHERE 20::DECIMAL<=score AND score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2) AS level_20
		, COUNT(*) FILTER (WHERE 10::DECIMAL<=score AND score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2)AS level_10
		, COUNT(*) FILTER (WHERE score<10::DECIMAL) OVER (PARTITION BY rank_grade_year, rank_tag2)AS level_lt10
		, student_id
		, score
		, tag2_rank AS rank
		, tag2rank_percentage AS percentile
		, tag2rank_pr AS pr
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		calc_avg_rank_tag2 AS calc_avg_rank
	WHERE
		rank_tag2 IS NOT NULL
		AND rank_tag2 <> ''
	UNION ALL
    --5.1 總計成績 加權平均 年排名
	SELECT
		rank_school_year 
		, rank_semester
		, rank_grade_year
		, item_type
		, exam_id AS ref_exam_id
		, item_name
		, '年排名'::TEXT AS rank_type
		, '' || rank_grade_year || '年級'::TEXT AS rank_name
		, true AS is_alive
		, grade_count AS matrix_count
		, AVG(Score::DECIMAL)FILTER(WHERE grade_rank <= TRUNC(grade_count * 0.25)) OVER(PARTITION BY rank_grade_year) AS avg_top_25
		, AVG(Score::DECIMAL)FILTER(WHERE grade_rank <= TRUNC(grade_count * 0.5)) OVER(PARTITION BY rank_grade_year) AS avg_top_50
		, AVG(Score::DECIMAL)OVER(PARTITION BY rank_grade_year) AS avg
		, AVG(Score::DECIMAL)FILTER(WHERE grade_rank >= TRUNC(grade_count * 0.5)) OVER(PARTITION BY rank_grade_year) AS avg_bottom_50
		, AVG(Score::DECIMAL)FILTER(WHERE grade_rank >= TRUNC(grade_count * 0.75)) OVER(PARTITION BY rank_grade_year) AS avg_bottom_25
		, COUNT(*) FILTER (WHERE 100::DECIMAL<=score::DECIMAL ) OVER(PARTITION BY rank_grade_year)AS level_gte100 
		, COUNT(*) FILTER (WHERE 90::DECIMAL<=score AND score <100::DECIMAL)  OVER(PARTITION BY rank_grade_year) AS level_90
		, COUNT(*) FILTER (WHERE 80::DECIMAL<=score AND score <90::DECIMAL)  OVER(PARTITION BY rank_grade_year) AS level_80
		, COUNT(*) FILTER (WHERE 70::DECIMAL<=score AND  score <80::DECIMAL)  OVER(PARTITION BY rank_grade_year)AS level_70
		, COUNT(*) FILTER (WHERE 60::DECIMAL<=score AND score <70::DECIMAL) OVER(PARTITION BY rank_grade_year) AS level_60
		, COUNT(*) FILTER (WHERE 50::DECIMAL<=score AND score <60::DECIMAL)  OVER (PARTITION BY rank_grade_year)AS level_50
		, COUNT(*) FILTER (WHERE 40::DECIMAL<=score AND score <50::DECIMAL) OVER (PARTITION BY rank_grade_year)AS level_40
		, COUNT(*) FILTER (WHERE 30::DECIMAL<=score AND score <40::DECIMAL)   OVER (PARTITION BY rank_grade_year)AS level_30
		, COUNT(*) FILTER (WHERE 20::DECIMAL<=score AND score <30::DECIMAL)  OVER (PARTITION BY rank_grade_year) AS level_20
		, COUNT(*) FILTER (WHERE 10::DECIMAL<=score AND score <20::DECIMAL)   OVER (PARTITION BY rank_grade_year)AS level_10
		, COUNT(*) FILTER (WHERE score<10::DECIMAL) OVER (PARTITION BY rank_grade_year)AS level_lt10
		, student_id
		, score
		, grade_rank AS rank
		, graderank_percentage AS percentile
		, graderank_pr AS pr
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		weight_sum_rank
	UNION ALL
    --5.2 總計成績 加權平均 班排名
	SELECT
		rank_school_year 
		, rank_semester
		, rank_grade_year
		, item_type
		, exam_id AS ref_exam_id
		, item_name
		, '班排名'::TEXT AS rank_type
		, rank_class_name AS rank_name
		, true AS is_alive
		, class_count AS matrix_count
		, AVG(score::DECIMAL)FILTER(WHERE class_rank <= TRUNC(class_count * 0.25)) OVER(PARTITION BY rank_class_name) AS avg_top_25
		, AVG(score::DECIMAL)FILTER(WHERE class_rank <= TRUNC(class_count * 0.5)) OVER(PARTITION BY rank_class_name) AS avg_top_50
		, AVG(score::DECIMAL)OVER(PARTITION BY rank_class_name) AS avg
		, AVG(score::DECIMAL)FILTER(WHERE class_rank >= TRUNC(class_count * 0.5)) OVER(PARTITION BY rank_class_name) AS avg_bottom_50
		, AVG(score::DECIMAL)FILTER(WHERE class_rank >= TRUNC(class_count * 0.75)) OVER(PARTITION BY rank_class_name) AS avg_bottom_25
		, COUNT(*) FILTER (WHERE 100::DECIMAL<=score::DECIMAL ) OVER(PARTITION BY rank_class_name)AS level_gte100 
		, COUNT(*) FILTER (WHERE 90::DECIMAL<=score AND score <100::DECIMAL) OVER(PARTITION BY rank_class_name) AS level_90
		, COUNT(*) FILTER (WHERE 80::DECIMAL<=score AND score <90::DECIMAL) OVER(PARTITION BY rank_class_name) AS level_80
		, COUNT(*) FILTER (WHERE 70::DECIMAL<=score AND score <80::DECIMAL) OVER(PARTITION BY rank_class_name)AS level_70
		, COUNT(*) FILTER (WHERE 60::DECIMAL<=score AND score <70::DECIMAL) OVER(PARTITION BY rank_class_name) AS level_60
		, COUNT(*) FILTER (WHERE 50::DECIMAL<=score AND score <60::DECIMAL) OVER(PARTITION BY rank_class_name)AS level_50
		, COUNT(*) FILTER (WHERE 40::DECIMAL<=score AND score <50::DECIMAL) OVER(PARTITION BY rank_class_name)AS level_40
		, COUNT(*) FILTER (WHERE 30::DECIMAL<=score AND score <40::DECIMAL) OVER(PARTITION BY rank_class_name)AS level_30
		, COUNT(*) FILTER (WHERE 20::DECIMAL<=score AND score <30::DECIMAL) OVER(PARTITION BY rank_class_name) AS level_20
		, COUNT(*) FILTER (WHERE 10::DECIMAL<=score AND score <20::DECIMAL) OVER(PARTITION BY rank_class_name)AS level_10
		, COUNT(*) FILTER (WHERE score<10::DECIMAL) OVER (PARTITION BY rank_class_name)AS level_lt10 
		, student_id
		, score
		, class_rank AS rank
		, classrank_percentage AS percentile
		, classrank_pr AS pr
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		weight_sum_rank
	UNION ALL
    --5.3 總計成績 加權平均 類別1排名
	SELECT
		rank_school_year 
		, rank_semester
		, rank_grade_year
		, item_type
		, exam_id AS ref_exam_id
		, item_name
		, '類別1排名'::TEXT AS rank_type
		, rank_tag1 AS rank_name
		, true AS is_alive
		, tag1_count AS matrix_count
		, AVG(Score::DECIMAL)FILTER(WHERE tag1_rank <= TRUNC(tag1_count * 0.25)) OVER(PARTITION BY rank_grade_year, rank_tag1) AS avg_top_25
		, AVG(Score::DECIMAL)FILTER(WHERE tag1_rank <= TRUNC(tag1_count * 0.5)) OVER(PARTITION BY rank_grade_year, rank_tag1) AS avg_top_50
		, AVG(Score::DECIMAL)OVER(PARTITION BY rank_grade_year, rank_tag1) AS avg
		, AVG(Score::DECIMAL)FILTER(WHERE tag1_rank >= TRUNC(tag1_count * 0.5)) OVER(PARTITION BY rank_grade_year, rank_tag1) AS avg_bottom_50
		, AVG(Score::DECIMAL)FILTER(WHERE tag1_rank >= TRUNC(tag1_count * 0.75)) OVER(PARTITION BY rank_grade_year, rank_tag1) AS avg_bottom_25
		, COUNT(*) FILTER (WHERE 100::DECIMAL<=score::DECIMAL ) OVER(PARTITION BY rank_grade_year, rank_tag1)AS level_gte100 
		, COUNT(*) FILTER (WHERE 90::DECIMAL<=score AND score <100::DECIMAL)OVER(PARTITION BY rank_grade_year, rank_tag1) AS level_90
		, COUNT(*) FILTER (WHERE 80::DECIMAL<=score AND score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1) AS level_80
		, COUNT(*) FILTER (WHERE 70::DECIMAL<=score AND score <80::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1)AS level_70
		, COUNT(*) FILTER (WHERE 60::DECIMAL<=score AND score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1) AS level_60
		, COUNT(*) FILTER (WHERE 50::DECIMAL<=score AND score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1)AS level_50
		, COUNT(*) FILTER (WHERE 40::DECIMAL<=score AND score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1)AS level_40
		, COUNT(*) FILTER (WHERE 30::DECIMAL<=score AND score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1)AS level_30
		, COUNT(*) FILTER (WHERE 20::DECIMAL<=score AND score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1) AS level_20
		, COUNT(*) FILTER (WHERE 10::DECIMAL<=score AND score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1)AS level_10
		, COUNT(*) FILTER (WHERE score<10::DECIMAL) OVER (PARTITION BY rank_grade_year, rank_tag1)AS level_lt10
		, student_id
		, score
		, tag1_rank AS rank
		, tag1rank_percentage AS percentile
		, tag1rank_pr AS pr
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		weight_sum_rank_tag1 AS weight_sum_rank
	WHERE
		rank_tag1 IS NOT NULL
		AND rank_tag1 <> ''
	UNION ALL
    --5.4 總計成績 加權平均 類別2排名
	SELECT
		rank_school_year 
		, rank_semester
		, rank_grade_year
		, item_type
		, exam_id AS ref_exam_id
		, item_name
		, '類別2排名'::TEXT AS rank_type
		, rank_tag2 AS rank_name
		, true AS is_alive
		, tag2_count AS matrix_count
		, AVG(Score::DECIMAL)FILTER(WHERE tag2_rank <= TRUNC(tag2_count * 0.25)) OVER(PARTITION BY rank_grade_year, rank_tag2) AS avg_top_25
		, AVG(Score::DECIMAL)FILTER(WHERE tag2_rank <= TRUNC(tag2_count * 0.5)) OVER(PARTITION BY rank_grade_year, rank_tag2) AS avg_top_50
		, AVG(Score::DECIMAL)OVER(PARTITION BY rank_grade_year, rank_tag2) AS avg
		, AVG(Score::DECIMAL)FILTER(WHERE tag2_rank >= TRUNC(tag2_count * 0.5)) OVER(PARTITION BY rank_grade_year, rank_tag2) AS avg_bottom_50
		, AVG(Score::DECIMAL)FILTER(WHERE tag2_rank >= TRUNC(tag2_count * 0.75)) OVER(PARTITION BY rank_grade_year, rank_tag2) AS avg_bottom_25
		, COUNT(*) FILTER (WHERE 100::DECIMAL<=score::DECIMAL ) OVER(PARTITION BY rank_grade_year, rank_tag2)AS level_gte100 
		, COUNT(*) FILTER (WHERE 90::DECIMAL<=score AND score <100::DECIMAL)OVER(PARTITION BY rank_grade_year, rank_tag2) AS level_90
		, COUNT(*) FILTER (WHERE 80::DECIMAL<=score AND score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2) AS level_80
		, COUNT(*) FILTER (WHERE 70::DECIMAL<=score AND score <80::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2)AS level_70
		, COUNT(*) FILTER (WHERE 60::DECIMAL<=score AND score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2) AS level_60
		, COUNT(*) FILTER (WHERE 50::DECIMAL<=score AND score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2)AS level_50
		, COUNT(*) FILTER (WHERE 40::DECIMAL<=score AND score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2)AS level_40
		, COUNT(*) FILTER (WHERE 30::DECIMAL<=score AND score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2)AS level_30
		, COUNT(*) FILTER (WHERE 20::DECIMAL<=score AND score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2) AS level_20
		, COUNT(*) FILTER (WHERE 10::DECIMAL<=score AND score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2)AS level_10
		, COUNT(*) FILTER (WHERE score<10::DECIMAL) OVER (PARTITION BY rank_grade_year, rank_tag2)AS level_lt10
		, student_id
		, score
		, tag2_rank AS rank
		, tag2rank_percentage AS percentile
		, tag2rank_pr AS pr
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		weight_sum_rank_tag2 AS weight_sum_rank
	WHERE
		rank_tag2 IS NOT NULL
		AND rank_tag2 <> ''
	UNION ALL
    --6.1 總計成績 加權平均 年排名
	SELECT
		rank_school_year 
		, rank_semester
		, rank_grade_year
		, item_type
		, exam_id AS ref_exam_id
		, item_name
		, '年排名'::TEXT AS rank_type
		, '' || rank_grade_year || '年級'::TEXT AS rank_name
		, true AS is_alive
		, grade_count AS matrix_count
		, AVG(Score::DECIMAL)FILTER(WHERE grade_rank <= TRUNC(grade_count * 0.25)) OVER(PARTITION BY rank_grade_year) AS avg_top_25
		, AVG(Score::DECIMAL)FILTER(WHERE grade_rank <= TRUNC(grade_count * 0.5)) OVER(PARTITION BY rank_grade_year) AS avg_top_50
		, AVG(Score::DECIMAL)OVER(PARTITION BY rank_grade_year) AS avg
		, AVG(Score::DECIMAL)FILTER(WHERE grade_rank >= TRUNC(grade_count * 0.5)) OVER(PARTITION BY rank_grade_year) AS avg_bottom_50
		, AVG(Score::DECIMAL)FILTER(WHERE grade_rank >= TRUNC(grade_count * 0.75)) OVER(PARTITION BY rank_grade_year) AS avg_bottom_25
		, COUNT(*) FILTER (WHERE 100::DECIMAL<=score::DECIMAL ) OVER(PARTITION BY rank_grade_year)AS level_gte100 
		, COUNT(*) FILTER (WHERE 90::DECIMAL<=score AND score <100::DECIMAL)  OVER(PARTITION BY rank_grade_year) AS level_90
		, COUNT(*) FILTER (WHERE 80::DECIMAL<=score AND score <90::DECIMAL)  OVER(PARTITION BY rank_grade_year) AS level_80
		, COUNT(*) FILTER (WHERE 70::DECIMAL<=score AND  score <80::DECIMAL)  OVER(PARTITION BY rank_grade_year)AS level_70
		, COUNT(*) FILTER (WHERE 60::DECIMAL<=score AND score <70::DECIMAL) OVER(PARTITION BY rank_grade_year) AS level_60
		, COUNT(*) FILTER (WHERE 50::DECIMAL<=score AND score <60::DECIMAL)  OVER (PARTITION BY rank_grade_year)AS level_50
		, COUNT(*) FILTER (WHERE 40::DECIMAL<=score AND score <50::DECIMAL) OVER (PARTITION BY rank_grade_year)AS level_40
		, COUNT(*) FILTER (WHERE 30::DECIMAL<=score AND score <40::DECIMAL)   OVER (PARTITION BY rank_grade_year)AS level_30
		, COUNT(*) FILTER (WHERE 20::DECIMAL<=score AND score <30::DECIMAL)  OVER (PARTITION BY rank_grade_year) AS level_20
		, COUNT(*) FILTER (WHERE 10::DECIMAL<=score AND score <20::DECIMAL)   OVER (PARTITION BY rank_grade_year)AS level_10
		, COUNT(*) FILTER (WHERE score<10::DECIMAL) OVER (PARTITION BY rank_grade_year)AS level_lt10
		, student_id
		, score
		, grade_rank AS rank
		, graderank_percentage AS percentile
		, graderank_pr AS pr
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		weight_avg_rank
	UNION ALL
    --6.2 總計成績 加權平均 班排名
	SELECT
		rank_school_year 
		, rank_semester
		, rank_grade_year
		, item_type
		, exam_id AS ref_exam_id
		, item_name
		, '班排名'::TEXT AS rank_type
		, rank_class_name AS rank_name
		, true AS is_alive
		, class_count AS matrix_count
		, AVG(score::DECIMAL)FILTER(WHERE class_rank <= TRUNC(class_count * 0.25)) OVER(PARTITION BY rank_class_name) AS avg_top_25
		, AVG(score::DECIMAL)FILTER(WHERE class_rank <= TRUNC(class_count * 0.5)) OVER(PARTITION BY rank_class_name) AS avg_top_50
		, AVG(score::DECIMAL)OVER(PARTITION BY rank_class_name) AS avg
		, AVG(score::DECIMAL)FILTER(WHERE class_rank >= TRUNC(class_count * 0.5)) OVER(PARTITION BY rank_class_name) AS avg_bottom_50
		, AVG(score::DECIMAL)FILTER(WHERE class_rank >= TRUNC(class_count * 0.75)) OVER(PARTITION BY rank_class_name) AS avg_bottom_25
		, COUNT(*) FILTER (WHERE 100::DECIMAL<=score::DECIMAL ) OVER(PARTITION BY rank_class_name)AS level_gte100 
		, COUNT(*) FILTER (WHERE 90::DECIMAL<=score AND score <100::DECIMAL) OVER(PARTITION BY rank_class_name) AS level_90
		, COUNT(*) FILTER (WHERE 80::DECIMAL<=score AND score <90::DECIMAL) OVER(PARTITION BY rank_class_name) AS level_80
		, COUNT(*) FILTER (WHERE 70::DECIMAL<=score AND score <80::DECIMAL) OVER(PARTITION BY rank_class_name)AS level_70
		, COUNT(*) FILTER (WHERE 60::DECIMAL<=score AND score <70::DECIMAL) OVER(PARTITION BY rank_class_name) AS level_60
		, COUNT(*) FILTER (WHERE 50::DECIMAL<=score AND score <60::DECIMAL) OVER(PARTITION BY rank_class_name)AS level_50
		, COUNT(*) FILTER (WHERE 40::DECIMAL<=score AND score <50::DECIMAL) OVER(PARTITION BY rank_class_name)AS level_40
		, COUNT(*) FILTER (WHERE 30::DECIMAL<=score AND score <40::DECIMAL) OVER(PARTITION BY rank_class_name)AS level_30
		, COUNT(*) FILTER (WHERE 20::DECIMAL<=score AND score <30::DECIMAL) OVER(PARTITION BY rank_class_name) AS level_20
		, COUNT(*) FILTER (WHERE 10::DECIMAL<=score AND score <20::DECIMAL) OVER(PARTITION BY rank_class_name)AS level_10
		, COUNT(*) FILTER (WHERE score<10::DECIMAL) OVER (PARTITION BY rank_class_name)AS level_lt10 
		, student_id
		, score
		, class_rank AS rank
		, classrank_percentage AS percentile
		, classrank_pr AS pr
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		weight_avg_rank
	UNION ALL
    --6.3 總計成績 加權平均 類別1排名
	SELECT
		rank_school_year 
		, rank_semester
		, rank_grade_year
		, item_type
		, exam_id AS ref_exam_id
		, item_name
		, '類別1排名'::TEXT AS rank_type
		, rank_tag1 AS rank_name
		, true AS is_alive
		, tag1_count AS matrix_count
		, AVG(Score::DECIMAL)FILTER(WHERE tag1_rank <= TRUNC(tag1_count * 0.25)) OVER(PARTITION BY rank_grade_year, rank_tag1) AS avg_top_25
		, AVG(Score::DECIMAL)FILTER(WHERE tag1_rank <= TRUNC(tag1_count * 0.5)) OVER(PARTITION BY rank_grade_year, rank_tag1) AS avg_top_50
		, AVG(Score::DECIMAL)OVER(PARTITION BY rank_grade_year, rank_tag1) AS avg
		, AVG(Score::DECIMAL)FILTER(WHERE tag1_rank >= TRUNC(tag1_count * 0.5)) OVER(PARTITION BY rank_grade_year, rank_tag1) AS avg_bottom_50
		, AVG(Score::DECIMAL)FILTER(WHERE tag1_rank >= TRUNC(tag1_count * 0.75)) OVER(PARTITION BY rank_grade_year, rank_tag1) AS avg_bottom_25
		, COUNT(*) FILTER (WHERE 100::DECIMAL<=score::DECIMAL ) OVER(PARTITION BY rank_grade_year, rank_tag1)AS level_gte100 
		, COUNT(*) FILTER (WHERE 90::DECIMAL<=score AND score <100::DECIMAL)OVER(PARTITION BY rank_grade_year, rank_tag1) AS level_90
		, COUNT(*) FILTER (WHERE 80::DECIMAL<=score AND score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1) AS level_80
		, COUNT(*) FILTER (WHERE 70::DECIMAL<=score AND score <80::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1)AS level_70
		, COUNT(*) FILTER (WHERE 60::DECIMAL<=score AND score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1) AS level_60
		, COUNT(*) FILTER (WHERE 50::DECIMAL<=score AND score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1)AS level_50
		, COUNT(*) FILTER (WHERE 40::DECIMAL<=score AND score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1)AS level_40
		, COUNT(*) FILTER (WHERE 30::DECIMAL<=score AND score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1)AS level_30
		, COUNT(*) FILTER (WHERE 20::DECIMAL<=score AND score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1) AS level_20
		, COUNT(*) FILTER (WHERE 10::DECIMAL<=score AND score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1)AS level_10
		, COUNT(*) FILTER (WHERE score<10::DECIMAL) OVER (PARTITION BY rank_grade_year, rank_tag1)AS level_lt10
		, student_id
		, score
		, tag1_rank AS rank
		, tag1rank_percentage AS percentile
		, tag1rank_pr AS pr
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		weight_avg_rank_tag1 AS weight_avg_rank
	WHERE
		rank_tag1 IS NOT NULL
		AND rank_tag1 <> ''
	UNION ALL
    --6.4 總計成績 加權平均 類別2排名
	SELECT
		rank_school_year 
		, rank_semester
		, rank_grade_year
		, item_type
		, exam_id AS ref_exam_id
		, item_name
		, '類別2排名'::TEXT AS rank_type
		, rank_tag2 AS rank_name
		, true AS is_alive
		, tag2_count AS matrix_count
		, AVG(Score::DECIMAL)FILTER(WHERE tag2_rank <= TRUNC(tag2_count * 0.25)) OVER(PARTITION BY rank_grade_year, rank_tag2) AS avg_top_25
		, AVG(Score::DECIMAL)FILTER(WHERE tag2_rank <= TRUNC(tag2_count * 0.5)) OVER(PARTITION BY rank_grade_year, rank_tag2) AS avg_top_50
		, AVG(Score::DECIMAL)OVER(PARTITION BY rank_grade_year, rank_tag2) AS avg
		, AVG(Score::DECIMAL)FILTER(WHERE tag2_rank >= TRUNC(tag2_count * 0.5)) OVER(PARTITION BY rank_grade_year, rank_tag2) AS avg_bottom_50
		, AVG(Score::DECIMAL)FILTER(WHERE tag2_rank >= TRUNC(tag2_count * 0.75)) OVER(PARTITION BY rank_grade_year, rank_tag2) AS avg_bottom_25
		, COUNT(*) FILTER (WHERE 100::DECIMAL<=score::DECIMAL ) OVER(PARTITION BY rank_grade_year, rank_tag2)AS level_gte100 
		, COUNT(*) FILTER (WHERE 90::DECIMAL<=score AND score <100::DECIMAL)OVER(PARTITION BY rank_grade_year, rank_tag2) AS level_90
		, COUNT(*) FILTER (WHERE 80::DECIMAL<=score AND score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2) AS level_80
		, COUNT(*) FILTER (WHERE 70::DECIMAL<=score AND score <80::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2)AS level_70
		, COUNT(*) FILTER (WHERE 60::DECIMAL<=score AND score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2) AS level_60
		, COUNT(*) FILTER (WHERE 50::DECIMAL<=score AND score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2)AS level_50
		, COUNT(*) FILTER (WHERE 40::DECIMAL<=score AND score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2)AS level_40
		, COUNT(*) FILTER (WHERE 30::DECIMAL<=score AND score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2)AS level_30
		, COUNT(*) FILTER (WHERE 20::DECIMAL<=score AND score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2) AS level_20
		, COUNT(*) FILTER (WHERE 10::DECIMAL<=score AND score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2)AS level_10
		, COUNT(*) FILTER (WHERE score<10::DECIMAL) OVER (PARTITION BY rank_grade_year, rank_tag2)AS level_lt10
		, student_id
		, score
		, tag2_rank AS rank
		, tag2rank_percentage AS percentile
		, tag2rank_pr AS pr
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		weight_avg_rank_tag2 AS weight_avg_rank
	WHERE
		rank_tag2 IS NOT NULL
		AND rank_tag2 <> ''
), update_data AS (
	UPDATE
		rank_matrix
	SET
		is_alive = NULL
	FROM 
		row
	WHERE
		rank_matrix.is_alive = true
		AND rank_matrix.school_year = row.rank_school_year::INT
		AND rank_matrix.semester = row.rank_semester::INT
		AND rank_matrix.grade_year = row.rank_grade_year::INT
		AND rank_matrix.ref_exam_id = row.ref_exam_id::INT

	RETURNING rank_matrix.*
), insert_batch_data AS (
	INSERT INTO rank_batch(
		school_year
		, semester
		, calculation_description
		, setting
	)
	SELECT
		DISTINCT
		row.rank_school_year::INT
		, row.rank_semester::INT
		, row.rank_school_year||' '||row.rank_semester||' 計算'||row.rank_exam_name||'排名' AS calculation_description
		, row.calculation_setting
	FROM
		row

	RETURNING *
), insert_matrix_data AS (
	INSERT INTO rank_matrix(
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
		, avg_top_25
		, avg_top_50
		, avg
		, avg_bottom_50
		, avg_bottom_25
		, level_gte100
		, level_90
		, level_80
		, level_70
		, level_60
		, level_50
		, level_40
		, level_30
		, level_20
		, level_10
		, level_lt10
	)
	SELECT DISTINCT
		insert_batch_data.id AS ref_batch_id
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
		, score_list.avg_top_25
		, score_list.avg_top_50
		, score_list.avg
		, score_list.avg_bottom_50
		, score_list.avg_bottom_25
		, score_list.level_gte100
		, score_list.level_90
		, score_list.level_80
		, score_list.level_70
		, score_list.level_60
		, score_list.level_50
		, score_list.level_40
		, score_list.level_30
		, score_list.level_20
		, score_list.level_10
		, score_list.level_lt10
	FROM
		score_list
		LEFT OUTER JOIN update_data
			ON update_data.id  < 0 --永遠為false，只是為了讓insert等待update執行完
		CROSS JOIN insert_batch_data
	RETURNING *
), insert_batch_student_data AS (
	INSERT INTO rank_batch_student(
		ref_batch_id
		, ref_student_id
		, grade_year
		, matrix_grade
		, matrix_class
		, matrix_tag1
		, matrix_tag2
	)
	SELECT
		insert_batch_data.id AS ref_batch_id
		, student_row.student_id
		, student_row.rank_grade_year
		, student_row.rank_grade_year||'年級' AS matrix_grade
		, student_row.rank_class_name
		, student_row.rank_tag1
		, student_row.rank_tag2
	FROM
		student_row
		CROSS JOIN insert_batch_data
), insert_detail_data AS (
	INSERT INTO rank_detail(
		ref_matrix_id
		, ref_student_id
		, score
		, rank
		, percentile
        , pr
	)
	SELECT
		insert_matrix_data.id AS ref_matrix_id
		, score_list.student_id AS ref_student_id
		, score_list.score AS score
		, score_list.rank AS rank
		, score_list.percentile AS percentile
		, score_list.pr AS pr
	FROM
		score_list
		LEFT OUTER JOIN insert_matrix_data
			ON insert_matrix_data.school_year = score_list.rank_school_year
			AND insert_matrix_data.semester = score_list.rank_semester
			AND insert_matrix_data.grade_year = score_list.rank_grade_year
			AND insert_matrix_data.item_type = score_list.item_type
			AND insert_matrix_data.ref_exam_id = score_list.ref_exam_id
			AND insert_matrix_data.item_name = score_list.item_name
			AND insert_matrix_data.rank_type = score_list.rank_type
			AND insert_matrix_data.rank_name = score_list.rank_name
)
SELECT
	score_list.rank_school_year
	, score_list.rank_semester
	, score_list.rank_grade_year
	, score_list.item_type
	, score_list.ref_exam_id
	, score_list.item_name
	, score_list.rank_type
	, score_list.rank_name
	, score_list.student_id
FROM 
	score_list
	LEFT OUTER JOIN insert_matrix_data
		ON insert_matrix_data.school_year = score_list.rank_school_year
		AND insert_matrix_data.semester = score_list.rank_semester
		AND insert_matrix_data.grade_year = score_list.rank_grade_year
		AND insert_matrix_data.item_type = score_list.item_type
		AND insert_matrix_data.ref_exam_id = score_list.ref_exam_id
		AND insert_matrix_data.item_name = score_list.item_name
		AND insert_matrix_data.rank_type = score_list.rank_type
		AND insert_matrix_data.rank_name = score_list.rank_name
";
                    #endregion

                    bkw.ReportProgress(50);
                    QueryHelper queryHelper = new QueryHelper();
                    queryHelper.Select(insertRankSql);
                    bkw.ReportProgress(100);
                }
                catch (Exception exception)
                {
                    bkwException = exception;
                }
            };

            bkw.RunWorkerCompleted += delegate
            {
                if (bkwException != null)
                {
                    btnCacluate.Enabled = true;
                    btnPrevious.Enabled = true;
                    throw new Exception("計算排名失敗", bkwException);
                }
                MessageBox.Show("計算完成");
                MotherForm.SetStatusBarMessage("排名計算完成");
                pbLoading.Visible = false;
                btnCacluate.Enabled = true;
                btnPrevious.Enabled = true;
                this.Close();
            };

            bkw.RunWorkerAsync();
        }

        private void cboStudentTag1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboStudentTag1.SelectedIndex != 0)
            {
                cboStudentTag2.Enabled = true;
            }
            else
            {
                cboStudentTag2.SelectedIndex = 0;
                cboStudentTag2.Enabled = false;
            }
            if (cboStudentTag1.Text == cboStudentTag2.Text || cboStudentTag1.Text == cboStudentFilter.Text)
            {
                cboStudentTag1.SelectedIndex = 0;
            }
        }

        private void cboStudentTag2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboStudentTag2.Text == cboStudentTag1.Text || cboStudentTag2.Text == cboStudentFilter.Text)
            {
                cboStudentTag2.SelectedIndex = 0;
            }
        }

        private void cboStudentFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboStudentFilter.Text == cboStudentTag1.Text || cboStudentFilter.Text == cboStudentTag2.Text)
            {
                cboStudentFilter.SelectedIndex = 0;
            }
        }

        private void btnImport_Click(object sender, EventArgs e)
        {

        }

        private void btnExport_Click(object sender, EventArgs e)
        {

        }

        private void btnPrevious_Click(object sender, EventArgs e)
        {
            #region 清除Panel裡的CheckBox
            foreach (CheckBox checkBox in _CheckBoxList)
            {
                plStudentView.Controls.Remove(checkBox);
            }
            #endregion

            btnCacluate.Enabled = true;
            plSetting.Visible = true;
            plStudentView.Visible = false;
            _CheckBoxList = new List<CheckBox>();
            if (dgvStudentList.Rows.Count > 0)
            {
                dgvStudentList.Rows.Clear();
            }
        }

        private void CalculateRegularAssessmentRank_Resize(object sender, EventArgs e)
        {
            //調整Loading圖案的位置
            pbLoading.Location = new Point(this.Width / 2 - 20, this.Height / 2 - 20);
        }
    }
}
