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

#define EXPERIMENT_VERSION
// #define DEBUG_CHARACTER_BODY

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

using jz;
using jz.physics;
using jz.physics.narrowphase;

namespace sail
{
    public static class Program
    {
        public const string kLogFile = "siat_xna_app.log";
        public const float kNearPlaneScale = 4.38e-3f;
        public const float kFarPlaneScale = 1.0f;

        public const string kAttachedMode = "Attached";
        public const string kAutoMode = "Auto";
        public const string kNaturalMode = "Natural";

        public const string kEnabled = "Enabled";
        public const string kDisabled = "Disabled";

#if EXPERIMENT_VERSION
        public const int kFullscreenWidth = 800;
        public const int kFullscreenHeight = 600;
#endif

        public static readonly string[] kLightNames = new string[]
            { "1930_room\\SIAT_room_0013_lights_spotLight1",
              "1930_room\\SIAT_room_0013_lights_spotLight2",
              "1930_room\\SIAT_room_0013_lights_spotLight3",
              "1930_room\\SIAT_room_0013_lights_point_room1",
              "1930_room\\SIAT_room_0013_lights_point_room2",
              "1930_room\\SIAT_room_0013_lights_point_room3"
            };

        public static readonly LightNode[] kLights = new LightNode[kLightNames.Length];

        public static readonly Matrix kCamera1 = Matrix.CreateRotationY(-0.5f * MathHelper.PiOver2) * Matrix.CreateTranslation(-0.25f, 1.0f, -7.0f);
        public static readonly Matrix kCamera2 = Matrix.CreateRotationY(-0.6f * MathHelper.Pi) * Matrix.CreateTranslation(-0.5f, 0.9f, -2.7f);
        public static readonly Matrix kCamera3 = Matrix.CreateRotationY(-MathHelper.PiOver2) * Matrix.CreateTranslation(-0.5f, 0.7f, 2.5f);

        public static readonly Vector3 kModel1 = new Vector3(1.0f, -0.14f, -7.7f);
        public static readonly Vector3 kModel2 = new Vector3(2.0f, -0.14f, -2.0f);
        public static readonly Vector3 kModel3 = new Vector3(1.0f, -0.14f, 2.7f);

        public static float kImageWidthFactor = 0.20f;
        public const float kGuiActiveTime = 3600.0f;

        public const string kExample1Filename = "exemplar1";
        public const string kExample2Filename = "exemplar2";
        public const string kExample3Filename = "exemplar3";

        public const string kArrowModel = "arrow";
        public const string kModel = "woman";
        public const string kModelLightData = "woman.dat";
        public const float kMoveRate = 0.6f;
        public const string kScene = "1930_room\\SIAT_room_0013_lights.dae";

        #region Private members
#if !EXPERIMENT_VERSION
        private static bool msbDisableSelfShadowing = false;
        private const float kDofFactor = 1.0f;
#endif

        private static bool msbAltDown = false;
        private static bool msbDoFullscreenToggle = false;

        private static bool mbUp = false;
        private static bool mbLeft = false;
        private static bool mbRight = false;
        private static bool mbDown = false;

        private static float mGuiActive = 0.0f;

        private static Siat.GuiElement mGuiElement;

        private static SceneNode Model;
        private static float ModelScale = 1.0f;
        private static float TargetHeight = 0.0f;
        private static ThreePointLighting Tpl;
        private static sail.ImageIlluminationMetrics CurrentMetrics;
        private static int CurrentMetricsNumber = 1;
        private static string CurrentMode = kAutoMode;

        private static bool mbArrowsEnabled = true;
        private static SceneNode KeyArrow;
        private static SceneNode FillArrow;

        private static CharacterBody PhysicalCharacterBody;
        private static PhysicsSceneNode PhysicalNode;

        private static Texture2D kExample1 = null;
        private static Texture2D kExample2 = null;
        private static Texture2D kExample3 = null;

        private static sail.ImageIlluminationMetrics kMetrics1;
        private static sail.ImageIlluminationMetrics kMetrics2;
        private static sail.ImageIlluminationMetrics kMetrics3;

        public static int LightCueNumber = 1;

        public static readonly Light[] LightCue1 = new Light[]
            {
                new Light(MathHelper.PiOver2, 13.0f, Vector3.UnitY, 5.0f * new Vector3(0.973831f, 1.0f, 0.922f), 5.0f * new Vector3(0.973831f, 1f, 0.922f), LightType.Spot),
                new Light(MathHelper.PiOver2, 13.0f, Vector3.UnitY, 5.0f * new Vector3(1.0f, 0.99065f, 0.94f), 5.0f * new Vector3(1.0f, 0.99065f, 0.94f), LightType.Spot),
                new Light(MathHelper.ToRadians(80.0f), 13.0f, Vector3.UnitY, 5.0f * new Vector3(0.973831f, 1.0f, 0.922f), 5.0f * new Vector3(0.973831f, 1.0f, 0.922f), LightType.Spot),

                new Light(MathHelper.Pi, 1.0f, Vector3.UnitX, new Vector3(0.5f), new Vector3(0.5f), LightType.Point),
                new Light(MathHelper.Pi, 1.0f, Vector3.UnitX, new Vector3(0.5f), new Vector3(0.5f), LightType.Point),
                new Light(MathHelper.Pi, 1.0f, Vector3.UnitX, new Vector3(0.25f), new Vector3(0.25f), LightType.Point),
            };

