using System;
using Godot;
using Mirage.Udp;

namespace Mirage
{
    [GlobalClass]
    public partial class NetworkHud : Node
    {
        [Export] private NetworkManager _manager;
        [Export] private UdpSocketFactory _socketFactory;
        [Export] private string _address = "127.0.0.1";
        [Export] private int _port = 7777;

        private Button serverButton;
        private Button clientButton;
        private Button hostButton;
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

            hostButton = new Button();
            hostButton.Text = "Start Host";
            hostButton.Pressed += StartHostPressed;
            AddChild(hostButton);

            stopButton = new Button();
            stopButton.Text = "Stop";
            stopButton.Pressed += StopPressed;
            AddChild(stopButton);

            var pos = new Vector2(20, 20);
            VerticalLayout(serverButton, ref pos);
            VerticalLayout(clientButton, ref pos);
            VerticalLayout(hostButton, ref pos);
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
            _socketFactory.Port = _port;
            _manager.StartServer();
            ToggleButtons(true);
        }

        private void StartClientPressed()
        {
            _socketFactory.Address = _address;
            _socketFactory.Port = _port;
            _manager.StartClient();
            ToggleButtons(true);
        }

        private void StartHostPressed()
        {
            _socketFactory.Port = _port;
            _manager.StartHost();
            ToggleButtons(true);
        }

        private void StopPressed()
        {
            _manager.Stop();
            ToggleButtons(false);
        }

        private void ToggleButtons(bool disabled)
        {
            serverButton.Disabled = disabled;
            clientButton.Disabled = disabled;
            hostButton.Disabled = disabled;
            stopButton.Disabled = !disabled;
        }
    }
}
