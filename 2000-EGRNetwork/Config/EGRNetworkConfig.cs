using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using static MRK.Logger;

namespace MRK {
    public class EGRNetworkConfig {
        readonly Dictionary<string, string> _config;
        string _configPath;

        private static readonly string _defaultConfig = "# 2000 EGR Network Config - MRKDaGods(Mohamed Ammar)\n" +
            "\n" +
            "NET_PORT=23466\n" +
            "NET_KEY=ntxsdI8cp4JEVcosVwz1\n" +
            "NET_THREAD_INTERVAL=100\n" +
            "\n" +
            "NET_WORKING_DIR=<set>\n" +
            "NET_PLACES_SRC_DIR=<set>\n" +
            "\n" +
            "NET_WTE_DB_PATH=<set>\n";

        public EGRNetworkConfig() {
            _config = new Dictionary<string, string>();
        }

        public bool Load() {
            LogInfo("Loading config...");

            _configPath = $@"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\2000-EGRNetworkConfig.2000";
            LogInfo($"Config path={_configPath}");

            bool result = false;
            if (File.Exists(_configPath)) {
                LogInfo("Reading...");

                foreach (string line in File.ReadAllLines(_configPath)) {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    string realLine = line.Trim();
                    if (realLine.StartsWith('#'))
                        continue;

                    int eqIdx = realLine.IndexOf('=');
                    if (eqIdx == -1)
                        continue;

                    if (eqIdx == 0) //line starts with =?
                        continue;

                    string key = realLine.Substring(0, eqIdx);
                    string val = realLine.Substring(eqIdx + 1);
                    _config[key] = val;
                }

                result = true;
            }

            LogInfo($"Config loaded num_entries={_config.Count}");
            return result;
        }

        public void ReloadConfig() {
            _config.Clear();
            Load();
        }

        public void Save() {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("# 2000 EGR Network Config - MRKDaGods(Mohamed Ammar)");

            foreach (KeyValuePair<string, string> pair in _config) {
                builder.AppendLine($"{pair.Key}={pair.Value}");
            }

            LogInfo("Writing config file");
            File.WriteAllText(_configPath, builder.ToString());
        }

        public void WriteDefaultConfig() {
            File.WriteAllText(_configPath, _defaultConfig);
        }

        public EGRNetworkConfigRecord this[string key] {
            get {
                string val;
                _config.TryGetValue(key, out val);
                return new EGRNetworkConfigRecord(val);
            }

            set {
                _config[key] = value.String;
            }
        }
    }
}
