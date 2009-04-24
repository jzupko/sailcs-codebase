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

#define ENABLE_CLEANUP

using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using siat;

namespace sail
{
    public struct ThreePointSettings
    {
        public static readonly ThreePointSettings kDefault = new ThreePointSettings(Degree.kZero, 0.0f, Degree.kZero);

        public const float kMinFill = 0.0f;
        public const float kMaxFill = 0.5f;
        public static readonly Degree kMinYaw = new Degree(0.0f);
        public static readonly Degree kMaxYaw = new Degree(135.0f);

        #region Private members
        private float mFill;
        private Degree mKeyRoll;
        private Degree mKeyYaw;
        #endregion

        public ThreePointSettings(Degree aKeyRoll, float aFill, Degree aKeyYaw)
        {
            mKeyRoll = aKeyRoll;
            while (mKeyRoll > Degree.k360) { mKeyRoll -= Degree.k360; }
            while (mKeyRoll < Degree.kZero) { mKeyRoll += Degree.k360; }
            mFill = aFill;
            mKeyYaw = aKeyYaw;
            Debug.Assert(mFill >= 0.0f);
            Debug.Assert(mKeyYaw >= Degree.kZero && mKeyYaw <= Degree.k180);
        }

        public float Fill
        {
            get { return mFill; }
            set
            {
                mFill = value;
                Debug.Assert(mFill >= 0.0f);
            }
        }

        public Degree KeyRoll
        {
            get
            {
                return mKeyRoll;
            }

            set
            {
                mKeyRoll = value;
                while (mKeyRoll > Degree.k360) { mKeyRoll -= Degree.k360; }
                while (mKeyRoll < Degree.kZero) { mKeyRoll += Degree.k360; }
            }
        }

        public Degree KeyYaw
        {
            get { return mKeyYaw; }
            set
            {
                mKeyYaw = value;
                Debug.Assert(mKeyYaw >= Degree.kZero && mKeyYaw <= Degree.k180);
            }
        }
    }

    public class LightLearner
    {
        #region Constants
        public const float kStepSize = 3.0f;
        public const float kMotWeight = 0.7f;
        public const float kDesiredWeight = (1.0f - kMotWeight);

        public const int kSegments = 12;
        public const int kSegmentsPlus1 = kSegments + 1;
        public const int kSegmentsPlus1Square = kSegmentsPlus1 * kSegmentsPlus1;
        public const float kSegmentsF = kSegments;

        public const int kStorage = kSegments * kSegmentsPlus1Square;

        public const float kJitterFactor = (kSegmentsF / 24.0f) * 0.80f;

        public const float kFillFactor = ThreePointSettings.kMaxFill / kSegmentsF;
        public const float kFillFactorHalf = 0.5f * kFillFactor;
        public const float kFillJitterScale = kFillFactor * kJitterFactor;
        public const float kFillJitterScaleAdjust = 0.5f * kFillJitterScale;

        public static readonly float kRollFactor = Degree.k360.Value / kSegmentsF;
        public static readonly float kRollFactorHalf = 0.5f * kRollFactor;
        public static readonly float kRollJitterScale = kRollFactor * kJitterFactor;
        public static readonly float kRollJitterScaleAdjust = 0.5f * kRollJitterScale;

        public static readonly float kYawFactor = ThreePointSettings.kMaxYaw.Value / kSegmentsF;
        public static readonly float kYawFactorHalf = 0.5f * kYawFactor;
        public static readonly float kYawJitterScale = kYawFactor * kJitterFactor;
        public static readonly float kYawJitterScaleAdjust = 0.5f * kYawJitterScale;
        #endregion

        #region Private members
        private int mR = 0;
        private int mF = 0;
        private int mY = 0;
        private Random mRandom = null;
        private ImageIlluminationMetrics[] mSamples = null;

        private void _Read(BinaryReader reader)
        {
            _Init();

            for (int i = 0; i < kStorage; i++)
            {
                mSamples[i].Entropy = reader.ReadSingle();
                mSamples[i].MaxIntensity = reader.ReadSingle();
                mSamples[i].Roll = reader.ReadSingle();
                mSamples[i].Yaw = reader.ReadSingle();
            }
        }

