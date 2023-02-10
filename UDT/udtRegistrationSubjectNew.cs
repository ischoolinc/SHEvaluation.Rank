using System.Collections.Generic;
using System.Linq;
using System.Text;
using FISCA.UDT;

namespace SHEvaluation.Rank.UDT
{
    [TableName("campus.technology_star.registration_subjectNew")]
    public class udtRegistrationSubjectNew : ActiveRecord
    {
        /// <summary>
        /// 報名群名稱
        /// </summary>
        [Field(Field = "reg_group_name", Indexed = false)]
        public string RegGroupName { get; set; }
        /// <summary>
        /// 比序項冚
        /// </summary>
        [Field(Field = "Calc_name", Indexed = false)]
        public string CalcName { get; set; }

        /// <summary>
        /// 科目名稱
        /// </summary>
        [Field(Field = "subject_name", Indexed = false)]
        public string SubjectName { get; set; }

        /// <summary>
        /// 年級
        /// </summary>
        [Field(Field = "Grade_Year", Indexed = false)]
        public string GradeYear { get; set; }
        
        /// <summary>
        /// 學期
        /// </summary>
        [Field(Field = "Semester", Indexed = false)]
        public string Semester { get; set; }


    }
}
