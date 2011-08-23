// Copyright (c) Microsoft Corporation.  All rights reserved.
//

using System;
using System.Windows.Forms;

namespace PackageThis.GUI
{
    public partial class ExportChmForm : Form
    {
        public ExportChmForm()
        {
            InitializeComponent();

            //show last settings
            ChmFileTextBox.Text = Gui.GetString("ChmFileTextBox", "");
            TitleTextBox.Text = Gui.GetString("ChmTitleTextBox", "");
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(ChmFileTextBox.Text) == false &&
                String.IsNullOrEmpty(TitleTextBox.Text) == false)
                OKBtn.Enabled = true;
            else
                OKBtn.Enabled = false;
        }

        private void BrowseBtn_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                ChmFileTextBox.Text = saveFileDialog1.FileName;
                this.ActiveControl = TitleTextBox;

            }

        }

        private void ExportChmForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (this.DialogResult == DialogResult.OK)
            {
                //Save settings
                Gui.SetString("ChmFileTextBox", ChmFileTextBox.Text);
                Gui.SetString("ChmTitleTextBox", TitleTextBox.Text);
            }
        }


    }
}