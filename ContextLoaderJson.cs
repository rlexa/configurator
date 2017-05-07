using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Configurator
{
    /// <summary>This is the main entry point for creating and populating a configuration context from JSON.</summary>
    public class ContextLoaderJson : IContextLoader
    {
        /// <summary>Recursive importing of files (expects a string or an array of strings and/or objects).</summary>
        public const string JSONTAG_IMPORT = "import";
        /// <summary>Root element (expects an array of objects).</summary>
        public const string JSONTAG_BEANS = "beans";
        /// <summary>Identifier of a bean (expects a string).</summary>
        public const string JSONTAG_BEAN_ID = "id";
        /// <summary>Identifier of a bean to merge/overwrite (expects a string).</summary>
        public const string JSONTAG_BEAN_IDMERGE = "id_merge";
        /// <summary>Used to inflate the instance (expects a string).</summary>
        public const string JSONTAG_BEAN_CLASS = "class";
        /// <summary>Indicates a factory to be used for static code and/or for non-default constructors (expects an object).</summary>
        public const string JSONTAG_BEAN_FACTORY = "factory";
        /// <summary>Used for instancing simple types by assigning a value (expects a json simple value).</summary>
        public const string JSONTAG_BEAN_ASSIGN = "assign";
        /// <summary>Identifier of a bean to merge with (expects a string).</summary>
        public const string JSONTAG_BEAN_PARENT = "parent";
        /// <summary>Abstract beans can't be inflated (expects a bool, default "false").</summary>
        public const string JSONTAG_BEAN_ABSTRACT = "abstract";
        /// <summary>Whether to create once or on any inflate request (expects "singleton" or "prototype", default "singleton").</summary>
        public const string JSONTAG_BEAN_SCOPE = "scope";
        /// <summary>Contains all setters for enclosing bean (expects an object). The value can be a simple JSON value or an object for more complex values.</summary>
        public const string JSONTAG_BEAN_PROPERTIES = "properties";
        /// <summary>See "factory". If set will search for that static function (expects a string).</summary>
        public const string JSONTAG_PROPERTY_FACTORY_METHOD = "static";
        /// <summary>See "factory". If set will search for a constructor with set parameters, if "static" is also set will search for a static function (expects an array).</summary>
        public const string JSONTAG_PROPERTY_FACTORY_PARAMS = "params";
        /// <summary>See "import". Path for another file to load (expects a string).</summary>
        public const string JSONTAG_PROPERTY_IMPORT_PATH = "path";
        /// <summary>See "import", "path". If "true" will load the file if found only (expects a bool, default "false").</summary>
        public const string JSONTAG_PROPERTY_IMPORT_OPTIONAL = "optional";
        /// <summary>Indicating a simple property value (expects a simple JSON value).</summary>
        public const string JSONTAG_PROPERTY_VALUE = "value";
        /// <summary>Indicating that the value is a bean targeted by "id" (expects a string).</summary>
        public const string JSONTAG_PROPERTY_REFERENCE = "value-ref";
        /// <summary>Indicating that the value is a bean (expects an object).</summary>
        public const string JSONTAG_PROPERTY_VALUE_BEAN = "value-bean";
        /// <summary>Indicating that the value is a list (expects an array).</summary>
        public const string JSONTAG_PROPERTY_VALUE_LIST = "value-list";
        /// <summary>Indicating that the value is an array (expects an array).</summary>
        public const string JSONTAG_PROPERTY_VALUE_ARRAY = "value-array";
        /// <summary>Indicating that the value is a set (expects an array).</summary>
        public const string JSONTAG_PROPERTY_VALUE_SET = "value-set";
        /// <summary>Indicating that the value is a dictionary (expects an object).</summary>
        public const string JSONTAG_PROPERTY_VALUE_MAP = "value-map";
        /// <summary>See "value-list", "value-array", "value-set", "value-map". Indicating that the collection is to be merged with parent (expects a bool, default "false").</summary>
        public const string JSONTAG_PROPERTY_COLLECTION_MERGE = "merge";
        /// <summary>See "value-map". Defines the type of the key (expects a string).</summary>
        public const string JSONTAG_PROPERTY_COLLECTION_CLASS_KEY = "value-class-key";
        /// <summary>See "value-list", "value-array", "value-set", "value-map". Defines the type of the value (expects a string).</summary>
        public const string JSONTAG_PROPERTY_COLLECTION_CLASS_VALUE = "value-class-value";
        /// <summary>See "scope".</summary>
        public const string JSONVAL_BEAN_SCOPE_PROTOTYPE = "prototype";
        /// <summary>See "scope".</summary>
        public const string JSONVAL_BEAN_SCOPE_SINGLETON = "singleton";

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
        public IContext loadContext(string path)
        {
            return string.IsNullOrWhiteSpace(path) ? null : mergeContext(path, new Context(), null);
        }

        /// <summary>
        /// Merges a configuration context with a file.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public IContext loadContext(IContext context, string path)
        {
            if (!string.IsNullOrWhiteSpace(path))
                mergeContext(path, context, null);
            return context;
        }

        private IContext mergeContext(string path, IContext context, CurrentConfiguration cfg)
        {
            if (cfg == null)
                cfg = new CurrentConfiguration();
            cfg.colPaths.Add(path);
            cfg.strCurPath = path;
            Context.Log(this, "loading '" + cfg.strCurPath);

            Dictionary<string, object> dctJson = null;
            try
            {
                using (System.IO.StreamReader stream = new System.IO.StreamReader(path))
                {
                    using (JsonTextReader rdr = new JsonTextReader(stream))
                    {
                        object ret = parseJson(rdr);
                        dctJson = ret as Dictionary<string, object>;
                    }
                }
            }
            catch (Exception ex) { throw new ConfiguratorException("Parsing JSON threw an exception.", ex); }
            if (dctJson != null)
                mergeContext(cfg, dctJson, context);
            return context;
        }

        private object parseJson(string json)
        {
            using (System.IO.StringReader sr = new System.IO.StringReader(json))
            {
                using (JsonTextReader rdr = new JsonTextReader(sr))
                {
                    return parseJson(rdr);
                }
            }
        }

        private object parseJson(JsonTextReader rdr)
        {
            while (rdr.TokenType == JsonToken.None || rdr.TokenType == JsonToken.Comment)
            {
                if (!rdr.Read())
                    break;
            }
            if (rdr.TokenType == JsonToken.StartObject)
            {
                int curDepth = rdr.Depth;
                Dictionary<string, object> ret = new Dictionary<string, object>();
                while (rdr.Read())
                {
                    if (rdr.Depth <= curDepth && rdr.TokenType == JsonToken.EndObject)
                        break;
                    if (rdr.TokenType == JsonToken.PropertyName)
                    {
                        string key = rdr.Value.ToString();
                        rdr.Read();
                        object val = parseJson(rdr);
                        ret.Add(key, val);
                    }
                }
                return ret;
            }
            else if (rdr.TokenType == JsonToken.StartArray)
            {
                int curDepth = rdr.Depth;
                List<object> ret = new List<object>();
                while (rdr.Read())
                {
                    if (rdr.Depth <= curDepth && rdr.TokenType == JsonToken.EndArray)
                        break;
                    object val = parseJson(rdr);
                    ret.Add(val);
                }
                return ret;
            }

            switch (rdr.TokenType)
            {
                case JsonToken.Float:
                    {
                        Double dd = (double)rdr.Value;
                        return dd.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    }
                case JsonToken.Boolean:
                case JsonToken.Date:
                case JsonToken.Integer:
                case JsonToken.String:
                    return rdr.Value.ToString();
            }

            return null;
        }

        private IContext mergeContext(CurrentConfiguration cfg, Dictionary<string, object> dctJson, IContext context)
        {
            if (dctJson != null)
            {
                if (dctJson.ContainsKey(JSONTAG_BEANS))
                {
                    object beans = dctJson[JSONTAG_BEANS];
                    if (beans != null)
                    {
                        List<object> colJson = beans as List<object>;
                        for (int ii = 0; ii < colJson.Count; ++ii)
                        {
                            object item = colJson[ii];
                            Dictionary<string, object> dctJsonBean = item as Dictionary<string, object>;
                            if (dctJsonBean != null)
                            {
                                if (dctJsonBean.ContainsKey(JSONTAG_IMPORT))
                                {
                                    object imports = dctJsonBean[JSONTAG_IMPORT];
                                    if (imports != null)
                                    {
                                        List<string> colJsonItems = new List<string>();
                                        List<bool> colJsonOptional = new List<bool>();
                                        if (typeof(string) == imports.GetType())
                                        {
                                            colJsonItems.Add(imports as string);
                                            colJsonOptional.Add(false);
                                        }
                                        else
                                        {
                                            List<object> colJsonRaw = imports as List<object>;
                                            for (int jj = 0; jj < colJsonRaw.Count; ++jj)
                                            {
                                                object itemRaw = colJsonRaw[jj];
                                                if (itemRaw == null)
                                                    continue;
                                                if (typeof(string) == itemRaw.GetType())
                                                {
                                                    colJsonItems.Add(itemRaw as string);
                                                    colJsonOptional.Add(false);
                                                }
                                                else
                                                {
                                                    Dictionary<string, object> dctJsonRaw = itemRaw as Dictionary<string, object>;
                                                    if (dctJsonRaw.ContainsKey(JSONTAG_PROPERTY_IMPORT_PATH) && dctJsonRaw[JSONTAG_PROPERTY_IMPORT_PATH] != null
                                                        && typeof(string) == dctJsonRaw[JSONTAG_PROPERTY_IMPORT_PATH].GetType())
                                                    {
                                                        colJsonItems.Add(dctJsonRaw[JSONTAG_PROPERTY_IMPORT_PATH] as string);
                                                        colJsonOptional.Add(dctJsonRaw.ContainsKey(JSONTAG_PROPERTY_IMPORT_OPTIONAL) && dctJsonRaw[JSONTAG_PROPERTY_IMPORT_OPTIONAL] != null
                                                            ? Boolean.Parse(dctJsonRaw[JSONTAG_PROPERTY_IMPORT_OPTIONAL] as string) : false);
                                                    }
                                                }
                                            }
                                        }
                                        for (int jj = 0; jj < colJsonItems.Count; ++jj)
                                        {
                                            string strPath = colJsonItems[jj];
                                            bool bOptional = colJsonOptional[jj];
                                            if (!string.IsNullOrWhiteSpace(strPath))
                                            {
                                                if (!System.IO.Path.IsPathRooted(strPath))
                                                {
                                                    string strCurDir = System.IO.Path.GetDirectoryName(cfg.strCurPath);
                                                    strPath = System.IO.Path.Combine(strCurDir, strPath);
                                                }
                                                if (!cfg.colPaths.Contains(strPath))
                                                {
                                                    if (!bOptional || System.IO.File.Exists(strPath))
                                                        mergeContext(strPath, context, cfg);
                                                    else
                                                        Context.Log(this, "optional path not loaded, doesn't exist: " + strPath);
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    BeanData bean = null;
                                    if (dctJsonBean.ContainsKey(JSONTAG_BEAN_IDMERGE))
                                    {
                                        bean = context.getBean(dctJsonBean[JSONTAG_BEAN_IDMERGE] as string);
                                    }
                                    else
                                    {
                                        bean = new BeanData();
                                        context.addBean(bean);
                                    }
                                    mergeContextBean(dctJsonBean, bean, context);
                                }
                            }
                        }
                    }
                }
            }

            return context;
        }

        private IContext mergeContextBean(Dictionary<string, object> dctJson, BeanData bean, IContext context)
        {
            foreach (string key in dctJson.Keys)
            {
                object val = dctJson[key];
                if (key == JSONTAG_BEAN_ID && !string.IsNullOrWhiteSpace(val as string))
                {
                    bean.id = val as string;
                    context.registerBeanWithId(bean);
                }
                else if (key == JSONTAG_BEAN_CLASS && !string.IsNullOrWhiteSpace(val as string))
                    bean.clss = val as string;
                else if (key == JSONTAG_BEAN_PARENT && !string.IsNullOrWhiteSpace(val as string))
                    bean.id_parent = val as string;
                else if (key == JSONTAG_BEAN_ABSTRACT && !string.IsNullOrWhiteSpace(val as string))
                    bean.is_abstract = Boolean.Parse(val as string);
                else if (key == JSONTAG_BEAN_SCOPE && !string.IsNullOrWhiteSpace(val as string))
                {
                    if (val as string == JSONVAL_BEAN_SCOPE_PROTOTYPE)
                        bean.scope = BeanData.BeanScopeType.BST_PROTOTYPE;
                    else if (val as string == JSONVAL_BEAN_SCOPE_SINGLETON)
                        bean.scope = BeanData.BeanScopeType.BST_SINGLETON;
                    else
                        throw new ConfiguratorException("Bean scope <" + JSONTAG_BEAN_SCOPE + "> value '" + (val as string) + "' invalid.");
                }
                else if (key == JSONTAG_BEAN_PROPERTIES && val != null)
                {
                    Dictionary<string, object> dctJsonProps = val as Dictionary<string, object>;
                    foreach (string prop in dctJsonProps.Keys)
                    {
                        BeanData.BeanProperty property = new BeanData.BeanProperty();
                        property.name = prop;
                        context.addBeanProperty(bean, property);
                        mergeContextBeanPropertyAnon(dctJsonProps[prop], property, context);
                    }
                }
                else if (key == JSONTAG_BEAN_FACTORY && val != null)
                {
                    BeanData.BeanProperty property = new BeanData.BeanProperty();
                    Dictionary<string, object> dctFactoryProps = val as Dictionary<string, object>;
                    if (dctFactoryProps.ContainsKey(JSONTAG_PROPERTY_FACTORY_METHOD) && !string.IsNullOrWhiteSpace(dctFactoryProps[JSONTAG_PROPERTY_FACTORY_METHOD] as string))
                        property.name = dctFactoryProps[JSONTAG_PROPERTY_FACTORY_METHOD] as string;
                    BeanData.BeanProperty.BeanValueCollection beanCol = new BeanData.BeanProperty.BeanValueCollection();
                    beanCol.type = BeanData.BeanProperty.BeanValueCollection.BeanValueCollectionType.BVCT_LIST;
                    property.value = beanCol;
                    if (dctFactoryProps.ContainsKey(JSONTAG_PROPERTY_FACTORY_PARAMS) && dctFactoryProps[JSONTAG_PROPERTY_FACTORY_PARAMS] != null)
                    {
                        List<object> colVals = dctFactoryProps[JSONTAG_PROPERTY_FACTORY_PARAMS] as List<object>;
                        Dictionary<string, object> dctVals = new Dictionary<string, object>();
                        for (int ii = 0; ii < colVals.Count; ++ii)
                            dctVals.Add("" + ii, colVals[ii]);
                        mergeContextBeanPropertyCollectionValue(dctVals, beanCol, context);
                    }
                    context.addBeanFactory(bean, property);
                }
                else if (key == JSONTAG_BEAN_ASSIGN)
                {
                    bean.valueAssign = val;
                }
            }

            return context;
        }

        private IContext mergeContextBeanPropertyAnon(object objJson, BeanData.BeanProperty property, IContext context)
        {
            if (objJson == null || objJson is string
                || objJson is short || objJson is int || objJson is long
                || objJson is float || objJson is double
                || objJson is bool)
            {
                property.type = BeanData.BeanProperty.BeanPropertyType.BPT_SIMPLE;
                property.value = objJson;
            }
            else
            {
                return mergeContextBeanProperty(objJson as Dictionary<string, object>, property, context);
            }
            return context;
        }

        private IContext mergeContextBeanProperty(Dictionary<string, object> dctJson, BeanData.BeanProperty property, IContext context)
        {
            if (dctJson.ContainsKey(JSONTAG_PROPERTY_REFERENCE))
            {
                property.type = BeanData.BeanProperty.BeanPropertyType.BPT_REFERENCE;
                property.value = dctJson[JSONTAG_PROPERTY_REFERENCE] as string;
            }
            else if (dctJson.ContainsKey(JSONTAG_PROPERTY_VALUE))
            {
                property.type = BeanData.BeanProperty.BeanPropertyType.BPT_SIMPLE;
                property.value = dctJson[JSONTAG_PROPERTY_VALUE] as string;
            }
            else if (dctJson.ContainsKey(JSONTAG_PROPERTY_VALUE_BEAN))
            {
                Dictionary<string, object> dctJsonBean = dctJson[JSONTAG_PROPERTY_VALUE_BEAN] as Dictionary<string, object>;
                BeanData beanNew = null;
                if (dctJsonBean.ContainsKey(JSONTAG_BEAN_IDMERGE))
                {
                    beanNew = context.getBean(dctJsonBean[JSONTAG_BEAN_IDMERGE] as string);
                }
                else
                {
                    beanNew = new BeanData();
                    context.addBean(beanNew);
                }
                property.type = BeanData.BeanProperty.BeanPropertyType.BPT_BEAN;
                property.value = beanNew;

                mergeContextBean(dctJsonBean, beanNew, context);
            }
            else if (dctJson.ContainsKey(JSONTAG_PROPERTY_VALUE_LIST) || dctJson.ContainsKey(JSONTAG_PROPERTY_VALUE_ARRAY)
                || dctJson.ContainsKey(JSONTAG_PROPERTY_VALUE_SET) || dctJson.ContainsKey(JSONTAG_PROPERTY_VALUE_MAP))
            {
                BeanData.BeanProperty.BeanValueCollection beanCol = new BeanData.BeanProperty.BeanValueCollection();
                beanCol.type = dctJson.ContainsKey(JSONTAG_PROPERTY_VALUE_LIST) ? BeanData.BeanProperty.BeanValueCollection.BeanValueCollectionType.BVCT_LIST
                    : dctJson.ContainsKey(JSONTAG_PROPERTY_VALUE_ARRAY) ? BeanData.BeanProperty.BeanValueCollection.BeanValueCollectionType.BVCT_ARRAY
                    : dctJson.ContainsKey(JSONTAG_PROPERTY_VALUE_SET) ? BeanData.BeanProperty.BeanValueCollection.BeanValueCollectionType.BVCT_SET
                    : BeanData.BeanProperty.BeanValueCollection.BeanValueCollectionType.BVCT_MAP;
                if (dctJson.ContainsKey(JSONTAG_PROPERTY_COLLECTION_MERGE))
                    beanCol.col_merge = Boolean.Parse(dctJson[JSONTAG_PROPERTY_COLLECTION_MERGE] as string);
                if (dctJson.ContainsKey(JSONTAG_PROPERTY_COLLECTION_CLASS_KEY))
                    beanCol.col_class_key = dctJson[JSONTAG_PROPERTY_COLLECTION_CLASS_KEY] as string;
                if (dctJson.ContainsKey(JSONTAG_PROPERTY_COLLECTION_CLASS_VALUE))
                    beanCol.col_class_value = dctJson[JSONTAG_PROPERTY_COLLECTION_CLASS_VALUE] as string;

                property.type = BeanData.BeanProperty.BeanPropertyType.BPT_COLLECTION;
                property.value = beanCol;

                Dictionary<string, object> dctVals = null;
                if (dctJson.ContainsKey(JSONTAG_PROPERTY_VALUE_LIST) || dctJson.ContainsKey(JSONTAG_PROPERTY_VALUE_ARRAY) || dctJson.ContainsKey(JSONTAG_PROPERTY_VALUE_SET))
                {
                    List<object> colVals = dctJson[dctJson.ContainsKey(JSONTAG_PROPERTY_VALUE_LIST) ? JSONTAG_PROPERTY_VALUE_LIST
                        : dctJson.ContainsKey(JSONTAG_PROPERTY_VALUE_ARRAY) ? JSONTAG_PROPERTY_VALUE_ARRAY : JSONTAG_PROPERTY_VALUE_SET] as List<object>;
                    dctVals = new Dictionary<string, object>();
                    for (int ii = 0; ii < colVals.Count; ++ii)
                        dctVals.Add("" + ii, colVals[ii]);
                }
                else
                    dctVals = dctJson[JSONTAG_PROPERTY_VALUE_MAP] as Dictionary<string, object>;
                mergeContextBeanPropertyCollectionValue(dctVals, beanCol, context);
            }

            return context;
        }

        private IContext mergeContextBeanPropertyCollectionValue(Dictionary<string, object> dctJson, BeanData.BeanProperty.BeanValueCollection collection, IContext context)
        {
            foreach (string key in dctJson.Keys)
            {
                BeanData.BeanProperty property = new BeanData.BeanProperty();
                property.name = key;
                collection.collection.Add(property);
                mergeContextBeanPropertyAnon(dctJson[key], property, context);
            }

            return context;
        }
    }
}
