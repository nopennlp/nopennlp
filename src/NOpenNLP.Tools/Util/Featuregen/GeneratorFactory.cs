/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License. You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

// This file has been modified from the original Apache OpenNLP 1.9.1 source:
// translated from Java to C# and adapted for .NET. See NOTICE.

using NOpenNLP.Tools.Support;
using NOpenNLP.Tools.Util.Ext;
using NOpenNLP.Tools.Util.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.XPath;
using J2N.Collections.Generic;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Util.Featuregen;

/// <summary>
/// Creates a set of feature generators based on a provided XML descriptor.
///
/// Example of an XML descriptor:
/// <para/>
/// &lt;featureGenerators name="namefind"&gt;
///     &lt;generator class="opennlp.tools.util.featuregen.CachedFeatureGeneratorFactory"&gt;
///         &lt;generator class="opennlp.tools.util.featuregen.WindowFeatureGeneratorFactory"&gt;
///           &lt;int name="prevLength"&gt;2&lt;/int&gt;
///           &lt;int name="nextLength"&gt;2&lt;/int&gt;
///           &lt;generator class="opennlp.tools.util.featuregen.TokenClassFeatureGeneratorFactory"/&gt;
///         &lt;/generator&gt;
///         &lt;generator class="opennlp.tools.util.featuregen.WindowFeatureGeneratorFactory"&gt;
///           &lt;int name="prevLength"&gt;2&lt;/int&gt;
///           &lt;int name="nextLength"&gt;2&lt;/int&gt;
///           &lt;generator class="opennlp.tools.util.featuregen.TokenFeatureGeneratorFactory"/&gt;
///         &lt;/generator&gt;
///         &lt;generator class="opennlp.tools.util.featuregen.DefinitionFeatureGeneratorFactory"/&gt;
///         &lt;generator class="opennlp.tools.util.featuregen.PreviousMapFeatureGeneratorFactory"/&gt;
///         &lt;generator class="opennlp.tools.util.featuregen.BigramNameFeatureGeneratorFactory"/&gt;
///         &lt;generator class="opennlp.tools.util.featuregen.SentenceFeatureGeneratorFactory"&gt;
///           &lt;bool name="begin"&gt;true&lt;/bool&gt;
///           &lt;bool name="end"&gt;false&lt;/bool&gt;
///         &lt;/generator&gt;
///     &lt;/generator&gt;
/// &lt;/featureGenerators&gt;
///
///
/// Each XML element is mapped to a <see cref="GeneratorFactory.IXmlFeatureGeneratorFactory"/> which
/// is responsible to process the element and create the specified
/// <see cref="IAdaptiveFeatureGenerator"/>. Elements can contain other
/// elements in this case it is the responsibility of the mapped factory to process
/// the child elements correctly. In some factories this leads to recursive
/// calls the
/// <c>GeneratorFactory.IXmlFeatureGeneratorFactory.Create</c>
/// method.
///
/// In the example above the generators element is mapped to the
/// <see cref="AggregatedFeatureGeneratorFactory"/> which then
/// creates all the aggregated <see cref="IAdaptiveFeatureGenerator"/>s to
/// accomplish this it evaluates the mapping with the same mechanism
/// and gives the child element to the corresponding factories. All
/// created generators are added to a new instance of the
/// <see cref="AggregatedFeatureGenerator"/> which is then returned.
/// </summary>
public class GeneratorFactory
{
    /// <summary>
    /// The <see cref="IXmlFeatureGeneratorFactory"/> is responsible to construct
    /// an <see cref="IAdaptiveFeatureGenerator"/> from an given XML <see cref="System.Xml.Linq.XElement"/>
    /// which contains all necessary configuration if any.
    /// </summary>
    internal interface IXmlFeatureGeneratorFactory
    {
        /// <summary>
        /// Creates an <see cref="IAdaptiveFeatureGenerator"/> from a the describing
        /// XML element.
        /// </summary>
        /// <param name="generatorElement">the element which contains the configuration</param>
        /// <param name="resourceManager">the resource manager which could be used
        ///     to access referenced resources</param>
        /// <returns>the configured <see cref="IAdaptiveFeatureGenerator"/></returns>
        IAdaptiveFeatureGenerator Create(XmlElement generatorElement, FeatureGeneratorResourceProvider resourceManager);
    }

