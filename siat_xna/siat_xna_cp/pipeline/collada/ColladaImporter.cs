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

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using siat.pipeline.collada.elements;
using siat.pipeline.collada.elements.fx;


namespace siat.pipeline.collada
{
    [ContentImporter(".dae", CacheImportedData = false, DefaultProcessor = "Siat XNA COLLADA Processor", DisplayName = "Siat XNA COLLADA Importer")]
    public sealed class ColladaImporter : ContentImporter<ColladaCOLLADA>
    {
        #region ContentImporter implementations
        public override ColladaCOLLADA Import(string aFilename, ContentImporterContext aContext)
        {
            ColladaCOLLADA ret;
            ColladaDocument.Load(aFilename, out ret);

            List<string> messages = ColladaDocument.LoggedMessages;
            int count = messages.Count;

            if (count > 0)
            {
                string headerMessage = "--------------------------------" + Environment.NewLine + 
                    "Warnings after COLLADA document import: \"" + aFilename + "\"" + Environment.NewLine +
                    "--------------------------------";

                aContext.Logger.LogImportantMessage(headerMessage);

                for (int i = 0; i < count; i++)
                {
                    aContext.Logger.LogImportantMessage(messages[i]);
                }
            }

            return ret;
        }
        #endregion
    }
}
