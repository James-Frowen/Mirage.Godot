using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Mirage.Collections;
using Mirage.Messages;
using Mirage.RemoteCalls;
using Mirage.Serialization;

namespace Mirage
{
    public static class SpawnHashHelper
    {
        public static bool IsPrefab(int hash)
        {
            return hash < 0;
        }
        public static bool IsSceneObject(int hash)
        {
            return hash > 0;
        }

        public static int CalculateId(Node root, bool isPrefab)
        {
            // prefabs are negative
            // scene objects are positive
            var spawnHash = Math.Abs(root.Name.GetHashCode());
            if (isPrefab)
                spawnHash *= -1;
            return spawnHash;
        }
    }

    public abstract partial class NetworkIdentity : Node
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
        public new INetworkPlayer Owner { get; internal set; }

        /// <summary>
        /// Does local peer have control over this object
        /// <para>true on server if <see cref="Owner"/> is null</para>
        /// </summary>
        public bool HasAuthority { get; internal set; }
        /// <summary>
        /// Is the main object that has <see cref="HasAuthority"/>
        /// </summary>
        public bool IsMainCharacter { get; internal set; }

        public NetworkNodeEvents Events { get; internal set; }
        public NetworkServer Server { get; private set; }
        public NetworkClient Client { get; private set; }
        public NetworkWorld World { get; internal set; }
        public SyncVarSender SyncVarSender { get; internal set; }
        public readonly HashSet<INetworkPlayer> observers = new HashSet<INetworkPlayer>();


        public bool IsServer => Server != null;
        public bool IsClient => Client != null;

        /// <summary>
        /// Child nodes
        /// </summary>
        private INetworkNode[] _nodes;
        public IReadOnlyList<INetworkNode> NetworkBehaviours => _nodes;


        internal void Prepare(bool prefab)
        {
            Root ??= this;
            SpawnHash = SpawnHashHelper.CalculateId(Root, prefab);

            _nodes = GetACllhildNodes(Root).ToArray();
            GD.Print($"Settings SpawnHash to {SpawnHash} for {Root.Name}");
        }

        private static IEnumerable<INetworkNode> GetACllhildNodes(Node node)
        {
            // todo can we use find_children instead?
            if (node is INetworkNode nn)
                yield return nn;

            foreach (var child in node.GetChildren())
            {
                GetACllhildNodes(child);
            }
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

        internal void OnDeserializeAll(PooledNetworkReader reader, bool initial)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// objects that can synchronize themselves, such as synclists
        /// </summary>
        protected readonly List<(INetworkNode node, ISyncObject obj)> syncObjects = new List<(INetworkNode node, ISyncObject obj)>();

        // this gets called in the constructor by the weaver
        // for every SyncObject in the component (e.g. SyncLists).
        // We collect all of them and we synchronize them with OnSerialize/OnDeserialize
        public void InitSyncObject(INetworkNode node, ISyncObject syncObject)
        {
            syncObjects.Add((node, syncObject));
            syncObject.OnChange += () => SyncObject_OnChange(node);
        }
        private void SyncObject_OnChange(INetworkNode node)
        {
            // dont need to mark dirty if already dirty
            if (_anySyncObjectDirty)
                return;

            bool shouldSync;
            if (node is INetworkNodeWithSettings withSettings)
            {
                shouldSync = withSettings.SyncSettings.ShouldSyncFrom(this);
            }
            else
            {
                shouldSync = SyncSettings.ShouldSyncFromDefault(this);
            }

            if (shouldSync)
            {
                _anySyncObjectDirty = true;
                SyncVarSender.AddDirtyObject(this);
            }
        }

        internal bool StillDirty()
        {
            throw new NotImplementedException();
        }

        internal void ClearShouldSync()
        {
            throw new NotImplementedException();
        }

        internal (bool ownerWritten, bool observersWritten) OnSerializeAll(bool v, PooledNetworkWriter ownerWriter, PooledNetworkWriter observersWriter)
        {
            throw new NotImplementedException();
        }

        internal void ClearShouldSyncDirtyOnly()
        {
            throw new NotImplementedException();
        }

        private bool _anySyncObjectDirty;

        // todo update comment
        /// <summary>
        /// Collection that holds information about all RPC in this networkbehaviour (including derived classes)
        /// <para>Can be used to get RPC name from its index</para>
        /// <para>NOTE: Weaver uses this collection to add rpcs, If adding your own rpc do at your own risk</para>
        /// </summary>
        [NonSerialized]
        private RemoteCallCollection _remoteCallCollection;
        internal RemoteCallCollection RemoteCallCollection
        {
            get
            {
                if (_remoteCallCollection == null)
                {
                    // we should be save to lazy init
                    // we only need to register RPCs when we receive them
                    // when sending the index is baked in by weaver
                    _remoteCallCollection = new RemoteCallCollection();
                    _remoteCallCollection.RegisterAll(_nodes);
                }
                return _remoteCallCollection;
            }
        }
    }
}
