using System.Collections.Generic;
using System.Linq;
using System.Text;
using FISCA.UDT;

namespace SHEvaluation.Rank.UDT
{
    [TableName("campus.technology_star.registration_subject")]
    public class udtRegistrationSubject : ActiveRecord
    {
         /// <summary>
        /// 科目名稱
        /// </summary>
        [Field(Field = "subject_name", Indexed = false)]
        public string SubjectName { get; set; }
        /// <summary>
        /// 一上科目
        /// </summary>
        [Field(Field = "subj_1a", Indexed = false)]
        public string Subj1A { get; set; }

        /// <summary>
        /// 一下科目
        /// </summary>
        [Field(Field = "subj_1b", Indexed = false)]
        public string Subj1B { get; set; }

        /// <summary>
        /// 二上科目
        /// </summary>
        [Field(Field = "subj_2a", Indexed = false)]
        public string Subj2A { get; set; }

        /// <summary>
        /// 二下科目
        /// </summary>
        [Field(Field = "subj_2b", Indexed = false)]
        public string Subj2B { get; set; }

        /// <summary>
        /// 三上科目
        /// </summary>
        [Field(Field = "subj_3a", Indexed = false)]
        public string Subj3A { get; set; }


    }
}
