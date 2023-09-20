namespace Mirage
{
    /// <summary>
    /// Extension and static methods, mostly used by weaver 
    /// </summary>
    public static class NetworkNodeExtensinos
    {
        // todo decide which of these should be extension methods vs static only
        public static bool IsServer(this INetworkNode node) => node.Identity.IsServer;
        public static bool IsClient(this INetworkNode node) => node.Identity.IsClient;
        public static bool HasAuthority(this INetworkNode node) => node.Identity.HasAuthority;
        public static bool IsMainCharacter(this INetworkNode node) => node.Identity.IsMainCharacter;

        public static NetworkServer Server(this INetworkNode node) => node.Identity.Server;
        public static NetworkClient Client(this INetworkNode node) => node.Identity.Client;

        public static INetworkPlayer GetClientPlayer(INetworkNode node) => node.Identity.Client.Player;
    }
}
