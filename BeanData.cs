using System.Collections.Generic;

namespace Configurator
{
    /// <summary>Contains all information about a configuration bean.</summary>
    public class BeanData
    {
        /// <summary>Contains all information about a configuration bean's property.</summary>
        public class BeanProperty
        {
            /// <summary>Contains all information about a configuration bean's property's collection.</summary>
            public class BeanValueCollection
            {
                /// <summary>Bean property collection's type.</summary>
                public enum BeanValueCollectionType
                {
                    /// <summary>Shouldn't happen.</summary>
                    BVCT_UNDEFINED,
                    /// <summary>Native array.</summary>
                    BVCT_ARRAY,
                    /// <summary>List.</summary>
                    BVCT_LIST,
                    /// <summary>Dictionary.</summary>
                    BVCT_MAP,
                    /// <summary>Set.</summary>
                    BVCT_SET
                }

                /// <summary>Collection type.</summary>
                public BeanValueCollectionType type = BeanValueCollectionType.BVCT_UNDEFINED;
                /// <summary>Merge with parent or not.</summary>
                public bool col_merge = false;
                /// <summary>Collection key type.</summary>
                public string col_class_key = null;
                /// <summary>Collection value type.</summary>
                public string col_class_value = null;
                /// <summary>Collection values.</summary>
                public List<BeanProperty> collection = new List<BeanProperty>();
            }

            /// <summary>Bean property's type.</summary>
            public enum BeanPropertyType
            {
                /// <summary>Shouldn't happen.</summary>
                BPT_UNDEFINED,
                /// <summary>Simple type e.g. int, string.</summary>
                BPT_SIMPLE,
                /// <summary>Is reference to another bean.</summary>
                BPT_REFERENCE,
                /// <summary>Is a nested bean.</summary>
                BPT_BEAN,
                /// <summary>Is a collection.</summary>
                BPT_COLLECTION
            }

            /// <summary>Bean property name used for setting value to instance.</summary>
            public string name = null;
            /// <summary>Bean property type used for setting value to instance.</summary>
            public BeanPropertyType type = BeanPropertyType.BPT_UNDEFINED;
            /// <summary>Bean property value used for setting value to instance.</summary>
            public object value = null;
        }

        /// <summary>Bean's scope type.</summary>
        public enum BeanScopeType
        {
            /// <summary>Create new instance on any inflate request.</summary>
            BST_PROTOTYPE,
            /// <summary>Create once and return same instance on any inflate request.</summary>
            BST_SINGLETON
        }

        /// <summary>Used as bean reference (can be "null" in nested anonymous beans).</summary>
        public string id = null;
        /// <summary>If not null will use as reference to find the parent bean.</summary>
        public string id_parent = null;
        /// <summary>If true won't allow inflating of that bean.</summary>
        public bool is_abstract = false;
        /// <summary>Used to find a type for inflating the instance.</summary>
        public string clss = null;
        /// <summary>Whether to inflate once or on every request.</summary>
        public BeanScopeType scope = BeanScopeType.BST_SINGLETON;
        /// <summary>Bean's properties.</summary>
        public List<BeanProperty> props = null;
        /// <summary>Bean's optional property used for factory i.e. custom instantiation.</summary>
        public BeanProperty factory = null;
        /// <summary>For singleton scope holds the instance to always return.</summary>
        public object beanSingletonInstance = null;
        /// <summary>For simple bean types used to directly assign the value.</summary>
        public object valueAssign = null;

        /// <summary>Returns property or null by "name".</summary>
        public BeanProperty getProperty(string name) {
            return getProperty(props, name);
        }

        /// <summary>Returns property or null from "props" by "name".</summary>
        static public BeanProperty getProperty(List<BeanProperty> props, string name) {
            if (props != null) {
                foreach (BeanProperty prop in props) {
                    if (prop.name == name)
                        return prop;
                }
            }
            return null;
        }
    }
}
