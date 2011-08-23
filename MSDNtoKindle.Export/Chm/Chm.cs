// Copyright (c) Microsoft Corporation.  All rights reserved.
//

using System.Reflection;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Xsl;
using PackageThis.Core;
using PackageThis.Core.Constants;
using PackageThis.Export.Hxs;

namespace PackageThis.Export.Chm
{
    class Chm : ICompilableHelp
    {
        private readonly string _withinChmDir;
        private readonly string _rawDir;
        private readonly string _chmDir;
        private const string CHM_SUB_DIR = "html";

        private readonly string _baseName;

        private readonly string _title;
        private readonly FileInfo _chmFile;
        private readonly string _locale;

        private readonly Content _contentDataSet;
        private readonly TreeNodeCollection _nodes;
        private readonly Dictionary<string, string> _links;

        private XslCompiledTransform _xform;

        private string _defaultPage = string.Empty;

        public int ExpectedLines { get; private set; }

        // {0} = filename with full path (c:\file.chm)
        // {1} = filename without extension
        // {2} = LCID
        // {3} = default page
        // {4} = title
        static readonly String crlf = Environment.NewLine;
        static readonly string _template = "[OPTIONS]" + crlf +
            "Auto Index=Yes" + crlf +
            "Compatibility=1.1 or later" + crlf +
            "Compiled file={0}" + crlf +
            "Contents file={1}.hhc" + crlf +
            "Create CHI file=No" + crlf +
            "Default Window=msdn" + crlf +
            "Default topic={3}" + crlf +
            "Display compile progress=Yes" + crlf +
            "Enhanced decompilation=Yes" + crlf +
            "Error log file={1}.log" + crlf +
            "Full-text search=Yes" + crlf +
            "Index file={1}.hhk" + crlf +
            "Language=0x{2:x}" + crlf + // in hex, eg. 0x0409
            "Title={4}" + crlf +
            "Binary TOC=Yes" + crlf +   // this enables MSDN style Next\Prev button (although stops merging if anyone wants to do that later)
            "Binary Index=Yes" + crlf + crlf +

            "[WINDOWS]" + crlf +
            "msdn=\"{4}\",\"{1}.hhc\",\"{1}.hhk\",\"{3}\",\"{3}\",,\"MSDN Library\",,\"MSDN Online\",0x73520,240,0x60387e,[30,30,770,540],0x30000,,,,,,0" + crlf + crlf +

            "[FILES]" + crlf +
            "green-left.jpg" + crlf +
            "green-middle.jpg" + crlf +
            "green-right.jpg" + crlf + crlf +

            "[INFOTYPES]" + crlf;


        private string PopulateTemplate(FileInfo filenameAndPath, string lcid, string defaultPage, string title, string newLineCharacter, bool enableBinaryTOC)
        {
            // {0} = filename with full path (c:\file.chm)
            // {1} = filename without extension
            // {2} = LCID
            // {3} = default page
            // {4} = title
            // {5} = enable binary TOC
            // {6} = newline character
            string template = "[OPTIONS]{6}" +
                                   "Auto Index=Yes{6}" +
                                   "Compatibility=1.1 or later{6}" +
                                   "Compiled file={0}{6}" +
                                   "Contents file={1}.hhc{6}" +
                                   "Create CHI file=No{6}" +
                                   "Default Window=msdn{6}" +
                                   "Default topic={3}{6}" +
                                   "Display compile progress=Yes{6}" +
                                   "Enhanced decompilation=Yes{6}" +
                                   "Error log file={1}.log{6}" +
                                   "Full-text search=Yes{6}" +
                                   "Index file={1}.hhk{6}" +
                                   "Language=0x{2:x}{6}" + // in hex, eg. 0x0409
                                   "Title={4}{6}" +
                                   "Binary TOC={5}{6}" +   // 'YES' enables MSDN style Next\Prev button (although stops merging if anyone wants to do that later)
                                   "Binary Index=Yes{6}{6}" +

                                   "[WINDOWS]{6}" +
                                   "msdn=\"{4}\",\"{1}.hhc\",\"{1}.hhk\",\"{3}\",\"{3}\",,\"MSDN Library\",,\"MSDN Online\",0x73520,240,0x60387e,[30,30,770,540],0x30000,,,,,,0{6}{6}" +

                                   "[FILES]{6}" +
                                   "green-left.jpg{6}" +
                                   "green-middle.jpg{6}" +
                                   "green-right.jpg{6}{6}" +

                                   "[INFOTYPES]{6}";

            var filenameWithoutExtension = filenameAndPath.Name;
            string binaryTOC = enableBinaryTOC ? "Yes" : "No";

            return string.Format(template, filenameAndPath.FullName, filenameWithoutExtension, lcid, defaultPage, title, binaryTOC, newLineCharacter);

        }




