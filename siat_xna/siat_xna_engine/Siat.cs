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

// #define ENABLE_MULTISAMPLING

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using siat.render;
using siat.scene;

/*! \mainpage Siat XNA
 * <br><h2>Core</h2></br>
 * - siat.Siat
 * - siat.Utilities
 * 
 * <br><h2>Pipeline</h2></br>
 * - siat.pipeline
 * - siat.pipeline.collada
 * 
 * <br><h2>Rendering</h2></br>
 * - siat.render.RenderRoot
 * 
 * <br><h2>Scene</h2></br>
 * - siat.scene.Cell
 * - siat.scene.SceneNode
 * 
 */

namespace siat
{
    /// <summary>
    /// A singleton entry point for the engine. 
    /// </summary>
    /// <remarks>
    /// Handles start and stop of engine services. Handles update and drawing each frame
    /// when Siat.Update() and Siat.Draw() are ticked by XNA.
    /// 
    /// The singleton instance of Siat is returned by Siat.Singleton.
    /// 
    /// In general, usage of Siat involves registering a handler for Siat.OnLoading() to load
    /// content. Handlers should also be added to Siat.OnUpdateBegin() or Siat.OnUpdateEnd()
    /// if update actions need to be taken each engine iteration. Once handlers are registered,
    /// Siat.Run() should be called. You may also need to register a handler for Siat.OnUnloading() if
    /// you manually create graphics resources, such as instantiating a render target. The 
    /// Siat.GraphicsDevice object will usually be disposed within the scope of a Siat.Unloading() event.
    /// </remarks>
    /// 
    /// \sa siat.Program.Go 
    /// \sa siat.Program.OnLoadHandler 
    /// \sa siat.Program.OnUpdateEndHandler
    /// 
    /// \todo Siat does not deinitialize cleanly on call to Siat.Exit(). A second call to 
    /// Siat.Run() will cause exceptions to be thrown. This appears to be due to the internal 
    /// workings of the base Game class.
    /// 
    /// <h2>Examples</h2>
    /// <code>
    /// Siat siat = Siat.Singleton;
    /// siat.OnLoading += MyLoadingHandler;
    /// siat.OnUpdateEnd += MyUpdateHandler;
    /// 
    /// siat.Run();
    /// </code>
    public sealed class Siat : Game
    {
        public const int kDefaultStencilMask = -1;
        public const int kDefaultReferenceStencil = 0;
        public static readonly Color kPickClearColor = new Color(0, 0, 0, 255);
        public static readonly Color kPickBaseColor = new Color(0, 0, 1, 255);
        private const string kMetricsFontFilename = "courier";
        private const string kBuiltInEffect = "BuiltInEffect";
        private const string kCursorTexture = "cursor";
        private const int kCursorXAdjust = -11;
        private const int kCursorYAdjust = -7;

