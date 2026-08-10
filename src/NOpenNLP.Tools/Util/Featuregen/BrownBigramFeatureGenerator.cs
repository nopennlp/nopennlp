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

using System.Collections.Generic;

namespace NOpenNLP.Tools.Util.Featuregen;

/// <summary>
/// Generates Brown cluster features for token bigrams.
/// </summary>
public class BrownBigramFeatureGenerator : IAdaptiveFeatureGenerator
{
    private readonly BrownCluster brownCluster; // NOpenNLP: made readonly

    /// <summary>
    /// Creates a new Brown Cluster bigram feature generator.
    /// </summary>
    /// <param name="brownCluster">A <see cref="BrownCluster"/>.</param>
    public BrownBigramFeatureGenerator(BrownCluster brownCluster)
    {
        this.brownCluster = brownCluster;
    }

    public virtual void CreateFeatures(IList<string> features, string[] tokens, int index, string[] previousOutcomes)
    {
        IList<string> wordClasses = BrownTokenClasses.GetWordClasses(tokens[index], brownCluster);
        if (index > 0)
        {
            IList<string> prevWordClasses = BrownTokenClasses.GetWordClasses(tokens[index - 1], brownCluster);
            for (int i = 0; i < wordClasses.Count && i < prevWordClasses.Count; i++)
                features.Add("p" + "browncluster" + "," + "browncluster" + "=" + prevWordClasses[i] + "," + wordClasses[i]);
        }

        if (index + 1 < tokens.Length)
        {
            IList<string> nextWordClasses = BrownTokenClasses.GetWordClasses(tokens[index + 1], brownCluster);
            for (int i = 0; i < wordClasses.Count && i < nextWordClasses.Count; i++)
            {
                features.Add("browncluster" + "," + "n" + "browncluster" + "=" + wordClasses[i] + "," + nextWordClasses[i]);
            }
        }
    }

    // NOpenNLP: IAdaptiveFeatureGenerator declares these as Java 8 default
    // methods; C# default interface implementations are unavailable on
    // netstandard2.0/net462, so the empty bodies are supplied here.
    public virtual void UpdateAdaptiveData(string[] tokens, string[] outcomes)
    {
    }

    public virtual void ClearAdaptiveData()
    {
    }
}
