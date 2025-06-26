using System;

namespace Mirage
{
    /// <summary>
    /// SyncVars are used to synchronize a variable from the server to all clients automatically.
    /// <para>Value must be changed on server, not directly by clients.  Hook parameter allows you to define a client-side method to be invoked when the client gets an update from the server.</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class SyncVarAttribute : Attribute
    {
        ///<summary>A function that should be called on the client when the value changes.</summary>
        public string hook;
        /// <summary>
        /// If true, this syncvar will only be sent with spawn message, any other changes will not be sent to existing objects
        /// </summary>
        public bool initialOnly;

        /// <summary>
        /// If true this syncvar hook will also fire on the server side.
        /// </summary>
        // todo add test to make sure this runs on owner client
        // public bool invokeHookOnSender;
        // [System.Obsolete("Use invokeHookOnSender instead", false)]
        public bool invokeHookOnServer;

        /// <summary>
        /// If true this syncvar hook will also fire the owner when it is sending data
        /// </summary>
        public bool invokeHookOnOwner;

        /// <summary>
        /// What type of look Mirage should look for
        /// </summary>
        public SyncHookType hookType = SyncHookType.Automatic;
    }

    public enum SyncHookType
    {
        /// <summary>
        /// Looks for hooks matching the signature, gives compile error if none or more than 1 is found
        /// </summary>
        Automatic = 0,

        /// <summary>
        /// Hook with signature <c>void hookName()</c>
        /// </summary>
        MethodWith0Arg,

        /// <summary>
        /// Hook with signature <c>void hookName(T newValue)</c>
        /// </summary>
        MethodWith1Arg,

        /// <summary>
        /// Hook with signature <c>void hookName(T oldValue, T newValue)</c>
        /// </summary>
        MethodWith2Arg,

        /// <summary>
        /// Hook with signature <c>event Action hookName;</c>
        /// </summary>
        EventWith0Arg,

        /// <summary>
        /// Hook with signature <c>event Action{T} hookName;</c>
        /// </summary>
        EventWith1Arg,

        /// <summary>
        /// Hook with signature <c>event Action{T,T} hookName;</c>
        /// </summary>
        EventWith2Arg,
    }
}