        private void _Write(BinaryWriter writer)
        {
            for (int i = 0; i < kStorage; i++)
            {
                writer.Write(mSamples[i].Entropy);
                writer.Write(mSamples[i].MaxIntensity);
                writer.Write(mSamples[i].Roll);
                writer.Write(mSamples[i].Yaw);
            }
        }

        private void _GetNextSettings(ref ThreePointSettings arSettings)
        {
            float roll = (mR * kRollFactor) + ((float)mRandom.NextDouble() * kRollJitterScale - kRollJitterScaleAdjust);
            float fill = (mF * kFillFactor) + ((float)mRandom.NextDouble() * kFillJitterScale - kFillJitterScaleAdjust);
            float yaw = (mY * kYawFactor) + ((float)mRandom.NextDouble() * kYawJitterScale - kYawJitterScaleAdjust);

            arSettings.KeyRoll = new Degree(roll);
            arSettings.Fill = Utilities.Max(fill, ThreePointSettings.kMinFill);
            arSettings.KeyYaw = Utilities.Clamp(new Degree(yaw), ThreePointSettings.kMinYaw, ThreePointSettings.kMaxYaw);
        }

        private float _GetError(
            ref ImageIlluminationMetrics arTarget, 
            ref ImageIlluminationMetrics arCurrent)
        {
            ImageIlluminationMetrics a = (arTarget - arCurrent);

            float ret = ImageIlluminationMetrics.Dot(ref a, ref a);

            return ret;
        }

        private float _GetErrorGradient(
            ref ImageIlluminationMetrics arTarget,
            ref ImageIlluminationMetrics ar0, 
            ref ImageIlluminationMetrics ar1)
        {
            float e0 = _GetError(ref arTarget, ref ar0);
            float e1 = _GetError(ref arTarget, ref ar1);
            
            float ret = 0.5f * (e1 - e0);

            return ret;
        }

        public float _GetErrorGradientRoll(
            ref ImageIlluminationMetrics arTarget,
            ref ThreePointSettings arCurrent)
        {
            float r = (arCurrent.KeyRoll.Value / kRollFactor);
            float f = (Utilities.Clamp(arCurrent.Fill, ThreePointSettings.kMinFill, ThreePointSettings.kMaxFill) / kFillFactor);
            float y = (Utilities.Clamp(arCurrent.KeyYaw, ThreePointSettings.kMinYaw, ThreePointSettings.kMaxYaw).Value / kYawFactor);

            int r0 = (int)Math.Floor(r);
            int f0 = (int)Math.Floor(f);
            int y0 = (int)Math.Floor(y);

            int r1 = (int)Math.Ceiling(r);
            int f1 = (int)Math.Ceiling(f);
            int y1 = (int)Math.Ceiling(y);

            if (r0 == r1) { r0--; r1++; }
            while (r0 < 0) { r0 += kSegments; }

            while (r0 >= kSegments) { r0 -= kSegments; }
            while (r1 >= kSegments) { r1 -= kSegments; }

            float fd = f - (float)f0;
            float yd = y - (float)y0;

            ImageIlluminationMetrics i0 = ImageIlluminationMetrics.Lerp(ref mSamples[_I(r0, f0, y0)], ref mSamples[_I(r0, f0, y1)], yd);
            ImageIlluminationMetrics i1 = ImageIlluminationMetrics.Lerp(ref mSamples[_I(r0, f1, y0)], ref mSamples[_I(r0, f1, y1)], yd);
            ImageIlluminationMetrics j0 = ImageIlluminationMetrics.Lerp(ref mSamples[_I(r1, f0, y0)], ref mSamples[_I(r1, f0, y1)], yd);
            ImageIlluminationMetrics j1 = ImageIlluminationMetrics.Lerp(ref mSamples[_I(r1, f1, y0)], ref mSamples[_I(r1, f1, y1)], yd);

            ImageIlluminationMetrics k0 = ImageIlluminationMetrics.Lerp(ref i0, ref i1, fd);
            ImageIlluminationMetrics k1 = ImageIlluminationMetrics.Lerp(ref j0, ref j1, fd);

            float ret = _GetErrorGradient(ref arTarget, ref k0, ref k1);

            return ret;
        }

