using System;
using Mirage.SocketLayer;

namespace Mirage
{
    /// <summary>
    /// An object that can send messages
    /// </summary>
    public interface IMessageSender
    {
        void Send<T>(T message, Channel channelId = Channel.Reliable);
        void Send(ArraySegment<byte> segment, Channel channelId = Channel.Reliable);
        void Send<T>(T message, INotifyCallBack notifyCallBack);
    }

    // delegates to give names to variables in handles
    public delegate void MessageDelegate<in T>(T message);
    public delegate void MessageDelegateWithPlayer<in T>(INetworkPlayer player, T message);


    /// <summary>
    /// An object that can receive messages
    /// </summary>
    public interface IMessageReceiver
    {
        /// <summary>
        /// Registers a handler for a network message that has INetworkPlayer and <typeparamref name="T"/> Message parameters
        /// <para>
        /// When network message are sent, the first 2 bytes are the Id for the type <typeparamref name="T"/>.
        /// When message is received the <paramref name="handler"/> with the matching Id is found and invoked
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handler"></param>
        /// <param name="allowUn authenticated">set this to true to allow message to be invoked before player is authenticated</param>
        void RegisterHandler<T>(MessageDelegateWithPlayer<T> handler);
        void UnregisterHandler<T>();
        void ClearHandlers();
        void HandleMessage(INetworkPlayer player, ArraySegment<byte> packet);
    }

    /// <summary>
    /// An object owned by a player that can: send/receive messages, have network visibility, be an object owner, authenticated permissions, and load scenes.
    /// May be from the server to client or from client to server
    /// </summary>
    public interface INetworkPlayer : IMessageSender
    {
        SocketLayer.IEndPoint Address { get; }
        SocketLayer.IConnection Connection { get; }
        /// <summary>
        /// True if this Player is the local player on the server or client
        /// </summary>
        bool IsHost { get; }

        void Disconnect();
        void MarkAsDisconnected();
    }
}
