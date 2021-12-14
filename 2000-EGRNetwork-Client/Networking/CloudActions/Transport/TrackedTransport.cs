//
// Client tracked transport
//

using MRK.Networking.Internal;
using MRK.Networking.Internal.Utils;
using MRK.System;
using System.Net;
using ThreadPool = MRK.Threading.ThreadPool;

namespace MRK.Networking.CloudActions.Transport
{
    public class TrackedTransport
    {
        private const int TrackLoopInterval = 100;
        private const float RequestTimeout = 5f;
        private const int MaximumRequestCount = 10;
        public const byte TrackResponseAck = 0x0;
        public const byte TrackResponseData = 0x1;
        public const byte TrackResponseScalar = 0x2;
        public const int MiniActionTokenLength = 10;

        private readonly ThreadPool _threadPool;
        private readonly List<TrackingRequest> _trackingRequests;
        private readonly IPEndPoint _endPoint;
        private readonly NetManager _netManager;
        private readonly CloudAuthentication _cloudAuthentication;

        public TrackedTransport(IPEndPoint endPoint, NetManager netManager, EventBasedNetListener netListener, CloudAuthentication cloudAuthentication)
        {
            _threadPool = new ThreadPool(15, 5);
            _trackingRequests = new List<TrackingRequest>();

            _endPoint = endPoint;
            _netManager = netManager;
            _cloudAuthentication = cloudAuthentication;

            netListener.NetworkReceiveUnconnectedEvent += OnNetworkReceiveUnconnected;
        }

        private void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            //ensure that our endpoint is the one communicating with us
            if (!_endPoint.Equals(remoteEndPoint))
            {
                Logger.LogError($"Received message from unknown endpoint, ep={remoteEndPoint}");
                return;
            }

            try
            {
                byte eventType = reader.GetByte();
                if (eventType > TrackResponseScalar)
                {
                    //invalid !!
                    Logger.LogError($"Invalid eventtype {eventType}");
                    return;
                }

                //read
                string miniActionToken = reader.GetString();
                TrackingRequest? trackingRequest = _trackingRequests.Find(x => {
                    return (eventType != TrackResponseScalar && x.MiniActionToken == miniActionToken)
                        || x.HasTransportToken(miniActionToken);
                });

                if (trackingRequest == null)
                {
                    Logger.LogError($"Cannot find tracker request {miniActionToken}");
                    return;
                }

                trackingRequest.ProcessEvent(eventType, reader);
            }
            catch (Exception ex)
            {
                Logger.LogError("Error processing network event data");
                Logger.LogInfoIndented(ex.ToString(), 1);
            }
        }

        public void QueueAction(CloudAction cloudAction)
        {
            if (cloudAction == null) return;

            TrackingRequest trackingRequest = new(cloudAction);
            Track(trackingRequest);
        }

        private void Track(TrackingRequest request)
        {
            if (request == null || _trackingRequests.Contains(request)) return;

            lock (_trackingRequests)
            {
                _trackingRequests.Add(request);
            }

            _threadPool.Run(() => TrackLoop(request));
        }

        private void TrackLoop(TrackingRequest request)
        {
            request.MarkStarted();

            Logger.LogInfo($"[Transport] tracking {request.Action.Context.RequestHeader.CloudActionToken}");

            while (request.Running)
            {
                switch (request.ActionState)
                {
                    case CloudActionState.Sending:
                        if (request.TimeSinceLastRequest > RequestTimeout)
                        {
                            if (request.RequestsSent++ >= MaximumRequestCount)
                            {
                                Logger.LogError($"[Transport] failed {request.Action.Context.RequestHeader.CloudActionToken}");
                                request.Action.SetFailed();
                            }
                            else
                            {
                                request.Interlocked(() => {
                                    request.TimeSinceLastRequest = Time.RelativeTimeSeconds;
                                    request.PrepareForNewRequest();

                                    //write mini action token for tracking
                                    request.Action.Context.WriteMiniActionTokenToSerializedBuffer(request.MiniActionToken);

                                    SendWithMiniToken(request.Action.Context, request.MiniActionToken, request);
                                    Logger.LogInfo($"[Transport] sent {request.Action.Context.RequestHeader.CloudActionToken} wmini={request.MiniActionToken}");
                                });
                            }
                        }
                        break;

                    case CloudActionState.Received:
                        Logger.LogInfo($"[Transport] {request.MiniActionToken} request elapsed {request.TimeSinceLastRequest}s");
                        request.Running = false;
                        break;
                }

                Thread.Sleep(TrackLoopInterval);
            }

            lock (_trackingRequests)
            {
                _trackingRequests.Remove(request);
            }
        }

        //UNSECURE !!!
        private void Send(CloudActionContext context)
        {
            if (context == null) return;

            _netManager.SendUnconnectedMessage(context.RequestData, _endPoint);
        }

        private void SendWithMiniToken(CloudActionContext context, string? miniActionToken, TrackingRequest request)
        {
            if (context == null || string.IsNullOrEmpty(miniActionToken)) return;

            NetDataWriter data = context.RequestData;
            _cloudAuthentication.AuthenticateDataStream(ref data, out string transportToken);
            request.AddTransportToken(transportToken);

            data.Put(miniActionToken);
            _netManager.SendUnconnectedMessage(data, _endPoint);
        }

        public void Stop()
        {
            lock (_trackingRequests)
            {
                foreach (TrackingRequest request in _trackingRequests)
                {
                    request.Interlocked(() => request.Running = false);
                }

                _trackingRequests.Clear();
            }

            _threadPool.Terminate();
        }
    }
}
