using MRK.Networking.CloudActions;

namespace MRK.Networking.CloudAPI
{
    public class Liveness : CloudAction
    {
        public override string Path
        {
            get
            {
                return "/2000/v1/liveness";
            }
        }

        public override void Execute(CloudActionContext context)
        {
            string liveCheck;
            if (context.GetRequestField("X-LiveCheck", out liveCheck))
            {
                context.Response = liveCheck == "liveNet" ? CloudResponse.Success : CloudResponse.Failure;
            }
            else
            {
                context.Response = CloudResponse.Failure;
            }

            context.Reply("live");
        }
    }
}
