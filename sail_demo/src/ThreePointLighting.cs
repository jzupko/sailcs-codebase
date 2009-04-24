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

// #define MOTIVATION_ONLY
// #define FIX_EYE
// #define FIX_KEY
// #define IMMEDIATE_CAMERA_SET

using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using sail;
using siat.render;
using siat.scene;

namespace siat
{
    public sealed class ThreePointLighting
    {
        public const UInt64 kThreePointLightingMask = (PoseableNode.kDefaultMask << 1);
        public const float kNearPlaneScale = 4.38e-4f;
        public const float kCamStep = 1.5f;

        #region Private members
        private bool mbAlwaysStep = false;
        private bool mbStep = false;
        private Vector3 mRelativeCenter = Vector3.Zero;
        private bool mbEnabled = false;
        private bool mbDumb = false;
        private Cell mCell;
        private Vector3 mKeyDiffuse;
        private Vector3 mKeySpecular;
        private Vector3 mBackDiffuse;
        private Vector3 mBackSpecular;
        private LightNode mBack;
        private LightNode mKey;
        private LightNode mFill;
        private SceneNode mNode;
        private List<LightNode> mMotivatingLights = new List<LightNode>();

        private ImageIlluminationMetrics mIdeal;
        private ThreePointSettings mMotivation = ThreePointSettings.kDefault;
        private ThreePointSettings mCurrent = ThreePointSettings.kDefault;

        private Matrix mCamera = Matrix.Identity;
        private Matrix mInvCamera = Matrix.Identity;
        private Radian mCameraYaw = Radian.kZero;
        private Radian mCameraPitch = Radian.kZero;
        private Radian mDesiredCameraYaw = Radian.kZero;
        private Radian mDesiredCameraPitch = Radian.kZero;

        private float mRigDistance = 0.0f;

        private LightLearner mLearner = new LightLearner();

        private Vector3 _GetCenter()
        {
            return (mNode.WorldPosition + mRelativeCenter);
        }

        private Radian _GetFillYaw(Radian aKeyYaw)
        {
            return (aKeyYaw - Radian.kPiOver2);
        }

        private Radian _GetBackYaw(Radian aKeyYaw)
        {
            return (aKeyYaw + Radian.kPiOver2 + Radian.kPiOver4);
        }

        private void _Get(ThreePointSettings aSettings, out Radian arKeyRoll, out Radian arKeyYaw, out Radian arFillYaw, out Radian arBackYaw)
        {
            arKeyRoll = aSettings.KeyRoll.ToRadians();
            arKeyYaw = aSettings.KeyYaw.ToRadians();

            arFillYaw = _GetFillYaw(arKeyYaw);
            arBackYaw = _GetBackYaw(arKeyYaw);
        }

        private Matrix _GetLocalTransform(Radian aRoll, Radian aYaw)
        {
            Matrix ret = Matrix.CreateRotationY(aYaw.Value) * Matrix.CreateRotationZ(aRoll.Value);

            return ret;
        }

        private Matrix _GetCameraTransform(Radian aYaw, Radian aPitch)
        {
            Matrix ret = Matrix.CreateRotationX(aPitch.Value) * Matrix.CreateRotationY(aYaw.Value);

            return ret;
        }

        private Matrix _GetInvCameraTransform(Radian aYaw, Radian aPitch)
        {
            Matrix ret = Matrix.CreateRotationY(-aYaw.Value) * Matrix.CreateRotationX(-aPitch.Value);

            return ret;
        }

        private Matrix _GetTransform(Radian aRoll, Radian aYaw)
        {
            Matrix trans = Matrix.CreateTranslation(Vector3.Backward * mRigDistance);
            Matrix postTrans = Matrix.CreateTranslation(_GetCenter());

            Matrix ret = trans * _GetLocalTransform(aRoll, aYaw) * mCamera * postTrans;

            return ret;
        }

