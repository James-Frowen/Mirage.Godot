using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Mirage.Logging;
using Mirage.Messages;
using Mirage.RemoteCalls;
using Mirage.Serialization;

namespace Mirage
{
    /// <summary>
    /// The ServerObjectManager.
    /// </summary>
    /// <remarks>
    /// <para>The set of networked objects that have been spawned is managed by ServerObjectManager.
    /// Objects are spawned with ServerObjectManager.Spawn() which adds them to this set, and makes them be created on clients.
    /// Spawned objects are removed automatically when they are destroyed, or than they can be removed from the spawned set by calling ServerObjectManager.UnSpawn() - this does not destroy the object.</para>
    /// </remarks>
    [GlobalClass]
    public partial class ServerObjectManager : Node
    {
        private static readonly ILogger logger = LogFactory.GetLogger(typeof(ServerObjectManager));
        /// <summary>
        /// HashSet for NetworkIdentity that can be re-used without allocation
        /// </summary>
        private static HashSet<NetworkIdentity> _setCache = new HashSet<NetworkIdentity>();

        internal RpcHandler _rpcHandler;

        private NetworkServer _server;
        public NetworkServer Server => _server;

        [ExportGroup("Authentication")]
        [Export(hintString: "Will only send spawn message to Players who are Authenticated. Checks the Player.IsAuthenticated property")]
        public bool OnlySpawnOnAuthenticated;

        public INetIdGenerator NetIdGenerator;
        private uint _nextNetworkId = 1;

        private uint GetNextNetworkId() => NetIdGenerator?.GenerateNetId() ?? checked(_nextNetworkId++);

        public INetworkVisibility DefaultVisibility { get; private set; }

        internal void ServerStarted(NetworkServer server)
        {
            if (_server != null && _server != server)
                throw new InvalidOperationException($"ServerObjectManager already in use by another NetworkServer, current:{_server}, new:{server}");

            _server = server;
            _server.Stopped.AddListener(OnServerStopped);

            DefaultVisibility = new AlwaysVisible(this);

            _rpcHandler = new RpcHandler(_server.MessageHandler, _server.World, RpcInvokeType.ServerRpc);

            SpawnOrActivate();
        }

        private void OnServerStopped()
        {
            // todo dont send messages on server stop, only reset NI
            foreach (var obj in _server.World.SpawnedIdentities.Reverse())
            {
                // Unspawn all, but only destroy non-scene objects on server
                DestroyObject(obj, !obj.IsSceneObject);
            }

            _server.World.ClearSpawnedObjects();
            // reset so ids stay small in each session
            _nextNetworkId = 1;

            // clear server after stopping
            _server.Stopped.RemoveListener(OnServerStopped);
            _server = null;
        }

        internal void SpawnOrActivate()
        {
            if (_server == null || !_server.Active)
            {
                logger.LogWarning("SpawnOrActivate called when server was not active");
                return;
            }

            SpawnSceneObjects();

            // host mode?
            if (_server.LocalClientActive)
            {
                StartHostClientObjects();
            }
        }

        /// <summary>
        /// Loops spawned collection for NetworkIdentities that are not IsClient and calls StartClient().
        /// </summary>
        // todo can this function be removed? do we only need to run it when host connects?
        private void StartHostClientObjects()
        {
            foreach (var identity in _server.World.SpawnedIdentities)
            {
                if (!identity.IsClient)
                {
                    if (logger.LogEnabled()) logger.Log("ActivateHostScene " + identity.NetId + " " + identity);

                    identity.StartClient();
                }
            }
        }

        /// <summary>
        /// This replaces the player object for a connection with a different player object. The old player object is not destroyed.
        /// <para>If a connection already has a player object, this can be used to replace that object with a different player object. This does NOT change the ready state of the connection, so it can safely be used while changing scenes.</para>
        /// </summary>
        /// <param name="player">Connection which is adding the player.</param>
        /// <param name="character">Player object spawned for the player.</param>
        /// <param name="prefabHash"></param>
        /// <param name="keepAuthority">Does the previous player remain attached to this connection?</param>
        /// <returns></returns>
        public void ReplaceCharacter(NetworkPlayer player, NetworkIdentity character, int prefabHash, bool keepAuthority = false)
        {
            character.PrefabHash = prefabHash;
            ReplaceCharacter(player, character, keepAuthority);
        }