        public static readonly Light[] LightCue2 = new Light[]
            {
                new Light(MathHelper.PiOver2, 13.0f, Vector3.UnitY, Vector3.Zero, Vector3.Zero, LightType.Spot),
                new Light(MathHelper.PiOver2, 13.0f, Vector3.UnitY, Vector3.Zero, Vector3.Zero, LightType.Spot),
                new Light(MathHelper.ToRadians(80.0f), 13.0f, Vector3.UnitY, Vector3.Zero, Vector3.Zero, LightType.Spot),

                new Light(MathHelper.Pi, 1.0f, Vector3.UnitZ, new Vector3(0, 0.15f, 0.45f), Vector3.Zero, LightType.Point),
                new Light(MathHelper.Pi, 1.0f, Vector3.UnitZ, new Vector3(0, 0.15f, 0.45f), Vector3.Zero, LightType.Point),
                new Light(MathHelper.Pi, 1.0f, Vector3.UnitZ, new Vector3(0, 0.15f, 0.45f), Vector3.Zero, LightType.Point),
            };

        public static readonly Light[] LightCue3 = new Light[]
            {
                new Light(MathHelper.PiOver2, 13.0f, Vector3.UnitZ, new Vector3(0.973831f, 1.0f, 0.922f), 5.0f * new Vector3(0.973831f, 1f, 0.922f), LightType.Spot),
                new Light(MathHelper.PiOver2, 13.0f, Vector3.UnitZ, new Vector3(1.0f, 0.99065f, 0.94f), 5.0f * new Vector3(1.0f, 0.99065f, 0.94f), LightType.Spot),
                new Light(MathHelper.ToRadians(80.0f), 13.0f, Vector3.UnitZ, new Vector3(0.973831f, 1.0f, 0.922f), 5.0f * new Vector3(0.973831f, 1.0f, 0.922f), LightType.Spot),

                new Light(MathHelper.Pi, 1.0f, Vector3.UnitY, 5.0f * new Vector3(0.87f, 0.64f, 0.17f), Vector3.Zero, LightType.Point),
                new Light(MathHelper.Pi, 1.0f, Vector3.UnitY, new Vector3(1.0f, 0.82f, 0.32f), Vector3.Zero, LightType.Point),
                new Light(MathHelper.Pi, 1.0f, Vector3.UnitY, 5.0f * new Vector3(0.87f, 0.64f, 0.17f), Vector3.Zero, LightType.Point),
            };

        private static Light[] msActiveLightTransition = null;

        private static float kLightTransitionStepSize = 0.1f;
        private static float kMinimumStep = 1e-1f;

        private static bool StepLight(LightNode aNode, Light aTarget)
        {
            float step = Utilities.Clamp((float)Siat.Singleton.Time.ElapsedGameTime.TotalSeconds * kLightTransitionStepSize, kMinimumStep, 1.0f);
            bool bDone = true;

            #region Falloff angle
            {
                float a = aNode.Light.FalloffAngleInRadians;
                float b = aTarget.FalloffAngleInRadians;

                while (a > (b + MathHelper.Pi)) { a -= MathHelper.TwoPi; }
                while (a < (b - MathHelper.Pi)) { a += MathHelper.TwoPi; }

                if (!Utilities.AboutEqual(a, b, Utilities.kLooseToleranceFloat))
                {
                    aNode.Light.FalloffAngleInRadians = MathHelper.Lerp(a, b, step);
                    bDone = false;
                }
            }
            #endregion

            #region Falloff exponent
            {
                float a = aNode.Light.FalloffExponent;
                float b = aTarget.FalloffExponent;

                if (!Utilities.AboutEqual(a, b, Utilities.kLooseToleranceFloat))
                {
                    aNode.Light.FalloffExponent = MathHelper.Lerp(a, b, step);
                    bDone = false;
                }
            }
            #endregion

            #region Attenuation
            {
                Vector3 a = aNode.Light.LightAttenuation;
                Vector3 b = aTarget.LightAttenuation;

                if (!Utilities.AboutEqual(a, b, Utilities.kLooseToleranceFloat))
                {
                    aNode.Light.LightAttenuation = Vector3.Lerp(a, b, step);
                    bDone = false;
                }
            }
            #endregion

            #region Diffuse
            {
                ColorHSLA a = ColorHSLA.From(aNode.Light.LightDiffuse);
                ColorHSLA b = ColorHSLA.From(aTarget.LightDiffuse);

                if (!a.AboutEqual(b, Utilities.kLooseToleranceFloat))
                {
                    ColorHSLA n = ColorHSLA.Lerp(a, b, step);
                    aNode.Light.LightDiffuse = ColorHSLA.ToVector3(n);
                    bDone = false;
                }
            }
            #endregion

            #region Specular
            {
                ColorHSLA a = ColorHSLA.From(aNode.Light.LightSpecular);
                ColorHSLA b = ColorHSLA.From(aTarget.LightSpecular);

                if (!a.AboutEqual(b, Utilities.kLooseToleranceFloat))
                {
                    ColorHSLA n = ColorHSLA.Lerp(a, b, step);
                    aNode.Light.LightSpecular = ColorHSLA.ToVector3(n);
                    bDone = false;
                }
            }
            #endregion

            return bDone;
        }

