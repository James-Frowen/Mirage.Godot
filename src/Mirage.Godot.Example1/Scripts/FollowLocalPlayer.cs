using Godot;
using Mirage;
using MirageGodot;

namespace Example1
{
    [NetworkMessage]
    public struct A
    {
        public string B;
    }
    public partial class FollowLocalPlayer : Node
    {
        [Export] private NetworkManager _networkManager;
        [Export] private FollowTarget _followTarget;

        public override void _Ready()
        {
            _networkManager.Client.World.onSpawn += World_onSpawn;

            _networkManager.Server.SendToAll(new A
            {
                B = "Test"
            });
        }

        private void World_onSpawn(NetworkNode obj)
        {
            // already set, dont need to set again
            if (_followTarget.Target != null)
                return;

            if (obj.HasAuthority)
            {
                var root = obj.Root;
                if (root is Node3D node3D)
                    _followTarget.Target = node3D;
                else
                    GD.PrintErr("Player's Root node was not a Node3D");
            }
        }
    }
}
