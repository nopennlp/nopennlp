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
using NOpenNLP.Tools.Util;
using NOpenNLP.Tools.Util.Model;
using JCG = J2N.Collections.Generic;
using System;
using System.Xml;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace NOpenNLP.Tools.Util.Featuregen;

/// <summary>
/// Generates Brown clustering features for token classes.
/// </summary>
public class BrownClusterTokenClassFeatureGeneratorFactory : GeneratorFactory.AbstractXmlFeatureGeneratorFactory, GeneratorFactory.XmlFeatureGeneratorFactory
{
    public BrownClusterTokenClassFeatureGeneratorFactory() : base()
    {
    }

    public virtual AdaptiveFeatureGenerator Create(XmlElement generatorElement, FeatureGeneratorResourceProvider resourceManager)
    {
        string dictResourceKey = generatorElement.GetAttribute("dict");
        object dictResource = resourceManager(dictResourceKey);
        if (!(dictResource is BrownCluster))
        {
            throw new InvalidFormatException("Not a BrownLexicon resource for key: " + dictResourceKey);
        }

        return new BrownTokenClassFeatureGenerator((BrownCluster)dictResource);
    }

    internal static void Register(IDictionary<string, GeneratorFactory.XmlFeatureGeneratorFactory> factoryMap)
    {
        factoryMap.Put("brownclustertokenclass", new BrownClusterTokenClassFeatureGeneratorFactory());
    }

    public override AdaptiveFeatureGenerator Create()
    {

        // if resourceManager is null, we don't instantiate
        if (resourceManager == null)
            return null;
        string dictResourceKey = GetStr("dict");
        object dictResource = resourceManager(dictResourceKey);
        if (!(dictResource is BrownCluster))
        {
            throw new InvalidFormatException("Not a BrownLexicon resource for key: " + dictResourceKey);
        }

        return new BrownTokenClassFeatureGenerator((BrownCluster)dictResource);
    }

    public override JCG.Dictionary<string, ArtifactSerializer> GetArtifactSerializerMapping()
    {
        JCG.Dictionary<string, ArtifactSerializer> mapping = new JCG.Dictionary<string, ArtifactSerializer>();
        mapping.Put(GetStr("dict"), new BrownCluster.BrownClusterSerializer());
        return mapping;
    }
}