        private static bool StepLights(Light[] aTargets)
        {
            int count = kLights.Length;
            bool bDone = true;
            for (int i = 0; i < count; i++)
            {
                bDone = StepLight(kLights[i], aTargets[i]) && bDone;
            }

            return bDone;
        }

#if !EXPERIMENT_VERSION
        private static bool msbRightDown = false;
        private static int msMouseX = 0;
        private static int msMouseY = 0;
        private static Cell msPickedCell = null;
        private static PoseableNode msPickedNode = null;
        private static float msPickDepth = 0.0f;
        private static Vector3 msLocalPickPoint = Vector3.Zero;

        private static void PickingCallback(Cell c, PoseableNode n, float aDepth)
        {
            msPickedCell = c;
            msPickedNode = n;
            msPickDepth = aDepth;

            if (msPickedNode != null)
            {
                Viewport vp = Siat.Singleton.GraphicsDevice.Viewport;
                Vector3 p = new Vector3(msMouseX, msMouseY, msPickDepth);
                Vector3 wp = Utilities.UnProject(p, vp, Shared.InverseViewProjectionTransform);
                msLocalPickPoint = Vector3.Transform(wp, Matrix.Invert(msPickedNode.WorldTransform));
            }
        }

        private static void MouseButtonHandler(ButtonState aState, MouseButtons aButton)
        {
            if (aButton == MouseButtons.Right)
            {
                msbRightDown = (aState == ButtonState.Pressed);

                if (msbRightDown)
                {
                    Siat.Singleton.Pick(msMouseX, msMouseY, PickingCallback);
                }
                else
                {
                    msPickedCell = null;
                    msPickedNode = null;
                }
            }
        }

        private static void MouseMoveHandler(int x, int y)
        {
            msMouseX = x;
            msMouseY = y;
        }

        private static void MouseUpdateHandler()
        {
            if (msPickedNode != null)
            {
                Viewport vp = Siat.Singleton.GraphicsDevice.Viewport;
                Vector3 wp = Vector3.Transform(msLocalPickPoint, msPickedNode.WorldTransform);

                Vector3 wpp = Utilities.Project(wp, vp, Shared.ViewProjectionTransform);
                wpp.X = msMouseX; wpp.Y = msMouseY;

                Vector3 up = Utilities.UnProject(wpp, vp, Shared.InverseViewProjectionTransform);

                msPickedNode.WorldPosition += (up - wp);
            }
        }
#endif

        private static void KeyLightCueHandler(KeyState aState, Keys aKey)
        {
            if (aState == KeyState.Up)
            {
                if (aKey == Keys.D1) { msActiveLightTransition = LightCue1; LightCueNumber = 1; }
                else if (aKey == Keys.D2) { msActiveLightTransition = LightCue2; LightCueNumber = 2; }
                else if (aKey == Keys.D3) { msActiveLightTransition = LightCue3; LightCueNumber = 3; }
            }
        }

#if !EXPERIMENT_VERSION
        private static void KeyCameraHandler(KeyState aState, Keys aKey)
        {
            CameraNode camera = Siat.Singleton.ActiveCamera;

            if (aState == KeyState.Up && camera != null)
            {
                if (aKey == Keys.D7) { camera.WorldTransform = kCamera1; Model.WorldPosition = kModel1; }
                else if (aKey == Keys.D8) { camera.WorldTransform = kCamera2; Model.WorldPosition = kModel2; }
                else if (aKey == Keys.D9) { camera.WorldTransform = kCamera3; Model.WorldPosition = kModel3; }

                if (PhysicalCharacterBody != null) { PhysicalCharacterBody.Frame = new CoordinateFrame(PhysicalCharacterBody.Frame.Orientation, Model.WorldPosition); }
            }
        }
#endif

