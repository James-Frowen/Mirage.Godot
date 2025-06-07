using System.Net;
using System.Net.Sockets;
using Godot;
using Mirage.SocketLayer;

namespace Mirage.Udp
{
    [GlobalClass]
    public partial class UdpSocketFactory : SocketFactory
    {
        [Export] public int Port = 7777;
        [Export] public string Address = "127.0.0.1";

        public override int MaxPacketSize => UdpMTU.MaxPacketSize;

        public override ISocket CreateClientSocket() => new UdpSocket();

        public override ISocket CreateServerSocket() => new UdpSocket();

        public override IEndPoint GetBindEndPoint()
        {
            return new EndPointWrapper(new IPEndPoint(IPAddress.IPv6Any, Port));
        }

        public override IEndPoint GetConnectEndPoint(string address = null, ushort? port = null)
        {
            var ipAddress = getAddress(address ?? Address);
            var portIn = port ?? Port;
            return new EndPointWrapper(new IPEndPoint(ipAddress, portIn));
        }

        private IPAddress getAddress(string addressString)
        {
            if (IPAddress.TryParse(addressString, out var address))
                return address;

            var results = Dns.GetHostAddresses(addressString);
            if (results.Length == 0)
            {
                throw new SocketException((int)SocketError.HostNotFound);
            }
            else
            {
                return results[0];
            }
        }
    }

    public class EndPointWrapper : IEndPoint
    {
        public EndPoint inner;

        public EndPointWrapper(EndPoint endPoint)
        {
            inner = endPoint;
        }

        public override bool Equals(object obj)
        {
            if (obj is EndPointWrapper other)
            {
                return inner.Equals(other.inner);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return inner.GetHashCode();
        }

        public override string ToString()
        {
            return inner.ToString();
        }

        IEndPoint IEndPoint.CreateCopy()
        {
            // copy the inner endpoint
            var copy = inner.Create(inner.Serialize());
            return new EndPointWrapper(copy);
        }
    }

    public class UdpMTU
    {
        /// <summary>
        /// IPv6 + UDP Header
        /// </summary>
        private const int HEADER_SIZE = 40 + 8;

        /// <summary>
        /// MTU is expected to be atleast this number
        /// </summary>
        private const int MIN_MTU = 1280;

        /// <summary>
        /// Max size of array that will be sent to or can be received from <see cref="ISocket"/>
        /// <para>This will also be the size of all buffers used by <see cref="Peer"/></para>
        /// <para>This is not max message size because this size includes packets header added by <see cref="Peer"/></para>
        /// </summary>
        // todo move these settings to socket
        public static int MaxPacketSize => MIN_MTU - HEADER_SIZE;
    }
}
