using System;
using System.IO;
using System.Linq;
using Godot;
using Mirage.Logging;

namespace Mirage
{
    public class FileLogger : ILogHandler
    {
        private readonly string _path;
        private readonly bool _includeStack;

        public FileLogger(string path, bool clearFile, bool includeStack)
        {
            var dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (clearFile)
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            File.AppendAllText(path, $"Starting log file: {DateTime.Now}\n\n");
            _path = path;
            _includeStack = includeStack;
        }
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

            if (_includeStack && OS.IsDebugBuild())
            {
                var stack = System.Environment.StackTrace;
                var lines = stack.Split('\n');
                var withoutLogStack = lines.Skip(4);
                msg += "\n" + string.Join("\n", withoutLogStack);
            }
            msg = $"[{logType}] {msg}\n\n";

            File.AppendAllText(_path, msg);
        }
    }
}
