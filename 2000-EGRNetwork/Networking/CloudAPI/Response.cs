using MRK.Networking.CloudActions;

namespace MRK.Networking.CloudAPI
{
    public class Response : CloudAction
    {
        public override string Path
        {
            get
            {
                return "/2000/v1/response";
            }
        }

        public override void Execute(CloudActionContext context)
        {
        }
    }
}
