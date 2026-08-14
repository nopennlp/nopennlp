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
using System.Collections.Generic;
using System.Xml;

namespace NOpenNLP.Tools.Util.Featuregen;

/// <summary>
/// </summary>
/// <seealso cref="CharacterNgramFeatureGenerator"/>
public class CharacterNgramFeatureGeneratorFactory : GeneratorFactory.AbstractXmlFeatureGeneratorFactory, GeneratorFactory.IXmlFeatureGeneratorFactory
{
    public virtual IAdaptiveFeatureGenerator Create(XmlElement generatorElement, FeatureGeneratorResourceProvider resourceManager)
    {
        string minString = generatorElement.GetAttribute("min");
        int min;
        try
        {
            min = int.Parse(minString);
        }
        catch (System.FormatException e)
        {
            throw new InvalidFormatException("min attribute '" + minString + "' is not a number!", e);
        }

        string maxString = generatorElement.GetAttribute("max");
        int max;
        try
        {
            max = int.Parse(maxString);
        }
        catch (System.FormatException e)
        {
            throw new InvalidFormatException("max attribute '" + maxString + "' is not a number!", e);
        }

        return new CharacterNgramFeatureGenerator(min, max);
    }

    internal static void Register(IDictionary<string, GeneratorFactory.IXmlFeatureGeneratorFactory> factoryMap)
    {
        factoryMap.Put("charngram", new CharacterNgramFeatureGeneratorFactory());
    }

    public override IAdaptiveFeatureGenerator Create()
    {
        return new CharacterNgramFeatureGenerator(GetInt32("min"), GetInt32("max"));
    }
}