        public float _GetErrorGradientFill(
            ref ImageIlluminationMetrics arTarget,
            ref ThreePointSettings arCurrent)
        {
            float r = (arCurrent.KeyRoll.Value / kRollFactor);
            float f = (Utilities.Clamp(arCurrent.Fill, ThreePointSettings.kMinFill, ThreePointSettings.kMaxFill) / kFillFactor);
            float y = (Utilities.Clamp(arCurrent.KeyYaw, ThreePointSettings.kMinYaw, ThreePointSettings.kMaxYaw).Value / kYawFactor);

            int r0 = (int)Math.Floor(r);
            int f0 = (int)Math.Floor(f);
            int y0 = (int)Math.Floor(y);

            int r1 = (int)Math.Ceiling(r);
            int f1 = (int)Math.Ceiling(f);
            int y1 = (int)Math.Ceiling(y);

            while (r0 >= kSegments) { r0 -= kSegments; }
            while (r1 >= kSegments) { r1 -= kSegments; }

            if (f0 == f1) { f0--; f1++; }
            if (f0 < 0) { f0 = 0; }
            if (f1 > kSegments) { f1 = kSegments; }

            float rd = r - (float)r0;
            float yd = y - (float)y0;

            ImageIlluminationMetrics i0 = ImageIlluminationMetrics.Lerp(ref mSamples[_I(r0, f0, y0)], ref mSamples[_I(r0, f0, y1)], yd);
            ImageIlluminationMetrics i1 = ImageIlluminationMetrics.Lerp(ref mSamples[_I(r1, f0, y0)], ref mSamples[_I(r1, f0, y1)], yd);
            ImageIlluminationMetrics j0 = ImageIlluminationMetrics.Lerp(ref mSamples[_I(r0, f1, y0)], ref mSamples[_I(r0, f1, y1)], yd);
            ImageIlluminationMetrics j1 = ImageIlluminationMetrics.Lerp(ref mSamples[_I(r1, f1, y0)], ref mSamples[_I(r1, f1, y1)], yd);

            ImageIlluminationMetrics k0 = ImageIlluminationMetrics.Lerp(ref i0, ref i1, rd);
            ImageIlluminationMetrics k1 = ImageIlluminationMetrics.Lerp(ref j0, ref j1, rd);

            float ret = _GetErrorGradient(ref arTarget, ref k0, ref k1);

            return ret;
        }

        public float _GetErrorGradientYaw(
            ref ImageIlluminationMetrics arTarget,
            ref ThreePointSettings arCurrent)
        {
            float r = (arCurrent.KeyRoll.Value / kRollFactor);
            float f = (Utilities.Clamp(arCurrent.Fill, ThreePointSettings.kMinFill, ThreePointSettings.kMaxFill) / kFillFactor);
            float y = (Utilities.Clamp(arCurrent.KeyYaw, ThreePointSettings.kMinYaw, ThreePointSettings.kMaxYaw).Value / kYawFactor);

            int r0 = (int)Math.Floor(r);
            int f0 = (int)Math.Floor(f);
            int y0 = (int)Math.Floor(y);

            int r1 = (int)Math.Ceiling(r);
            int f1 = (int)Math.Ceiling(f);
            int y1 = (int)Math.Ceiling(y);

            while (r0 >= kSegments) { r0 -= kSegments; }
            while (r1 >= kSegments) { r1 -= kSegments; }

            if (y0 == y1) { y0--; y1++; }
            if (y0 < 0) { y0 = 0; }
            if (y1 > kSegments) { y1 = kSegments; }

            float rd = r - (float)r0;
            float fd = f - (float)f0;

            ImageIlluminationMetrics i0 = ImageIlluminationMetrics.Lerp(ref mSamples[_I(r0, f0, y0)], ref mSamples[_I(r0, f1, y0)], fd);
            ImageIlluminationMetrics i1 = ImageIlluminationMetrics.Lerp(ref mSamples[_I(r1, f0, y0)], ref mSamples[_I(r1, f1, y0)], fd);
            ImageIlluminationMetrics j0 = ImageIlluminationMetrics.Lerp(ref mSamples[_I(r0, f0, y1)], ref mSamples[_I(r0, f1, y1)], fd);
            ImageIlluminationMetrics j1 = ImageIlluminationMetrics.Lerp(ref mSamples[_I(r1, f0, y1)], ref mSamples[_I(r1, f1, y1)], fd);

            ImageIlluminationMetrics k0 = ImageIlluminationMetrics.Lerp(ref i0, ref i1, rd);
            ImageIlluminationMetrics k1 = ImageIlluminationMetrics.Lerp(ref j0, ref j1, rd);

            float ret = _GetErrorGradient(ref arTarget, ref k0, ref k1);

            return ret;
        }

