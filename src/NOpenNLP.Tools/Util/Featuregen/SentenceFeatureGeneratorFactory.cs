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
/// <seealso cref="SentenceFeatureGenerator"/>
public class SentenceFeatureGeneratorFactory : GeneratorFactory.AbstractXmlFeatureGeneratorFactory, GeneratorFactory.IXmlFeatureGeneratorFactory
{
    public virtual IAdaptiveFeatureGenerator Create(XmlElement generatorElement, FeatureGeneratorResourceProvider resourceManager)
    {
        string beginFeatureString = generatorElement.GetAttribute("begin");
        bool beginFeature = true;
        if (beginFeatureString.Length != 0)
            beginFeature = bool.Parse(beginFeatureString);
        string endFeatureString = generatorElement.GetAttribute("end");
        bool endFeature = true;
        if (endFeatureString.Length != 0)
            endFeature = bool.Parse(endFeatureString);
        return new SentenceFeatureGenerator(beginFeature, endFeature);
    }

    internal static void Register(IDictionary<string, GeneratorFactory.IXmlFeatureGeneratorFactory> factoryMap)
    {
        factoryMap.Put("sentence", new SentenceFeatureGeneratorFactory());
    }

    public override IAdaptiveFeatureGenerator Create()
    {
        string beginFeatureString = generatorElement.GetAttribute("begin");
        return new SentenceFeatureGenerator(GetBool("begin", true), GetBool("end", true));
    }
}
