using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Configurator
{
    /// <summary>This is the interface for the configuration context responsible for holding beans and inflating instances.</summary>
    public class Context: IContext
    {
        private List<BeanData> m_colBeans = new List<BeanData>();
        private Dictionary<string, BeanData> m_dctBeans = new Dictionary<string, BeanData>();

        internal static void Log(object context, string log) {
            Console.Out.WriteLine(DateTime.UtcNow.ToString("hh:mm:ss") + " Configurator" + (context == null ? "" : " " + context.GetType().Name) + ": " + log);
        }

        /// <summary>Util functions for finding a type at runtime.</summary><param name="clss">e.g. "int", "System.Int64", "Configurator.Context, Configurator"</param><returns></returns>
        public static Type findType(string clss) {
            if (string.IsNullOrWhiteSpace(clss))
                return null;
            Type clazz = Type.GetType(clss);
            if (clazz == null) {
                if (clss == "bool")
                    clazz = typeof(bool);
                else if (clss == "byte")
                    clazz = typeof(byte);
                else if (clss == "char")
                    clazz = typeof(char);
                else if (clss == "short")
                    clazz = typeof(short);
                else if (clss == "int")
                    clazz = typeof(int);
                else if (clss == "long")
                    clazz = typeof(long);
                else if (clss == "ushort")
                    clazz = typeof(ushort);
                else if (clss == "uint")
                    clazz = typeof(uint);
                else if (clss == "ulong")
                    clazz = typeof(ulong);
                else if (clss == "float")
                    clazz = typeof(float);
                else if (clss == "double")
                    clazz = typeof(double);
                else if (clss == "string")
                    clazz = typeof(string);
                else if (clss == "object")
                    clazz = typeof(object);
            }
            while (clazz == null && clss.IndexOf(".") > 0 && (clss.IndexOf(",") < 0 || clss.IndexOf(",") > clss.IndexOf("."))) {
                string suffix = "";
                if (clss.IndexOf(",") > 0) {
                    suffix = clss.Substring(clss.IndexOf(","));
                    clss = clss.Substring(0, clss.IndexOf(","));
                }
                string[] classes = clss.Split(new char[] { '.' });
                clss = null;
                for (int ii = 0; ii < classes.Length; ++ii) {
                    clss += (clss == null ? "" : ii < classes.Length - 1 ? "." : "+") + classes[ii];
                }
                clss += suffix;
                clazz = Type.GetType(clss);
            }
            return clazz;
        }

        /// <summary>Adds or replaces a property to/in a bean.</summary><param name="bean"></param><param name="property"></param>
        public static void addReplaceBeanProperty(BeanData bean, BeanData.BeanProperty property) {
            if (bean != null && property != null) {
                if (bean.props == null)
                    bean.props = new List<BeanData.BeanProperty>();
                for (int ii = bean.props.Count - 1; ii >= 0; --ii) {
                    if (bean.props[ii].name == property.name)
                        bean.props.RemoveAt(ii);
                }
                bean.props.Add(property);
            }
        }

        /// <summary>Adds or replaces the factory property.</summary><param name="bean"></param><param name="property"></param>
        public static void addReplaceBeanFactory(BeanData bean, BeanData.BeanProperty property) {
            if (bean != null) {
                bean.factory = property;
            }
        }

        /// <summary>Adds the bean to context, throws exception if bean.id already added.</summary><param name="bean"></param>
        public void addBean(BeanData bean) {
            if (bean != null) {
                if (string.IsNullOrWhiteSpace(bean.id)) {
                    m_colBeans.Add(bean);
                } else {
                    registerBeanWithId(bean);
                }
            }
        }

        /// <summary>Adds the bean to context, throws exception if bean.id already added.</summary><param name="bean"></param>
        public void registerBeanWithId(BeanData bean) {
            if (bean != null && !string.IsNullOrWhiteSpace(bean.id)) {
                if (m_dctBeans.ContainsKey(bean.id))
                    throw new ConfiguratorException("Bean with id '" + bean.id + "' already registered.");
                else
                    m_dctBeans.Add(bean.id, bean);
            }
        }

        /// <summary>Adds or replaces a property to/in a bean.</summary><param name="bean"></param><param name="property"></param>
        public void addBeanProperty(BeanData bean, BeanData.BeanProperty property) {
            addReplaceBeanProperty(bean, property);
        }

        /// <summary>Adds or replaces the factory property.</summary><param name="bean"></param><param name="property"></param>
        public void addBeanFactory(BeanData bean, BeanData.BeanProperty property) {
            addReplaceBeanFactory(bean, property);
        }

        /// <summary>Returns bean from context or null if not found (no inflation of instances).</summary><param name="id"></param><returns></returns>
        public BeanData getBean(string id) {
            return m_dctBeans.ContainsKey(id) ? m_dctBeans[id] : null;
        }

        /// <summary>Creates (or finds if "singleton" scope) and returns the bean's corresponding instance.</summary><param name="id"></param><returns></returns>
        public object inflate(string id) {
            BeanData bean = getBean(id);
            if (bean == null)
                throw new ConfiguratorException("No such bean registered: '" + id + "'.");
            return inflate(bean);
        }

        private object inflate(BeanData bean) {
            if (bean.scope == BeanData.BeanScopeType.BST_SINGLETON && bean.beanSingletonInstance != null)
                return bean.beanSingletonInstance;
            object instance = null;
            string infoTag = string.IsNullOrWhiteSpace(bean.id) ? bean.clss : bean.id;
            if (bean.is_abstract)
                throw new ConfiguratorException("Bean '" + infoTag + "' is defined as abstract and can't be inflated.");
            if (!string.IsNullOrWhiteSpace(bean.id_parent) && getBean(bean.id_parent) == null)
                throw new ConfiguratorException("No such bean registered: '" + bean.id_parent + "'.");
            string clss = getBeanClass(bean);
            if (string.IsNullOrWhiteSpace(clss))
                throw new ConfiguratorException("Bean '" + infoTag + "' class undefined.");
            // SEARCH CLASS
            Type clazz = null;
            try {
                clazz = findClass(clss);
            } catch (Exception ex) {
                throw new ConfiguratorException("Exception while inflating bean '" + infoTag + "'.", ex);
            }
            if (clazz == null)
                throw new ConfiguratorException("Bean '" + clss + "' class unresolvable (don't forget to use assembly name i.e. 'NAMESPACE.CLASS+INNER, ASSEMBLY').");
            try {
                if (bean.valueAssign != null)
                    instance = Context.convertValue(clazz, bean.valueAssign);
                else
                    instance = createInstance(infoTag, bean, clazz);
                if (instance == null)
                    throw new ConfiguratorException("Bean '" + clss + "' instantiating failed.");
                else if (!clazz.GetTypeInfo().IsAssignableFrom(instance.GetType().GetTypeInfo()))
                    throw new ConfiguratorException("Bean '" + clss + "' instantiating failed, instead returned type '" + instance.GetType().Name + "'.");
                if (!inflateProperties(bean, instance, new List<string>())) {
                    instance = null;
                    throw new ConfiguratorException("Bean '" + clss + "' setting properties failed.");
                }
            } catch (Exception ex) {
                if (ex.GetType() == typeof(ConfiguratorException))
                    throw ex;
                else
                    throw new ConfiguratorException("Exception while instantiating bean '" + infoTag + "'.", ex);
            }

            if (instance != null && bean.scope == BeanData.BeanScopeType.BST_SINGLETON)
                bean.beanSingletonInstance = instance;

            return instance;
        }

        private object createInstance(string infoTag, BeanData bean, Type clazz) {
            object ret = null;
            if (bean.factory == null) {
                ret = Activator.CreateInstance(clazz);
            } else {
                var typeInfo = clazz.GetTypeInfo();
                BeanData.BeanProperty.BeanValueCollection parms = bean.factory.value == null ? null : bean.factory.value as BeanData.BeanProperty.BeanValueCollection;
                object[] vals = parms == null ? null : new object[parms.collection.Count];
                if (parms != null && parms.collection.Count > 0) {
                    for (int ii = 0; ii < parms.collection.Count; ++ii) {
                        vals[ii] = getPropertyValue(bean, parms.collection[ii]);
                    }
                }

                MethodBase methodFirstPerfect = null;
                MethodBase methodFirstOk = null;
                object[] valsPerfect = null;
                object[] valsOk = null;
                if (string.IsNullOrEmpty(bean.factory.name)) {
                    findAppropriateMethod(false, null, typeInfo.DeclaredConstructors.ToArray(), vals, out methodFirstPerfect, out valsPerfect, out methodFirstOk, out valsOk);
                    MethodBase methodUse = methodFirstPerfect != null ? methodFirstPerfect : methodFirstOk;
                    if (methodUse != null) {
                        try { ret = (methodUse as ConstructorInfo).Invoke(methodUse == methodFirstPerfect ? valsPerfect : valsOk); } catch (Exception) { }
                    }
                } else {
                    findAppropriateMethod(true, bean.factory.name, clazz.GetRuntimeMethods().ToArray(), vals, out methodFirstPerfect, out valsPerfect, out methodFirstOk, out valsOk);
                    MethodBase methodUse = methodFirstPerfect != null ? methodFirstPerfect : methodFirstOk;
                    if (methodUse != null) {
                        try { ret = (methodUse as MethodInfo).Invoke(null, methodUse == methodFirstPerfect ? valsPerfect : valsOk); } catch (Exception) { }
                    }
                }
            }
            return ret;
        }

        /// <summary>Tries to find an appropriate method.</summary>
        /// <param name="bStaticMethods"></param>
        /// <param name="optNameMethod"></param>
        /// <param name="methods"></param>
        /// <param name="vals"></param>
        /// <param name="methodFirstPerfect"></param>
        /// <param name="valsPerfect"></param>
        /// <param name="methodFirstOk"></param>
        /// <param name="valsOk"></param>
        public static void findAppropriateMethod(bool bStaticMethods, string optNameMethod, MethodBase[] methods, object[] vals, out MethodBase methodFirstPerfect, out object[] valsPerfect, out MethodBase methodFirstOk, out object[] valsOk) {
            methodFirstPerfect = null;
            methodFirstOk = null;
            valsPerfect = null;
            valsOk = null;
            for (int ii = 0; (methodFirstOk == null || methodFirstPerfect == null) && ii < methods.Length; ++ii) {
                MethodBase method = methods[ii];
                if (method.GetParameters().Length == (vals == null ? 0 : vals.Length) && method.IsStatic == bStaticMethods && (string.IsNullOrEmpty(optNameMethod) || method.Name == optNameMethod)) {
                    bool bPerfect = true;
                    object[] valsConverted = new object[vals.Length];
                    for (int jj = 0; jj < vals.Length; ++jj) {
                        object value = vals[jj];
                        Type typeParam = method.GetParameters()[jj].ParameterType;
                        if (value == null || typeParam.GetTypeInfo().IsAssignableFrom(value.GetType().GetTypeInfo())) {
                            valsConverted[jj] = value;
                        } else {
                            valsConverted[jj] = Context.convertValue(typeParam, value);
                            bPerfect = false;
                        }
                    }
                    if (methodFirstOk == null) {
                        methodFirstOk = method;
                        valsOk = valsConverted;
                    }
                    if (bPerfect && methodFirstPerfect == null) {
                        methodFirstPerfect = method;
                        valsPerfect = valsConverted;
                    }
                }
            }
        }

        private bool inflateProperties(BeanData bean, object instance, List<string> ignoreProperties) {
            bool bRet = true;
            if (bean != null && instance != null) {
                string infoTag = string.IsNullOrWhiteSpace(bean.id) ? bean.clss : bean.id;
                for (int ii = 0; bRet && bean.props != null && ii < bean.props.Count; ++ii) {
                    BeanData.BeanProperty prop = bean.props[ii];
                    if (string.IsNullOrWhiteSpace(prop.name))
                        throw new ConfiguratorException("Bean '" + infoTag + "' has unnamed property.");
                    if (ignoreProperties.Contains(prop.name))
                        continue;
                    ignoreProperties.Add(prop.name);
                    object propVal = getPropertyValue(bean, prop);
                    if (!applyProperty(instance, prop.name, propVal))
                        throw new ConfiguratorException("Bean '" + infoTag + "' property '" + prop.name + "' could not be applied.");
                }
                if (bRet && !string.IsNullOrWhiteSpace(bean.id_parent)) {
                    BeanData beanParent = getBean(bean.id_parent);
                    if (beanParent != null)
                        bRet = inflateProperties(beanParent, instance, ignoreProperties);
                }
            }
            return bRet;
        }

        private object getPropertyValue(BeanData bean, BeanData.BeanProperty prop) {
            object ret = null;
            string infoTag = string.IsNullOrWhiteSpace(bean.id) ? bean.clss : bean.id;
            switch (prop.type) {
                case BeanData.BeanProperty.BeanPropertyType.BPT_SIMPLE: {
                        ret = prop.value;
                    }
                    break;
                case BeanData.BeanProperty.BeanPropertyType.BPT_REFERENCE: {
                        if (prop.value == null || string.IsNullOrWhiteSpace(prop.value.ToString()))
                            throw new ConfiguratorException("Bean '" + infoTag + "' property '" + prop.name + "' reference value is empty.");
                        object beanRef = inflate(prop.value.ToString());
                        if (beanRef == null)
                            throw new ConfiguratorException("Bean '" + infoTag + "' property '" + prop.name + "' reference value '" + prop.value.ToString() + "' inflated to null.");
                        ret = beanRef;
                    }
                    break;
                case BeanData.BeanProperty.BeanPropertyType.BPT_BEAN: {
                        if (prop.value == null || prop.value.GetType() != typeof(BeanData))
                            throw new ConfiguratorException("Bean '" + infoTag + "' property '" + prop.name + "' missing nested bean.");
                        object beanNested = inflate(prop.value as BeanData);
                        if (beanNested == null)
                            throw new ConfiguratorException("Bean '" + infoTag + "' property '" + prop.name + "' nested bean inflated to null.");
                        ret = beanNested;
                    }
                    break;
                case BeanData.BeanProperty.BeanPropertyType.BPT_COLLECTION: {
                        BeanData.BeanProperty.BeanValueCollection beanCollection = prop.value as BeanData.BeanProperty.BeanValueCollection;
                        if (beanCollection == null || beanCollection.type == BeanData.BeanProperty.BeanValueCollection.BeanValueCollectionType.BVCT_UNDEFINED)
                            throw new ConfiguratorException("Bean '" + infoTag + "' property '" + prop.name + "' collection value invalid.");
                        List<BeanData.BeanProperty> items = getPropertyCollectionValue(bean, prop.name);
                        if (items == null)
                            throw new ConfiguratorException("Bean '" + infoTag + "' property '" + prop.name + "' collection content couldn't be extracted.");
                        string classKey = beanCollection.col_class_key;
                        string classVal = beanCollection.col_class_value;
                        Type clazzKey = findClass(classKey);
                        Type clazzVal = findClass(classVal);
                        if (clazzKey == null && beanCollection.type == BeanData.BeanProperty.BeanValueCollection.BeanValueCollectionType.BVCT_MAP)
                            throw new ConfiguratorException("Bean '" + infoTag + "' property '" + prop.name + "' collection missing key type definition.");
                        if (clazzVal == null)
                            throw new ConfiguratorException("Bean '" + infoTag + "' property '" + prop.name + "' collection missing value type definition.");
                        object itemsValued = preparePropertyCollectionValue(bean, beanCollection, clazzKey, clazzVal, items);
                        ret = itemsValued;
                    }
                    break;
                case BeanData.BeanProperty.BeanPropertyType.BPT_UNDEFINED:
                default:
                    throw new ConfiguratorException("Bean '" + infoTag + "' property '" + prop.name + "' type unresolved.");
            }
            return ret;
        }

        private object preparePropertyCollectionValue(BeanData bean, BeanData.BeanProperty.BeanValueCollection beanCollection, Type clazzKey, Type clazzValue, List<BeanData.BeanProperty> items) {
            object ret = null;
            string infoTag = string.IsNullOrWhiteSpace(bean.id) ? bean.clss : bean.id;
            switch (beanCollection.type) {
                case BeanData.BeanProperty.BeanValueCollection.BeanValueCollectionType.BVCT_MAP: {
                        Type clazzGeneric = typeof(Dictionary<,>);
                        Type[] clazzParams = { clazzKey, clazzValue };
                        Type clazzConstructed = clazzGeneric.MakeGenericType(clazzParams);
                        object instance = null;
                        try {
                            instance = Activator.CreateInstance(clazzConstructed);
                            if (instance == null)
                                throw new ConfiguratorException("Bean '" + infoTag + "' collection property type '" + clazzConstructed.FullName + "' instantiating failed.");
                        } catch (Exception ex) {
                            if (ex.GetType() == typeof(ConfiguratorException))
                                throw ex;
                            else
                                throw new ConfiguratorException("Bean '" + infoTag + "' exception while instantiating collection property type '" + clazzConstructed.FullName + "'.", ex);
                        }
                        MethodInfo method = clazzConstructed.GetRuntimeMethod("Add", clazzParams);
                        if (method == null)
                            throw new ConfiguratorException("Bean '" + infoTag + "' collection property type '" + clazzConstructed.FullName + "' missing add method.");
                        for (int ii = 0; ii < items.Count; ++ii) {
                            BeanData.BeanProperty property = items[ii];
                            object key = Context.convertValue(clazzKey, property.name);
                            object val = Context.convertValue(clazzValue, getPropertyValue(bean, property));
                            method.Invoke(instance, new object[] { key, val });
                        }
                        ret = instance;
                    }
                    break;
                case BeanData.BeanProperty.BeanValueCollection.BeanValueCollectionType.BVCT_ARRAY:
                case BeanData.BeanProperty.BeanValueCollection.BeanValueCollectionType.BVCT_LIST:
                case BeanData.BeanProperty.BeanValueCollection.BeanValueCollectionType.BVCT_SET: {
                        Type clazzGeneric = beanCollection.type == BeanData.BeanProperty.BeanValueCollection.BeanValueCollectionType.BVCT_ARRAY
                            || beanCollection.type == BeanData.BeanProperty.BeanValueCollection.BeanValueCollectionType.BVCT_LIST ? typeof(List<>) : typeof(HashSet<>);
                        Type[] clazzParams = { clazzValue };
                        Type clazzConstructed = clazzGeneric.MakeGenericType(clazzParams);

                        object instance = null;
                        try {
                            instance = Activator.CreateInstance(clazzConstructed);
                            if (instance == null)
                                throw new ConfiguratorException("Bean '" + infoTag + "' collection property type '" + clazzConstructed.FullName + "' instantiating failed.");
                        } catch (Exception ex) {
                            if (ex.GetType() == typeof(ConfiguratorException))
                                throw ex;
                            else
                                throw new ConfiguratorException("Bean '" + infoTag + "' exception while instantiating collection property type '" + clazzConstructed.FullName + "'.", ex);
                        }
                        MethodInfo method = clazzConstructed.GetRuntimeMethod("Add", clazzParams);
                        if (method == null)
                            throw new ConfiguratorException("Bean '" + infoTag + "' collection property type '" + clazzConstructed.FullName + "' missing add method.");
                        for (int ii = 0; ii < items.Count; ++ii) {
                            BeanData.BeanProperty property = items[ii];
                            object val = Context.convertValue(clazzValue, getPropertyValue(bean, property));
                            method.Invoke(instance, new object[] { val });
                        }
                        if (beanCollection.type == BeanData.BeanProperty.BeanValueCollection.BeanValueCollectionType.BVCT_ARRAY) {
                            MethodInfo methodToArray = clazzConstructed.GetRuntimeMethod("ToArray", new Type[] { });
                            if (methodToArray == null)
                                throw new ConfiguratorException("Bean '" + infoTag + "' collection property type '" + clazzConstructed.FullName + "' missing ToArray method.");
                            instance = methodToArray.Invoke(instance, new object[] { });
                        }
                        ret = instance;
                    }
                    break;
            }
            return ret;
        }

        private List<BeanData.BeanProperty> getPropertyCollectionValue(BeanData bean, string propertyName) {
            BeanData.BeanProperty property = bean.getProperty(propertyName);
            if (property != null && property.type == BeanData.BeanProperty.BeanPropertyType.BPT_COLLECTION && property.value != null) {
                BeanData.BeanProperty.BeanValueCollection bvc = property.value as BeanData.BeanProperty.BeanValueCollection;
                if (bvc != null) {
                    List<BeanData.BeanProperty> colRet = new List<BeanData.BeanProperty>(bvc.collection);
                    if (bvc.col_merge && !string.IsNullOrWhiteSpace(bean.id_parent)) {
                        BeanData beanParent = getBean(bean.id_parent);
                        if (beanParent != null) {
                            List<BeanData.BeanProperty> colParent = getPropertyCollectionValue(beanParent, propertyName);
                            if (colParent != null && colParent.Count > 0) {
                                if (bvc.type == BeanData.BeanProperty.BeanValueCollection.BeanValueCollectionType.BVCT_MAP) {
                                    foreach (BeanData.BeanProperty item in colParent) {
                                        if (!string.IsNullOrWhiteSpace(item.name) && BeanData.getProperty(colRet, item.name) == null)
                                            colRet.Add(item);
                                    }
                                } else {
                                    List<BeanData.BeanProperty> colTemp = new List<BeanData.BeanProperty>(colParent);
                                    colTemp.AddRange(colRet);
                                    colRet = colTemp;
                                }
                            }
                        }
                    }
                    return colRet;
                }
            }
            return null;
        }

        private bool applyProperty(object instance, string name, object value) {
            bool bApplied = false;
            if (instance != null && !string.IsNullOrWhiteSpace(name)) {
                Type clazz = instance.GetType();
                MethodInfo[] methods = clazz.GetRuntimeMethods().ToArray();
                PropertyInfo[] props = clazz.GetRuntimeProperties().ToArray();
                FieldInfo[] fields = clazz.GetRuntimeFields().ToArray();

                string setter = "set" + name.ToUpper()[0] + name.Substring(1);
                for (int ii = 0; !bApplied && ii < methods.Length; ++ii) {
                    MethodInfo method = methods[ii];
                    if ((method.Name == setter || method.Name == setter.ToUpper()[0] + setter.Substring(1)) && method.GetParameters().Length == 1) {
                        Type typeParam = method.GetParameters()[0].ParameterType;
                        if (value == null || typeParam.GetTypeInfo().IsAssignableFrom(value.GetType().GetTypeInfo())) {
                            try {
                                method.Invoke(instance, new object[] { value });
                                bApplied = true;
                            } finally {
                            }
                        } else {
                            object valueNew = Context.convertValue(typeParam, value);
                            if (valueNew != null) {
                                try {
                                    method.Invoke(instance, new object[] { valueNew });
                                    bApplied = true;
                                } finally {
                                }
                            }
                        }
                    }
                }

                setter = name;
                for (int ii = 0; !bApplied && ii < props.Length; ++ii) {
                    PropertyInfo property = props[ii];
                    if (property.Name.ToLower() == setter.ToLower() && property.CanWrite && property.SetMethod != null && property.SetMethod.GetParameters().Length == 1) {
                        MethodInfo method = property.SetMethod;
                        Type typeParam = method.GetParameters()[0].ParameterType;
                        if (value == null || typeParam.GetTypeInfo().IsAssignableFrom(value.GetType().GetTypeInfo())) {
                            try {
                                method.Invoke(instance, new object[] { value });
                                bApplied = true;
                            } finally {
                            }
                        } else {
                            object valueNew = Context.convertValue(typeParam, value);
                            if (valueNew != null) {
                                try {
                                    method.Invoke(instance, new object[] { valueNew });
                                    bApplied = true;
                                } finally {
                                }
                            }
                        }
                    }
                }

                setter = name;
                for (int ii = 0; !bApplied && ii < fields.Length; ++ii) {
                    FieldInfo field = fields[ii];
                    if (field.Name == setter && field.IsPublic) {
                        Type typeParam = field.FieldType;
                        if (value == null || typeParam.GetTypeInfo().IsAssignableFrom(value.GetType().GetTypeInfo())) {
                            try {
                                field.SetValue(instance, value);
                                bApplied = true;
                            } finally {
                            }
                        } else {
                            object valueNew = Context.convertValue(typeParam, value);
                            if (valueNew != null) {
                                try {
                                    field.SetValue(instance, valueNew);
                                    bApplied = true;
                                } finally {
                                }
                            }
                        }
                    }
                }

            }
            return bApplied;
        }

        /// <summary>Tries to convert a value to the preferred type.</summary><param name="prefer"></param><param name="value"></param><returns></returns>
        public static object convertValue(Type prefer, object value) {
            if (prefer != null && value != null) {
                string val = value.ToString();
                if (value is float)
                    val = new Decimal((float)value).ToString(System.Globalization.CultureInfo.InvariantCulture);
                else if (value is double) {
                    Double d = (double)value;
                    val = d.ToString(System.Globalization.CultureInfo.InvariantCulture);
                }
                if (prefer == typeof(string) || prefer == typeof(String))
                    return val;
                else if (prefer == typeof(bool) || prefer == typeof(Boolean))
                    return Boolean.Parse(val);
                else if (prefer == typeof(byte) || prefer == typeof(Byte))
                    return Byte.Parse(val);
                else if (prefer == typeof(char) || prefer == typeof(Char))
                    return Char.Parse(val);
                else if (prefer == typeof(short) || prefer == typeof(Int16))
                    return Int16.Parse(val, System.Globalization.CultureInfo.InvariantCulture);
                else if (prefer == typeof(int) || prefer == typeof(Int32))
                    return Int32.Parse(val, System.Globalization.CultureInfo.InvariantCulture);
                else if (prefer == typeof(long) || prefer == typeof(Int64))
                    return Int64.Parse(val, System.Globalization.CultureInfo.InvariantCulture);
                else if (prefer == typeof(ushort) || prefer == typeof(UInt16))
                    return UInt16.Parse(val, System.Globalization.CultureInfo.InvariantCulture);
                else if (prefer == typeof(uint) || prefer == typeof(UInt32))
                    return UInt32.Parse(val, System.Globalization.CultureInfo.InvariantCulture);
                else if (prefer == typeof(ulong) || prefer == typeof(UInt64))
                    return UInt64.Parse(val, System.Globalization.CultureInfo.InvariantCulture);
                else if (prefer == typeof(float) || prefer == typeof(Single))
                    return Single.Parse(val, System.Globalization.CultureInfo.InvariantCulture);
                else if (prefer == typeof(double) || prefer == typeof(Double))
                    return Double.Parse(val, System.Globalization.CultureInfo.InvariantCulture);
            }
            return value;
        }

        /// <summary>Util function for how to find a certain type.</summary><param name="clss"></param><returns></returns>
        public Type findClass(string clss) {
            return findType(clss);
        }

        private string getBeanClass(BeanData bean) {
            if (!string.IsNullOrWhiteSpace(bean.clss))
                return bean.clss;
            if (!string.IsNullOrWhiteSpace(bean.id_parent)) {
                BeanData beanParent = getBean(bean.id_parent);
                if (beanParent == null)
                    throw new ConfiguratorException("No such bean registered: '" + bean.id_parent + "'.");
                return getBeanClass(beanParent);
            }
            return null;
        }
    }
}
