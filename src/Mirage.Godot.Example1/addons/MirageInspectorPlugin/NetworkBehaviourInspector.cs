#if TOOLS
using Godot;

namespace Mirage.Editor
{
    public partial class NetworkBehaviourInspector : EditorInspectorPlugin
    {
        public override bool _CanHandle(GodotObject @object)
        {
            if (@object is NetworkIdentity)
                return false;
            if (@object is not Node)
                return false;

            return IsNetworkBehaviour(@object, out _);
        }

        public static bool IsNetworkBehaviour(GodotObject @object, out string typeName)
        {
            if (@object?.GetScript().As<Script>() is CSharpScript script)
            {
                var i = script.New().AsGodotObject();
                var type = i.GetType();
                if (type.IsSubclassOf(typeof(NetworkBehaviour)))
                {

                    typeName = type.Name;
                    return true;
                }
            }

            typeName = "ERROR";
            return false;
        }

        public override void _ParseEnd(GodotObject @object)
        {
            if (@object is not Node node)
                return;
            GD.Print($"GD Running NetworkBehaviourInspector");

            var identity = NodeHelper.GetNetworkIdentity(node, false);



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

            var grid = new GridContainer();
            grid.Columns = 2;
            AddCustomControl(grid);

            var label = new Label { Text = "Network Identity" };
            grid.AddChild(label);

            if (identity != null)
            {
                var button = new Button
                {
                    Text = identity.Name,
                    TooltipText = identity.GetPath(),
                    FocusMode = Control.FocusModeEnum.None,
                    Flat = true,
                };
                button.Pressed += () =>
                {
                    EditorInterface.Singleton.InspectObject(identity);
                };

                grid.AddChild(button);
            }
            else
            {
                var errorLabel = new Label { Text = "Not Found!" };
                var theme = EditorInterface.Singleton.GetBaseControl().Theme;
                if (theme.HasColor("error_color", "Editor"))
                {
                    errorLabel.AddThemeColorOverride("font_color", theme.GetColor("error_color", "Editor"));
                }
                grid.AddChild(errorLabel);
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
