using MRK.IO;
using System.Collections.Generic;

namespace MRK
{
    public class CommandLine
    {
        private readonly string[] _commandLineArgs;
        private readonly ObjectStream<string> _commandLineStream;
        private Dictionary<string, CommandLineOption> _commandLineOptions;

        public CommandLine(string[] args)
        {
            _commandLineArgs = args;
            _commandLineStream = new ObjectStream<string>(_commandLineArgs);
            _commandLineOptions = new Dictionary<string, CommandLineOption>();
        }

        public bool ParseArguments()
        {
            _commandLineStream.Reset();
            _commandLineOptions.Clear();

            while (!_commandLineStream.IsEOS())
            {
                string current = _commandLineStream.Read();
                if (current.StartsWith("--"))
                {
                    //--option=true
                    int eqIdx = current.IndexOf('=');
                    if (eqIdx == -1)
                    {
                        return false;
                    }

                    string optionName = current.Substring(2, eqIdx - 2);
                    string optionValue = current[(eqIdx + 1)..];
                    _commandLineOptions[optionName] = new CommandLineOption
                    {
                        Name = optionName,
                        Value = optionValue
                    };
                }
                else if (current.StartsWith("-"))
                {
                    //-a
                    string optionName = current[1..];
                    _commandLineOptions[optionName] = new CommandLineOption
                    {
                        Name = optionName
                    };
                }
            }

            return true;
        }

        public bool GetCommandLineOption(string name, out CommandLineOption option)
        {
            return _commandLineOptions.TryGetValue(name, out option);
        }

        public void PrintCommandLineOptions()
        {
            Logger.LogInfo("CommandLine:");
            Logger.IndentLevel++;
            foreach (CommandLineOption commandLineOption in _commandLineOptions.Values)
            {
                Logger.LogInfo($"[{commandLineOption.Name}] {commandLineOption.Value}");
            }
            Logger.IndentLevel--;
        }
    }
}
