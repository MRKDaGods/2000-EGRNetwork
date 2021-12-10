using MRK.Networking.CloudActions;

namespace MRK.Networking.CloudAPI
{
    public class Liveness : CloudAction
    {
        private static int ms_Counter = 0;

        public override string Path
        {
            get
            {
                return "/2000/v1/liveness";
            }
        }

        public override void Execute(CloudActionContext context)
        {
            ms_Counter++;
            context.Response = CloudResponse.Success;
            context.Reply(ms_Counter.ToString());
        }
    }
}
