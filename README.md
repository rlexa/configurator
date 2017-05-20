# Configurator Project (C#)

The Configurator project provides configuration based approach for C# applications using data configuration concepts very similar to Java Spring framework.

## Introduction
The Configurator framework provides the possibility to use configuration based approach for software development. Configuration based approach benefits any project in multiple ways e.g.:

- **Configurability**
	- moving hard coded values to configuration
	- data inheritance (easy replication of a unit with multiple properties to child units with individual values)
- **Availability**
	- on any configuration change an application just has to be restarted instead of recompiled
- **Transparency**
	- configuration data is open, machine readable and to some extent human readable
	- moving configuration values from e.g. a DB to configuration files provides versioned file history
- **Flexibility**
	- code is typically written in much more stable and functional way if configuration based approach is used (less singletons, more flexible code etc.)
	- deployment can become much easier to handle due to configuration values like authorization data being moved to dedicated configuration files
- **Testing**
	- input data for unit/integration testing is much easier to maintain when loaded from configuration files
etc.

## Concepts
The goal is to create a **framework for C# applications** which supports **flexible configuration** providing functionality starting with **configuring at runtime** constant values and all the way to loading all **code modules directly from configuration**. Following concepts are implemented in order to achieve this goal.

### Data Based Configuration
The data structures has to support data-driven logic up unitl the last possible moment. This means that configuration data inheritance is to be perceived on data level and not on runtime type level.

### Configuration Files
The **configuration data is saved in files** (currently JSON or XML) which are loaded at runtime. Used throughout the application this eliminates the dangers of hardcoded values and makes the setup values very transparent. Furthermore the configuration can be adjusted at latest possible time which makes it very **easy for deployment engineers to setup values** without interfering with developers. Additionally, as opposed to setting the values in a database, the editing of base (pre-deployment change) configuration files and checking those into the **versioning system provides change history**. The loading context is structured in a way that allows for **loading of multiple files** providing additional modularization and transparency of the configuration data itself.
The loading of configuration files is done via following code:

    IContext context = ContextLoader.loadContext(strPath);

With that the configuration context is loaded including all other importes files (references in the root file) and can be used to inflate runtime modules. Typically a root file would look like this:

...XML...

    <?xml version="1.0" encoding="utf-8" ?>
    <beans>
        <import path="..."/>
        ...
        <bean id=...>
            <property name=...>
            ...
        </bean>
        ...
    </beans>

...JSON...

    {
        "beans": [
            { "import": ... },
            { "id": ... },
            ...
        ]
    }

The configuration modules defined in the files are called “beans” in the configuration which is not exactly the same as Java beans – it just shows that Java Spring framework was used as reference for implementation.

### Inflating Modules
Loading of actual runtime type instances is done via inflating configuration modules referenced by an ID. The loading of configuration context does not create any instances. **Runtime type instances are created explicitly at inflating time**:

...C#...

	namespace FooTest
	{
		public class FooClass { ... }
	}

...XML...

	<bean id="foo1" "scope"="prototype" class="FooTest.FooClass, Foo">...</bean>

...JSON...

	{ "id": "foo1", "scope": "prototype", "class": "FooTest.FooClass, Foo", ... }

...C#...

	IContext context = ContextLoader.loadContext(strPath);
	FooClass oInstance = context.inflate("foo1") as FooClass;

The FooClass instance and all possibly therein referenced configuration modules in the example is created at inflate time, not at loadContext time. The “scope” attribute is “singleton” by default and can be set to “prototype” – a “singleton” scope module will be created just once and any inflate try thereafter will return the already created instance, whereas “prototype” scope leads to creating a new instance everytime.

### Configuration via C# Reflection
The configuration data in the files **targets actual C# setter functions, properties and public members**. Furthermore **simple type variables can be configured** and referenced throughout the configuration context.

...C#...

	public class FooClass
	{
	    public string name;
	    private int m_iValue;
	    private float m_fValue;
	    public void setPrivateValue(int value) { m_iValue = value; }
	    public float FloatProperty { get { return m_fValue; } set { m_fValue= value; } }
	}

While inflating an instance of the FooClass type following configuration keys can be used to setup the module: “name”, “PrivateValue” and “FloatProperty”:

...XML...

	<bean id="foo1">
	    <property name="name" value="I am Foo"/>
	    <property name="PrivateValue" value="123"/>
	    <property name="FloatProperty" value="1.23"/>
	</bean>

...JSON...

	{
	    "id": "foo1",
	    "properties":
	    {
	        "name": "I am Foo",
	        "PrivateValue": 123,
	        "FloatProperty": 1.23
	    }
	}

For any given key the inflation **context tries to search for a public member of that name, a property providing a setter or a public function prepending the key with “set” or “Set”**.
**Simple type values like int, float, string etc. can also be configured** in which case the configured value is assigned at inflate time:

...XML...

	<bean id="important_const" class="string" assign="imastring!" />

...JSON...

	{ "id": "important_const", "class": "string", "assign": "imastring!" }

