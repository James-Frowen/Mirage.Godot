namespace Mirage
{
    public static class MessageReceiverExtensions
    {
        /// <summary>
        /// Registers a handler for a network message that has just <typeparamref name="T"/> Message parameter
        /// <para>
        /// When network message are sent, the first 2 bytes are the Id for the type <typeparamref name="T"/>.
        /// When message is received the <paramref name="handler"/> with the matching Id is found and invoked
        /// </para>
        /// </summary>
        public static void RegisterHandler<T>(this IMessageReceiver receiver, MessageDelegate<T> handler)
        {
            receiver.RegisterHandler<T>((_, value) => handler.Invoke(value));
        }
    }
}
