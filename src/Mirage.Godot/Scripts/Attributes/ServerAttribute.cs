using System;

namespace Mirage
{
    /// <summary>
    /// Prevents a method from running if server is not active.
    /// <para>Can only be used inside a NetworkBehaviour</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ServerAttribute : Attribute
    {
        /// <summary>
        /// If true,  when the method is called from a client, it throws an error
        /// If false, no error is thrown, but the method won't execute
        /// useful for unity built in methods such as Await, Update, Start, etc.
        /// </summary>
        public bool error = true;
    }
}
