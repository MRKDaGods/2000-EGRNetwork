using static MRK.EGRLogger;

namespace MRK {
    public class EGRRemoteTileProviderTokenAuthenticated : EGRBase, IEGRRemoteTileProvider {
        const string ZOOM = "{ZOOM}";
        const string X = "{X}";
        const string Y = "{Y}";
        const string TOKEN = "{TOKEN}";
        const string RESOLUTION = "{RESOLUTION}";
        const string TILESET = "{TILESET}";
        
        readonly string m_BaseURL;
        readonly string m_ProviderName;
        readonly string m_Token;
        readonly Dictionary<string, string> m_TilesetTranslations;
        readonly string m_HighResolution;
        readonly string m_LowResolution;

        public EGRRemoteTileProviderTokenAuthenticated(string name, string token, bool logInfo = false) {
            m_BaseURL = Client.Config[$"REMOTE_TILE_PROVIDER_{name}"].String;
            m_ProviderName = name;
            m_Token = token;
            m_TilesetTranslations = new Dictionary<string, string>();

            string[] resTranslationStr = Client.Config[$"RESOLUTION_TRANSLATION_{name}"].String.Split('|');
            m_HighResolution = resTranslationStr[0];
            m_LowResolution = resTranslationStr[1];

            if (logInfo) {
                LogInfo($"Initialized EGRRemoteTileProviderTokenAuthenticated, name={name} token={token} baseURL={m_BaseURL}");
            }
        }

        public string GetTileRequest(string tileset, EGRTileID tileID, bool low) {
            string translatedTileset;
            if (!m_TilesetTranslations.TryGetValue(tileset, out translatedTileset)) {
                string translation = Client.Config[$"TILESET_TRANSLATION_{m_ProviderName}_{tileset}"].String;
                m_TilesetTranslations[tileset] = translation;
                translatedTileset = translation;
            }

            return m_BaseURL.Replace(TILESET, translatedTileset)
                .Replace(ZOOM, tileID.Z.ToString())
                .Replace(X, tileID.X.ToString())
                .Replace(Y, tileID.Y.ToString())
                .Replace(RESOLUTION, low ? m_LowResolution : m_HighResolution)
                .Replace(TOKEN, m_Token);
        }
    }
}
