using System.Collections.Generic;
using System.Linq;
using System.Text;
using FISCA.UDT;

namespace SHEvaluation.Rank.UDT
{
    [TableName("campus.technology_star.registration_dept")]
    public class udtRegistrationDept : ActiveRecord
    {
        /// <summary>
        /// 科別ID
        /// </summary>
        [Field(Field = "ref_dept_id", Indexed = false)]
        public string RefDeptID { get; set; }

        /// <summary>
        /// 科別名稱
        /// </summary>
        [Field(Field = "dept_name", Indexed = false)]
        public string DeptName { get; set; }

        /// <summary>
        /// 報名科名稱
        /// </summary>
        [Field(Field = "reg_dept_name", Indexed = false)]
        public string RegDeptName { get; set; }

        /// <summary>
        /// 報名群代碼
        /// </summary>
        [Field(Field = "reg_group_code", Indexed = false)]
        public string RegGroupCode { get; set; }

        /// <summary>
        /// 報名群名稱
        /// </summary>
        [Field(Field = "reg_group_name", Indexed = false)]
        public string RegGroupName { get; set; }
    }
}
