using MRK.Networking.CloudActions;

namespace MRK.Networking.CloudAPI
{
    public class Liveness : CloudAction
    {
        private readonly string _extraInfo;

        public override string Path
        {
            get { return "/2000/v1/liveness"; }
        }

        public Liveness(CloudActionContext context, string extraInfo) : base(context)
        {
            _extraInfo = extraInfo;
        }

        protected override void OnRequestSend()
        {
            //just send a hi
            Context.AddField(new CloudRequestFieldString("X-LiveCheck", "liveNet"));
            Context.AddField(new CloudRequestFieldString("X-LiveExtraInfo", _extraInfo));
        }

        protected override void OnResponseReceived()
        {
            Logger.LogInfo($"Liveness received, r={Response}");
            Logger.LogInfo($"Liveness sent: {Context.ResponseData.GetString()}");
        }
    }
}
