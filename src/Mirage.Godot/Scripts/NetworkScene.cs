using Godot;

namespace Mirage
{
    public partial class NetworkScene : Node
    {
        [Export] private NetworkIdentity[] _sceneObjects;

        public override void _Ready()
        {
            foreach (var identity in _sceneObjects)
            {
                var (sceneHash, prefabHash) = PrefabHashHelper.GetSceneHash(identity);
                identity.PrefabHash = prefabHash;
                identity.SceneHash = sceneHash;
            }
        }
    }
}
