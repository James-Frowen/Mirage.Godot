namespace Mirage
{
    // A client sends this message to the server
    // to calculate RTT and synchronize time
    [NetworkMessage]
    public struct NetworkPingMessage
    {
        public double ClientTime;
    }

    // The server responds with this message
    // The client can use this to calculate RTT and sync time
    [NetworkMessage]
    public struct NetworkPongMessage
    {
        public double ClientTime;
        public double ServerTime;
    }
}
