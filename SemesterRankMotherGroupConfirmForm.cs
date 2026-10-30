using System;
using System.Drawing;
using System.Windows.Forms;
using DevComponents.DotNetBar;
using DevComponents.DotNetBar.Controls;
using SHEvaluation.Rank.MotherGroup;

namespace SHEvaluation.Rank
{
    public partial class SemesterRankMotherGroupConfirmForm : FISCA.Presentation.Controls.BaseForm
    {
        private readonly CalculateSemesterAssessmentRank _host;

        public SemesterRankMotherGroupConfirmForm()
        {
            InitializeComponent();
        }

        public SemesterRankMotherGroupConfirmForm(CalculateSemesterAssessmentRank host)
            : this()
        {
            if (host == null)
                throw new ArgumentNullException("host");
            _host = host;
            if (MinimumSize.IsEmpty)
                MinimumSize = Size;
        }

        internal SemesterRankConfirmRequest Request { get; set; }

        internal LabelX SchoolYearLabel { get { return lbSchoolYear; } }
        internal LabelX SemesterLabel { get { return lbSemester; } }
        internal ButtonX ExportButton { get { return btnExport; } }
        internal ButtonX CalculateButton { get { return btnCacluate; } }
        internal ButtonX PreviousButton { get { return btnPrevious; } }
        internal DataGridViewX StudentGrid { get { return dgvStudentList; } }
        internal PictureBox LoadingImage { get { return pbLoading; } }
        internal Label SummaryLabel { get { return _lblMotherGroupSummary; } }
        internal TextBox SearchBox { get { return _txtMotherGroupSearch; } }
        internal DataGridView PopulationGrid { get { return _dgvRankPopulation; } }
        internal DataGridView DetailGrid { get { return _dgvGroupCounts; } }
        internal Button RestoreButton { get { return _btnRestoreParticipation; } }
        internal Label AddStudentCaption { get { return _lblAddStudentCaption; } }
        internal TextBox AddStudentNumber { get { return _txtAddStudentNumber; } }
        internal Button AddStudentButton { get { return _btnAddHistoryStudent; } }

        private void btnCacluate_Click(object sender, EventArgs e)
        {
            if (_host != null)
                _host.btnCacluate_Click(sender, e);
        }

        private void btnPrevious_Click(object sender, EventArgs e)
        {
            if (_host != null)
                _host.btnPrevious_Click(sender, e);
        }

        private void txtMotherGroupSearch_TextChanged(object sender, EventArgs e)
        {
            if (_host != null)
                _host.txtMotherGroupSearch_TextChanged(sender, e);
        }

        private void btnRestoreParticipation_Click(object sender, EventArgs e)
        {
            if (_host != null)
                _host.btnRestoreParticipation_Click(sender, e);
        }

        private void btnAddHistoryStudent_Click(object sender, EventArgs e)
        {
            if (_host != null)
                _host.btnAddHistoryStudent_Click(sender, e);
        }

        private void SemesterRankMotherGroupConfirmForm_Resize(object sender, EventArgs e)
        {
            if (pbLoading == null)
                return;
            pbLoading.Location = new Point(ClientSize.Width / 2 - 16, ClientSize.Height / 2 - 16);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_host != null && _host.RankCalculationRunning)
            {
                e.Cancel = true;
                return;
            }
            if (e.CloseReason == CloseReason.UserClosing && _host != null)
            {
                e.Cancel = true;
                _host.btnPrevious_Click(this, EventArgs.Empty);
                return;
            }
            base.OnFormClosing(e);
        }

        private void SemesterRankMotherGroupConfirmForm_Load(object sender, EventArgs e)
        {
            this.MaximumSize = this.MinimumSize = this.Size;
        }
    }
}
