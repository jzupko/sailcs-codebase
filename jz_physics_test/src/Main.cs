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
using jz.physics;
using jz.physics.broadphase;
using jz.physics.narrowphase;
using siat;
using siat.scene;
using siat.render;
using siat.pipeline.collada;
using siat.pipeline.collada.elements;

namespace jz
{
    public static class Program
    {
        const string kLogFile = "jz_physics_test.log";

        private static void _PairTableTest()
        {
            Random r = new Random();
            PairTable table = new PairTable();
            Pair pair = new Pair();

            for (int i = 0; i < 1000000; i++)
            {
                int s = (r.Next(3) - 1);
                ushort a = (ushort)(r.Next());
                ushort b = (ushort)(r.Next());

                switch (s)
                {
                    case -1: table.Add(a, b); break;
                    case 0: table.Remove(a, b); break;
                    case 1: table.Get(a, b, ref pair); break;
                    default: break;
                }
            }

            Pair[] pairs = new Pair[0];
            table.GetAllPairs(ref pairs);
        }

        private const int kBoxCount = 5;

        private static SceneNodePoser mPoser;
        private static BoxBody[] mBodies = new BoxBody[kBoxCount];
        private static MeshPartNode[] mMeshes = new MeshPartNode[kBoxCount];

        private static World mWorld = new World(new Sap());

        private static void _OnLoad()
        {
            Siat siat = Siat.Singleton;
            
            SceneNode root = new SceneNode();
            CameraNode camera = new CameraNode(null);
            camera.bActive = true;
            camera.Parent = root;
            float aspectRatio = (float)siat.GraphicsDevice.Viewport.Width /
                (float)siat.GraphicsDevice.Viewport.Height;
            camera.ProjectionTransform = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspectRatio, 0.01f, (float)(kBoxCount * 5) + 10.0f);
            camera.WorldTransform =
                Matrix.CreateRotationY(MathHelper.PiOver2) * 
                Matrix.CreateTranslation(Vector3.Right * (float)(kBoxCount * 5));

            LightNode light = new LightNode();
            light.Light.Type = LightType.Directional;
            light.Light.LightDiffuse = Color.White.ToVector3();
            light.WorldPosition = Vector3.Backward * 10.0f;

            Vector3 offset = Vector3.Up * 2.0f + Vector3.Forward * 0.0f;
            Vector3 cur = Vector3.Zero;

            mPoser = new SceneNodePoser(root);
            mWorld.Gravity *= 1.0f;

            for (int i = 0; i < kBoxCount; i++)
            {
                mMeshes[i] = new MeshPartNode();
                mMeshes[i].Parent = root;
                mMeshes[i].MeshPart = siat.UnitBoxMeshPart;
                mMeshes[i].Effect = siat.BuiltInEffect;
                mMeshes[i].bDrawBoundingBox = true;
                mMeshes[i].WorldPosition = cur;
                cur += offset;
            }

            BoundingBox box = new BoundingBox(-Vector3.One, Vector3.One);

            Matrix m = mMeshes[0].WorldTransform;
            mBodies[0] = new BoxBody(ref box);
            mBodies[0].Frame = new CoordinateFrame(Matrix3.Identity, m.Translation);
            mBodies[0].Type = BodyFlags.kStatic;
            mBodies[0].CollidesWith = BodyFlags.kDynamic;
            mBodies[0].World = mWorld;

            for (int i = 1; i < kBoxCount; i++)
            {
                m = mMeshes[i].WorldTransform;
                mBodies[i] = new BoxBody(ref box);
                mBodies[i].Frame = new CoordinateFrame(Matrix3.Identity, m.Translation);
                mBodies[i].Type = BodyFlags.kDynamic;
                mBodies[i].CollidesWith = BodyFlags.kDynamic | BodyFlags.kStatic;
                mBodies[i].World = mWorld;
            }
        }

        private static void _OnPreUpdate()
        {
            mWorld.Tick((float)Siat.Singleton.Time.ElapsedGameTime.TotalSeconds);
        }

        private static void _OnKeyHandler(KeyState aState, Keys aKey)
        {
            if (aState == KeyState.Up)
            {
                if (aKey == Keys.Escape) { Siat.Singleton.Exit(); }
                else if (aKey == Keys.F1)
                {
                    mMeshes[0].bDrawBoundingBox = (mBodies[0].World == null);
                    mBodies[0].World = (mBodies[0].World == null) ? mBodies[0].World = mWorld : null;
                }
            }
        }

        private static void _OnPostUpdate()
        {
            for (int i = 0; i < kBoxCount; i++)
            {
                mMeshes[i].WorldTransform = mBodies[i].Frame.ToMatrix();
            }

            mPoser.Node.Update(null, ref Utilities.kIdentity, false);
            mPoser.StartPose();
        }

        private static void _RigidBodyTest()
        {
            Siat siat = Siat.Singleton;
            siat.OnLoading += _OnLoad;
            siat.OnUpdateBegin += _OnPreUpdate;
            siat.OnUpdateEnd += _OnPostUpdate;
            Input.Singleton.AddKeyCallback(Keys.Escape, _OnKeyHandler);
            Input.Singleton.AddKeyCallback(Keys.F1, _OnKeyHandler);
            
            siat.Run();
        }

        private static void _ColladaTest()
        {
            string filename = "..\\..\\sail_demo\\media\\1930_room\\SIAT_room_0013_lights.dae";

            ColladaCOLLADA doc;
            ColladaDocument.Load(filename, out doc);

            ColladaProcessor proc = new ColladaProcessor();
            proc.ProcessPhysics = true;
            proc.Process(doc, null);
        }

