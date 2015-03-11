using RSG.Utils;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RSG
{
    /// <summary>
    /// Adds extra details to log messages.
    /// </summary>
    public class RSGLogEnricher : ILogEventEnricher
    {
        /// <summary>
        /// Instance ID for the application. Used to differentuate logs from different runs of the app.
        /// </summary>
        public static readonly string AppInstanceID = Guid.NewGuid().ToString();

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("UserName", Environment.UserName));
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("MachineName", Environment.MachineName));
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("AppInstanceID", AppInstanceID));

        }
    }
}
