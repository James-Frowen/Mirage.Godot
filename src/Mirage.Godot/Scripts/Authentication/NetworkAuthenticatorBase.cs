using System;
using Godot;

namespace Mirage.Authentication
{
    public abstract partial class NetworkAuthenticatorBase : Node, INetworkAuthenticator
    {
        public virtual string AuthenticatorName => GetType().Name;

        internal abstract void Setup(MessageHandler messageHandler, Action<NetworkPlayer, AuthenticationResult> afterAuth);
    }
}

