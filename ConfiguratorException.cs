using System;

namespace Configurator
{
    /// <summary>Configurator based exception.</summary>
    public class ConfiguratorException: Exception
    {
        /// <summary>Default.</summary>
        public ConfiguratorException()
            : base() {
        }

        /// <summary>Exception constructor.</summary>
        public ConfiguratorException(string message)
            : base(message) {
        }

        /// <summary>Exception constructor.</summary>
        public ConfiguratorException(string message, Exception innerException)
            : base(message, innerException) {
        }
    }
}
