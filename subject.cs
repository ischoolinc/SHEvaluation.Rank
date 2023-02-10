using System;
using System.Collections.Generic;
using System.Text;

namespace SHEvaluation.Rank
{
    class subject
    {
        private string _Domain;
        private string _Gategory;
        private string _SubjectName;
        private string _SubjectLevel;
        private string _SubjectCode;
        private int _GradeYear;
        private int _Semester;
        private string _Required;
        private decimal _Score;
        private bool _HasScore;
        private decimal _Credit;
        private decimal _Credit1;
        private bool _SameCoursePlan;
        private bool _Acquired;
        private int _SchoolYear;
        private int _Row;
        private int _Col;


        public string Domain
        { get { return _Domain; } set { _Domain = value; } }
        public string Gategory
        { get { return _Gategory; } set { _Gategory = value; } }
        public string SubjectName
        { get { return _SubjectName; } set { _SubjectName = value; } }

        public string SubjectLevel
        { get { return _SubjectLevel; } set { _SubjectLevel = value; } }
        public string SubjectCode
        { get { return _SubjectCode; } set { _SubjectCode = value; } }

        public int GradeYear
        { get { return _GradeYear; } set { _GradeYear = value; } }

        public int Semester
        { get { return _Semester; } set { _Semester = value;} }

        public string Required
        { get { return _Required; } set { _Required = value; } }


        public decimal Credit
        { get { return _Credit; } set { _Credit = value; } }
        public decimal Credit1
        { get { return _Credit1; } set { _Credit1 = value; } }

        public bool SameCoursePlan
        { get { return _SameCoursePlan; } set { _SameCoursePlan = value; } }

        public bool Acquired
        { get { return _Acquired; } set { _Acquired = value; } }

        public decimal Score
        { get { return _Score; } set { _Score = value; } }


        public bool HasScore
        { get { return _HasScore; } set { _HasScore = value; } }

        public int SchoolYear
        { get { return _SchoolYear; } set { _SchoolYear = value; } }

        public int Row
        { get { return _Row; } set { _Row = value; } }

        public int Col
        { get { return _Col; } set { _Col = value; } }
    }
}
