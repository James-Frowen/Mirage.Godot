using Godot;
using Mirage;

namespace Example1
{
    public partial class FollowLocalPlayer : Node
    {
        [Export] private NetworkManager _networkManager;
        [Export] private FollowTarget _followTarget;

        public override void _Ready()
        {
            _networkManager.Client.Started.AddListener(ClientStarted);
        }

        private void ClientStarted()
        {
            _networkManager.Client.World.onSpawn += World_onSpawn;
        }

        private void World_onSpawn(NetworkIdentity obj)
        {
            // already set, dont need to set again
            if (_followTarget.HasValidTarget())
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
