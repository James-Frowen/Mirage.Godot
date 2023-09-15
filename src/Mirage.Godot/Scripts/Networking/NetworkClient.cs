using System.Collections.Generic;
using Godot;
using Mirage;

namespace MirageGodot
{
    public class NetworkClient : MirageServer
    {
        [Export] public List<NetworkNode> Prefabs;

        public NetworkWorld World { get; internal set; }
    }
}
