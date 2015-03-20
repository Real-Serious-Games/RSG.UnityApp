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
    /// Serilog sink that writes to Unity log.
    /// </summary>
    [SerilogSink]
    internal class SerilogUnitySink : ILogEventSink
    {
        public void Emit(LogEvent logEvent)
        {
            var msg = "[RSG]: " + logEvent.RenderMessage();

            if (logEvent.Level == LogEventLevel.Error || logEvent.Level == LogEventLevel.Fatal)
            {
                Debug.LogError(msg);
            }
            else if (logEvent.Level == LogEventLevel.Warning)
            {
                Debug.LogWarning(msg);
            }
            else
            {
                Debug.Log(msg);
            }
        }            
    }
}