        private int _I(int aRoll, int aFill, int aYaw)
        {
            int ret = (aRoll * kSegmentsPlus1Square) + (aFill * kSegmentsPlus1) + aYaw;

            return ret;
        }

        private int _Index
        {
            get
            {
                return _I(mR, mF, mY);
            }
        }

        private bool _IncrementIndex()
        {
            mY++;
            if (mY >= kSegmentsPlus1) { mY = 0; mF++; }
            if (mF >= kSegmentsPlus1) { mF = 0; mR++; }
            if (mR >= kSegments) { return false; }
            else { return true; }
        }

        private void _Init()
        {
            mR = 0;
            mF = 0;
            mY = 0;

            mRandom = new Random();
            mSamples = new ImageIlluminationMetrics[kStorage];
        }
        #endregion

        public ImageIlluminationMetrics Get(ref ThreePointSettings arSettings)
        {
            float r = (arSettings.KeyRoll.Value / kRollFactor);
            float f = (Utilities.Clamp(arSettings.Fill, ThreePointSettings.kMinFill, ThreePointSettings.kMaxFill) / kFillFactor);
            float y = (Utilities.Clamp(arSettings.KeyYaw, ThreePointSettings.kMinYaw, ThreePointSettings.kMaxYaw).Value / kYawFactor);

            int r0 = (int)Math.Floor(r);
            int f0 = (int)Math.Floor(f);
            int y0 = (int)Math.Floor(y);

            int r1 = (int)Math.Ceiling(r);
            int f1 = (int)Math.Ceiling(f);
            int y1 = (int)Math.Ceiling(y);

            while (r0 >= kSegments)
            {
                r0 -= kSegments;
            }
            while (r1 >= kSegments)
            {
                r1 -= kSegments;
            }

            float rd = r - (float)r0;
            float fd = f - (float)f0;
            float yd = y - (float)y0;

            ImageIlluminationMetrics i0 = ImageIlluminationMetrics.Lerp(ref mSamples[_I(r0, f0, y0)], ref mSamples[_I(r0, f0, y1)], yd);
            ImageIlluminationMetrics i1 = ImageIlluminationMetrics.Lerp(ref mSamples[_I(r0, f1, y0)], ref mSamples[_I(r0, f1, y1)], yd);
            ImageIlluminationMetrics j0 = ImageIlluminationMetrics.Lerp(ref mSamples[_I(r1, f0, y0)], ref mSamples[_I(r1, f0, y1)], yd);
            ImageIlluminationMetrics j1 = ImageIlluminationMetrics.Lerp(ref mSamples[_I(r1, f1, y0)], ref mSamples[_I(r1, f1, y1)], yd);

            ImageIlluminationMetrics k0 = ImageIlluminationMetrics.Lerp(ref i0, ref i1, fd);
            ImageIlluminationMetrics k1 = ImageIlluminationMetrics.Lerp(ref j0, ref j1, fd);

            ImageIlluminationMetrics ret = ImageIlluminationMetrics.Lerp(ref k0, ref k1, rd);

            return ret;
        }

        public void Init(ref ThreePointSettings arSettings)
        {
            _Init();
            _GetNextSettings(ref arSettings);
        }


