// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using PackageThis.ContentService;
using PackageThis.Core;
using PackageThis.Core.Constants;
using PackageThis.WinformsGUI.Properties;

namespace PackageThis.GUI
{
    public partial class MainForm : Form
    {
        private static AppController _appController;
        private static string _currentLocale = CultureInfo.CurrentCulture.Name.ToLower();
        private static string _workingDir;
        private static string _tempPath;
        private static string _tempDir;

        public MainForm()
        {
            SplashForm.Init();
            InitializeComponent();
        }

        public string AssemblyVersion { get; set; }

        private void MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                Text = String.Format("Package This! - {0}", Assembly.GetExecutingAssembly().GetName().Version);

                // Unicode Start of String, chosen to avoid collisions possible with default sep
                // when we serialize the path to a file.
                TOCTreeView.PathSeparator = "\x0098";

                CreateTempDir();

                RootContentItem.CurrentLibrary = Settings.Default.currentLibrary;

                //statusStrip1.Items.Add(workingDir);
                statusStrip1.Items.Add(RootContentItem.Name);

                SplashForm.Status("Connecting to server..."); //First time we hit the server (at least in Australia) we get a 15 sec delay
                populateLocaleMenu();

                SplashForm.Status("Loading controls...");
                populateLibraryMenu();

