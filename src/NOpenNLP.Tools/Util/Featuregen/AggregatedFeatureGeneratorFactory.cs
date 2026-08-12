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
using System;
using System.Collections.Generic;
using System.Xml;

namespace NOpenNLP.Tools.Util.Featuregen;

/// <summary>
/// </summary>
/// <seealso cref="AggregatedFeatureGenerator"/>
public class AggregatedFeatureGeneratorFactory : GeneratorFactory.AbstractXmlFeatureGeneratorFactory, GeneratorFactory.IXmlFeatureGeneratorFactory
{
    public virtual IAdaptiveFeatureGenerator Create(XmlElement generatorElement, FeatureGeneratorResourceProvider resourceManager)
    {
        ICollection<IAdaptiveFeatureGenerator> aggregatedGenerators = new LinkedList<IAdaptiveFeatureGenerator>();
        XmlNodeList childNodes = generatorElement.ChildNodes;
        for (int i = 0; i < childNodes.Count; i++)
        {
            XmlNode? childNode = childNodes.Item(i);

            if (childNode is XmlElement aggregatedGeneratorElement)
            {
                aggregatedGenerators.Add(GeneratorFactory.CreateGenerator(aggregatedGeneratorElement, resourceManager));
            }
        }

        return new AggregatedFeatureGenerator([.. aggregatedGenerators]);
    }

    internal static void Register(IDictionary<string, GeneratorFactory.IXmlFeatureGeneratorFactory> factoryMap)
    {
        factoryMap.Put("generators", new AggregatedFeatureGeneratorFactory());
    }

    public override IAdaptiveFeatureGenerator Create()
    {
        IList<IAdaptiveFeatureGenerator> aggregatedGenerators = new List<IAdaptiveFeatureGenerator>();
        foreach (var arg in args)
        {
            if (arg.Key.StartsWith("generator#", StringComparison.Ordinal))
            {
                aggregatedGenerators.Add((IAdaptiveFeatureGenerator)arg.Value);
            }
        }

        return new AggregatedFeatureGenerator([.. aggregatedGenerators]);
    }
}
