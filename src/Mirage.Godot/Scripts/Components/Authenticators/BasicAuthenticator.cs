using Mirage.Authentication;

namespace Mirage.Authenticators
{
    public partial class BasicAuthenticator : NetworkAuthenticator<BasicAuthenticator.JoinMessage>
    {
        private BasicAuthenticatorFactory _settings;

        public BasicAuthenticator(BasicAuthenticatorFactory settings)
        {
            _settings = settings;
        }

        // called on server to validate
        protected override AuthenticationResult Authenticate(NetworkPlayer player, JoinMessage message)
        {
            if (_settings.ServerCode == message.ServerCode)
            {
                return AuthenticationResult.CreateSuccess(this, null);
            }
            else
            {
                return AuthenticationResult.CreateFail("Server code invalid", this);
            }
        }

        // called on client to create message to send to server
        public void SendCode(NetworkClient client, string serverCode = null)
        {
            var message = new JoinMessage
            {
                // use the argument or field if null
                ServerCode = serverCode ?? _settings.ServerCode
            };

            SendAuthentication(client, message);
        }

        [NetworkMessage]
        public struct JoinMessage
        {
            public string ServerCode;
        }
    }
}