                _appController = new AppController(RootContentItem.ContentId, _currentLocale, RootContentItem.Version, TOCTreeView, _workingDir);
            }
            finally
            {
                SplashForm.Done();
            }
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            BringToFront();
            Activate(); //The splash form will kick us to the back. This brings us forward again.
        }

        private void selectLocale_Click(object sender, EventArgs e)
        {
            var selected = sender as ToolStripMenuItem;
        }

        private void populateLocaleMenu()
        {
            SortedDictionary<string, string> locales = Utility.GetLocales();

            localeToolStripMenuItem.DropDownItems.Clear();

            foreach (string displayName in locales.Keys)
            {
                var menuItem = new ToolStripMenuItem(displayName, null, localeToolStripMenuItem_Click);
                menuItem.Name = locales[displayName];
                localeToolStripMenuItem.DropDownItems.Add(menuItem);

                //if (currentLocale == locales[displayName])
                if (_currentLocale.Substring(0, 3) == locales[displayName].Substring(0, 3)) //allows for en-au == en-us
                {
                    menuItem.Checked = true;
                    _currentLocale = locales[displayName]; //record the locale that matches the MSDN locale pallet
                }
            }
        }

        private void populateLibraryMenu()
        {
            for (int i = 0; i < RootContentItem.Libraries.Count; i++)
            {
                var menuItem = new ToolStripMenuItem(RootContentItem.Libraries[i], null, libraryToolStripMenuItem_Click);

                menuItem.Name = RootContentItem.Libraries[i];
                menuItem.Text = "&" + menuItem.Text;
                libraryToolStripMenuItem.DropDownItems.Insert(i, menuItem);

                if (RootContentItem.CurrentLibrary == i)
                    menuItem.Checked = true;
                else
                    menuItem.Checked = false;
            }
        }

        private void TOCTreeView_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                _appController.ExpandNode(e.Node);
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }

        private void localeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var selected = sender as ToolStripMenuItem;

            if (selected.Checked)
                return;


            for (int i = 0; i < localeToolStripMenuItem.DropDownItems.Count; i++)
            {
                if ((localeToolStripMenuItem.DropDownItems[i] as ToolStripMenuItem).Checked)
                {
                    (localeToolStripMenuItem.DropDownItems[i] as ToolStripMenuItem).Checked = false;
                }
            }

            selected.Checked = true;


            _currentLocale = selected.Name;
            reloadLibrary();
        }

        private void libraryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var selected = sender as ToolStripMenuItem;

            if (selected.Checked)
                return;

            for (int i = 0; i < libraryToolStripMenuItem.DropDownItems.Count; i++)
            {
                if ((libraryToolStripMenuItem.DropDownItems[i] as ToolStripMenuItem).Checked)
                {
                    (libraryToolStripMenuItem.DropDownItems[i] as ToolStripMenuItem).Checked = false;
                }
            }

            selected.Checked = true;

            RootContentItem.CurrentLibrary = Settings.Default.currentLibrary = RootContentItem.Libraries.IndexOf(selected.Name);
            Settings.Default.Save();

            reloadLibrary();
        }

        private void reloadLibrary()
        {
            Cursor.Current = Cursors.WaitCursor;

            try
            {
                CleanUpTempDir();
                CreateTempDir();

                statusStrip1.Items.Clear();
                //statusStrip1.Items.Add(workingDir);
                statusStrip1.Items.Add(RootContentItem.Name);

                TOCTreeView.BeginUpdate();
                TOCTreeView.Nodes.Clear();

                _appController = new AppController(RootContentItem.ContentId, _currentLocale, RootContentItem.Version, TOCTreeView, _workingDir);

                TOCTreeView.EndUpdate();

                if (ContentDataSet.Tables[TableNames.ITEMINSTANCE] != null)
                    ContentDataSet.Tables[TableNames.ITEMINSTANCE].Clear();

                if (ContentDataSet.Tables[TableNames.ITEM] != null)
                    ContentDataSet.Tables[TableNames.ITEM].Clear();

                if (ContentDataSet.Tables[TableNames.PICTURE] != null)
                    ContentDataSet.Tables[TableNames.PICTURE].Clear();
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }


        private void TOCTreeView_BeforeCheck(object sender, TreeViewCancelEventArgs e)
        {
            //string[] split = e.Node.FullPath.Split(new char[] {'\x0098'});


            if (e.Node.Checked == false)
            {
                Cursor.Current = Cursors.WaitCursor;
                try
                {
                    if (_appController.WriteContent(e.Node, ContentDataSet) == false)
                        e.Cancel = true;
                }
                finally
                {
                    Cursor.Current = Cursors.Default;
                }
            }
            else
            {
                _appController.RemoveContent(e.Node, ContentDataSet);
            }
        }

        private void TOCTreeView_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
                TOCTreeView.SelectedNode = TOCTreeView.GetNodeAt(e.X, e.Y);

        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                ContentDataSet.Tables[TableNames.ITEMINSTANCE].WriteXml(saveFileDialog1.FileName, XmlWriteMode.WriteSchema);
        }


        private void selectNodeAndChildrenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dpf = new DownloadProgressForm(TOCTreeView.SelectedNode, ContentDataSet);

            dpf.ShowDialog();
        }

        private void deselectThisNodeAndAllChildrenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _appController.UncheckNodes(TOCTreeView.SelectedNode);
        }


        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            CleanUpTempDir();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        // TODO: Move this to AppController.
        private void CleanUpTempDir()
        {
            // Do some sanity checks on workingDir so we don't, due to a b ug, delete too much.

            if ((_workingDir.StartsWith(_tempPath) != true) || (_workingDir.Contains(_tempDir) != true))
                return;

            try
            {
                Directory.Delete(_workingDir, true);
            }
            catch (IOException ex)
            {
                Debug.WriteLine(ex.Message);
                Debugger.Break();
            }
        }

        private void CreateTempDir()
        {
            _tempPath = Path.Combine(Path.GetTempPath(), "PackageThis");
                // <-- If we are not going to cleanup properly then lets at least group under this folder 
            _tempDir = Path.GetRandomFileName();
            _workingDir = Path.Combine(_tempPath, _tempDir) + "\\";
            Directory.CreateDirectory(_workingDir);
        }

        private void exportToChmFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //if (ContentDataSet.Tables[TableNames.ITEM].Rows.Count == 0)
            //    return;

            ////Test if HH Workshop installed
            //string key = @"HKEY_CURRENT_USER\Software\Microsoft\HTML Help Workshop";
            //var install = (string) Registry.GetValue(key, "InstallDir", null);
            //string hhcExe = Path.Combine(install, "hhc.exe");

            //if (install == null || File.Exists(hhcExe) == false)
            //{
            //    MessageBox.Show("Please install the HTML Help Workshop from http://www.microsoft.com/download/en/details.aspx?id=21138", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            //    return;
            //}


            //var frm = new ExportChmForm();

            //if (frm.ShowDialog() != DialogResult.OK)
            //    return;

            //_appController.CreateChm(frm.ChmFileTextBox.Text, frm.TitleTextBox.Text,
            //                        _currentLocale, ContentDataSet);
        }

        private void exportToHxsFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //if (ContentDataSet.Tables[TableNames.ITEM].Rows.Count == 0)
            //    return;

            //// Test if Help SDK Install - Suck it and see
            //try
            //{
            //    var hxsCompiler = new HxComp();
            //    hxsCompiler.Initialize();
            //    hxsCompiler = null;
            //}
            //catch
            //{
            //    MessageBox.Show(@"Please install the VS 2005\2008 SDK, which includes the MS Help 2.x SDK and compiler.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            //    return;
            //}

            //var exportDialog = new GenerateHxsForm();

            //if (exportDialog.ShowDialog() != DialogResult.OK)
            //{
            //    return;
            //}

            //_appController.CreateHxs(exportDialog.FileTextBox.Text,
            //                        exportDialog.TitleTextBox.Text,
            //                        exportDialog.CopyrightComboBox.Text,
            //                        _currentLocale,
            //                        ContentDataSet);
        }

        private void exportToMshcFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //if (ContentDataSet.Tables[TableNames.ITEM].Rows.Count == 0)
            //    return;

            //var frm = new ExportMshcForm();

            //if (frm.ShowDialog() != DialogResult.OK)
            //    return;

            //_appController.CreateMshc(frm.MshcFileTextBox.Text, _currentLocale, ContentDataSet, frm.VendorName.Text, frm.ProdName.Text, frm.BookName.Text);
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var d = new AboutBox();
            d.ShowDialog(this);
        }

        private void toolStripMenuOnlineDocumentation_Click(object sender, EventArgs e)
        {
            Process.Start("http://packagethis.codeplex.com/documentation");
        }

        private void mnuInstallMshcHelpFile_Click(object sender, EventArgs e)
        {
            var frm = new InstallMshcForm();
            frm.LocaleName.Text = _currentLocale;

            if (frm.ShowDialog() != DialogResult.OK)
                return;

            string HelpLibManagerExe = @"c:\program files\Microsoft Help Viewer\v1.0\HelpLibManager.exe";
            string arguments = String.Format(@"/product {0} /version {1} /locale {2}", frm.ProdName.Text, frm.VersionName.Text, frm.LocaleName.Text);

            // Install
            if (frm.MshaFileTextBox.Text.Length != 0)
                arguments = arguments + String.Format(@" /sourceMedia {0}", frm.MshaFileTextBox.Text);

            if (File.Exists(HelpLibManagerExe) == false)
            {
                MessageBox.Show("File not found: " + HelpLibManagerExe);
                return;
            }

            var process = new Process();
            process.StartInfo.FileName = HelpLibManagerExe;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = true;
            process.StartInfo.Verb = "runas"; //run as administrator -- Required for installation
            process.Start();

        }

        //eg. Open associated web page http://msdn.microsoft.com/en-us/library/ms533050(v=vs.85).aspx
        private void gotoWebPage_toolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (TOCTreeView.SelectedNode != null)
            {
                var mtpsNode = TOCTreeView.SelectedNode.Tag as MtpsNode;

                String docContentId = _appController.GetDocShortId(TOCTreeView.SelectedNode);

                Process.Start(String.Format("http://msdn.microsoft.com/{0}/library/{1}({2}).aspx", mtpsNode.TargetLocale, docContentId, mtpsNode.TargetVersion));
            }
        }

        //eg. Open associated mtps page of doc http://services.mtps.microsoft.com/serviceapi/content/ms533050/en-us;vs.85
        private void gotoMtpsPage_toolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (TOCTreeView.SelectedNode != null)
            {
                var mtpsNode = TOCTreeView.SelectedNode.Tag as MtpsNode;

                String docContentId = _appController.GetDocShortId(TOCTreeView.SelectedNode);

                Process.Start(String.Format("http://services.mtps.microsoft.com/serviceapi/content/{1}/{0};{2}", mtpsNode.TargetLocale, docContentId, mtpsNode.TargetVersion));
            }
        }

        private void gotoTocMTPSPageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (TOCTreeView.SelectedNode != null)
            {
                var mtpsNode = TOCTreeView.SelectedNode.Tag as MtpsNode;
                Process.Start(String.Format("http://services.mtps.microsoft.com/serviceapi/content/{1}/{0};{2}", mtpsNode.NavLocale, mtpsNode.TargetContentId, mtpsNode.NavVersion));
            }
        }


        private void menuStrip1_MenuActivate(object sender, EventArgs e)
        {
            Boolean hasTableItems = ContentDataSet.Tables[TableNames.ITEM].Rows.Count > 0;
            exportToChmFileToolStripMenuItem.Enabled = hasTableItems;
            exportToHxsFileToolStripMenuItem.Enabled = hasTableItems;
            exportToMshcFileToolStripMenuItem.Enabled = hasTableItems;
        }

        private void menuStrip1_MenuDeactivate(object sender, EventArgs e)
        {
            exportToChmFileToolStripMenuItem.Enabled = true;
            exportToHxsFileToolStripMenuItem.Enabled = true;
            exportToMshcFileToolStripMenuItem.Enabled = true;
        }

        private void toolStripMenuOpenWorkDir_Click(object sender, EventArgs e)
        {
            Process.Start(_workingDir);
        }
    }
}