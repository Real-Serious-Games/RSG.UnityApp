using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using UnityEngine;
using System.Collections;
using Newtonsoft.Json;
using RSG.Utils;

namespace RSG
{
    /// <summary>
    /// Serilog sink that writes to Http log using Unity's WWW class.
    /// </summary>
    internal class SerilogHttpSink : ILogEventSink
    {
        /// <summary>
        /// Logs that have been batched fro sending.
        /// </summary>
        private static readonly List<LogEvent> batchedLogs = new List<LogEvent>();

        /// <summary>
        /// URL to post logs to.
        /// </summary>
        private static string logPostUrl;

        /// <summary>
        /// Headers used for HTTP POST.
        /// </summary>
        private static readonly Dictionary<string, string> headers = new Dictionary<string, string>();

        public SerilogHttpSink(string logPostUrl)
        {
            Argument.StringNotNullOrEmpty(() => logPostUrl);

            SerilogHttpSink.logPostUrl = logPostUrl;

            headers.Add("Content-Type", "application/json");
        }

        public void Emit(LogEvent logEvent)
        {
            lock (batchedLogs)
            {
                batchedLogs.Add(logEvent);
            }

            /* Single log sending.
            var logs = LinqExts.FromItems(logEvent)
                .Select(log => new
                {
                    Timestamp = log.Timestamp,
                    Level = log.Level.ToString(),
                    MessageTemplate = log.MessageTemplate.Text,
                    RenderedMessage = log.RenderMessage(),
                    Properties = log.Properties
                })
                .ToArray();
            var json = JsonConvert.SerializeObject(new
            {
                Logs = logs
            });

            new WWW(logPostUrl, Encoding.ASCII.GetBytes(json), headers);
             * */
        }

        /// <summary>
        /// Send a batch of logs all at once.
        /// </summary>
        public static void SendBatch()
        {
            if (batchedLogs.Count == 0)
            {
                // Nothing batched.
                return;
            }

            lock (batchedLogs)
            {
                var logs = batchedLogs.Select(logEvent => new
                    {
                        Timestamp = logEvent.Timestamp,
                        Level = logEvent.Level.ToString(),
                        MessageTemplate = logEvent.MessageTemplate.Text,
                        RenderedMessage = logEvent.RenderMessage(),
                        Properties = logEvent.Properties
                    })
                    .ToArray();

                var json = JsonConvert.SerializeObject(new
                {
                    Logs = logs
                });

                batchedLogs.Clear(); // Clear pending logs.

                new WWW(logPostUrl, Encoding.ASCII.GetBytes(json), headers);
            }
        }
    }
}