        #region Private members
        private static readonly short[] mkUnitBoxIndices = new short[] { 0, 1, 2, 2, 1, 3, 1, 4, 3, 3, 4, 6, 4, 5, 6, 6, 5, 7, 5, 0, 7, 7, 0, 2, 2, 3, 7, 7, 3, 6, 5, 4, 0, 0, 4, 1 };
        private static readonly float[] mkUnitBoxVertices = new float[] { -1, -1, 1, 1, -1, 1, -1, 1, 1, 1, 1, 1, 1, -1, -1, -1, -1, -1, 1, 1, -1, -1, 1, -1 };
        private static readonly short[] mkUnitFrustumIndices = new short[] { 0, 1, 2, 0, 2, 3, 0, 3, 4, 0, 4, 1, 1, 4, 2, 2, 4, 3 };
        private static readonly float[] mkUnitFrustumVertices = new float[] { 0, 0, 0, -1, -1, -1, -1, 1, -1, 1, 1, -1, 1, -1, -1 };
        private static readonly short[] mkUnitSphereIndices = new short[] { 0, 1, 2, 3, 4, 5, 0, 2, 6, 3, 7, 4, 0, 6, 8, 3, 9, 7, 0, 8, 10, 3, 11, 9, 0, 10, 12, 3, 13, 11, 0, 12, 14, 3, 15, 13, 0, 14, 16, 3, 17, 15, 0, 16, 18, 3, 19, 17, 0, 18, 20, 3, 21, 19, 0, 20, 22, 3, 23, 21, 0, 22, 24, 3, 25, 23, 0, 24, 1, 3, 5, 25, 1, 26, 27, 1, 27, 2, 2, 27, 6, 27, 28, 6, 6, 28, 29, 6, 29, 8, 8, 29, 10, 29, 30, 10, 10, 30, 31, 10, 31, 12, 12, 31, 14, 31, 32, 14, 14, 32, 33, 14, 33, 16, 16, 33, 18, 33, 34, 18, 18, 34, 35, 18, 35, 20, 20, 35, 22, 35, 36, 22, 22, 36, 37, 22, 37, 24, 24, 37, 1, 37, 26, 1, 26, 38, 27, 38, 39, 27, 27, 39, 40, 27, 40, 28, 28, 40, 29, 40, 41, 29, 29, 41, 42, 29, 42, 30, 30, 42, 31, 42, 43, 31, 31, 43, 44, 31, 44, 32, 32, 44, 33, 44, 45, 33, 33, 45, 46, 33, 46, 34, 34, 46, 35, 46, 47, 35, 35, 47, 48, 35, 48, 36, 36, 48, 37, 48, 49, 37, 37, 49, 38, 37, 38, 26, 38, 50, 51, 38, 51, 39, 39, 51, 40, 51, 52, 40, 40, 52, 53, 40, 53, 41, 41, 53, 42, 53, 54, 42, 42, 54, 55, 42, 55, 43, 43, 55, 44, 55, 56, 44, 44, 56, 57, 44, 57, 45, 45, 57, 46, 57, 58, 46, 46, 58, 59, 46, 59, 47, 47, 59, 48, 59, 60, 48, 48, 60, 61, 48, 61, 49, 49, 61, 38, 61, 50, 38, 50, 5, 51, 5, 4, 51, 51, 4, 7, 51, 7, 52, 52, 7, 53, 7, 9, 53, 53, 9, 11, 53, 11, 54, 54, 11, 55, 11, 13, 55, 55, 13, 15, 55, 15, 56, 56, 15, 57, 15, 17, 57, 57, 17, 19, 57, 19, 58, 58, 19, 59, 19, 21, 59, 59, 21, 23, 59, 23, 60, 60, 23, 61, 23, 25, 61, 61, 25, 5, 61, 5, 50 };
        private static readonly float[] mkUnitSphereVertices = new float[] { 8.71482E-10f, -0.998307f, -2.4683E-08f, 0.500784f, -0.86456f, -2.4683E-08f, 0.433692f, -0.86456f, 0.250679f, 8.71482E-10f, 0.998308f, -2.4683E-08f, 0.433692f, 0.864559f, 0.250679f, 0.500784f, 0.864559f, -2.4683E-08f, 0.250392f, -0.86456f, 0.434188f, 0.250392f, 0.864559f, 0.434188f, 8.71482E-10f, -0.86456f, 0.501357f, 8.71482E-10f, 0.864559f, 0.501357f, -0.250392f, -0.86456f, 0.434188f, -0.250392f, 0.864559f, 0.434188f, -0.433692f, -0.86456f, 0.250679f, -0.433692f, 0.864559f, 0.250679f, -0.500784f, -0.86456f, -2.4683E-08f, -0.500784f, 0.864559f, -2.4683E-08f, -0.433692f, -0.86456f, -0.250679f, -0.433692f, 0.864559f, -0.250679f, -0.250392f, -0.86456f, -0.434188f, -0.250392f, 0.864559f, -0.434188f, 8.71481E-10f, -0.86456f, -0.501357f, 8.71481E-10f, 0.864559f, -0.501357f, 0.250392f, -0.86456f, -0.434188f, 0.250392f, 0.864559f, -0.434188f, 0.433692f, -0.86456f, -0.250679f, 0.433692f, 0.864559f, -0.250679f, 0.867384f, -0.499154f, -2.4683E-08f, 0.751177f, -0.499154f, 0.434188f, 0.433692f, -0.499154f, 0.752036f, 8.71482E-10f, -0.499154f, 0.868377f, -0.433692f, -0.499154f, 0.752036f, -0.751177f, -0.499154f, 0.434188f, -0.867384f, -0.499154f, -2.4683E-08f, -0.751177f, -0.499154f, -0.434188f, -0.433692f, -0.499154f, -0.752036f, 8.71481E-10f, -0.499154f, -0.868377f, 0.433692f, -0.499154f, -0.752036f, 0.751177f, -0.499154f, -0.434188f, 1.00157f, -1.93919E-07f, -2.4683E-08f, 0.867384f, -1.93919E-07f, 0.501357f, 0.500784f, -1.93919E-07f, 0.868376f, 8.71482E-10f, -1.93919E-07f, 1.00271f, -0.500784f, -1.93919E-07f, 0.868376f, -0.867384f, -1.93919E-07f, 0.501357f, -1.00157f, -1.93919E-07f, -2.4683E-08f, -0.867384f, -1.93919E-07f, -0.501357f, -0.500784f, -1.93919E-07f, -0.868377f, 8.71481E-10f, -1.93919E-07f, -1.00271f, 0.500784f, -1.93919E-07f, -0.868377f, 0.867384f, -1.93919E-07f, -0.501357f, 0.867384f, 0.499154f, -2.4683E-08f, 0.751176f, 0.499154f, 0.434188f, 0.433692f, 0.499154f, 0.752036f, 8.71482E-10f, 0.499154f, 0.868376f, -0.433692f, 0.499154f, 0.752036f, -0.751176f, 0.499154f, 0.434188f, -0.867384f, 0.499154f, -2.4683E-08f, -0.751176f, 0.499154f, -0.434188f, -0.433692f, 0.499154f, -0.752036f, 8.71481E-10f, 0.499154f, -0.868377f, 0.433692f, 0.499154f, -0.752036f, 0.751176f, 0.499154f, -0.434188f };

        private static readonly short[] mkUnitQuadIndices = new short[] { 0, 1, 2, 1, 3, 2 };
        private static readonly float[] mkUnitQuadVertices = new float[]
            {
                -1, -1, 0, 0, 0,
                -1,  1, 0, 0, 1,
                 1, -1, 0, 1, 0,
                 1,  1, 0, 1, 1 };

        internal VertexDeclaration mPosOnlyVertexDeclaration = null;
        private VertexDeclaration mQuadVertexDeclaration = null;

        private static Siat msSingleton = new Siat();

        private bool mbConsoleEnabled = true;
        private CameraNode mActiveCamera = null;
        private GameTime mCurrentTick = new GameTime();
        private uint mFrameTick = 0;
        private GraphicsDeviceManager mGraphicsDeviceManager = null;
        private Input mInput = null;
        private MeshPart mUnitBoxMeshPart;
        private MeshPart mUnitFrustumMeshPart;
        private MeshPart mUnitQuadMeshPart;
        private MeshPart mUnitSphereMeshPart;
        private bool mbDoneLoadInternal = false;

        #region GUI output
        private List<GuiElement> mGuiElements = new List<GuiElement>();
        private List<TextElement> mTextElements = new List<TextElement>();
        private List<string> mConsole = new List<string>();

        private bool mbStatsEnabled = true;
        private double mTimeDelta = 0.0;
        private double mFramesPerSecond = 0.0;
        private double mFrameCount = 0.0;
        private int mFacetsCount = 0;
        private int mDrawOpCount = 0;
        private int mMinPerOp = int.MaxValue;
        private int mMaxPerOp = int.MinValue;
        internal int mEffectPasses = 0;

        private const byte kBackAlpha = 127;
        private Color mConsoleColor = Color.White;
        private Color[] mBackColor = new Color[] { new Color(0, 0, 0, kBackAlpha) };
        private bool mbBackColorDirty = true;
        private readonly Vector2 mkDrawOpsPosition = new Vector2(0, 0.0f);
        private readonly Vector2 mkTextOffset = new Vector2(0, 15.0f);

