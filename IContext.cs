using System;

namespace Configurator
{
    /// <summary>This is the interface for the configuration context responsible for holding beans and inflating instances.</summary>
    public interface IContext
    {
        /// <summary>Adds a configuration bean to the context.</summary><param name="bean"></param>
        void addBean(BeanData bean);
        /// <summary>Adds a property to a bean.</summary><param name="bean"></param><param name="property"></param>
        void addBeanProperty(BeanData bean, BeanData.BeanProperty property);
        /// <summary>Adds a factory property to a bean.</summary><param name="bean"></param><param name="property"></param>
        void addBeanFactory(BeanData bean, BeanData.BeanProperty property);
        /// <summary>Registers a bean in the context.</summary><param name="bean"></param>
        void registerBeanWithId(BeanData bean);
        /// <summary>Returns the bean with "is" from current context.</summary><param name="id"></param><returns></returns>
        BeanData getBean(string id);
        /// <summary>Creates and sets all properties of the bean referenced by "id".</summary><param name="id"></param><returns></returns>
        object inflate(string id);
        /// <summary>Util function for how to find a certain type.</summary><param name="clss"></param><returns></returns>
        Type findClass(string clss);
    }
}
