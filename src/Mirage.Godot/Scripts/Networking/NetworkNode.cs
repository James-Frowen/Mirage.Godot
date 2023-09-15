using System;
using Godot;
using Mirage;
using Mirage.Serialization;
using MirageGodot.Messages;

namespace MirageGodot
{
    public abstract partial class NetworkNode : Node
    {
        /// <summary>
        /// Set this to parent node if NetworkNode isn't the root
        /// </summary>
        [Export] public Node Root;
        // todo ReadOnly
        [Export] public int SpawnHash;


        public uint NetId { get; internal set; }
        /// <summary>
        /// [SERVER ONLY] Players that owns this object
        /// </summary>
        public INetworkPlayer Player { get; internal set; }

        /// <summary>
        /// Does local peer have control over this object
        /// <para>true on server if <see cref="Player"/> is null</para>
        /// </summary>
        public bool HasAuthority { get; internal set; }

        public NetworkNodeEvents Events { get; internal set; }
        public NetworkServer Server { get; private set; }
        public NetworkClient Client { get; private set; }

        internal void Prepare(bool prefab)
        {
            Root ??= this;

            // prefabs are negative
            // scene objects are positive
            SpawnHash = Math.Abs(Root.Name.GetHashCode());
            if (prefab)
                SpawnHash *= -1;

            GD.Print($"Settings SpawnHash to {SpawnHash} for {Root.Name}");
        }
        internal void ServerSpawn(NetworkServer server)
        {
            Server = server;
            throw new NotImplementedException();
        }
        internal void ClientSpawn(NetworkClient client, SpawnMessage spawnMessage)
        {
            Client = client;
            throw new NotImplementedException();
        }

        internal void OnDeserializeAll(PooledNetworkReader payloadReader, bool v)
        {
            throw new NotImplementedException();
        }
    }
}
