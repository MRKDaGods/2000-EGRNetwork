using System;
using System.Text;
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
        private static readonly object _indentLock;
        [ThreadStatic]
        private static bool _isPreserved;
        [ThreadStatic]
        private static StringBuilder _preservedStream;

        public static int IndentLevel { get; set; }

        static Logger()
        {
            _indentLock = new object();
        }

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

            string output = $"[{EGR.RelativeTime:hh\\:mm\\:ss\\.fff}] {prefix}{new string('\t', IndentLevel)} {msg}";
            if (_isPreserved)
            {
                _preservedStream.AppendLine(output);
            }
            else
            {
                WriteLine(output);
            }
        }

        public static void LogInfoIndented(string msg, int indentOffset)
        {
            lock (_indentLock)
            {
                IndentLevel += indentOffset;
                LogInfo(msg);
                IndentLevel -= indentOffset;
            }
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

        public static void PreserveStream()
        {
            _isPreserved = true;

            if (_preservedStream == null)
            {
                _preservedStream = new StringBuilder();
            }

            _preservedStream.Clear();
        }

        public static void FlushPreservedStream()
        {
            if (!_isPreserved)
            {
                return;
            }

            Write(_preservedStream.ToString());

            _isPreserved = false;
            _preservedStream.Clear();
        }
    }
}
