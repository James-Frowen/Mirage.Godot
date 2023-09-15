using Mirage.Logging;
using Mirage.Serialization;
using Mirage.SocketLayer;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Mirage
{
    public sealed class MirageServer : MiragePeer
    {
        private static readonly ILogger logger = LogFactory.GetLogger(typeof(MirageServer));

        public event Action<INetworkPlayer> Connected;
        public event Action<INetworkPlayer> Disconnected;

        public IReadOnlyCollection<INetworkPlayer> Players => _connections.Values;

        private readonly Dictionary<IConnection, INetworkPlayer> _connections = new Dictionary<IConnection, INetworkPlayer>();

        public void StartServer(ISocket socket, int maxPacketSize, IEndPoint endPoint)
        {
            if (logger.LogEnabled()) logger.Log($"NetworkServer created, Mirage version: {Version.Current}");
            logger.Assert(Players.Count == 0, "Player should have been reset since previous session");
            logger.Assert(_connections.Count == 0, "Connections should have been reset since previous session");

            Start(socket, maxPacketSize, mb => new DataHandler(MessageHandler, _connections));
            MessageHandler.RegisterHandler<NetworkPingMessage>(Time.OnServerPing);

            Peer.Bind(endPoint);
            _started?.Invoke();
        }
        protected override void AddPeerEvents()
        {
            Peer.OnConnected += Peer_OnConnected;
            Peer.OnDisconnected += Peer_OnDisconnected;
        }
        protected override void RemovePeerEvents()
        {
            Peer.OnConnected -= Peer_OnConnected;
            Peer.OnDisconnected -= Peer_OnDisconnected;
        }

        private void Peer_OnConnected(IConnection conn)
        {
            var player = new NetworkPlayer(conn, false);
            if (logger.LogEnabled()) logger.Log($"Server new player {player}");

            // add connection
            _connections[player.Connection] = player;

            // let everyone know we just accepted a connection
            Connected?.Invoke(player);
        }
        private void Peer_OnDisconnected(IConnection conn, DisconnectReason reason)
        {
            if (logger.LogEnabled()) logger.Log($"Client {conn} disconnected with reason: {reason}");

            if (_connections.TryGetValue(conn, out var player))
            {
                OnDisconnected(player);
            }
            else
            {
                // todo remove or replace with assert
                if (logger.WarnEnabled()) logger.LogWarning($"No handler found for disconnected client {conn}");
            }
        }

        /// <summary>
        /// This removes an external connection.
        /// </summary>
        /// <param name="connectionId">The id of the connection to remove.</param>
        private void RemoveConnection(INetworkPlayer player)
        {
            _connections.Remove(player.Connection);
        }

        //called once a client disconnects from the server
        private void OnDisconnected(INetworkPlayer player)
        {
            if (logger.LogEnabled()) logger.Log("Server disconnect client:" + player);

            // set the flag first so we dont try to send any messages to the disconnected
            // connection as they wouldn't get them
            player.MarkAsDisconnected();

            RemoveConnection(player);

            Disconnected?.Invoke(player);
        }

        public void Stop()
        {
            if (!Active)
            {
                logger.LogWarning("Can't stop server because it is not active");
                return;
            }

            // just clear list, connections will be disconnected when peer is closed
            _connections.Clear();

            _stopped?.Invoke();
            Active = false;

            _started.Reset();
            _stopped.Reset();

            Cleanup();
        }

        public void SendToAll<T>(T msg, Channel channelId = Channel.Reliable)
        {
            var enumerator = _connections.Values.GetEnumerator();
            SendToMany(enumerator, msg, channelId);
        }

        /// <summary>
        /// Warning: this will allocate, Use <see cref="SendToMany{T}(IReadOnlyList{INetworkPlayer}, T, bool, Channel)"/> or <see cref="SendToMany{T, TEnumerator}(TEnumerator, T, bool, Channel)"/> instead
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="players"></param>
        /// <param name="msg"></param>
        /// <param name="excludeLocalPlayer"></param>
        /// <param name="channelId"></param>
        public void SendToMany<T>(IEnumerable<INetworkPlayer> players, T msg, Channel channelId = Channel.Reliable)
        {
            using (var list = AutoPool<List<INetworkPlayer>>.Take())
            {
                ListHelper.AddToList(list, players);
                MirageServer.SendToMany(list, msg, channelId);
            }
        }
        /// <summary>
        /// use to avoid allocation of IEnumerator
        /// </summary>
        public void SendToMany<T, TEnumerator>(TEnumerator playerEnumerator, T msg, Channel channelId = Channel.Reliable)
            where TEnumerator : struct, IEnumerator<INetworkPlayer>
        {
            using (var list = AutoPool<List<INetworkPlayer>>.Take())
            {
                ListHelper.AddToList<INetworkPlayer, TEnumerator>(list, playerEnumerator);
                MirageServer.SendToMany(list, msg, channelId);
            }
        }


        /// <summary>
        /// Sends to list of players.
        /// <para>All other SendTo... functions call this, it dooes not do any extra checks, just serializes message if not empty, then sends it</para>
        /// </summary>
        // need explicity List function here, so that implicit casts to List from wrapper works
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SendToMany<T>(List<INetworkPlayer> players, T msg, Channel channelId = Channel.Reliable)
            => SendToMany((IReadOnlyList<INetworkPlayer>)players, msg, channelId);

        /// <summary>
        /// Sends to list of players.
        /// <para>All other SendTo... functions call this, it dooes not do any extra checks, just serializes message if not empty, then sends it</para>
        /// </summary>
        public static void SendToMany<T>(IReadOnlyList<INetworkPlayer> players, T msg, Channel channelId = Channel.Reliable)
        {
            // avoid serializing when list is empty
            if (players.Count == 0)
                return;

            using (var writer = NetworkWriterPool.GetWriter())
            {
                if (logger.LogEnabled()) logger.Log($"Sending {typeof(T)} to {players.Count} players, channel:{channelId}");

                // pack message into byte[] once
                MessagePacker.Pack(msg, writer);
                var segment = writer.ToArraySegment();
                var count = players.Count;

                for (var i = 0; i < count; i++)
                {
                    players[i].Send(segment, channelId);
                }

                NetworkDiagnostics.OnSend(msg, segment.Count, count);
            }
        }


        /// <summary>
        /// This class will later be removed when we have a better implementation for IDataHandler
        /// </summary>
        private sealed class DataHandler : IDataHandler
        {
            private readonly IMessageReceiver _messageHandler;
            private readonly Dictionary<IConnection, INetworkPlayer> _players;

            public DataHandler(IMessageReceiver messageHandler, Dictionary<IConnection, INetworkPlayer> connections)
            {
                _messageHandler = messageHandler;
                _players = connections;
            }

            public void ReceiveMessage(IConnection connection, ArraySegment<byte> message)
            {
                if (_players.TryGetValue(connection, out var player))
                {
                    _messageHandler.HandleMessage(player, message);
                }
                else
                {
                    // todo remove or replace with assert
                    if (logger.WarnEnabled()) logger.LogWarning($"No player found for message received from client {connection}");
                }
            }
        }
    }
}
