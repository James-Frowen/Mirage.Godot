using Godot;

namespace MirageGodot.Example
{
    public partial class Character : NetworkNode
    {
        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            Events.OnStartServer.AddListener(StartServer);
            Events.OnStartClient.AddListener(StartClient);
        }

        private void StartServer()
        {
            GD.Print($"StartServer {Name} {NetId} {HasAuthority} {Player}");
        }

        private void StartClient()
        {
            GD.Print($"StartClient {Name} {NetId} {HasAuthority} {Player}");
        }

        // Called every frame. 'delta' is the elapsed time since the previous frame.
        public override void _Process(double delta)
        {
        }
    }
}