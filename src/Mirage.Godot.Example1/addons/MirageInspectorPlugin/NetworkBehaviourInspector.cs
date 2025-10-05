#if TOOLS
using Godot;

namespace Mirage.Editor
{
    public partial class NetworkBehaviourInspector : EditorInspectorPlugin
    {
        public override bool _CanHandle(GodotObject @object)
        {
            if (@object is NetworkBehaviour)
            {
                return true;
            }
            else if (@object is Node node)
            {
                //GD.Print($"NB {@object is NetworkBehaviour} {@object.GetType()}");

                //var objectType = @object.GetType();
                //var scriptVariant = @object.GetScript();
                //var script = scriptVariant.As<Script>();
                //var isCsScript = false;
                //string scriptClassName = null;
                //System.Type scriptType = null;
                //var isSubNB = false;
                //if (script is CSharpScript cSharpScript)
                //{
                //    isCsScript = true;
                //    // This is the important part: use reflection to check the class type
                //    scriptClassName = cSharpScript.GetClass();
                //    scriptType = System.Type.GetType(scriptClassName);

                //    isSubNB = scriptType != null && scriptType.IsSubclassOf(typeof(NetworkBehaviour));
                //}

                //NetworkBehaviour nb = null;
                //try
                //{
                //    nb = scriptVariant.As<NetworkBehaviour>();
                //}
                //catch { }

                //GD.Print("--- NetworkBehaviourInspector._CanHandle ---");
                //GD.Print($"Object: {@object}");
                //GD.Print($"Object C# Type: {objectType.FullName}");
                //GD.Print($"Script Path: {script?.ResourcePath}");
                //GD.Print($"Result of 'scriptVariant.As<NetworkBehaviour>()': {nb}");
                //GD.Print($"Result of 'isCsScript': {isCsScript}");
                //GD.Print($"Result of 'scriptClassName': {scriptClassName}");
                //GD.Print($"Result of 'scriptType': {scriptType}");
                //GD.Print($"Result of 'isSubNB': {isSubNB}");
                //GD.Print($"Result of 'IsClass(NetworkBehaviour)': {@object.IsClass("NetworkBehaviour")}");


                //GD.Print("-----------------------------------------");
                //node.PrintTree();
                // TODO find class and see if it is child of NetworkBehaviour
            }
            return false;
        }

        public override void _ParseEnd(GodotObject @object)
        {
            if (@object is not Node node)
                return;
            GD.Print($"GD Running NetworkBehaviourInspector");

            // Add a separator to make it look nice
            AddCustomControl(new HSeparator());

            var identity = NodeHelper.GetNetworkIdentity(node, false);

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
        }
    }
}
#endif
