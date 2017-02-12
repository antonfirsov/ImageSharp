using System;
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
        /// Lol
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

        struct Bum
        {
            private UIntFloatUnion r;
            
            private UIntFloatUnion g;
            
            private UIntFloatUnion b;
            
            private UIntFloatUnion a;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vector4 ToVector4() => new Vector4(this.r.f, this.g.f, this.b.f, this.a.f);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Load(Color bytepixel)
            {
                this.r.i = Magic.UInt | bytepixel.R;
                this.g.i = Magic.UInt | bytepixel.G;
                this.b.i = Magic.UInt | bytepixel.B;
                this.a.i = Magic.UInt | bytepixel.A;
            }
        }

        internal static readonly ArrayPool<uint> UIntPool = ArrayPool<uint>.Create(1024 * 128, 50);
        internal static readonly ArrayPool<float> FloatPool = ArrayPool<float>.Create(1024 * 128, 50);

        private static readonly uint[] UnpackVectorData =
                    {
                        1, 256, 256 * 256, 256 * 256 * 256,
                        1, 1, 1, 1,
                        1, 1, 1, 1, 1, 1, 1, 1,
                    };

        
        /// <summary>
        /// Lol
        /// </summary>
        /// <param name="input"></param>
        /// <param name="result"></param>
        internal static void UnpackUints(Color[] input, uint[] result)
        {
            if (result.Length < input.Length * 4)
            {
                throw new ArgumentException();
            }

            Vector<uint> unpack = new Vector<uint>(UnpackVectorData);
            Vector<uint> magic = new Vector<uint>(Magic.UInt);
            Vector<uint> mask = new Vector<uint>(255);

            for (int i = 0; i < input.Length; i++)
            {
                int i4 = i * 4;
                Vector<uint> v = new Vector<uint>(input[i].PackedValue);
                v /= unpack;
                v &= mask;
                v |= magic;

                v.CopyTo(result, i4);
                //Color c = input[i];
                //result[i4 + 0] = Magic.UInt | c.R;
                //result[i4 + 1] = Magic.UInt | c.G;
                //result[i4 + 2] = Magic.UInt | c.B;
                //result[i4 + 3] = Magic.UInt | c.A;
            }

            //UIntPool.Return(temp);
        }

        private struct RGBAUint
        {
            public uint R;
            public uint G;
            public uint B;
            public uint A;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Load(Color color)
            {
                uint p = color.PackedValue;
                this.R = p;
                this.G = p >> Color.GreenShift;
                this.B = p >> Color.BlueShift;
                this.A = p >> Color.AlphaShift;
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
            
            uint[] temp = UIntPool.Rent(rawInputSize + Vector<uint>.Count);
            float[] fTemp = Unsafe.As<float[]>(temp);

            //RGBAUint helper = default(RGBAUint);

            fixed (uint* tPtr = temp)
            fixed (Color* cPtr = input)
            {
                uint* src = (uint*)cPtr;
                uint* srcEnd = src + input.Length;
                uint* dst = tPtr;
                //RGBAUint* rgbaPtr = (RGBAUint*)tPtr;

                for (; src < srcEnd; src++)
                {
                    uint p = *src;
                    *dst++ = p;
                    *dst++ = p >> Color.GreenShift;
                    *dst++ = p >> Color.BlueShift;
                    *dst++ = p >> Color.AlphaShift;
                    //helper.Load(input[i]);
                    //*rgbaPtr = helper;
                    //rgbaPtr++;
                }

                for (int i = 0; i < rawInputSize; i += Vector<uint>.Count)
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
                    uint byteCount = (uint) rawInputSize * sizeof(float);

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
            //FloatPool.Return(fTemp);
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

                    Color* cPtr = (Color*) cFixed;
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

        /// <summary>
        /// Lolz
        /// </summary>
        /// <param name="input"></param>
        /// <param name="result"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void ColorToVector4BasicBatched2(Color[] input, Vector4[] result)
        {
            fixed (Color* cFixed = input)
            {
                fixed (Vector4* rFixed = result)
                {
                    Vector4 v;
                    Vector4 maxBytes = new Vector4(255);

                    uint* cPtr = (uint*)cFixed;
                    uint* cEnd = cPtr + input.Length;
                    Vector4* rPtr = rFixed;
                    for (; cPtr < cEnd; cPtr++, rPtr++)
                    {
                        uint c = *cPtr;
                        uint r = c & 255u;
                        uint g = (c >> Color.GreenShift) & 255u;
                        uint b = (c >> Color.BlueShift) & 255u;
                        uint a = (c >> Color.AlphaShift) & 255u;
                        v = new Vector4((float)r, (float)g, (float)b, (float)a);
                        v /= maxBytes;
                        *rPtr = v;
                    }
                }
            }
        }
    }
}
