using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Mirage.AsyncTasks;
using Mirage.Logging;

namespace Mirage.Authentication
{
    public sealed partial class AuthenticatorSettings : Node
    {
        private static readonly ILogger logger = LogFactory.GetLogger<AuthenticatorSettings>();

        [Export]
        public int TimeoutSeconds = 60;

        [Export(hintString: "Should the host player authenticate? If this is false then they will be marked as Authenticated automatically without going through a NetworkAuthenticator")]
        public bool RequireHostToAuthenticate;

        [Export(hintString: "List of Authenticators allowed, User can use any of them")]
        public NetworkAuthenticator[] Authenticators = new NetworkAuthenticator[0];

        private readonly Dictionary<NetworkPlayer, TaskCompletionSource<AuthenticationResult>> _pending = new Dictionary<NetworkPlayer, TaskCompletionSource<AuthenticationResult>>();

        private MessageHandler _authHandler;
        private NetworkServer _server;

        public void Setup(NetworkServer server)
        {
            if (_server != null && _server != server)
                throw new InvalidOperationException($"ServerAuthenticator already in use by another NetworkServer, current:{_server}, new:{server}");
            _server = server;

            server.MessageHandler.RegisterHandler<AuthMessage>(HandleAuthMessage, allowUnauthenticated: true);

            // message handler used just for Auth message
            // this is needed because message are wrapped inside AuthMessage
            _authHandler = new MessageHandler(null, true, _server.RethrowException);

            server.Disconnected += ServerDisconnected;

            foreach (var authenticator in Authenticators)
            {
                authenticator.Setup(_authHandler, AfterAuth);
            }
        }

        private void HandleAuthMessage(NetworkPlayer player, AuthMessage authMessage)
        {
            _authHandler.HandleMessage(player, authMessage.Payload);
        }

        private void ServerDisconnected(NetworkPlayer player)
        {
            // if player is pending, then set their result to fail
            if (_pending.TryGetValue(player, out var taskCompletion))
            {
                taskCompletion.TrySetResult(AuthenticationResult.CreateFail("Disconnected"));
            }
        }

        public async Task<AuthenticationResult> ServerAuthenticate(NetworkPlayer player)
        {
            if (SkipHost(player))
                return AuthenticationResult.CreateSuccess("Host player");

            if (logger.LogEnabled()) logger.Log($"Server authentication started {player}");

            var result = await RunServerAuthenticate(player);

            if (logger.LogEnabled())
            {
                var successText = result.Success ? "success" : "failed";
                var authenticatorName = result.Authenticator?.AuthenticatorName ?? "Null";
                logger.Log($"Server authentication {successText} {player}, Reason:{result.Reason}, Authenticator:{authenticatorName}");
            }

            return result;
        }

        private bool SkipHost(NetworkPlayer player)
        {
            var isHost = player == _server.LocalPlayer;

            if (!isHost)
                return false;

            var skip = !RequireHostToAuthenticate;
            return skip;
        }

        private async Task<AuthenticationResult> RunServerAuthenticate(NetworkPlayer player)
        {
            TaskCompletionSource<AuthenticationResult> taskCompletion;
            // host player should be added by PreAddHostPlayer, so we just get item
            if (player == _server.LocalPlayer)
            {
                taskCompletion = _pending[player];
            }
            // remote player should add new token here
            else
            {
                taskCompletion = new TaskCompletionSource<AuthenticationResult>();
                _pending.Add(player, taskCompletion);
            }


            try
            {
                var timeout = GoTask.Delay(TimeoutSeconds * 1000);
                var completion = taskCompletion.Task;

                var winner = await Task.WhenAny(completion, timeout);

                // need cancel for when player disconnects
                if (winner == timeout)
                {
                    return AuthenticationResult.CreateFail("Timeout");
                }

                // await will throw if thre is error, or result result if success
                return await completion;
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return AuthenticationResult.CreateFail($"Exception {e.GetType()}");
            }
            finally
            {
                _pending.Remove(player);
            }
        }

        internal void AfterAuth(NetworkPlayer player, AuthenticationResult result)
        {
            if (_pending.TryGetValue(player, out var taskCompletion))
            {
                taskCompletion.TrySetResult(result);
            }
            else
            {
                logger.LogError("Received AfterAuth Callback from player that was not in pending authentication");
            }
        }

        internal void PreAddHostPlayer(NetworkPlayer player)
        {
            // dont add if host dont require auth
            if (!RequireHostToAuthenticate)
                return;

            // host player is a special case, they are added early
            // otherwise Client.Connected can't be used to send auth message
            // because that is called before RunServerAuthenticate is called.
            var taskCompletion = new TaskCompletionSource<AuthenticationResult>();
            _pending.Add(player, taskCompletion);
        }
    }
}

