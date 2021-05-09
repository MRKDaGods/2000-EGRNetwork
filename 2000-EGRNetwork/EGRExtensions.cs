using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MRK {
    public static class EGRExtensions {
        public static ulong NextULong(this Random rng) {
            byte[] buf = new byte[8];
            rng.NextBytes(buf);
            return BitConverter.ToUInt64(buf, 0);
        }

        //returns a uniformly random ulong between ulong.Min and Max without modulo bias
        public static ulong NextULong(this Random rng, ulong max, bool inclusiveUpperBound = false) {
            return rng.NextULong(ulong.MinValue, max, inclusiveUpperBound);
        }

        //returns a uniformly random ulong between Min and Max without modulo bias
        public static ulong NextULong(this Random rng, ulong min, ulong max, bool inclusiveUpperBound = false) {
            ulong range = max - min;

            if (inclusiveUpperBound) {
                if (range == ulong.MaxValue) {
                    return rng.NextULong();
                }

                range++;
            }

            if (range <= 0) {
                throw new ArgumentOutOfRangeException("Max must be greater than min when inclusiveUpperBound is false, and greater than or equal to when true", "max");
            }

            ulong limit = ulong.MaxValue - ulong.MaxValue % range;
            ulong r;
            do {
                r = rng.NextULong();
            } while (r > limit);

            return r % range + min;
        }

        public static bool EOF(this BinaryReader binaryReader) {
            return binaryReader.BaseStream.Position == binaryReader.BaseStream.Length;
        }
    }
}
