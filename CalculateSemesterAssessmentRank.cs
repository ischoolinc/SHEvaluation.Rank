using FISCA.Data;
using FISCA.Presentation;
using FISCA.Presentation.Controls;
using K12.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace SHEvaluation.Rank
{
    public partial class CalculateSemesterAssessmentRank : BaseForm
    {
        string _DefaultSchoolYear = "";
        string _DefaultSemester = "";
        List<TagConfigRecord> _TagList = new List<TagConfigRecord>();
        List<StudentRecord> _StudentList = new List<StudentRecord>();
        List<StudentRecord> _StudentFilterList = new List<StudentRecord>();
        List<int> _GradeYearList = new List<int>();

        public CalculateSemesterAssessmentRank()
        {
            InitializeComponent();

            #region 讀取需要的資料
            _DefaultSchoolYear = K12.Data.School.DefaultSchoolYear;
            _DefaultSemester = K12.Data.School.DefaultSemester;
            _TagList = K12.Data.TagConfig.SelectByCategory(TagCategory.Student);
            _StudentList = K12.Data.Student.SelectAll().Where(x => (x.Status == StudentRecord.StudentStatus.一般)
                                                            && !string.IsNullOrEmpty(x.RefClassID)
                                                            && x.Class.GradeYear != null).ToList();
            #endregion
        }

        private void CalculateSemesterAssessmentRank_Load(object sender, EventArgs e)
        {
            plStudentView.Visible = false;

            #region 將資料填入comboBox
            //因為目前只提供計算預設學年度學期的排名，所以這邊先註解起來
            //cboSchoolYear.Items.Add(_DefaultSchoolYear);
            //cboSchoolYear.SelectedIndex = 0;
            //cboSemester.Items.Add(_DefaultSemester);
            //cboSemester.SelectedIndex = 0;
            lbCalcSchoolYear.Text = _DefaultSchoolYear;
            lbCalcSemester.Text = _DefaultSemester;

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
            #endregion

            #region 將年級資料填入listView
            foreach (int gradeYear in _StudentList.Select(x => x.Class.GradeYear).Distinct().OrderBy(x => x).ToList())
            {
                ListViewItem listViewItem = new ListViewItem();
                listViewItem.Text = "" + gradeYear + "年級";
                listViewItem.Checked = true;
                listGradeYear.Items.Add(listViewItem);
            }
            #endregion
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

        private void btnNext_Click(object sender, EventArgs e)
        {
            plStudentView.Visible = true;

            //因為目前只提供計算預設學年度學期的排名，所以這邊先註解起來
            lbSchoolYear.Text = lbCalcSchoolYear.Text; //cboSchoolYear.Text;
            lbSemester.Text = lbCalcSemester.Text; //cboSemester.Text;

            string studentFilter = cboStudentFilter.Text.Trim('[', ']');
            string studentTag1 = cboStudentTag1.Text.Trim('[', ']');
            string studentTag2 = cboStudentTag2.Text.Trim('[', ']');

            _GradeYearList = new List<int>();
            foreach (ListViewItem listViewItem in listGradeYear.Items)
            {
                if (listViewItem.Checked == true)
                {
                    int gradeYear = Convert.ToInt32(listViewItem.Text.Trim('年', '級'));
                    _GradeYearList.Add(gradeYear);
                }
            }

            #region 讀取學生清單
            btnPrevious.Enabled = false;
            btnCacluate.Enabled = false;

            _StudentFilterList = new List<StudentRecord>();
            BackgroundWorker bkw = new BackgroundWorker();
            bkw.WorkerReportsProgress = true;
            Exception bkwException = null;
            DataTable deptDataTable = new DataTable();

            bkw.ProgressChanged += delegate (object obj, ProgressChangedEventArgs e1)
            {
                MotherForm.SetStatusBarMessage("讀取學生清單", e1.ProgressPercentage);
            };

            bkw.DoWork += delegate
            {
                try
                {
                    bkw.ReportProgress(1);
                    foreach (int gradeYear in _GradeYearList)
                    {
                        _StudentFilterList.AddRange(_StudentList.Where(x => x.Class.GradeYear == gradeYear).ToList());
                    }

                    bkw.ReportProgress(50);
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

                    bkw.ReportProgress(70);

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

                    QueryHelper queryHelper = new QueryHelper();
                    deptDataTable = queryHelper.Select(queryDeptSQL);

                    #endregion

                    bkw.ReportProgress(100);
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
                    throw new Exception("學生列表讀取失敗", bkwException);
                }
                if (_StudentFilterList.Count == 0)
                {
                    MessageBox.Show("沒有找到符合條件的學生");
                    btnPrevious.Enabled = true;
                    return;
                }

                #region 整理學生基本資料
                var studentViewList = (from student in _StudentFilterList
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

                #region 將資料填入dataGridView
                List<DataGridViewRow> rowList = new List<DataGridViewRow>();
                foreach (var student in studentViewList)
                {
                    string tag1 = "", tag2 = "";
                    DataGridViewRow row = new DataGridViewRow();
                    row.CreateCells(dgvStudentList);
                    row.Tag = student.studentID;
                    row.Cells[0].Value = student.studentClass;
                    row.Cells[1].Value = student.studentSeatNo;
                    row.Cells[2].Value = student.studentNumber;
                    row.Cells[3].Value = student.studentName;
                    row.Cells[4].Value = student.RankGradeYear;
                    DataRow[] dataRow = deptDataTable.Select("student_id = '" + student.studentID + "'");
                    row.Cells[5].Value = dataRow.First()["deptname"];
                    row.Cells[6].Value = student.RankClassName;
                    if (tag1Student.Where(x => x.RefStudentID == student.studentID).Count() > 0)
                    {
                        tag1 = tag1Student.First(x => x.RefStudentID == student.studentID).Name;
                        row.Cells[7].Value = tag1;
                    }
                    if (tag2Student.Where(x => x.RefStudentID == student.studentID).Count() > 0)
                    {
                        tag2 = tag2Student.First(x => x.RefStudentID == student.studentID).Name;
                        row.Cells[8].Value = tag2;
                    }

                    rowList.Add(row);
                }

                dgvStudentList.Rows.AddRange(rowList.ToArray());
                #endregion

                btnPrevious.Enabled = true;
                btnCacluate.Enabled = true;
                MotherForm.SetStatusBarMessage("學生列表讀取完成");
            };

            bkw.RunWorkerAsync();
            #endregion
        }

        private void btnPrevious_Click(object sender, EventArgs e)
        {
            plStudentView.Visible = false;
            btnCacluate.Enabled = true;
            if (dgvStudentList.Rows.Count > 0)
            {
                dgvStudentList.Rows.Clear();
            }
        }

        private void btnCacluate_Click(object sender, EventArgs e)
        {

            // 學生依年級分批使用
            Dictionary<string, List<string>> gradeStudentDict = new Dictionary<string, List<string>>();


            //        #region 產生學生清單的SQL
            //        List<string> studentSqlList = new List<string>();
            //        foreach (DataGridViewRow row in dgvStudentList.Rows)
            //        {
            //            #region 單筆學生資料的SQL
            //            string studentSql = @"
            //SELECT
            //    '" + row.Tag + @"'::BIGINT AS student_id
            //    , '" + "" + row.Cells[3].Value + @"'::TEXT AS student_name
            //    , '" + ("" + row.Cells[4].Value).Trim('年', '級') + @"'::INT AS rank_grade_year
            //    , '" + "" + row.Cells[5].Value + @"'::TEXT AS rank_dept_name
            //    , '" + "" + row.Cells[6].Value + @"'::TEXT AS rank_class_name
            //    , '" + "" + row.Cells[7].Value + @"'::TEXT AS rank_tag1
            //    , '" + "" + row.Cells[8].Value + @"'::TEXT AS rank_tag2
            //";
            //            #endregion
            //            //把單筆學生資料的SQL加入到List
            //            studentSqlList.Add(studentSql);
            //        }

            #region 依年級產生學生清單的SQL
            foreach (DataGridViewRow row in dgvStudentList.Rows)
            {
                string gr = row.Cells[4].Value + "";
                #region 單筆學生資料的SQL
                string studentSql = @"
SELECT
'" + row.Tag + @"'::BIGINT AS student_id
, '" + "" + row.Cells[3].Value + @"'::TEXT AS student_name
, '" + ("" + row.Cells[4].Value).Trim('年', '級') + @"'::INT AS rank_grade_year
, '" + "" + row.Cells[5].Value + @"'::TEXT AS rank_dept_name
, '" + "" + row.Cells[6].Value + @"'::TEXT AS rank_class_name
, '" + "" + row.Cells[7].Value + @"'::TEXT AS rank_tag1
, '" + "" + row.Cells[8].Value + @"'::TEXT AS rank_tag2
";
                #endregion

                if (!gradeStudentDict.ContainsKey(gr))
                    gradeStudentDict.Add(gr, new List<string>());

                gradeStudentDict[gr].Add(studentSql);
            }
            #endregion


            btnCacluate.Enabled = false;
            btnPrevious.Enabled = false;
            string schoolYear = lbSchoolYear.Text;
            string semester = lbSemester.Text;
            string studentFilter = cboStudentFilter.Text.Trim('[', ']');
            string tag1 = cboStudentTag1.Text.Trim('[', ']'); ;
            string tag2 = cboStudentTag2.Text.Trim('[', ']'); ;
            string calculationSetting = "";



            BackgroundWorker bkw = new BackgroundWorker();
            bkw.WorkerReportsProgress = true;
            Exception bkwException = null;
            pbLoading.Visible = true;

            bkw.ProgressChanged += delegate (object obj, ProgressChangedEventArgs eventArgs)
            {
                MotherForm.SetStatusBarMessage("計算排名中", eventArgs.ProgressPercentage);
            };

            bkw.DoWork += delegate
            {
                try
                {
                    bkw.ReportProgress(1);

                    QueryHelper queryHelper = new QueryHelper();

                    int pr = 20;

                    // 依年級分批計算
                    foreach (string gr in gradeStudentDict.Keys)
                    {

                        #region 有學生資料的SQL
                        string studentListSql = @"
WITH student_list AS
(
    " + string.Join(@"
    UNION ALL", gradeStudentDict[gr]) + @"
)
";
                        #endregion


                        #region 產生計算規則的SQL
                        #region 產生要儲存到rank_batch的setting的Xml
                        XmlDocument xdoc = new XmlDocument();
                        var settingEle = xdoc.CreateElement("Setting");
                        settingEle.SetAttribute("學年度", schoolYear);
                        settingEle.SetAttribute("學期", semester);
                        settingEle.SetAttribute("考試名稱", "學期成績");
                        settingEle.SetAttribute("不排名學生類別", studentFilter);
                        settingEle.SetAttribute("類別一", tag1);
                        settingEle.SetAttribute("類別二", tag2);

                        var gradeYearEle = xdoc.CreateElement("年級");
                        gradeYearEle.InnerText = "" + gr.Trim('年', '級');
                        settingEle.AppendChild(gradeYearEle);

                        calculationSetting = settingEle.OuterXml;
                        #endregion

                        List<string> calcConditionListSQL = new List<string>();

                        #region 單筆計算規則的SQL
                        string calcCondition = @"
    SELECT
        '" + gr.Trim('年', '級') + @"'::TEXT AS rank_grade_year
        , '" + schoolYear + @"'::TEXT AS rank_school_year
        , '" + semester + @"'::TEXT AS rank_semester
        , '-1'::TEXT AS ref_exam_id
        , '學期成績'::TEXT AS rank_exam_name
        , '" + calculationSetting + @"'::TEXT AS calculation_setting
";
                        #endregion
                        //將單筆計算規則的SQL加到List
                        calcConditionListSQL.Add(calcCondition);


                        //將計算規則的List拆開
                        #region 計算規則的SQL
                        string calcConditionSQL = @"
, calc_condition AS
(
    " + string.Join(@"
    UNION ALL", calcConditionListSQL) + @"
)
";
                        #endregion
                        #endregion


                        #region 計算排名SQL
                        string insertRankSql = @"
" + studentListSql + @"
" + calcConditionSQL + @"
, subject_score AS
(
	SELECT
		calc_condition.rank_school_year
		, calc_condition.rank_semester
		, student_list.student_id
		, student_list.student_name
		, student_list.rank_grade_year
		, student_list.rank_dept_name
		, student_list.rank_class_name
		, student_list.rank_tag1
		, student_list.rank_tag2
		, calc_condition.ref_exam_id::BIGINT AS exam_id
		, array_to_string(xpath('/root/Subject/@科目', xmlparse(content  concat('<root>', subj_score_ele , '</root>'))), '')::TEXT As subject
		, NULLIF(array_to_string(xpath('/root/Subject/@原始成績', xmlparse(content concat('<root>', subj_score_ele , '</root>'))), ''),'')::DECIMAL As subject_origin_score
		, GREATEST
		(
			NULLIF(array_to_string(xpath('/root/Subject/@原始成績', xmlparse(content concat('<root>', subj_score_ele , '</root>'))), ''), '')::DECIMAL
			, NULLIF(array_to_string(xpath('/root/Subject/@學年調整成績', xmlparse(content concat('<root>', subj_score_ele , '</root>'))), ''), '')::DECIMAL
			, NULLIF(array_to_string(xpath('/root/Subject/@擇優採計成績', xmlparse(content concat('<root>', subj_score_ele , '</root>'))), ''), '')::DECIMAL
			, NULLIF(array_to_string(xpath('/root/Subject/@補考成績', xmlparse(content concat('<root>', subj_score_ele , '</root>'))), ''), '')::DECIMAL
			, NULLIF(array_to_string(xpath('/root/Subject/@重修成績', xmlparse(content concat('<root>', subj_score_ele , '</root>'))), ''), '')::DECIMAL
		) AS subject_greatest_score
	FROM 
	(
		SELECT 
			sems_subj_score.*
			, unnest(xpath('/root/SemesterSubjectScoreInfo/Subject', xmlparse(content concat('<root>', score_info , '</root>') ))) AS subj_score_ele
		FROM 
			sems_subj_score
	) AS sems_subj_score_ext
	INNER JOIN student_list
		ON sems_subj_score_ext.ref_student_id = student_list.student_id
	INNER JOIN calc_condition
		ON sems_subj_score_ext.school_year = calc_condition.rank_school_year::INT
		AND sems_subj_score_ext.semester = calc_condition.rank_semester::INT
		AND student_list.rank_grade_year = calc_condition.rank_grade_year::INT
)
, subj_origin_rank AS
(
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_dept_name
		, rank_class_name
		, exam_id
		, subject::TEXT AS item_name
		, student_id
		, rank_tag1
		, rank_tag2
		, subject_origin_score AS origin_score
		, RANK() OVER(PARTITION BY rank_grade_year, subject ORDER BY subject_origin_score DESC) AS grade_origin_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name, subject ORDER BY subject_origin_score DESC) AS dept_origin_rank
		, RANK() OVER(PARTITION BY rank_class_name, subject ORDER BY subject_origin_score DESC) AS class_origin_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag1, subject ORDER BY subject_origin_score DESC) AS tag1_origin_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag2, subject ORDER BY subject_origin_score DESC) AS tag2_origin_rank
		, RANK() OVER(PARTITION BY rank_grade_year, subject ORDER BY subject_origin_score ASC) AS grade_origin_rank_reverse
		, RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name, subject ORDER BY subject_origin_score ASC) AS dept_origin_rank_reverse
		, RANK() OVER(PARTITION BY rank_class_name, subject ORDER BY subject_origin_score ASC) AS class_origin_rank_reverse
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag1, subject ORDER BY subject_origin_score ASC) AS tag1_origin_rank_reverse
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag2, subject ORDER BY subject_origin_score ASC) AS tag2_origin_rank_reverse
		, COUNT(student_id) OVER(PARTITION BY rank_grade_year, subject) AS grade_count
		, COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_dept_name, subject) AS dept_count
		, COUNT(student_id) OVER(PARTITION BY rank_class_name, subject) AS class_count
		, COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_tag1, subject) AS tag1_count
		, COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_tag2, subject) AS tag2_count
	FROM
		subject_score
	WHERE
		subject IS NOT NULL
		AND subject_origin_score IS NOT NULL
)
, subj_greatest_rank AS
(
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_dept_name
		, rank_class_name
		, exam_id
		, subject::TEXT AS item_name
		, student_id
		, rank_tag1
		, rank_tag2
		, subject_greatest_score AS greatest_score
		, RANK() OVER(PARTITION BY rank_grade_year, subject ORDER BY subject_greatest_score DESC) AS grade_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name, subject ORDER BY subject_greatest_score DESC) AS dept_rank
		, RANK() OVER(PARTITION BY rank_class_name, subject ORDER BY subject_greatest_score DESC) AS class_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag1, subject ORDER BY subject_greatest_score DESC) AS tag1_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag2, subject ORDER BY subject_greatest_score DESC) AS tag2_rank
		, RANK() OVER(PARTITION BY rank_grade_year, subject ORDER BY subject_greatest_score ASC) AS grade_rank_reverse
		, RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name, subject ORDER BY subject_greatest_score ASC) AS dept_rank_reverse
		, RANK() OVER(PARTITION BY rank_class_name, subject ORDER BY subject_greatest_score ASC) AS class_rank_reverse
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag1, subject ORDER BY subject_greatest_score ASC) AS tag1_rank_reverse
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag2, subject ORDER BY subject_greatest_score ASC) AS tag2_rank_reverse
		, COUNT(student_id) OVER(PARTITION BY rank_grade_year, subject) AS grade_count
		, COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_dept_name, subject) AS dept_count
		, COUNT(student_id) OVER(PARTITION BY rank_class_name, subject) AS class_count
		, COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_tag1, subject) AS tag1_count
		, COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_tag2, subject) AS tag2_count
	FROM
		subject_score
	WHERE
		subject IS NOT NULL
		AND subject_greatest_score IS NOT NULL
)
, subj_origin_rank_expand AS
(
	SELECT
		subj_origin_rank.*
		, FLOOR((grade_origin_rank::DECIMAL - 1)*100::DECIMAL / grade_count) + 1 AS graderank_origin_percentage
		, FLOOR((dept_origin_rank::DECIMAL - 1)*100::DECIMAL / dept_count) + 1 AS deptrank_origin_percentage
		, FLOOR((class_origin_rank::DECIMAL - 1)*100::DECIMAL / class_count) + 1 AS classrank_origin_percentage
		, FLOOR((tag1_origin_rank::DECIMAL - 1)*100::DECIMAL / tag1_count) + 1 AS tag1rank_origin_percentage
		, FLOOR((tag2_origin_rank::DECIMAL - 1)*100::DECIMAL / tag2_count) + 1 AS tag2rank_origin_percentage
		, FLOOR((grade_origin_rank_reverse::DECIMAL - 1)*100::DECIMAL / grade_count)  AS graderank_origin_pr
		, FLOOR((dept_origin_rank_reverse::DECIMAL - 1)*100::DECIMAL / dept_count)  AS deptrank_origin_pr
		, FLOOR((class_origin_rank_reverse::DECIMAL - 1)*100::DECIMAL / class_count)  AS classrank_origin_pr
		, FLOOR((tag1_origin_rank_reverse::DECIMAL - 1)*100::DECIMAL / tag1_count)  AS tag1rank_origin_pr
		, FLOOR((tag2_origin_rank_reverse::DECIMAL - 1)*100::DECIMAL / tag2_count)  AS tag2rank_origin_pr
	FROM
		subj_origin_rank
)
, subj_greatest_rank_expand AS
(
	SELECT
		subj_greatest_rank.*
		, FLOOR((grade_rank::DECIMAL - 1)*100::DECIMAL / grade_count) + 1 AS graderank_percentage
		, FLOOR((dept_rank::DECIMAL - 1)*100::DECIMAL / dept_count) + 1 AS deptrank_percentage
		, FLOOR((class_rank::DECIMAL - 1)*100::DECIMAL / class_count) + 1 AS classrank_percentage
		, FLOOR((tag1_rank::DECIMAL - 1)*100::DECIMAL / tag1_count) + 1 AS tag1rank_percentage
		, FLOOR((tag2_rank::DECIMAL - 1)*100::DECIMAL / tag2_count) + 1 AS tag2rank_percentage
		, FLOOR((grade_rank_reverse::DECIMAL - 1)*100::DECIMAL / grade_count) AS graderank_pr
		, FLOOR((dept_rank_reverse::DECIMAL - 1)*100::DECIMAL / dept_count) AS deptrank_pr
		, FLOOR((class_rank_reverse::DECIMAL - 1)*100::DECIMAL / class_count) AS classrank_pr
		, FLOOR((tag1_rank_reverse::DECIMAL - 1)*100::DECIMAL / tag1_count) AS tag1rank_pr
		, FLOOR((tag2_rank_reverse::DECIMAL - 1)*100::DECIMAL / tag2_count) AS tag2rank_pr
	FROM
		subj_greatest_rank
)
, entry_score_list AS
(
	SELECT
		calc_condition.rank_school_year
		, calc_condition.rank_semester
		, student_list.student_id
		, student_list.student_name
		, student_list.rank_grade_year
		, student_list.rank_dept_name
		, student_list.rank_class_name
		, student_list.rank_tag1
		, student_list.rank_tag2
		, calc_condition.ref_exam_id::BIGINT AS exam_id
		, array_to_string(xpath('/root/Entry/@分項', xmlparse(content  concat('<root>', entry_score_ele , '</root>'))),'')::TEXT As entry
		, NULLIF(array_to_string(xpath('/root/Entry/@成績', xmlparse(content   concat('<root>', entry_score_ele , '</root>'))), ''),'')::DECIMAL AS entry_score
	FROM
	(
		SELECT
			sems_entry_score.*
			, unnest(xpath('/root/SemesterEntryScore/Entry', xmlparse(content  concat('<root>', sems_entry_score.score_info , '</root>') ))) As entry_score_ele
		FROM
		sems_entry_score
	) AS sems_entry_score_ext
	INNER JOIN student_list
		ON sems_entry_score_ext.ref_student_id = student_list.student_id
	INNER JOIN calc_condition
		ON sems_entry_score_ext.school_year = calc_condition.rank_school_year::INT
		AND sems_entry_score_ext.semester = calc_condition.rank_semester::INT
		AND sems_entry_score_ext.grade_year = calc_condition.rank_grade_year::INT
)
, entry_rank_list AS
(
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_dept_name
		, rank_class_name
		, exam_id
		, entry::TEXT AS item_name
		, student_id
		, rank_tag1
		, rank_tag2
		, entry_score
		, RANK() OVER(PARTITION BY rank_grade_year, entry ORDER BY entry_score DESC) AS grade_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name, entry ORDER BY entry_score DESC) AS dept_rank
		, RANK() OVER(PARTITION BY rank_class_name, entry ORDER BY entry_score DESC) AS class_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag1, entry ORDER BY entry_score DESC) AS tag1_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag2, entry ORDER BY entry_score DESC) AS tag2_rank
		, RANK() OVER(PARTITION BY rank_grade_year, entry ORDER BY entry_score ASC) AS grade_rank_reverse
		, RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name, entry ORDER BY entry_score ASC) AS dept_rank_reverse
		, RANK() OVER(PARTITION BY rank_class_name, entry ORDER BY entry_score ASC) AS class_rank_reverse
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag1, entry ORDER BY entry_score ASC) AS tag1_rank_reverse
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag2, entry ORDER BY entry_score ASC) AS tag2_rank_reverse
		, COUNT(student_id) OVER(PARTITION BY rank_grade_year, entry) AS grade_count
		, COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_dept_name, entry) AS dept_count
		, COUNT(student_id) OVER(PARTITION BY rank_class_name, entry) AS class_count
		, COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_tag1, entry) AS tag1_count
		, COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_tag2, entry) AS tag2_count
	FROM
		entry_score_list
	WHERE
		entry IS NOT NULL
		AND entry_score IS NOT NULL
)
, entry_rank_expand AS
(
	SELECT
		entry_rank_list.*
		, FLOOR((grade_rank::DECIMAL - 1)*100::DECIMAL / grade_count) + 1 AS graderank_percentage
		, FLOOR((dept_rank::DECIMAL - 1)*100::DECIMAL / dept_count) + 1 AS deptrank_percentage
		, FLOOR((class_rank::DECIMAL - 1)*100::DECIMAL / class_count) + 1 AS classrank_percentage
		, FLOOR((tag1_rank::DECIMAL - 1)*100::DECIMAL / tag1_count) + 1 AS tag1rank_percentage
		, FLOOR((tag2_rank::DECIMAL - 1)*100::DECIMAL / tag2_count) + 1 AS tag2rank_percentage
        , FLOOR((grade_rank_reverse::DECIMAL - 1)*100::DECIMAL / grade_count)  AS graderank_pr
        , FLOOR((dept_rank_reverse::DECIMAL - 1)*100::DECIMAL / dept_count)  AS deptrank_pr
        , FLOOR((class_rank_reverse::DECIMAL - 1)*100::DECIMAL / class_count)  AS classrank_pr
        , FLOOR((tag1_rank_reverse::DECIMAL - 1)*100::DECIMAL / tag1_count)  AS tag1rank_pr
        , FLOOR((tag2_rank_reverse::DECIMAL - 1)*100::DECIMAL / tag2_count)  AS tag2rank_pr
	FROM
		entry_rank_list
)
, score_list AS
(
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, '學期/科目成績(原始)'::TEXT AS item_type
		, exam_id AS ref_exam_id
		, item_name
		, '年排名'::TEXT AS rank_type
		, '' || rank_grade_year || '年級'::TEXT AS rank_name
		, true AS is_alive
		, grade_count AS matrix_count
		, AVG(origin_score::DECIMAL) FILTER(WHERE grade_origin_rank *4 <= grade_count ) OVER(PARTITION BY rank_grade_year, item_name) AS avg_top_25
		, AVG(origin_score::DECIMAL) FILTER(WHERE grade_origin_rank *2 <= grade_count ) OVER(PARTITION BY rank_grade_year, item_name) AS avg_top_50
		, AVG(origin_score::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS avg
		, AVG(origin_score::DECIMAL) FILTER(WHERE (grade_count - grade_origin_rank + 1) *2 <= grade_count ) OVER(PARTITION BY rank_grade_year, item_name) AS avg_bottom_50
		, AVG(origin_score::DECIMAL) FILTER(WHERE (grade_count - grade_origin_rank + 1) *4 <= grade_count ) OVER(PARTITION BY rank_grade_year, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL<=origin_score) OVER(PARTITION BY rank_grade_year, item_name)AS level_gte100 
		, COUNT(*) FILTER(WHERE 90::DECIMAL<=origin_score AND origin_score <100::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL<=origin_score AND origin_score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL<=origin_score AND origin_score <80::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL<=origin_score AND origin_score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL<=origin_score AND origin_score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL<=origin_score AND origin_score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL<=origin_score AND origin_score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL<=origin_score AND origin_score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL<=origin_score AND origin_score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_10
		, COUNT(*) FILTER(WHERE origin_score<10::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_lt10 
		, student_id
		, origin_score AS score
		, grade_origin_rank AS rank
        , graderank_origin_pr AS pr
		, graderank_origin_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		subj_origin_rank_expand

	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, '學期/科目成績(原始)'::TEXT AS item_type
		, exam_id AS ref_exam_id
		, item_name
		, '科排名'::TEXT AS rank_type
		, rank_dept_name AS rank_name
		, true AS is_alive
		, dept_count AS matrix_count
		, AVG(origin_score::DECIMAL) FILTER(WHERE dept_origin_rank * 4 <= dept_count ) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg_top_25
		, AVG(origin_score::DECIMAL) FILTER(WHERE dept_origin_rank * 2 <= dept_count ) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg_top_50
		, AVG(origin_score::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg
		, AVG(origin_score::DECIMAL) FILTER(WHERE ( dept_count - dept_origin_rank +1) * 2 <= dept_count ) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg_bottom_50
		, AVG(origin_score::DECIMAL) FILTER(WHERE ( dept_count - dept_origin_rank +1) * 4 <= dept_count ) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL<=origin_score) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_gte100 
		, COUNT(*) FILTER(WHERE 90::DECIMAL<=origin_score AND origin_score <100::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL<=origin_score AND origin_score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL<=origin_score AND origin_score <80::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL<=origin_score AND origin_score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL<=origin_score AND origin_score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL<=origin_score AND origin_score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL<=origin_score AND origin_score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL<=origin_score AND origin_score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL<=origin_score AND origin_score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_10
		, COUNT(*) FILTER(WHERE origin_score<10::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_lt10 
		, student_id
		, origin_score AS score
		, dept_origin_rank AS rank
        , deptrank_origin_pr AS pr
		, deptrank_origin_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		subj_origin_rank_expand

	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, '學期/科目成績(原始)'::TEXT AS item_type
		, exam_id AS ref_exam_id
		, item_name
		, '班排名'::TEXT AS rank_type
		, rank_class_name AS rank_name
		, true AS is_alive
		, class_count AS matrix_count
		, AVG(origin_score::DECIMAL) FILTER(WHERE class_origin_rank *4 <=  class_count ) OVER(PARTITION BY rank_class_name, item_name) AS avg_top_25
		, AVG(origin_score::DECIMAL) FILTER(WHERE class_origin_rank *2 <=  class_count ) OVER(PARTITION BY rank_class_name, item_name) AS avg_top_50
		, AVG(origin_score::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS avg
		, AVG(origin_score::DECIMAL) FILTER(WHERE (class_count -class_origin_rank +1) *2 <= class_count ) OVER(PARTITION BY rank_class_name, item_name) AS avg_bottom_50
		, AVG(origin_score::DECIMAL) FILTER(WHERE (class_count -class_origin_rank +1) *4 <= class_count ) OVER(PARTITION BY rank_class_name, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL<=origin_score) OVER(PARTITION BY rank_class_name, item_name)AS level_gte100 
		, COUNT(*) FILTER(WHERE 90::DECIMAL<=origin_score AND origin_score <100::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL<=origin_score AND origin_score <90::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL<=origin_score AND origin_score <80::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL<=origin_score AND origin_score <70::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL<=origin_score AND origin_score <60::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL<=origin_score AND origin_score <50::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL<=origin_score AND origin_score <40::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL<=origin_score AND origin_score <30::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL<=origin_score AND origin_score <20::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_10
		, COUNT(*) FILTER(WHERE origin_score<10::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_lt10 
		, student_id
		, origin_score AS score
		, class_origin_rank AS rank
        , classrank_origin_pr AS pr
		, classrank_origin_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		subj_origin_rank_expand

	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, '學期/科目成績(原始)'::TEXT AS item_type
		, exam_id AS ref_exam_id
		, item_name
		, '類別1排名'::TEXT AS rank_type
		, rank_tag1 AS rank_name
		, true AS is_alive
		, tag1_count AS matrix_count
		, AVG(origin_score::DECIMAL) FILTER(WHERE tag1_origin_rank *4 <= tag1_count) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_top_25
		, AVG(origin_score::DECIMAL) FILTER(WHERE tag1_origin_rank *2 <= tag1_count) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_top_50
		, AVG(origin_score::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg
		, AVG(origin_score::DECIMAL) FILTER(WHERE (tag1_count -tag1_origin_rank +1 ) *2 <= tag1_count ) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_bottom_50
		, AVG(origin_score::DECIMAL) FILTER(WHERE (tag1_count -tag1_origin_rank +1 ) *4 <= tag1_count ) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL<=origin_score) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)AS level_gte100 
		, COUNT(*) FILTER(WHERE 90::DECIMAL<=origin_score AND origin_score <100::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL<=origin_score AND origin_score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL<=origin_score AND origin_score <80::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL<=origin_score AND origin_score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL<=origin_score AND origin_score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL<=origin_score AND origin_score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL<=origin_score AND origin_score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL<=origin_score AND origin_score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL<=origin_score AND origin_score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_10
		, COUNT(*) FILTER(WHERE origin_score<10::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_lt10 
		, student_id
		, origin_score AS score
		, tag1_origin_rank AS rank
        , tag1rank_origin_pr AS pr
		, tag1rank_origin_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		subj_origin_rank_expand
	WHERE
		rank_tag1 IS NOT NULL
		AND rank_tag1 <> ''

	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, '學期/科目成績(原始)'::TEXT AS item_type
		, exam_id AS ref_exam_id
		, item_name
		, '類別2排名'::TEXT AS rank_type
		, rank_tag2 AS rank_name
		, true AS is_alive
		, tag2_count AS matrix_count
		, AVG(origin_score::DECIMAL) FILTER(WHERE tag2_origin_rank *4 <= tag2_count) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_top_25
		, AVG(origin_score::DECIMAL) FILTER(WHERE tag2_origin_rank *2 <= tag2_count) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_top_50
		, AVG(origin_score::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg
		, AVG(origin_score::DECIMAL) FILTER(WHERE ( tag2_count - tag2_origin_rank +1) *2 <= tag2_count ) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_bottom_50
		, AVG(origin_score::DECIMAL) FILTER(WHERE ( tag2_count - tag2_origin_rank +1) *4 <= tag2_count ) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL<=origin_score) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)AS level_gte100 
		, COUNT(*) FILTER(WHERE 90::DECIMAL<=origin_score AND origin_score <100::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL<=origin_score AND origin_score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL<=origin_score AND origin_score <80::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL<=origin_score AND origin_score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL<=origin_score AND origin_score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL<=origin_score AND origin_score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL<=origin_score AND origin_score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL<=origin_score AND origin_score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL<=origin_score AND origin_score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_10
		, COUNT(*) FILTER(WHERE origin_score<10::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_lt10 
		, student_id
		, origin_score AS score
		, tag2_origin_rank AS rank
        , tag2rank_origin_pr AS pr
		, tag2rank_origin_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		subj_origin_rank_expand
	WHERE
		rank_tag2 IS NOT NULL
		AND rank_tag2 <> ''

	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, '學期/科目成績'::TEXT AS item_type
		, exam_id AS ref_exam_id
		, item_name
		, '年排名'::TEXT AS rank_type
		, '' || rank_grade_year || '年級'::TEXT AS rank_name
		, true AS is_alive
		, grade_count AS matrix_count
		, AVG(greatest_score::DECIMAL) FILTER(WHERE grade_rank * 4 <= grade_count ) OVER(PARTITION BY rank_grade_year, item_name) AS avg_top_25
		, AVG(greatest_score::DECIMAL) FILTER(WHERE grade_rank * 2 <= grade_count ) OVER(PARTITION BY rank_grade_year, item_name) AS avg_top_50
		, AVG(greatest_score::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS avg
		, AVG(greatest_score::DECIMAL) FILTER(WHERE (grade_count - grade_rank +1) * 2 <= grade_count) OVER(PARTITION BY rank_grade_year, item_name) AS avg_bottom_50
		, AVG(greatest_score::DECIMAL) FILTER(WHERE (grade_count - grade_rank +1) * 4 <= grade_count) OVER(PARTITION BY rank_grade_year, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL<=greatest_score) OVER(PARTITION BY rank_grade_year, item_name)AS level_gte100 
		, COUNT(*) FILTER(WHERE 90::DECIMAL<=greatest_score AND greatest_score <100::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL<=greatest_score AND greatest_score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL<=greatest_score AND greatest_score <80::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL<=greatest_score AND greatest_score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL<=greatest_score AND greatest_score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL<=greatest_score AND greatest_score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL<=greatest_score AND greatest_score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL<=greatest_score AND greatest_score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL<=greatest_score AND greatest_score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_10
		, COUNT(*) FILTER(WHERE greatest_score<10::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_lt10 
		, student_id
		, greatest_score AS score
		, grade_rank AS rank
        , graderank_pr AS pr
		, graderank_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		subj_greatest_rank_expand
	
	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, '學期/科目成績'::TEXT AS item_type
		, exam_id AS ref_exam_id
		, item_name
		, '科排名'::TEXT AS rank_type
		, rank_dept_name AS rank_name
		, true AS is_alive
		, dept_count AS matrix_count
		, AVG(greatest_score::DECIMAL) FILTER(WHERE dept_rank * 4 <= dept_count ) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg_top_25
		, AVG(greatest_score::DECIMAL) FILTER(WHERE dept_rank * 2 <= dept_count ) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg_top_50
		, AVG(greatest_score::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg
		, AVG(greatest_score::DECIMAL) FILTER(WHERE (dept_count - dept_rank + 1) * 2 <= dept_count ) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg_bottom_50
		, AVG(greatest_score::DECIMAL) FILTER(WHERE (dept_count - dept_rank + 1) * 4 <= dept_count ) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL<=greatest_score) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)AS level_gte100 
		, COUNT(*) FILTER(WHERE 90::DECIMAL<=greatest_score AND greatest_score <100::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL<=greatest_score AND greatest_score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL<=greatest_score AND greatest_score <80::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL<=greatest_score AND greatest_score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL<=greatest_score AND greatest_score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL<=greatest_score AND greatest_score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL<=greatest_score AND greatest_score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL<=greatest_score AND greatest_score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL<=greatest_score AND greatest_score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_10
		, COUNT(*) FILTER(WHERE greatest_score<10::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_lt10 
		, student_id
		, greatest_score AS score
		, dept_rank AS rank
        , deptrank_pr AS pr
		, deptrank_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		subj_greatest_rank_expand

	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, '學期/科目成績'::TEXT AS item_type
		, exam_id AS ref_exam_id
		, item_name
		, '班排名'::TEXT AS rank_type
		, rank_class_name AS rank_name
		, true AS is_alive
		, class_count AS matrix_count
		, AVG(greatest_score::DECIMAL) FILTER(WHERE class_rank *4 <= class_count ) OVER(PARTITION BY rank_class_name, item_name) AS avg_top_25
		, AVG(greatest_score::DECIMAL) FILTER(WHERE class_rank *2 <= class_count ) OVER(PARTITION BY rank_class_name, item_name) AS avg_top_50
		, AVG(greatest_score::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS avg
		, AVG(greatest_score::DECIMAL) FILTER(WHERE (class_count - class_rank +1) * 2  <= class_count ) OVER(PARTITION BY rank_class_name, item_name) AS avg_bottom_50
		, AVG(greatest_score::DECIMAL) FILTER(WHERE (class_count - class_rank +1) * 4  <= class_count ) OVER(PARTITION BY rank_class_name, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL<=greatest_score) OVER(PARTITION BY rank_class_name, item_name)AS level_gte100 
		, COUNT(*) FILTER(WHERE 90::DECIMAL<=greatest_score AND greatest_score <100::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL<=greatest_score AND greatest_score <90::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL<=greatest_score AND greatest_score <80::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL<=greatest_score AND greatest_score <70::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL<=greatest_score AND greatest_score <60::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL<=greatest_score AND greatest_score <50::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL<=greatest_score AND greatest_score <40::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL<=greatest_score AND greatest_score <30::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL<=greatest_score AND greatest_score <20::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_10
		, COUNT(*) FILTER(WHERE greatest_score<10::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_lt10 
		, student_id
		, greatest_score AS score
		, class_rank AS rank
        , classrank_pr AS pr
		, classrank_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		subj_greatest_rank_expand

	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, '學期/科目成績'::TEXT AS item_type
		, exam_id AS ref_exam_id
		, item_name
		, '類別1排名'::TEXT AS rank_type
		, rank_tag1 AS rank_name
		, true AS is_alive
		, tag1_count AS matrix_count
		, AVG(greatest_score::DECIMAL) FILTER(WHERE tag1_rank * 4 <= tag1_count ) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_top_25
		, AVG(greatest_score::DECIMAL) FILTER(WHERE tag1_rank * 2 <= tag1_count ) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_top_50
		, AVG(greatest_score::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg
		, AVG(greatest_score::DECIMAL) FILTER(WHERE ( tag1_count - tag1_rank +1 ) *2 <= tag1_count ) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_bottom_50
		, AVG(greatest_score::DECIMAL) FILTER(WHERE ( tag1_count - tag1_rank +1 ) *4 <= tag1_count ) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL<=greatest_score) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)AS level_gte100 
		, COUNT(*) FILTER(WHERE 90::DECIMAL<=greatest_score AND greatest_score <100::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL<=greatest_score AND greatest_score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL<=greatest_score AND greatest_score <80::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL<=greatest_score AND greatest_score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL<=greatest_score AND greatest_score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL<=greatest_score AND greatest_score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL<=greatest_score AND greatest_score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL<=greatest_score AND greatest_score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL<=greatest_score AND greatest_score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_10
		, COUNT(*) FILTER(WHERE greatest_score<10::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_lt10 
		, student_id
		, greatest_score AS score
		, tag1_rank AS rank
        , tag1rank_pr AS pr
		, tag1rank_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		subj_greatest_rank_expand
	WHERE
		rank_tag1 IS NOT NULL
		AND rank_tag1 <> ''

	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, '學期/科目成績'::TEXT AS item_type
		, exam_id AS ref_exam_id
		, item_name
		, '類別2排名'::TEXT AS rank_type
		, rank_tag2 AS rank_name
		, true AS is_alive
		, tag2_count AS matrix_count
		, AVG(greatest_score::DECIMAL) FILTER(WHERE tag2_rank *4 <=  tag2_count) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_top_25
		, AVG(greatest_score::DECIMAL) FILTER(WHERE tag2_rank *2 <=  tag2_count) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_top_50
		, AVG(greatest_score::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg
		, AVG(greatest_score::DECIMAL) FILTER(WHERE (tag2_count - tag2_rank+1 ) <= tag2_count ) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_bottom_50
		, AVG(greatest_score::DECIMAL) FILTER(WHERE (tag2_count - tag2_rank+1 ) <= tag2_count ) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL<=greatest_score) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)AS level_gte100 
		, COUNT(*) FILTER(WHERE 90::DECIMAL<=greatest_score AND greatest_score <100::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL<=greatest_score AND greatest_score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL<=greatest_score AND greatest_score <80::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL<=greatest_score AND greatest_score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL<=greatest_score AND greatest_score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL<=greatest_score AND greatest_score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL<=greatest_score AND greatest_score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL<=greatest_score AND greatest_score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL<=greatest_score AND greatest_score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_10
		, COUNT(*) FILTER(WHERE greatest_score<10::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_lt10 
		, student_id
		, greatest_score AS score
		, tag2_rank AS rank
        , tag2rank_pr AS pr
		, tag2rank_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		subj_greatest_rank_expand
	WHERE
		rank_tag2 IS NOT NULL
		AND rank_tag2 <> ''

	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, '學期/分項成績(原始)'::TEXT AS item_type
		, exam_id AS ref_exam_id
		, REPLACE(item_name, '(原始)', '') AS item_name
		, '年排名'::TEXT AS rank_type
		, '' || rank_grade_year || '年級'::TEXT AS rank_name
		, true AS is_alive
		, grade_count AS matrix_count
		, AVG(entry_score::DECIMAL) FILTER(WHERE grade_rank *4  <= grade_count ) OVER(PARTITION BY rank_grade_year, item_name) AS avg_top_25
		, AVG(entry_score::DECIMAL) FILTER(WHERE grade_rank *2  <= grade_count ) OVER(PARTITION BY rank_grade_year, item_name) AS avg_top_50
		, AVG(entry_score::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS avg
		, AVG(entry_score::DECIMAL) FILTER(WHERE (grade_count -grade_rank +1 )*2 <= grade_count ) OVER(PARTITION BY rank_grade_year, item_name) AS avg_bottom_50
		, AVG(entry_score::DECIMAL) FILTER(WHERE (grade_count -grade_rank +1 )*4 <= grade_count ) OVER(PARTITION BY rank_grade_year, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL<=entry_score) OVER(PARTITION BY rank_grade_year, item_name)AS level_gte100 
		, COUNT(*) FILTER(WHERE 90::DECIMAL<=entry_score AND entry_score <100::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL<=entry_score AND entry_score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL<=entry_score AND entry_score <80::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL<=entry_score AND entry_score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL<=entry_score AND entry_score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL<=entry_score AND entry_score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL<=entry_score AND entry_score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL<=entry_score AND entry_score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL<=entry_score AND entry_score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_10
		, COUNT(*) FILTER(WHERE entry_score<10::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_lt10 
		, student_id
		, entry_score AS score
		, grade_rank AS rank
        , graderank_pr AS pr
		, graderank_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		entry_rank_expand
	WHERE
		item_name LIKE '%(原始)%'
	
	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, '學期/分項成績(原始)'::TEXT AS item_type
		, exam_id AS ref_exam_id
		, REPLACE(item_name, '(原始)', '') AS item_name
		, '科排名'::TEXT AS rank_type
		, rank_dept_name AS rank_name
		, true AS is_alive
		, dept_count AS matrix_count
		, AVG(entry_score::DECIMAL) FILTER(WHERE dept_rank * 4 <= dept_count ) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg_top_25
		, AVG(entry_score::DECIMAL) FILTER(WHERE dept_rank  *2 <= dept_count ) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg_top_50
		, AVG(entry_score::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg
		, AVG(entry_score::DECIMAL) FILTER(WHERE (dept_count -dept_rank +1 )*2 <= dept_count ) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg_bottom_50
		, AVG(entry_score::DECIMAL) FILTER(WHERE (dept_count- dept_rank +1 )*4 <= dept_count ) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL<=entry_score) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_gte100 
		, COUNT(*) FILTER(WHERE 90::DECIMAL<=entry_score AND entry_score <100::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL<=entry_score AND entry_score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL<=entry_score AND entry_score <80::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL<=entry_score AND entry_score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL<=entry_score AND entry_score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL<=entry_score AND entry_score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL<=entry_score AND entry_score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL<=entry_score AND entry_score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL<=entry_score AND entry_score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_10
		, COUNT(*) FILTER(WHERE entry_score<10::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_lt10 
		, student_id
		, entry_score AS score
		, dept_rank AS rank
        , deptrank_pr AS pr
		, deptrank_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		entry_rank_expand
	WHERE
		item_name LIKE '%(原始)%'
	
	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, '學期/分項成績(原始)'::TEXT AS item_type
		, exam_id AS ref_exam_id
		, REPLACE(item_name, '(原始)', '') AS item_name
		, '班排名'::TEXT AS rank_type
		, rank_class_name AS rank_name
		, true AS is_alive
		, class_count AS matrix_count
		, AVG(entry_score::DECIMAL) FILTER(WHERE  class_rank *4 <= class_count ) OVER(PARTITION BY rank_class_name, item_name) AS avg_top_25
		, AVG(entry_score::DECIMAL) FILTER(WHERE  class_rank *2 <= class_count ) OVER(PARTITION BY rank_class_name, item_name) AS avg_top_50
		, AVG(entry_score::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS avg
		, AVG(entry_score::DECIMAL) FILTER(WHERE (class_count -class_rank +1 ) *2 <= class_count ) OVER(PARTITION BY rank_class_name, item_name) AS avg_bottom_50
		, AVG(entry_score::DECIMAL) FILTER(WHERE (class_count -class_rank +1 ) *4 <= class_count ) OVER(PARTITION BY rank_class_name, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL<=entry_score) OVER(PARTITION BY rank_class_name, item_name)AS level_gte100 
		, COUNT(*) FILTER(WHERE 90::DECIMAL<=entry_score AND entry_score <100::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL<=entry_score AND entry_score <90::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL<=entry_score AND entry_score <80::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL<=entry_score AND entry_score <70::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL<=entry_score AND entry_score <60::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL<=entry_score AND entry_score <50::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL<=entry_score AND entry_score <40::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL<=entry_score AND entry_score <30::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL<=entry_score AND entry_score <20::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_10
		, COUNT(*) FILTER(WHERE entry_score<10::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_lt10 
		, student_id
		, entry_score AS score
		, class_rank AS rank
        , classrank_pr AS pr
		, classrank_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		entry_rank_expand
	WHERE
		item_name LIKE '%(原始)%'
	
	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, '學期/分項成績(原始)'::TEXT AS item_type
		, exam_id AS ref_exam_id
		, REPLACE(item_name, '(原始)', '') AS item_name
		, '類別1排名'::TEXT AS rank_type
		, rank_tag1 AS rank_name
		, true AS is_alive
		, tag1_count AS matrix_count
		, AVG(entry_score::DECIMAL) FILTER(WHERE tag1_rank *4 <= tag1_count ) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_top_25
		, AVG(entry_score::DECIMAL) FILTER(WHERE tag1_rank *2 <= tag1_count ) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_top_50
		, AVG(entry_score::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg
		, AVG(entry_score::DECIMAL) FILTER(WHERE (tag1_count - tag1_rank + 1 ) *2 <= tag1_count ) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_bottom_50
		, AVG(entry_score::DECIMAL) FILTER(WHERE (tag1_count - tag1_rank + 1 ) *4 <= tag1_count ) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL<=entry_score) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_gte100 
		, COUNT(*) FILTER(WHERE 90::DECIMAL<=entry_score AND entry_score <100::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL<=entry_score AND entry_score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL<=entry_score AND entry_score <80::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL<=entry_score AND entry_score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL<=entry_score AND entry_score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL<=entry_score AND entry_score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL<=entry_score AND entry_score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL<=entry_score AND entry_score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL<=entry_score AND entry_score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_10
		, COUNT(*) FILTER(WHERE entry_score<10::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_lt10 
		, student_id
		, entry_score AS score
		, tag1_rank AS rank
        , tag1rank_pr AS pr
		, tag1rank_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		entry_rank_expand
	WHERE
		item_name LIKE '%(原始)%'
		AND rank_tag1 IS NOT NULL
		AND rank_tag1 <> ''
	
	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, '學期/分項成績(原始)'::TEXT AS item_type
		, exam_id AS ref_exam_id
		, REPLACE(item_name, '(原始)', '') AS item_name
		, '類別2排名'::TEXT AS rank_type
		, rank_tag2 AS rank_name
		, true AS is_alive
		, tag2_count AS matrix_count
		, AVG(entry_score::DECIMAL) FILTER(WHERE tag2_rank  *4 <=  tag2_count) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_top_25
		, AVG(entry_score::DECIMAL) FILTER(WHERE tag2_rank  *2 <=  tag2_count) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_top_50
		, AVG(entry_score::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg
		, AVG(entry_score::DECIMAL) FILTER(WHERE (tag2_count -tag2_rank +1 ) *2 <= tag2_count ) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_bottom_50
		, AVG(entry_score::DECIMAL) FILTER(WHERE (tag2_count -tag2_rank +1 ) *4 <= tag2_count ) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL<=entry_score) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_gte100 
		, COUNT(*) FILTER(WHERE 90::DECIMAL<=entry_score AND entry_score <100::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL<=entry_score AND entry_score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL<=entry_score AND entry_score <80::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL<=entry_score AND entry_score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL<=entry_score AND entry_score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL<=entry_score AND entry_score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL<=entry_score AND entry_score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL<=entry_score AND entry_score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL<=entry_score AND entry_score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_10
		, COUNT(*) FILTER(WHERE entry_score<10::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_lt10 
		, student_id
		, entry_score AS score
		, tag2_rank AS rank
        , tag2rank_pr AS pr
		, tag2rank_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		entry_rank_expand
	WHERE
		item_name LIKE '%(原始)%'
		AND rank_tag2 IS NOT NULL
		AND rank_tag2 <> ''
	
	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, '學期/分項成績'::TEXT AS item_type
		, exam_id AS ref_exam_id
		, item_name
		, '年排名'::TEXT AS rank_type
		, '' || rank_grade_year || '年級'::TEXT AS rank_name
		, true AS is_alive
		, grade_count AS matrix_count
		, AVG(entry_score::DECIMAL) FILTER(WHERE grade_rank  *4 <=  grade_count ) OVER(PARTITION BY rank_grade_year, item_name) AS avg_top_25
		, AVG(entry_score::DECIMAL) FILTER(WHERE grade_rank  *2 <=  grade_count ) OVER(PARTITION BY rank_grade_year, item_name) AS avg_top_50
		, AVG(entry_score::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS avg
		, AVG(entry_score::DECIMAL) FILTER(WHERE ( grade_count -grade_rank +1 ) *2 <= grade_count ) OVER(PARTITION BY rank_grade_year, item_name) AS avg_bottom_50
		, AVG(entry_score::DECIMAL) FILTER(WHERE ( grade_count -grade_rank +1 ) *4 <= grade_count ) OVER(PARTITION BY rank_grade_year, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL<=entry_score) OVER(PARTITION BY rank_grade_year, item_name)AS level_gte100 
		, COUNT(*) FILTER(WHERE 90::DECIMAL<=entry_score AND entry_score <100::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL<=entry_score AND entry_score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL<=entry_score AND entry_score <80::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL<=entry_score AND entry_score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL<=entry_score AND entry_score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL<=entry_score AND entry_score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL<=entry_score AND entry_score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL<=entry_score AND entry_score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL<=entry_score AND entry_score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_10
		, COUNT(*) FILTER(WHERE entry_score<10::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_lt10 
		, student_id
		, entry_score AS score
		, grade_rank AS rank
        , graderank_pr AS pr
		, graderank_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		entry_rank_expand
	WHERE
		item_name NOT LIKE '%(原始)%'
	
	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, '學期/分項成績'::TEXT AS item_type
		, exam_id AS ref_exam_id
		, item_name
		, '科排名'::TEXT AS rank_type
		, rank_dept_name AS rank_name
		, true AS is_alive
		, dept_count AS matrix_count
		, AVG(entry_score::DECIMAL) FILTER(WHERE dept_rank *4 <= dept_count ) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg_top_25
		, AVG(entry_score::DECIMAL) FILTER(WHERE dept_rank *2 <= dept_count ) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg_top_50
		, AVG(entry_score::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg
		, AVG(entry_score::DECIMAL) FILTER(WHERE (dept_count -dept_rank +1 ) *2 <= dept_count ) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg_bottom_50
		, AVG(entry_score::DECIMAL) FILTER(WHERE (dept_count -dept_rank +1 ) *4 <= dept_count ) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL<=entry_score) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_gte100 
		, COUNT(*) FILTER(WHERE 90::DECIMAL<=entry_score AND entry_score <100::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL<=entry_score AND entry_score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL<=entry_score AND entry_score <80::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL<=entry_score AND entry_score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL<=entry_score AND entry_score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL<=entry_score AND entry_score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL<=entry_score AND entry_score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL<=entry_score AND entry_score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL<=entry_score AND entry_score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_10
		, COUNT(*) FILTER(WHERE entry_score<10::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_lt10 
		, student_id
		, entry_score AS score
		, dept_rank AS rank
        , deptrank_pr AS pr
		, deptrank_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		entry_rank_expand
	WHERE
		item_name NOT LIKE '%(原始)%'
	
	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, '學期/分項成績'::TEXT AS item_type
		, exam_id AS ref_exam_id
		, item_name
		, '班排名'::TEXT AS rank_type
		, rank_class_name AS rank_name
		, true AS is_alive
		, class_count AS matrix_count
		, AVG(entry_score::DECIMAL) FILTER(WHERE class_rank  *4 <=  class_count ) OVER(PARTITION BY rank_class_name, item_name) AS avg_top_25
		, AVG(entry_score::DECIMAL) FILTER(WHERE class_rank  *2 <=  class_count ) OVER(PARTITION BY rank_class_name, item_name) AS avg_top_50
		, AVG(entry_score::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS avg
		, AVG(entry_score::DECIMAL) FILTER(WHERE (class_count -class_rank +1 )*2 <= class_count ) OVER(PARTITION BY rank_class_name, item_name) AS avg_bottom_50
		, AVG(entry_score::DECIMAL) FILTER(WHERE (class_count -class_rank +1 )*4 <= class_count ) OVER(PARTITION BY rank_class_name, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL<=entry_score) OVER(PARTITION BY rank_class_name, item_name)AS level_gte100 
		, COUNT(*) FILTER(WHERE 90::DECIMAL<=entry_score AND entry_score <100::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL<=entry_score AND entry_score <90::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL<=entry_score AND entry_score <80::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL<=entry_score AND entry_score <70::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL<=entry_score AND entry_score <60::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL<=entry_score AND entry_score <50::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL<=entry_score AND entry_score <40::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL<=entry_score AND entry_score <30::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL<=entry_score AND entry_score <20::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_10
		, COUNT(*) FILTER(WHERE entry_score<10::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_lt10 
		, student_id
		, entry_score AS score
		, class_rank AS rank
        , classrank_pr AS pr
		, classrank_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		entry_rank_expand
	WHERE
		item_name NOT LIKE '%(原始)%'
	
	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, '學期/分項成績'::TEXT AS item_type
		, exam_id AS ref_exam_id
		, item_name
		, '類別1排名'::TEXT AS rank_type
		, rank_tag1 AS rank_name
		, true AS is_alive
		, tag1_count AS matrix_count
		, AVG(entry_score::DECIMAL) FILTER(WHERE tag1_rank *4 <= tag1_count ) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_top_25
		, AVG(entry_score::DECIMAL) FILTER(WHERE tag1_rank *2 <= tag1_count ) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_top_50
		, AVG(entry_score::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg
		, AVG(entry_score::DECIMAL) FILTER(WHERE ( tag1_count - tag1_rank +1 ) * 2 <= tag1_count ) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_bottom_50
		, AVG(entry_score::DECIMAL) FILTER(WHERE  (tag1_count - tag1_rank +1 ) * 4 <= tag1_count ) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL<=entry_score) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_gte100 
		, COUNT(*) FILTER(WHERE 90::DECIMAL<=entry_score AND entry_score <100::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL<=entry_score AND entry_score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL<=entry_score AND entry_score <80::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL<=entry_score AND entry_score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL<=entry_score AND entry_score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL<=entry_score AND entry_score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL<=entry_score AND entry_score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL<=entry_score AND entry_score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL<=entry_score AND entry_score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_10
		, COUNT(*) FILTER(WHERE entry_score<10::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_lt10 
		, student_id
		, entry_score AS score
		, tag1_rank AS rank
        , tag1rank_pr AS pr
		, tag1rank_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		entry_rank_expand
	WHERE
		item_name NOT LIKE '%(原始)%'
		AND rank_tag1 IS NOT NULL
		AND rank_tag1 <> ''
	
	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, '學期/分項成績'::TEXT AS item_type
		, exam_id AS ref_exam_id
		, item_name
		, '類別2排名'::TEXT AS rank_type
		, rank_tag2 AS rank_name
		, true AS is_alive
		, tag2_count AS matrix_count
		, AVG(entry_score::DECIMAL) FILTER(WHERE tag2_rank  *4 <=  tag2_count ) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_top_25
		, AVG(entry_score::DECIMAL) FILTER(WHERE tag2_rank  *2 <=  tag2_count ) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_top_50
		, AVG(entry_score::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg
		, AVG(entry_score::DECIMAL) FILTER(WHERE (tag2_count -tag2_rank +1 ) *2 <= tag2_count ) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_bottom_50
		, AVG(entry_score::DECIMAL) FILTER(WHERE (tag2_count -tag2_rank +1 ) *4 <= tag2_count ) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL<=entry_score) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_gte100 
		, COUNT(*) FILTER(WHERE 90::DECIMAL<=entry_score AND entry_score <100::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL<=entry_score AND entry_score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL<=entry_score AND entry_score <80::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL<=entry_score AND entry_score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL<=entry_score AND entry_score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL<=entry_score AND entry_score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL<=entry_score AND entry_score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL<=entry_score AND entry_score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL<=entry_score AND entry_score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_10
		, COUNT(*) FILTER(WHERE entry_score<10::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_lt10 
		, student_id
		, entry_score AS score
		, tag2_rank AS rank
        , tag2rank_pr AS pr
		, tag2rank_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		entry_rank_expand
	WHERE
		item_name NOT LIKE '%(原始)%'
		AND rank_tag2 IS NOT NULL
		AND rank_tag2 <> ''
		 
)
, update_data AS
(
	UPDATE
		rank_matrix
	SET
		is_alive = NULL
	FROM
		calc_condition
	WHERE
		rank_matrix.is_alive = true
		AND rank_matrix.school_year = calc_condition.rank_school_year::INT
		AND rank_matrix.semester = calc_condition.rank_semester::INT
		AND rank_matrix.grade_year = calc_condition.rank_grade_year::INT
		AND rank_matrix.ref_exam_id = calc_condition.ref_exam_id::INT

	RETURNING rank_matrix.*
)
, insert_batch_data AS
(
	INSERT INTO
		rank_batch
		(
			school_year
			, semester
			, calculation_description
			, setting
		)
		SELECT
			DISTINCT
			calc_condition.rank_school_year::INT
			, calc_condition.rank_semester::INT
			, calc_condition.rank_school_year || ' ' || calc_condition.rank_semester || ' 計算' || calc_condition.rank_exam_name || '排名' AS calculation_description
			, calc_condition.calculation_setting
		FROM
			calc_condition

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
			, matrix_tag1
			, matrix_tag2
		)
		SELECT
			insert_batch_data.id AS ref_batch_id
			, score_list.student_id
			, score_list.rank_grade_year::INT
			, score_list.rank_grade_year||'年級' AS matrix_grade
			, score_list.rank_class_name
			, score_list.rank_tag1
			, score_list.rank_tag2
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
            , pr
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
SELECT count(*) FROM score_list
";

                        //SELECT
                        //	score_list.rank_school_year::INT
                        //	, score_list.rank_semester::INT
                        //	, score_list.rank_grade_year::INT
                        //	, score_list.item_type
                        //	, score_list.ref_exam_id
                        //	, score_list.item_name
                        //	, score_list.rank_type
                        //	, score_list.rank_name
                        //	, score_list.student_id
                        //FROM 
                        //	score_list
                        //	LEFT OUTER JOIN insert_matrix_data
                        //		ON insert_matrix_data.school_year = score_list.rank_school_year::INT
                        //		AND insert_matrix_data.semester = score_list.rank_semester::INT
                        //		AND insert_matrix_data.grade_year = score_list.rank_grade_year::INT
                        //		AND insert_matrix_data.item_type = score_list.item_type
                        //		AND insert_matrix_data.ref_exam_id = score_list.ref_exam_id
                        //		AND insert_matrix_data.item_name = score_list.item_name
                        //		AND insert_matrix_data.rank_type = score_list.rank_type
                        //		AND insert_matrix_data.rank_name = score_list.rank_name
                        //";
                        #endregion

                        //// debug 
                        //string fiPath = Application.StartupPath + @"\sems_sql1.sql";
                        //using (System.IO.StreamWriter fi = new System.IO.StreamWriter(fiPath))
                        //{
                        //    fi.WriteLine(insertRankSql);
                        //}

                        try
                        {

                            DataTable dt1 = queryHelper.Select(insertRankSql);
                            //if (dt1.Rows.Count > 0)
                            //{
                            //    string fiPath = Application.StartupPath + @"\debug.txt";
                            //    using (System.IO.StreamWriter fi = new System.IO.StreamWriter(fiPath,true))
                            //    {
                            //        fi.WriteLine(gr +" 筆數："+dt1.Rows[0][0].ToString());
                            //    }
                            //}

                            bkw.ReportProgress(pr);
                            pr += 20;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }

                    }

                    bkw.ReportProgress(100);
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
                    btnCacluate.Enabled = true;
                    btnPrevious.Enabled = true;
                    throw new Exception("計算排名失敗", bkwException);
                }

                MessageBox.Show("計算完成");
                MotherForm.SetStatusBarMessage("排名計算完成");
                pbLoading.Visible = false;
                btnCacluate.Enabled = true;
                btnPrevious.Enabled = true;
            };

            bkw.RunWorkerAsync();
        }

        private void CalculateSemesterAssessmentRank_Resize(object sender, EventArgs e)
        {
            pbLoading.Location = new Point(this.Width / 2 - 20, this.Height / 2 - 20);
        }
    }
}