        private float _GetAttenuation(LightNode l)
        {
            LightType type = l.Light.Type;

            if (type == LightType.Directional)
            {
                return 1.0f;
            }
            else
            {
                float d = Vector3.Distance(_GetCenter(), l.WorldPosition);
                if (d < 0.0f) { d = 0.0f; }

                float att = 1.0f;
                if (!Utilities.AboutZero(d))
                {
                    att = 1.0f /
                        (l.Light.LightAttenuation.X +
                        (l.Light.LightAttenuation.Y * d) +
                        (l.Light.LightAttenuation.Z * (d * d)));
                }

                if (type == LightType.Spot)
                {
                    Vector3 nlv = Utilities.SafeNormalize(_GetCenter() - l.WorldPosition);
                    if (Utilities.AboutZero(nlv)) { nlv = Vector3.Backward; }

                    float dot = Vector3.Dot(nlv, l.WorldLightDirection);
                    float spot = (float)Math.Pow(Utilities.Max(dot, 0.0f), l.Light.FalloffExponent);
                    if (dot < l.Light.FalloffCosHalfAngle) { spot = 0.0f; }

                    att *= spot;
                }

                return att;
            }
        }

        private Vector3 _GetDiffuseColor(LightNode l)
        {
            return (l.Light.LightDiffuse * _GetAttenuation(l));
        }

        private void _GetDiffuseAndSpecular(LightNode l, out Vector3 diff, out Vector3 spec)
        {
            float att = _GetAttenuation(l);
            diff = (l.Light.LightDiffuse * att);
            spec = (l.Light.LightSpecular * att);
        }

        private Vector3 _GetDirection(LightNode l)
        {
            Vector3 dir;
            LightType type = l.Light.Type;

            if (type == LightType.Directional)
            {
                dir = -l.WorldLightDirection;
            }
            else
            {
                dir = Utilities.SafeNormalize(l.WorldPosition - _GetCenter());
                if (Utilities.AboutZero(dir)) { dir = Vector3.Backward; }
            }

            return dir;
        }

        private Vector3 _GetKeyWorldDirection()
        {
            int count = mMotivatingLights.Count;
            Vector3 key = Vector3.Zero;

            for (int i = 0; i < count; i++)
            {
                if (mMotivatingLights[i].LightMask == kThreePointLightingMask) { continue; }

                Vector3 dir = _GetDirection(mMotivatingLights[i]);
                Vector3 color = _GetDiffuseColor(mMotivatingLights[i]);

                float d = Utilities.GetLuminance(color);

                key += (d * dir);
            }

            if (Utilities.AboutZero(key)) { key = Vector3.Backward; }
            else { key.Normalize(); }

            return key;
        }

        private void _GetRollYaw(ref Vector3 aDirection, out Radian arRoll, out Radian arYaw)
        {
            #region Roll
            {
                Vector2 n = new Vector2(aDirection.X, aDirection.Y);
                float len = n.Length();

                if (len > Utilities.kZeroToleranceFloat)
                {
                    n /= len;
                    arRoll = new Radian((float)Math.Atan2(n.Y, n.X));
                    if (arRoll < Radian.kZero) { arRoll += Radian.kTwoPi; }
                }
                else { arRoll = Radian.kZero; }
            }
            #endregion

            #region Yaw
            {
                Vector3 n = !Utilities.AboutZero(arRoll.Value) ? Vector3.TransformNormal(aDirection, Matrix.CreateRotationZ(-(arRoll.Value))) : aDirection;
                Vector2 n2 = new Vector2(n.Z, n.X);
                float len = n2.Length();
                if (len > Utilities.kZeroToleranceFloat)
                {
                    n2 /= len;
                    arYaw = new Radian((float)Math.Acos(n2.X));
                }
                else { arYaw = Radian.kZero; }

                Debug.Assert(arYaw.Value >= 0.0f && arYaw.Value <= MathHelper.Pi);
            }
            #endregion
        }

