using System;

namespace Mirage
{
    /// <summary>
    /// The server uses a Remote Procedure Call (RPC) to run this function on specific clients.
    /// <para>Note that if you set the target as Connection, you need to pass a specific connection as a parameter of your method</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ClientRpcAttribute : Attribute
    {
        public Channel channel = Channel.Reliable;
        public RpcTarget target = RpcTarget.Observers;
        public bool excludeOwner;

    }

    /// <summary>
    /// Used by ClientRpc to tell mirage who to send remote call to
    /// </summary>
    public enum RpcTarget
    {
        /// <summary>
        /// Sends to the <see cref="NetworkPlayer">Player</see> that owns the object
        /// </summary>
        Owner,
        /// <summary>
        /// Sends to all <see cref="NetworkPlayer">Players</see> that can see the object
        /// </summary>
        Observers,
        /// <summary>
        /// Sends to the <see cref="NetworkPlayer">Player</see> that is given as an argument in the RPC function (requires target to be an observer)
        /// </summary>
        Player
    }
}
