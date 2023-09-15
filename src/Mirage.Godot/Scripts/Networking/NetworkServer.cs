using System;
using Mirage;

namespace MirageGodot
{
    public class NetworkServer : MirageServer
    {
        public NetworkWorld World { get; internal set; }

        internal void Spawn(NetworkNode obj)
        {
            throw new NotImplementedException();
        }
    }
}