        /// <summary>
        /// This replaces the player object for a connection with a different player object. The old player object is not destroyed.
        /// <para>If a connection already has a player object, this can be used to replace that object with a different player object. This does NOT change the ready state of the connection, so it can safely be used while changing scenes.</para>
        /// </summary>
        /// <param name="player">Connection which is adding the player.</param>
        /// <param name="identity">Player object spawned for the player.</param>
        /// <param name="keepAuthority">Does the previous player remain attached to this connection?</param>
        /// <returns></returns>
        public void ReplaceCharacter(NetworkPlayer player, NetworkIdentity identity, bool keepAuthority = false)
        {
            if (identity.Owner != null && identity.Owner != player)
            {
                throw new ArgumentException($"Cannot replace player for connection. New player is already owned by a different connection {identity}");
            }
            if (!player.HasCharacter)
            {
                throw new InvalidOperationException($"ReplaceCharacter can only be called if Player already has a charater");
            }

            //NOTE: there can be an existing player
            logger.Log("NetworkServer ReplacePlayer");

            var previousCharacter = player.Identity;

            player.Identity = identity;

            // Set the connection on the NetworkIdentity on the server, NetworkIdentity.SetLocalPlayer is not called on the server (it is on clients)
            identity.SetOwner(player);

            // special case,  we are in host mode,  set hasAuthority to true so that all overrides see it
            if (_server.LocalPlayer != null && player == _server.LocalPlayer)
            {
                identity.HasAuthority = true;
                _server.LocalClient.Player.Identity = identity;
            }

            // add connection to observers AFTER the playerController was set.
            // by definition, there is nothing to observe if there is no player
            // controller.
            //
            // IMPORTANT: do this in AddCharacter & ReplaceCharacter!
            SpawnVisibleObjects(player, identity);

            if (logger.LogEnabled()) logger.Log($"Replacing playerGameObject object netId: {identity.NetId} asset ID {identity.PrefabHash:X}");

            Respawn(identity);

            if (!keepAuthority)
                previousCharacter.RemoveClientAuthority();
        }

        /// <summary>
        /// <para>When <see cref="AddCharacterMessage"/> is received from a player, the server calls this to associate the character GameObject with the NetworkPlayer.</para>
        /// <para>When a character is added for a player the object is automatically spawned, so you do not need to call ServerObjectManager.Spawn for that object.</para>
        /// <para>This function is used for adding a character, not replacing. If there is already a character then use <see cref="ReplaceCharacter"/> instead.</para>
        /// </summary>
        /// <param name="player">the Player to add the character to</param>
        /// <param name="character">The Network Object to add to the Player. Can be spawned or unspawned. Calling this method will respawn it.</param>
        /// <param name="prefabHash">New prefab hash to give to the player, used for dynamically creating objects at runtime.</param>
        /// <exception cref="ArgumentException">throw when the player already has a character</exception>
        public void AddCharacter(NetworkPlayer player, NetworkIdentity character, int prefabHash)
        {
            character.PrefabHash = prefabHash;
            AddCharacter(player, character);
        }