        SpriteBatch mGuiBatch = null;
        SpriteFont mMetricsFont = null;
        #endregion

        #region Picking
        private PickingCallback mPickCallback = null;
        private Color mPickColor = kPickBaseColor;
        private Dictionary<object, PickingPair> mPickTable = new Dictionary<object, PickingPair>();
        private Texture2D mBackTexture = null;
        private Texture2D mCursorTexture = null;
        private bool mbSoftwareCursor = false;

        internal Rectangle mPickRectangle;
        internal ResolveTexture2D mPickTexture = null;
        #endregion

        #region Built-in effects
        private SiatEffect mBuiltInEffect = null;
        #endregion

        private Siat()
        {
            base.IsFixedTimeStep = false;
            mGraphicsDeviceManager = new GraphicsDeviceManager(this);
            mGraphicsDeviceManager.PreparingDeviceSettings += new EventHandler<PreparingDeviceSettingsEventArgs>(_HandlePreparingDeviceSettings);
            mGraphicsDeviceManager.PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8;
            mGraphicsDeviceManager.PreferMultiSampling = true;
            mGraphicsDeviceManager.MinimumPixelShaderProfile = ShaderProfile.PS_2_A;
            mGraphicsDeviceManager.MinimumVertexShaderProfile = ShaderProfile.VS_2_A;
            mGraphicsDeviceManager.SynchronizeWithVerticalRetrace = false;
            mInput = Input.Singleton;
        }

        private void _HandlePreparingDeviceSettings(object aSender, PreparingDeviceSettingsEventArgs e)
        {
            #region Setup multisampling
#if ENABLE_MULTISAMPLING
            if (!Deferred.bActive)
            {
                GraphicsAdapter a = e.GraphicsDeviceInformation.Adapter;
                SurfaceFormat currentFormat = a.CurrentDisplayMode.Format;
                e.GraphicsDeviceInformation.PresentationParameters.MultiSampleQuality = 1;
                e.GraphicsDeviceInformation.PresentationParameters.MultiSampleType = MultiSampleType.NonMaskable;
            }
            else
#endif
            {
                GraphicsAdapter a = e.GraphicsDeviceInformation.Adapter;
                SurfaceFormat currentFormat = a.CurrentDisplayMode.Format;
                e.GraphicsDeviceInformation.PresentationParameters.MultiSampleQuality = 0;
                e.GraphicsDeviceInformation.PresentationParameters.MultiSampleType = MultiSampleType.None;
            }
            #endregion
        }
        #endregion

        #region Overrides
        protected override void Initialize()
        {
            mInput.Initialize();

            base.Initialize();
        }

        protected override void BeginRun()
        {
            base.BeginRun();
        }

        protected override void EndRun()
        {
            base.EndRun();
        }

