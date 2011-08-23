// Copyright (c) Microsoft Corporation.  All rights reserved.
//

using System;
using System.ComponentModel;
using System.Windows.Forms;
using PackageThis.Export;

namespace PackageThis.GUI
{
    public partial class ExportProgressForm : Form, IProgressReporter
    {
        private ICompilableHelp helpFile;
        private int expectedLines;
        private int lines = 0;


        public ExportProgressForm(ICompilableHelp helpFile, int expectedLines)
        {
            InitializeComponent();

            this.helpFile = helpFile;
            this.expectedLines = expectedLines;
        }

        private void ProgressForm_Load(object sender, EventArgs e)
        {
            backgroundWorker1.RunWorkerAsync();
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;


            helpFile.Compile(this as IProgressReporter);
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.Close();
            
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            string message = e.UserState as string;

            if (message != "" && message[0] == '!')          // Leading ! = reset progress bar
            {
                lines = 0;
                progressBar1.Value = 0;
                if (message.Length > 1)
                    label1.Text = message.Substring(1);      //skip ! leading char
            }
            else
            {
                progressBar1.Value = e.ProgressPercentage;
                if (message != "*")                          // * = advances progress only
                    label1.Text = message;
            }
        }

        void IProgressReporter.ProgressMessage(string message)
        {
            if (expectedLines > 0)
            {
                int percent = (lines++ * 100) / expectedLines;

                if (percent > 100)
                    percent = 100;

                backgroundWorker1.ReportProgress(percent, message);
            }
        }

        // http://blogs.msdn.com/greggm/archive/2005/11/18/494648.aspx
        //
        public override string ToString()
        {
            return "Disabled to make debugger work.";
        }
    }
}