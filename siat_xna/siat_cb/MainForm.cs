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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using siat.src;

namespace siat
{
    public partial class MainForm : Form
    {
        public const int kMaxLines = 80;

        #region Private members
        private string mInDirectory = string.Empty;
        private volatile Thread mWorker = null;

        private void _Handler(string e)
        {
            if (mWorker != null)
            {
                if (Output.InvokeRequired) { Invoke(new Logger.MessageHandler(_Handler), new object[] { e }); }
                else
                {
                    string[] lines = Output.Lines;
                    int newLength = (lines.Length >= kMaxLines) ? kMaxLines : lines.Length + 1;

                    string[] n = new string[newLength];
                    Array.Copy(lines, 0, n, 1, (newLength - 1));
                    n[0] = e;

                    Output.Lines = n;
                }
            }
        }

        private void _HandleClose(object aSender, CancelEventArgs e)
        {
            if (mWorker != null)
            {
                e.Cancel = true;
                MessageBox.Show("Cannot exit during a build.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        void _HandleKeyUp(object aSender, KeyEventArgs e)
        {
            if (e != null && e.KeyCode == Keys.F5)
            {
                e.Handled = true;
                RunMenuItem_Click(this, new EventArgs());
            }
        }

        private void _Worker(object obj)
        {
            try
            {
                _Handler("Building...");
                Builder.Process(mInDirectory, _Handler);
            }
            catch (Exception e) { _Handler(e.Message); }

            _Handler("Done.");
            mWorker = null;
        }
        #endregion

        public MainForm() : this(Directory.GetCurrentDirectory()) {}
        public MainForm(string aInDirectory)
        {
            mInDirectory = Path.GetFullPath(aInDirectory);
            InitializeComponent();
            InFolder.Text = mInDirectory;
            Closing += _HandleClose;
            
            KeyPreview = true;
            KeyUp += _HandleKeyUp;
        }

        private void FolderMenuItem_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fd = new FolderBrowserDialog();
            fd.Description = "Input directory";
            fd.SelectedPath = mInDirectory;

            if (fd.ShowDialog() == DialogResult.OK)
            {
                mInDirectory = fd.SelectedPath;
                mInDirectory = Path.GetFullPath(mInDirectory);
                InFolder.Text = mInDirectory;
            }
        }

        private void RunMenuItem_Click(object sender, EventArgs e)
        {
            if (mWorker == null)
            {
                mWorker = new Thread(_Worker);
                menuStrip1_Click(this, new EventArgs());
                mWorker.Start();
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void menuStrip1_Click(object send, EventArgs e)
        {
            exitToolStripMenuItem.Enabled = (mWorker == null);
            RunMenuItem.Enabled = (mWorker == null);
        }
    }
}