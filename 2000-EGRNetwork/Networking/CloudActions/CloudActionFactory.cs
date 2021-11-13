using System;
using System.Collections.Generic;
using System.Reflection;

namespace MRK.Networking.CloudActions
{
    public class CloudActionFactory
    {
        private static readonly Dictionary<string, CloudAction> _cloudActions;

        static CloudActionFactory()
        {
            _cloudActions = new Dictionary<string, CloudAction>();

            //resolve cloud actions
            EGR.GlobalThreadPool.Run(ResolveCloudActions);
        }

        private static void ResolveCloudActions()
        {
            //use reflection
            foreach (Type type in Assembly.GetExecutingAssembly().ManifestModule.GetTypes())
            {
                if (type.BaseType == typeof(CloudAction))
                {
                    CloudAction cloudAction = (CloudAction)Activator.CreateInstance(type);
                    _cloudActions[cloudAction.Path] = cloudAction;
                }
            }
        }

        public static CloudAction GetCloudAction(string path)
        {
            CloudAction cloudAction;
            _cloudActions.TryGetValue(path, out cloudAction);
            return cloudAction;
        }

        public static string GetCloudActionPath(int version, string name)
        {
            return $"/2000/v{version}/{name}";
        }
    }
}