        protected override void LoadContent()
        {
            #region Check device caps.
            if (!(GraphicsDevice.DepthStencilBuffer.Format == DepthFormat.Depth24Stencil8 ||
                  GraphicsDevice.DepthStencilBuffer.Format == DepthFormat.Depth24Stencil8Single))
            {
                throw new Exception("Siat XNA requires an 8-bit stencil buffer which could not be set.");
            }
            #endregion

            _RestoreState();

            Content.RootDirectory = Utilities.kMediaRoot;

            GraphicsDevice.EvictManagedResources();

            if (Deferred.bActive) { Deferred.OnLoad(); }
            else { ForwardPost.OnLoad(); }
            ShadowMaps.OnLoad();

            #region Metrics content
            mGuiBatch = new SpriteBatch(GraphicsDevice);
            // Note: usage of Content here is intentional. Don't want the font to be unloaded,
            //       even if SiatContentManager.Unload() is called.
            mMetricsFont = Content.Load<SpriteFont>(kMetricsFontFilename);
            #endregion

            mBuiltInEffect = new SiatEffect(kBuiltInEffect, Content.Load<Effect>(kBuiltInEffect));
            mCursorTexture = Content.Load<Texture2D>(kCursorTexture);

            #region Setup vertex declarations
            VertexElement[] elements = new VertexElement[1];
            elements[0].Offset = 0;
            elements[0].Stream = 0;
            elements[0].UsageIndex = 0;
            elements[0].VertexElementFormat = VertexElementFormat.Vector3;
            elements[0].VertexElementMethod = VertexElementMethod.Default;
            elements[0].VertexElementUsage = VertexElementUsage.Position;
            mPosOnlyVertexDeclaration = new VertexDeclaration(GraphicsDevice, elements);

            VertexElement[] quadElements = new VertexElement[2];
            quadElements[0] = elements[0];
            quadElements[1].Offset = sizeof(float) * 3;
            quadElements[1].Stream = 0;
            quadElements[1].UsageIndex = 0;
            quadElements[1].VertexElementFormat = VertexElementFormat.Vector2;
            quadElements[1].VertexElementMethod = VertexElementMethod.Default;
            quadElements[1].VertexElementUsage = VertexElementUsage.TextureCoordinate;
            mQuadVertexDeclaration = new VertexDeclaration(GraphicsDevice, quadElements);
            #endregion

            #region Generate basic geometry
            #region Sphere
            {
                int sphereIndicesSizeInBytes = sizeof(short) * mkUnitSphereIndices.Length;
                int sphereVerticesSizeInBytes = sizeof(float) * mkUnitSphereVertices.Length;

                #if SIAT_DEFAULT_CLOCKWISE_WINDING
                short[] sphereIndices = new short[mkUnitSphereIndices.Length];

                int count = sphereIndices.Length;
                for (int i = 0; i < count; i++)
                {
                    int mod = i % 3;

                    switch (mod)
                    {
                        case 0: sphereIndices[i] = mkUnitSphereIndices[i]; break;
                        case 1: sphereIndices[i] = mkUnitSphereIndices[i + 1]; break;
                        case 2: sphereIndices[i] = mkUnitSphereIndices[i - 1]; break;
                    }
                }
                #else
                short[] sphereIndices = mUnitSphereIndices;
                #endif

                mUnitSphereMeshPart = new MeshPart("UnitSphereMeshPart");
                mUnitSphereMeshPart.Indices = new IndexBuffer(GraphicsDevice, sphereIndicesSizeInBytes, BufferUsage.WriteOnly, IndexElementSize.SixteenBits);
                mUnitSphereMeshPart.Indices.SetData<short>(sphereIndices);
                mUnitSphereMeshPart.BoundingSphere = new BoundingSphere(Vector3.Zero, 1.0f);
                mUnitSphereMeshPart.AABB = BoundingBox.CreateFromSphere(mUnitSphereMeshPart.BoundingSphere);
                mUnitSphereMeshPart.PrimitiveCount = sphereIndices.Length / 3;
                mUnitSphereMeshPart.PrimitiveType = PrimitiveType.TriangleList;
                mUnitSphereMeshPart.Vertices = new VertexBuffer(GraphicsDevice, sphereVerticesSizeInBytes, BufferUsage.WriteOnly);
                mUnitSphereMeshPart.Vertices.SetData<float>(mkUnitSphereVertices);
                mUnitSphereMeshPart.VertexCount = mkUnitSphereVertices.Length / 3;
                mUnitSphereMeshPart.VertexDeclaration = mPosOnlyVertexDeclaration;
                mUnitSphereMeshPart.VertexStride = sizeof(float) * 3;
            }
            #endregion

            #region Box geometry
            {
                int boxIndicesSizeInBytes = sizeof(short) * mkUnitBoxIndices.Length;
                int boxVerticesSizeInBytes = sizeof(float) * mkUnitBoxVertices.Length;

                #if SIAT_DEFAULT_CLOCKWISE_WINDING
                short[] boxIndices = new short[mkUnitBoxIndices.Length];

                int count = boxIndices.Length;
                for (int i = 0; i < count; i++)
                {
                    int mod = i % 3;

                    switch (mod)
                    {
                        case 0: boxIndices[i] = mkUnitBoxIndices[i]; break;
                        case 1: boxIndices[i] = mkUnitBoxIndices[i + 1]; break;
                        case 2: boxIndices[i] = mkUnitBoxIndices[i - 1]; break;
                    }
                }
                #else
                short[] boxIndices = mUnitBoxIndices;
                #endif

                mUnitBoxMeshPart = new MeshPart("UnitBoxMeshPart");
                mUnitBoxMeshPart.Indices = new IndexBuffer(GraphicsDevice, boxIndicesSizeInBytes, BufferUsage.WriteOnly, IndexElementSize.SixteenBits);
                mUnitBoxMeshPart.Indices.SetData<short>(boxIndices);
                mUnitBoxMeshPart.AABB = new BoundingBox(-Vector3.One, Vector3.One);
                mUnitBoxMeshPart.BoundingSphere = BoundingSphere.CreateFromBoundingBox(mUnitBoxMeshPart.AABB);
                mUnitBoxMeshPart.PrimitiveCount = boxIndices.Length / 3;
                mUnitBoxMeshPart.PrimitiveType = PrimitiveType.TriangleList;
                mUnitBoxMeshPart.Vertices = new VertexBuffer(GraphicsDevice, boxVerticesSizeInBytes, BufferUsage.WriteOnly);
                mUnitBoxMeshPart.Vertices.SetData<float>(mkUnitBoxVertices);
                mUnitBoxMeshPart.VertexCount = mkUnitBoxVertices.Length / 3;
                mUnitBoxMeshPart.VertexDeclaration = mPosOnlyVertexDeclaration;
                mUnitBoxMeshPart.VertexStride = sizeof(float) * 3;
            }
            #endregion

            #region Frustum geometry
            {
                int frustumIndicesSizeInBytes = sizeof(short) * mkUnitFrustumIndices.Length;
                int frustumVerticesSizeInBytes = sizeof(float) * mkUnitFrustumVertices.Length;

#if SIAT_DEFAULT_COUNTER_CLOCKWISE_WINDING
                short[] frustumIndices = new short[mkUnitFrustumIndices.Length];

                int count = frustumIndices.Length;
                for (int i = 0; i < count; i++)
                {
                    int mod = i % 3;

                    switch (mod)
                    {
                        case 0: frustumIndices[i] = mkUnitFrustumIndices[i]; break;
                        case 1: frustumIndices[i] = mkUnitFrustumIndices[i + 1]; break;
                        case 2: frustumIndices[i] = mkUnitFrustumIndices[i - 1]; break;
                    }
                }
#elif SIAT_DEFAULT_CLOCKWISE_WINDING
                short[] frustumIndices = mkUnitFrustumIndices;
#endif

                mUnitFrustumMeshPart = new MeshPart("UnitFrustumMeshPart");
                mUnitFrustumMeshPart.Indices = new IndexBuffer(GraphicsDevice, frustumIndicesSizeInBytes, BufferUsage.WriteOnly, IndexElementSize.SixteenBits);
                mUnitFrustumMeshPart.Indices.SetData<short>(frustumIndices);
                mUnitFrustumMeshPart.AABB = new BoundingBox(-Vector3.One, new Vector3(1, 1, 0));
                mUnitFrustumMeshPart.BoundingSphere = BoundingSphere.CreateFromBoundingBox(mUnitFrustumMeshPart.AABB);
                mUnitFrustumMeshPart.PrimitiveCount = frustumIndices.Length / 3;
                mUnitFrustumMeshPart.PrimitiveType = PrimitiveType.TriangleList;
                mUnitFrustumMeshPart.Vertices = new VertexBuffer(GraphicsDevice, frustumVerticesSizeInBytes, BufferUsage.WriteOnly);
                mUnitFrustumMeshPart.Vertices.SetData<float>(mkUnitFrustumVertices);
                mUnitFrustumMeshPart.VertexCount = mkUnitFrustumVertices.Length / 3;
                mUnitFrustumMeshPart.VertexDeclaration = mPosOnlyVertexDeclaration;
                mUnitFrustumMeshPart.VertexStride = sizeof(float) * 3;
            }
            #endregion

            #region Quad geometry
            {
                int quadIndicesSizeInBytes = sizeof(short) * mkUnitQuadIndices.Length;
                int quadVerticesSizeInBytes = sizeof(float) * mkUnitQuadVertices.Length;

#if SIAT_DEFAULT_COUNTER_CLOCKWISE_WINDING
                short[] quadIndices = new short[mkUnitQuadIndices.Length];

                int count = quadIndices.Length;
                for (int i = 0; i < count; i++)
                {
                    int mod = i % 3;

                    switch (mod)
                    {
                        case 0: quadIndices[i] = mkUnitQuadIndices[i]; break;
                        case 1: quadIndices[i] = mkUnitQuadIndices[i + 1]; break;
                        case 2: quadIndices[i] = mkUnitQuadIndices[i - 1]; break;
                    }
                }
#elif SIAT_DEFAULT_CLOCKWISE_WINDING
                short[] quadIndices = mkUnitQuadIndices;
#endif

                mUnitQuadMeshPart = new MeshPart("UnitQuadMeshPart");
                mUnitQuadMeshPart.Indices = new IndexBuffer(GraphicsDevice, quadIndicesSizeInBytes, BufferUsage.WriteOnly, IndexElementSize.SixteenBits);
                mUnitQuadMeshPart.Indices.SetData<short>(quadIndices);
                mUnitQuadMeshPart.AABB = new BoundingBox(new Vector3(-1, -1, 0), new Vector3(1, 1, 0));
                mUnitQuadMeshPart.BoundingSphere = BoundingSphere.CreateFromBoundingBox(mUnitQuadMeshPart.AABB);
                mUnitQuadMeshPart.PrimitiveCount = quadIndices.Length / 3;
                mUnitQuadMeshPart.PrimitiveType = PrimitiveType.TriangleList;
                mUnitQuadMeshPart.Vertices = new VertexBuffer(GraphicsDevice, quadVerticesSizeInBytes, BufferUsage.WriteOnly);
                mUnitQuadMeshPart.Vertices.SetData<float>(mkUnitQuadVertices);
                mUnitQuadMeshPart.VertexCount = mkUnitQuadVertices.Length / 5;
                mUnitQuadMeshPart.VertexDeclaration = mQuadVertexDeclaration;
                mUnitQuadMeshPart.VertexStride = sizeof(float) * 5;
            }
            #endregion
            #endregion

            #region Setup picking texture
            PresentationParameters pms = GraphicsDevice.PresentationParameters;
            mBackTexture = new Texture2D(GraphicsDevice, 1, 1, 1, TextureUsage.None, SurfaceFormat.Color);
            mPickTexture = new ResolveTexture2D(GraphicsDevice, pms.BackBufferWidth, pms.BackBufferHeight, 1, pms.BackBufferFormat);

            mbDoneLoadInternal = true;
            #endregion

            if (OnLoading != null) OnLoading();

            base.LoadContent();

            RenderRoot.msRenderState = GraphicsDevice.RenderState;
            RenderRoot.msGraphics = GraphicsDevice;
            RenderRoot.msSiat = this;
        }

