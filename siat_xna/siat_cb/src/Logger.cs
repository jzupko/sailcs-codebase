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

using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace siat.src
{
    public class Logger : ILogger
    {
        public const string kAcceptedMessageSender = "BuildContent";

        #region Private members
        private List<string> mErrors = new List<string>();
        private MessageHandler mMessageHandler = null;
        private string mParameters = string.Empty;
        private LoggerVerbosity mVerbosity = LoggerVerbosity.Diagnostic;

        private void _HandleError(object aSender, BuildErrorEventArgs e)
        {
            mErrors.Add(e.Message);
        }

        void _HandleMessage(object aSender, BuildMessageEventArgs e)
        {
            if (mMessageHandler != null)
            {
                if (e != null && e.SenderName == kAcceptedMessageSender)
                {
                    mMessageHandler(e.Message);
                }
            }
        }
        #endregion

        public delegate void MessageHandler(string e);

        public Logger() : this(null) { }
        public Logger(MessageHandler aHandler)
        {
            mMessageHandler = aHandler;
        }

        public string[] Errors { get { return mErrors.ToArray(); } }

        public void Initialize(IEventSource eventSource)
        {
            if (eventSource != null)
            {
                eventSource.ErrorRaised += _HandleError;
                eventSource.MessageRaised += _HandleMessage;
            }
        }

        public void Shutdown() {}
        public string Parameters { get { return mParameters; } set { mParameters = value; } }
        public LoggerVerbosity Verbosity { get { return mVerbosity; } set { mVerbosity = value; } }
    }
}
