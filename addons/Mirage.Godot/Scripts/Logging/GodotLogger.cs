using System;
using Godot;
using Mirage.Logging;

namespace Mirage
{
    public class GodotLogger : ILogHandler
    {
        public void LogException(Exception exception)
        {
            LogFormat(LogType.Error, "[Exception] {0}", exception);
        }

        public void LogFormat(LogType logType, string format, params object[] args)
        {
            // only use format if there are args
            var msg = (args != null && args.Length > 0)
                ? string.Format(format, args)
                : format;

            switch (logType)
            {
                case LogType.Exception:
                case LogType.Assert:
                case LogType.Error:
                    GD.PrintErr(msg);
                    break;
                case LogType.Warning:
                    GD.Print($"[Warn] {msg}");
                    break;
                case LogType.Log:
                    GD.Print(msg);
                    break;
            }
        }
    }
}
