using RSG.Utils;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RSG
{
    public class SerilogLogger : RSG.Utils.ILogger
    {
        /// <summary>
        /// Serilog logger.
        /// </summary>
        private readonly Serilog.ILogger serilog;

        /// <summary>
        /// Enables verbose logging.
        /// </summary>
        public bool EnableVerbose { get; set; }

        public SerilogLogger(Serilog.ILogger serilog)
        {
            Argument.NotNull(() => serilog);

            this.serilog = serilog;
        }

        /// <summary>
        /// Log an error.
        /// </summary>
        public void LogError(string message, params object[] args)
        {
            serilog.Error(message, args);
        }

        /// <summary>
        /// Log an error.
        /// </summary>
        public void LogError(Exception ex, string message, params object[] args)
        {
            serilog.Error(ex, message, args);

            LogExceptions(ex);
        }

        /// <summary>
        /// Helper to log inner exceptions.
        /// </summary>
        private void LogExceptions(Exception ex)
        {
            while (ex != null)
            {
                var formattedException = ex as FormattedException;
                if (formattedException != null)
                {
                    serilog.Warning(ex, "Exception ocurred\r\n" + formattedException.MessageTemplate, formattedException.PropertyValues);
                }
                else
                {
                    serilog.Warning(ex,
                        "Exception ocurred: {ExceptionMessage}\r\n" +
                        "Details:\r\n" +
                        "{@Exception}",
                        ex.Message, ex
                    );
                }

                ex = ex.InnerException;
            }            
        }

        /// <summary>
        /// Log an info message.
        /// </summary>
        public void LogInfo(string message, params object[] args)
        {
            serilog.Information(message, args);
        }

        /// <summary>
        /// Log a warning message.
        /// </summary>
        public void LogWarning(string message, params object[] args)
        {
            serilog.Warning(message, args);
        }

        /// <summary>
        /// Log a verbose message, by default these aren't output.
        /// </summary>
        public void LogVerbose(string message, params object[] args)
        {
            serilog.Verbose(message, args);
        }
    }
}
