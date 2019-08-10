// gcam: SyncroSim Base Package for the Global Change Assessment Model (GCAM).
// Copyright © 2007-2019 Apex Resource Management Solutions Ltd. (ApexRMS). All rights reserved.

using SyncroSim.Core;
using SyncroSim.Core.Forms;
using System.Windows.Forms;

namespace SyncroSim.GCAM
{
    public partial class ApplicationDatafeedView : DataFeedView
    {
        public ApplicationDatafeedView()
        {
            InitializeComponent();
        }

        public override void LoadDataFeed(DataFeed dataFeed)
        {
            base.LoadDataFeed(dataFeed);
            this.SetTextBoxBinding(this.TextBoxGCAMFolder, Shared.APPLICATION_FOLDER_COLUMN_NAME);
            this.AddStandardCommands();
        }

        public override void EnableView(bool enable)
        {
            base.EnableView(enable);
            this.TextBoxGCAMFolder.Enabled = false;
        }

        private void ChooseFolder()
        {
            FolderBrowserDialog dlg = new FolderBrowserDialog();

            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                DataSheet ds = this.DataFeed.GetDataSheet(Shared.APPLICATION_DATAFEED_NAME);

                ds.SetSingleRowData(Shared.APPLICATION_FOLDER_COLUMN_NAME, dlg.SelectedPath);
                this.RefreshBoundControls();
            }
        }

        private void ButtonBrowseGCAMFolder_Click(object sender, System.EventArgs e)
        {
            this.ChooseFolder();
        }
    }
}
