// Copyright (c) Microsoft Corporation.  All rights reserved.
//
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Web;
using System.Windows.Forms;
using System.Xml;
using PackageThis.ContentService;
using PackageThis.Core.Constants;

// Version variables in this code are collection + "." + version, but ContentItem requires a
// version and collection, eg. version="10", collection="MSDN"

namespace PackageThis.Core
{
    public class AppController
    {
        private readonly string _rawDir;
        private readonly TreeView _tocControl;
        private readonly string _workingDir;



        // static private StreamWriter sw;

        public Dictionary<string, string> Links = new Dictionary<string, string>();

        public AppController(string topNode, string locale, string version, TreeView tocControl, string workingDir)
        {
            _tocControl = tocControl;
            _workingDir = workingDir;

            _rawDir = Path.Combine(workingDir, "raw");
            Directory.CreateDirectory(_rawDir);

            var contentItem = lookupTOCNode(topNode, locale, version);

            processNodeList(contentItem, tocControl.Nodes);
        }

        private ContentItem lookupTOCNode(string contentIdentifier, string locale, string version)
        {
            string[] splitVersion = version.Split(new[] {'.'});

            var contentItem = new ContentItem(contentIdentifier, locale, splitVersion[1], splitVersion[0], ApplicationStrings.APPLICATION_NAME);

            contentItem.Load(false, false);

            return contentItem;
        }


        private void processNodeList(ContentItem contentItem, TreeNodeCollection tnCollection)
        {
            var xmlDocument = new XmlDocument();
            var nsm = new XmlNamespaceManager(xmlDocument.NameTable);
            nsm.AddNamespace("toc", "urn:mtpg-com:mtps/2004/1/toc");
            nsm.AddNamespace("mtps", "http://msdn2.microsoft.com/mtps");
            nsm.AddNamespace("asp", "http://msdn2.microsoft.com/asp");
            nsm.AddNamespace("mshelp", "http:/msdn.microsoft.com/mshelp");

            if (string.IsNullOrEmpty(contentItem.Toc)) return;

            xmlDocument.LoadXml(contentItem.Toc);

            var nodes = xmlDocument.SelectNodes("/toc:Node/toc:Node", nsm);
            
            if (nodes == null) return;

            foreach (XmlNode node in nodes)
            {
                var title = GetAttribute(node.Attributes["toc:Title"]);

                var target = HttpUtility.UrlDecode(GetAttribute(node.Attributes["toc:Target"]));

                var targetLocale = GetAttribute(node.Attributes["toc:TargetLocale"]);
                var targetVersion = GetAttribute(node.Attributes["toc:TargetVersion"]);

                var subTree = HttpUtility.UrlDecode(GetAttribute(node.Attributes["toc:SubTree"]));
                var subTreeVersion = GetAttribute(node.Attributes["toc:SubTreeVersion"]);
                var subTreeLocale = GetAttribute(node.Attributes["toc:SubTreeLocale"]);
                var isPhantom = GetAttribute(node.Attributes["toc:IsPhantom"]);

                if (isPhantom != "true" &&
                    title != "@PhantomNode" &&
                    title != "@NoTitle" &&
                    string.IsNullOrEmpty(title) != true &&
                    string.IsNullOrEmpty(target) != true)
                {
                    TreeNode treeNode = tnCollection.Add(title);

                    var mtpsNode = new MtpsNode(subTree, subTreeLocale, subTreeVersion,contentItem.ContentId, target, targetLocale, targetVersion, title);

                    treeNode.Tag = mtpsNode;


                    // Mark nodes red that point outside this server
                    if (mtpsNode.External)
                    {
                        treeNode.ForeColor = Color.Red;

                        //treeNode.NodeFont = new System.Drawing.Font(tocControl.Font, System.Drawing.FontStyle.Italic);
                    }


                    if (subTree != null)
                    {
                        // Add a + as the title so any node with subnodes is expandable.
                        // Only load the subnodes when user expands this node.
                        // We rely on Tag == null rather than Text == "+" in case
                        // there really is a node with a title of "+".
                        treeNode.Nodes.Add("+");
                    }
                }
                else
                {
                    if (subTree != null)
                    {
                        // TODO: add a ContentItem constructor that takes a combined 
                        // version string: MSDN.10, Office.12, etc.

                        string[] splitVersion = subTreeVersion.Split(new[] {'.'});
                        var childContentItem = new ContentItem(subTree, subTreeLocale,splitVersion[1], splitVersion[0], ApplicationStrings.APPLICATION_NAME);
                        childContentItem.Load(false);

                        processNodeList(childContentItem, tnCollection);
                    }
                }
            }
        }


