using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FISCA.Data;
using System.Data;
using FISCA.Presentation.Controls;
using K12.Data;

namespace SHEvaluation.Rank.DAO
{
    public class QueryData
    {
        /// <summary>
        /// 取得科別
        /// </summary>
        /// <returns></returns>
        public DataTable GetDeptInfoDT()
        {
            try
            {
                QueryHelper qh = new QueryHelper();
                string qry = "SELECT id,name FROM dept ORDER BY code,name;";
                DataTable dt = qh.Select(qry);
                return dt;
            }
            catch (Exception ex)
            {
                MsgBox.Show("取得系統科別失敗，" + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// 取得傳入學生ID 的成績年級與學期
        /// </summary>
        /// <param name="GradeYear"></param>
        /// <returns></returns>
        public Dictionary<string, List<GradeYearSemesterInfo>> GetScoreGradeSemesterByGradeYear(List<string> StudentIDList)
        {
            Dictionary<string, List<GradeYearSemesterInfo>> value = new Dictionary<string, List<GradeYearSemesterInfo>>();
            try
            {
                if (StudentIDList.Count > 0)
                {
                    QueryHelper qh = new QueryHelper();
                    //                string qry = "" +
                    //                   "WITH grade_semester AS( " +
                    //"SELECT student.id AS student_id,sems_subj_score.school_year,sems_subj_score.semester,sems_subj_score.grade_year FROM student INNER JOIN class ON student.ref_class_id = class.id LEFT JOIN sems_subj_score ON sems_subj_score.ref_student_id = student.id WHERE student.id IN(" + string.Join(",", StudentIDList.ToArray()) + ") " +
                    //"UNION ALL " +
                    //"SELECT student.id AS student_id," + K12.Data.School.DefaultSchoolYear + " AS school_year," + K12.Data.School.DefaultSemester + " AS semester,class.grade_year FROM student INNER JOIN class ON student.ref_class_id = class.id WHERE student.id IN(" + string.Join(",", StudentIDList.ToArray()) + ")) " +
                    //"SELECT student_id,max(school_year) AS school_year,semester,grade_year FROM grade_semester WHERE grade_year IS NOT NULL  GROUP BY student_id,semester,grade_year ORDER BY student_id,grade_year,semester " +
                    //                    "";

                    // 實際有成績
                    string qry = "" +
                       "WITH grade_semester AS( " +
    "SELECT student.id AS student_id,sems_subj_score.school_year,sems_subj_score.semester,sems_subj_score.grade_year FROM student INNER JOIN class ON student.ref_class_id = class.id LEFT JOIN sems_subj_score ON sems_subj_score.ref_student_id = student.id WHERE student.id IN(" + string.Join(",", StudentIDList.ToArray()) + ") " +
    ") " +
    "SELECT student_id,max(school_year) AS school_year,semester,grade_year FROM grade_semester WHERE grade_year IS NOT NULL  GROUP BY student_id,semester,grade_year ORDER BY student_id,grade_year,semester " +
                        "";

                    DataTable dt = qh.Select(qry);

                    foreach (DataRow dr in dt.Rows)
                    {
                        string sid = dr["student_id"].ToString();

                        if (!value.ContainsKey(sid))
                            value.Add(sid, new List<GradeYearSemesterInfo>());

                        int gr, sc, ss;
                        int.TryParse(dr["grade_year"].ToString(), out gr);
                        int.TryParse(dr["semester"].ToString(), out ss);
                        int.TryParse(dr["school_year"].ToString(), out sc);

                        GradeYearSemesterInfo gs = new GradeYearSemesterInfo();
                        gs.GradeYear = gr;
                        gs.Semester = ss;
                        gs.SchoolYear = sc;

                        value[sid].Add(gs);

                    }
                }
            }
            catch (Exception ex)
            {
                MsgBox.Show("取得學生成績年級、學期 失敗，" + ex.Message);
                return value;
            }

            return value;
        }

        public DataTable GetSkillDomainSubject(int entryYear)  
        {
            DataTable value = new DataTable();
            try
            {
                QueryHelper qh = new QueryHelper();
                string query = @" WITH student_data AS(
 SELECT   
	student.id AS student_id  
	, student.name AS student_name  
	, student_number  
	, student.seat_no  
	, class_name  
	, class.grade_year AS grade_year  
	, COALESCE(student.gdc_code,class.gdc_code)  AS gdc_code  
	, class.display_order
 FROM student   
 INNER JOIN class  
 ON student.ref_class_id = class.id  
 WHERE   
  student.status IN(1,2) AND class.grade_year IN(  3  )  
  AND COALESCE(student.gdc_code, class.gdc_code)  IS NOT NULL  
 )
 SELECT   
 --subject_name
 --,course_code
 	DISTINCT(subject_name) AS 科目名稱
 FROM 
 	$moe.subjectcode 
 INNER JOIN 
 	student_data ON gdc_code=group_code
 WHERE 
 	entry_year = '{0}' 
 	AND SUBSTR(course_code,20,2) IN ('A1',	'A2',	'A3',	'A4',	'A5',	'A6',	'B1',	'B2',	'B3',	'B4',	'C1',	'C2',	'C3',	'C4',	'C5',	'D1','D2','E2',	'E3',	'F1',	'F2',	'F3',	'H1','H2','H3','H4','H5','H6','I1',	'I2','J1','J2',	'K1',	'K2',	'K3',	'L1',	'L2',	'L3',	'L4',	'M1',	'M2',	'M3',	'M4',	'M5',	'M6',	'M7',	'N1',	'N2',	'N3',	'N4',	'N5',	'N6',	'N7',	'O1',	'O2',	'O3',	'O4',	'O5',	'O6',	'U1',	'U2',	'U3',	'U4',	'U5',	'U6',	'U7',	'U8',	'U9',	'UA',	'UB',	'UC',	'UD',	'UE',	'UF',	'G3',	'G1',	'G2')

 
 ";
                query =  string.Format(query,entryYear);
                DataTable dt = qh.Select(query);
                value = dt;
                //foreach (DataRow dr in dt.Rows)
                //    value.Rows.Add(dr);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return value;


        }



    }
}
