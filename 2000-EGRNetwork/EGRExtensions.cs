using System;
using System.Collections.Generic;
using System.IO;

namespace MRK
{
    public static class EGRExtensions
    {
        public static ulong NextULong(this Random rng)
        {
            byte[] buf = new byte[8];
            rng.NextBytes(buf);
            return BitConverter.ToUInt64(buf, 0);
        }

        //returns a uniformly random ulong between ulong.Min and Max without modulo bias
        public static ulong NextULong(this Random rng, ulong max, bool inclusiveUpperBound = false)
        {
            return rng.NextULong(ulong.MinValue, max, inclusiveUpperBound);
        }

        //returns a uniformly random ulong between Min and Max without modulo bias
        public static ulong NextULong(this Random rng, ulong min, ulong max, bool inclusiveUpperBound = false)
        {
            ulong range = max - min;

            if (inclusiveUpperBound)
            {
                if (range == ulong.MaxValue)
                {
                    return rng.NextULong();
                }

                range++;
            }

            if (range <= 0)
            {
                throw new ArgumentOutOfRangeException("Max must be greater than min when inclusiveUpperBound is false, and greater than or equal to when true", "max");
            }

            ulong limit = ulong.MaxValue - ulong.MaxValue % range;
            ulong r;
            do
            {
                r = rng.NextULong();
            } while (r > limit);

            return r % range + min;
        }

        public static bool EOF(this BinaryReader binaryReader)
        {
            return binaryReader.BaseStream.Position == binaryReader.BaseStream.Length;
        }

        public static string StringifyArray<T>(this T[] arr, char sep = ',')
        {
            if (arr.Length == 0)
                return string.Empty;

            string str = "";
            foreach (T t in arr)
                str += t.ToString() + sep;

            return str[0..^1]; //remove trailing sep
        }

        public static string StringifyList<T>(this List<T> list, char sep = ',')
        {
            if (list.Count == 0)
                return string.Empty;

            string str = "";
            foreach (T t in list)
                str += t.ToString() + sep;

            return str[0..^1]; //remove trailing sep
        }
    }
}