        private static void _OnAnimationLoad()
        {
            Cell cell = Cell.GetCell("woman");
            CameraEditingNode camera = new CameraEditingNode(cell);
            camera.ProjectionTransform = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, 800.0f / 600.0f, 0.1f, 1000.0f);
            camera.bActive = true;

            LightNode node = new LightNode();
            node.Light.Type = LightType.Directional;
            node.Light.LightDiffuse = Color.White.ToVector3();
            node.Parent = cell.WaitForRootSceneNode;

            cell.WaitForRootSceneNode.Apply<AnimatedMeshPartNode>(SceneNode.ApplyType.RecurseDown,
                SceneNode.ApplyStop.Delegate, delegate(AnimatedMeshPartNode e)
                {
                    e.AnimationControl.StartIndex = 10;
                    e.AnimationControl.EndIndex = 92;
                    e.AnimationControl.bPlay = true;
                    return false;
                });
        }

        private static void _AnimationTest()
        {
            Siat siat = Siat.Singleton;
            siat.OnLoading += _OnAnimationLoad;
            Input.Singleton.AddKeyCallback(Keys.Escape, _OnKeyHandler);

            siat.Run();
        }

        private const int kRasterizerWidth = 512;
        private const int kRasterizerHeight = 512;

        private static readonly Vector3 kBox0 = new Vector3(-1, -1, -1);
        private static readonly Vector3 kBox1 = new Vector3(-1, -1,  1);
        private static readonly Vector3 kBox2 = new Vector3(-1,  1, -1);
        private static readonly Vector3 kBox3 = new Vector3(-1,  1,  1);
        private static readonly Vector3 kBox4 = new Vector3( 1, -1, -1);
        private static readonly Vector3 kBox5 = new Vector3( 1, -1,  1);
        private static readonly Vector3 kBox6 = new Vector3( 1,  1, -1);
        private static readonly Vector3 kBox7 = new Vector3( 1,  1,  1);

        private static readonly Triangle[] kRasterizerData = new Triangle[]
            {
                new Triangle(kBox0, kBox2, kBox4),
                new Triangle(kBox2, kBox6, kBox4),

                new Triangle(kBox4, kBox6, kBox5),
                new Triangle(kBox6, kBox7, kBox5),

                new Triangle(kBox5, kBox7, kBox1),
                new Triangle(kBox7, kBox3, kBox1),

                new Triangle(kBox1, kBox3, kBox0),
                new Triangle(kBox3, kBox2, kBox0),

                new Triangle(kBox2, kBox3, kBox6),
                new Triangle(kBox3, kBox7, kBox6),

                new Triangle(kBox1, kBox0, kBox5),
                new Triangle(kBox0, kBox4, kBox5)
            };

        private static DepthRasterizer msRasterizer = new DepthRasterizer(kRasterizerWidth, kRasterizerHeight);
        private static Texture2D msRasterizerTexture = null;
        private static Siat.GuiElement msRasterizerElement;
        private static void _OnRasterizerLoad()
        {
            if (msRasterizerTexture != null) { msRasterizerTexture.Dispose(); }

            Siat siat = Siat.Singleton;
            GraphicsDevice gd = siat.GraphicsDevice;

            siat.Resize(kRasterizerWidth, kRasterizerHeight, false);
            Rectangle rect = new Rectangle(0, 0, gd.PresentationParameters.BackBufferWidth, gd.PresentationParameters.BackBufferHeight);

            msRasterizerTexture = new Texture2D(Siat.Singleton.GraphicsDevice, kRasterizerWidth, kRasterizerHeight, 1, TextureUsage.None, SurfaceFormat.Color);
            msRasterizerElement = new Siat.GuiElement(msRasterizerTexture, rect, Color.White);

            msRasterizer.ProjectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver2, 1.0f, 0.1f, 18.0f);
            msRasterizer.ViewMatrix = Matrix.Invert(Matrix.CreateTranslation(Vector3.Backward * 8.0f));

            msRasterizer.CullMode = Utilities.kBackFaceCulling;
        }

        private static float mAngle = 0.0f;
        private static void _OnRasterizerUpdate()
        {
            mAngle += (float)Siat.Singleton.Time.ElapsedGameTime.TotalSeconds;
            if (mAngle > MathHelper.TwoPi) { mAngle -= MathHelper.TwoPi; }

            msRasterizer.WorldMatrix = Matrix.CreateRotationY(mAngle);
        }

        private static void _OnRasterizerDraw()
        {
            msRasterizer.Clear(1.0f);
            int count = kRasterizerData.Length;
            for (int j = 0; j < 5; j++)
            {
                for (int i = 0; i < count; i++)
                {
                    msRasterizer.Rasterize(kRasterizerData[i]);
                }
                msRasterizer.WorldMatrix *= Matrix.CreateTranslation(Vector3.Up * 2.1f);
            }

            msRasterizer.Get(msRasterizerTexture);

            Siat siat = Siat.Singleton;
            siat.bStatsEnabled = true;
            siat.ConsoleColor = Color.Black;
            siat.AddGuiElement(ref msRasterizerElement);
        }

        private static void _DepthSoftwareRasterizerTest()
        {
            Siat siat = Siat.Singleton;
            siat.OnLoading += _OnRasterizerLoad;
            siat.OnDrawBegin += _OnRasterizerDraw;
            siat.OnUpdateBegin += _OnRasterizerUpdate;
            Input.Singleton.AddKeyCallback(Keys.Escape, _OnKeyHandler);

            siat.Run();
        }

        public static void Go()
        {
            // _PairTableTest();
            _RigidBodyTest();
            // _ColladaTest();
            // _AnimationTest();
            // _DepthSoftwareRasterizerTest();
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
