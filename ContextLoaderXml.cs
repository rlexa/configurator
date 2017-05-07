using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Configurator
{
    /// <summary>This is the main entry point for creating and populating a configuration context from XML.</summary>
    public class ContextLoaderXml: IContextLoader
    {
        /// <summary>Root element, contains imports and beans.</summary>
        public const string XMLTAG_BEANS = "beans";
        /// <summary>Import element, contains recursive file loading info.</summary>
        public const string XMLTAG_IMPORT = "import";
        /// <summary>Configuration bean element, contains information ultimately leading to inflating instances.</summary>
        public const string XMLTAG_BEAN = "bean";
        /// <summary>See "bean". Factory element, indicates custom instance functionality (static function with/without parameters, custom constructors).</summary>
        public const string XMLTAG_FACTORY = "factory";
        /// <summary>See "bean". Property element, contains information about an inflated instance's property.</summary>
        public const string XMLTAG_PROPERTY = "property";
        /// <summary>See "property". Null element, used as value.</summary>
        public const string XMLTAG_NULL = "null";
        /// <summary>See "property". List element, inflated to a List.</summary>
        public const string XMLTAG_LIST = "list";
        /// <summary>See "property". Array element, inflated to a native array.</summary>
        public const string XMLTAG_ARRAY = "array";
        /// <summary>See "property". Set element, inflated to a Set.</summary>
        public const string XMLTAG_SET = "set";
        /// <summary>See "property". Map element, inflated to a Dictionary.</summary>
        public const string XMLTAG_MAP = "map";
        /// <summary>See "list", "array", "set", "map". Item element, inflated to a collection's item.</summary>
        public const string XMLTAG_ITEM = "item";
        /// <summary>See "factory". Param elements, if set will be used to identify the static function if "name" set or a custom constructor.</summary>
        public const string XMLTAG_PARAM = "param";

        /// <summary>See "import". Path to the file to import.</summary>
        public const string XMLATTR_IMPORT_PATH = "path";
        /// <summary>See "import". If "true" the file will be loaded only if it exists (default "false").</summary>
        public const string XMLATTR_IMPORT_OPTIONAL = "optional";
        /// <summary>See "bean". Indicates the bean's identifier.</summary>
        public const string XMLATTR_BEAN_ID = "id";
        /// <summary>See "bean". Identifier of a bean to merge/overwrite.</summary>
        public const string XMLATTR_BEAN_IDMERGE = "id-merge";
        /// <summary>See "bean". Class string to use as type for instance inflation.</summary>
        public const string XMLATTR_BEAN_CLASS = "class";
        /// <summary>See "bean". Identifier of a bean to merge with.</summary>
        public const string XMLATTR_BEAN_PARENT = "parent";
        /// <summary>See "bean". Abstract beans can't be inflated (default "false").</summary>
        public const string XMLATTR_BEAN_ABSTRACT = "abstract";
        /// <summary>See "bean". Whether to create once or on any inflate request (expects "singleton" or "prototype", default "singleton").</summary>
        public const string XMLATTR_BEAN_SCOPE = "scope";
        /// <summary>See "bean". Used for instancing simple types by assigning a value (expects a parseable simple value).</summary>
        public const string XMLATTR_BEAN_ASSIGN = "assign";

        /// <summary>See "scope".</summary>
        public const string XMLVAL_BEAN_SCOPE_PROTOTYPE = "prototype";
        /// <summary>See "scope".</summary>
        public const string XMLVAL_BEAN_SCOPE_SINGLETON = "singleton";

        /// <summary>See "property", "factory". For "property" indicates the key to use for setting the value at runtime. For "factory" indicates the static function.</summary>
        public const string XMLATTR_PROPERTY_NAME = "name";
        /// <summary>See "property", "item". Indicates a simple value to use for setting at runtime</summary>
        public const string XMLATTR_PROPERTY_VALUE = "value";
        /// <summary>See "property". Indicating that the value is a bean targeted by "id".</summary>
        public const string XMLATTR_PROPERTY_REFERENCE = "value-ref";
        /// <summary>See "list", "array", "set", "map". Indicating that the collection is to be merged with parent (default "false").</summary>
        public const string XMLATTR_PROPERTY_COLLECTION_MERGE = "merge";
        /// <summary>See "map". Defines the type of the key.</summary>
        public const string XMLATTR_PROPERTY_COLLECTION_CLASS_KEY = "class-key";
        /// <summary>See "list", "array", "set", "map". Defines the type of the value.</summary>
        public const string XMLATTR_PROPERTY_COLLECTION_CLASS_VALUE = "class-value";
        /// <summary>See "item". Indicates the value to use for setting at runtime</summary>
        public const string XMLATTR_PROPERTY_KEY = "key";

        private class CurrentConfiguration
        {
            public string strCurPath = null;
            public HashSet<string> colPaths = new HashSet<string>();
        }

        /// <summary>
        /// Creates and returns a configuration context from a file.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public IContext loadContext(string path) {
            return string.IsNullOrWhiteSpace(path) ? null : mergeContext(path, new Context(), null);
        }

        /// <summary>
        /// Merges a configuration context with a file.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public IContext loadContext(IContext context, string path) {
            if (!string.IsNullOrWhiteSpace(path))
                mergeContext(path, context, null);
            return context;
        }

        private IContext mergeContext(string path, IContext context, CurrentConfiguration cfg) {
            Context.Log(this, "loading '" + path);
            XDocument doc = null;
            try {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read)) {
                    using (var reader = new StreamReader(stream, System.Text.Encoding.UTF8)) {
                        doc = XDocument.Parse(reader.ReadToEnd());
                    }
                }
            } catch (FileNotFoundException) {
                throw;
            } catch (Exception ex) {
                throw new ConfiguratorException("Parsing XML threw an exception.", ex);
            }

            if (cfg == null)
                cfg = new CurrentConfiguration();
            cfg.colPaths.Add(path);
            cfg.strCurPath = path;

            if (doc.Root.Name == XMLTAG_BEANS) {
                mergeContext(cfg, doc.Root, context);
            }

            foreach (var item in doc.Root.Elements()) {
                if (item.Name == XMLTAG_BEANS) {
                    mergeContext(cfg, item, context);
                }
            }

            return context;
        }

        private IContext mergeContext(CurrentConfiguration cfg, XElement xmlBeans, IContext context) {
            foreach (var item in xmlBeans.Elements()) {
                if (item.Name == XMLTAG_IMPORT) {
                    string strPath = null;
                    bool bOptional = false;
                    foreach (var attr in item.Attributes()) {
                        if (attr.Name == XMLATTR_IMPORT_PATH && !string.IsNullOrWhiteSpace(attr.Value)) {
                            strPath = attr.Value;
                        } else if (attr.Name == XMLATTR_IMPORT_OPTIONAL) {
                            bOptional = bool.Parse(attr.Value);
                        }
                    }
                    if (string.IsNullOrWhiteSpace(strPath))
                        throw new ConfiguratorException("Import element: invalid or missing '" + XMLATTR_IMPORT_PATH + "' attribute.");
                    else {
                        if (!Path.IsPathRooted(strPath)) {
                            string strCurDir = Path.GetDirectoryName(cfg.strCurPath);
                            strPath = Path.Combine(strCurDir, strPath);
                        }
                        if (!cfg.colPaths.Contains(strPath)) {
                            if (!bOptional || File.Exists(strPath))
                                mergeContext(strPath, context, cfg);
                            else
                                Context.Log(this, "optional path not loaded, doesn't exist: " + strPath);
                        }
                    }
                } else if (item.Name == XMLTAG_BEAN) {
                    BeanData bean = null;
                    foreach (var attr in item.Attributes()) {
                        if (attr.Name == XMLATTR_BEAN_IDMERGE) {
                            bean = context.getBean(attr.Value as string);
                            break;
                        }
                    }
                    if (bean == null) {
                        bean = new BeanData();
                        context.addBean(bean);
                    }
                    mergeContextBean(item, bean, context);
                }
            }

            return context;
        }

        private IContext mergeContextBean(XElement xmlBean, BeanData bean, IContext context) {
            foreach (var attr in xmlBean.Attributes()) {
                if (attr.Name == XMLATTR_BEAN_ID && !string.IsNullOrWhiteSpace(attr.Value)) {
                    bean.id = attr.Value;
                    context.registerBeanWithId(bean);
                } else if (attr.Name == XMLATTR_BEAN_CLASS && !string.IsNullOrWhiteSpace(attr.Value))
                    bean.clss = attr.Value;
                else if (attr.Name == XMLATTR_BEAN_PARENT && !string.IsNullOrWhiteSpace(attr.Value))
                    bean.id_parent = attr.Value;
                else if (attr.Name == XMLATTR_BEAN_ABSTRACT && !string.IsNullOrWhiteSpace(attr.Value))
                    bean.is_abstract = Boolean.Parse(attr.Value);
                else if (attr.Name == XMLATTR_BEAN_SCOPE && !string.IsNullOrWhiteSpace(attr.Value)) {
                    if (attr.Value == XMLVAL_BEAN_SCOPE_PROTOTYPE)
                        bean.scope = BeanData.BeanScopeType.BST_PROTOTYPE;
                    else if (attr.Value == XMLVAL_BEAN_SCOPE_SINGLETON)
                        bean.scope = BeanData.BeanScopeType.BST_SINGLETON;
                    else
                        throw new ConfiguratorException("Bean scope <" + XMLATTR_BEAN_SCOPE + "> value '" + attr.Value + "' invalid.");
                } else if (attr.Name == XMLATTR_BEAN_ASSIGN) {
                    bean.valueAssign = attr.Value;
                }
            }

            foreach (var prop in xmlBean.Elements()) {
                if (prop.Name == XMLTAG_PROPERTY) {
                    BeanData.BeanProperty property = new BeanData.BeanProperty();
                    mergeContextBeanProperty(prop, property, context);
                    context.addBeanProperty(bean, property);
                } else if (prop.Name == XMLTAG_FACTORY) {
                    BeanData.BeanProperty property = new BeanData.BeanProperty();
                    mergeContextBeanProperty(prop, property, context);
                    BeanData.BeanProperty.BeanValueCollection beanCol = new BeanData.BeanProperty.BeanValueCollection();
                    beanCol.type = BeanData.BeanProperty.BeanValueCollection.BeanValueCollectionType.BVCT_LIST;
                    property.value = beanCol;
                    mergeContextBeanPropertyCollectionValue(prop, beanCol, context);
                    context.addBeanFactory(bean, property);
                }
            }

            return context;
        }

        private IContext mergeContextBeanProperty(XElement xmlProperty, BeanData.BeanProperty property, IContext context) {
            string value = null;
            string valueRef = null;

            foreach (var attr in xmlProperty.Attributes()) {
                if (attr.Name == XMLATTR_PROPERTY_NAME && !string.IsNullOrWhiteSpace(attr.Value))
                    property.name = attr.Value;
                else if (attr.Name == XMLATTR_PROPERTY_VALUE && !string.IsNullOrWhiteSpace(attr.Value))
                    value = attr.Value;
                else if (attr.Name == XMLATTR_PROPERTY_REFERENCE && !string.IsNullOrWhiteSpace(attr.Value))
                    valueRef = attr.Value;
            }

            if (value != null) {
                property.type = BeanData.BeanProperty.BeanPropertyType.BPT_SIMPLE;
                property.value = value;
            } else if (valueRef != null) {
                property.type = BeanData.BeanProperty.BeanPropertyType.BPT_REFERENCE;
                property.value = valueRef;
            } else if (xmlProperty.Elements().Count() == 1) {
                var xmlPropValue = xmlProperty.Elements().First();
                if (xmlPropValue.Name == XMLTAG_NULL) {
                    property.type = BeanData.BeanProperty.BeanPropertyType.BPT_SIMPLE;
                    property.value = null;
                } else if (xmlPropValue.Name == XMLTAG_BEAN) {
                    BeanData beanNew = null;
                    foreach (var attr in xmlPropValue.Attributes()) {
                        if (attr.Name == XMLATTR_BEAN_IDMERGE) {
                            beanNew = context.getBean(attr.Value as string);
                            break;
                        }
                    }
                    if (beanNew == null) {
                        beanNew = new BeanData();
                        context.addBean(beanNew);
                    }

                    property.type = BeanData.BeanProperty.BeanPropertyType.BPT_BEAN;
                    property.value = beanNew;

                    mergeContextBean(xmlPropValue, beanNew, context);
                } else if (xmlPropValue.Name == XMLTAG_LIST || xmlPropValue.Name == XMLTAG_ARRAY || xmlPropValue.Name == XMLTAG_MAP || xmlPropValue.Name == XMLTAG_SET) {
                    BeanData.BeanProperty.BeanValueCollection beanCol = new BeanData.BeanProperty.BeanValueCollection();
                    beanCol.type = xmlPropValue.Name == XMLTAG_LIST ? BeanData.BeanProperty.BeanValueCollection.BeanValueCollectionType.BVCT_LIST
                        : xmlPropValue.Name == XMLTAG_ARRAY ? BeanData.BeanProperty.BeanValueCollection.BeanValueCollectionType.BVCT_ARRAY
                        : xmlPropValue.Name == XMLTAG_SET ? BeanData.BeanProperty.BeanValueCollection.BeanValueCollectionType.BVCT_SET
                        : BeanData.BeanProperty.BeanValueCollection.BeanValueCollectionType.BVCT_MAP;

                    foreach (var attr in xmlPropValue.Attributes()) {
                        if (attr.Name == XMLATTR_PROPERTY_COLLECTION_MERGE && !string.IsNullOrWhiteSpace(attr.Value))
                            beanCol.col_merge = bool.Parse(attr.Value);
                        else if (attr.Name == XMLATTR_PROPERTY_COLLECTION_CLASS_KEY && !string.IsNullOrWhiteSpace(attr.Value))
                            beanCol.col_class_key = attr.Value;
                        else if (attr.Name == XMLATTR_PROPERTY_COLLECTION_CLASS_VALUE && !string.IsNullOrWhiteSpace(attr.Value))
                            beanCol.col_class_value = attr.Value;
                    }

                    property.type = BeanData.BeanProperty.BeanPropertyType.BPT_COLLECTION;
                    property.value = beanCol;
                    mergeContextBeanPropertyCollectionValue(xmlPropValue, beanCol, context);
                }
            }

            return context;
        }

        private IContext mergeContextBeanPropertyCollectionValue(XElement xmlPropertyCollectionValue, BeanData.BeanProperty.BeanValueCollection collection, IContext context) {
            var ii = 0;
            foreach (var prop in xmlPropertyCollectionValue.Elements()) {
                if (prop.Name == XMLTAG_ITEM) {
                    string key = null;
                    if (collection.type != BeanData.BeanProperty.BeanValueCollection.BeanValueCollectionType.BVCT_MAP)
                        key = "" + ii++;

                    foreach (var attr in prop.Attributes()) {
                        if (attr.Name == XMLATTR_PROPERTY_KEY && !string.IsNullOrWhiteSpace(attr.Value) && collection.type == BeanData.BeanProperty.BeanValueCollection.BeanValueCollectionType.BVCT_MAP)
                            key = attr.Value;
                    }

                    if (string.IsNullOrWhiteSpace(key))
                        throw new ConfiguratorException("Map property value missing a key.");

                    BeanData.BeanProperty property = new BeanData.BeanProperty();
                    property.name = key;
                    collection.collection.Add(property);
                    mergeContextBeanProperty(prop, property, context);
                }
            }

            return context;
        }

    }
}
