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


        internal void Prepare()
        {
            if (Root == null)
                Root = this;

            SpawnHash = Root.Name.GetHashCode();
            GD.Print($"Settings SpawnHash to {SpawnHash} for {Root.Name}");
        }
        internal void ServerSpawn(NetworkServer server)
        {

        }
        internal void ClientSpawn(NetworkClient client, SpawnMessage spawnMessage)
        {

        }

        internal void OnDeserializeAll(PooledNetworkReader payloadReader, bool v)
        {
            throw new NotImplementedException();
        }
    }
}
