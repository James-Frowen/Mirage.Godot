using Godot;

namespace Mirage
{
    public partial class CharacterSpawner : Node
    {
        [Export] private NetworkManager _networkManager;
        [Export] private bool _spawnOnConnect;
        [Export] private PackedScene _player;

        public override void _Ready()
        {
            _networkManager.Server.Authenticated += Server_Authenticated;
        }

        private void Server_Authenticated(NetworkPlayer player)
        {
            if (_spawnOnConnect)
            {
                var clone = _player.Instantiate();
                GetTree().Root.AddChild(clone);

                var identity = clone.GetNetworkIdentity();
                _networkManager.ServerObjectManager.AddCharacter(player, identity);
            }
        }
    }
}
