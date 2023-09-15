using Mirage.Events;
using Mirage.Logging;
using Mirage.SocketLayer;
using System;

namespace Mirage
{
    public enum ConnectState
    {
        Disconnected,
        Connecting,
        Connected,
    }

    public sealed class MirageClient : MiragePeer
    {
        private static readonly ILogger logger = LogFactory.GetLogger(typeof(MirageClient));

        private readonly AddLateEvent<INetworkPlayer> _connected = new AddLateEvent<INetworkPlayer>();
        public IAddLateEvent<INetworkPlayer> Connected => _connected;
        public event Action<ClientStoppedReason> Disconnected;

        public INetworkPlayer Player { get; private set; }

        public bool IsConnected { get; private set; }

        /// <summary>
        /// Connect client to a NetworkServer instance.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        public void Connect(ISocket socket, int maxPacketSize, IEndPoint endPoint)
        {
            if (logger.LogEnabled()) logger.Log($"Client connecting to endpoint: {endPoint}");

            DataHandler dataHandler = null;
            Start(socket, maxPacketSize, (mh) =>
            {
                dataHandler = new DataHandler(mh);
                return dataHandler;
            });

            var connection = Peer.Connect(endPoint);

            // setup all the handlers
            Player = new NetworkPlayer(connection, false);
            dataHandler.SetConnection(connection, Player);

            MessageHandler.RegisterHandler<NetworkPongMessage>(Time.OnClientPong);

            _started.Invoke();
        }
        protected override void AddPeerEvents()
        {
            Peer.OnConnected += Peer_OnConnected;
            Peer.OnConnectionFailed += Peer_OnConnectionFailed;
            Peer.OnDisconnected += Peer_OnDisconnected;
        }
        protected override void RemovePeerEvents()
        {
            Peer.OnConnected -= Peer_OnConnected;
            Peer.OnConnectionFailed -= Peer_OnConnectionFailed;
            Peer.OnDisconnected -= Peer_OnDisconnected;
        }

        private void Peer_OnConnected(IConnection conn)
        {
            Time.UpdateClient(Player);

            IsConnected = true;
            _connected?.Invoke(Player);
        }

        private void Peer_OnConnectionFailed(IConnection conn, RejectReason reason)
        {
            if (logger.LogEnabled()) logger.Log($"Failed to connect to {conn.EndPoint} with reason {reason}");
            Player?.MarkAsDisconnected();
            Disconnected?.Invoke(reason.ToClientStoppedReason());
            Cleanup();
        }

        private void Peer_OnDisconnected(IConnection conn, DisconnectReason reason)
        {
            if (logger.LogEnabled()) logger.Log($"Disconnected from {conn.EndPoint} with reason {reason}");
            Player?.MarkAsDisconnected();
            Disconnected?.Invoke(reason.ToClientStoppedReason());
            Cleanup();
        }

        protected override void Cleanup()
        {
            logger.Log("Shutting down client.");

            base.Cleanup();

            IsConnected = false;
            Player = null;
            _connected.Reset();
            _stopped.Reset();
        }

        /// <summary>
        /// Disconnect from server.
        /// <para>The disconnect message will be invoked.</para>
        /// </summary>
        public void Disconnect()
        {
            if (!Active)
            {
                logger.LogWarning("Can't disconnect client because it is not active");
                return;
            }

            Player.Connection.Disconnect();
            Cleanup();
        }

        public override void UpdateSent()
        {
            if (IsConnected)
                Time.UpdateClient(Player);

            base.UpdateSent();
        }

        internal class DataHandler : IDataHandler
        {
            private IConnection _connection;
            private INetworkPlayer _player;
            private readonly IMessageReceiver _messageHandler;

            public DataHandler(IMessageReceiver messageHandler)
            {
                _messageHandler = messageHandler;
            }

            public void SetConnection(IConnection connection, INetworkPlayer player)
            {
                _connection = connection;
                _player = player;
            }

            public void ReceiveMessage(IConnection connection, ArraySegment<byte> message)
            {
                logger.Assert(_connection == connection);
                _messageHandler.HandleMessage(_player, message);
            }
        }
    }
}
