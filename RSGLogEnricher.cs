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
        /// Interface that configures the app.
        /// </summary>
        private IAppConfigurator appConfigurator;

        public RSGLogEnricher(IAppConfigurator appConfigurator)
        {
            this.appConfigurator = appConfigurator;
        }

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
            
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("AppMajorVersion", appConfigurator.MajorVersionNumber));
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("AppMinorVersion", appConfigurator.MinorVersionNumber));
        }
    }
}