    public abstract class AbstractXmlFeatureGeneratorFactory
    {
        protected XmlElement generatorElement;
        protected FeatureGeneratorResourceProvider resourceManager;
        // to respect the order <generator/> in AggregatedFeatureGenerator, let's use LinkedHashMap
        protected readonly LinkedDictionary<string, object> args; // NOpenNLP: made readonly

        public AbstractXmlFeatureGeneratorFactory()
        {
            args = new LinkedDictionary<string, object>();
        }

        public virtual JCG.Dictionary<string, IArtifactSerializer> GetArtifactSerializerMapping()
        {
            return null;
        }

        internal void Init(XmlElement element, FeatureGeneratorResourceProvider resourceManager)
        {
            this.generatorElement = element;
            this.resourceManager = resourceManager;
            IList<IAdaptiveFeatureGenerator> generators = new JCG.List<IAdaptiveFeatureGenerator>();
            XmlNodeList childNodes = generatorElement.ChildNodes;
            for (int i = 0; i < childNodes.Count; i++)
            {
                XmlNode childNode = childNodes.Item(i);
                if (childNode is XmlElement)
                {
                    XmlElement elem = (XmlElement)childNode;
                    string type = elem.Name;
                    if (type.Equals("generator"))
                    {
                        string key = "generator#" + generators.Count;
                        IAdaptiveFeatureGenerator afg = BuildGenerator(elem, resourceManager);
                        generators.Add(afg);
                        if (afg != null)
                            args.Put(key, afg);
                    }
                    else
                    {
                        string name = elem.GetAttribute("name");
                        XmlNode cn = elem.FirstChild;
                        XmlText text = (XmlText)cn;
                        switch (type)
                        {
                            case "int":
                                args.Put(name, int.Parse(text.Data));
                                break;
                            case "long":
                                args.Put(name, long.Parse(text.Data));
                                break;
                            case "float":
                                args.Put(name, float.Parse(text.Data));
                                break;
                            case "double":
                                args.Put(name, double.Parse(text.Data));
                                break;
                            case "str":
                                args.Put(name, text.Data);
                                break;
                            case "bool":
                                args.Put(name, bool.Parse(text.Data));
                                break;
                            default:
                                throw new InvalidFormatException("child element must be one of generator, int, long, float, double," + " str or bool");
                                break;
                        }
                    }
                }
            }

            if (generators.Count > 1)
            {
                IAdaptiveFeatureGenerator aggregatedFeatureGenerator = new AggregatedFeatureGenerator([.. generators]);
                args.Put("generator#0", aggregatedFeatureGenerator);
            }
        }

        public virtual int GetInt(string name)
        {
            args.TryGetValue(name, out object value);
            if (value == null)
            {
                throw new InvalidFormatException("parameter " + name + " must be set!");
            }
            else if (value is int)
            {
                return (int)value;
            }
            else
            {
                throw new InvalidFormatException("parameter " + name + " must be integer!");
            }
        }

        public virtual int GetInt(string name, int defValue)
        {
            args.TryGetValue(name, out object value);
            if (value == null)
            {
                return defValue;
            }
            else if (value is int)
            {
                return (int)value;
            }
            else
            {
                throw new InvalidFormatException("parameter " + name + " must be integer!");
            }
        }

        public virtual long GetLong(string name)
        {
            args.TryGetValue(name, out object value);
            if (value == null)
            {
                throw new InvalidFormatException("parameter " + name + " must be set!");
            }
            else if (value is long)
            {
                return (long)value;
            }
            else
            {
                throw new InvalidFormatException("parameter " + name + " must be long!");
            }
        }

