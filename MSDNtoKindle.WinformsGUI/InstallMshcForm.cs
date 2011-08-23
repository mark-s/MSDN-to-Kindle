using System;
using System.Windows.Forms;
using System.IO;

namespace PackageThis.GUI
{
    public partial class InstallMshcForm : Form
    {
        public InstallMshcForm()
        {
            InitializeComponent();

            //show last settings -- The .msha file lives in the same folder as the .mshc file

            String mshcFileName = Gui.GetString(Gui.VID_MshcFile, "");  //The file entered in the export dialog
            if (mshcFileName.Length != 0)
                MshaFileTextBox.Text = Path.GetDirectoryName(mshcFileName) + @"\HelpContentSetup.msha";
            else
                MshaFileTextBox.Text = Gui.GetString(MshaFileTextBox.Name, "");
        }

        private void BrowseBtn_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                MshaFileTextBox.Text = openFileDialog1.FileName;
                this.ActiveControl = MshaFileTextBox;
            }
        }

        private void InstallMshcForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (this.DialogResult == DialogResult.OK)
            {
                //Trim Whitespace
                MshaFileTextBox.Text = MshaFileTextBox.Text.Trim();
                ProdName.Text = ProdName.Text.Trim();
                VersionName.Text = VersionName.Text.Trim();
                LocaleName.Text = LocaleName.Text.Trim();

                //Save settings
                Gui.SetString(MshaFileTextBox.Name, MshaFileTextBox.Text);
            }
            
        }
    }
}