Additionally it is **possible to target static factory functions** for configuring singletons:

...C#...

	namespace FooTest
	{
	    public class FooClass1
	    {
	        public static FooClass1 GetInstance() { ... }
	    }
	    public class FooClass2
	    {
	        public static FooClass2 GetInstance(string param1) { ... }
	    }
	}

...XML...

	<bean id="singleton1" class="FooTest.FooClass1, Foo">
		<factory name="GetInstance" />
	</bean>
	<bean id="singleton2" class="FooTest.FooClass2, Foo">
		<factory name="GetInstance">
			<item value="somevalue" />
		</factory>
	</bean>

...JSON...

	{
		"id": "singleton1",
		"class": "FooTest.FooClass1, Foo",
		"factory": { "static": "GetInstance" }
	},
	{
		"id": "singleton2",
		"class": "FooTest.FooClass2, Foo",
		"factory": { "static": "GetInstance", "params": [ "somevalue" ] }
	}

In case a default **constructor is missing a constructor with arguments can also be targeted** from configuration:

...XML...

	<bean id="foo1" ...>
		<factory><item value="constructor_param_value"/></factory>
	</bean>

...JSON...

	{ "id": "foo1", ..., "factory": {"params": [ "constructor_param_value" ]} }

### Collections
Following collection types are supported in configuration context: “map” (Dictionary), “list” (List), “set” (HashSet) and “array” (native array). For maps the key type has to be provided in configuration and for every collection the value type has to be provided. The collections also support merging on inheritance (see later).

...C#...

	public class FooClass
	{
		...
	    public Dictionary<int, string> dct = null;
	    public List<double> lst = null;
	    public long[] arr = null;
	    public HashSet<short> set = null;
		...
	}

...XML...

	<bean ...>
        <property name="dct">
			<map class-key="int" class-value="string">
		        <item key="10" value="ten"/>
		        <item key="11" value="eleven"/>
            </map>
        </property>
        <property name="lst">
            <list class-value="double">
                <item value="11.1"/>
                <item value="22.2"/>
            </list>
        </property>
        <property name="arr">
            <array class-value="long">
                <item value="123"/>
                <item value="234"/>
            </array>
        </property>
        <property name="set">
            <set class-value="short">
                <item value="1"/>
                <item value="2"/>
            </set>
        </property>
    </bean>

...JSON...

	{
		...
	    "properties": {
	        "dct": {
		        "value-class-key": "int",
		        "value-class-value": "string",
		        "value-map": { "10": "ten", "11": "eleven" }
	        },
	        "lst": {
		        "value-class-value": "double",
		        "value-list": [ 11.1, 22.2 ]
	        },
	        "array": {
		        "value-class-value": "long",
		        "value-array": [ 123, 234 ]
	        },
	        "set": {
		        "value-class-value": "short",
		        "value-list": [ 1, 2 ]
	        }
        }
    }

### Data Referencing

Apart from anonymous nested modules used as values all other modules should have the “id” attribute set. This attribute is used throughout configuration and code to inflate and reference the module.

...XML...

	<bean id="Math" ... ><property name="pi" value="3.14"/></bean>

...JSON...

	{ "id"="Math", ... "properties": {"pi":3.14} }

The module in the example can now be referenced by the ID “Math”. On parsing another module with the same ID as an existing one an exception will be thrown, except when excplicitly importing another module with the same ID for overwriting in which case “id-merge” attribute has to be used.
Module referencing also opens up more possibilities for setting configuration values:

...XML...

	<bean id="Math" class="Some.Namespace.Math, SomeAssembly">
		<property name="pi" value="3.14"/>
	</bean>
	<bean id="MathModuleHolder" ...>
	    <property name="MathModules">
	        <map class-key="string" class-value="Some.Namespace.Math, SomeAssembly">
	            <item key="VanillaWorld" value-ref="Math"/>
	            <item key="CrazyWorld">
		            <bean class="Some.Namespace.Math, SomeAssembly">
			            <property name="pi" value="4.13"/>
					</bean>
				</item>
	            <item key="SlightlyCrazyWorld">
		            <bean parent="Math">
			            <property name="pi" value="1.34"/>
			        </bean>
			    </item>
	            <item key="NullWorld"></null></item>
            </map>
	    </property>
	</bean>

...JSON...

	{
		"id": "Math",
		"class": "Some.Namespace.Math, SomeAssembly",
		"properties": { "pi": 3.14, "euler": 2.72 }
	}
	{
	    "id": "MathModuleHolder",
	    ...
	    "properties": {
	        "MathModules": {
	            "value-class-key": "string",
	            "value-class-value": "Some.Namespace.Math, SomeAssembly",
	            "value-map": {
	                "VanillaWorld": { "value-ref": "Math" },
	                "CrazyWorld": {
		                "value-bean": {
			                "class": "Some.Namespace.Math, SomeAssembly",
			                "properties": {"pi": 4.13, "euler": 7.27}
		                }
	                },
	                "SlightlyCrazyWorld": {
		                "value-bean": {
			                "parent": "Math",
			                "properties": {"pi": 1.34}
		                }
	                },
	                "NullWorld": null
	            }
	        }
	    }
	}

