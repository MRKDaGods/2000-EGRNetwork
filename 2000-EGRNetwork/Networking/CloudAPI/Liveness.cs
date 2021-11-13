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
            context.Response = CloudResponse.Success;
            context.Reply("live");
        }
    }
}
