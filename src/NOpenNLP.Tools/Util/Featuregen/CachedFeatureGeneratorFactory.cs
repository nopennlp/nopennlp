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
using System.Collections.Generic;
using System.Xml;

namespace NOpenNLP.Tools.Util.Featuregen;

/// <summary>
/// </summary>
/// <seealso cref="CachedFeatureGenerator"/>
public class CachedFeatureGeneratorFactory : GeneratorFactory.AbstractXmlFeatureGeneratorFactory, GeneratorFactory.IXmlFeatureGeneratorFactory
{
    public CachedFeatureGeneratorFactory() : base()
    {
    }

    public virtual IAdaptiveFeatureGenerator Create(XmlElement generatorElement, FeatureGeneratorResourceProvider resourceManager)
    {
        XmlElement cachedGeneratorElement = null;
        XmlNodeList kids = generatorElement.ChildNodes;
        for (int i = 0; i < kids.Count; i++)
        {
            XmlNode childNode = kids.Item(i);
            if (childNode is XmlElement)
            {
                cachedGeneratorElement = (XmlElement)childNode;
                break;
            }
        }

        if (cachedGeneratorElement == null)
        {
            throw new InvalidFormatException("Could not find containing generator element!");
        }

        IAdaptiveFeatureGenerator cachedGenerator = GeneratorFactory.CreateGenerator(cachedGeneratorElement, resourceManager);
        return new CachedFeatureGenerator(cachedGenerator);
    }

    internal static void Register(IDictionary<string, GeneratorFactory.IXmlFeatureGeneratorFactory> factoryMap)
    {
        factoryMap.Put("cache", new CachedFeatureGeneratorFactory());
    }

    public override IAdaptiveFeatureGenerator Create()
    {
        IAdaptiveFeatureGenerator generator = (IAdaptiveFeatureGenerator)args["generator#0"];
        if (generator == null)
        {
            throw new InvalidFormatException("Could not find containing generator element!");
        }

        return new CachedFeatureGenerator(generator);
    }
}