        /// <summary>
        /// <para>When <see cref="AddCharacterMessage"/> is received from a player, the server calls this to associate the character GameObject with the NetworkPlayer.</para>
        /// <para>When a character is added for a player the object is automatically spawned, so you do not need to call ServerObjectManager.Spawn for that object.</para>
        /// <para>This function is used for adding a character, not replacing. If there is already a character then use <see cref="ReplaceCharacter"/> instead.</para>
        /// </summary>
        /// <param name="player">the Player to add the character to</param>
        /// <param name="character">The Network Object to add to the Player. Can be spawned or unspawned. Calling this method will respawn it.</param>
        /// <exception cref="ArgumentException">throw when the player already has a character</exception>
        public void AddCharacter(NetworkPlayer player, NetworkIdentity identity)
        {
            // cannot have an existing player object while trying to Add another.
            if (player.HasCharacter)
            {
                throw new ArgumentException("AddCharacter can only be called if the player does not already have a character");
            }

            // make sure we have a controller before we call SetClientReady
            // because the observers will be rebuilt only if we have a controller
            player.Identity = identity;

            identity.SetServerValues(_server, this);

            // Set the connection on the NetworkIdentity on the server, NetworkIdentity.SetLocalPlayer is not called on the server (it is on clients)
            identity.SetOwner(player);

            // special case, we are in host mode, set hasAuthority to true so that all overrides see it
            if (_server.LocalPlayer != null && player == _server.LocalPlayer)
            {
                identity.HasAuthority = true;
                _server.LocalClient.Player.Identity = identity;
            }

            // spawn any new visible scene objects
            SpawnVisibleObjects(player, identity);

            if (logger.LogEnabled()) logger.Log($"Adding new playerGameObject object netId: {identity.NetId} asset ID {identity.PrefabHash:X}");

            Respawn(identity);
        }

        private void Respawn(NetworkIdentity identity)
        {
            if (!identity.IsSpawned)
            {
                // If the object has not been spawned, then do a full spawn and update observers
                Spawn(identity, identity.Owner);
            }
            else
            {
                // otherwise just replace his data
                SendSpawnMessage(identity, identity.Owner);
            }
        }

        /// <summary>
        /// Sends spawn message to player if it is not loading a scene
        /// </summary>
        /// <param name="identity"></param>
        /// <param name="player"></param>
        internal void ShowToPlayer(NetworkIdentity identity, NetworkPlayer player)
        {
            var visiblity = identity.Visibility;
            if (visiblity is NetworkVisibility networkVisibility)
                networkVisibility.InvokeVisibilityChanged(player, true);

            // dont send if loading scene
            if (player.SceneIsReady)
                SendSpawnMessage(identity, player);
        }

        internal void HideToPlayer(NetworkIdentity identity, NetworkPlayer player)
        {
            var visiblity = identity.Visibility;
            if (visiblity is NetworkVisibility networkVisibility)
                networkVisibility.InvokeVisibilityChanged(player, false);

            player.Send(new ObjectHideMessage { NetId = identity.NetId });
        }

        /// <summary>
        /// Removes the character from a player, with the option to keep the player as the owner of the object
        /// </summary>
        /// <param name="player"></param>
        /// <param name="keepAuthority"></param>
        /// <exception cref="InvalidOperationException">Throws when player does not have a character</exception>
        public void RemoveCharacter(NetworkPlayer player, bool keepAuthority = false)
        {
            ThrowIfNoCharacter(player);

            var identity = player.Identity;
            player.Identity = null;
            if (!keepAuthority)
            {
                logger.Assert(identity.Owner == player, "Owner should be player that is being removed");
                identity.SetOwner(null);
            }

            player.Send(new RemoveCharacterMessage { KeepAuthority = keepAuthority });
        }

        /// <summary>
        /// Removes and destroys the character from a player
        /// </summary>
        /// <param name="player"></param>
        /// <param name="destroyServerObject"></param>
        /// <exception cref="InvalidOperationException">Throws when player does not have a character</exception>
        public void DestroyCharacter(NetworkPlayer player, bool destroyServerObject = true)
        {
            ThrowIfNoCharacter(player);

            Destroy(player.Identity, destroyServerObject);
            player.Identity = null;
        }

        /// <exception cref="InvalidOperationException">Throws when player does not have a character</exception>
        private static void ThrowIfNoCharacter(NetworkPlayer player)
        {
            if (!player.HasCharacter)
            {
                throw new InvalidOperationException("Player did not have a character");
            }
        }

        /// <summary>
        /// Assigns <paramref name="prefabHash"/> to the <paramref name="identity"/> and then spawns it with <paramref name="owner"/>
        /// <para>
        ///     <see cref="NetworkIdentity.PrefabHash"/> can only be set on an identity if the current value is Empty
        /// </para>
        /// <para>
        ///     This method is useful if you are creating network objects at runtime and both server and client know what <see cref="Guid"/> to set on an object
        /// </para>
        /// </summary>
        /// <param name="obj">The object to spawn.</param>
        /// <param name="prefabHash">The prefabHash of the object to spawn. Used for custom spawn handlers.</param>
        /// <param name="owner">The connection that has authority over the object</param>
        public void Spawn(NetworkIdentity identity, int prefabHash, NetworkPlayer owner = null)
        {
            identity.PrefabHash = prefabHash;
            Spawn(identity, owner);
        }