        public Chm(string workingDir, string title, FileInfo chmFile, string locale, TreeNodeCollection nodes, Content contentDataSet, Dictionary<string, string> links)
        {
            _title = title;
            _chmFile = chmFile;
            _locale = locale;
            _nodes = nodes;
            _contentDataSet = contentDataSet;
            _links = links;

            _rawDir = Path.Combine(workingDir, "raw");

            // The source shouldn't be hidden away. If an error happens (likely) the user needs to check logs etc.
            _chmDir = GetUniqueDir(chmFile);
            _withinChmDir = Path.Combine(_chmDir, CHM_SUB_DIR);
            _baseName = Path.GetFileNameWithoutExtension(chmFile.FullName);

            var resourceStream = typeof(AppController).Assembly.GetManifestResourceStream("PackageThis.Assets.chm.xslt");

            Debug.Assert(resourceStream != null, "resourceStream != null");

            var transformFile = XmlReader.Create(resourceStream);

            _xform = new XslCompiledTransform(true);

            _xform.Load(transformFile);


        }

        private string GetUniqueDir(FileInfo targetFile)
        {

            var tempPath = Path.GetTempPath();

            var basedir = Path.ChangeExtension(targetFile.Name, "ProjectSource");

            var dir = Path.Combine(tempPath, basedir);

            return dir;   //return a folder that does not exist
        }



        private void CreateOutputDirectoryStructure(string chmDirectoryTop, string chmDirectoryChild)
        {
            if (Directory.Exists(chmDirectoryTop))
            {
                Directory.Delete(chmDirectoryTop, true);
            }

            Directory.CreateDirectory(chmDirectoryTop);
            Directory.CreateDirectory(chmDirectoryChild);
        }





        public void Create()
        {

            CreateOutputDirectoryStructure(_chmDir, _withinChmDir);

            foreach (var fileName in Directory.GetFiles(_rawDir))
            {
                File.Copy(fileName, Path.Combine(_withinChmDir, Path.GetFileName(fileName)), true);
            }

            var hhk = new Hhk(Path.Combine(_chmDir, _baseName + ".hhk"), _locale);

            foreach (DataRow row in _contentDataSet.Tables[TableNames.ITEM].Rows)
            {
                Transform(row[ColumnNames.CONTENTID].ToString(),
                    row["Metadata"].ToString(),
                    row["Annotations"].ToString(),
                    row["VersionId"].ToString(),
                    row["Title"].ToString(),
                    _contentDataSet);

                var document = new XmlDocument();
                document.LoadXml(row["Metadata"].ToString());

                var nsManager = new XmlNamespaceManager(document.NameTable);
                nsManager.AddNamespace("se", "urn:mtpg-com:mtps/2004/1/search");
                nsManager.AddNamespace("xhtml", "http://www.w3.org/1999/xhtml");

                var xmlNodes = document.SelectNodes("//xhtml:meta[@name='MSHKeywordK']/@content", nsManager);

                if (xmlNodes != null)
                    foreach (XmlNode xmlNode in xmlNodes)
                    {
                        hhk.Add(xmlNode.InnerText,
                                Path.Combine(CHM_SUB_DIR, row[ColumnNames.CONTENTID].ToString() + ApplicationStrings.FILE_EXTENSION_HTM),
                                row["Title"].ToString());
                    }
            }

            hhk.Save();


            // Create TOC
            using (var hhc = new Hhc(Path.Combine(_chmDir, _baseName + ".hhc"), _locale))
            {
                CreateHhc(_nodes, hhc, _contentDataSet);
            }

            var lcid = new CultureInfo(_locale).LCID;

            using (var fileStream = new FileStream(Path.Combine(_chmDir, _baseName + ".hhp"), FileMode.Create, FileAccess.Write, FileShare.None))
            using (var streamWriter = new StreamWriter(fileStream))
            {
                streamWriter.Write(_template, _chmFile, _baseName, lcid, _defaultPage, _title);
            }


            WriteExtraFiles();

            var numFiles = Directory.GetFiles(_chmDir, "*", SearchOption.AllDirectories).Length;

            ExpectedLines = numFiles + 15;

        }



        // Compile() is called by a background Thread in ProgressForm so be careful
        public void Compile(IProgressReporter progressReporter)
        {

            // Use registry to find the compiler and invoke as a separate process.
            const string key = @"HKEY_CURRENT_USER\Software\Microsoft\HTML Help Workshop";

            var install = (string)Registry.GetValue(key, "InstallDir", null);
            var hhcExe = Path.Combine(install, "hhc.exe");

            if (install == null || File.Exists(hhcExe) == false)
            {
                throw new ApplicationException("Please install the HTML Help Workshop.");
            }


            var compileProcess = new Process
                                     {
                                         StartInfo =
                                             {
                                                 FileName = "\"" + Path.Combine(install, "hhc.exe") + "\"",
                                                 Arguments = "\"" + _baseName + ".hhp" + "\"",
                                                 CreateNoWindow = true,
                                                 WorkingDirectory = _chmDir,
                                                 UseShellExecute = false,
                                                 RedirectStandardOutput = true
                                             }
                                     };


            compileProcess.Start();

            var streamReader = compileProcess.StandardOutput;

            // The UI doesn't update because stdout isn't flushed, so for now, just toss
            // the message and call the progressReporter with the same
            // message.
            while (streamReader.EndOfStream != true)
            {
                streamReader.ReadLine();
                {
                    progressReporter.ProgressMessage("Compiling");
                }
            }

            compileProcess.Close();

        }

