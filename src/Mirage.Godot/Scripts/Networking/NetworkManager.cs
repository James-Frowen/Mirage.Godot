using Godot;
using Mirage;
using Mirage.Logging;
using Mirage.SocketLayer;

namespace MirageGodot
{
    public partial class NetworkManager : Node
    {
        [Export] public SocketFactory SocketFactory;
        [Export] public bool EnableAllLogs;
        [Export] public PackedScene[] Prefabs;

        [ExportGroup("Settings")]
        [Export] public bool DisconnectOnException;

        public NetworkServer Server { get; private set; }
        public NetworkClient Client { get; private set; }


        public NetworkManager()
        {
            LogFactory.ReplaceLogHandler(new GodotLogger(), true);
            GeneratedCode.Init();
            Server = new NetworkServer();
            Client = new NetworkClient(this);
        }

        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            if (EnableAllLogs)
            {
                LogFactory.SetDefaultLogLevel(LogType.Log, true);
                LogFactory.GetLogger<Peer>().filterLogType = LogType.Warning;
            }

            Server.Setup(disconnectOnException: DisconnectOnException);
            Client.Setup(disconnectOnException: DisconnectOnException);
            //Client.RegisterPrefab(Prefabs);

            foreach (var prefab in Prefabs)
            {
                //prefab.Prepare();
            }
        }

        public void StartServer()
        {
            GD.Print("StartUDPServer");
            Server.StartServer(SocketFactory);
        }

        public void StartClient()
        {
            GD.Print("StartUDPClient");
            Client.Connect(SocketFactory);
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