        protected override void UnloadContent()
        {
            mbDoneLoadInternal = false;

            RenderRoot._ResetTrees();

            Cell.UnloadAll();

            RenderRoot.msSiat = null;
            RenderRoot.msActiveEffect = null;
            RenderRoot.msGraphics = null;
            RenderRoot.msRenderState = null;

            base.UnloadContent();

            if (OnUnloading != null) OnUnloading();

            mPickTexture.Dispose(); mPickTexture = null;
            mBackTexture.Dispose(); mBackTexture = null;
            mUnitQuadMeshPart.Vertices.Dispose(); mUnitQuadMeshPart.Indices.Dispose(); mUnitQuadMeshPart = null;
            mUnitFrustumMeshPart.Vertices.Dispose(); mUnitFrustumMeshPart.Indices.Dispose(); mUnitFrustumMeshPart = null;
            mUnitBoxMeshPart.Vertices.Dispose(); mUnitBoxMeshPart.Indices.Dispose(); mUnitBoxMeshPart = null;
            mUnitSphereMeshPart.Vertices.Dispose(); mUnitSphereMeshPart.Indices.Dispose(); mUnitSphereMeshPart = null;
            mQuadVertexDeclaration.Dispose(); mQuadVertexDeclaration = null;
            mPosOnlyVertexDeclaration.Dispose(); mPosOnlyVertexDeclaration = null;
            mMetricsFont = null;
            mGuiBatch.Dispose(); mGuiBatch = null;

            ForwardPost.OnUnload();
            ShadowMaps.OnUnload();
            Deferred.OnUnload();

            Content.Unload();
        }

        protected override void OnExiting(object aSender, EventArgs aArguments)
        {
            UnloadContent();
        }

