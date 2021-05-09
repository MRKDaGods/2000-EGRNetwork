using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MRK.EGRLogger;

namespace MRK {
    public class EGRTileID {
        public int Z;
        public int X;
        public int Y;
        public byte[] Data;
    }

    public class EGRTileManager {
        const ulong MAX_CACHE_SIZE = 209715200UL;

        string m_RootPath;
        readonly Dictionary<string, Tuple<EGRFileSysIOTileID, List<EGRTileID>>> m_Tiles;
        ulong m_CacheSize;

        string m_TilesPath => $"{m_RootPath}\\Tiles";

        public EGRTileManager() {
            m_Tiles = new Dictionary<string, Tuple<EGRFileSysIOTileID, List<EGRTileID>>>();
        }

        public void Initialize(string root) {
            m_RootPath = root;
            if (!Directory.Exists(root))
                Directory.CreateDirectory(root);
        }

        public bool GetTile(string tileset, EGRTileID tileID) {
            lock (m_Tiles) {
                if (!m_Tiles.ContainsKey(tileset)) {
                    m_Tiles[tileset] = new Tuple<EGRFileSysIOTileID, List<EGRTileID>>(
                        new EGRFileSysIOTileID($"{m_TilesPath}\\{tileset}"), new List<EGRTileID>());

                    return false;
                }

                var tileStorage = m_Tiles[tileset];
                EGRTileID id = tileStorage.Item2.Find(tile => tile.Z == tileID.Z && tile.X == tileID.X && tile.Y == tileID.Y);
                if (id != null) {
                    tileID.Data = id.Data;
                    return true;
                }

                //try read from file
                id = tileStorage.Item1.Read($"{tileID.Z}\\{tileID.X}\\{tileID.Y}\\tile.png");
                if (id != null) {
                    tileID.Data = id.Data;

                    while (m_CacheSize > MAX_CACHE_SIZE) {
                        m_CacheSize -= (ulong)tileStorage.Item2[0].Data.Length;
                        tileStorage.Item2.RemoveAt(0);
                    }

                    tileStorage.Item2.Add(tileID);
                    m_CacheSize += (ulong)tileID.Data.Length;

                    return true;
                }
            }

            return false;
        }

        public void AddTile(string tileset, EGRTileID tileID) {
            lock (m_Tiles) {
                if (!m_Tiles.ContainsKey(tileset)) {
                    m_Tiles[tileset] = new Tuple<EGRFileSysIOTileID, List<EGRTileID>>(
                        new EGRFileSysIOTileID($"{m_TilesPath}\\{tileset}"), new List<EGRTileID>());
                }

                m_Tiles[tileset].Item1.Write(tileID);
            }
        }

        public void AddTilesFromFile(string root, string tileset) {
            int tileCount = 0;

            foreach (string tiles in Directory.EnumerateFiles(root, "*.png")) {
                string name = Path.GetFileNameWithoutExtension(tiles);
                string[] coords = name.Split('@');
                if (coords.Length != 3) {
                    LogInfo($"{name} is invalid");
                    continue;
                }

                string z = coords[0];
                string x = coords[1];
                string y = coords[2];
                if (File.Exists($"{m_TilesPath}\\{tileset}\\{z}\\{x}\\{y}\\tile.png")) continue;

                byte[] data = File.ReadAllBytes(tiles);
                AddTile(tileset, new EGRTileID { Z = int.Parse(z), X = int.Parse(x), Y = int.Parse(y), Data = data });
                tileCount++;
            }

            LogInfo($"Tiles added = {tileCount}");
        }
    }
}
