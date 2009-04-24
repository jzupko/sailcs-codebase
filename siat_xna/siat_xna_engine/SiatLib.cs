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

using SiatId = System.IntPtr;
using SiatRlt = System.Int32;

using System;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;

namespace siat
{
    #region Basic types of Siat C library
    public enum SiatResultCodes
    {
        Ok = 0,
        OutOfRange = 1,
        AlreadyInit = 2,
        NotInit = 3,
        InitFailed = 4,
        Exception = 5,
        NoActiveEntity = 6,
        InvalidParameter = 7
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SiatFloat3
    {
        public SiatFloat3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public SiatFloat3(ref Vector3 u)
        {
            X = u.X;
            Y = u.Y;
            Z = u.Z;
        }

        public Vector3 ToVector3()
        {
            return new Vector3(X, Y, Z);
        }

        public float X;
        public float Y;
        public float Z;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SiatFloat4
    {
        public SiatFloat4(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public SiatFloat4(ref Vector4 u)
        {
            X = u.X;
            Y = u.Y;
            Z = u.Z;
            W = u.W;
        }

        public SiatFloat4(ref Quaternion q)
        {
            X = q.X;
            Y = q.Y;
            Z = q.Z;
            W = q.W;
        }

        public Quaternion ToQuaternion()
        {
            return new Quaternion(X, Y, Z, W);
        }

        public Vector4 ToVector4()
        {
            return new Vector4(X, Y, Z, W);
        }

        public float X;
        public float Y;
        public float Z;
        public float W;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SiatMatrix4
    {
        public float M11; public float M12; public float M13; public float M14;
        public float M21; public float M22; public float M23; public float M24;
        public float M31; public float M32; public float M33; public float M34;
        public float M41; public float M42; public float M43; public float M44;

        public SiatMatrix4(float m11, float m12, float m13, float m14,
                           float m21, float m22, float m23, float m24,
                           float m31, float m32, float m33, float m34,
                           float m41, float m42, float m43, float m44)
        {
            M11 = m11; M12 = m12; M13 = m13; M14 = m14;
            M21 = m21; M22 = m22; M23 = m23; M24 = m24;
            M31 = m31; M32 = m32; M33 = m33; M34 = m34;
            M41 = m41; M42 = m42; M43 = m43; M44 = m44;
        }

        public SiatMatrix4(ref Matrix m)
        {
            M11 = m.M11; M12 = m.M12; M13 = m.M13; M14 = m.M14;
            M21 = m.M21; M22 = m.M22; M23 = m.M23; M24 = m.M24;
            M31 = m.M31; M32 = m.M32; M33 = m.M33; M34 = m.M34;
            M41 = m.M41; M42 = m.M42; M43 = m.M43; M44 = m.M44;
        }

        public Matrix ToMatrix()
        {
            return new Matrix(M11, M12, M13, M14,
                              M21, M22, M23, M24,
                              M31, M32, M33, M34,
                              M41, M42, M43, M44);
        }
    }

    public enum SiatEleLightChangeOp
    {
        Color = 0,
        Orientaiton = 1,
        Position = 2,
        Range = 3,
        NumberOfCodes = 4
    }
    #endregion

    #region ELE Callbacks
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate SiatId SiatEleLightCreateCallback();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void SiatEleLightChangeCallback(SiatEleLightChangeOp aOp, SiatId aId, ref SiatFloat4 aData);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void SiatEleLightDestroyCallback(SiatId aId);
    #endregion

    /// <summary>
    /// Provides access to the siat C++ support library, which currently includes 
    /// Boost physics and the ELE lighting system.
    /// </summary>
    public sealed class SiatLib
    {
        #region Private members
        #region DLL imports
        #region Init
        [DllImport(kSiatLibDll, EntryPoint = "siatInit")]
        private static extern SiatRlt siatInit();

        [DllImport(kSiatLibDll, EntryPoint = "siatDeinit")]
        private static extern SiatRlt siatDeinit();
        #endregion

        #region ELE
        [DllImport(kSiatLibDll, EntryPoint = "siatEleRun")]
        private static extern SiatRlt siatEleRun(SiatEleLightCreateCallback aCreate, SiatEleLightChangeCallback aChange, SiatEleLightDestroyCallback aDestroy);

        #region Camera
        [DllImport(kSiatLibDll, EntryPoint = "siatEleGetCameraOrientation")]
        private static extern SiatRlt siatEleGetCameraOrientation(out SiatFloat4 aOutQuat);

        [DllImport(kSiatLibDll, EntryPoint = "siatEleSetCameraOrientation")]
        private static extern SiatRlt siatEleSetCameraOrientation(ref SiatFloat4 aQuat);

        [DllImport(kSiatLibDll, EntryPoint = "siatEleGetCameraPosition")]
        private static extern SiatRlt siatEleGetCameraPosition(out SiatFloat3 aOutPos);

        [DllImport(kSiatLibDll, EntryPoint = "siatEleSetCameraPosition")]
        private static extern SiatRlt siatEleSetCameraPosition(ref SiatFloat3 aPos);
        #endregion

        #region Characters
        [DllImport(kSiatLibDll, EntryPoint = "siatEleCreateCharacter")]
        private static extern SiatRlt siatEleCreateCharacter(out SiatId aOutId);

        [DllImport(kSiatLibDll, EntryPoint = "siatEleDestroyCharacter")]
        private static extern SiatRlt siatEleDestroyCharacter(SiatId aId);

        [DllImport(kSiatLibDll, EntryPoint = "siatEleCharacter")]
        private static extern SiatRlt siatEleCharacter(SiatId aId);

        [DllImport(kSiatLibDll, EntryPoint = "siatEleGetCharacterOrientation")]
        private static extern SiatRlt siatEleGetCharacterOrientation(out SiatFloat4 aOutQuat);

        [DllImport(kSiatLibDll, EntryPoint = "siatEleSetCharacterOrientation")]
        private static extern SiatRlt siatEleSetCharacterOrientation(ref SiatFloat4 aQuat);

        [DllImport(kSiatLibDll, EntryPoint = "siatEleGetCharacterPosition")]
        private static extern SiatRlt siatEleGetCharacterPosition(out SiatFloat3 aOutPos);

        [DllImport(kSiatLibDll, EntryPoint = "siatEleSetCharacterPosition")]
        private static extern SiatRlt siatEleSetCharacterPosition(ref SiatFloat3 aPos);

        [DllImport(kSiatLibDll, EntryPoint = "siatEleGetCharacterBounding")]
        private static extern SiatRlt siatEleGetCharacterBounding(out float aOutRadius, out float aOutHeight);

        [DllImport(kSiatLibDll, EntryPoint = "siatEleSetCharacterBounding")]
        private static extern SiatRlt siatEleSetCharacterBounding(float aRadius, float aHeight);
        #endregion

        #region Stage
        [DllImport(kSiatLibDll, EntryPoint = "siatEleCreateStage")]
        private static extern SiatRlt siatEleCreateStage(out SiatId aOutId);

        [DllImport(kSiatLibDll, EntryPoint = "siatEleDestroyStage")]
        private static extern SiatRlt siatEleDestroyStage(SiatId aId);

        [DllImport(kSiatLibDll, EntryPoint = "siatEleStage")]
        private static extern SiatRlt siatEleStage(SiatId aId);

        [DllImport(kSiatLibDll, EntryPoint = "siatEleGetStageDimensions")]
        private static extern SiatRlt siatEleGetStageDimensions(out SiatFloat3 aOutDimensions);

        [DllImport(kSiatLibDll, EntryPoint = "siatEleSetStageDimensions")]
        private static extern SiatRlt siatEleSetStageDimensions(float aWidth, float aHeight, float aDepth);

        [DllImport(kSiatLibDll, EntryPoint = "siatEleGetStagePosition")]
        private static extern SiatRlt siatEleGetStagePosition(out SiatFloat3 aOutPos);

        [DllImport(kSiatLibDll, EntryPoint = "siatEleSetStagePosition")]
        private static extern SiatRlt siatEleSetStagePosition(ref SiatFloat3 aPos);
        #endregion

        #region Settings
        [DllImport(kSiatLibDll, EntryPoint = "siatEleGetSetting")]
        private static extern SiatRlt siatEleGetSetting(string aKey, out float aValue);

        [DllImport(kSiatLibDll, EntryPoint = "siatEleSetSetting")]
        private static extern SiatRlt siatEleSetSetting(string aKey, float aValue);
        #endregion
        #endregion
        #endregion

        private void _CheckReturnCode(SiatRlt aReturnCode)
        {
            SiatResultCodes code = (SiatResultCodes)aReturnCode;

            if (code != SiatResultCodes.Ok)
            {
                throw new Exception("Siat C Lib Exception: " + Enum.GetName(typeof(SiatResultCodes), code));
            }
        }
        #endregion

        #region Singleton implementations
        private static readonly SiatLib mSingleton = new SiatLib();

        public static SiatLib Singleton
        {
            get
            {
                return mSingleton;
            }
        }

        private SiatLib()
        {
            _CheckReturnCode(siatInit());
        }

        ~SiatLib()
        {
            siatDeinit();
        }
        #endregion

        public static readonly SiatId kInvalidId = SiatId.Zero;

#if DEBUG
            public const string kSiatLibDll = "..\\..\\..\\siat\\bin\\siat_d.dll";
#else
        public const string kSiatLibDll = "..\\..\\..\\siat\\bin\\siat.dll";
#endif

        #region ELE functions
        public void RunEle(SiatEleLightCreateCallback aCreate, SiatEleLightChangeCallback aChange, SiatEleLightDestroyCallback aDestroy)
        {
            _CheckReturnCode(siatEleRun(aCreate, aChange, aDestroy));
        }

        #region ELE Camera functions
        public Quaternion EleCameraOrientation
        {
            get
            {
                SiatFloat4 ori;
                _CheckReturnCode(siatEleGetCameraOrientation(out ori));

                return ori.ToQuaternion();
            }

            set
            {
                SiatFloat4 ori = new SiatFloat4(ref value);
                _CheckReturnCode(siatEleSetCameraOrientation(ref ori));
            }
        }

        public Vector3 EleCameraPosition
        {
            get
            {
                SiatFloat3 pos;
                _CheckReturnCode(siatEleGetCameraPosition(out pos));

                return pos.ToVector3();
            }

            set
            {
                SiatFloat3 pos = new SiatFloat3(ref value);
                _CheckReturnCode(siatEleSetCameraPosition(ref pos));
            }
        }
        #endregion

        #region ELE Character functions
        public SiatId EleCreateCharacter()
        {
            SiatId id;
            _CheckReturnCode(siatEleCreateCharacter(out id));

            return id;
        }

        public void EleDestroyCharacter(SiatId aId)
        {
            _CheckReturnCode(siatEleDestroyCharacter(aId));
        }

        public void EleCharacter(SiatId aId)
        {
            _CheckReturnCode(siatEleCharacter(aId));
        }

        public Cylinder EleCharacterBounding
        {
            get
            {
                Cylinder ret;
                _CheckReturnCode(siatEleGetCharacterBounding(out ret.Radius, out ret.Height));

                return ret;
            }

            set
            {
                _CheckReturnCode(siatEleSetCharacterBounding(value.Radius, value.Height));
            }
        }

        public Quaternion EleCharacterOrientation
        {
            get
            {
                SiatFloat4 ori;
                _CheckReturnCode(siatEleGetCharacterOrientation(out ori));

                return ori.ToQuaternion();
            }

            set
            {
                SiatFloat4 ori = new SiatFloat4(ref value);
                _CheckReturnCode(siatEleSetCharacterOrientation(ref ori));
            }
        }

        public Vector3 EleCharacterPosition
        {
            get
            {
                SiatFloat3 pos;
                _CheckReturnCode(siatEleGetCharacterPosition(out pos));

                return pos.ToVector3();
            }

            set
            {
                SiatFloat3 pos = new SiatFloat3(ref value);
                _CheckReturnCode(siatEleSetCharacterPosition(ref pos));
            }
        }
        #endregion

        #region ELE Stage functions
        public SiatId EleCreateStage()
        {
            SiatId id;
            _CheckReturnCode(siatEleCreateStage(out id));

            return id;
        }

        public void EleDestroyStage(SiatId aId)
        {
            _CheckReturnCode(siatEleDestroyStage(aId));
        }

        public void EleStage(SiatId aId)
        {
            _CheckReturnCode(siatEleStage(aId));
        }

        public Vector3 EleStageDimensions
        {
            get
            {
                SiatFloat3 dimensions;
                _CheckReturnCode(siatEleGetStageDimensions(out dimensions));

                return dimensions.ToVector3();
            }

            set
            {
                SiatFloat3 dimensions = new SiatFloat3(ref value);
                _CheckReturnCode(siatEleSetStageDimensions(dimensions.X, dimensions.Y, dimensions.Z));
            }
        }

        public Vector3 EleStagePosition
        {
            get
            {
                SiatFloat3 pos;
                _CheckReturnCode(siatEleGetStagePosition(out pos));

                return pos.ToVector3();
            }

            set
            {
                SiatFloat3 pos = new SiatFloat3(ref value);
                _CheckReturnCode(siatEleSetStagePosition(ref pos));
            }
        }
        #endregion

        #region ELE Settings functions
        public float EleGetSetting(string aKey)
        {
            float value;
            _CheckReturnCode(siatEleGetSetting(aKey, out value));

            return value;
        }

        public void EleSetSetting(string aKey, float aValue)
        {
            _CheckReturnCode(siatEleSetSetting(aKey, aValue));
        }
        #endregion
        #endregion
    }
}