        private static void KeyLightingHandler(KeyState aState, Keys aKey)
        {
            if (aState == KeyState.Up)
            {
                if (aKey == Keys.Q) { CurrentMetrics = kMetrics1; mGuiElement.Texture = kExample1; CurrentMetricsNumber = 1; }
                else if (aKey == Keys.W) { CurrentMetrics = kMetrics2; mGuiElement.Texture = kExample2; CurrentMetricsNumber = 2; }
                else if (aKey == Keys.E) { CurrentMetrics = kMetrics3; mGuiElement.Texture = kExample3; CurrentMetricsNumber = 3; }

                mGuiActive = kGuiActiveTime;
                Tpl.Ideal = CurrentMetrics;
                if (Tpl.bDumb) { Tpl.Current = ThreePointLighting.Approximate(ref CurrentMetrics); }
            }
        }

        private static void KeyMoveHandler(KeyState aState, Keys aKey)
        {
            if (aKey == Keys.Up) { mbUp = (aState != KeyState.Up); }
            else if (aKey == Keys.Down) { mbDown = (aState != KeyState.Up); }
            else if (aKey == Keys.Left) { mbLeft = (aState != KeyState.Up); }
            else if (aKey == Keys.Right) { mbRight = (aState != KeyState.Up); }

            bool b = (mbUp || mbDown || mbLeft || mbRight);

            Model.Apply<AnimatedMeshPartNode>(SceneNode.ApplyType.RecurseDown, TreeNode<SceneNode>.ApplyStop.Delegate,
                delegate(AnimatedMeshPartNode e)
                {
                    e.AnimationControl.bPlay = b;
                    return false;
                });

        }

        private static void _UpdateArrows()
        {
            KeyArrow.Apply<PoseableNode>(SceneNode.ApplyType.RecurseDown,
                SceneNode.ApplyStop.Delegate, delegate(PoseableNode e)
                {
                    e.bEnablePosing = mbArrowsEnabled;
                    return false;
                });

            FillArrow.Apply<PoseableNode>(SceneNode.ApplyType.RecurseDown,
                SceneNode.ApplyStop.Delegate, delegate(PoseableNode e)
                {
                    e.bEnablePosing = mbArrowsEnabled;
                    return false;
                });
        }

        private static void KeyHandler(KeyState aState, Keys aKey)
        {
            if (aKey == Keys.LeftAlt || aKey == Keys.RightAlt) { msbAltDown = (aState == KeyState.Down); }
            else if (aKey == Keys.Enter)
            {
                if (aState == KeyState.Down && msbAltDown) { msbDoFullscreenToggle = true; }
                else if (aState == KeyState.Up && msbDoFullscreenToggle)
                {
                    msbDoFullscreenToggle = false;
                    if (Siat.Singleton.IsFullScreen) { Siat.Singleton.ToggleFullscreen(); }
                    else
                    {
#if EXPERIMENT_VERSION
                        Siat.Singleton.Resize(kFullscreenWidth, kFullscreenHeight, true);
#else
                        Siat.Singleton.ToggleFullscreen();
#endif
                    }
                }
            }
            else if (aState == KeyState.Up)
            {
                if (aKey == Keys.Escape) { Siat.Singleton.Exit(); }
                else if (aKey == Keys.H) { Siat.Singleton.bConsoleEnabled = !Siat.Singleton.bConsoleEnabled; }
                else if (aKey == Keys.A) { Tpl.bEnabled = true; Tpl.bDumb = false; CurrentMode = kAutoMode; _UpdateArrows(); }
                else if (aKey == Keys.S) { Tpl.bEnabled = false; CurrentMode = kNaturalMode; }
                else if (aKey == Keys.D)
                {
                    Tpl.Current = ThreePointLighting.Approximate(ref CurrentMetrics);
                    Tpl.bEnabled = true;
                    _UpdateArrows();
                    CurrentMode = kAttachedMode;
                }
                else if (aKey == Keys.Tab)
                {
#if !EXPERIMENT_VERSION
                    if (CurrentMode == kNaturalMode)
                    {
                        msbDisableSelfShadowing = !msbDisableSelfShadowing;

                        Model.Apply<MeshPartNode>(SceneNode.ApplyType.RecurseDown,
                            SceneNode.ApplyStop.Delegate, delegate(MeshPartNode e)
                            {
                                e.bExcludeFromShadowing = msbDisableSelfShadowing;
                                return false;
                            });
                    }
                    else
#else
                    if (CurrentMode != kNaturalMode)
#endif
                    {
                        mbArrowsEnabled = !mbArrowsEnabled;
                        _UpdateArrows();
                    }
                }
#if !EXPERIMENT_VERSION
                else if (aKey == Keys.X)
                {
                    if (CurrentMode == kAutoMode) { Tpl.bAlwaysStep = !Tpl.bAlwaysStep; }
                }
                else if (aKey == Keys.F1)
                {
                    RenderRoot.bDeferredLighting = !RenderRoot.bDeferredLighting;
                }
#endif
            }
        }

