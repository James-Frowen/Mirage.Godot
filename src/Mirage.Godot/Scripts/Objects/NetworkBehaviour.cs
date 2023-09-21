using Godot;
using Mirage.Logging;
using Mirage.RemoteCalls;
using Mirage.Serialization;

namespace Mirage
{
    /// <summary>
    /// Default NetworkBehaviour that can be used to quickly add RPC and Sync var to nodes
    /// <para>
    /// If you dont need RPC or SyncVar use <see cref="INetworkNode"/> instead
    /// </para>
    /// </summary>
    public abstract partial class NetworkBehaviour : Node, INetworkNode, INetworkNodeWithRpc, INetworkNodeWithSettings, INetworkNodeWithSyncVar
    {
        private static readonly ILogger logger = LogFactory.GetLogger(typeof(NetworkBehaviour));

        public const int COMPONENT_INDEX_NOT_FOUND = -1;

        [ExportGroup("Sync Settings")]
        [Export] public SyncFrom From;
        [Export] public SyncTo To;
        [Export] public SyncTiming Timing;
        [Export] public float Interval;

        private NetworkIdentity _identity;
        private int? _componentIndex;
        public NetworkNodeSyncVars NetworkNodeSyncVars;

        public NetworkIdentity Identity
        {
            get
            {
                if (_identity == null)
                {
                    _identity = this.TryGetNetworkIdentity();
                }
                return _identity;
            }
        }

        /// <summary>
        /// Returns the index of the component on this object
        /// </summary>
        public int ComponentIndex
        {
            get
            {
                if (_componentIndex.HasValue)
                    return _componentIndex.Value;

                // note: FindIndex causes allocations, we search manually instead
                for (var i = 0; i < Identity.NetworkBehaviours.Length; i++)
                {
                    var component = Identity.NetworkBehaviours[i];
                    if (component == this)
                    {
                        _componentIndex = i;
                        return i;
                    }
                }

                // this should never happen
                logger.LogError("Could not find component in GameObject. You should not add/remove components in networked objects dynamically");

                return COMPONENT_INDEX_NOT_FOUND;
            }
        }

        public SyncSettings SyncSettings => new SyncSettings(From, To, Timing, Interval);

        public int GetRpcCount()
        {
            // genereated by weaver
            return 0;
        }

        public void RegisterRpc(RemoteCallCollection remoteCallCollection)
        {
            // genereated by weaver
        }

        public void SerializeSyncVars(NetworkReader reader, bool initial)
        {
            // genereated by weaver
        }

        public bool DeserializeSyncVars(NetworkReader reader, bool initial)
        {
            // genereated by weaver
            return false;
        }
    }
}
