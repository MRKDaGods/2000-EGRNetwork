namespace MRK.Networking.CloudActions
{
    public abstract class CloudAction
    {
        private readonly CloudActionContext _context;
        private CloudActionState _state;
        private bool _failed;

        public abstract string Path
        {
            get;
        }

        public CloudActionContext Context
        {
            get { return _context; }
        }

        public CloudActionState State
        {
            get { return _state; }
        }

        public CloudResponse Response
        {
            get { return _context.Response; }
        }

        public bool Failed
        {
            get { return _failed; }
        }

        public CloudAction(CloudActionContext context)
        {
            _context = context;
            _context.Action = this;
            _context.ResponseReceived += OnResponseReceived;

            _state = CloudActionState.None;
        }

        public void Send()
        {
            if (_state != CloudActionState.None) return;

            //add all request fields and stuff here
            OnRequestSend();

            _state = CloudActionState.Sending;
            _context.CloudActionToken = EGRUtils.GetRandomString(16); //act token
            _context.Serialize();

            SendInternal();
        }

        private void SendInternal()
        {
            //queue to transport
            _context.Transport.QueueAction(this);
        }

        public void SetFailed()
        {
            _failed = true;
            _context.Response = CloudResponse.Failure;
            SetState(CloudActionState.Received);
        }

        public void SetState(CloudActionState state)
        {
            if (state < _state)
            {
                Logger.LogError($"Attempting to set state of lower val!! s={state} _s={_state}");
                return;
            }

            Logger.LogInfo($"CloudAction state updated from {_state} to {state}");
            _state = state;
        }

        protected virtual void OnRequestSend()
        {
        }

        protected virtual void OnResponseReceived()
        {
        }
    }
}
