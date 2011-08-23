// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Data;
using System.Xml;
using System.Xml.Xsl;
using System.Globalization;
using System.Diagnostics;
using PackageThis.Core;
using PackageThis.Core.Constants;

namespace PackageThis.Export.Hxs
{
    class HxS : ICompilableHelp
    {
        private string projectFile;
        private string outputFile;
        private string locale;
        private string title;
        private string copyright;

        private TreeNodeCollection nodes;
        private Content contentDataSet;
        static private XslCompiledTransform xform = null;
        static private Stream resourceStream = typeof(AppController).Assembly.GetManifestResourceStream("PackageThis.Assets.hxs.xslt");
        static private XmlReader transformFile = XmlReader.Create(resourceStream);
        private Dictionary<string, string> links;
        delegate void ShowCompileError();

        private string rawDir;
        private string hxsDir;
        private string withinHxsDir;
        private string hxsSubDir = "html";
        private string logFile;

        private string baseFilename;
        public int expectedLines = 0;

        public HxS(string workingDir, string hxsFile,
            string title, string copyright, string locale,
            TreeNodeCollection nodes,
            Content contentDataSet,
            Dictionary<string, string> links)
        {
            this.locale = locale;
            this.title = title;
            this.copyright = copyright;
            this.nodes = nodes;
            this.contentDataSet = contentDataSet;
            this.links = links;

            this.outputFile = Path.GetFullPath(hxsFile);
            this.rawDir = Path.Combine(workingDir, "raw");

            // The source shouldn't be hidden away. If an error happens (likely) the user needs to check logs etc.
            //this.hxsDir = Path.Combine(workingDir, "hxs");
            this.hxsDir = GetUniqueDir(hxsFile);
            this.withinHxsDir = Path.Combine(hxsDir, hxsSubDir);
            this.baseFilename = Path.GetFileNameWithoutExtension(hxsFile);
            this.baseFilename = this.baseFilename.Replace(" ", "_");  //replace spaces with _ otherwise we get compile errors

            this.logFile = Path.Combine(hxsDir, this.baseFilename + ".log");
            this.projectFile = Path.Combine(hxsDir, baseFilename + ".hxc");

            if (xform == null)
            {
                xform = new XslCompiledTransform(true);
                xform.Load(transformFile);
            }

        }


        private string GetUniqueDir(string targetFile)
        {
            string basedir = Path.ChangeExtension(targetFile, "ProjectSource");
            string dir = basedir;
            int x = 0;
            while (Directory.Exists(dir))
            {
                x++;
                string num = x.ToString();
                dir = basedir + "." + num.PadLeft(3, '0');
            }
            return dir;   //return a folder that does not exist
        }


        public void Create()
        {
            if (Directory.Exists(hxsDir) == true)
            {
                Directory.Delete(hxsDir, true);
            }

            Directory.CreateDirectory(hxsDir);
            Directory.CreateDirectory(withinHxsDir);

            foreach (string file in Directory.GetFiles(rawDir))
            {
                File.Copy(file, Path.Combine(withinHxsDir, Path.GetFileName(file)), true);
            }


            foreach (DataRow row in contentDataSet.Tables[TableNames.ITEM].Rows)
            {
                //if (Int32.Parse(row["Size"].ToString()) != 0)
                //{
                    Transform(row[ColumnNames.CONTENTID].ToString(),
                        row["Metadata"].ToString(),
                        row["Annotations"].ToString(),
                        row["VersionId"].ToString(),
                        row["Title"].ToString(),
                        contentDataSet);
                //}
            }


            // Create TOC
            Hxt hxt = new Hxt(Path.Combine(hxsDir, baseFilename + ".hxt"), Encoding.UTF8);
            CreateHxt(this.nodes, hxt, contentDataSet);
            hxt.Close();


            CreateHxks(baseFilename);

            WriteExtraFiles();

            Hxf hxf = new Hxf(Path.Combine(hxsDir, baseFilename + ".hxf"), Encoding.UTF8);

            string[] files = Directory.GetFiles(hxsDir, "*", SearchOption.AllDirectories);

            foreach (string file in files)
            {
                hxf.WriteLine(file.Replace(hxsDir, ""));
            }

            hxf.Close();


            string lcid = new CultureInfo(locale).LCID.ToString();
            Hxc hxc = new Hxc(baseFilename, title, lcid, "1.0", copyright, hxsDir, Encoding.UTF8);

            int numHtmlFiles = Directory.GetFiles(hxsDir, "*.htm", SearchOption.AllDirectories).Length;
            int numFiles = Directory.GetFiles(hxsDir, "*", SearchOption.AllDirectories).Length;

            // This gives the number of information lines output by the compiler. It
            // was determined experimentally, and should give some means of making an
            // accurate progress bar during a compile.
            // Actual equation is numInfoLines = 2*numHtmlFiles + (numFiles - numHtmlFiles) + 6
            // After factoring, we get this equation
            expectedLines = numHtmlFiles + numFiles + 6;


        }


        // Compile() is called by a background Thread in ProgressForm so be carful