        private void _GetYawPitch(ref Vector3 aDirection, out Radian arYaw, out Radian arPitch)
        {
            arYaw = new Radian((float)Math.Atan2(aDirection.X, aDirection.Z));
            arPitch = new Radian(-(float)Math.Asin(aDirection.Y));
        }

        private void _UpdateHelper(out Vector3 arKeyWorldDir, out Vector3 arBackWorldDir)
        {
            float time = (float)Siat.Singleton.Time.ElapsedGameTime.TotalSeconds;

            #region Calculate new camera vector
            {
#if FIX_EYE
                Vector3 eye = Vector3.Left;
#else
                Vector3 eye = Vector3.Normalize(Vector3.TransformNormal(Vector3.Backward, Shared.InverseViewTransform));
#endif
                Debug.Assert(eye.Length() > Utilities.kLooseToleranceFloat);

                _GetYawPitch(ref eye, out mDesiredCameraYaw, out mDesiredCameraPitch);

#if IMMEDIATE_CAMERA_SET
                mCameraYaw = mDesiredCameraYaw;
                mCameraPitch = mDesiredCameraPitch;
                
#else
                float t = Utilities.Clamp(time * kCamStep, 0.0f, 1.0f);
                mCameraYaw = Radian.Lerp(mCameraYaw, mDesiredCameraYaw, t);
                mCameraPitch = Radian.Lerp(mCameraPitch, mDesiredCameraPitch, t);
#endif

                mCamera = _GetCameraTransform(mCameraYaw, mCameraPitch);
                mInvCamera = _GetInvCameraTransform(mCameraYaw, mCameraPitch);
            }
            #endregion

            Radian keyRoll;
            Radian keyYaw;

            #region Key direction
            {
#if FIX_KEY
                arKeyWorldDir = Vector3.Right;
#else
                arKeyWorldDir = _GetKeyWorldDirection();
#endif
                Vector3 localKeyDir = Vector3.Normalize(Vector3.TransformNormal(arKeyWorldDir, mInvCamera));

                _GetRollYaw(ref localKeyDir, out keyRoll, out keyYaw);
            }
            #endregion

            mMotivation.KeyRoll = keyRoll.ToDegrees();
            mMotivation.KeyYaw = keyYaw.ToDegrees();

            #region Fill and back
            {
                Radian backYaw = _GetBackYaw(keyYaw);
                Matrix backM = _GetTransform(keyRoll, backYaw);

                arBackWorldDir = Vector3.TransformNormal(Vector3.Backward, backM);
            }
            #endregion
        }

        private void __OnUpdateHandler(Cell aCell, SceneNode aNode)
        {
            if (mbDumb) { return; }

            float time = (float)Siat.Singleton.Time.ElapsedGameTime.TotalSeconds;

            mMotivatingLights.Clear();
            BoundingSphere sphere = mNode.WorldBounding;
            mCell.Query<LightNode>(ref sphere, mMotivatingLights);

            Vector3 keyWorldDir;
            Vector3 backWorldDir;
            _UpdateHelper(out keyWorldDir, out backWorldDir);

            float fillLum = 0.0f;
            mKeyDiffuse = Vector3.Zero;
            mKeySpecular = Vector3.Zero;
            mBackDiffuse = Vector3.Zero;
            mBackSpecular = Vector3.Zero;

            int count = mMotivatingLights.Count;
            for (int i = 0; i < count; i++)
            {
                if (mMotivatingLights[i].LightMask == kThreePointLightingMask) { continue; }

                Vector3 dir = _GetDirection(mMotivatingLights[i]);
                Vector3 diff;
                Vector3 spec;
                _GetDiffuseAndSpecular(mMotivatingLights[i], out diff, out spec);

                float keyDot = Utilities.Max(Vector3.Dot(dir, keyWorldDir), 0.0f);
                mKeyDiffuse += (keyDot * diff);
                mKeySpecular += (keyDot * spec);

                float floatDot = Utilities.Max(Vector3.Dot(dir, -keyWorldDir), 0.0f);
                fillLum += (floatDot * Utilities.GetLuminance(ref diff));

                float backDot = Utilities.Max(Vector3.Dot(dir, backWorldDir), 0.0f);
                mBackDiffuse += (backDot * diff);
                mBackSpecular += (backDot * spec);
            }

            float keyLuminance = Utilities.GetLuminance(ref mKeyDiffuse);
            float keyLuminanceFactor = (Utilities.GreaterThan(keyLuminance, 0.0f)) ? 1.0f / keyLuminance : 0.0f;
            mMotivation.Fill = (fillLum * keyLuminanceFactor);

            mbStep = true;
        }

