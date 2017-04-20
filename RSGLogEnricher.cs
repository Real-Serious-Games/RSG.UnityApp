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
    [LogEnricher]
    public class RSGLogEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("UserName", Environment.UserName));
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("MachineName", Environment.MachineName));
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("AppInstanceID", App.AppInstanceID));
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("DeviceID", App.DeviceID));
            if (!string.IsNullOrEmpty(App.DeviceName))
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("DeviceName", App.DeviceName));
            }
            
            // Ash: adding version number to logging shouold now be project specific.
            //logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("AppMajorVersion", appConfigurator.MajorVersionNumber));
            //logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("AppMinorVersion", appConfigurator.MinorVersionNumber));
        }
    }
}
