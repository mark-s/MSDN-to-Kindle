// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Web;

// Because the hhc file is not a valid xml file, we have to write tags directly.

namespace PackageThis.Export.Chm
{
    public class Hhc : IDisposable
    {
        private bool _disposed;
        private StreamWriter _writer;
        static readonly String crlf = Environment.NewLine;

        // Constructor
        public Hhc(string filePath, string locale)
        {
            var codePage = new CultureInfo(locale).TextInfo.ANSICodePage;

            var encoding = Encoding.GetEncoding(codePage);
            
            _disposed = false;

            _writer = new StreamWriter(filePath, false, encoding);
            _writer.WriteLine("<!DOCTYPE HTML PUBLIC \"-//IETF//DTD HTML//EN\"/>" + crlf +
                "<HTML>" + crlf +
                "<HEAD>" + crlf +
                "<META HTTP-EQUIV=\"Content Type\" CONTENT=\"text/html; CHARSET={0}\">" + crlf +
                "<meta name=\"GENERATOR\" content=\"Package This\" />" + crlf +
                "<!-- Sitemap 1.0 -->" + crlf +
                "</HEAD><BODY>", encoding.WebName);

        }

        // Destructor
        ~Hhc() { Dispose(false); }

        // Free resources immediately
        protected void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                }
                // Close file
                _writer.Close();
                _writer = null;
                
                // Disposed
                _disposed = true;
                
            }
        }

        public void WriteStartNode(string title, string url)
        {
            title = HttpUtility.HtmlEncode(title);
            url = HttpUtility.HtmlEncode(url);

            _writer.WriteLine("<UL>" + crlf + "<LI><OBJECT type=\"text/sitemap\"/>");
                

            if (string.IsNullOrEmpty(title) != true)
            {
                _writer.WriteLine("<param name=\"Name\" value=\"" + title + "\">");
                
            }

            if (string.IsNullOrEmpty(url) != true)
            {
                _writer.WriteLine("<param name=\"Local\" value=\"" + url + "\">");

            }
            else
            {
                // creates a folder icon
                _writer.WriteLine("<param name=\"ImageNumber\" value=\"1\"/>");

            }

            _writer.WriteLine("</OBJECT>");
            _writer.Flush();
        }

        public void WriteEndNode()
        {
            _writer.WriteLine("</UL>");
            _writer.Flush();
        }

        public void Close()
        {
            // Write footer

            _writer.WriteLine("</BODY></HTML>");

            _writer.Flush();

            // Free resources
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        public void Dispose()
        {
            Close();
        }
    }
}
