using Godot;
using Mirage.SocketLayer;

namespace Mirage
{
    public abstract partial class SocketFactory : Node, ISocketFactory
    {
        public abstract int MaxPacketSize { get; }

        public abstract ISocket CreateClientSocket();
        public abstract ISocket CreateServerSocket();
        public abstract IEndPoint GetBindEndPoint();
        public abstract IEndPoint GetConnectEndPoint(string address = null, ushort? port = null);
    }
}
