using MRK.Networking.Packets;
using System;
using System.IO;
using static MRK.EGRLogger;

namespace MRK {
    public struct EGRTileID : IMRKNetworkSerializable<EGRTileID> {
        public int Z;
        public int X;
        public int Y;

        public EGRTileID() {
            Z = X = Y = 0;
        }

        public override string ToString() {
            return $"{Z}/{X}/{Y}";
        }

        public override int GetHashCode() {
            int hash = X.GetHashCode();
            hash = (hash * 397) ^ Y.GetHashCode();
            hash = (hash * 397) ^ Z.GetHashCode();

            return hash;
        }

        public void Write(PacketDataStream stream) {
            stream.WriteInt32(Z);
            stream.WriteInt32(X);
            stream.WriteInt32(Y);
        }

        public void Read(PacketDataStream stream) {
            Z = stream.ReadInt32();
            X = stream.ReadInt32();
            Y = stream.ReadInt32();
        }
    }

    public class EGRTileManager : MRKBehaviour {
        const int TILEPOOL_INTERVAL = 500;

        readonly string m_RootPath;
        IEGRRemoteTileProvider m_TileProvider;
        readonly MRKThreadPool m_LocalThreadPool;

        string TilesPath => $"{m_RootPath}\\Tiles";

        public EGRTileIO TileIO { get; private set; }

        public EGRTileManager(string rootPath) {
            m_RootPath = rootPath;
            TileIO = new EGRTileIO(TilesPath);

            InitTileProvider();

            m_LocalThreadPool = new MRKThreadPool(TILEPOOL_INTERVAL);
        }

        void InitTileProvider() {
            string type = Client.Config[$"TILE_MANAGER_DEFAULT_PROVIDER_TYPE"].String;
            switch (type) {
                case "TOKEN_AUTHENTICATED":
                    string name = Client.Config["TILE_MANAGER_DEFAULT_PROVIDER_NAME"].String;
                    string token = Client.Config["TILE_MANAGER_DEFAULT_PROVIDER_TOKEN"].String;
                    m_TileProvider = new EGRRemoteTileProviderTokenAuthenticated(name, token, true);
                    break;

                default:
                    LogError("[Tile Manager] Invalid TileProvider!!!");
                    break;
            }
        }

        public bool IsTileIDValid(EGRTileID tileID) {
            int maxValidTile = (1 << tileID.Z) - 1;
            return tileID.X > maxValidTile || tileID.X < 0 || tileID.Y > maxValidTile || tileID.Y < 0;
        }

        public void GetTile(string tileset, EGRTileID tileID, bool lowRes, Action<EGRTile> callback) {
            if (callback == null)
                return;

            if (IsTileIDValid(tileID)) {
                callback?.Invoke(null);
                return;
            }

            EGRTile tile = TileIO.GetTile(tileset, tileID, lowRes);
            if (tile != null) {
                callback(tile);
                return;
            }

            if (m_TileProvider == null) {
                LogError("[Tile Manager] Cannot fetch tile due to invalid TileProvider");
                return;
            }

            string request = m_TileProvider.GetTileRequest(tileset, tileID, false);
            LogInfo($"[Tile Manager] Processing request {request}");
            Client.DownloadManager.Download(request, (info) => {
                if (info.Failure != null) {
                    LogError($"[Tile Manager] Tile download failed ID=({tileID}) {info.Failure}");
                    callback(null);
                    return;
                }

                //TODO check for queued tiles (DOUBLE REQS)
                LogInfo($"[Tile Manager] Tile downloaded, elapsed={info.ElapsedTime}s size={info.Data.Length} bytes");

                if (lowRes) {
                    //encode
                    m_LocalThreadPool.QueueTask(() => {
                        MemoryStream stream = MRKImageEncoder.EncodeImageWithQuality(info.Data, 256, 256);
                        byte[] data = stream != null ? stream.GetBuffer() : info.Data;
                        callback(new EGRTile {
                            ID = tileID,
                            Data = data,
                            LowResolution = lowRes
                        });

                        TileIO.AddTile(tileset, tileID, lowRes, data);
                        TileIO.AddTile(tileset, tileID, false, info.Data); //the higher quality one

                        if (stream != null) {
                            stream.Dispose();
                        }
                    });
                }
                else {
                    m_LocalThreadPool.QueueTask(() => TileIO.AddTile(tileset, tileID, lowRes, info.Data));
                    callback(new EGRTile {
                        ID = tileID,
                        Data = info.Data,
                        LowResolution = lowRes
                    });
                }
            });
        }
    }
}