        public virtual long GetLong(string name, long defValue)
        {
            args.TryGetValue(name, out object value);
            if (value == null)
            {
                return defValue;
            }
            else if (value is long)
            {
                return (long)value;
            }
            else
            {
                throw new InvalidFormatException("parameter " + name + " must be long!");
            }
        }

        public virtual float GetFloat(string name)
        {
            args.TryGetValue(name, out object value);
            if (value == null)
            {
                throw new InvalidFormatException("parameter " + name + " must be set!");
            }
            else if (value is float)
            {
                return (float)value;
            }
            else
            {
                throw new InvalidFormatException("parameter " + name + " must be float!");
            }
        }

        public virtual float GetFloat(string name, float defValue)
        {
            args.TryGetValue(name, out object value);
            if (value == null)
            {
                return defValue;
            }
            else if (value is float)
            {
                return (float)value;
            }
            else
            {
                throw new InvalidFormatException("parameter " + name + " must be float!");
            }
        }

        public virtual double GetDouble(string name)
        {
            args.TryGetValue(name, out object value);
            if (value == null)
            {
                throw new InvalidFormatException("parameter " + name + " must be set!");
            }
            else if (value is double)
            {
                return (double)value;
            }
            else
            {
                throw new InvalidFormatException("parameter " + name + " must be double!");
            }
        }

        public virtual double GetDouble(string name, double defValue)
        {
            args.TryGetValue(name, out object value);
            if (value == null)
            {
                return defValue;
            }
            else if (value is double)
            {
                return (double)value;
            }
            else
            {
                throw new InvalidFormatException("parameter " + name + " must be double!");
            }
        }

        public virtual string GetStr(string name)
        {
            args.TryGetValue(name, out object value);
            if (value == null)
            {
                throw new InvalidFormatException("parameter " + name + " must be set!");
            }
            else if (value is string)
            {
                return (string)value;
            }
            else
            {
                throw new InvalidFormatException("parameter " + name + " must be double!");
            }
        }

        public virtual string GetStr(string name, string defValue)
        {
            args.TryGetValue(name, out object value);
            if (value == null)
            {
                return defValue;
            }
            else if (value is string)
            {
                return (string)value;
            }
            else
            {
                throw new InvalidFormatException("parameter " + name + " must be String!");
            }
        }

        public virtual bool GetBool(string name)
        {
            args.TryGetValue(name, out object value);
            if (value == null)
            {
                throw new InvalidFormatException("parameter " + name + " must be set!");
            }
            else if (value is bool)
            {
                return (bool)value;
            }
            else
            {
                throw new InvalidFormatException("parameter " + name + " must be boolean!");
            }
        }

        public virtual bool GetBool(string name, bool defValue)
        {
            args.TryGetValue(name, out object value);
            if (value == null)
            {
                return defValue;
            }
            else if (value is bool)
            {
                return (bool)value;
            }
            else
            {
                throw new InvalidFormatException("parameter " + name + " must be boolean!");
            }
        }

        /// <summary>
        /// </summary>
        /// <returns>null if the subclass uses <c>resourceManager</c> to instantiate</returns>
        /// <exception cref="InvalidFormatException"></exception>
        public abstract IAdaptiveFeatureGenerator Create();
    }

    // TODO: We have to support custom resources here. How does it work ?!
    // Attributes get into a Map<String, String> properties
    // How can serialization be supported ?!
    // The model is loaded, and the manifest should contain all serializer classes registered for the
    // resources by name.
    // When training, the descriptor could be consulted first to register the serializers, and afterwards
    // they are stored in the model.
    // TODO: (OPENNLP-1174) just remove this class when back-compat is no longer needed
    class CustomFeatureGeneratorFactory : IXmlFeatureGeneratorFactory
    {
        public virtual IAdaptiveFeatureGenerator Create(XmlElement generatorElement, FeatureGeneratorResourceProvider resourceManager)
        {
            string featureGeneratorClassName = generatorElement.GetAttribute("class");
            IAdaptiveFeatureGenerator generator = ExtensionLoader.InstantiateExtension<IAdaptiveFeatureGenerator>(featureGeneratorClassName);
            if (generator is CustomFeatureGenerator)
            {
                CustomFeatureGenerator customGenerator = (CustomFeatureGenerator)generator;
                JCG.Dictionary<string, string> properties = new JCG.Dictionary<string, string>();
                XmlAttributeCollection attributes = generatorElement.Attributes;
                for (int i = 0; i < attributes.Count; i++)
                {
                    XmlNode attribute = attributes.Item(i);
                    if (!"class".Equals(attribute.Name))
                    {
                        properties.Put(attribute.Name, attribute.Value);
                    }
                }

                if (resourceManager != null)
                {
                    customGenerator.Init(properties, resourceManager);
                }
            }

            return generator;
        }

