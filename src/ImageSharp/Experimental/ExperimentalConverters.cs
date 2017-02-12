﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ImageSharp.Experimental
{
    using System.Buffers;
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Lol
    /// </summary>
    public class ExperimentalConverters
    {
        /// <summary>
        /// Lol
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 ColorToVector4(ref Color color)
        {
            return color.ToVector4();
        }

        private static class Magic
        {
            public const float Float = 32768.0f;

            public const uint UInt = 1191182336; // reinterpreted value of 32768.0f

            public const float B = 256.0f / 255.0f;
        }

        /// <summary>
        /// http://stackoverflow.com/a/5362789
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 ColorToVector4Bithack(Color bytepixel)
        {
            UIntFloatUnion r, g, b, a;
            r = g = b = a = default(UIntFloatUnion);
            
            Vector4 floatpixel = default(Vector4);

            r.i = Magic.UInt | bytepixel.R;
            g.i = Magic.UInt | bytepixel.G;
            b.i = Magic.UInt | bytepixel.B;
            a.i = Magic.UInt | bytepixel.A;

            floatpixel.X = (r.f - Magic.Float) * Magic.B;
            floatpixel.Y = (g.f - Magic.Float) * Magic.B;
            floatpixel.Z = (b.f - Magic.Float) * Magic.B;
            floatpixel.W = (a.f - Magic.Float) * Magic.B;
            
            return floatpixel;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct UIntFloatUnion
        {
            [FieldOffset(0)]
            public float f;

            [FieldOffset(0)]
            public uint i;
        }

        internal static readonly ArrayPool<uint> UIntPool = ArrayPool<uint>.Create(1024 * 128, 50);
        internal static readonly ArrayPool<float> FloatPool = ArrayPool<float>.Create(1024 * 128, 50);
        
        private struct RGBAUint
        {
            private uint r;
            private uint g;
            private uint b;
            private uint a;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Load(uint p)
            {
                this.r = p;
                this.g = p >> Color.GreenShift;
                this.b = p >> Color.BlueShift;
                this.a = p >> Color.AlphaShift;
            }
        }


        /// <summary>
        /// Lolz
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void ColorToVector4BithackBatchedArrays(Color[] input, Vector4[] result)
        {
            Vector<float> bVec = new Vector<float>(Magic.B);
            Vector<uint> magicInt = new Vector<uint>(Magic.UInt);
            Vector<float> magicFloat = new Vector<float>(Magic.Float);
            Vector<uint> mask = new Vector<uint>(255);

            int rawInputSize = input.Length * 4;
            int vecSize = Vector<uint>.Count;

            uint[] temp = UIntPool.Rent(rawInputSize + Vector<uint>.Count);
            float[] fTemp = Unsafe.As<float[]>(temp);


            fixed (uint* tPtr = temp)
            fixed (Color* cPtr = input)
            {
                uint* src = (uint*)cPtr;
                uint* srcEnd = src + input.Length;
                RGBAUint* dst = (RGBAUint*)tPtr;

                for (; src < srcEnd; src++, dst++)
                {
                    dst->Load(*src);
                }

                for (int i = 0; i < rawInputSize; i += vecSize)
                {
                    Vector<uint> vi = new Vector<uint>(temp, i);

                    vi &= mask;
                    vi |= magicInt;

                    Vector<float> vf = Vector.AsVectorSingle(vi);
                    vf = (vf - magicFloat) * bVec;
                    vf.CopyTo(fTemp, i);
                }



                fixed (Vector4* p = result)
                {
                    uint byteCount = (uint)rawInputSize * sizeof(float);

                    if (byteCount > 1024u)
                    {
                        Marshal.Copy(fTemp, 0, (IntPtr)p, rawInputSize);
                    }
                    else
                    {
                        Unsafe.CopyBlock(p, tPtr, (uint)byteCount);
                    }
                }
            }

            UIntPool.Return(temp);
        }

        
        /// <summary>
        /// Lol
        /// </summary>
        /// <param name="input"></param>
        /// <param name="result"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void ColorToVector4BasicBatched(Color[] input, Vector4[] result)
        {
            fixed (Color* cFixed = input)
            {
                fixed (Vector4* rFixed = result)
                {
                    Vector4 v;
                    Vector4 maxBytes = new Vector4(255);

                    Color* cPtr = cFixed;
                    Color* cEnd = cPtr + input.Length;
                    Vector4* rPtr = rFixed;
                    for (; cPtr < cEnd; cPtr++, rPtr++)
                    {
                        v = new Vector4(cPtr->R, cPtr->G, cPtr->B, cPtr->A);
                        v /= maxBytes;
                        *rPtr = v;
                    }
                }
            }
        }
    }
}
