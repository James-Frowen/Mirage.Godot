using Godot;
using Mirage;
using Mirage.Godot;
using Mirage.Logging;
using Mirage.SocketLayer;
using Mirage.Sockets.Udp;
using System;

namespace Mirage.Godot
{
    public partial class NetworkManager : Node
    {
        [Export] private ISocketFactory SocketFactory;

        public static NetworkManager i;

        public MirageServer Server;
        public MirageClient Client;

        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
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


public partial class NetworkRunner : Node
{
    public MirageServer Server { get; private set; }
    public MirageClient Client { get; private set; }

    private MiragePeer _activePeer;

    public NetworkRunner()
    {
        LogFactory.ReplaceLogHandler(new GodotLogger(), true);
        LogFactory.SetDefaultLogLevel(LogType.Log);
        LogFactory.GetLogger<Peer>().filterLogType = LogType.Warning;

        Server = new MirageServer();
        Client = new MirageClient();
    }

    public MirageServer StartUDPServer(int port)
    {
        if (_activePeer != null)
            throw new InvalidOperationException("Already running");

        GD.Print("StartUDPServer");
        var socketFactory = new UdpSocketFactory();
        var socket = socketFactory.CreateSocket();
        var endpoint = socketFactory.GetBindEndPoint(port);
        Server.StartServer(socket, socketFactory.MaxPacketSize, endpoint);

        _activePeer = Server;
        return Server;
    }
    public MirageClient StartUDPClient(string address, int port)
    {
        if (_activePeer != null)
            throw new InvalidOperationException("Already running");

        GD.Print("StartUDPClient");

        var socketFactory = new UdpSocketFactory();
        var socket = socketFactory.CreateSocket();
        var endpoint = socketFactory.GetConnectEndPoint(address, checked((ushort)port));
        Client.Connect(socket, socketFactory.MaxPacketSize, endpoint);

        _activePeer = Client;
        return Client;
    }

    public void Stop()
    {
        if (_activePeer is MirageServer server)
            server.Stop();
        else if (_activePeer is MirageClient client)
            client.Disconnect();

        _activePeer = null;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        if (_activePeer == null)
            return;

        _activePeer.UpdateReceive();
        _activePeer.UpdateSent();
    }
}

public partial class NetworkHud : Node
{
    [Export] private NetworkRunner networkRunner;
    [Export] private string Address = "127.0.0.1";
    [Export] private int Port = 7777;

    private Button serverButton;
    private Button clientButton;
    private Button stopButton;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        try
        {
            Mirage.GeneratedCode.Init();
        }
        catch (Exception e)
        {
            GD.PrintErr(e.ToString());
        }

        serverButton = new Button();
        serverButton.Text = "Start Server";
        serverButton.Pressed += StartServerPressed;
        AddChild(serverButton);

        clientButton = new Button();
        clientButton.Text = "Start Client";
        clientButton.Pressed += StartClientPressed;
        AddChild(clientButton);

        stopButton = new Button();
        stopButton.Text = "Stop";
        stopButton.Pressed += StopPressed;
        AddChild(stopButton);

        var pos = new Vector2(20, 20);
        VerticalLayout(serverButton, ref pos);
        VerticalLayout(clientButton, ref pos);
        VerticalLayout(stopButton, ref pos);

        ToggleButtons(false);
    }

    private void VerticalLayout(Button button, ref Vector2 pos, Vector2? sizeNullable = null, int padding = 10)
    {
        var size = sizeNullable ?? new Vector2(200, 40);
        button.Position = pos;
        button.Size = size;
        pos.Y += size.Y + padding;
    }

    private void StartServerPressed()
    {
        networkRunner.StartUDPServer(Port);
        ToggleButtons(true);
    }

    private void StartClientPressed()
    {
        networkRunner.StartUDPClient(Address, Port);
        ToggleButtons(true);
    }

    private void StopPressed()
    {
        networkRunner.Stop();
        ToggleButtons(false);
    }

    private void ToggleButtons(bool active)
    {
        serverButton.Disabled = active;
        clientButton.Disabled = active;
        stopButton.Disabled = !active;
    }
}
