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

using System;
using System.Collections.Generic;
using System.Text;

// From Nocturnal\Checksum\Hash64.h of:
////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2008 Insomniac Games
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the Insomniac Open License
// as published by Insomniac Games.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even an implied warranty. See the
// Insomniac Open License for more details.
//
// You should have received a copy of the Insomniac Open License
// along with this code; if not, write to the Insomniac Games
// 2255 N. Ontario St Suite 550, Burbank, CA 91504, or email
// nocturnal@insomniacgames.com, or visit
// http://nocturnal.insomniacgames.com.
////////////////////////////////////////////////////////////////////////////

namespace siat
{
    public static class Hash
    {
        /*
        --------------------------------------------------------------------
        lookup8.c, by Bob Jenkins, January 4 1997, Public Domain.
        hash(), hash2(), hash3, and mix() are externally useful functions.
        Routines to test the hash are included if SELF_TEST is defined.
        You can use this free for any purpose.  It has no warranty.
        --------------------------------------------------------------------
        */
        private static void mix64(ref UInt64 a, ref UInt64 b, ref UInt64 c)
        {
            a -= b; a -= c; a ^= (c >> 43);
            b -= c; b -= a; b ^= (a << 9);
            c -= a; c -= b; c ^= (b >> 8);
            a -= b; a -= c; a ^= (c >> 38);
            b -= c; b -= a; b ^= (a << 23);
            c -= a; c -= b; c ^= (b >> 5);
            a -= b; a -= c; a ^= (c >> 35);
            b -= c; b -= a; b ^= (a << 49);
            c -= a; c -= b; c ^= (b >> 11);
            a -= b; a -= c; a ^= (c >> 12);
            b -= c; b -= a; b ^= (a << 18);
            c -= a; c -= b; c ^= (b >> 22);
        }

        public static UInt64 Calculate64(byte[] aIn, UInt64 level)
        {
            unsafe
            {
                fixed (byte* p = aIn)
                {
                    UInt64 length = (UInt64)aIn.Length;
                    UInt64 a, b, c, len;
                    byte* k = p;

                    /* Set up the internal state */
                    len = length;
                    a = b = level;                         /* the previous hash value */
                    c = 0x9e3779b97f4a7c13L; /* the golden ratio; an arbitrary value */

                    /*---------------------------------------- handle most of the key */
                    if ((((uint)k) & 7) != 0)
                    {
                        while (len >= 24)
                        {
                            a += (k[0] + ((UInt64)k[1] << 8) + ((UInt64)k[2] << 16) + ((UInt64)k[3] << 24)
                            + ((UInt64)k[4] << 32) + ((UInt64)k[5] << 40) + ((UInt64)k[6] << 48) + ((UInt64)k[7] << 56));
                            b += (k[8] + ((UInt64)k[9] << 8) + ((UInt64)k[10] << 16) + ((UInt64)k[11] << 24)
                            + ((UInt64)k[12] << 32) + ((UInt64)k[13] << 40) + ((UInt64)k[14] << 48) + ((UInt64)k[15] << 56));
                            c += (k[16] + ((UInt64)k[17] << 8) + ((UInt64)k[18] << 16) + ((UInt64)k[19] << 24)
                            + ((UInt64)k[20] << 32) + ((UInt64)k[21] << 40) + ((UInt64)k[22] << 48) + ((UInt64)k[23] << 56));
                            mix64(ref a, ref b, ref c);
                            k += 24; len -= 24;
                        }
                    }
                    else
                    {
                        while (len >= 24)    /* aligned */
                        {
                            a += *(UInt64*)(k + 0);
                            b += *(UInt64*)(k + 8);
                            c += *(UInt64*)(k + 16);
                            mix64(ref a, ref b, ref c);
                            k += 24; len -= 24;
                        }
                    }

                    /*------------------------------------- handle the last 23 bytes */
                    c += length;
                    switch (len)              /* all the case statements fall through */
                    {
                        case 23: c += ((UInt64)k[22] << 56); goto case 22;
                        case 22: c += ((UInt64)k[21] << 48); goto case 21;
                        case 21: c += ((UInt64)k[20] << 40); goto case 20;
                        case 20: c += ((UInt64)k[19] << 32); goto case 19;
                        case 19: c += ((UInt64)k[18] << 24); goto case 18;
                        case 18: c += ((UInt64)k[17] << 16); goto case 17;
                        case 17: c += ((UInt64)k[16] << 8); goto case 16;
                        /* the first byte of c is reserved for the length */
                        case 16: b += ((UInt64)k[15] << 56); goto case 15;
                        case 15: b += ((UInt64)k[14] << 48); goto case 14;
                        case 14: b += ((UInt64)k[13] << 40); goto case 13;
                        case 13: b += ((UInt64)k[12] << 32); goto case 12;
                        case 12: b += ((UInt64)k[11] << 24); goto case 11;
                        case 11: b += ((UInt64)k[10] << 16); goto case 10;
                        case 10: b += ((UInt64)k[9] << 8); goto case 9;
                        case 9: b += ((UInt64)k[8]); goto case 8;
                        case 8: a += ((UInt64)k[7] << 56); goto case 7;
                        case 7: a += ((UInt64)k[6] << 48); goto case 6;
                        case 6: a += ((UInt64)k[5] << 40); goto case 5;
                        case 5: a += ((UInt64)k[4] << 32); goto case 4;
                        case 4: a += ((UInt64)k[3] << 24); goto case 3;
                        case 3: a += ((UInt64)k[2] << 16); goto case 2;
                        case 2: a += ((UInt64)k[1] << 8); goto case 1;
                        case 1: a += ((UInt64)k[0]); break;
                        /* case 0: nothing left to add */
                    }
                    mix64(ref a, ref b, ref c);

                    /*-------------------------------------------- report the result */
                    return c;
                }
            }
        }

        public static uint Calculate32(byte[] aIn, UInt64 level)
        {
            UInt64 hash = Calculate64(aIn, level);
            uint ret = (uint)((((UInt64)1 << 32) - 1) & hash);

            return ret;
        }

        public static uint Calculate32(string aIn, UInt64 level)
        {
            byte[] data = System.Text.Encoding.UTF8.GetBytes(aIn);

            return Calculate32(data, level);
        }
    }
}
