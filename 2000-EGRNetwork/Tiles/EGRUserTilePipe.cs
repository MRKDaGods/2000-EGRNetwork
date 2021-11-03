using MRK.Networking;
using MRK.Networking.Packets;
using System.Collections.Generic;
using System.Threading;

namespace MRK {
    public class EGRUserTileRequest {
        public string Tileset;
        public EGRTileID TileID;
        public bool Low;
        public int Buffer;
        public int Hash;
        public bool Cancelled;
    }

    public class EGRUserTilePipe : MRKBehaviour {
        readonly EGRSessionUser m_SessionUser;
        readonly List<EGRUserTileRequest> m_TileRequests;
        bool m_IsRunning;
        readonly List<EGRUserTileRequest> m_ActiveRequests;

        public EGRUserTilePipe(EGRSessionUser sessionUser) {
            m_SessionUser = sessionUser;
            m_TileRequests = new List<EGRUserTileRequest>();
            m_ActiveRequests = new List<EGRUserTileRequest>();

            //start thread
            m_IsRunning = true;
            new Thread(PipeThread).Start();
        }

        public void QueueTile(EGRUserTileRequest request) {
            lock (m_TileRequests) {
                m_TileRequests.Add(request);
            }
        }

        void SendTile(EGRUserTileRequest request) {
            lock (m_ActiveRequests) {
                m_ActiveRequests.Add(request);
            }

            Client.TileManager.GetTile(request.Tileset, request.TileID, request.Low, (tile) => {
                lock (m_ActiveRequests) {
                    m_ActiveRequests.Remove(request);
                }

                if (request.Cancelled)
                    return;

                m_SessionUser.Network.SendPacket(m_SessionUser.Peer, request.Buffer, PacketType.TILEFETCH, DeliveryMethod.ReliableUnordered, (stream) => {
                    bool success = tile != null;
                    stream.WriteBool(success);
                    stream.Write<EGRTileID>(request.TileID);

                    if (success) {
                        stream.WriteInt32(tile.Data.Length); //dataSz
                        stream.WriteBytes(tile.Data); //data
                    }
                });
            });
        }

        void PipeThread() {
            while (m_IsRunning) {
                if (m_ActiveRequests.Count <= 5) {
                    if (m_TileRequests.Count > 0) {
                        EGRUserTileRequest request;
                        lock (m_TileRequests) {
                            request = m_TileRequests[0];
                            m_TileRequests.RemoveAt(0);
                        }

                        SendTile(request);
                    }
                }

                Thread.Sleep(100);
            }
        }

        public void CancelRequest(string tileset, int hash, bool low) {
            int idx = m_TileRequests.FindIndex(x => x.Tileset == tileset && x.Hash == hash && x.Low == low);
            if (idx != -1) {
                lock (m_TileRequests) {
                    m_TileRequests.RemoveAt(idx);
                }
            }
            else {
                EGRUserTileRequest req = m_ActiveRequests.Find(x => x.Tileset == tileset && x.Hash == hash && x.Low == low);
                if (req != null) {
                    req.Cancelled = true;
                }
            }
        }

        public void Stop() {
            m_IsRunning = false;
        }
    }
}
