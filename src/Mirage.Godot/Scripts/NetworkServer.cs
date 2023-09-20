using System;
using System.Collections.Generic;
using Mirage.RemoteCalls;

namespace Mirage
{
    public class NetworkServer : MirageServer
    {
        public NetworkWorld World { get; internal set; }

        public INetworkPlayer LocalPlayer => LocalClient?.Player;
        public NetworkClient LocalClient { get; internal set; }
        internal RpcHandler _rpcHandler;

        internal void Spawn(NetworkIdentity obj)
        {
            throw new NotImplementedException();
        }

        public void SendToObservers<T>(NetworkIdentity identity, T msg, bool excludeLocalPlayer, bool excludeOwner, Channel channelId = Channel.Reliable)
        {
            var observers = identity.observers;
            if (observers.Count == 0)
                return;

            using (var list = AutoPool<List<INetworkPlayer>>.Take())
            {
                var enumerator = observers.GetEnumerator();
                ListHelper.AddToList(list, enumerator, excludeLocalPlayer ? LocalPlayer : null, excludeOwner ? identity.Owner : null);
                NetworkServer.SendToMany(list, msg, channelId);
            }
        }
    }
}
