using System;
using System.Threading.Tasks;

namespace Mirage
{
    /// <summary>
    /// Register handler just for 1 message
    /// <para>Useful on client when you want too receive a single auth message</para>
    /// </summary>
    public class MessageWaiter<T>
    {
        private bool _received;
        private T _message;
        private MirageClient _client;
        private MessageHandler _messageHandler;
        private MessageDelegateWithPlayer<T> callback;

        public MessageWaiter(MirageClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _messageHandler = _client.MessageHandler;
            _messageHandler.RegisterHandler<T>(HandleMessage);
        }

        private void HandleMessage(INetworkPlayer player, T message)
        {
            _message = message;
            _received = true;

            _messageHandler.UnregisterHandler<T>();
            callback?.Invoke(player, message);
        }

        public async Task<(bool disconnected, T message)> WaitAsync()
        {
            while (true)
            {
                if (_received || !_client.IsConnected)
                    break;

                await Task.Delay(1);
            }

            // check _client.IsConnected again here, incase we disconnected after _receiving 
            return (!_client.IsConnected, _message);
        }

        /// <summary>
        /// Use callback instead of async for methods that uses ArraySegment, because internal buffer will be recylced and data will be load before Async completes
        /// </summary>
        /// <param name="callback"></param>
        public void Callback(MessageDelegateWithPlayer<T> callback)
        {
            this.callback = callback;
        }
    }
}
