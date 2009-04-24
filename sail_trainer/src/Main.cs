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

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;

using siat;
using siat.render;
using siat.scene;
using siat.pipeline;
using siat.pipeline.collada;
using siat.pipeline.collada.elements;

namespace sail
{
    /// <summary>
    /// Example program that illustrates rendering without Cells.
    /// </summary>
    /// <remarks>
    /// This example program is also the training component for Joe's lighting system (jaz147@psu.edu).
    /// </remarks>
    public static class Program
    {
        #region Private members
        private static bool mbTraining = false;
        private static bool mbStepping = true;
        #endregion

        public const int kWidth = 256;
        public const int kHeight = 256;
        public const float kMaximumYaw = MathHelper.PiOver2;
        public const string kStandardEffectFile = "..\\..\\siat_xna\\siat_xna_cp\\impl\\collada_effect.h";

        public const string kModel = "woman";
        public const int kModelAnimationIndex = 10;
        public const string kModelLightData = "woman.dat";
        public static readonly Vector3 kModelWorldCenter = Vector3.Up * -37.0f;

        public const string kLogFile = "sail_trainer.log";
        public const float kNearPlaneScale = 4.38e-4f;
        public const float kFarPlaneScale = 2.0f;

        public static sail.LightLearner Learner;
        public static CameraNode Camera = new CameraNode(null);
        public static float mRigDistance = 0.0f;
        public static LightNode KeyLight = new LightNode();
        public static LightNode FillLight = new LightNode();
        public static sail.LightExtractorImage ImageData = new sail.LightExtractorImage();
        public static SceneNode Model;
        public static SceneNodePoser Poser;
        public static ResolveTexture2D ResolveTexture;
        public static sail.ImageIlluminationMetrics IdealSettings;
        public static sail.ThreePointSettings MotSettings;
        public static sail.ThreePointSettings ThreePointSettings;

        public static void KeyHandler(KeyState aState, Keys aKey)
        {
            if (aState == KeyState.Up && aKey == Keys.Escape)
            {
                Siat.Singleton.Exit();
            }
        }

        public static void _CreateFlat(float aFarPlane)
        {
            List<CompilerMacro> macros = new List<CompilerMacro>();
            macros.Add(PipelineUtilities.NewMacro("EMISSION_COLOR", "siat_EmissionColor"));

            CompiledEffect ce = Effect.CompileEffectFromFile(kStandardEffectFile, macros.ToArray(), null, CompilerOptions.None, TargetPlatform.Windows);
            SiatEffect effect = new SiatEffect("skybox_effect", new Effect(Siat.Singleton.GraphicsDevice, ce.GetEffectCode(), CompilerOptions.None, null));

            SiatMaterial material = new SiatMaterial();
            material.AddParameter("siat_EmissionColor", Color.Magenta.ToVector4());

            MeshPart meshPart = Siat.Singleton.UnitBoxMeshPart;

            Matrix transform = Matrix.CreateScale(100.0f) * Matrix.CreateTranslation(aFarPlane * 0.98f * Vector3.Forward);

            MeshPartNode node = new MeshPartNode();
            node.MeshPart = meshPart;
            node.Material = material;
            node.Effect = effect;
            node.WorldTransform = transform;
            node.Parent = Model;
        }

        public static void OnLoadHandler()
        {
            Siat siat = Siat.Singleton;

            // Loads the model content into the global ContentManager.
            Model = siat.Content.Load<SceneNode>(kModel);

            // Updates animations to be at their first frame.
            SceneNode.TickRetrieveQueue();
            Model.Update(null, ref Utilities.kIdentity, true);

            // SceneNodePoser is a helper object. It allows posing of arbitrary scene graphs. This is
            // normally handled automagically by Cell.FrustumPose() and Cell.LightingPose() when
            // nodes are part of cells.
            Poser = new SceneNodePoser(Model);

            float fov = MathHelper.ToRadians(60.0f);
            float radius = Model.WorldBounding.Radius;
            float twoRadius = 2.0f * radius;
            float nearPlane = kNearPlaneScale * twoRadius;
            float farPlane = 2.0f * twoRadius;
            mRigDistance = (nearPlane + ((float)Math.Sqrt(2.0f) * radius / (float)Math.Tan(fov * 0.5f)));

            // Set up the projection transforms. Even when not using cells the Camera needs to be active
            // so it knows to push its projection and view transforms after an update.
            Camera.bActive = true;
            Camera.ProjectionTransform = Matrix.CreatePerspectiveFieldOfView(fov, (float)kWidth / (float)kHeight, nearPlane, farPlane);

            // Setup the lights as children of the model. This causes the light's local position to be
            // relative to the model. It also automatically updates and poses the light whenever the
            // model is updated and posed.
            FillLight.Parent = Model;
            FillLight.Light.Type = LightType.Directional;
            KeyLight.Parent = Model;
            KeyLight.Light.Type = LightType.Point;

            ImageData.Width = siat.GraphicsDevice.PresentationParameters.BackBufferWidth;
            ImageData.Height = siat.GraphicsDevice.PresentationParameters.BackBufferHeight;
            ImageData.Format = siat.GraphicsDevice.PresentationParameters.BackBufferFormat;
            ImageData.Data = new byte[ImageData.Width * ImageData.Height * Utilities.GetStride(ImageData.Format)];

            // Create a new light learner for training.
            Learner = new sail.LightLearner();

            MotSettings.Fill = 0.0f;
            MotSettings.KeyRoll = new Degree(90.0f);
            MotSettings.KeyYaw = new Degree(90.0f);

            // Create a hack to clear to magenta.
            if (RenderRoot.bDeferredLighting) { _CreateFlat(farPlane); }
            else { RenderRoot.ClearColor = sail.ImageData.kMaskColor; }

            if (!System.IO.File.Exists(kModelLightData))
            {
                Learner.Init(ref ThreePointSettings);
                mbTraining = true;

                // Create the texture for resholving the back-buffer;
                ResolveTexture = new ResolveTexture2D(siat.GraphicsDevice,
                    ImageData.Width, ImageData.Height, 1, ImageData.Format);
            }
            else
            {
                sail.ThreePointSettings settings = new sail.ThreePointSettings(new Degree(180.0f), 0.5f, new Degree(45.0f));
                Learner.Load(kModelLightData);
                IdealSettings = Learner.Get(ref settings);
                ThreePointSettings = settings;
            }
        }

