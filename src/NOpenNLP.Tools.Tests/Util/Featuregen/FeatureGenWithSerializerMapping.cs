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
using System;
using System.Collections.Generic;
using NOpenNLP.Tools.Util.Model;

namespace NOpenNLP.Tools.Util.Featuregen;

// TODO: (OPENNLP-1174) remove back-compat support when it is unnecessary
// NOpenNLP: referenced by CustomClassLoadingWithSerializers_classic.xml using an
// assembly-qualified .NET type name, since ExtensionLoader.ResolveType only
// searches the library assembly.
[Obsolete("Obsolete")]
public class FeatureGenWithSerializerMapping : CustomFeatureGenerator, IArtifactToSerializerMapper
{
    public override void CreateFeatures(IList<string> features, string[] tokens, int index,
        string[] previousOutcomes)
    {
    }

    public override void UpdateAdaptiveData(string[] tokens, string[] outcomes)
    {
    }

    public override void ClearAdaptiveData()
    {
    }

    public IDictionary<string, IArtifactSerializer> ArtifactSerializerMapping
    {
        get
        {
            IDictionary<string, IArtifactSerializer> mapping = new Dictionary<string, IArtifactSerializer>
            {
                ["test.resource"] = new WordClusterDictionary.WordClusterDictionarySerializer(),
            };
            return new J2N.Collections.ObjectModel.ReadOnlyDictionary<string, IArtifactSerializer>(mapping);
        }
    }

    public override void Init(IDictionary<string, string> properties,
        FeatureGeneratorResourceProvider resourceProvider)
    {
    }
}
