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
using System.Xml;
using System.Collections.Generic;

namespace NOpenNLP.Tools.Util.Featuregen;

/// <summary>
/// </summary>
/// <seealso cref="WindowFeatureGenerator"/>
public class WindowFeatureGeneratorFactory : GeneratorFactory.AbstractXmlFeatureGeneratorFactory, GeneratorFactory.IXmlFeatureGeneratorFactory
{
    public virtual IAdaptiveFeatureGenerator Create(XmlElement generatorElement, FeatureGeneratorResourceProvider resourceManager)
    {
        XmlElement? nestedGeneratorElement = null;
        XmlNodeList kids = generatorElement.ChildNodes;
        for (int i = 0; i < kids.Count; i++)
        {
            XmlNode? childNode = kids.Item(i);
            if (childNode is XmlElement node)
            {
                nestedGeneratorElement = node;
                break;
            }
        }

        if (nestedGeneratorElement == null)
        {
            throw new InvalidFormatException("window feature generator must contain" + " an aggregator element");
        }

        IAdaptiveFeatureGenerator? nestedGenerator = GeneratorFactory.CreateGenerator(nestedGeneratorElement, resourceManager);
        string prevLengthString = generatorElement.GetAttribute("prevLength");
        int prevLength;
        try
        {
            prevLength = int.Parse(prevLengthString);
        }
        catch (System.FormatException e)
        {
            throw new InvalidFormatException("prevLength attribute '" + prevLengthString + "' is not a number!", e);
        }

        string nextLengthString = generatorElement.GetAttribute("nextLength");
        int nextLength;
        try
        {
            nextLength = int.Parse(nextLengthString);
        }
        catch (System.FormatException e)
        {
            throw new InvalidFormatException("nextLength attribute '" + nextLengthString + "' is not a number!", e);
        }

        return new WindowFeatureGenerator(nestedGenerator, prevLength, nextLength);
    }

    internal static void Register(IDictionary<string, GeneratorFactory.IXmlFeatureGeneratorFactory> factoryMap)
    {
        factoryMap.Put("window", new WindowFeatureGeneratorFactory());
    }

    public override IAdaptiveFeatureGenerator Create()
    {
        IAdaptiveFeatureGenerator generator = (IAdaptiveFeatureGenerator)args["generator#0"];
        if (generator == null)
        {
            throw new InvalidFormatException("window feature generator must contain" + " an aggregator element");
        }

        return new WindowFeatureGenerator(generator, GetInt32("prevLength"), GetInt32("nextLength"));
    }
}
