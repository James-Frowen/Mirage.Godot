using Godot;
using JamesFrowen.NetworkPositionSync;
using Mirage;
using Mirage.Logging;

namespace Example2d
{
    [GlobalClass]
    public partial class Manager2d : Node
    {
        [Export] public NetworkManager NetworkManager;
        [Export] public PackedScene playerPrefab;
        [Export] public int cubeCount = 10;
        [Export] public PackedScene cubePrefab;

        private NetworkIdentity Spawn(PackedScene prefab)
        {
            var clone = prefab.Instantiate();
            GetTree().Root.AddChild(clone);

            var identity = NodeHelper.GetNetworkIdentity(clone, true);
            identity.PrefabHash = PrefabHashHelper.GetPrefabHash(prefab);
            return identity;
        }

        public override void _Ready()
        {
            NetworkManager.ClientObjectManager.RegisterPrefab(playerPrefab);
            NetworkManager.ClientObjectManager.RegisterPrefab(cubePrefab);

            NetworkManager.Server.Started.AddListener(ServerStarted);
            NetworkManager.Server.Authenticated += ServerAuthenticated;
            LogFactory.GetLogger<SyncPositionBehaviourCollection>().filterLogType = LogType.Log;
        }

        private void ServerStarted()
        {
            for (var i = 0; i < cubeCount; i++)
            {
                var clone = Spawn(cubePrefab);
                NetworkManager.ServerObjectManager.Spawn(clone);
            }
        }

        private void ServerAuthenticated(NetworkPlayer player)
        {
            var clone = Spawn(playerPrefab);
            NetworkManager.ServerObjectManager.AddCharacter(player, clone);
        }
    }
}

