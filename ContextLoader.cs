using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Configurator
{
    /// <summary>Util class helping with loading a configuration context from files.</summary>
    public class ContextLoader
    {
        private static Dictionary<string, IContextLoader> s_dctExtensionLoader;

        static ContextLoader()
        {
            s_dctExtensionLoader = new Dictionary<string, IContextLoader>();
            setLoader("xml", new ContextLoaderXml());
            setLoader("json", new ContextLoaderJson());
        }

        /// <summary>Custom context loaders can be set or replaced here ("xml" and "json" already supported).</summary><param name="fileExtension">e.g. "json".</param><param name="loader">if "null" then will remove the extension.</param>
        public static void setLoader(string fileExtension, IContextLoader loader)
        {
            if (!string.IsNullOrWhiteSpace(fileExtension))
            {
                fileExtension = fileExtension.ToLower();
                if (loader == null && s_dctExtensionLoader.ContainsKey(fileExtension))
                    s_dctExtensionLoader.Remove(fileExtension);
                else
                    s_dctExtensionLoader[fileExtension] = loader;
            }
        }

        /// <summary>Loads and returns a configuration context with the designated loader.</summary><param name="path"></param><param name="loader"></param><returns></returns>
        public static IContext loadContext(string path, IContextLoader loader)
        {
            return !string.IsNullOrWhiteSpace(path) && loader != null ? loader.loadContext(path) : null;
        }

        /// <summary>Creates a loader and subsequently a configuration context which is then returned.</summary><param name="path">Currently supported extensions are "xml" and "json".</param><returns></returns>
        public static IContext loadContext(string path)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                string lpath = path.ToLower();
                foreach (string extension in s_dctExtensionLoader.Keys)
                {
                    if (lpath.EndsWith("." + extension))
                        return loadContext(path, s_dctExtensionLoader[extension]);
                }
            }
            return null;
        }
    }
}
