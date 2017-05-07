using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Configurator
{
    /// <summary>Configurator based exception.</summary>
    public class ConfiguratorException : Exception
    {
        /// <summary>Default.</summary>
        public ConfiguratorException()
            : base()
        {
        }

        /// <summary>Exception constructor.</summary>
        public ConfiguratorException(string message)
            : base(message)
        {
        }

        /// <summary>Exception constructor.</summary>
        protected ConfiguratorException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>Exception constructor.</summary>
        public ConfiguratorException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
