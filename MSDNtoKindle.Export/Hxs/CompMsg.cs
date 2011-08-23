// Copyright (c) Microsoft Corporation.  All rights reserved.
//

using System;
using System.IO;

namespace PackageThis.Export.Hxs
{
    class CompMsg : IHxCompError
    {
        private IProgressReporter progressReporter;
        private StreamWriter writer = null;
        private int cError;
        private int cFatal;
        private int cWarn;
        private int cInfo;

        public bool Abort = false;

        public CompMsg(IProgressReporter progressReporter, String logFile)
        {
            this.progressReporter = progressReporter;
            Abort = false;

            cError = 0;
            cFatal = 0;
            cWarn = 0;
            cInfo = 0;

            if (File.Exists(logFile))
                File.Delete(logFile);
            writer = new StreamWriter(logFile, true, System.Text.Encoding.UTF8);

        }


        public void ReportMessage(HxCompErrorSeverity Severity, string DescriptionString)
        {
            progressReporter.ProgressMessage(DescriptionString);

            string status = "";
            if (Severity == HxCompErrorSeverity.HxCompErrorSeverity_Error)
            {
                status = "Error: ";
                cError++;
            }
            else if (Severity == HxCompErrorSeverity.HxCompErrorSeverity_Fatal)
            {
                status = "Fatal: ";
                cFatal++;
            }
            else if (Severity == HxCompErrorSeverity.HxCompErrorSeverity_Warning)
            {
                status = "Warning: ";
                cWarn++;
            }
            else //if (Severity == HxCompErrorSeverity.HxCompErrorSeverity_Information)
            {
                status = "Info: ";
                cInfo++;
            }

            Log(status + DescriptionString);
 
        }

        public void ReportError(string TaskItemString, string Filename, int nLineNum, int nCharNum, HxCompErrorSeverity Severity, string DescriptionString)
        {
            //Console.ForegroundColor = ConsoleColor.Red;
            //Console.WriteLine(DescriptionString);
            //Console.ResetColor();

            ReportMessage(Severity, DescriptionString); 
        }

        public HxCompStatus QueryStatus()
        {
            //return new MSHelpCompiler.HxCompStatus();
            if (Abort)
                return HxCompStatus.HxCompStatus_Cancel;
            else
                return HxCompStatus.HxCompStatus_Continue;
        }

        //=== Not part of Hx IHxCompError interface ==

        public void Log(string text)
        {
            if (writer != null)
            {
                writer.WriteLine(text);
            }
        }

        public void SaveLog()
        {
            if (writer != null)
            {
                Log("");
                Log(String.Format("Done - Total Warnings: {0}, Errors: {1}, Fatal: {2}", cWarn.ToString(), cError.ToString(), cFatal.ToString() ));

                writer.Flush();
                writer.Close();
                writer = null;
            }
        }

        public int ErrorCount
        {
            get { return cError + cFatal; }
        }

        public int WarnCount
        {
            get { return cWarn; }
        }


    }



}
