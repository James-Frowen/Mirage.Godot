using Godot;

namespace Mirage
{
    [GlobalClass]
    public partial class CharacterSpawner : Node
    {
        [Export] public NetworkServer Server;
        [Export] public ServerObjectManager ServerObjectManager;
        [Export] public bool SpawnOnConnect;
        [Export] public PackedScene Player;
        private int spawnOffset;

        public override void _Ready()
        {
            Server.Authenticated += Server_Authenticated;
        }

        private void Server_Authenticated(NetworkPlayer player)
        {
            if (SpawnOnConnect)
            {
                var clone = Player.Instantiate();
                if (clone is Node3D node3d)
                {
                    node3d.Position += Vector3.Forward * (2 * spawnOffset);
                    spawnOffset++;

                    GD.Print($"Spawning at {node3d.Position}");
                }
                if (clone is Node2D node2d)
                {
                    node2d.Position = new Vector2(600, 200);
                }

                GetTree().Root.AddChild(clone);

                var identity = clone.GetNetworkIdentity();
                identity.PrefabHash = PrefabHashHelper.GetPrefabHash(Player);
                ServerObjectManager.AddCharacter(player, identity);
            }
        }
    }
}