        internal static void Register(JCG.Dictionary<string, IXmlFeatureGeneratorFactory> factoryMap)
        {
            factoryMap.Put("custom", new CustomFeatureGeneratorFactory());
        }
    }

    // TODO: (OPENNLP-1174) just remove when back-compat is no longer needed
    private static readonly JCG.Dictionary<string, IXmlFeatureGeneratorFactory> factories = new JCG.Dictionary<string, IXmlFeatureGeneratorFactory>(); // NOpenNLP: made readonly

    // TODO: (OPENNLP-1174) just remove when back-compat is no longer needed
    static GeneratorFactory()
    {
        AggregatedFeatureGeneratorFactory.Register(factories);
        CachedFeatureGeneratorFactory.Register(factories);
        CharacterNgramFeatureGeneratorFactory.Register(factories);
        DefinitionFeatureGeneratorFactory.Register(factories);
        DictionaryFeatureGeneratorFactory.Register(factories);
        DocumentBeginFeatureGeneratorFactory.Register(factories);
        PreviousMapFeatureGeneratorFactory.Register(factories);
        SentenceFeatureGeneratorFactory.Register(factories);
        TokenClassFeatureGeneratorFactory.Register(factories);
        TokenFeatureGeneratorFactory.Register(factories);
        BigramNameFeatureGeneratorFactory.Register(factories);
        TokenPatternFeatureGeneratorFactory.Register(factories);
        PosTaggerFeatureGeneratorFactory.Register(factories);
        PrefixFeatureGeneratorFactory.Register(factories);
        SuffixFeatureGeneratorFactory.Register(factories);
        WindowFeatureGeneratorFactory.Register(factories);
        WordClusterFeatureGeneratorFactory.Register(factories);
        BrownClusterTokenFeatureGeneratorFactory.Register(factories);
        BrownClusterTokenClassFeatureGeneratorFactory.Register(factories);
        BrownClusterBigramFeatureGeneratorFactory.Register(factories);
        CustomFeatureGeneratorFactory.Register(factories);
        POSTaggerNameFeatureGeneratorFactory.Register(factories);
    }

    /// <summary>
    /// Creates a <see cref="IAdaptiveFeatureGenerator"/> for the provided element.
    /// To accomplish this it looks up the corresponding factory by the
    /// element tag name. The factory is then responsible for the creation
    /// of the generator from the element.
    /// </summary>
    /// <param name="generatorElement"></param>
    /// <param name="resourceManager"></param>
    /// <returns></returns>
    internal static IAdaptiveFeatureGenerator CreateGenerator(XmlElement generatorElement, FeatureGeneratorResourceProvider resourceManager)
    {
        string elementName = generatorElement.Name;

        // check it is new format?
        if (elementName.Equals("featureGenerators"))
        {
            IList<IAdaptiveFeatureGenerator> generators = new JCG.List<IAdaptiveFeatureGenerator>();
            XmlNodeList childNodes = generatorElement.ChildNodes;
            for (int i = 0; i < childNodes.Count; i++)
            {
                XmlNode childNode = childNodes.Item(i);
                if (childNode is XmlElement)
                {
                    XmlElement elem = (XmlElement)childNode;
                    string type = elem.Name;
                    if (type.Equals("generator"))
                    {
                        generators.Add(BuildGenerator(elem, resourceManager));
                    }
                    else
                        throw new InvalidFormatException("Unexpected element: " + elementName);
                }
            }

            IAdaptiveFeatureGenerator featureGenerator = null;
            if (generators.Count == 1)
                featureGenerator = generators[0];
            else if (generators.Count > 1)
                featureGenerator = new AggregatedFeatureGenerator([.. generators]);
            else
                throw new InvalidFormatException("featureGenerators must have one or more generators");

            // disallow manually specifying CachedFeatureGenerator
            if (featureGenerator is CachedFeatureGenerator)
                throw new InvalidFormatException("CachedFeatureGeneratorFactory cannot be specified manually." + "Use cache=\"true\" attribute in featureGenerators element instead.");

            // check cache usage
            if (bool.Parse(generatorElement.GetAttribute("cache")))
                return new CachedFeatureGenerator(featureGenerator);
            else
                return featureGenerator;
        }
        else
        {

            // support classic format
            IXmlFeatureGeneratorFactory generatorFactory = factories[elementName];
            if (generatorFactory != null)
            {
                return generatorFactory.Create(generatorElement, resourceManager);
            }
            else
                throw new InvalidFormatException("Unexpected element: " + elementName);
        }
    }

