using Godot;
using Mirage.Authentication;

namespace Mirage.Authenticators
{
    public partial class BasicAuthenticatorFactory : AuthenticatorFactory
    {
        [Export] public string ServerCode;

        public override NetworkAuthenticator CreateAuthenticator()
        {
            // pass in this, so that the export values can be changed at runtime
            return new BasicAuthenticator(this);
        }
    }
}