        private string GetAttribute(XmlAttribute attribute)
        {
            return (attribute == null ? null : attribute.Value);
        }

        public void ExpandNode(TreeNode node)
        {

            //if (node.Nodes[0].Tag != null) return;

            var mtpsNode = node.Tag as MtpsNode;

            try
            {
                var contentItem = lookupTOCNode(mtpsNode.NavAssetId, mtpsNode.NavLocale,mtpsNode.NavVersion);
                processNodeList(contentItem, node.Nodes);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debugger.Break();
            }

            node.Nodes.Remove(node.Nodes[0]); // This removes the node labeled "+"
        }

        public void UncheckNodes(TreeNode node)
        {
            // Events are created even if the checked state doesn't change.
            // That confuses the event handler because it assumes that the
            // event is only fired on a state change.


            if (node.Checked)
                node.Checked = false;

            foreach (TreeNode currentNode in node.Nodes)
            {
                if (currentNode.Checked)
                    currentNode.Checked = false;

                UncheckNodes(currentNode);
            }
        }


        public String GetDocShortId(TreeNode node) //use this when the short id is missing
        {
            var mtpsNode = node.Tag as MtpsNode;

            // RWC: Inherited this system with some weird bugs. Sometimes a valid node can return a null contentId
            // Often in these situations the AssetId is Content GUID which can be used to pull down an mtps file and parse for the short id

            string[] split = mtpsNode.TargetAssetId.Split(new[] {'-'});
            if (split.Length == 5 && split[1].Length == 4 && split[2].Length == 4 && split[3].Length == 4)
                //it's a guid format we can look this up
            {
                MtpsFile.ReadData(mtpsNode.TargetAssetId, mtpsNode.TargetVersion, mtpsNode.TargetLocale);
                    //download mtps file and grab shortId
                if (MtpsFile.ShortId != "")
                    return MtpsFile.ShortId;
            }

            // Try the tradition method

            string[] splitVersion = mtpsNode.TargetVersion.Split(new[] {'.'});
            var contentItem = new ContentItem(ContentIdentifier.ASSETID + mtpsNode.TargetAssetId, mtpsNode.TargetLocale,splitVersion[1], splitVersion[0], ApplicationStrings.APPLICATION_NAME);
            try
            {
                contentItem.Load(true);
            }
            catch (Exception ex )
            {
                Debug.WriteLine(ex.Message);
                Debugger.Break();
            }

            return contentItem.ContentId ?? (contentItem.ContentId = "");
        }


