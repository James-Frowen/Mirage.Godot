using System;
using Godot;
using Mirage.Logging;

namespace Mirage.Authenticators.SessionId
{
    /// <summary>
    /// Creates a session to be used by <see cref="SessionIdAuthenticator"/>
    /// </summary>
    public partial class CreateSession : Node
    {
        private static readonly ILogger logger = LogFactory.GetLogger<CreateSession>();

        [Export] public NetworkServer Server;
        [Export] public NetworkClient Client;
        [Export] public SessionIdAuthenticator Authenticator;
        [Export] public bool AutoRefreshSession = true;
        private bool _sentRefresh = false;

        public override void _Ready()
        {
            if (Client != null)
            {
                Client.Connected.AddListener(ClientConnected);
                Client.Authenticated.AddListener(ClientAuthenticated);
            }

            if (Server != null)
            {
                Server.Started.AddListener(ServerStarted);
            }
        }

        private void ServerStarted()
        {
            Server.MessageHandler.RegisterHandler<RequestSessionMessage>(HandleRequestSession);
        }

        private void ClientConnected(NetworkPlayer player)
        {
            if (Authenticator.ClientIdStore.TryGetSession(out var session))
            {
                // if before timeout, then use it to authenticate
                if (DateTime.Now < session.Timeout)
                {
                    if (logger.LogEnabled()) logger.Log("Client connected, Sending Session Authentication automatically");
                    SendAuthentication(session);
                }
            }
        }

        private void SendAuthentication(ClientSession session)
        {
            var msg = new SessionKeyMessage
            {
                SessionKey = new ArraySegment<byte>(session.Key)
            };
            Authenticator.SendAuthentication(Client, msg);
        }

        private void ClientAuthenticated(NetworkPlayer player)
        {
            if (!Authenticator.ClientIdStore.TryGetSession(out _))
            {
                if (logger.LogEnabled()) logger.Log("Client authenicated but didn't have session, Requesting Session now");
                RequestSession();
            }
        }

        private void RequestSession()
        {
            var waiter = new MessageWaiter<SessionKeyMessage>(Client, allowUnauthenticated: false);

            Client.Send(new RequestSessionMessage { });

            _sentRefresh = true;
            waiter.Callback((_, msg) =>
            {
                // copy to new array, because ArraySegment will be reused aft
                var key = msg.SessionKey.ToArray();
                var session = new ClientSession
                {
                    Key = key,
                    Timeout = DateTime.Now.AddMinutes(Authenticator.TimeoutMinutes),
                };

                Authenticator.ClientIdStore.StoreSession(session);
                _sentRefresh = false;
            });
        }

        private void HandleRequestSession(NetworkPlayer player, RequestSessionMessage message)
        {
            if (logger.LogEnabled()) logger.Log($"{player} requested new session token");
            var sessionKey = Authenticator.CreateOrRefreshSession(player);
            player.Send(new SessionKeyMessage { SessionKey = sessionKey });
        }

        public override void _Process(double delta)
        {
            if (AutoRefreshSession)
                CheckRefresh();
        }

        private void CheckRefresh()
        {
            // sent message and waiting for reply from server
            if (_sentRefresh)
                return;

            if (Client == null || !Client.Active)
                return;

            if (!Authenticator.ClientIdStore.TryGetSession(out var session))
                return;

            if (ShouldRefresh(Authenticator.TimeoutMinutes, session.Timeout))
            {
                if (logger.LogEnabled()) logger.Log("Refreshing token before timeout, Requesting Session now");

                RequestSession();
            }
        }

        private static bool ShouldRefresh(int timeoutMinutes, DateTime sessionTimeout)
        {
            var halfTotalTimeout = timeoutMinutes / 2.0;
            var timeRemaining = sessionTimeout - DateTime.Now;

            return timeRemaining.TotalMinutes <= halfTotalTimeout;
        }
    }
}
