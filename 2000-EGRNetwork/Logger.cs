using static System.Console;

namespace MRK
{
    public enum LogType
    {
        Info,
        Warning,
        Error
    }

    public class Logger
    {
        public static int IndentLevel { get; set; }

        public static void Log(LogType type, string msg)
        {
            string prefix = "[";

            switch (type)
            {

                case LogType.Info:
                    prefix += "INFO";
                    break;

                case LogType.Error:
                    prefix += "ERROR";
                    break;

                case LogType.Warning:
                    prefix += "WARN";
                    break;
            }

            prefix += "]";

            WriteLine($"[{EGR.RelativeTime:hh\\:mm\\:ss\\.fff}] {prefix}{new string('\t', IndentLevel)} {msg}");
        }

        public static void LogInfo(string msg)
        {
            Log(LogType.Info, msg);
        }

        public static void LogWarning(string msg)
        {
            Log(LogType.Warning, msg);
        }

        public static void LogError(string msg)
        {
            Log(LogType.Error, msg);
        }
    }
}