        private SceneNode.Callback _OnUpdateHandler;
        #endregion

        public ThreePointLighting(Cell aCell, SceneNode aNode)
        {
            _OnUpdateHandler = __OnUpdateHandler;

            if (aCell == null || aNode == null)
            {
                throw new ArgumentNullException();
            }

            mCell = aCell;
            mNode = aNode;
        }

        public static ThreePointSettings Approximate(ref ImageIlluminationMetrics aMetrics)
        {
            ThreePointSettings ret = new ThreePointSettings(
                (aMetrics.Roll / ImageIlluminationMetrics.kRollMax) * Degree.k360,
                (aMetrics.Entropy / ImageIlluminationMetrics.kEntropyMax) * ThreePointSettings.kMaxFill,
                (aMetrics.Yaw / ImageIlluminationMetrics.kYawMax) * ThreePointSettings.kMaxYaw);

            return ret;
        }

        ~ThreePointLighting()
        {
            mKey.Parent = null;
            mFill.Parent = null;
            mBack.Parent = null;
        }

        public SceneNode Node
        {
            get { return mNode; }
            set
            {
                if (value != mNode)
                {
                    if (value == null) { throw new ArgumentNullException("Target node of ThreePointLighting cannot be null."); }

                    bool b = mbEnabled;
                    bEnabled = false;
                    mNode = value;
                    bEnabled = b;
                }
            }
        }

        public bool bAlwaysStep { get { return mbAlwaysStep; } set { mbAlwaysStep = value; } }

        public bool bDumb
        {
            get
            {
                return mbDumb;
            }
            
            set
            {
                mbDumb = value;

                if (mbDumb)
                {
                    mKeyDiffuse = Vector3.One;
                    mKeySpecular = Vector3.One;
                    mBackDiffuse = Vector3.One;
                    mBackSpecular = Vector3.One;
                }
                else
                {
                    __OnUpdateHandler(null, null);
                }
            }
        }

        public ThreePointSettings Current { get { return mCurrent; } set { mCurrent = value;  bDumb = true; } }
        public LightNode Key { get { return mKey; } }
        public LightNode Fill { get { return mFill; } }
        
        public void Update() { __OnUpdateHandler(null, null); }

