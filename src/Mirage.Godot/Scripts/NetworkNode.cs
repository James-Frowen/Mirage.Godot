using Mirage.RemoteCalls;

namespace Mirage
{
    public static class NetworkNodeExtensinos
    {
        public static bool IsServer(this INetworkNode node) => node.Identity.IsServer;
        public static bool IsClient(this INetworkNode node) => node.Identity.IsClient;
        public static bool HasAuthority(this INetworkNode node) => node.Identity.HasAuthority;
        public static bool IsMainCharacter(this INetworkNode node) => node.Identity.IsMainCharacter;

        public static NetworkServer Server(this INetworkNode node) => node.Identity.Server;
        public static NetworkClient Client(this INetworkNode node) => node.Identity.Client;
    }

    public interface INetworkNode
    {
        NetworkIdentity Identity { get; }
        int ComponentIndex { get; }
    }

    /// <summary>
    /// Add this to override default sync settings for Nodes
    /// </summary>
    public interface INetworkNodeWithSettings : INetworkNode
    {
        /// <summary>
        /// Sync settings for this NetworkBehaviour
        /// <para>Settings will be hidden in inspector unless Behaviour has SyncVar or SyncObjects</para>
        /// </summary>
        public SyncSettings SyncSettings { get; }
    }

    public interface INetworkNodeWithRpc : INetworkNode
    {
        int GetRpcCount();
        void RegisterRpc(RemoteCallCollection remoteCallCollection);
    }
}