        private void _RestoreState()
        {
            RenderState rs = GraphicsDevice.RenderState;
            rs.AlphaBlendEnable = false;
            rs.AlphaBlendOperation = BlendFunction.Add;
            rs.AlphaDestinationBlend = Blend.One;
            rs.AlphaFunction = CompareFunction.Always;
            rs.AlphaSourceBlend = Blend.One;
            rs.AlphaTestEnable = false;
            rs.BlendFactor = Color.White;
            rs.BlendFunction = BlendFunction.Add;
            rs.ColorWriteChannels = ColorWriteChannels.All;
            rs.ColorWriteChannels1 = ColorWriteChannels.All;
            rs.ColorWriteChannels2 = ColorWriteChannels.All;
            rs.ColorWriteChannels3 = ColorWriteChannels.All;
            rs.CounterClockwiseStencilDepthBufferFail = StencilOperation.Keep;
            rs.CounterClockwiseStencilFail = StencilOperation.Keep;
            rs.CounterClockwiseStencilFunction = CompareFunction.Always;
            rs.CounterClockwiseStencilPass = StencilOperation.Keep;
            rs.CullMode = Utilities.kBackFaceCulling;
            rs.DepthBias = 0;
            rs.DepthBufferEnable = true;
            rs.DepthBufferFunction = CompareFunction.LessEqual;
            rs.DepthBufferWriteEnable = true;
            rs.DestinationBlend = Blend.Zero;
            rs.FillMode = FillMode.Solid;
            rs.FogColor = Color.TransparentBlack;
            rs.FogDensity = 1.0f;
            rs.FogEnable = false;
            rs.FogEnd = 1.0f;
            rs.FogStart = 0.0f;
            rs.FogTableMode = FogMode.None;
            rs.FogVertexMode = FogMode.None;
            rs.MultiSampleAntiAlias = (Deferred.bActive) ? false : true;
            rs.MultiSampleMask = kDefaultStencilMask;
            rs.PointSizeMax = 64.0f;
            rs.PointSizeMin = 1.0f;
            rs.PointSpriteEnable = false;
            rs.RangeFogEnable = false;
            rs.ReferenceAlpha = 0;
            rs.ReferenceStencil = 0;
            rs.ScissorTestEnable = false;
            rs.SeparateAlphaBlendEnabled = false;
            rs.SlopeScaleDepthBias = 0;
            rs.SourceBlend = Blend.One;
            rs.StencilDepthBufferFail = StencilOperation.Keep;
            rs.StencilEnable = false;
            rs.StencilFail = StencilOperation.Keep;
            rs.StencilFunction = CompareFunction.Always;
            rs.StencilMask = Siat.kDefaultStencilMask;
            rs.StencilPass = StencilOperation.Keep;
            rs.StencilWriteMask = Siat.kDefaultStencilMask;
            rs.TwoSidedStencilMode = false;
            rs.Wrap0 = TextureWrapCoordinates.None;
            rs.Wrap1 = TextureWrapCoordinates.None;
            rs.Wrap2 = TextureWrapCoordinates.None;
            rs.Wrap3 = TextureWrapCoordinates.None;
            rs.Wrap4 = TextureWrapCoordinates.None;
            rs.Wrap5 = TextureWrapCoordinates.None;
            rs.Wrap6 = TextureWrapCoordinates.None;
            rs.Wrap7 = TextureWrapCoordinates.None;
            rs.Wrap8 = TextureWrapCoordinates.None;
            rs.Wrap9 = TextureWrapCoordinates.None;
            rs.Wrap10 = TextureWrapCoordinates.None;
            rs.Wrap11 = TextureWrapCoordinates.None;
            rs.Wrap12 = TextureWrapCoordinates.None;
            rs.Wrap13 = TextureWrapCoordinates.None;
            rs.Wrap14 = TextureWrapCoordinates.None;
            rs.Wrap15 = TextureWrapCoordinates.None;
        }

        private void _ResolvePick(ref Color aColor)
        {
            float depth = ((float)aColor.A) / 255.0f;
            object color = new Color(aColor.R, aColor.G, aColor.B); // ignore alpha.
            PickingPair pair;
            mPickTable.TryGetValue(color, out pair);
            mPickCallback(pair.Cell, pair.Node, depth);

            mPickCallback = null;
            mPickColor = kPickBaseColor;
            mPickRectangle = Rectangle.Empty;
            mPickTable.Clear();
        }

        /// <summary>
        /// Starts the pose and draw passes each frame.
        /// </summary>
        /// <remarks>
        /// This function is ticked each frame by XNA. It calls siat.scene.CameraNode.StartPose() on the
        /// active camera to initiate a pose pass. The pose pass performs visibility culling
        /// and prepares geometry for rendering. It then calls siat.render.RenderRoot.Draw() to actually
        /// call the necessary graphics API calls to perform drawing.
        /// </remarks>
        /// <param name="aDrawTick">Time data for the current draw tick.</param>
        protected override void Draw(GameTime aDrawTick)
        {
            mCurrentTick = aDrawTick;
            RenderState rs = GraphicsDevice.RenderState;

            #region Posing
            mFrameTick++;

            if (OnPoseBegin != null) OnPoseBegin();
            if (mActiveCamera != null) mActiveCamera.StartPose();
            if (OnPoseEnd != null) OnPoseEnd();
            #endregion

            RenderRoot.msGraphics = GraphicsDevice;
            RenderRoot.msRenderState = GraphicsDevice.RenderState;

            #region Picking
            if (mPickCallback != null)
            {
                Color pixel = RenderRoot.Pick();
                _ResolvePick(ref pixel);
            }
            #endregion

            #region Drawing
            if (OnDrawBegin != null) OnDrawBegin();
            RenderRoot.Draw();

            #region Text output
            _RestoreState();

            if (mbStatsEnabled)
            {
                AddConsoleLine("FPS: " + string.Format("{0:0.00}", mFramesPerSecond));
                AddConsoleLine("Facets: " + string.Format("{0}", mFacetsCount));
                AddConsoleLine("Draw ops: " + string.Format("{0}", mDrawOpCount));
                AddConsoleLine("Min per op: " + string.Format("{0}", mMinPerOp));
                AddConsoleLine("Max per op: " + string.Format("{0}", mMaxPerOp));
                AddConsoleLine("Effect passes: " + string.Format("{0}", mEffectPasses));
            }

            mGuiBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Deferred, SaveStateMode.None);
            {
                #region Background
                if (mbConsoleEnabled)
                {
                    if (mbBackColorDirty)
                    {
                        mBackTexture.SetData<Color>(mBackColor);
                        mbBackColorDirty = false;
                    }

                    float height = ((float)(mConsole.Count + 1) * mkTextOffset.Y);
                    Rectangle rect = new Rectangle((int)mkDrawOpsPosition.X, (int)mkDrawOpsPosition.Y,
                        GraphicsDevice.PresentationParameters.BackBufferWidth, (int)height);

                    mGuiBatch.Draw(mBackTexture, rect, Color.White);
                }
                #endregion

                int count = mGuiElements.Count;
                for (int i = 0; i < count; i++)
                {
                    mGuiBatch.Draw(mGuiElements[i].Texture, mGuiElements[i].Rectangle, mGuiElements[i].Color);
                }

                if (mbConsoleEnabled)
                {
                    Vector2 position = mkDrawOpsPosition;
                    count = mConsole.Count;

                    for (int i = 0; i < count; i++)
                    {
                        string current = mConsole[i];

                        mGuiBatch.DrawString(mMetricsFont, current, position, mConsoleColor);
                        position += mkTextOffset;
                    }
                }

                count = mTextElements.Count;
                for (int i = 0; i < count; i++)
                {
                    mGuiBatch.DrawString(mMetricsFont, mTextElements[i].Text, mTextElements[i].Position, mTextElements[i].Color);
                }

                if (!IsMouseVisible && mbSoftwareCursor)
                {
                    MouseState state = Mouse.GetState();
                    Rectangle rect = new Rectangle(state.X + kCursorXAdjust, state.Y + kCursorYAdjust, mCursorTexture.Width, mCursorTexture.Height);
                    mGuiBatch.Draw(mCursorTexture, rect, Color.White);
                }
            }
            mGuiBatch.End();

