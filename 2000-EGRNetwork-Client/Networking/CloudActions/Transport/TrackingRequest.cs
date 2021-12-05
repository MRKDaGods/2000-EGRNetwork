﻿using MRK.Networking.Internal;
using MRK.System;

namespace MRK.Networking.CloudActions.Transport
{
    public class TrackingRequest
    {
        private readonly CloudAction _action;
        private float _lastRequestTime;
        private float _requestStartTime;
        private readonly object _lock;

        public CloudActionState ActionState
        {
            get { return _action.State; }
        }

        public CloudAction Action
        {
            get { return _action; }
        }

        public float TimeSinceLastRequest
        {
            get { return Time.RelativeTimeSeconds - _lastRequestTime; }
            set { _lastRequestTime = value; }
        }

        public float TimeSinceRequestStart
        {
            get { return Time.RelativeTimeSeconds - _requestStartTime; }
        }

        public int RequestsSent
        {
            get; set;
        }

        public string? MiniActionToken
        {
            get; private set;
        }

        public byte TrackResponse
        {
            get; set;
        }

        public bool Running
        {
            get; set;
        }

        public TrackingRequest(CloudAction action)
        {
            _lock = new object();
            _action = action;

            RequestsSent = 0;
            TrackResponse = 0x0;
            Running = true;
        }

        public void MarkStarted()
        {
            _requestStartTime = Time.RelativeTimeSeconds;
        }

        public void PrepareForNewRequest()
        {
            MiniActionToken = EGRUtils.GetRandomString(TrackedTransport.MiniActionTokenLength);
        }

        public void Interlocked(Action action)
        {
            if (action != null)
            {
                lock (_lock)
                {
                    action();
                }
            }
        }

        public void ProcessEvent(byte eventType, NetPacketReader reader)
        {
            if (ActionState == CloudActionState.Received) return;

            if (eventType == TrackedTransport.TrackResponseAck)
            {
                if (ActionState == CloudActionState.Sending)
                {
                    _action.SetState(CloudActionState.Sent);
                }
            }
            else if (eventType == TrackedTransport.TrackResponseData)
            {
                if (ActionState == CloudActionState.Sending || ActionState == CloudActionState.Sent)
                {
                    _action.Context.SetResponse(reader);
                    _action.SetState(CloudActionState.Received);
                }
            }
        }
    }
}
