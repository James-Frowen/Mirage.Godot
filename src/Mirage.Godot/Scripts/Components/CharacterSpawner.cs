using Godot;

namespace Mirage
{
    public partial class CharacterSpawner : Node
    {
        [Export] private NetworkManager _networkManager;
        [Export] private bool _spawnOnConnect;
        [Export] private PackedScene _player;
        private int spawnOffset;

        public override void _Ready()
        {
            _networkManager.Server.Authenticated += Server_Authenticated;
        }

        private void Server_Authenticated(NetworkPlayer player)
        {
            if (_spawnOnConnect)
            {
                var clone = _player.Instantiate();
                if (clone is Node3D node3d)
                {
                    node3d.Position += Vector3.Forward * (2 * spawnOffset);
                    spawnOffset++;

                    GD.Print($"Spawning at {node3d.Position}");
                }


                GetTree().Root.AddChild(clone);

                var identity = clone.GetNetworkIdentity();
                identity.PrefabHash = PrefabHashHelper.GetPrefabHash(_player);
                _networkManager.ServerObjectManager.AddCharacter(player, identity);
            }
        }
    }
}
