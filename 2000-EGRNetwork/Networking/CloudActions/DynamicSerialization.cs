using MRK.Networking.Internal.Utils;
using System;
using System.Collections.Generic;

namespace MRK.Networking.CloudActions
{
    public partial class DynamicSerialization
    {
        public const byte ReadString = 0x0;
        private const byte ReadMax = ReadString + 1;

        private static readonly Dictionary<byte, Func<NetDataReader, object>> _resolvers;

        static DynamicSerialization()
        {
            _resolvers = new();
            _resolvers[ReadString] = (reader) => reader.GetString();
        }

        public static bool Apply(NetDataReader data, byte[] dynamicSerialization, out object value)
        {
            value = null;
            if (data == null || dynamicSerialization == null || dynamicSerialization.Length == 0) return false;

            Context context = new Context(dynamicSerialization.Length > 1);

            foreach (byte b in dynamicSerialization)
            {
                if (b >= ReadMax)
                {
                    return false;
                }

                object resolvedValue = _resolvers[b](data);
                context.Value = resolvedValue;
            }

            value = context.Value;
            return true;
        }
    }
}
