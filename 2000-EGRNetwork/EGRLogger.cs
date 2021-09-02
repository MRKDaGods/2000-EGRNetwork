using System;
using static System.Console;

namespace MRK {
    public enum EGRLogType {
        Info,
        Warning,
        Error
    }

    public class EGRLogger {
        public static void Log(EGRLogType type, string msg) {
            string prefix = "[";

            switch (type) {

                case EGRLogType.Info:
                    prefix += "INFO";
                    break;

                case EGRLogType.Error:
                    prefix += "ERROR";
                    break;

                case EGRLogType.Warning:
                    prefix += "WARN";
                    break;
            }

            prefix += "]";

            WriteLine($"[{EGRMain.Instance.RelativeTime:hh\\:mm\\:ss\\.fff}] {prefix} {msg}");
        }

        public static void LogInfo(string msg) {
            Log(EGRLogType.Info, msg);
        }

        public static void LogWarning(string msg) {
            Log(EGRLogType.Warning, msg);
        }

        public static void LogError(string msg) {
            Log(EGRLogType.Error, msg);
        }
    }
}
