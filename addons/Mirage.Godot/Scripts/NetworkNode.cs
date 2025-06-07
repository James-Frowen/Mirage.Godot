using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;
using Mirage.Collections;
using Mirage.Logging;
using Mirage.RemoteCalls;
using Mirage.Serialization;

namespace Mirage
{
    public interface INetworkNode
    {
        StringName Name { get; }
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
        List<ISyncObject> SyncObjects { get; }
        bool SerializeSyncVars(NetworkWriter writer, bool initial);
        void DeserializeSyncVars(NetworkReader reader, bool initial);
    }

    public class NetworkNodeSyncVars
    {
        private static readonly ILogger logger = LogFactory.GetLogger(typeof(NetworkNodeSyncVars));

        public readonly INetworkNodeWithSyncVar Node;
        public readonly NetworkIdentity Identity;
        public SyncSettings SyncSettings;

        private float _nextSyncTime;
        internal bool _anySyncObjectDirty;
        internal ulong _syncVarDirtyBits;
        internal ulong _deserializeMask;
        private bool _syncObjectsInitialized;
        private ulong _syncVarHookGuard;
        private List<ISyncObject> syncObjects => Node.SyncObjects;

        public NetworkNodeSyncVars(INetworkNodeWithSyncVar node, NetworkIdentity identity)
        {
            Node = node;
            if (node is NetworkBehaviour behaviour)
            {
                behaviour.NetworkNodeSyncVars = this;
            }

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

        protected internal bool GetSyncVarHookGuard(ulong dirtyBit)
        {
            return (_syncVarHookGuard & dirtyBit) != 0UL;
        }

        protected internal void SetSyncVarHookGuard(ulong dirtyBit, bool value)
        {
            if (value)
                _syncVarHookGuard |= dirtyBit;
            else
                _syncVarHookGuard &= ~dirtyBit;
        }

        /// <summary>
        /// calls SetNetworkBehaviour on each SyncObject, but only once
        /// </summary>
        internal void InitializeSyncObjects()
        {
            if (_syncObjectsInitialized)
                return;

            _syncObjectsInitialized = true;

            // find all the ISyncObjects in this behaviour
            foreach (var syncObject in syncObjects)
            {
                syncObject.OnChange += SyncObject_OnChange;
                syncObject.SetNetworkBehaviour(Node);
            }
        }

        private void SyncObject_OnChange()
        {
            if (SyncSettings.ShouldSyncFrom(Identity, true))
            {
                _anySyncObjectDirty = true;
                Identity.SyncVarSender.AddDirtyObject(Identity);
            }
        }

        /// <summary>
        /// Call this after updating SyncSettings to update all SyncObjects
        /// <para>
        /// This only needs to be called manually if updating syncSettings at runtime.
        /// Mirage will automatically call this after serializing or deserializing with initialState
        /// </para>
        /// </summary>
        public void UpdateSyncObjectShouldSync()
        {
            var shouldSync = SyncSettings.ShouldSyncFrom(Identity, true);

            if (logger.LogEnabled()) logger.Log($"Settings SyncObject sync on to {shouldSync} for {Node?.Name}");
            for (var i = 0; i < syncObjects.Count; i++)
            {
                syncObjects[i].SetShouldSyncFrom(shouldSync);
            }
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

            if (SyncSettings.ShouldSyncFrom(Identity, false))
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
            return _syncVarDirtyBits != 0L || _anySyncObjectDirty;
        }

        /// <summary>
        /// Virtual function to override to send custom serialization data. The corresponding function to send serialization data is OnDeserialize().
        /// </summary>
        /// <remarks>
        /// <para>The initialState flag is useful to differentiate between the first time an object is serialized and when incremental updates can be sent. The first time an object is sent to a client, it must include a full state snapshot, but subsequent updates can save on bandwidth by including only incremental changes. Note that SyncVar hook functions are not called when initialState is true, only for incremental updates.</para>
        /// <para>If a class has SyncVars, then an implementation of this function and OnDeserialize() are added automatically to the class. So a class that has SyncVars cannot also have custom serialization functions.</para>
        /// <para>The OnSerialize function should return true to indicate that an update should be sent. If it returns true, then the dirty bits for that script are set to zero, if it returns false then the dirty bits are not changed. This allows multiple changes to a script to be accumulated over time and sent when the system is ready, instead of every frame.</para>
        /// </remarks>
        /// <param name="writer">Writer to use to write to the stream.</param>
        /// <param name="initialState">If this is being called to send initial state.</param>
        /// <returns>True if data was written.</returns>
        public virtual bool OnSerialize(NetworkWriter writer, bool initialState)
        {
            var objectWritten = SerializeObjects(writer, initialState);

            var syncVarWritten = Node.SerializeSyncVars(writer, initialState);

            return objectWritten || syncVarWritten;
        }

        /// <summary>
        /// Virtual function to override to receive custom serialization data. The corresponding function to send serialization data is OnSerialize().
        /// </summary>
        /// <param name="reader">Reader to read from the stream.</param>
        /// <param name="initialState">True if being sent initial state.</param>
        public virtual void OnDeserialize(NetworkReader reader, bool initialState)
        {
            DeserializeObjects(reader, initialState);

            _deserializeMask = 0;
            Node.DeserializeSyncVars(reader, initialState);
        }

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

        public bool SerializeObjects(NetworkWriter writer, bool initialState)
        {
            if (syncObjects.Count == 0)
                return false;

            if (initialState)
            {
                var written = SerializeObjectsAll(writer);
                // after initial we need to set up objects for syncDirection
                UpdateSyncObjectShouldSync();
                return written;
            }
            else
            {
                return SerializeObjectsDelta(writer);
            }
        }

        public bool SerializeObjectsAll(NetworkWriter writer)
        {
            var dirty = false;
            for (var i = 0; i < syncObjects.Count; i++)
            {
                var syncObject = syncObjects[i];
                syncObject.OnSerializeAll(writer);
                dirty = true;
            }
            return dirty;
        }

        public bool SerializeObjectsDelta(NetworkWriter writer)
        {
            var dirty = false;
            // write the mask
            writer.WritePackedUInt64(DirtyObjectBits());
            // serializable objects, such as synclists
            for (var i = 0; i < syncObjects.Count; i++)
            {
                var syncObject = syncObjects[i];
                if (syncObject.IsDirty)
                {
                    syncObject.OnSerializeDelta(writer);
                    dirty = true;
                }
            }
            return dirty;
        }

        internal void DeserializeObjects(NetworkReader reader, bool initialState)
        {
            if (syncObjects.Count == 0)
                return;

            if (initialState)
            {
                DeSerializeObjectsAll(reader);
                UpdateSyncObjectShouldSync();
            }
            else
            {
                DeSerializeObjectsDelta(reader);
            }
        }

        internal void DeSerializeObjectsAll(NetworkReader reader)
        {
            for (var i = 0; i < syncObjects.Count; i++)
            {
                var syncObject = syncObjects[i];
                syncObject.OnDeserializeAll(reader);
            }
        }

        internal void DeSerializeObjectsDelta(NetworkReader reader)
        {
            var dirty = reader.ReadPackedUInt64();
            for (var i = 0; i < syncObjects.Count; i++)
            {
                var syncObject = syncObjects[i];
                if ((dirty & (1UL << i)) != 0)
                {
                    syncObject.OnDeserializeDelta(reader);
                }
            }
        }

        internal void ResetSyncObjects()
        {
            foreach (var syncObject in syncObjects)
            {
                syncObject.Reset();
            }
        }
    }
}
