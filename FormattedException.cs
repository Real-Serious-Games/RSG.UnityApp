using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RSG
{
    /// <summary>
    /// An exception whose message can be formatted using a Serilog message template.
    /// </summary>
    public class FormattedException : ApplicationException
    {
        /// <summary>
        /// The template for the exceptions message (Serilog format).
        /// </summary>
        public string MessageTemplate
        {
            get;
            private set;
        }

        /// <summary>
        /// Property 
        /// </summary>
        public object[] PropertyValues
        {
            get;
            private set;
        }

        public FormattedException(string messageTemplate, params object[] propertyValues) :
            base(MakeExceptionMessage(messageTemplate, propertyValues))
        {
            this.MessageTemplate = messageTemplate;
            this.PropertyValues = propertyValues;
        }

        public FormattedException(Exception innerException, string messageTemplate, params object[] propertyValues) :
            base(MakeExceptionMessage(messageTemplate, propertyValues), innerException)
        {
            this.MessageTemplate = messageTemplate;
            this.PropertyValues = propertyValues;
        }

        /// <summary>
        /// Use serilog to format the error message.
        /// </summary>
        private static string MakeExceptionMessage(string messageTemplate, object[] propertyValues)
        {
            var output = new StringWriter();

            var logger = new LoggerConfiguration()
                .WriteTo.TextWriter(
                    output, 
                    outputTemplate: "{Message}"
                )
                .CreateLogger();

            logger.Write(LogEventLevel.Error, messageTemplate, propertyValues);

            return output.ToString().Trim();
        }
    }
}
