using Mirage;
using Mirage.Events;

namespace MirageGodot
{
    public class NetworkNodeEvents
    {
        internal AddLateEvent _onStartServer = new AddLateEvent();
        internal AddLateEvent _onStartClient = new AddLateEvent();
        internal AddLateEvent _onStartLocalPlayer = new AddLateEvent();
        internal AddLateEvent<bool> _onAuthorityChanged = new AddLateEvent<bool>();
        internal AddLateEvent<INetworkPlayer> _onOwnerChanged = new AddLateEvent<INetworkPlayer>();
        internal AddLateEvent _onStopClient = new AddLateEvent();
        internal AddLateEvent _onStopServer = new AddLateEvent();
        internal bool _clientStarted;
        internal bool _localPlayerStarted;
        internal bool _hadAuthority;

        /// <summary>
        /// This is invoked for NetworkBehaviour objects when they become active on the server.
        /// <para>This could be triggered by NetworkServer.Start() for objects in the scene, or by ServerObjectManager.Spawn() for objects you spawn at runtime.</para>
        /// <para>This will be called for objects on a "host" as well as for object on a dedicated server.</para>
        /// <para>OnStartServer is invoked before this object is added to collection of spawned objects</para>
        /// </summary>
        public IAddLateEvent OnStartServer => _onStartServer;

        /// <summary>
        /// Called on every NetworkBehaviour when it is activated on a client.
        /// <para>Objects on the host have this function called, as there is a local client on the host. The values of SyncVars on object are guaranteed to be initialized
        /// correctly with the latest state from the server when this function is called on the client.</para>
        /// </summary>
        public IAddLateEvent OnStartClient => _onStartClient;

        /// <summary>
        /// Called when the local player object has been set up.
        /// <para>This happens after OnStartClient(), as it is triggered by an ownership message from the server. This is an appropriate place to activate components or
        /// functionality that should only be active for the local player, such as cameras and input.</para>
        /// </summary>
        public IAddLateEvent OnStartLocalPlayer => _onStartLocalPlayer;

        /// <summary>
        /// This is invoked on behaviours that have authority given or removed, see <see cref="HasAuthority">NetworkIdentity.hasAuthority</see>
        /// <para>This is called after <see cref="OnStartServer">OnStartServer</see> and before <see cref="OnStartClient">OnStartClient.</see></para>
        /// <para>
        /// When <see cref="AssignClientAuthority"/> or <see cref="RemoveClientAuthority"/> is called on the server, this will be called on the client that owns the object.
        /// </para>
        /// <para>
        /// When an object is spawned with <see cref="ServerObjectManager.Spawn">ServerObjectManager.Spawn</see> with a NetworkConnection parameter included,
        /// this will be called on the client that owns the object.
        /// </para>
        /// <para>NOTE: this even is only called for client and host</para>
        /// </summary>
        public IAddLateEvent<bool> OnAuthorityChanged => _onAuthorityChanged;

        /// <summary>
        /// This is invoked on behaviours that have an owner assigned.
        /// <para>This even is only called on server</para>
        /// <para>See <see cref="OnAuthorityChanged"/> for more comments on owner and authority</para>
        /// </summary>
        public IAddLateEvent<INetworkPlayer> OnOwnerChanged => _onOwnerChanged;

        /// <summary>
        /// This is invoked on clients when the server has caused this object to be destroyed.
        /// <para>This can be used as a hook to invoke effects or do client specific cleanup.</para>
        /// </summary>
        ///<summary>Called on clients when the server destroys the GameObject.</summary>
        public IAddLateEvent OnStopClient => _onStopClient;

        /// <summary>
        /// This is called on the server when the object is unspawned
        /// </summary>
        /// <remarks>Can be used as hook to save player information</remarks>
        public IAddLateEvent OnStopServer => _onStopServer;
    }
}
