using Godot;
using Mirage.Logging;
using Mirage.SocketLayer;

namespace Mirage
{
    public partial class NetworkManager : Node
    {
        [ExportGroup("Server")]
        [Export] public NetworkServer Server;
        [Export] public ServerObjectManager ServerObjectManager;
        [Export] public int MaxConnections;

        [ExportGroup("Client")]
        [Export] public NetworkClient Client;
        [Export] public ClientObjectManager ClientObjectManager;

        [ExportGroup("Shared")]
        [Export] public SocketFactory SocketFactory;
        [Export] public bool EnableAllLogs;
        [Export] public NetworkScene NetworkScene;

        public NetworkManager()
        {
            LogFactory.ReplaceLogHandler(new GodotLogger(), true);
            GeneratedCode.Init();
        }

        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            if (EnableAllLogs)
            {
                LogFactory.SetDefaultLogLevel(LogType.Log, true);
                LogFactory.GetLogger<Peer>().filterLogType = LogType.Warning;
            }
        }

        public void StartServer()
        {
            GD.Print("Starting Server Mode");
            // dont create a new peer config if we have already dont it somewhere else
            if (Server.PeerConfig == null)
            {
                // set MaxConnections
                Server.PeerConfig = new Config
                {
                    MaxConnections = MaxConnections,
                };
            }
            Server.StartServer();
            ClientObjectManager.PrepareToSpawnSceneObjects();
        }

        public void StartClient()
        {
            GD.Print("Starting Client Mode");
            Client.Connect();
        }

        public void StartHost()
        {
            GD.Print("Starting Host Mode");
            // dont create a new peer config if we have already dont it somewhere else
            if (Server.PeerConfig == null)
            {
                // set MaxConnections
                Server.PeerConfig = new Config
                {
                    MaxConnections = MaxConnections,
                };
            }
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
