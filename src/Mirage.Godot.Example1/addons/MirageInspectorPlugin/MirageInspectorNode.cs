#if TOOLS
using System;
using Godot;

namespace Mirage.Editor
{
    [Tool]
    public partial class MirageInspectorNode : EditorPlugin
    {
        private NetworkBehaviourInspector _behaviourInspector;
        private NetworkIdentityInspector _identityInspector;

        public override void _EnterTree()
        {
            GD.Print($"GD Running MirageInspectorNode");
            Console.WriteLine($"Running MirageInspectorNode");

            _behaviourInspector = new NetworkBehaviourInspector();
            _identityInspector = new NetworkIdentityInspector();

            AddInspectorPlugin(_behaviourInspector);
            AddInspectorPlugin(_identityInspector);
        }

        public override void _ExitTree()
        {
            RemoveInspectorPlugin(_behaviourInspector);
            RemoveInspectorPlugin(_identityInspector);
        }
    }
}
#endif
