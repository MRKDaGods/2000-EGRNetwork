using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using static MRK.EGRLogger;

namespace MRK {
    public class EGRNetworkConfig {
        readonly Dictionary<string, string> m_Config;
        string m_ConfigPath;

        static readonly string ms_DefaultConfig = "# 2000 EGR Network Config - MRKDaGods(Mohamed Ammar)\n" +
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
            m_Config = new Dictionary<string, string>();
        }

        public bool Load() {
            LogInfo("Loading config...");

            m_ConfigPath = $@"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\2000-EGRNetworkConfig.2000";
            LogInfo($"Config path={m_ConfigPath}");

            bool result = false;
            if (File.Exists(m_ConfigPath)) {
                LogInfo("Reading...");

                foreach (string line in File.ReadAllLines(m_ConfigPath)) {
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
                    m_Config[key] = val;
                }

                result = true;
            }

            LogInfo($"Config loaded num_entries={m_Config.Count}");
            return result;
        }

        public void ReloadConfig() {
            m_Config.Clear();
            Load();
        }

        public void Save() {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("# 2000 EGR Network Config - MRKDaGods(Mohamed Ammar)");

            foreach (KeyValuePair<string, string> pair in m_Config) {
                builder.AppendLine($"{pair.Key}={pair.Value}");
            }

            LogInfo("Writing config file");
            File.WriteAllText(m_ConfigPath, builder.ToString());
        }

        public void WriteDefaultConfig() {
            File.WriteAllText(m_ConfigPath, ms_DefaultConfig);
        }

        public string this[string key] {
            get {
                string val;
                m_Config.TryGetValue(key, out val);
                return val;
            }

            set {
                m_Config[key] = value;
            }
        }
    }
}