        private static void _MetricsHelper(Texture2D aTexture, out sail.ImageIlluminationMetrics arMetrics)
        {
            sail.LightExtractorImage image;

            image.Data = new byte[aTexture.Width * aTexture.Height * Utilities.GetStride(aTexture.Format)];
            image.Format = aTexture.Format;
            image.Height = aTexture.Height;
            image.Width = aTexture.Width;

            aTexture.GetData<byte>(image.Data);
            sail.LightingExtractor.ExtractLighting(ref image, out arMetrics);
        }

        private static void SetupPhysicalWorld()
        {
            Siat siat = Siat.Singleton;
            Cell cell = siat.ActiveCamera.Cell;

            SceneNode root = cell.WaitForRootSceneNode;
            root.Apply<PhysicsSceneNode>(SceneNode.ApplyType.RecurseDown, SceneNode.ApplyStop.First,
                delegate(PhysicsSceneNode e)
                {
                    PhysicalNode = e;
                    return true;
                });

            #region Add physical body for character
            if (PhysicalNode != null)
            {
                float radius = 0.22f * Model.WorldBounding.Radius;
                float height = Model.WorldBounding.Radius;
                Vector3 center = (Model.WorldBounding.Center - Model.WorldPosition) +
                    (Vector3.Up * 1.0f);

                PhysicalCharacterBody = new CharacterBody(radius, height, center);
                PhysicalCharacterBody.bDisableUp = true;
                PhysicalCharacterBody.CollidesWith = BodyFlags.kStatic;
                PhysicalCharacterBody.Type = BodyFlags.kKinematic;
                PhysicalCharacterBody.World = PhysicalNode.World;
            }
            #endregion
        }

        private static void _LightHelper(SceneNode e, int i)
        {
            kLights[i] = (LightNode)e;
        }

        private static void OnLoadHandler()
        {
            Siat siat = Siat.Singleton;
            Cell cell = Cell.GetCell(kScene);
            SceneNode root = cell.WaitForRootSceneNode;

            float aspectRatio = siat.GraphicsDevice.Viewport.AspectRatio;
            float diagonal = Utilities.GetDiagonal(cell.WorldBounding);

            float near = kNearPlaneScale * diagonal;
            float far = kFarPlaneScale * diagonal;

            CameraEditingNode camera = new CameraEditingNode(cell);
            camera.Parent = root;
            camera.ProjectionTransform = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver2, aspectRatio, near, far);
            camera.bActive = true;
            camera.MaxDistance = far * 0.5f;

            Model = siat.Content.Load<SceneNode>(kModel);
            Model.Update(null, ref Utilities.kIdentity, true);
            ModelScale = (1.0f / Model.WorldBounding.Radius) * 0.70f;

            Model.WorldTransform = Matrix.CreateScale(ModelScale) *
                Matrix.CreateRotationY(-MathHelper.PiOver2) * Matrix.CreateTranslation(kModel1);
            Model.Parent = root;
            Model.Update(camera.Cell, ref Utilities.kIdentity, false);

            camera.Update(camera.Cell, ref Utilities.kIdentity, false);
            TargetHeight = Model.WorldBounding.Radius * 2.0f * 0.90f;
            camera.ForceTarget = (Model.WorldPosition + (Vector3.Up * TargetHeight));
            camera.Distance = 1.0f;
            camera.Yaw = -MathHelper.PiOver2;

            kExample1 = siat.Content.Load<Texture2D>(kExample1Filename);
            kExample2 = siat.Content.Load<Texture2D>(kExample2Filename);
            kExample3 = siat.Content.Load<Texture2D>(kExample3Filename);

            mGuiElement.Color = Color.White;
            mGuiElement.Texture = kExample1;
            ResizeHandler();
            siat.OnResize += ResizeHandler;

            _MetricsHelper(kExample1, out kMetrics1);
            _MetricsHelper(kExample2, out kMetrics2);
            _MetricsHelper(kExample3, out kMetrics3);

            CurrentMetrics = kMetrics1;

            Tpl = new ThreePointLighting(cell, Model);
            Tpl.Learner.Load(kModelLightData);
            Tpl.Ideal = CurrentMetrics;
            Tpl.bEnabled = true;
            Tpl.RelativeCenter = (Model.WorldBounding.Center - Model.WorldPosition) + (Vector3.Up * 1.0f);
            camera.Update(camera.Cell, ref Utilities.kIdentity, false);
            Tpl.Update();

            mGuiActive = kGuiActiveTime;

