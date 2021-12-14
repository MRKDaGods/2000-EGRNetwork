using MRK.Collections;
using MRK.Networking.CloudActions;
using MRK.Networking.Internal;
using MRK.Security;
using MRK.System;
using MRK.Threading;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace MRK.Networking
{
    public class CloudNetwork : Network
    {
        public const int CloudAPIVersion = 1;
        private const float ContextLifetime = 50f;
        private const int RequestBufferCapacity = 100;
        private const float RequestDensityMaxDiff = 10f;
        private const float RequestDensityMaxAllowed = 1f; //15 requests per second

        private readonly Dictionary<string, Tuple<CloudActionContext, Lifetime.Frame<CloudActionContext>>> _storedContexts;
        private readonly Dictionary<InterlockedReference<IPEndPoint>, RangedCircularBuffer> _lastRequestTimes;

        public CloudNetwork(string name, int port, string key) : base(name, port, key, true, 50)
        {
            _storedContexts = new Dictionary<string, Tuple<CloudActionContext, Lifetime.Frame<CloudActionContext>>>();
            _lastRequestTimes = new Dictionary<InterlockedReference<IPEndPoint>, RangedCircularBuffer>(new InterlockedReferenceComparer<IPEndPoint>());

            //enable unconnected messages
            _netManager.UnconnectedMessagesEnabled = true;

            //listen to unconnected events
            _eventListener.NetworkReceiveUnconnectedEvent += OnNetworkReceiveUnconnected;
        }

        protected override void OnNetworkConnectRequest(ConnectionRequest request)
        {
            //we are not accepting any connections
            request.RejectForce();
        }

        private bool AuthenticateCloudConnection(IPEndPoint remoteEndPoint, NetPacketReader reader, out CloudNetworkUser networkUser)
        {
            networkUser = null;

            try
            {
                //1- key
                string key = reader.GetString();
                if (key == Key)
                {
                    //2- hwid
                    string hwid = reader.GetString();
                    hwid = Xor.Single(hwid, (char)(key[0] ^ key[^1]));
                    if (hwid.StartsWith("egr"))
                    {
                        networkUser = new CloudNetworkUser(hwid, remoteEndPoint);
                        return true;
                    }
                }
            }
            catch
            {
            }

            return false;
        }

        private void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            _threadPool.Run(() => ProcessCloudAction(remoteEndPoint, reader));
        }

        private void ProcessCloudAction(IPEndPoint remoteEndPoint, NetPacketReader reader)
        {
            Logger.PreserveStream();
            Logger.LogInfo($"Processing cloud request from {remoteEndPoint}");

            CloudNetworkUser networkUser;
            if (!AuthenticateCloudConnection(remoteEndPoint, reader, out networkUser))
            {
                Logger.LogError($"Cannot authenticate connection, endpoint={remoteEndPoint}");
                goto __exit;
            }

            string rawCloudTransportToken = null;
            try
            {
                string cloudActionToken = reader.GetString();

                if (!ValidateCloudActionToken(cloudActionToken, networkUser.HWID, remoteEndPoint, out rawCloudTransportToken))
                {
                    Logger.LogError($"Could not validate action token, t={cloudActionToken}");
                    goto __exit;
                }

                Logger.LogInfo($"valid transport token={rawCloudTransportToken}");

                var interlockedEndpoint = new InterlockedReference<IPEndPoint>
                {
                    Value = remoteEndPoint
                };

                if (ExceedsRateLimit(interlockedEndpoint, out bool recordExists))
                {
                    Logger.LogError("Rate limit exceeded");
                    goto __fail;
                }
                else if (!recordExists)
                {
                    //add new
                    AddNewRequestTimeRecord(interlockedEndpoint);
                }

                string cloudActionPath = reader.GetString();
                Logger.LogInfo($"cloud action path={cloudActionPath}");

                CloudAction cloudAction = CloudActionFactory.GetCloudAction(cloudActionPath);
                if (cloudAction != null)
                {
                    Logger.LogInfo("Executing cloud action");
                    CloudActionContext cloudActionContext = new(this, networkUser, reader, rawCloudTransportToken);
                    if (!cloudActionContext.Valid)
                    {
                        Logger.LogError("Invalid request data");
                        goto __fail;
                    }

                    //Look up stored contexts
                    if (SendIfCached(cloudActionContext.ActionToken, cloudActionContext.RequestHeader.MiniActionToken))
                    {
                        Logger.LogInfo("Replied with cached response");
                        goto __exit;
                    }

                    cloudAction.Execute(cloudActionContext);

                    //TODO add optional field 'do-not-store'
                    //store context for future use
                    _storedContexts[cloudActionContext.ActionToken] = new(
                        cloudActionContext,
                        Lifetime.Attach(cloudActionContext, ContextLifetime, DisposeContext)
                    );

                    goto __exit;
                }
            }
            catch
            {
            }

            //we cant reply to a client with no CloudActionToken
            if (rawCloudTransportToken == null)
            {
                goto __exit;
            }

        __fail:
            //failed
            CloudAction responseAction = CloudActionFactory.GetCloudAction(CloudActionFactory.GetCloudActionPath(CloudAPIVersion, "response"));
            CloudActionContext responseActionContext = new(this, networkUser, reader, rawCloudTransportToken, true)
            {
                Response = CloudResponse.Failure
            };
            responseAction.Execute(responseActionContext);

        __exit:
            Logger.FlushPreservedStream();
        }

        private void DisposeContext(CloudActionContext context)
        {
            context.PreventFutureAccess();
            _storedContexts.Remove(context.ActionToken);

            Logger.LogInfo($"Disposed context {context.ActionToken}");
        }

        private bool SendIfCached(string actionToken, string miniToken)
        {
            if (!_storedContexts.TryGetValue(actionToken, out var context)) return false;

            //acquire lock in this thread
            return context.Item1.Interlocked(() => {
                //we have reached max attempts
                if (!context.Item1.Sendable)
                {
                    //dispose the attached frame
                    context.Item2.Interlocked(() => context.Item2.Dispose());
                    return false;
                }
                else
                {
                    //send the exact same response again
                    context.Item1.Retry(miniToken);
                }

                return true;
            });
        }

        public void CloudSend(CloudActionContext context)
        {
            if (context == null || context.NetworkUser == null)
            {
                return;
            }

            try
            {
                _netManager.SendUnconnectedMessage(context.DataWriter, context.NetworkUser.RemoteEndPoint);
            }
            catch
            {
                Logger.LogError("Failed to CloudSend to " + context.NetworkUser.RemoteEndPoint);
            }
        }

        private bool ValidateCloudActionToken(string token, string hwid, IPEndPoint remoteEndPoint, out string rawToken)
        {
            rawToken = null;
            if (token == null || hwid == null || remoteEndPoint == null)
            {
                return false;
            }

            //THIS BREAKS VPNs
            byte[] addr = Encoding.UTF8.GetBytes(hwid); //remoteEndPoint.Address.GetAddressBytes();
            //xor addr with hwid.length
            Xor.SingleNonAlloc(addr, (char)hwid.Length);

            //xor with hwid[0] ^ hwid[^1]
            Xor.SingleNonAlloc(addr, (char)(hwid[0] ^ hwid[^1]));

            rawToken = Xor.Multiple(token, addr);
            return rawToken.StartsWith("egr");
        }

        private bool ExceedsRequestDensity(RangedCircularBuffer buffer)
        {
            return buffer.Density(Time.RelativeTimeSeconds, RequestDensityMaxDiff) > RequestDensityMaxAllowed;
        }

        private bool ExceedsRateLimit(InterlockedReference<IPEndPoint> interlockedEndpoint, out bool recordExists)
        {
            if (_lastRequestTimes.TryGetValue(interlockedEndpoint, out RangedCircularBuffer buffer))
            {
                recordExists = true;

                return interlockedEndpoint.Interlocked(() => {
                    buffer.Add(Time.RelativeTimeSeconds);
                    return ExceedsRequestDensity(buffer);
                });
            }

            recordExists = false;
            return false;
        }

        private bool RequestTimeTimeout(Tuple<InterlockedReference<IPEndPoint>, RangedCircularBuffer> tuple)
        {
            return Time.RelativeTimeSeconds - tuple.Item2.Current <= RequestDensityMaxDiff * 2;
        }

        private void AddNewRequestTimeRecord(InterlockedReference<IPEndPoint> interlockedEndpoint)
        {
            lock (_lastRequestTimes)
            {
                var buffer = new RangedCircularBuffer(RequestBufferCapacity);
                _lastRequestTimes[interlockedEndpoint] = buffer;

                //attach lifetime
                Lifetime.Attach(
                    new Tuple<InterlockedReference<IPEndPoint>, RangedCircularBuffer>(interlockedEndpoint, buffer),
                    RequestTimeTimeout,
                    DisposeRequestTime
                );
            }
        }

        private void DisposeRequestTime(Tuple<InterlockedReference<IPEndPoint>, RangedCircularBuffer> tuple)
        {
            lock (_lastRequestTimes)
            {
                _lastRequestTimes.Remove(tuple.Item1);
            }
        }
    }
}