    internal static XmlElement GetFirstChild(XmlElement elem)
    {
        XmlNodeList nodes = elem.ChildNodes;
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes.Item(i) is XmlElement)
            {
                return (XmlElement)nodes.Item(i);
            }
        }

        return null;
    }

    /// <summary>
    /// Creates a <see cref="IAdaptiveFeatureGenerator"/> for the provided element.
    /// To accomplish this it looks up the corresponding factory by the
    /// element tag name. The factory is then responsible for the creation
    /// of the generator from the element.
    /// </summary>
    /// <param name="generatorElement"></param>
    /// <param name="resourceManager"></param>
    /// <returns></returns>
    internal static IAdaptiveFeatureGenerator BuildGenerator(XmlElement generatorElement, FeatureGeneratorResourceProvider resourceManager)
    {
        string className = generatorElement.GetAttribute("class");
        if (className == null)
        {
            throw new InvalidFormatException("generator must have class attribute");
        }
        else
        {
            // NOpenNLP: Type.GetType() returns null rather than throwing
            // ClassNotFoundException, so we check for null explicitly.
            Type factoryClass = ExtensionLoader.ResolveType(className);
            if (factoryClass is null)
            {
                throw new TypeLoadException("Could not load type: " + className);
            }

            try
            {
                AbstractXmlFeatureGeneratorFactory factory = (AbstractXmlFeatureGeneratorFactory)Activator.CreateInstance(factoryClass);
                factory.Init(generatorElement, resourceManager);
                return factory.Create();
            }
            // catch (NoSuchMethodException e)
            // {
            //     throw new Exception(e);
            // }
            // catch (InvocationTargetException e)
            // {
            //     throw new Exception(e);
            // }
            // catch (InstantiationException e)
            // {
            //     throw new Exception(e);
            // }
            // catch (IllegalAccessException e)
            // {
            //     throw new Exception(e);
            // }
            // NOpenNLP: the four reflection exceptions above all map to these
            // in .NET, and upstream wraps each in a RuntimeException.
            catch (Exception e) when (e.IsNoSuchMethodException() || e.IsInvocationTargetException() || e.IsInstantiationException() || e.IsIllegalAccessException())
            {
                throw RuntimeException.Create(e);
            }
            // catch (ClassNotFoundException e)
            // {
            //     throw new Exception(e);
            // }
        }
    }

    private static XmlDocument CreateDOM(Stream xmlDescriptorIn)
    {
        var xmlDoc = new XmlDocument();

        try
        {
            xmlDoc.Load(xmlDescriptorIn);
        }
        catch (XmlException ex)
        {
            throw new InvalidFormatException("Descriptor is not valid XML!", ex);
        }

        return xmlDoc;
    }

    /// <summary>
    /// Creates an <see cref="IAdaptiveFeatureGenerator"/> from an provided XML descriptor.
    ///
    /// Usually this XML descriptor contains a set of nested feature generators
    /// which are then used to generate the features by one of the opennlp
    /// components.
    /// </summary>
    /// <param name="xmlDescriptorIn">the <see cref="System.IO.Stream"/> from which the descriptor
    ///     is read, the stream remains open and must be closed by the caller.</param>
    /// <param name="resourceManager">the resource manager which is used to resolve resources
    ///     referenced by a key in the descriptor</param>
    /// <returns>created feature generators</returns>
    /// <exception cref="IOException">if an error occurs during reading from the descriptor
    ///     <see cref="System.IO.Stream"/></exception>
    public static IAdaptiveFeatureGenerator Create(Stream xmlDescriptorIn, FeatureGeneratorResourceProvider resourceManager)
    {
        XmlDocument xmlDescriptorDOM = CreateDOM(xmlDescriptorIn);
        XmlElement generatorElement = xmlDescriptorDOM.DocumentElement;

        // TODO: (OPENNLP-1174) use #buildGenerator() after back-compat support is gone
        return CreateGenerator(generatorElement, resourceManager);
    }

    public static JCG.Dictionary<string, IArtifactSerializer> ExtractArtifactSerializerMappings(Stream xmlDescriptorIn)
    {
        XmlDocument xmlDescriptorDOM = CreateDOM(xmlDescriptorIn);
        XmlElement element = xmlDescriptorDOM.DocumentElement;
        string elementName = element.Name;

        // check it is new format?
        if (elementName.Equals("featureGenerators"))
        {
            JCG.Dictionary<string, IArtifactSerializer> mapping = new JCG.Dictionary<string, IArtifactSerializer>();
            XmlNodeList nodes = element.ChildNodes;
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes.Item(i) is XmlElement)
                {
                    XmlElement childElem = (XmlElement)nodes.Item(i);
                    if (childElem.Name.Equals("generator"))
                    {
                        ExtractArtifactSerializerMappings(mapping, childElem);
                    }
                }
            }

            return mapping;
        }
        else
        {
            return ExtractArtifactSerializerMappingsClassicFormat(element);
        }
    }

    internal static void ExtractArtifactSerializerMappings(JCG.Dictionary<string, IArtifactSerializer> mapping, XmlElement element)
    {
        string className = element.GetAttribute("class");
        if (className != null)
        {
            // NOpenNLP: Type.GetType() returns null rather than throwing
            // ClassNotFoundException, so we check for null explicitly.
            Type factoryClass = ExtensionLoader.ResolveType(className);
            if (factoryClass is null)
            {
                throw new TypeLoadException("Could not load type: " + className);
            }

            try
            {
                AbstractXmlFeatureGeneratorFactory factory = (AbstractXmlFeatureGeneratorFactory)Activator.CreateInstance(factoryClass);
                factory.Init(element, null);
                JCG.Dictionary<string, IArtifactSerializer> map = factory.GetArtifactSerializerMapping();
                if (map != null)
                    mapping.PutAll(map);
            }
            catch (InvalidFormatException)
            {
                // NOpenNLP: intentionally ignored, matching upstream
            }
            // catch (NoSuchMethodException e)
            // {
            //     throw;
            // }
            // catch (TargetInvocationException e)
            // {
            //     throw;
            // }
            // catch (InstantiationException e)
            // {
            //     throw;
            // }
            // catch (MethodAccessException e)
            // {
            //     throw;
            // }
            // NOpenNLP: the four reflection exceptions above all map to these
            // in .NET, and upstream wraps each in a RuntimeException.
            catch (Exception e) when (e.IsNoSuchMethodException() || e.IsInvocationTargetException() || e.IsInstantiationException() || e.IsIllegalAccessException())
            {
                throw RuntimeException.Create(e);
            }
            // catch (ClassNotFoundException e)
            // {
            //     throw;
            // }
        }

        XmlNodeList nodes = element.ChildNodes;
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes.Item(i) is XmlElement)
            {
                XmlElement childElem = (XmlElement)nodes.Item(i);
                if (childElem.Name.Equals("generator"))
                {
                    ExtractArtifactSerializerMappings(mapping, childElem);
                }
            }
        }
    }

    internal static JCG.Dictionary<string, IArtifactSerializer> ExtractArtifactSerializerMappingsClassicFormat(XmlElement elem)
    {
        JCG.Dictionary<string, IArtifactSerializer> mapping = new JCG.Dictionary<string, IArtifactSerializer>();
        //Xpath xPath = XPathFactory.NewInstance().NewXPath();
        XmlNodeList customElements;
        try
        {
            customElements = elem.SelectNodes("//custom");
        }
        catch (XPathException e)
        {
            throw new InvalidOperationException("The hard coded XPath expression should always be valid!");
        }

        for (int i = 0; i < customElements.Count; i++)
        {
            if (customElements.Item(i) is XmlElement)
            {
                XmlElement customElement = (XmlElement)customElements.Item(i);

                // Note: The resource provider is not available at that point, to provide
                // resources they need to be loaded first!
                IAdaptiveFeatureGenerator generator = CreateGenerator(customElement, null);
                if (generator is IArtifactToSerializerMapper)
                {
                    IArtifactToSerializerMapper mapper = (IArtifactToSerializerMapper)generator;
                    mapping.PutAll(mapper.GetArtifactSerializerMapping());
                }
            }
        }

        XmlNodeList allElements;
        try
        {
            allElements = elem.SelectNodes("//*");
        }
        catch (XPathException e)
        {
            throw new InvalidOperationException("The hard coded XPath expression should always be valid!");
        }

        for (int i = 0; i < allElements.Count; i++)
        {
            if (allElements.Item(i) is XmlElement)
            {
                XmlElement xmlElement = (XmlElement)allElements.Item(i);
                string dictName = xmlElement.GetAttribute("dict");
                if (dictName != null)
                {
                    switch (xmlElement.Name)
                    {
                        case "wordcluster":
                            mapping.Put(dictName, new WordClusterDictionary.WordClusterDictionarySerializer());
                            break;
                        case "brownclustertoken":
                            mapping.Put(dictName, new BrownCluster.BrownClusterSerializer());
                            break;
                        case "brownclustertokenclass":
                            mapping.Put(dictName, new BrownCluster.BrownClusterSerializer());
                            break;
                        case "brownclusterbigram":
                            mapping.Put(dictName, new BrownCluster.BrownClusterSerializer());
                            break;
                        case "dictionary":
                            mapping.Put(dictName, new DictionarySerializer());
                            break;
                    }
                }

                string modelName = xmlElement.GetAttribute("model");
                if (modelName != null)
                {
                    switch (xmlElement.Name)
                    {
                        case "tokenpos":
                            mapping.Put(modelName, new POSModelSerializer());
                            break;
                    }
                }
            }
        }

        return mapping;
    }

    /// <summary>
    /// Provides a list with all the elements in the xml feature descriptor.
    /// </summary>
    /// <param name="xmlDescriptorIn">the xml feature descriptor</param>
    /// <returns>a list containing all elements</returns>
    /// <exception cref="IOException">if inputstream cannot be open</exception>
    /// <exception cref="InvalidFormatException">if xml is not well-formed</exception>
    public static IList<XmlElement> GetDescriptorElements(Stream xmlDescriptorIn)
    {
        IList<XmlElement> elements = new JCG.List<XmlElement>();
        XmlDocument xmlDescriptorDOM = CreateDOM(xmlDescriptorIn);
        //XPath xPath = XPathFactory.NewInstance().NewXPath();
        XmlNodeList allElements;
        try
        {
            allElements = xmlDescriptorDOM.DocumentElement.SelectNodes("//*");
        }
        catch (XPathException e)
        {
            throw new InvalidOperationException("The hard coded XPath expression should always be valid!");
        }

        for (int i = 0; i < allElements.Count; i++)
        {
            if (allElements.Item(i) is XmlElement)
            {
                XmlElement customElement = (XmlElement)allElements.Item(i);
                elements.Add(customElement);
            }
        }

        return elements;
    }
}
