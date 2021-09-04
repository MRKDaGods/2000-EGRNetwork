namespace MRK {
    public class EGRTileIO {
        readonly string m_RootPath;

        public EGRTileIO(string rootPath) {
            m_RootPath = rootPath;

            if (!Directory.Exists(rootPath)) {
                MRKPlatformUtils.CreateRecursiveDirectory(rootPath);
            }
        }

        public string GetTilesetDirectory(string tileset) {
            return MRKPlatformUtils.LocalizePath($"{m_RootPath}|{tileset}");
        }

        public bool TilesetExists(string tileset) {
            return Directory.Exists(GetTilesetDirectory(tileset));
        }

        public void CreateTileset(string tileset) {
            Directory.CreateDirectory(GetTilesetDirectory(tileset));
        }

        public string GetQualifiedTileDirectory(string tileset, EGRTileID tileID) {
            return MRKPlatformUtils.LocalizePath($"{GetTilesetDirectory(tileset)}|{tileID.Z}|{tileID.X}|{tileID.Y}");
        }

        public bool TileExists(string tileset, EGRTileID tileID) {
            return Directory.Exists(GetQualifiedTileDirectory(tileset, tileID));
        }

        public string GetLocalTileResName(bool low) {
            return (low ? "L" : "H") + ".png";
        }

        public EGRTile GetTile(string tileset, EGRTileID tileID, bool lowRes) {
            if (!TilesetExists(tileset))
                return null;

            if (!TileExists(tileset, tileID))
                return null;

            string tileParentDir = GetQualifiedTileDirectory(tileset, tileID);
            string targetTile = MRKPlatformUtils.LocalizePath($"{tileParentDir}|{GetLocalTileResName(lowRes)}");

            //additional check
            if (!File.Exists(targetTile))
                return null; //oops?

            try {
                byte[] data = File.ReadAllBytes(targetTile);
                return new EGRTile {
                    ID = tileID,
                    Data = data,
                    LowResolution = lowRes
                };
            }
            catch {
                return null;
            }
        }

        public void AddTile(string tileset, EGRTileID tileID, bool lowRes, byte[] data) {
            //check if tileset exists
            if (!TilesetExists(tileset)) {
                CreateTileset(tileset);
            }

            string tileParentDir = GetQualifiedTileDirectory(tileset, tileID);
            if (!Directory.Exists(tileParentDir)) {
                //create local dir
                MRKPlatformUtils.CreateRecursiveDirectory(tileParentDir);
            }

            string targetTile = MRKPlatformUtils.LocalizePath($"{tileParentDir}|{GetLocalTileResName(lowRes)}");
            File.WriteAllBytes(targetTile, data);
        }
    }
}
