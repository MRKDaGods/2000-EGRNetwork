using MRK.Services;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace MRK.Networking
{
    public class Server
    {
        private readonly HashSet<Network> _runningNetworks;
        private readonly Dictionary<string, ServerNetworkUser> _connectedUsers;
        private CloudNetwork _cloudNetwork;
        private readonly Dictionary<Type, Service> _services;

        public Server()
        {
            _runningNetworks = new HashSet<Network>();
            _connectedUsers = new Dictionary<string, ServerNetworkUser>();
            _services = new Dictionary<Type, Service>();
        }

        public void Initialize()
        {
            EGRNetworkConfig config = EGR.Config;
            _cloudNetwork = new CloudNetwork("CLOUD", config["NET_CLOUD_PORT"].Int, config["NET_CLOUD_KEY"].String);
            _cloudNetwork.Start();

            EGR.GlobalThreadPool.Run(InitializeServices);

            //call CloudActionFactory static ctor
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(CloudActions.CloudActionFactory).TypeHandle);
        }

        private void InitializeServices()
        {
            Logger.LogInfo("Initializing services");

            foreach (Type type in Assembly.GetExecutingAssembly().ManifestModule.GetTypes())
            {
                if (type.BaseType == typeof(Service))
                {
                    Service service = (Service)Activator.CreateInstance(type);
                    service.Initialize();
                    _services[type] = service;
                }
            }
        }

        public void Stop()
        {
            foreach (Service service in _services.Values) service.Shutdown();
            _cloudNetwork.Stop();
        }

        public NetworkUser GetOrAddUser(string hwid)
        {
            ServerNetworkUser networkUser;
            if (!_connectedUsers.TryGetValue(hwid, out networkUser))
            {
                networkUser = new ServerNetworkUser(hwid);
                _connectedUsers[hwid] = networkUser;
            }

            return networkUser;
        }

        public void RemoveUser(Network network, ServerNetworkUser networkUser)
        {
            if (_connectedUsers.ContainsKey(networkUser.HWID))
            {
                networkUser.ConnectedNetworks.Remove(network);
                if (networkUser.ConnectedNetworks.Count == 0)
                {
                    //remove user from Server COMPLETELY
                    _connectedUsers.Remove(networkUser.HWID);
                }
            }
        }

        public T GetService<T>() where T : Service
        {
            Service service;
            if (_services.TryGetValue(typeof(T), out service)) return (T)service;
            return null;
        }
    }
}
