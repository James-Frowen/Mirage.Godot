using Godot;
using Mirage.Authentication;

namespace Mirage.Authenticators.SessionId
{
    public partial class SessionIdAuthenticatorFactory : AuthenticatorFactory
    {
        [Export(hintString: "how many bytes to use for session ID")]
        public int SessionIDLength = 32;
        [Export(hintString: "How long ID is valid for, in minutes. 1440 => 1 day")]
        public int TimeoutMinutes = 1440;

        public readonly SessionIdAuthenticator Authenticator;

        public SessionIdAuthenticatorFactory()
        {
            Authenticator = new SessionIdAuthenticator(this);
        }

        public override NetworkAuthenticator CreateAuthenticator()
        {
            // pass in this, so that the export values can be changed at runtime
            return Authenticator;
        }
    }
}
