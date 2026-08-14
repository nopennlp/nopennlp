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

// This file has been modified from the original Apache OpenNLP 1.9.4 source:
// translated from Java to C# and adapted for .NET. See NOTICE.

using NOpenNLP.Tools.Support;
using NOpenNLP.Tools.Util.Model;
using JCG = J2N.Collections.Generic;
using System.Xml;
using System.Collections.Generic;

namespace NOpenNLP.Tools.Util.Featuregen;

/// <summary>
/// </summary>
/// <seealso cref="DictionaryFeatureGenerator"/>
public class DictionaryFeatureGeneratorFactory : GeneratorFactory.AbstractXmlFeatureGeneratorFactory, GeneratorFactory.IXmlFeatureGeneratorFactory
{
    public virtual IAdaptiveFeatureGenerator Create(XmlElement generatorElement, FeatureGeneratorResourceProvider resourceManager)
    {
        string dictResourceKey = generatorElement.GetAttribute("dict");
        object? dictResource = resourceManager(dictResourceKey);
        if (dictResource is not NOpenNLP.Tools.Dictionary.Dictionary dict)
        {
            throw new InvalidFormatException($"No dictionary resource for key: {dictResourceKey}");
        }

        string prefix = generatorElement.GetAttribute("prefix");
        return new DictionaryFeatureGenerator(prefix, dict);
    }

    internal static void Register(IDictionary<string, GeneratorFactory.IXmlFeatureGeneratorFactory> factoryMap)
    {
        factoryMap.Put("dictionary", new DictionaryFeatureGeneratorFactory());
    }

    public override IAdaptiveFeatureGenerator? Create()
    {
        // if resourceManager is null, we don't instantiate
        if (resourceManager == null)
            return null;
        string dictResourceKey = GetStr("dict");
        object? dictResource = resourceManager(dictResourceKey);
        if (dictResource is not NOpenNLP.Tools.Dictionary.Dictionary dict)
        {
            throw new InvalidFormatException($"No dictionary resource for key: {dictResourceKey}");
        }

        return new DictionaryFeatureGenerator(GetStr("prefix"), dict);
    }

    public override IDictionary<string, IArtifactSerializer> ArtifactSerializerMapping
    {
        get
        {
            JCG.Dictionary<string, IArtifactSerializer> mapping = new JCG.Dictionary<string, IArtifactSerializer>();
            mapping.Put(GetStr("dict"), new DictionarySerializer());
            return mapping;
        }
    }
}