        void ICompilableHelp.Compile(IProgressReporter progressReporter) //Called by Progress form
        {
            try
            {
                HxComp hxsCompiler = new HxComp();
                hxsCompiler.Initialize();

                CompMsg compMsg = new CompMsg(progressReporter, this.logFile);
                compMsg.Log("Date: " + DateTime.Today.ToShortDateString() + ", " + DateTime.Today.ToShortTimeString());
                compMsg.Log("Log file: " + logFile);
                compMsg.Log("Project file: " + projectFile);
                compMsg.Log("");
                int cookie = hxsCompiler.AdviseCompilerMessageCallback(compMsg);
             
                // Compile
                //
                hxsCompiler.Compile(projectFile, hxsDir, outputFile, 0);

                hxsCompiler.UnadviseCompilerMessageCallback(cookie);   //Done - Break link with compMsg Obj
                compMsg.SaveLog();
                hxsCompiler = null;

                //Show the log file if errors 
                if (compMsg.ErrorCount > 0)   //If nore Warnings
                {
                    SafeShowCompileError();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        private void SafeShowCompileError()
        {
            if (Application.OpenForms[0].InvokeRequired)
            {
                ShowCompileError d = new ShowCompileError(SafeShowCompileError);
                Application.OpenForms[0].Invoke(d, new object[] { });
                return;
            }
            if (MessageBox.Show("View compile errors?", "Compile Error", 
                MessageBoxButtons.YesNo) == DialogResult.Yes)
                Process.Start(this.logFile);
        }


        public void Decompile()
        {
        }


        public void CreateHxt(TreeNodeCollection nodeCollection, Hxt hxt, Content contentDataSet)
        {
            bool opened = false; // Keep track of opening or closing of TOC entries in the .hxt

            foreach (TreeNode node in nodeCollection)
            {
                if (node.Checked == true)
                {
                    MtpsNode mtpsNode = node.Tag as MtpsNode;

                    DataRow row = contentDataSet.Tables[TableNames.ITEM].Rows.Find(mtpsNode.TargetAssetId);
                    string Url;

                    //if (Int32.Parse(row["Size"].ToString()) == 0)
                    //    Url = null;
                    //else
                        Url = Path.Combine(hxsSubDir,
                            row[ColumnNames.CONTENTID].ToString() + ApplicationStrings.FILE_EXTENSION_HTM);


                    hxt.WriteStartNode(mtpsNode.Title, Url);

                    opened = true;
                }
                if (node.Nodes.Count != 0 || node.Tag != null)
                {
                    CreateHxt(node.Nodes, hxt, contentDataSet);
                }
                if (opened)
                {
                    opened = false;
                    hxt.WriteEndNode();
                }
            }

        }

        void CreateHxks(string baseFileName)
        {
            // HACK: What in the sweet name of the most holy is going on here??
            Hxk hxk = new Hxk(baseFileName, "A", hxsDir);
            hxk = new Hxk(baseFileName, "B", hxsDir);
            hxk = new Hxk(baseFileName, "F", hxsDir);
            hxk = new Hxk(baseFileName, "K", hxsDir);
            hxk = new Hxk(baseFileName, "N", hxsDir);
            hxk = new Hxk(baseFileName, "S", hxsDir);
        }


        // Includes stoplist and stylesheet
        void WriteExtraFiles()
        {
            WriteExtraFile("Classic.css");

            // TODO: Locate stop lists for other locales and add them to the project.
            WriteExtraFile("msdnFTSstop_Unicode.stp");


            WriteExtraFile("green-left.jpg");
            WriteExtraFile("green-middle.jpg");
            WriteExtraFile("green-right.jpg");

        }

        void WriteExtraFile(string filename)
        {
            using (Stream resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("PackageThis.Assets." + filename))
            using (FileStream fs = new FileStream(Path.Combine(hxsDir, filename), FileMode.Create, FileAccess.Write))
            {
                try
                {
                    int b;
                    while ((b = resourceStream.ReadByte()) != -1)
                    {
                        fs.WriteByte((byte) b);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    Debugger.Break();
                }
            }
        }


        public void Transform(string contentId, string metadataXml, string annotationsXml,
            string versionId, string docTitle, Content contentDataSet)
        {
            XsltArgumentList arguments = new XsltArgumentList();
            Link link = new Link(contentDataSet, links);
            XmlDocument metadata = new XmlDocument();
            XmlDocument annotations = new XmlDocument();

            string filename = Path.Combine(withinHxsDir, contentId + ApplicationStrings.FILE_EXTENSION_HTM);

            string xml;
            if (File.Exists(filename))
            {
                StreamReader sr = new StreamReader(filename);
                xml = sr.ReadToEnd();
                sr.Close();
            }
            else  //Default to Simple headers only
            {
                xml = "<div class=\"topic\" xmlns=\"http://www.w3.org/1999/xhtml\">" + Environment.NewLine +
                      "  <div class=\"majorTitle\">" + docTitle + "</div>" + Environment.NewLine +
                      "  <div class=\"title\">" + docTitle + "</div>" + Environment.NewLine +
                      "  <div id=\"mainSection\">" + Environment.NewLine +
                      "    <div id=\"mainBody\">" + Environment.NewLine +
                      "      <p></p>" + Environment.NewLine +                //RWC: Too difficult to add child link list like MSDN Online. Just leave blank.
                      "    </div>" + Environment.NewLine +
                      "  </div>" + Environment.NewLine +
                      "</div>" + Environment.NewLine;
            }



            metadata.LoadXml(metadataXml);
            annotations.LoadXml(annotationsXml);

            // Do transform

            arguments.AddParam("metadata", "", metadata.CreateNavigator());
            arguments.AddParam("annotations", "", annotations.CreateNavigator());
            arguments.AddParam("version", "", versionId);
            arguments.AddParam("locale", "", locale);

            arguments.AddExtensionObject("urn:Link", link);


            TextReader tr = new StringReader(xml);
            XmlReader xr = XmlReader.Create(tr);

            using (StreamWriter sw = new StreamWriter(filename, false, Encoding.UTF8))
            {
                try
                {
                    xform.Transform(xr, arguments, sw);

                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    Debugger.Break();
                }
            }


        }





    }

}
