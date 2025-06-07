using System;
using System.Threading.Tasks;
using Mirage.Serialization;
namespace Mirage.Authentication;
public abstract partial class NetworkAuthenticator<T> : NetworkAuthenticator, INetworkAuthenticator
{
    private Action<NetworkPlayer, AuthenticationResult> _afterAuth;

    internal sealed override void Setup(MessageHandler messageHandler, Action<NetworkPlayer, AuthenticationResult> afterAuth)
    {
        messageHandler.RegisterHandler<T>(HandleAuth, allowUnauthenticated: true);
        _afterAuth = afterAuth;
    }

    private async Task HandleAuth(NetworkPlayer player, T msg)
    {
        var result = await AuthenticateAsync(player, msg);
        _afterAuth.Invoke(player, result);
    }

    /// <summary>
    /// Called on server to Authenticate a message from client
    /// <para>
    /// Use <see cref="AuthenticateAsync(T)"/> OR <see cref="Authenticate(T)"/>. 
    /// By default the async version just call the normal version.
    /// </para>
    /// </summary>
    /// <param name="player">player that send message</param>
    /// <param name="message"></param>
    /// <returns></returns>
    protected internal virtual Task<AuthenticationResult> AuthenticateAsync(NetworkPlayer player, T message)
    {
        return Task.FromResult(Authenticate(player, message));
    }

    /// <summary>
    /// Called on server to Authenticate a message from client
    /// <para>
    /// Use <see cref="AuthenticateAsync(T)"/> OR <see cref="Authenticate(T)"/>. 
    /// By default the async version just call the normal version.
    /// </para>
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    protected virtual AuthenticationResult Authenticate(NetworkPlayer player, T message) => throw new NotImplementedException("You must Implement Authenticate or AuthenticateAsync");

    /// <summary>
    /// Sends Authentication from client
    /// </summary>
    public void SendAuthentication(NetworkClient client, T message)
    {
        using (var writer = NetworkWriterPool.GetWriter())
        {
            MessagePacker.Pack(message, writer);
            var payload = writer.ToArraySegment();

            client.Send(new AuthMessage { Payload = payload });
        }
    }
}