﻿using static MRK.EGRLogger;

namespace MRK.Networking.Packets {
    [PacketHandler(PacketType.LGNACCTK)]
    public class PacketHandleLoginAccountToken {
        static void Handle(EGRNetwork network, EGRSessionUser sessionUser, PacketDataStream stream, int buffer) {
            if (string.IsNullOrEmpty(sessionUser.HWID)) {
                LogError($"[{sessionUser.Peer.Id}] does not have a valid hwid, hwid={sessionUser.HWID}");
                return;
            }

            if (sessionUser.Account != null) {
                LogError($"[{sessionUser.Peer.Id}] is already logged in, token={sessionUser.Token}");

                network.SendPacket(sessionUser.Peer, buffer, PacketType.LGNACC, DeliveryMethod.ReliableOrdered, (x) => {
                    x.WriteByte((byte)EGRStandardResponse.FAILED);
                });

                return;
            }

            string token = stream.ReadString();

            EGRAccount acc;
            bool success = EGRMain.Instance.AccountManager.LoginAccount(token, sessionUser, out acc);
            if (success) {
                sessionUser.AssignAccount(acc);
            }

            LogInfo($"[{sessionUser.Peer.Id}] login acc tk, tk={token}, hwid={sessionUser.HWID}, result={success}");

            network.SendPacket(sessionUser.Peer, buffer, PacketType.LGNACC, DeliveryMethod.ReliableOrdered, (x) => {
                x.WriteByte(success ? (byte)EGRStandardResponse.SUCCESS : (byte)EGRStandardResponse.FAILED);

                if (success) {
                    x.WriteString(acc.FirstName);
                    x.WriteString(acc.LastName);
                    x.WriteString(acc.Email);
                    x.WriteSByte(acc.Gender);
                    x.WriteString(sessionUser.Token.Token);

                    if (!acc.IsDeviceID())
                        x.WriteString(acc.Password);
                }
            });
        }
    }
}
