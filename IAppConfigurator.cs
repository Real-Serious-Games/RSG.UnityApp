using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RSG
{
    /// <summary>
    /// Interface that can be implemented by user's of the library to pass in settings to RSG.UnityApp.
    /// </summary>
    public interface IAppConfigurator
    {
        /// <summary>
        /// URL used to HTTP post log messages (or null to disable HTTP post).
        /// </summary>
        string LogPostUrl { get; }
    }
}