        public static void OnUnloadHandler()
        {
            if (ResolveTexture != null)
            {
                ResolveTexture.Dispose();
                ResolveTexture = null;
            }
        }

        public static void OnDrawHandler()
        {
            if (mbTraining)
            {
                Siat siat = Siat.Singleton;
                GraphicsDevice graphics = siat.GraphicsDevice;
                graphics.ResolveBackBuffer(ResolveTexture);

                ResolveTexture.GetData<byte>(ImageData.Data);
                mbTraining = Learner.Tick(ref ImageData, ref ThreePointSettings);

                if (!mbTraining)
                {
                    Learner.Save(kModelLightData);
                    ThreePointSettings = MotSettings;
                }
            }
            else if (mbStepping)
            {
                Learner.Step(ref IdealSettings, ref ThreePointSettings, ref MotSettings, (float)Siat.Singleton.Time.ElapsedGameTime.TotalSeconds);
            }
        }

        public static void OnPoseHandler()
        {
            Poser.StartPose();
        }

        public static void OnUpdateHandler()
        {
            #region Calculate new camera, key, and fill settings.
            // KeyLight.Light.LightDiffuse = new Vector3(ThreePointSettings.KeyIntensity);
            KeyLight.Light.LightDiffuse = new Vector3(1.0f);
            FillLight.Light.LightDiffuse = new Vector3(ThreePointSettings.Fill);

            // float cameraPitch = ThreePointSettings.CameraPitch.ToRadians().Value;
            // float cameraYaw = ThreePointSettings.CameraYaw.ToRadians().Value;
            float keyYaw = ThreePointSettings.KeyYaw.ToRadians().Value;
            float keyRoll = ThreePointSettings.KeyRoll.ToRadians().Value;
            float fillYaw = keyYaw - MathHelper.PiOver2;
            float fillRoll = keyRoll;

            Matrix translation = Matrix.CreateTranslation(Vector3.Backward * mRigDistance);

            // Matrix cameraTransform = translation * Matrix.CreateFromAxisAngle(Vector3.Right, cameraPitch) * Matrix.CreateFromAxisAngle(Vector3.Up, cameraYaw);
            Matrix cameraTransform = translation * Matrix.CreateFromAxisAngle(Vector3.Right, 0.0f) * Matrix.CreateFromAxisAngle(Vector3.Up, 0.0f);
            Matrix keyTransform = translation * Matrix.CreateFromAxisAngle(Vector3.Up, keyYaw) * Matrix.CreateFromAxisAngle(Vector3.Backward, keyRoll);
            Matrix fillTransform = translation * Matrix.CreateFromAxisAngle(Vector3.Up, fillYaw) * Matrix.CreateFromAxisAngle(Vector3.Backward, fillRoll);

            Camera.WorldTransform = cameraTransform;
            KeyLight.WorldTransform = keyTransform;
            FillLight.WorldTransform = fillTransform;
            #endregion

            Camera.Update(null, ref Utilities.kIdentity, false);
            Model.WorldPosition = kModelWorldCenter;
            Model.Update(null, ref Utilities.kIdentity, false);
        }

        public static void Go()
        {
            Input input = Input.Singleton;
            input.AddKeyCallback(Keys.Escape, KeyHandler);

            Siat siat = Siat.Singleton;
            siat.Resize(kWidth, kHeight, false);
            siat.bConsoleEnabled = false;
            siat.OnDrawEnd += OnDrawHandler;
            siat.OnPoseBegin += OnPoseHandler;
            siat.OnLoading += OnLoadHandler;
            siat.OnUnloading += OnUnloadHandler;
            siat.OnUpdateEnd += OnUpdateHandler;
            siat.Run();
        }

        public static void Main(string[] aArgs)
        {


#if !DEBUG || CLIENT_USAGE
            try
            {
#endif
            Go();
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