            {
                // Unsets textures to avoid locking errors, new bug in XNA 2.0.
                int count = GraphicsDevice.GraphicsDeviceCapabilities.MaxSimultaneousTextures;
                for (int i = 0; i < count; i++)
                {
                    GraphicsDevice.Textures[i] = null;
                }
            }

            _RestoreState();

            mFacetsCount = 0;
            mDrawOpCount = 0;
            mEffectPasses = 0;
            mMinPerOp = int.MaxValue;
            mMaxPerOp = int.MinValue;
            mConsole.Clear();
            mGuiElements.Clear();
            mTextElements.Clear();
            mFrameCount += 1.0;
            #endregion

            base.Draw(aDrawTick);
            #endregion
        }

        protected override void EndDraw()
        {
            base.EndDraw();
            if (OnDrawEnd != null) OnDrawEnd();
        }

        protected override void Update(GameTime aCurrentUpdateTick)
        {
            mCurrentTick = aCurrentUpdateTick;

            mInput.Update();

            #region Metrics update
            {
                mTimeDelta += mCurrentTick.ElapsedGameTime.TotalSeconds;

                if (Utilities.GreaterThan(mTimeDelta, 1.0))
                {
                    mFramesPerSecond = mFrameCount / mTimeDelta;
                    mTimeDelta = 0.0;
                    mFrameCount = 0.0;
                }
            }
            #endregion

            if (OnUpdateBegin != null) OnUpdateBegin();
            if (mActiveCamera != null && mActiveCamera.Cell != null) mActiveCamera.StartUpdate();
            if (OnUpdateEnd != null) OnUpdateEnd();

            base.Update(aCurrentUpdateTick);
        }
        #endregion

        #region Types
        public struct IndexedDrawSettings
        {
            public PrimitiveType PrimitiveType;
            public int BaseVertex;
            public int MinVertexIndex;
            public int NumberOfVertices;
            public int StartIndex;
            public int PrimitiveCount;
        }

        public struct PickingPair
        {
            public PickingPair(Cell aCell, PoseableNode aNode)
            {
                Cell = aCell;
                Node = aNode;
            }

            public Cell Cell;
            public PoseableNode Node;
        }

        public struct UserPrimitivesDrawSettings
        {
            public PrimitiveType PrimitiveType;
            public int PrimitiveCount;
            public Vector3[] VertexBuffer;
        }
        #endregion

        public struct GuiElement
        {
            public GuiElement(Texture2D aTexture, Rectangle aRectangle, Color aColor)
            {
                Texture = aTexture;
                Rectangle = aRectangle;
                Color = aColor;
            }

            public Texture2D Texture;
            public Rectangle Rectangle;
            public Color Color;
        }

        public struct TextElement
        {
            public TextElement(string aText, Vector2 aPosition, Color aColor)
            {
                Text = aText;
                Position = aPosition;
                Color = aColor;
            }

            public string Text;
            public Vector2 Position;
            public Color Color;
        }

        public IndexedDrawSettings DrawIndexedSettings = default(IndexedDrawSettings);
        
        public static Siat Singleton { get { return msSingleton; } }

        public bool bConsoleEnabled { get { return mbConsoleEnabled; } set { mbConsoleEnabled = value; } }
        public bool bStatsEnabled { get { return mbStatsEnabled; } set { mbStatsEnabled = value; } }
        public SiatEffect BuiltInEffect { get { return mBuiltInEffect; } }

        public bool bEnableSoftwareMouseCursor
        {
            get
            {
                return mbSoftwareCursor;
            }
            
            set
            {
                mbSoftwareCursor = value;

                if (mbSoftwareCursor)
                {
                    IsMouseVisible = false;
                }
            }
        }

        public CameraNode ActiveCamera
        {
            get
            {
                return mActiveCamera;
            }

            set
            {
                if (value != mActiveCamera)
                {
                    if (mActiveCamera != null)
                    {
                        mActiveCamera.bActive = false;
                    }

                    mActiveCamera = value;

                    if (mActiveCamera != null)
                    {
                        mActiveCamera.bActive = true;
                    }
                }
            }
        }

        /// <summary>
        /// Adds a line of text to be displayed in the rendering window.
        /// </summary>
        /// <param name="aLine">Text to be displayed.</param>
        /// <remarks>
        /// The line will display for one frame and disappear unless added again. 
        /// Siat.AddConsoleLine() is intended for displaying metrics that are updated per-frame.
        /// </remarks>
        public void AddConsoleLine(string aLine)
        {
            mConsole.Add(aLine);
        }

        /// <summary>
        /// Adds a GUI element to be drawn to the render window.
        /// </summary>
        /// <param name="aLine">GUI element to draw.</param>
        /// <remarks>
        /// The element will display for on frame and disappear unless added again.
        /// This is a temporary hack to allow displaying of 2D images.
        /// </remarks>
        public void AddGuiElement(ref GuiElement aElement)
        {
            mGuiElements.Add(aElement);
        }

        public void AddTextElement(ref TextElement aElement)
        {
            mTextElements.Add(aElement);
        }

        public void DrawIndexedPrimitives()
        {
            mDrawOpCount++;
            mMinPerOp = Utilities.Min(mMinPerOp, DrawIndexedSettings.PrimitiveCount);
            mMaxPerOp = Utilities.Max(mMaxPerOp, DrawIndexedSettings.PrimitiveCount);
            mFacetsCount += DrawIndexedSettings.PrimitiveCount;

            GraphicsDevice.DrawIndexedPrimitives(DrawIndexedSettings.PrimitiveType,
                                                 DrawIndexedSettings.BaseVertex,
                                                 DrawIndexedSettings.MinVertexIndex,
                                                 DrawIndexedSettings.NumberOfVertices,
                                                 DrawIndexedSettings.StartIndex,
                                                 DrawIndexedSettings.PrimitiveCount);
        }

