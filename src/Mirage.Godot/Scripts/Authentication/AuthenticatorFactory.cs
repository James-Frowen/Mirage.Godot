using Godot;

namespace Mirage.Authentication
{
    /// <summary>
    /// Godot doesn't allow generic base types for nodes, so use a Factory node to Create the Authenticato
    /// </summary>
    public abstract partial class AuthenticatorFactory : Node
    {
        public abstract NetworkAuthenticator CreateAuthenticator();
    }
}

