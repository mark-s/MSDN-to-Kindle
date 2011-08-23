// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Text;
using System.Xml;

namespace PackageThis.Export.Hxs
{
    public class Hxt
    {
        private bool Disposed;
        private XmlWriter writer;

        // Constructor
        public Hxt(string filePath, Encoding encoding)
        {

            Disposed = false;

            XmlWriterSettings settings = new XmlWriterSettings { Indent = true, NewLineChars = Environment.NewLine, Encoding = encoding, };
            writer = XmlWriter.Create(filePath, settings);

            // Write header
            writer.WriteStartDocument();
            writer.WriteDocType("HelpTOC", null, "MS-Help://Hx/Resources/HelpTOC.DTD", null);
            writer.WriteStartElement("HelpTOC");
            writer.WriteAttributeString("DTDVersion", "1.0");
            writer.Flush();

        }

        // Destructor
        ~Hxt() { Dispose(false); }

        // Free resources immediately
        protected void Dispose(bool Disposing)
        {
            if (!Disposed)
            {
                if (Disposing)
                {
                }
                // Close file
                writer.Close();
                writer = null;
                
                // Disposed
                Disposed = true;
                
            }
        }

        public void WriteStartNode(string title, string url)
        {

            writer.WriteStartElement("HelpTOCNode");

            if (string.IsNullOrEmpty(title) != true)
            {
                writer.WriteAttributeString("Title", title);
            }
            if (string.IsNullOrEmpty(url) != true)
            {
                writer.WriteAttributeString("Url", url);
            }
            writer.Flush();
        }

        public void WriteEndNode()
        {
            writer.WriteEndElement();
            writer.Flush();
        }

        public void Close()
        {
            // Write footer
            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Flush();
            // Free resources
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