            KeyArrow = siat.Content.Load<SceneNode>(kArrowModel);
            KeyArrow.LocalTransform = Matrix.CreateScale(0.30f);
            KeyArrow.Parent = Tpl.Key;
            FillArrow = KeyArrow.Clone(Tpl.Fill);

            Model.Apply<AnimatedMeshPartNode>(SceneNode.ApplyType.RecurseDown, TreeNode<SceneNode>.ApplyStop.Delegate,
                delegate(AnimatedMeshPartNode e)
                {
                    e.AnimationControl.StartIndex = 10;
                    e.AnimationControl.EndIndex = 92;
                    e.AnimationControl.bPlay = false;
                    return false;
                });

            Cell.RefreshAll();

            SetupPhysicalWorld();
            if (PhysicalCharacterBody != null)
            {
                Matrix m = Matrix.CreateRotationY(-MathHelper.PiOver2) * Matrix.CreateTranslation(kModel1);
                PhysicalCharacterBody.Frame = new CoordinateFrame(Matrix3.CreateFromUpperLeft(ref m), m.Translation);
            }

            int lightCount = kLightNames.Length;
            for (int i = 0; i < lightCount; i++)
            {
                SceneNode.Retrieve(kLightNames[i], delegate(SceneNode e)
                {
                    _LightHelper(e, i);
                });
            }

#if EXPERIMENT_VERSION
            Tpl.bAlwaysStep = false;
            RenderRoot.bDeferredLighting = false;
            RenderRoot.bFilteredShadows = false;
            siat.bStatsEnabled = false;
            siat.Resize(kFullscreenWidth, kFullscreenHeight, true);
#else
            siat.bEnableSoftwareMouseCursor = true;
            siat.bStatsEnabled = true;
            RenderRoot.bDeferredLighting = true;
            RenderRoot.bFilteredShadows = false;

            float v = -Utilities.Min(Vector3.Transform(Model.WorldPosition, Shared.ViewTransform).Z, 0.0f);

            DeferredPost.bEnableAmbientOcclusion = true;
            DeferredPost.bEnableAntiAliasing = true;
            DeferredPost.bEnableBloom = true;
            DeferredPost.bEnableMotionBlur = true;

            //DeferredPost.bEnableDof = true;
            DeferredPost.DofFocalPlane = new Plane(Vector3.Backward, v);
            DeferredPost.DofHyperfocalDistance = 1.0f;

            //DeferredPost.bEnableFog = true;
            DeferredPost.FogColor = Color.WhiteSmoke;
            DeferredPost.FogFalloff = 0.75f;
            DeferredPost.FogHeight = 2.0f;
            DeferredPost.FogDensity = 1.0f;
#endif
        }

        private static void ResizeHandler()
        {
            if (mGuiElement.Texture != null)
            {
                int width = Siat.Singleton.GraphicsDevice.Viewport.Width;
                int height = Siat.Singleton.GraphicsDevice.Viewport.Height;

                int desiredWidth = (int)(kImageWidthFactor * width);
                int desiredHeight = (int)(((float)desiredWidth / (float)mGuiElement.Texture.Width) * (float)mGuiElement.Texture.Height);

                int x = width - desiredWidth;
                int y = 0;

                mGuiElement.Rectangle = new Rectangle(x, y, desiredWidth, desiredHeight);
            }
        }

        private static void _Helper(Vector3 aWorldPosition, string aText)
        {
            Siat siat = Siat.Singleton;
            Viewport view = siat.GraphicsDevice.Viewport;
            Vector3 test = Vector3.Transform(aWorldPosition, Shared.ViewProjectionTransform);

            if (test.Z > 0.0f)
            {
                Vector3 screenPosition = Utilities.Project(aWorldPosition, view, Shared.ViewProjectionTransform);
                Vector2 position2d = new Vector2((float)Math.Floor(screenPosition.X), view.Height - (float)Math.Floor(screenPosition.Y));

                if (position2d.X > 0.0f && position2d.X < view.Width &&
                    position2d.Y > 0.0f && position2d.Y < view.Height)
                {
                    Siat.TextElement element = new Siat.TextElement(aText, position2d, Color.White);
                    siat.AddTextElement(ref element);
                }
            }
        }

