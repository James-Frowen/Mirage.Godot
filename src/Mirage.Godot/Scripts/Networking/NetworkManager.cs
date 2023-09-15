using Godot;
using Mirage.Logging;
using Mirage.SocketLayer;

namespace MirageGodot
{
    public partial class NetworkManager : Node
    {
        [Export] public SocketFactory SocketFactory;
        [Export] public bool EnableAllLogs;

        // todo do we really want singleton?
        public static NetworkManager i;

        public NetworkServer Server { get; private set; }
        public NetworkClient Client { get; private set; }


        public NetworkManager()
        {
            LogFactory.ReplaceLogHandler(new GodotLogger(), true);
        }

        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            i = this;
            if (EnableAllLogs)
            {
                LogFactory.SetDefaultLogLevel(LogType.Log, true);
                LogFactory.GetLogger<Peer>().filterLogType = LogType.Warning;
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
