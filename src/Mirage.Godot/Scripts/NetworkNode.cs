using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Mirage.Collections;
using Mirage.Logging;
using Mirage.RemoteCalls;
using Mirage.Serialization;

namespace Mirage
{
    public interface INetworkNode
    {
        NetworkIdentity Identity { get; }
        int ComponentIndex { get; }
    }

    /// <summary>
    /// Add this to override default sync settings for Nodes
    /// </summary>
    public interface INetworkNodeWithSettings : INetworkNode
    {
        /// <summary>
        /// Sync settings for this NetworkBehaviour
        /// <para>Settings will be hidden in inspector unless Behaviour has SyncVar or SyncObjects</para>
        /// </summary>
        public SyncSettings SyncSettings { get; }
    }

    public interface INetworkNodeWithRpc : INetworkNode
    {
        int GetRpcCount();
        void RegisterRpc(RemoteCallCollection remoteCallCollection);
    }

    public interface INetworkNodeWithSyncVar : INetworkNode
    {
        void SerializeSyncVars(NetworkReader reader, bool initial);
        bool DeserializeSyncVars(NetworkReader reader, bool initial);
    }

    public class NetworkBehaviourSyncVars
    {
        private static readonly ILogger logger = LogFactory.GetLogger(typeof(NetworkBehaviourSyncVars));

        public readonly INetworkNode Node;
        public readonly NetworkIdentity Identity;
        public SyncSettings SyncSettings;

        private readonly List<ISyncObject> syncObjects = new List<ISyncObject>();

        private float _nextSyncTime;
        private bool _anySyncObjectDirty;
        private ulong _syncVarDirtyBits;
        private ulong _deserializeMask;

        // called by weaver
        public void InitSyncObject(INetworkNode node, ISyncObject syncObject)
        {
            syncObjects.Add(syncObject);
            syncObject.OnChange += SyncObject_OnChange;
        }
        private void SyncObject_OnChange()
        {
            // dont need to mark dirty if already dirty
            if (_anySyncObjectDirty)
                return;

            if (SyncSettings.ShouldSyncFrom(Identity))
            {
                _anySyncObjectDirty = true;
                Identity.SyncVarSender.AddDirtyObject(Identity);
            }
        }

        public NetworkBehaviourSyncVars(INetworkNode node, NetworkIdentity identity)
        {
            Node = node;
            Identity = identity;
            if (node is INetworkNodeWithSettings withSettings)
            {
                SyncSettings = withSettings.SyncSettings;
            }
            else
            {
                SyncSettings = SyncSettings.Default;
            }
        }

        protected internal bool SyncVarEqual<T>(T value, T fieldValue)
        {
            // newly initialized or changed value?
            return EqualityComparer<T>.Default.Equals(value, fieldValue);
        }

        /// <summary>
        /// Used to set the behaviour as dirty, so that a network update will be sent for the object.
        /// these are masks, not bit numbers, ie. 0x004 not 2
        /// </summary>
        /// <param name="bitMask">Bit mask to set.</param>
        public void SetDirtyBit(ulong bitMask)
        {
            if (logger.LogEnabled()) logger.Log($"Dirty bit set {bitMask} on {Identity}");
            _syncVarDirtyBits |= bitMask;

            if (SyncSettings.ShouldSyncFrom(Identity))
                Identity.SyncVarSender.AddDirtyObject(Identity);
        }


        /// <summary>
        /// Used to clear dirty bit.
        /// <para>Object may still be in dirty list, so will be checked in next update. but values in this mask will no longer be set until they are changed again</para>
        /// </summary>
        /// <param name="bitMask">Bit mask to set.</param>
        public void ClearDirtyBit(ulong bitMask)
        {
            _syncVarDirtyBits &= ~bitMask;
        }

        public void ClearDirtyBits()
        {
            _syncVarDirtyBits = 0L;

            // flush all unsynchronized changes in syncobjects
            for (var i = 0; i < syncObjects.Count; i++)
            {
                syncObjects[i].Flush();
            }
            _anySyncObjectDirty = false;
        }

        /// <summary>
        /// Clears dirty bits and sets the next sync time
        /// </summary>
        /// <param name="now"></param>
        public void ClearShouldSync(float now)
        {
            SyncSettings.UpdateTime(ref _nextSyncTime, now);
            ClearDirtyBits();
        }

        /// <summary>
        /// True if this behaviour is dirty and it is time to sync
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ShouldSync(float time)
        {
            return AnyDirtyBits() && TimeToSync(time);
        }

        /// <summary>
        /// If it is time to sync based on last sync and <see cref="SyncSettings"/>
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TimeToSync(float time)
        {
            return time >= _nextSyncTime;
        }

        /// <summary>
        /// Are any SyncVar or SyncObjects dirty
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AnyDirtyBits()
        {
            return SyncVarDirtyBits != 0L || AnySyncObjectDirty;
        }

        /// <summary>
        /// mask from the most recent DeserializeSyncVars
        /// </summary>
        protected internal void SetDeserializeMask(ulong dirtyBit, int offset)
        {
            _deserializeMask |= dirtyBit << offset;
        }

        internal ulong DirtyObjectBits()
        {
            ulong dirtyObjects = 0;
            for (var i = 0; i < syncObjects.Count; i++)
            {
                var syncObject = syncObjects[i];
                if (syncObject.IsDirty)
                {
                    dirtyObjects |= 1UL << i;
                }
            }
            return dirtyObjects;
        }
    }
}
