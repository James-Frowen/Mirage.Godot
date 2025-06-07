using System;
using Godot;

namespace Mirage.Authentication
{
    public interface INetworkAuthenticator
    {
        string AuthenticatorName { get; }
    }

    public abstract partial class NetworkAuthenticator : Node, INetworkAuthenticator
    {
        public virtual string AuthenticatorName => GetType().Name;

        internal abstract void Setup(MessageHandler messageHandler, Action<NetworkPlayer, AuthenticationResult> afterAuth);
    }

}