As shown in the example, the framework supports nested referencing and creation of modules (which is the only place anonymous modules would be useful). In this case when ID “MathModuleHolder” is inflated it will have 4 entries in its dictionary “MathModules” where all 4 of them were set using different techniques; a reference to an already defined module, a creation of an anonymous module, an inheritance module and a NULL value.

### Data Inheritance
The framework provides inheritance concepts on data level supporting configuration of multiple similar data modules with minimal work:

...XML...

	<bean id="ACar" abstract="true">
	    <property name="type" value="Combi"/>
	    <property name="model"></null></property>
	    <property name="features">
		    <list class-value="string">
		        <item value="Steering Wheel"/>
		        <item value="Motor"/>
		    </list>
	    </property>
	</bean>
	<bean id="CarModel1" parent="ACar">
	    <property name="model" value="HC123" />
	    <property name="features">
		    <list merge="true">
		        <item value="Seat Heating"/>
		    </list>
	    </property>
	</bean>

...JSON...

	{
		"id": "ACar",
		"abstract": true,
		"properties": {
			"type": "Combi",
			"model": null,
			"features": {
				"value-class-value": "string",
				"value-list": ["Steering Wheel", "Motor"]
			}
		}
	},
	{
		"id": "CarModel1",
		"parent": "ACar",
		"properties": {
			"model": "HC123",
			"features": {
				"merge":true,
				"value-list": ["Seat Heating"]
			}
		}
	}

Any non-anonymous module can be a parent to another module in which case the properties of both will be merged in a way that the child overwrites the parent’s properties if set. For collections the flag “merge” has to be defined as “true” if merging of values should happen. A parent module can be flagged as “abstract” which would prevent inflation of that module at runtime.

### Nested Import
From within a configuration file other configuration files may be referenced and imported.

...XML...

	<import path="./additional_file.xml" />
	<import path="./may_not_even_exist.xml" optional="true" />

...JSON...

	{
	    /* import: can be a string or an array of strings and/or objects */
	    "import": [
		    "./additional_file.json",
		    { "path": "./may_not_even_exist.json", "optional": true }
	    ]
	}

The importing happens when the import module is encountered allowing for controlled overwriting of modules. Usually the import modules are set on the top for better dependency transparency, but as instancing happens at inflating time only they don’t have to be.

## Example: Optional Tool Configuration

Sometimes there is a need for a tool which lets users/clients look up and maybe even change some data in e.g. some database. Apart from the configuration for the tool itself it would be a good idea to provide a possibility for the user to have an own configuration file which is tool-optional i.e. even after updates of the tool itself this file would stay the same and basically provide some user specific configuration.
Here is a small example for a configuration root file for such a tool:

	<?xml version="1.0" encoding="utf-8" ?>
	<beans>
	    ... configuration beans for overall setup including e.g. ...
	    ... references to the "LoginData" bean below ...
	    <bean id="LoginData" class...>
	        <property name="dbname" value="project_data" />
	        <property name="user" value="guest" />
	        <property name="password" value="guest" />
	    </bean>
	</beans>

OK then, let’s say the “LoginData” module is used in the actual application – either inflated directly or (always the better approach) referenced directly in the configuration. The tool now can be used to view some data from whatever content “project_data” database is holding. But what if one of the tool users is an administrator and he would like to also use the tool for editing? Even after the editing capabilities would be added, the “guest” login is surely set to have readonly access. The administrator now could go ahead and simply adjust the values in the configuration to have his own authorization data – this is already a good approach as it allows for change of login data without having to re-compile-build-deploy the whole tool. But what if the administrator forgets about his login data being in the configuration and sends the tool incl. configuration to somebody else? And what if there is an update of the tool coming up and the administrator would overwrite the current configuration with the updated one – he would have to remember to update his login data again.
A better way would be to provide a possibility for the user to have his own file overwriting the login data which he wouldn’t need to update and ideally wouldn’t have to overwrite on any tool related updates (apart from rarely needed property rename and such). So let’s import an optional file there:

	<?xml version="1.0" encoding="utf-8" ?>
	<beans>
	    ... configuration beans for overall setup including e.g. ...
	    ... references to the "LoginData" bean below ...
		<bean id="LoginData" class...>
	        <property name="dbname" value="project_data" />
	        <property name="user" value="guest" />
	        <property name="password" value="guest" />
	    </bean>

	    <import path="../usercfg.xml" optional="true" />

	</beans>

This way the configuration context will search for a “usercfg.xml” file in parent directory of the executed tool and parse it if it’s there. The template file provided to the user would look like this:

	<?xml version="1.0" encoding="utf-8" ?>
	<beans>
		<bean id-merge="LoginData">
		    <property name="user" value="guest" />
		    <property name="password" value="guest" />
	    </bean>
	</beans>

The user could now create the file if needed using this template and add his login data.