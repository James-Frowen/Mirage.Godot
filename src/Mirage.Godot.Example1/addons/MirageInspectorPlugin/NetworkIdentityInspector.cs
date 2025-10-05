#if TOOLS
using System.Linq;
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

            AddCustomControl(new HSeparator());

            var behaviours = NodeHelper.FindNetworkBehaviours(identity.Root ?? identity, byte.MaxValue);

            var label = new Label { Text = "Network Behaviours" };
            AddCustomControl(label);

            if (behaviours != null && behaviours.Any())
            {
                var grid = new GridContainer
                {
                    Columns = 2
                };
                AddCustomControl(grid);

                foreach (var behaviour in behaviours)
                {
                    if (behaviour is Node behaviourNode)
                    {
                        var nameLabel = new Label { Text = behaviourNode.Name };
                        grid.AddChild(nameLabel);

                        var button = new Button
                        {
                            Text = $"({behaviour.GetType().Name})",
                            TooltipText = behaviourNode.GetPath(),
                            FocusMode = Control.FocusModeEnum.None,
                            Flat = true,
                        };
                        button.Pressed += () =>
                        {
                            EditorInterface.Singleton.InspectObject(behaviourNode);
                        };
                        grid.AddChild(button);
                    }
                }
            }
            else
            {
                var noBehavioursLabel = new Label { Text = "No Network Behaviours found." };
                AddCustomControl(noBehavioursLabel);
            }
        }
    }
}
#endif
