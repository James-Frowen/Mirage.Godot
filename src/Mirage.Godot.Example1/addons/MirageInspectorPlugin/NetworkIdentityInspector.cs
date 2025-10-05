#if TOOLS
using System.Collections.Generic;
using Godot;

namespace Mirage.Editor
{
    public partial class NetworkIdentityInspector : EditorInspectorPlugin
    {
        public override bool _CanHandle(GodotObject @object)
        {
            return @object is NetworkIdentity;
        }

        public override void _ParseEnd(GodotObject @object)
        {
            if (@object is not NetworkIdentity identity)
                return;

            var behaviours = new List<(Node, string name)>();
            var allNodes = NodeHelper.FindNetworkBehavioursInternal<Node>(identity.Root ?? identity, byte.MaxValue);
            foreach (var node in allNodes)
            {
                if (NetworkBehaviourInspector.IsNetworkBehaviour(node, out var typeName))
                {
                    behaviours.Add((node, typeName));
                }
            }

            {
                var separator = new HSeparator();
                var sb = new StyleBoxLine();
                sb.Color = new Color(.3f, .3f, .3f);
                sb.Thickness = 4;
                separator.AddThemeConstantOverride("separation", 30);
                separator.AddThemeStyleboxOverride("separator", sb);
                AddCustomControl(separator);
            }

            var header = new Label { Text = "Network Inspector", HorizontalAlignment = HorizontalAlignment.Center, };
            AddCustomControl(header);


            var label = new Label { Text = "Network Behaviours:" };
            AddCustomControl(label);

            if (behaviours != null && behaviours.Count > 0)
            {
                var grid = new GridContainer() { Columns = 1 };
                AddCustomControl(grid);

                foreach (var (node, typeName) in behaviours)
                {
                    var button = new Button
                    {
                        Alignment = HorizontalAlignment.Left,
                        Text = $"- {node.Name} ({typeName})",
                        TooltipText = node.GetPath(),
                        FocusMode = Control.FocusModeEnum.None,
                        Flat = true,
                    };
                    button.Pressed += () =>
                    {
                        EditorInterface.Singleton.InspectObject(node);
                    };
                    grid.AddChild(button);
                }
            }
            else
            {
                var noBehavioursLabel = new Label { Text = "No Network Behaviours found." };
                AddCustomControl(noBehavioursLabel);
            }

            {
                var separator = new HSeparator();
                var sb = new StyleBoxLine();
                sb.Color = new Color(.3f, .3f, .3f);
                sb.Thickness = 4;
                separator.AddThemeConstantOverride("separation", 30);
                separator.AddThemeStyleboxOverride("separator", sb);
                AddCustomControl(separator);
            }
        }
    }
}
#endif
