namespace MRK.Networking.CloudActions
{
    public abstract class CloudAction
    {
        public abstract string Path
        {
            get;
        }

        public abstract void Execute(CloudActionContext context);
    }
}
