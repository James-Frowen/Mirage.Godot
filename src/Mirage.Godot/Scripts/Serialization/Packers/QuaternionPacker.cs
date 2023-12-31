/*
MIT License

Copyright (c) 2021 James Frowen

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;
using System.Runtime.CompilerServices;
using Godot;

namespace Mirage.Serialization
{
    public sealed class QuaternionPacker
    {
        /// <summary>Default packer using 9 bits per element, 29 bits total</summary>
        public static readonly QuaternionPacker Default9 = new QuaternionPacker(9);
        /// <summary>Default packer using 10 bits per element, 32 bits total</summary>
        public static readonly QuaternionPacker Default10 = new QuaternionPacker(10);

        public static uint PackAsInt(Quaternion value)
        {
            return (uint)Default10.Pack(value);
        }
        public static Quaternion UnpackFromInt(uint value)
        {
            return Default10.Unpack(value);
        }

        /// <summary>
        /// 1 / sqrt(2)
        /// </summary>
        private const float MAX_VALUE = 1f / 1.414214f;

        /// <summary>
        /// bit count per element writen
        /// </summary>
        private readonly int _bitCountPerElement;

        /// <summary>
        /// total bit count for Quaternion
        /// <para>
        /// count = 3 * perElement + 2;
        /// </para>
        /// </summary>
        private readonly int _totalBitCount;
        private readonly uint _readMask;
        private readonly FloatPacker _floatPacker;

        /// <param name="quaternionBitLength">10 per "smallest 3" is good enough for most people</param>
        public QuaternionPacker(int quaternionBitLength = 10)
        {
            // (this.BitLength - 1) because pack sign by itself
            _bitCountPerElement = quaternionBitLength;
            _totalBitCount = 2 + (quaternionBitLength * 3);
            _floatPacker = new FloatPacker(MAX_VALUE, quaternionBitLength);
            _readMask = (uint)BitMask.Mask(_bitCountPerElement);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Pack(NetworkWriter writer, Quaternion value)
        {
            writer.Write(Pack(value), _totalBitCount);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong Pack(Quaternion value)
        {
            QuickNormalize(ref value);

            FindLargestIndex(ref value, out var index);

            GetSmallerDimensions(index, ref value, out var a, out var b, out var c);

            // largest needs to be positive to be calculated by reader 
            // if largest is negative flip sign of others because Q = -Q
            if (value[(int)index] < 0)
            {
                a = -a;
                b = -b;
                c = -c;
            }

            // todo, should we be rounding down for abc? because if they are rounded up their sum may be greater than largest

            ulong combine = 0;
            // write Index as (3-i) so that Quaternion.identity will be all zeros
            combine |= (ulong)(3 - index) << (_bitCountPerElement * 3);
            combine |= (ulong)_floatPacker.PackNoClamp(a) << (_bitCountPerElement * 2);
            combine |= (ulong)_floatPacker.PackNoClamp(b) << _bitCountPerElement;
            combine |= _floatPacker.PackNoClamp(c);
            return combine;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void QuickNormalize(ref Quaternion quaternion)
        {
            var dot =
                (quaternion.X * quaternion.X) +
                (quaternion.Y * quaternion.Y) +
                (quaternion.Z * quaternion.Z) +
                (quaternion.W * quaternion.W);

            const float allowedEpsilon = 1E-5f;
            const float minAllowed = 1 - allowedEpsilon;
            const float maxAllowed = 1 + allowedEpsilon;
            // only normalize if dot product is outside allowed range
            if (minAllowed > dot || maxAllowed < dot)
            {
                var dotSqrt = (float)Math.Sqrt(dot);
                // rotation is 0
                if (dotSqrt < allowedEpsilon)
                {
                    // identity
                    quaternion.X = 0;
                    quaternion.Y = 0;
                    quaternion.Z = 0;
                    quaternion.W = 1;
                }
                else
                {
                    var iDotSqrt = 1 / dotSqrt;
                    quaternion.X *= iDotSqrt;
                    quaternion.Y *= iDotSqrt;
                    quaternion.Z *= iDotSqrt;
                    quaternion.W *= iDotSqrt;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void FindLargestIndex(ref Quaternion quaternion, out uint index)
        {
            var x2 = quaternion.X * quaternion.X;
            var y2 = quaternion.Y * quaternion.Y;
            var z2 = quaternion.Z * quaternion.Z;
            var w2 = quaternion.W * quaternion.W;

            index = 0;
            var current = x2;
            // check vs sq to avoid doing mathf.abs
            if (y2 > current)
            {
                index = 1;
                current = y2;
            }
            if (z2 > current)
            {
                index = 2;
                current = z2;
            }
            if (w2 > current)
            {
                index = 3;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void GetSmallerDimensions(uint largestIndex, ref Quaternion quaternion, out float a, out float b, out float c)
        {
            switch (largestIndex)
            {
                case 0:
                    a = quaternion.Y;
                    b = quaternion.Z;
                    c = quaternion.W;
                    return;
                case 1:
                    a = quaternion.X;
                    b = quaternion.Z;
                    c = quaternion.W;
                    return;
                case 2:
                    a = quaternion.X;
                    b = quaternion.Y;
                    c = quaternion.W;
                    return;
                case 3:
                    a = quaternion.X;
                    b = quaternion.Y;
                    c = quaternion.Z;
                    return;
                default:
                    ThrowIfOutOfRange();
                    a = b = c = default;
                    return;
            }
        }

        private static void ThrowIfOutOfRange() => throw new IndexOutOfRangeException("Invalid Quaternion index!");


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Quaternion Unpack(NetworkReader reader)
        {
            return Unpack(reader.Read(_totalBitCount));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Quaternion Unpack(ulong combine)
        {
            // Index writen as (3-i)
            // so (3-c) to decode
            var index = 3 - (uint)(combine >> (_bitCountPerElement * 3));

            var a = _floatPacker.Unpack((uint)(combine >> (_bitCountPerElement * 2)) & _readMask);
            var b = _floatPacker.Unpack((uint)(combine >> (_bitCountPerElement * 1)) & _readMask);
            var c = _floatPacker.Unpack((uint)combine & _readMask);

            var l2 = 1 - ((a * a) + (b * b) + (c * c));
            var largest = (float)Math.Sqrt(l2);
            // this Quaternion should already be normallized because of the way that largest is calculated
            switch (index)
            {
                case 0:
                    return new Quaternion(largest, a, b, c);
                case 1:
                    return new Quaternion(a, largest, b, c);
                case 2:
                    return new Quaternion(a, b, largest, c);
                case 3:
                    return new Quaternion(a, b, c, largest);
                default:
                    ThrowIfOutOfRange();
                    return default;
            }
        }
    }
}
