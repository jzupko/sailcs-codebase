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

using Microsoft.Build.BuildEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace siat.src
{
    /// <summary>
    /// Builder that wraps MS build environment.
    /// </summary>
    /// <remarks>
    /// Based on WinFormsContentLoading project: http://www.ziggyware.com/readarticle.php?article_id=183
    /// </remarks>
    public static class Builder
    {
#if DEBUG
        public const string kOutPath = "..\\debug";
#else
        public const string kOutPath = "..\\release";
#endif
        public const string kProjectFilename = "media.contentproj";
        public static readonly string[] kXnaAssemblies = new string[]
            {
                "Microsoft.Xna.Framework.Content.Pipeline.EffectImporter",
                "Microsoft.Xna.Framework.Content.Pipeline.TextureImporter",
            };
        public const string kXnaVersionString = ", Version=2.0.0.0, PublicKeyToken=6d5c3888ef60e27d";

        public const string kSiatAssembly = "..\\..\\siat_xna\\siat_xna_cp\\siat_xna_cp.csproj";
        public const string kSiatProject = "{BBB45A1B-7E60-470D-88FC-588573DF4500}";
        public const string kSiatName = "siat_xna_cp";

        public static readonly string[] kExtensions = new string[] { ".dae", ".jpg", ".tga", ".png" };
        public static readonly string[] kImporters = new string[] { "ColladaImporter", "", "", "" };
        public static readonly string[] kProcessors = new string[] { "ColladaProcessor", "", "", "" };

        #region Private members
        private static void _AddInputs(string aInDirectory, Project aProject)
        {
            if (Directory.Exists(aInDirectory))
            {
                string[] files = Directory.GetFiles(aInDirectory, "*", SearchOption.AllDirectories);

                foreach (string e in files)
                {
                    string ext = Path.GetExtension(e).ToLower();
                    int index = Array.IndexOf(kExtensions, ext);

                    if (index >= 0)
                    {
                        string filename = Path.GetFileName(e);
                        string fullPath = Path.GetFullPath(e);
                        string name = _ExtractName(aInDirectory, e);

                        BuildItem itm = aProject.AddNewItem("Compile", fullPath);
                        itm.SetMetadata("Link", filename);
                        itm.SetMetadata("Name", name);

                        if (!string.IsNullOrEmpty(kImporters[index])) { itm.SetMetadata("Importer", kImporters[index]); }
                        if (!string.IsNullOrEmpty(kProcessors[index])) { itm.SetMetadata("Processor", kProcessors[index]); }
                    }
                }
            }
        }

        public static string _ExtractName(string aInDirectory, string aFilename)
        {
            string ret = aFilename;
            int index = aFilename.LastIndexOf(aInDirectory);
            if (index >= 0) { ret = aFilename.Substring(index + aInDirectory.Length); }

            ret = ret.TrimStart(Path.DirectorySeparatorChar);

            ret = Utilities.RemoveExtension(ret);
            ret = ret.Trim();

            return ret;
        }
        #endregion

        public static void Process(string aInDirectory, Logger.MessageHandler aHandler)
        {
            Logger logger = new Logger(aHandler);
            Engine engine = new Engine(RuntimeEnvironment.GetRuntimeDirectory());
            engine.RegisterLogger(logger);

            Project proj = new Project(engine);
            proj.FullFileName = Path.Combine(kOutPath, kProjectFilename);
#if DEBUG
            proj.SetProperty("Configuration", "Debug");
#else
            proj.SetProperty("Configuration", "Release");
#endif
            proj.SetProperty("ContentRootDirectory", Utilities.kMediaRoot);
            proj.SetProperty("OutputPath", kOutPath);
            proj.SetProperty("XnaPlatform", "Windows");
            proj.SetProperty("XnaFrameworkVersion", "v2.0");

            foreach (string e in kXnaAssemblies) { proj.AddNewItem("Reference", e); }

            {
                BuildItem itm = proj.AddNewItem("ProjectReference", kSiatAssembly);
                itm.SetMetadata("Project", kSiatProject);
                itm.SetMetadata("Name", kSiatName);
            }

            proj.AddNewImport("$(MSBuildExtensionsPath)\\Microsoft\\XNA Game Studio\\v2.0\\Microsoft.Xna.GameStudio.ContentPipeline.targets", null);

            _AddInputs(aInDirectory, proj);

            if (!Directory.Exists(kOutPath)) { Directory.CreateDirectory(kOutPath); }
            
            string errors = string.Empty;

            try
            {
                if (!proj.Build()) { errors = string.Join(Environment.NewLine, logger.Errors); }
            }
            catch (Exception e)
            {
                errors = e.Message +
                    string.Join(Environment.NewLine, logger.Errors);
            }

            if (errors != string.Empty) { throw new Exception(errors); }
        }

        public static void Process(string aInDirectory)
        {
            Process(aInDirectory, null);
        }
    }
}