        /// <summary>
        /// Spawns the <paramref name="identity"/> and keeping owner as <see cref="NetworkIdentity.Owner"/>
        /// </summary>
        public void Spawn(NetworkIdentity identity, NetworkPlayer owner)
        {
            identity.SetOwner(owner);
            Spawn(identity);
        }

        /// <summary>
        /// Spawns the <paramref name="identity"/> and assigns <paramref name="owner"/> to be it's owner
        /// </summary>
        public void Spawn(NetworkIdentity identity)
        {
            if (_server == null || !_server.Active)
            {
                throw new InvalidOperationException("NetworkServer is not active. Cannot spawn objects without an active server.");
            }

            identity.SetServerValues(_server, this);

            // special case to make sure hasAuthority is set
            // on start server in host mode
            // note: we need != null here, HasAuthority should never be null on server
            //       this is so that logic in syncvar sender works correctly
            if (_server.LocalPlayer != null && identity.Owner == _server.LocalPlayer)
                identity.HasAuthority = true;

            if (!identity.IsSpawned)
            {
                // the object has not been spawned yet
                identity.NetId = GetNextNetworkId();
                identity.StartServer();
                _server.World.AddIdentity(identity.NetId, identity);
            }

            if (logger.LogEnabled()) logger.Log($"SpawnObject NetId:{identity.NetId} PrefabHash:{identity.PrefabHash:X}");

            identity.RebuildObservers(true);
        }

        internal void SendSpawnMessage(NetworkIdentity identity, NetworkPlayer player)
        {
            logger.Assert(!OnlySpawnOnAuthenticated || player.IsAuthenticated || identity.Visibility != DefaultVisibility,
                "SendSpawnMessage should only be called if OnlySpawnOnAuthenticated is false, player is authenticated, or there is custom visibility");
            if (logger.LogEnabled()) logger.Log($"Server SendSpawnMessage: name={identity.Name} PrefabHash={identity.PrefabHash:X} SceneHash={identity.SceneHash:X} netId={identity.NetId}");

            if (identity.PrefabHash == 0)
                throw new SpawnObjectException($"{identity} had no PrefabHash. Without one it will be unable to spawn on client");

            // one writer for owner, one for observers
            using (PooledNetworkWriter ownerWriter = NetworkWriterPool.GetWriter(), observersWriter = NetworkWriterPool.GetWriter())
            {
                var isOwner = identity.Owner == player;

                var payload = CreateSpawnMessagePayload(isOwner, identity, ownerWriter, observersWriter);

                var msg = new SpawnMessage
                {
                    NetId = identity.NetId,
                    IsMainCharacter = player.Identity == identity,
                    IsOwner = isOwner,
                    PrefabHash = identity.PrefabHash,
                    SceneHash = identity.SceneHash,
                    Payload = payload,
                };

                msg.SpawnValues = CreateSpawnValues(identity);

                player.Send(msg);
            }
        }

        private SpawnValues CreateSpawnValues(NetworkIdentity identity)
        {
            var settings = identity.SpawnSettings;
            SpawnValues values = default;

            if (settings.SendName) values.Name = identity.Name;

            var root = identity.Root;
            if (root is Node3D root3d)
            {
                // values in msg are nullable, so by default they are null
                // only set those values if the identity's settings say to send them
                if (settings.SendPosition) values.Position = root3d.Position;
                if (settings.SendRotation) values.Rotation = root3d.Quaternion;
                switch (settings.SendActive)
                {
                    case SyncActiveOption.SyncWithServer:
                        values.SelfActive = root3d.Visible;
                        break;
                    case SyncActiveOption.ForceEnable:
                        values.SelfActive = true;
                        break;
                }

            }
            else if (root is Node2D root2d)
            {
                // values in msg are nullable, so by default they are null
                // only set those values if the identity's settings say to send them
                if (settings.SendPosition) values.Position2d = root2d.Position;
                if (settings.SendRotation) values.Rotation2d = root2d.Rotation;
                switch (settings.SendActive)
                {
                    case SyncActiveOption.SyncWithServer:
                        values.SelfActive = root2d.Visible;
                        break;
                    case SyncActiveOption.ForceEnable:
                        values.SelfActive = true;
                        break;
                }
            }

            return values;
        }

