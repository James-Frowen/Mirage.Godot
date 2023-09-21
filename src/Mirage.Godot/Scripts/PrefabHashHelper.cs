using Godot;

namespace Mirage
{
    public static class PrefabHashHelper
    {
        public static int GetPrefabHash(PackedScene prefab)
        {
            var path = prefab.ResourcePath;
            var hash = path.GetStableHashCode();
            GD.Print($"Creating PrefabHash:{hash:X} from '{path}'");
            return hash;
        }

        public static (int SceneHash, int PrefabHash) GetSceneHash(NetworkIdentity identity)
        {
            var scenePath = identity.Root.SceneFilePath;
            var nodePath = identity.Root.GetPath().ToString();

            var sceneHash = scenePath.GetStableHashCode();
            var prefabHash = nodePath.GetStableHashCode();

            GD.Print($"Creating Scene Id. SceneHash:{sceneHash:X} from '{scenePath}'. PrefabHash:{prefabHash:X} from '{nodePath}'.");

            return (sceneHash, prefabHash);
        }

        public static ulong? ToSceneId(int? sceneHash, int prefabHash)
        {
            if (sceneHash.HasValue)
            {
                return (((ulong)sceneHash.Value) << 32) | (uint)prefabHash;
            }
            return null;
        }
    }
}
