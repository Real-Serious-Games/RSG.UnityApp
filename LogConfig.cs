using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RSG
{
    /// <summary>
    /// Configuration file to initialise the logger.
    /// </summary>
    public class LogConfig
    {
        /// <summary>
        /// URL used to HTTP post log messages (or null to disable HTTP post).
        /// </summary>
        public string LogPostUrl { get; set; }

        /// <summary>
        /// Set to true to enable verbose logging.
        /// </summary>
        public bool Verbose { get; set; }
    }
}