        internal void SendRemoveAuthorityMessage(NetworkIdentity identity, NetworkPlayer previousOwner)
        {
            if (logger.LogEnabled()) logger.Log($"Server SendRemoveAuthorityMessage: name={identity.Name} PrefabHash={identity.PrefabHash:X} SceneHash={identity.SceneHash:X} netId={identity.NetId}");

            previousOwner.Send(new RemoveAuthorityMessage
            {
                NetId = identity.NetId,
            });
        }

        private static ArraySegment<byte> CreateSpawnMessagePayload(bool isOwner, NetworkIdentity identity, PooledNetworkWriter ownerWriter, PooledNetworkWriter observersWriter)
        {
            // Only call OnSerializeAllSafely if there are NetworkBehaviours
            if (identity.NetworkBehaviours.Length == 0)
            {
                return default;
            }

            // serialize all components with initialState = true
            // (can be null if has none)
            identity.OnSerializeAll(true, ownerWriter, observersWriter);

            // use owner segment if 'conn' owns this identity, otherwise
            // use observers segment
            var payload = isOwner ?
                ownerWriter.ToArraySegment() :
                observersWriter.ToArraySegment();

            return payload;
        }

        /// <summary>
        /// Destroys this object and corresponding objects on all clients.
        /// <param name="identity">Game object to destroy.</param>
        /// <param name="destroyServerObject">Sets if server object will also be destroyed</param>
        /// </summary>
        public void Destroy(NetworkIdentity identity, bool destroyServerObject = true)
        {
            if (identity == null)
            {
                logger.Log("NetworkServer DestroyObject is null");
                return;
            }

            DestroyObject(identity, destroyServerObject);
        }

        private void DestroyObject(NetworkIdentity identity, bool destroyServerObject)
        {
            if (logger.LogEnabled()) logger.Log("DestroyObject instance:" + identity.NetId);

            _server.World.RemoveIdentity(identity);
            identity.Owner?.RemoveOwnedObject(identity);

            identity.SendToRemoteObservers(new ObjectDestroyMessage { NetId = identity.NetId });

            identity.ClearObservers();
            if (_server.LocalClientActive)
            {
                // see ClientObjectManager.UnSpawn for comments
                if (identity.HasAuthority)
                    identity.CallStopAuthority();

                identity.StopClient();
            }

            identity.StopServer();

            identity.NetworkReset();
            // when unspawning, dont destroy the server's object
            if (destroyServerObject)
            {
                identity.Root.QueueFree();
            }
        }

        /// <summary>
        /// This causes NetworkIdentity objects in a scene to be spawned on a server.
        /// <para>
        ///     Calling SpawnObjects() causes all scene objects to be spawned.
        ///     It is like calling Spawn() for each of them.
        /// </para>
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when server is not active</exception>
        public void SpawnSceneObjects()
        {
            // only if server active
            if (_server == null || !_server.Active)
                throw new InvalidOperationException("Server was not active");

            var comparer = new NetworkIdentityComparer();
            var identities = this.GetAllNetworkIdentities().OrderBy(x => x, comparer);
            foreach (var identity in identities)
            {
                if (identity.IsSceneObject)
                {
                    if (logger.LogEnabled()) logger.Log($"SpawnObjects PrefabHash={identity.PrefabHash:X} SceneHash={identity.SceneHash:X} name:{identity.Name}");

                    Spawn(identity);
                }
            }
        }

        /// <summary>
        /// Sends spawn message for scene objects and other visible objects to the given player if it has a character
        /// <para>
        /// If there is a <see cref="Mirage.NetworkSceneManager"/> then this will be called after the client finishes loading the scene and sends <see cref="SceneReadyMessage"/>
        /// </para>
        /// </summary>
        /// <param name="player">The player to spawn objects for</param>
        // note: can't use optional param here because we need just NetworkPlayer version for event
        public void SpawnVisibleObjects(NetworkPlayer player)
        {
            SpawnVisibleObjects(player, false, (HashSet<NetworkIdentity>)null);
        }