        private static void OnDrawBeingHandler()
        {
            Siat siat = Siat.Singleton;

            siat.AddConsoleLine("Press H to show/hide this text.");
            siat.AddConsoleLine("");

            siat.AddConsoleLine("Use the arrow keys to move the character.");
            siat.AddConsoleLine("Use the mouse to control the camera:");
            siat.AddConsoleLine("    - Left-click and drag to rotate the camera.");
            siat.AddConsoleLine("    - Use the mouse wheel to zoom the camera in and out.");
            siat.AddConsoleLine("");

            siat.AddConsoleLine("Press 1, 2, 3 to select a room lighting cue.");
            siat.AddConsoleLine("Press Q, W, E to select desired character lighting.");
            siat.AddConsoleLine("Press A, S, D to switch between auto, natural, and attached lighting.");
            siat.AddConsoleLine("");
            
#if !EXPERIMENT_VERSION
            if (CurrentMode == kNaturalMode) { siat.AddConsoleLine("Press Z to enable/disable self-shadowing."); }
            else if (CurrentMode == kAutoMode) { siat.AddConsoleLine("Press X to enable/disable always stepping."); }
#endif
            
#if !EXPERIMENT_VERSION
            siat.AddConsoleLine("Press ALT+ENTER to toggle fullscreen mode.");
#endif
            siat.AddConsoleLine("Mode: {" + CurrentMode +
                "}, Cue #: {" + LightCueNumber.ToString() +
                "}, Character Lighting #: {" + CurrentMetricsNumber.ToString() + "}");

            if (CurrentMode != kNaturalMode)
            {
                siat.AddConsoleLine("");
                siat.AddConsoleLine("Press TAB to enable/disable character light arrows.");
            }

#if !EXPERIMENT_VERSION
            if (CurrentMode == kAutoMode) { siat.AddConsoleLine("Always stepping: " + ((!Tpl.bAlwaysStep) ? kDisabled : kEnabled)); }
            if (CurrentMode == kNaturalMode) { siat.AddConsoleLine("Self Shadowing: " + ((msbDisableSelfShadowing) ? kDisabled : kEnabled)); }

            siat.AddConsoleLine("Lighting mode: " + ((RenderRoot.bDeferredLighting) ? "Deferred" : "Forward"));
#if DEBUG
            siat.AddConsoleLine("Total queries issued: " + siat.ActiveCamera.Cell.TotalQueriesIssued.ToString());
#endif
#endif

            if (mGuiActive > 0.0f)
            {
                siat.AddGuiElement(ref mGuiElement);
                mGuiActive -= (float)siat.Time.ElapsedGameTime.TotalSeconds;
            }

            if (mbArrowsEnabled && Tpl.bEnabled)
            {
                Vector3 position = Tpl.Key.WorldPosition;
                string str = "Key: (Roll: " + Tpl.Current.KeyRoll.ToString() + "º, Yaw: " + Tpl.Current.KeyYaw.ToString() + "º)";
                _Helper(position, str);

                position = Tpl.Fill.WorldPosition;
                str = "Fill: (Intensity: " + Tpl.Current.Fill.ToString() + ")";
                _Helper(position, str);
            }

#if !EXPERIMENT_VERSION
#if DEBUG_CHARACTER_BODY
            if (PhysicalCharacterBody != null)
            {
                MatrixWrapper m = new MatrixWrapper(
                    Matrix.CreateScale(Utilities.GetHalfExtents(PhysicalCharacterBody.LocalAABB)) *
                    Matrix.CreateTranslation(Utilities.GetCenter(PhysicalCharacterBody.LocalAABB)) *
                    PhysicalCharacterBody.Frame.ToMatrix());

                RenderRoot.PoseOperations.WireframeBox(m);
            }
#endif
#endif
        }

        private static void OnUpdateBeginHandler()
        {
            Tpl.Tick();

            if (msActiveLightTransition != null)
            {
                if (StepLights(msActiveLightTransition))
                {
                    msActiveLightTransition = null;
                }

                Tpl.Update();
            }

            if (Model != null)
            {
                float delta = kMoveRate * (float)Siat.Singleton.Time.ElapsedGameTime.TotalSeconds;
                Vector3 move = Vector3.Zero;

                if (mbUp) { move += (Vector3.TransformNormal(Vector3.Forward, Shared.InverseViewTransform)); }
                if (mbDown) { move += (Vector3.TransformNormal(Vector3.Backward, Shared.InverseViewTransform)); }
                if (mbLeft) { move += (Vector3.TransformNormal(Vector3.Left, Shared.InverseViewTransform)); }
                if (mbRight) { move += (Vector3.TransformNormal(Vector3.Right, Shared.InverseViewTransform)); }

                move -= (Vector3.Dot(Vector3.Up, move) * Vector3.Up);
                move = Utilities.SafeNormalize(move) * delta;

                if (!Utilities.AboutZero(ref move))
                {
                    Vector3 axis = Vector3.Normalize(move);
                    Vector3 cross = Vector3.Cross(Vector3.Backward, axis);
                    if (Utilities.AboutZero(ref cross)) { cross = Vector3.Up; }

                    float angle = Utilities.SmallestAngle(Vector3.Backward, axis);
                    Matrix rotation = Matrix.CreateFromAxisAngle(Vector3.Normalize(cross), angle);

                    if (PhysicalCharacterBody != null)
                    {
                        PhysicalCharacterBody.Frame = new CoordinateFrame(Matrix3.CreateFromUpperLeft(ref rotation), PhysicalCharacterBody.Frame.Translation + move);
                        PhysicalNode.World.Tick((float)Siat.Singleton.Time.ElapsedGameTime.TotalSeconds);
                        Model.WorldTransform = Matrix.CreateScale(ModelScale) * PhysicalCharacterBody.Frame.ToMatrix();
                    }
                    else
                    {
                        Model.WorldTransform = Matrix.CreateScale(ModelScale) * rotation;
                        Model.WorldPosition += move;
                    }
                }

#if !EXPERIMENT_VERSION
                {
                    PresentationParameters p = Siat.Singleton.GraphicsDevice.PresentationParameters;

                    float v = -Utilities.Min(Vector3.Transform(Model.WorldPosition, Shared.ViewTransform).Z, 0.0f);

                    DeferredPost.DofFocalPlane = new Plane(Vector3.Backward, v);
                }
#endif

                CameraNode camera = Siat.Singleton.ActiveCamera;

                if (camera != null && camera is CameraEditingNode)
                {
                    ((CameraEditingNode)camera).Target = (Model.WorldPosition + (Vector3.Up * TargetHeight));
                }
            }
        }

