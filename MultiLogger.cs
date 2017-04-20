using RSG.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RSG
{
    /// <summary>
    /// Forwards log messages to multiple destinations.
    /// </summary>
    public class MultiLogger : ILogger
    {
        private ILogger[] destLoggers;

        public MultiLogger(params ILogger[] destLoggers)
        {
            Argument.NotNull(() => destLoggers);

            this.destLoggers = destLoggers;
        }

        public void LogError(Exception ex, string message, params object[] args)
        {
            destLoggers.Each(log => log.LogError(ex, message, args));
        }

        public void LogError(string message, params object[] args)
        {
            destLoggers.Each(log => log.LogError(message, args));
        }

        public void LogInfo(string message, params object[] args)
        {
            destLoggers.Each(log => log.LogInfo(message, args));
        }

        public void LogVerbose(string message, params object[] args)
        {
            destLoggers.Each(log => log.LogVerbose(message, args));
        }

        public void LogWarning(string message, params object[] args)
        {
            destLoggers.Each(log => log.LogWarning(message, args));
        }
    }
}
