using Godot;
using Mirage.Logging;
using Mirage.SocketLayer;

namespace Mirage
{
    [GlobalClass]
    public partial class NetworkManager : Node
    {
        private static readonly ILogger logger = LogFactory.GetLogger<NetworkManager>();

        [Export] public NetworkServer Server;
        [Export] public ServerObjectManager ServerObjectManager;
        [Export] public int MaxConnections;

        [Export] public NetworkClient Client;
        [Export] public ClientObjectManager ClientObjectManager;

        [Export] public SocketFactory SocketFactory;
        [Export] public bool EnableAllLogs;
        [Export] public NetworkScene NetworkScene;

        public override void _Ready()
        {
            base._Ready();
            GeneratedCode.Init();
        }

        public virtual void StartServer()
        {
            logger.Log("Starting Server Mode");
            Server.PeerConfig ??= new Config { MaxConnections = MaxConnections };
            Server.StartServer();
        }

        public virtual void StartClient()
        {
            logger.Log("Starting Client Mode");
            Client.Connect();
        }

        public virtual void StartHost()
        {
            logger.Log("Starting Host Mode");
            Server.StartServer(Client);
        }

        public void Stop()
        {
            if (Server.Active)
                Server.Stop();
            if (Client.Active)
                Client.Disconnect();
        }

        // Called every frame. 'delta' is the elapsed time since the previous frame.
        public override void _Process(double delta)
        {
            if (Server.Active)
                Server.UpdateReceive();
            if (Client.Active)
                Client.UpdateReceive();

            if (Server.Active)
                Server.UpdateSent();
            if (Client.Active)
                Client.UpdateSent();
        }
    }
}