        private static void Go()
        {
            Input input = Input.Singleton;
            input.AddKeyCallback(Keys.H, KeyHandler);
            input.AddKeyCallback(Keys.D1, KeyLightCueHandler);
            input.AddKeyCallback(Keys.D2, KeyLightCueHandler);
            input.AddKeyCallback(Keys.D3, KeyLightCueHandler);
            input.AddKeyCallback(Keys.Q, KeyLightingHandler);
            input.AddKeyCallback(Keys.W, KeyLightingHandler);
            input.AddKeyCallback(Keys.E, KeyLightingHandler);
            input.AddKeyCallback(Keys.A, KeyHandler);
            input.AddKeyCallback(Keys.S, KeyHandler);
            input.AddKeyCallback(Keys.D, KeyHandler);
            input.AddKeyCallback(Keys.Up, KeyMoveHandler);
            input.AddKeyCallback(Keys.Down, KeyMoveHandler);
            input.AddKeyCallback(Keys.Left, KeyMoveHandler);
            input.AddKeyCallback(Keys.Right, KeyMoveHandler);
            input.AddKeyCallback(Keys.Tab, KeyHandler);

            input.AddKeyCallback(Keys.Enter, KeyHandler);
            input.AddKeyCallback(Keys.LeftAlt, KeyHandler);
            input.AddKeyCallback(Keys.RightAlt, KeyHandler);

            input.AddKeyCallback(Keys.Escape, KeyHandler);

            Siat siat = Siat.Singleton;

#if !EXPERIMENT_VERSION
            input.OnMouseButton += MouseButtonHandler;
            input.OnMouseMove += MouseMoveHandler;
            siat.OnUpdateBegin += MouseUpdateHandler;

            input.AddKeyCallback(Keys.D7, KeyCameraHandler);
            input.AddKeyCallback(Keys.D8, KeyCameraHandler);
            input.AddKeyCallback(Keys.D9, KeyCameraHandler);

            input.AddKeyCallback(Keys.X, KeyHandler);
            input.AddKeyCallback(Keys.F1, KeyHandler);

            siat.bStatsEnabled = true;
#endif

            siat.OnDrawBegin += OnDrawBeingHandler;
            siat.OnLoading += OnLoadHandler;
            siat.OnUpdateBegin += OnUpdateBeginHandler;
            siat.Run();
        }
        #endregion

        public static void Main(string[] aArgs)
        {
#if !DEBUG
            try
            {
#endif
            Go();
#if !DEBUG
            }
            catch (Exception e)
            {
                string caption = "Error";
                string windowMessage = "This application has experienced a critical error and must close." + System.Environment.NewLine +
                    "Please send the file \"" + kLogFile + "\" to jaz147@psu.edu.";

                string msg = "Exception: \"" + e.Message + "\"" + System.Environment.NewLine;
                msg += "Source: \"" + e.Source + "\"" + System.Environment.NewLine;
                msg += "Target site: \"" + e.TargetSite + "\"" + System.Environment.NewLine;
                msg += "Stack trace:" + System.Environment.NewLine + e.StackTrace;

                using (System.IO.StreamWriter errorWriter = new StreamWriter(kLogFile))
                {
                    errorWriter.Write(msg);
                }

                System.Windows.Forms.MessageBox.Show(windowMessage, caption, 
                    System.Windows.Forms.MessageBoxButtons.OK, 
                    System.Windows.Forms.MessageBoxIcon.Error);
            }
#endif
            }
    }
}
