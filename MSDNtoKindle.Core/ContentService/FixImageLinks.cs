using System;
using System.Xml;

namespace PackageThis.ContentService
{
    public static class FixImageLinks
    {

     
           private  const string IMGTAG = "img";
           private const string SRCTAG = "src";

        public static String XmlFixLinks(string xml, string[] imageFiles)
        {
            var xmldoc = new XmlDocument();
            xmldoc.LoadXml(xml);
            var imgNodes = xmldoc.GetElementsByTagName(IMGTAG);    //get all <img> nodes

            foreach (XmlNode node in imgNodes)
            {
                if (node == null || node.Name != IMGTAG || node.Attributes.Count < 2)
                    continue;

                var srcValue = node.Attributes[SRCTAG].Value;
                var filename = FindFullFilename(srcValue, imageFiles);

                // Src= link already set correctly?

                if (String.IsNullOrEmpty(srcValue) == false && srcValue == filename)
                    continue;

                // Found a better match?

                if (filename.Length != 0)
                {
                    node.Attributes[SRCTAG].Value = srcValue;
                    continue;
                }

                // Need to look in the preceding comment <!-- .. ImageName="bingo" .. -->

                var lastNode = node.PreviousSibling;
                if (lastNode != null && lastNode.NodeType == XmlNodeType.Comment)
                {
                    var imageName = ExtractImageNameFromComment(lastNode.Value);  // pass full comment //<!-- .. ImageName="bingo" .. -->
                    filename = FindFullFilename(imageName, imageFiles);
                    if (filename.Length != 0)
                    {
                        node.Attributes[SRCTAG].Value = filename;    //found a filename
                        continue;
                    }
                }

                // No match? Use first img in list -- Changes are it's the right one!

                //node.Attributes["src"].Value = imageFiles[0];
            }

            return xmldoc.OuterXml;
        }


        private static String FindFullFilename(String nameFragment, string[] imageFiles)
        {
            if (nameFragment.Length != 0)
                foreach (String iname in imageFiles)
                    if (iname != null && iname.IndexOf(nameFragment) >= 0)
                        return iname;
            return string.Empty;
        }

        private static String ExtractImageNameFromComment(String sComment)
        {
            const string attrName = "ImageName=\"";
            int p1 = sComment.IndexOf(attrName);
            if (p1 >= 0)
            {
                p1 = p1 + attrName.Length;      //first char after opening "
                int p2 = sComment.IndexOf('"', p1);    //closing "
                if (p2 > p1)
                    return sComment.Substring(p1, p2 - p1);
            }
            return string.Empty;
        }

    }
}