        public void Step(ref ImageIlluminationMetrics arTarget, ref ThreePointSettings arCurrent, ref ThreePointSettings arMotivation, float aTimeStep)
        {
            ThreePointSettings orig = arCurrent;
            ImageIlluminationMetrics targetMetrics = arTarget;
            ImageIlluminationMetrics curMetrics = Get(ref arCurrent);
            ImageIlluminationMetrics motMetrics = Get(ref arMotivation);

            float rollDelta = kStepSize * aTimeStep * ((kDesiredWeight * _GetErrorGradientRoll(ref targetMetrics, ref arCurrent)) + (kMotWeight * _GetErrorGradientRoll(ref motMetrics, ref arCurrent)));
            float fillDelta = kStepSize * aTimeStep * ((kDesiredWeight * _GetErrorGradientFill(ref targetMetrics, ref arCurrent)) + (kMotWeight * _GetErrorGradientFill(ref motMetrics, ref arCurrent)));
            float yawDelta = kStepSize * aTimeStep * ((kDesiredWeight * _GetErrorGradientYaw(ref targetMetrics, ref arCurrent)) + kMotWeight * (_GetErrorGradientYaw(ref motMetrics, ref arCurrent)));

            arCurrent.KeyRoll -= (rollDelta * Degree.k180);
            arCurrent.Fill = Utilities.Max(arCurrent.Fill - (fillDelta * ThreePointSettings.kMaxFill), ThreePointSettings.kMinFill);
            arCurrent.KeyYaw = Utilities.Clamp(arCurrent.KeyYaw - (ThreePointSettings.kMaxYaw * yawDelta), ThreePointSettings.kMinYaw, Degree.k180);
        }

        public void Save(string aFilename)
        {
            BinaryWriter writer = new BinaryWriter(new FileStream(aFilename, FileMode.Create));
            _Write(writer);
        }

        public void Load(string aFilename)
        {
            if (System.IO.File.Exists(aFilename))
            {
                BinaryReader reader = new BinaryReader(new FileStream(aFilename, FileMode.Open));
                _Read(reader);
            }
            else
            {
                throw new FileNotFoundException(aFilename);
            }
        }

        public const float kRollTolerance = 15.0f;
        public const float kYawMinimum = 30.0f;

        public bool Tick(ref LightExtractorImage aImage, ref ThreePointSettings arSettings)
        {
            int kIndex = _Index;

            ImageIlluminationMetrics sample;
            LightingExtractor.ExtractLighting(ref aImage, out sample);

            // Cleanup handles cases where:
            // - the yaw becomes indeterminant because it is too big and the key is behind the object.
            // - the roll becomes indeterminant because the yaw is too small and the light is effectively
            //   camera mounted.
            // - the fill becomes indeterminant because the yaw is too small.
#if ENABLE_CLEANUP
            sample.Roll = (arSettings.KeyRoll / Degree.k360).Value * ImageIlluminationMetrics.kRollMax;

            if (arSettings.KeyYaw.Value - (2.0f * kYawFactor) > ThreePointSettings.kMinYaw.Value)
            {
                ThreePointSettings set0 = new ThreePointSettings(arSettings.KeyRoll, arSettings.Fill, arSettings.KeyYaw - new Degree(2.0f * kYawFactor));
                ThreePointSettings set1 = new ThreePointSettings(arSettings.KeyRoll, arSettings.Fill, arSettings.KeyYaw - new Degree(1.0f * kYawFactor));

                ImageIlluminationMetrics s0 = Get(ref set0);
                ImageIlluminationMetrics s1 = Get(ref set1);

                if (sample.Yaw < s1.Yaw)
                {
                    sample.Yaw = (s0.Yaw + (2.0f * (s1.Yaw - s0.Yaw)));
                }
            }

            if (arSettings.Fill - (2.0f * kFillFactor) > ThreePointSettings.kMinFill)
            {
                ThreePointSettings set0 = new ThreePointSettings(arSettings.KeyRoll, arSettings.Fill - (2.0f * kFillFactor), arSettings.KeyYaw);
                ThreePointSettings set1 = new ThreePointSettings(arSettings.KeyRoll, arSettings.Fill - (1.0f * kFillFactor), arSettings.KeyYaw);

                ImageIlluminationMetrics s0 = Get(ref set0);
                ImageIlluminationMetrics s1 = Get(ref set1);

                if (sample.Entropy < s1.Entropy)
                {
                    sample.Entropy = (s0.Entropy + (2.0f * (s1.Entropy - s0.Entropy))); 
                }
            }
#endif

            mSamples[kIndex] = sample;

            if (_IncrementIndex())
            {
                _GetNextSettings(ref arSettings);
                return true;
            }
            else { return false; }
        }
    }

}
