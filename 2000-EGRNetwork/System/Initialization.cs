using System;
using System.Runtime.CompilerServices;

namespace MRK.System
{
    public class Initialization
    {
        public static void RunStaticConstructor(Type type)
        {
            RuntimeHelpers.RunClassConstructor(type.TypeHandle);
        }

        public static void Initialize()
        {
            RunStaticConstructor(typeof(Time));
        }
    }
}