        public bool WriteContent(TreeNode node, Content contentDataSet)
        {
            DataRow row;
            var mtpsNode = node.Tag as MtpsNode;

            Debug.Assert(mtpsNode != null, "mtpsNode != null");

            string[] splitVersion = mtpsNode.TargetVersion.Split(new[] {'.'});

            var contentItem = new ContentItem(ContentIdentifier.ASSETID + mtpsNode.TargetAssetId, mtpsNode.TargetLocale, splitVersion[1], splitVersion[0], ApplicationStrings.APPLICATION_NAME);

            try
            {
                contentItem.Load(true);

                // RWC: This is a HACK -- There are a few pages where the ContentId returns as null even though it's a valid page
                //
                if (String.IsNullOrEmpty(contentItem.ContentId))
                {
                    contentItem.ContentId = GetDocShortId(node);
                }
            }
            catch
            {
                node.ForeColor = Color.Red;
                return false; // tell the event handler to reject the click.
            }

            if (contentDataSet.Tables[TableNames.ITEM].Rows.Find(mtpsNode.TargetAssetId) == null)
            {
                //Issue#14155: Sometimes there can be missing values that cause exceptions. 
                //Lets's try and bluff our way through
                if (string.IsNullOrEmpty(contentItem.ContentId))
                {
                    node.ForeColor = Color.Red; //Set tree node red to flag problem
                    contentItem.ContentId = "PackageThis-" + mtpsNode.TargetAssetId;
                        //Create a repeatable ID using the Asset ID
                }
                // If we get no meta/search or wiki data, plug in NOP data so we can limp along (this usually comes with null contentId above)
                if (string.IsNullOrEmpty(contentItem.Metadata))
                    contentItem.Metadata = "<se:search xmlns:se=\"urn:mtpg-com:mtps/2004/1/search\" />";
                if (string.IsNullOrEmpty(contentItem.Annotations))
                    contentItem.Annotations = "<an:annotations xmlns:an=\"urn:mtpg-com:mtps/2007/1/annotations\" />";

                row = contentDataSet.Tables[TableNames.ITEM].NewRow();
                row[ColumnNames.CONTENTID] = contentItem.ContentId;
                row["Title"] = mtpsNode.Title;
                row["VersionId"] = mtpsNode.TargetVersion;
                row[ColumnNames.ASSETID] = mtpsNode.TargetAssetId;
                row["Pictures"] = contentItem.NumImages;
                row["Size"] = contentItem.Xml == null ? 0 : contentItem.Xml.Length;
                row["Metadata"] = contentItem.Metadata;
                row["Annotations"] = contentItem.Annotations;

                contentDataSet.Tables[TableNames.ITEM].Rows.Add(row);
            }
            if (contentDataSet.Tables[TableNames.ITEMINSTANCE].Rows.Find(node.FullPath) == null)
            {
                row = contentDataSet.Tables[TableNames.ITEMINSTANCE].NewRow();
                row[ColumnNames.CONTENTID] = contentItem.ContentId;
                row["FullPath"] = node.FullPath;
                contentDataSet.Tables[TableNames.ITEMINSTANCE].Rows.Add(row);
            }
            foreach (string imageFilename in contentItem.ImageFilenames)
            {
                row = contentDataSet.Tables[TableNames.PICTURE].NewRow();
                row[ColumnNames.CONTENTID] = contentItem.ContentId;
                row["Filename"] = imageFilename;
            }


            if (string.IsNullOrEmpty(contentItem.Links) == false)
            {
                var linkDoc = new XmlDocument();
                var nsm = new XmlNamespaceManager(linkDoc.NameTable);

                nsm.AddNamespace("k", "urn:mtpg-com:mtps/2004/1/key");
                nsm.AddNamespace("mtps", "urn:msdn-com:public-content-syndication");

                linkDoc.LoadXml(contentItem.Links);

                XmlNodeList nodes = linkDoc.SelectNodes("//mtps:link", nsm);

                foreach (XmlNode xmlNode in nodes)
                {
                    var assetIdNode = xmlNode.SelectSingleNode("mtps:assetId", nsm);
                    var contentIdNode = xmlNode.SelectSingleNode("k:contentId", nsm);

                    if (assetIdNode == null || contentIdNode == null)
                        continue;

                    var assetId = assetIdNode.InnerText;
                    var contentId = contentIdNode.InnerText;

                    if (string.IsNullOrEmpty(assetId) == false)
                    {
                        // Remove "assetId:" from front
                        assetId = HttpUtility.UrlDecode(assetIdNode.InnerText.Remove(0, ContentIdentifier.ASSETID.Length));

                        if (Links.ContainsKey(assetId) == false)
                        {
                            Links.Add(assetId, contentId);
                        }
                    }
                }
            }

            contentItem.Write(_rawDir);

            return true;
        }


        public void RemoveContent(TreeNode node, Content contentDataSet)
        {

            if (node.Tag == null) return;

            var row = contentDataSet.Tables[TableNames.ITEMINSTANCE].Rows.Find(node.FullPath);

            if (row == null) return;

            var parentRow = row.GetParentRow("FK_Item_ItemInstance");

            contentDataSet.Tables[TableNames.ITEMINSTANCE].Rows.Remove(row);

            var count = parentRow.GetChildRows("FK_Item_ItemInstance").Length;

            if (count != 0) return;

            foreach (var fileName in Directory.GetFiles(_rawDir, parentRow[ColumnNames.CONTENTID] + "*"))
            {
                File.Delete(fileName);
            }
            contentDataSet.Tables[TableNames.ITEM].Rows.Remove(parentRow);
        }

        //public void CreateChm(string chmFile, string title, string locale, Content contentDataSet)
        //{
        //    var chm = new Chm(_workingDir, title,chmFile, locale, _tocControl.Nodes, contentDataSet, Links);

        //    chm.Create();

        //    var progressForm = new ExportProgressForm(chm, chm.ExpectedLines);

        //    progressForm.ShowDialog();
        //}

        //public void CreateMshc(string mshcFile, string locale, Content contentDataSet, string vendorName, string prodName, string bookName)
        //{
        //    var mshc = new Mshc(_workingDir, mshcFile, locale, _tocControl.Nodes, contentDataSet, Links, vendorName, prodName, bookName);

        //    var progressForm = new ExportProgressForm(mshc, mshc.ExpectedLines);

        //    progressForm.ShowDialog();
        //}

        //public void CreateHxs(string hxsFile, string title, string copyright, string locale, Content contentDataSet)
        //{
        //    var hxs = new HxS(_workingDir, hxsFile,title, copyright, locale,_tocControl.Nodes,contentDataSet,Links);

        //    hxs.Create();

        //    var hxsProgressForm = new ExportProgressForm(hxs, hxs.expectedLines);

        //    hxsProgressForm.ShowDialog();
        //}


    }

}