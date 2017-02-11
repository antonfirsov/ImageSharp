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

        internal static Vector<uint> GetUnpackVector()
        {
            uint[] a = UIntPool.Rent(Vector<uint>.Count);
            a[0] = 0;
            a[1] = 256;
            a[2] = 256 * 256;
            a[3] = 256 * 256 * 256;

            Vector<uint> result = new Vector<uint>(a);
            UIntPool.Return(a);
            return result;
        }

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

            //uint[] temp = UIntPool.Rent(result.Length + Vector<uint>.Count);
            Vector<uint> unpack = GetUnpackVector();
            //Buffer.BlockCopy(input, 0, temp, 0, input.Length);
            Vector<uint> magic = new Vector<uint>(Magic.UInt);

            for (int i = 0; i < input.Length; i++)
            {
                int i4 = i * 4;
                Vector<uint> v = new Vector<uint>(input[i].PackedValue);
                v /= unpack;
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

        /// <summary>
        /// Lolz
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ColorToVector4BithackBatchedArrays(Color[] input, Vector4[] result)
        {
            Vector4 bVec = new Vector4(Magic.B);
            Vector4 magicVec = new Vector4(Magic.Float);

            Bum bum = default(Bum);
            
            for (int i = 0; i < input.Length; i++)
            {
                bum.Load(input[i]);
                Vector4 v = bum.ToVector4();
                result[i] = (v - magicVec) * bVec;
            }
            
        }
    }
}
