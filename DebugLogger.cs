using RSG.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace RSG.UnityApp.Internal
{
    /// <summary>
    /// A simple debug logger.
    /// </summary>
    public class DebugLogger : ILogger
    {
        /// <summary>
        /// Log an error message.
        /// </summary>
        /// <param name="message"></param>
        public void LogError(string message, params object[] args)
        {
            Trace.WriteLine("[Error]: " + message);

            Console.WriteLine("[Error]: " + message);
        }

        /// <summary>
        /// Log an error caused by an exception.
        /// </summary>
        public void LogError(Exception ex, string message, params object[] args)
        {
            Trace.WriteLine("[Error]: " + message);
            Trace.WriteLine(ex.ToString());

            Console.WriteLine("[Error]: " + message);
            Console.WriteLine(ex.ToString());
        }

        /// <summary>
        /// Log a warning message.
        /// </summary>
        public void LogWarning(string message, params object[] args)
        {
            Trace.WriteLine("[Warning]: " + message);

            Console.WriteLine("[Warning]: " + message);
        }

        /// <summary>
        /// Log an info message.
        /// </summary>
        public void LogInfo(string message, params object[] args)
        {
            Trace.WriteLine(message);

            Console.WriteLine(message);
        }

        /// <summary>
        /// Log a verbose message, disabled by default.
        /// </summary>
        public void LogVerbose(string message, params object[] args)
        {
            LogInfo(message);
        }
    }
}