        private void CreateHhc(TreeNodeCollection nodeCollection, Hhc hhc, Content contentDataSet)
        {
            var opened = false; // Keep track of opening or closing of TOC entries

            foreach (TreeNode node in nodeCollection)
            {
                Debug.Assert(node != null, "node is null");

                if (node.Checked)
                {
                    var mtpsNode = node.Tag as MtpsNode;

                    Debug.Assert(mtpsNode != null, "mtpsNode != null");

                    var row = contentDataSet.Tables[TableNames.ITEM].Rows.Find(mtpsNode.TargetAssetId);


                    var url = Path.Combine(CHM_SUB_DIR, row[ColumnNames.CONTENTID] + ApplicationStrings.FILE_EXTENSION_HTM);

                    // Save the first page we see in the TOC as the default page as required by the chm.
                    if (_defaultPage == null)
                        _defaultPage = url;


                    hhc.WriteStartNode(mtpsNode.Title, url);

                    opened = true;
                }

                if (node.Nodes.Count != 0 || node.Tag != null)
                {
                    CreateHhc(node.Nodes, hhc, contentDataSet);
                }

                if (opened)
                {
                    opened = false;
                    hhc.WriteEndNode();
                }
            }

        }

        private void Transform(string contentId, string metadataXml, string annotationsXml, string versionId, string docTitle, Content contentDataSet)
        {

            var annotations = new XmlDocument();

            var filename = Path.Combine(_withinChmDir, contentId + ApplicationStrings.FILE_EXTENSION_HTM);

            string xml;
            if (File.Exists(filename))
            {
                using (var sr = new StreamReader(filename))
                {
                    xml = sr.ReadToEnd();
                    sr.Close();
                }
            }
            else  //Probably a node file that simply lists its children -- We will deal with this at a later date
            {
                xml = "<div class=\"topic\" xmlns=\"http://www.w3.org/1999/xhtml\">" + crlf +
                      "  <div class=\"majorTitle\">" + docTitle + "</div>" + crlf +
                      "  <div class=\"title\">" + docTitle + "</div>" + crlf +
                      "  <div id=\"mainSection\">" + crlf +
                      "    <div id=\"mainBody\">" + crlf +
                      "      <p></p>" + crlf +                //RWC: Too difficult to add child link list like MSDN Online. Just leave blank.
                      "    </div>" + crlf +
                      "  </div>" + crlf +
                      "</div>" + crlf;
            }

            var codePage = new CultureInfo(_locale).TextInfo.ANSICodePage;

            // We use these fallbacks because &nbsp; is unknown under DBCS like Japanese
            // and translated to ? by default.
            var encoding = Encoding.GetEncoding(codePage, new EncoderReplacementFallback(" "), new DecoderReplacementFallback(" "));

            var metadata = new XmlDocument();
            metadata.LoadXml(metadataXml);
            annotations.LoadXml(annotationsXml);

            Debug.Assert(contentDataSet != null, "contentDataSet is null !");

            var link = new Link(contentDataSet, _links);
            var arguments = new XsltArgumentList();

            arguments.AddParam("metadata", "", metadata.CreateNavigator());
            arguments.AddParam("annotations", "", annotations.CreateNavigator());
            arguments.AddParam("version", "", versionId);
            arguments.AddParam("locale", "", _locale);
            arguments.AddParam("charset", "", encoding.WebName);

            arguments.AddExtensionObject("urn:Link", link);

            TextReader textReader = new StringReader(xml);
            using (var xmlReader = XmlReader.Create(textReader))
            using (var streamWriter = new StreamWriter(filename, false, encoding))
            {
                try
                {
                    _xform.Transform(xmlReader, arguments, streamWriter);

                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    Debugger.Break();
                }
            }
        }



        // TODO: ReFactor the following as they are very similar to the .hxs equiv.

        // Includes stoplist and stylesheet
        private void WriteExtraFiles()
        {
            WriteExtraFile("Classic.css");

            WriteExtraFile("green-left.jpg");
            WriteExtraFile("green-middle.jpg");
            WriteExtraFile("green-right.jpg");

        }

        private void WriteExtraFile(string filename)
        {
            using (var manifestResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("PackageThis.Assets." + filename))
            using (var fileStream = new FileStream(Path.Combine(_chmDir, filename), FileMode.Create, FileAccess.Write))
            {
                int readByte;

                Debug.Assert(manifestResourceStream != null, "manifestResourceStream != null");

                while ((readByte = manifestResourceStream.ReadByte()) != -1)
                {
                    fileStream.WriteByte((byte)readByte);
                }
            }

        }

    }
}
