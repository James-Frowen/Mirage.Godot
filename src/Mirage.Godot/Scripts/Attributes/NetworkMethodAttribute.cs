using System;

namespace Mirage
{
    /// <summary>
    /// Prevents this method from running unless the NetworkFlags match the current state
    /// <para>Can only be used inside a NetworkBehaviour</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class NetworkMethodAttribute : Attribute
    {
        /// <summary>
        /// If true, if called incorrectly method will throw.<br/>
        /// If false, no error is thrown, but the method won't execute.<br/>
        /// <para>
        /// useful for unity built in methods such as Await, Update, Start, etc.
        /// </para>
        /// </summary>
        public bool error = true;

        public NetworkMethodAttribute(NetworkFlags flags) { }
    }

    [Flags]
    public enum NetworkFlags
    {
        // note: NotActive can't be 0 as it needs its own flag
        //       This is so that people can check for (Server | NotActive)
        /// <summary>
        /// If both server and client are not active. Can be used to check for singleplayer or unspawned object
        /// </summary>
        NotActive = 1,
        Server = 2,
        Client = 4,
        /// <summary>
        /// If either Server or Client is active.
        /// <para>
        /// Note this will not check host mode. For host mode you need to use <see cref="ServerAttribute"/> and <see cref="ClientAttribute"/>
        /// </para>
        /// </summary>
        Active = Server | Client,
        HasAuthority = 8,
        LocalOwner = 16,
    }
}
