using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Mirage.Logging
{
    public partial class GodotLoggerSettings : GodotObject
    {
        public string Name;
        public string Namespace;
        public LogType logLevel;
    }



    [GlobalClass]
    public partial class MirageLogSettings : Node
    {
        public List<GodotLoggerSettings> LogLevels = new List<GodotLoggerSettings>();

        public override void _EnterTree()
        {
            var settings = LogLevels.Select(x => new LogSettingsSO.LoggerSettings(x.Name, x.Namespace, x.logLevel)).ToList();
            LogSettingsExtensions.LoadIntoLogFactory(settings);
        }
    }
}
