//
// Copyright (c) 2009 Joseph A. Zupko
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 

// #define CLIENT_USAGE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using siat.src;

namespace siat
{
    public static class Program
    {
        public const string kInArg = "-in";
        public const string kLogFile = "siat_cb.log";
        public const string kDefaultInPath = "..\\..\\sail_demo\\media";

        #region Private members
        [DllImport("user32.dll")]
        private static extern int SetForegroundWindow(IntPtr hWnd);
            
        private static void Go(string aInDir)
        {
            Process cur = Process.GetCurrentProcess();
            IntPtr curHandle = cur.MainWindowHandle;

            Process[] p = Process.GetProcessesByName(cur.ProcessName);
            if (p.Length <= 1)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm(aInDir));
            }
            else
            {
                for (int i = 0; i < p.Length; i++)
                {
                    if (p[i].MainWindowHandle != curHandle)
                    {
                        SetForegroundWindow(p[i].MainWindowHandle);
                        break;
                    }
                }
            }
        }
        #endregion

        [STAThread]
        public static void Main(string[] aArgs)
        {
#if !DEBUG || CLIENT_USAGE
            try
            {
#endif

            string inDir = kDefaultInPath;

            int count = (aArgs.Length - 1);
            for (int i = 0; i < count; i++)
            {
                if (aArgs[i].ToLower().Trim() == kInArg)
                {
                    inDir = aArgs[i + 1];
                    break;
                }
            }

            Go(inDir);
            #if !DEBUG || CLIENT_USAGE
            }
            catch (Exception e)
            {
                string caption = "Exception: Please send \"" + kLogFile + "\" to the appropriate parties.";
                string msg = "Exception: \"" + e.Message + "\"" + System.Environment.NewLine;
                msg += "Source: \"" + e.Source + "\"" + System.Environment.NewLine;
                msg += "Target site: \"" + e.TargetSite + "\"" + System.Environment.NewLine;
                msg += "Stack trace:" + System.Environment.NewLine + e.StackTrace;

                using (System.IO.StreamWriter errorWriter = new StreamWriter(kLogFile))
                {
                    errorWriter.Write(msg);
                }

                System.Windows.Forms.MessageBox.Show(msg, caption, 
                    System.Windows.Forms.MessageBoxButtons.OK, 
                    System.Windows.Forms.MessageBoxIcon.Error);
            }
#endif
            }
    }
}
