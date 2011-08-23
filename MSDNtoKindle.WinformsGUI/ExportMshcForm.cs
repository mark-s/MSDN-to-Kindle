// Copyright (c) Microsoft Corporation.  All rights reserved.
//

using System;
using System.Windows.Forms;
using System.IO;

namespace PackageThis.GUI
{
    public partial class ExportMshcForm : Form
    {
        public ExportMshcForm()
        {
            InitializeComponent();

            //show last settings
            MshcFileTextBox.Text = Gui.GetString(Gui.VID_MshcFile, "");
            VendorName.Text = Gui.GetString(VendorName.Name, "PackageThis");
        }

        public void UpdateFields()
        {
            String filename_NoExt = Path.GetFileNameWithoutExtension(MshcFileTextBox.Text);
            ProdName.Text = "PackageThis_" + filename_NoExt;   //these names must be unique for each package
            BookName.Text = "PackageThis_" + filename_NoExt;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            OKBtn.Enabled = (String.IsNullOrEmpty(MshcFileTextBox.Text) == false);
            UpdateFields();
        }

        private void BrowseBtn_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                MshcFileTextBox.Text = saveFileDialog1.FileName;
                this.ActiveControl = VendorName;
            }
        }

        private void ExportMshcForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (this.DialogResult == DialogResult.OK)
            {
                MshcFileTextBox.Text = MshcFileTextBox.Text.Trim();
                VendorName.Text = VendorName.Text.Trim();
                ProdName.Text = ProdName.Text.Trim();
                BookName.Text = BookName.Text.Trim();
                if (Directory.Exists(MshcFileTextBox.Text))
                {
                    e.Cancel = true;
                    MessageBox.Show("Can't create file \"" + Path.GetFileName(MshcFileTextBox.Text) + "\".\n\nA folder already exists with this name!",
                        "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }


                //Save settings
                Gui.SetString(Gui.VID_MshcFile, MshcFileTextBox.Text);
                Gui.SetString(VendorName.Name, VendorName.Text);
                Gui.SetString(ProdName.Name, ProdName.Text);
                Gui.SetString(BookName.Name, BookName.Text);
            }
            
        }


    }
}