        /// <summary>
        /// Sends spawn message for scene objects and other visible objects to the given player if it has a character
        /// </summary>
        /// <param name="player">The player to spawn objects for</param>
        /// <param name="ignoreHasCharacter">If true will spawn visibile objects even if player does not have a spawned character yet</param>
        public void SpawnVisibleObjects(NetworkPlayer player, bool ignoreHasCharacter)
        {
            SpawnVisibleObjects(player, ignoreHasCharacter, (HashSet<NetworkIdentity>)null);
        }

        /// <summary>
        /// Sends spawn message for scene objects and other visible objects to the given player if it has a character
        /// </summary>
        /// <param name="player">The player to spawn objects for</param>
        /// <param name="ignoreHasCharacter">If true will spawn visibile objects even if player does not have a spawned character yet</param>
        public void SpawnVisibleObjects(NetworkPlayer player, NetworkIdentity skip)
        {
            SpawnVisibleObjects(player, false, skip);
        }

        /// <summary>
        /// Sends spawn message for scene objects and other visible objects to the given player if it has a character
        /// </summary>
        /// <param name="player">The player to spawn objects for</param>
        /// <param name="ignoreHasCharacter">If true will spawn visibile objects even if player does not have a spawned character yet</param>
        /// <param name="skip">NetworkIdentity to skip when spawning. Can be null</param>
        public void SpawnVisibleObjects(NetworkPlayer player, bool ignoreHasCharacter, NetworkIdentity skip)
        {
            _setCache.Clear();
            _setCache.Add(skip);
            SpawnVisibleObjects(player, ignoreHasCharacter, _setCache);
        }

        /// <summary>
        /// Sends spawn message for scene objects and other visible objects to the given player if it has a character
        /// </summary>
        /// <param name="player">The player to spawn objects for</param>
        /// <param name="ignoreHasCharacter">If true will spawn visibile objects even if player does not have a spawned character yet</param>
        /// <param name="skip">NetworkIdentity to skip when spawning. Can be null</param>
        public void SpawnVisibleObjects(NetworkPlayer player, bool ignoreHasCharacter, HashSet<NetworkIdentity> skip)
        {
            // todo Call player.RemoveAllVisibleObjects() first so that it will send spawn message for objects destroyed in scene change

            if (!ignoreHasCharacter && !player.HasCharacter)
            {
                if (logger.LogEnabled()) logger.Log($"SpawnVisibleObjects: not spawning objects for {player} because it does not have a character");
                return;
            }

            if (!player.SceneIsReady)
            {
                // client needs to finish loading scene before we can spawn objects
                // otherwise it would not find scene objects.
                if (logger.LogEnabled()) logger.Log($"SpawnVisibleObjects: not spawning objects for {player} because scene not ready");
                return;
            }

            if (logger.LogEnabled()) logger.Log($"SpawnVisibleObjects: Checking Observers on {_server.World.SpawnedIdentities.Count} objects for player: {player}");

            // add connection to each nearby NetworkIdentity's observers, which
            // internally sends a spawn message for each one to the connection.
            foreach (var identity in _server.World.SpawnedIdentities)
            {
                // allow for skips so that addChatacter doesn't send 2 spawn message for existing object
                if (skip != null && skip.Contains(identity))
                    continue;

                if (logger.LogEnabled()) logger.Log($"Checking Observers on server objects name='{identity.Name}' netId={identity.NetId} PrefabHash={identity.PrefabHash:X} SceneHash={identity.SceneHash:X}");

                var visible = identity.OnCheckObserver(player);
                if (visible)
                {
                    identity.AddObserver(player);
                }
            }
        }

        private sealed class NetworkIdentityComparer : IComparer<NetworkIdentity>
        {
            public int Compare(NetworkIdentity x, NetworkIdentity y)
            {
                return x.NetId.CompareTo(y.NetId);
            }
        }
    }
}
