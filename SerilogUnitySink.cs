using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RSG
{
    /// <summary>
    /// Extension to configure SErilog to write to Unity sink.
    /// </summary>
    public static class SerilogUnitySinkExt
    {
        public static LoggerConfiguration UnityLog(this LoggerSinkConfiguration config)
        {
            return config.Sink<SerilogUnitySink>();
        }
    }

    /// <summary>
    /// Serilog sink that writes to Unity log.
    /// </summary>
    [SerilogSink]
    internal class SerilogUnitySink : ILogEventSink
    {
        public void Emit(LogEvent logEvent)
        {
            if (logEvent.Level == LogEventLevel.Debug || logEvent.Level == LogEventLevel.Information)
            {
                Debug.Log(logEvent.RenderMessage());
            }
            else if (logEvent.Level == LogEventLevel.Error || logEvent.Level == LogEventLevel.Fatal)
            {
                Debug.LogError(logEvent.RenderMessage());
            }
            else if (logEvent.Level == LogEventLevel.Warning)
            {
                Debug.LogWarning(logEvent.RenderMessage());
            }
        }            
    }
}
