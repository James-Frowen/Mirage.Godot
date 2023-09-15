using System;
using System.Collections.Generic;
using Mirage;
using Mirage.Logging;
using Mirage.Serialization;
using MirageGodot.Messages;

namespace MirageGodot
{
    public class NetworkClient : MirageClient
    {
        private static readonly ILogger logger = LogFactory.GetLogger(typeof(NetworkClient));

        public NetworkManager Manager { get; }
        public NetworkWorld World { get; internal set; }

        private readonly Dictionary<int, SpawnHandler> _handlers = new Dictionary<int, SpawnHandler>();

        public NetworkClient(NetworkManager networkManager)
        {
            Manager = networkManager;
        }

        protected override void OnStarted()
        {
            MessageHandler.RegisterHandler<SpawnMessage>(OnSpawnMessage);
        }

        public void RegisterPrefab(NetworkNode[] nodes)
        {
            foreach (var node in nodes)
            {
                RegisterPrefab(node);
            }
        }

        private void RegisterPrefab(NetworkNode prefab)
        {
            var spawnHash = prefab.SpawnHash;
            ThrowIfZeroHash(spawnHash);

            if (logger.LogEnabled()) logger.Log($"Registering prefab '{prefab.Name}' as asset:{spawnHash:X}");
            _handlers[spawnHash] = new SpawnHandler(prefab);
        }
        private static void ThrowIfZeroHash(int prefabHash)
        {
            if (prefabHash == 0)
                throw new ArgumentException("prefabHash is zero", nameof(prefabHash));
        }

        private void OnSpawnMessage(SpawnMessage msg)
        {
            if (msg.SpawnHash == 0)
                throw new SpawnObjectException($"Empty prefabHash and sceneId for netId: {msg.NetId}");

            if (logger.LogEnabled()) logger.Log($"[ClientObjectManager] Spawn: {msg}");

            // was the object already spawned?
            var existing = World.TryGetIdentity(msg.NetId, out var node);

            if (!existing)
            {
                var spawnHash = msg.SpawnHash;
                if (!_handlers.TryGetValue(spawnHash, out var handler))
                    throw new SpawnObjectException($"No prefab for {msg.SpawnHash:X}. did you forget to add it to the ClientObjectManager?");

                node = SpawnPrefab(msg, handler);
            }

            AfterSpawn(msg, existing, node);
        }
        private NetworkNode SpawnPrefab(SpawnMessage msg, SpawnHandler handler)
        {
            var prefab = handler.Prefab;

            if (logger.LogEnabled()) logger.Log($"Instantiate Prefab for netid:{msg.NetId}, hash:{msg.SpawnHash:X}, prefab:{prefab.Name}");

            throw new NotImplementedException();
            // we need to set position and rotation here incase that their values are used from awake/onenable
            //var pos = msg.SpawnValues.Position ?? prefab.transform.position;
            //var rot = msg.SpawnValues.Rotation ?? prefab.transform.rotation;
            //return Instantiate(prefab, pos, rot);
        }
        private void AfterSpawn(SpawnMessage msg, bool alreadyExisted, NetworkNode identity)
        {
            // should never happen, Spawn methods above should throw instead
            Debug.Assert(identity != null);

            if (identity.NetId != 0 && identity.NetId != msg.NetId)
                logger.LogWarning($"Spawn Identity already had a netId but SpawnMessage has a differnet NetId. Current Id={identity.NetId}, SpawnMessag Id={msg.NetId}");

            ApplySpawnPayload(identity, msg);

            // add after applying payload, but only if it is new object
            if (!alreadyExisted)
                World.AddIdentity(msg.NetId, identity);
        }
        private void ApplySpawnPayload(NetworkNode identity, SpawnMessage msg)
        {
            // deserialize components if any payload
            // (Count is 0 if there were no components)
            if (msg.Payload.Count > 0)
            {
                using (var payloadReader = NetworkReaderPool.GetReader(msg.Payload, World))
                {
                    identity.OnDeserializeAll(payloadReader, true);
                }
            }

            identity.ClientSpawn(this, msg);
        }
    }
    public class SpawnHandler
    {
        public readonly NetworkNode Prefab;

        public SpawnHandler(NetworkNode prefab)
        {
            Prefab = prefab ?? throw new ArgumentNullException(nameof(prefab));
        }
    }

    /// <summary>
    /// Exception thrown when spawning fails
    /// </summary>
    [Serializable]
    public class SpawnObjectException : Exception
    {
        public SpawnObjectException(string message) : base(message)
        {
        }
    }
}
