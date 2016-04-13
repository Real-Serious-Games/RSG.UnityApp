using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RSG
{
    /// <summary>
    /// This attribute can be attached to 'log enricher' classes.
    /// This is a plugin system for adding new log properties to Serilog logging output.
    /// </summary>
    public class LogEnricherAttribute : Attribute
    {
    }
}
