namespace Mirage.SocketLayer
{
    public interface ISocketFactory
    {
        int MaxPacketSize { get; }

        ISocket CreateClientSocket();
        ISocket CreateServerSocket();
        IEndPoint GetBindEndPoint();
        IEndPoint GetConnectEndPoint(string address = null, ushort? port = null);
    }
}