        public bool bEnabled
        {
            get
            {
                return mbEnabled;
            }

            set
            {
                if (value != mbEnabled)
                {
                    if (mbEnabled)
                    {
                        mKey.bEnablePosing = false;
                        mFill.bEnablePosing = false;
                        mBack.bEnablePosing = false;
                        mNode.Apply<PoseableNode>(SceneNode.ApplyType.RecurseDown, SceneNode.ApplyStop.Delegate,
                            delegate(PoseableNode n)
                            {
                                n.LightMask = PoseableNode.kDefaultMask;
                                return false;
                            });
                        mNode.OnUpdateEnd -= _OnUpdateHandler;
                    }
                    else
                    {
                        SceneNode root = mCell.WaitForRootSceneNode;

                        if (mKey == null)
                        {
                            mKey = new LightNode(); 
                            mKey.Parent = root;

                            mKey.Light.Type = LightType.Point;
                            mKey.LightMask = kThreePointLightingMask;
                            mKey.ShadowMask = kThreePointLightingMask;
                        }

                        if (mFill == null)
                        {
                            mFill = new LightNode();
                            mFill.Parent = root;

                            mFill.Light.Type = LightType.Directional;
                            mFill.LightMask = kThreePointLightingMask;
                            mFill.ShadowMask = kThreePointLightingMask;
                        }

                        if (mBack == null)
                        {
                            mBack = new LightNode();
                            mBack.Parent = root;

                            mBack.Light.Type = LightType.Point;
                            mBack.LightMask = kThreePointLightingMask;
                            mBack.ShadowMask = kThreePointLightingMask;
                        }

                        mKey.bEnablePosing = true;
                        mFill.bEnablePosing = true;
                        mBack.bEnablePosing = true;
                        mNode.OnUpdateEnd += _OnUpdateHandler;
                        mNode.Apply<PoseableNode>(SceneNode.ApplyType.RecurseDown, SceneNode.ApplyStop.Delegate,
                            delegate(PoseableNode n)
                            {
                                n.LightMask = kThreePointLightingMask;
                                return false;
                            });

                        float fov = MathHelper.ToRadians(60.0f);
                        float radius = mNode.WorldBounding.Radius;
                        float twoRadius = 2.0f * radius;
                        float nearPlane = kNearPlaneScale * twoRadius;
                        float farPlane = 2.0f * twoRadius;
                        mRigDistance = (nearPlane + ((float)Math.Sqrt(2.0f) * radius / (float)Math.Tan(fov * 0.5f)));
                        _OnUpdateHandler(mCell, mNode);
                    }

                    mbEnabled = !mbEnabled;
                }
            }
        }

        public Vector3 RelativeCenter { get { return mRelativeCenter; } set { mRelativeCenter = value; } }

        public ImageIlluminationMetrics Ideal { get { return mIdeal; } set { mIdeal = value; } }
        public LightLearner Learner { get { return mLearner; } }

        public void Tick()
        {
            if (mbEnabled)
            {
                float time = (float)Siat.Singleton.Time.ElapsedGameTime.TotalSeconds;

                if (!mbDumb)
                {
#if MOTIVATION_ONLY
                    mCurrent = mMotivation;
#else
                    Learner.Step(ref mIdeal, ref mCurrent, ref mMotivation, time);
#endif
                    if (mbAlwaysStep) { mbStep = true; }
                }

                #region Calculate new key and fill settings.
                if (mbStep)
                {
                    mKey.Light.LightDiffuse = mKeyDiffuse;
                    mKey.Light.LightSpecular = mKeySpecular;

                    mFill.Light.LightDiffuse = mCurrent.Fill * mKeyDiffuse;

                    float fill = Utilities.GetLuminance(mFill.Light.LightDiffuse);
                    float bd = Utilities.GetLuminance(mBackDiffuse);
                    float bs = Utilities.GetLuminance(mBackSpecular);

                    float bdf = (Utilities.GreaterThan(bd, 0.0f) && bd > fill) ? (fill / bd) : 1.0f;
                    float bsf = (Utilities.GreaterThan(bs, 0.0f) && bs > fill) ? (fill / bs) : 1.0f;

                    mBack.Light.LightDiffuse = (mBackDiffuse * bdf);
                    mBack.Light.LightSpecular = (mBackSpecular * bsf);
                }

                if (mbDumb || mbStep)
                {
                    Radian keyRoll;
                    Radian keyYaw;
                    Radian fillYaw;
                    Radian backYaw;
                    _Get(mCurrent, out keyRoll, out keyYaw, out fillYaw, out backYaw);

                    mKey.WorldTransform = _GetTransform(keyRoll, keyYaw);
                    mFill.WorldTransform = _GetTransform(keyRoll, fillYaw);
                    mBack.WorldTransform = _GetTransform(keyRoll, backYaw);

                    mbStep = false;
                }
                #endregion
            }
        }
    }
}