        public uint FrameTick
        {
            get
            {
                return mFrameTick;
            }
        }

        public object GetPickingColor(Cell aCell, PoseableNode aNode)
        {
            object ret = mPickColor;
            mPickColor.PackedValue++;
            mPickTable.Add(ret, new PickingPair(aCell, aNode));

            return ret;
        }

        public delegate void PickingCallback(Cell c, PoseableNode n, float aDepth);
        public void Pick(int aMouseX, int aMouseY, PickingCallback aCallback)
        {
            bool bValid = aMouseX >= 0 && aMouseY >= 0 &&
                aMouseX < GraphicsDevice.PresentationParameters.BackBufferWidth &&
                aMouseY < GraphicsDevice.PresentationParameters.BackBufferHeight;

            if (bValid && mPickCallback == null && mActiveCamera != null && mActiveCamera.Cell != null)
            {
                Viewport viewport = GraphicsDevice.Viewport;
                Matrix inverseViewProjection = Shared.InverseViewProjectionTransform;

                Vector3 n = new Vector3(aMouseX, aMouseY, viewport.MinDepth);
                Vector3 f = new Vector3(aMouseX, aMouseY, viewport.MaxDepth);

                Vector3 wn = Utilities.UnProject(n, viewport, inverseViewProjection);
                Vector3 wf = Utilities.UnProject(f, viewport, inverseViewProjection);

                Vector3 direction = Vector3.Normalize(wf - wn);
                Ray worldRay = new Ray(wn, direction);

                mPickCallback = aCallback;
                mPickRectangle = new Rectangle(aMouseX, aMouseY, 1, 1);

                mActiveCamera.Cell.Pick(ref worldRay);
            }
        }

        public bool IsFullScreen { get { return mGraphicsDeviceManager.IsFullScreen; } }

        public void Resize(int aWidth, int aHeight, bool abFullscreen)
        {
            if (abFullscreen)
            {
                mWindowWidth = mGraphicsDeviceManager.GraphicsDevice.Viewport.Width;
                mWindowHeight = mGraphicsDeviceManager.GraphicsDevice.Viewport.Height;
            }

            mGraphicsDeviceManager.PreferredBackBufferWidth = aWidth;
            mGraphicsDeviceManager.PreferredBackBufferHeight = aHeight;
            mGraphicsDeviceManager.IsFullScreen = abFullscreen;
            mGraphicsDeviceManager.ApplyChanges();

            if (mbDoneLoadInternal)
            {
                #region Resize pick texture
                if (mPickTexture != null) { mPickTexture.Dispose(); mPickTexture = null; }

                PresentationParameters pms = GraphicsDevice.PresentationParameters;
                mPickTexture = new ResolveTexture2D(GraphicsDevice, pms.BackBufferWidth, pms.BackBufferHeight, 1, pms.BackBufferFormat);
                #endregion

                if (Deferred.bActive) { Deferred.OnResize(); }
                else { ForwardPost.OnResize(); }

                Cell.RefreshAll();
                if (OnResize != null) OnResize();
            }
        }

        public GameTime Time
        {
            get
            {
                return mCurrentTick;
            }
        }

        private int mWindowWidth = 800;
        private int mWindowHeight = 600;

        public void ToggleFullscreen()
        {
            if (mGraphicsDeviceManager.IsFullScreen)
            {
                Resize(mWindowWidth, mWindowHeight, false);
            }
            else
            {
                int desiredWidth = mGraphicsDeviceManager.GraphicsDevice.DisplayMode.Width;
                int desiredHeight = mGraphicsDeviceManager.GraphicsDevice.DisplayMode.Height;

                Resize(desiredWidth, desiredHeight, true);
            }
        }

        public MeshPart UnitBoxMeshPart { get { return mUnitBoxMeshPart; } }
        public MeshPart UnitFrustumMeshPart { get { return mUnitFrustumMeshPart; } }
        public MeshPart UnitQuadMeshPart { get { return mUnitQuadMeshPart; } }
        public MeshPart UnitSphereMeshPart { get { return mUnitSphereMeshPart; } }

        public Color BackColor { get { return mBackColor[0]; } set { mBackColor[0] = new Color(value.R, value.G, value.B, kBackAlpha); mbBackColorDirty = true; } }
        public Color ConsoleColor { get { return mConsoleColor; } set { mConsoleColor = value; } }

        public delegate void Callback();

        /// <summary>
        /// Callback called when content should be loaded.
        /// </summary>
        /// <remarks>
        /// This callback is called when content needs to be loaded. This occurs at
        /// engine startup and can also occur after a DeviceDestroyed event. 
        /// </remarks>
        public event Callback OnLoading;

        /// <summary>
        /// Callback called before a draw pass has begun.
        /// </summary>
        public event Callback OnDrawBegin;

        /// <summary>
        /// Callback called after a draw pass has completed.
        /// </summary>
        public event Callback OnDrawEnd;

        /// <summary>
        /// Callback called before a pose pass has begun.
        /// </summary>
        public event Callback OnPoseBegin;

        /// <summary>
        /// Callback called after a pose pass has completed.
        /// </summary>
        public event Callback OnPoseEnd;

        /// <summary>
        /// Callback called when a viewport resize occurs.
        /// </summary>
        public event Callback OnResize;

        /// <summary>
        /// Callback called before an update pass has begun.
        /// </summary>
        public event Callback OnUpdateBegin;

        /// <summary>
        /// Callback called after an update pass has completed.
        /// </summary>
        public event Callback OnUpdateEnd;

        /// <summary>
        /// Callback called when content should be unloaded.
        /// </summary>
        /// <remarks>
        /// This callback is called whenever content needs to be unloaded. This can occur
        /// when the engine shuts down and also when a DeviceDestroyed event occurs, which can
        /// happen (for example) when the display window is moved to a different monitor.
        /// </remarks>
        public event Callback OnUnloading;
    }
}
