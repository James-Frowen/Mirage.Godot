using System;
using Mirage.Logging;

namespace Mirage
{
    public class MultiLogger : ILogHandler
    {
        private readonly ILogHandler[] _logHandlers;
        public MultiLogger(params ILogHandler[] logHandlers)
        {
            _logHandlers = logHandlers;
        }

        public void LogException(Exception exception)
        {
            for (var i = 0; i < _logHandlers.Length; i++)
            {
                _logHandlers[i].LogException(exception);
            }
        }

        public void LogFormat(LogType logType, string format, params object[] args)
        {
            for (var i = 0; i < _logHandlers.Length; i++)
            {
                _logHandlers[i].LogFormat(logType, format, args);
            }
        }
    }
}
