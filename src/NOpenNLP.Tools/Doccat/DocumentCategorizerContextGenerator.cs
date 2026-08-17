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

// This file has been modified from the original Apache OpenNLP source:
// translated from Java to C# and adapted for .NET. See NOTICE.

using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Doccat;

/// <summary>
/// Context generator for document categorizer
/// </summary>
internal class DocumentCategorizerContextGenerator
{
    private readonly IFeatureGenerator[] mFeatureGenerators; // NOpenNLP: made readonly

    internal DocumentCategorizerContextGenerator(params IFeatureGenerator[] featureGenerators)
    {
        mFeatureGenerators = featureGenerators;
    }

    public virtual string[] GetContext(string[] text, IDictionary<string, object> extraInformation)
    {
        JCG.List<string> context = [];

        foreach (IFeatureGenerator mFeatureGenerator in mFeatureGenerators)
        {
            ICollection<string> extractedFeatures =
                mFeatureGenerator.ExtractFeatures(text, extraInformation);
            context.AddRange(extractedFeatures);
        }

        return context.ToArray();
    }
}
