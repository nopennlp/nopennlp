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
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Util.Featuregen;

/// <summary>
/// This <see cref="IAdaptiveFeatureGenerator"/> generates features indicating the
/// outcome associated with two previously occuring words.
/// </summary>
public class PreviousTwoMapFeatureGenerator : IAdaptiveFeatureGenerator
{
    private readonly JCG.Dictionary<string, string> previousMap = new(); // NOpenNLP: made readonly

    /// <summary>
    /// Generates previous decision features for the token based on contents of the previous map.
    /// </summary>
    public virtual void CreateFeatures(IList<string> features, string[] tokens, int index, string[] preds)
    {
        if (index > 0)
        {
            features.Add("ppd=" + GetPrevious(tokens[index]) + "," +
                GetPrevious(tokens[index - 1]));
        }
    }

    // NOpenNLP: Java's Map.get returns null for an absent key, which string
    // concatenation renders as "null". The C# indexer throws instead, so the
    // lookup goes through TryGetValue and reproduces the "null" text.
    private string GetPrevious(string token)
        => previousMap.TryGetValue(token, out string? previous) ? previous : "null";

    public virtual void UpdateAdaptiveData(string[] tokens, string[] outcomes)
    {
        for (int i = 0; i < tokens.Length; i++)
        {
            previousMap.Put(tokens[i], outcomes[i]);
        }
    }

    /// <summary>
    /// Clears the previous map.
    /// </summary>
    public virtual void ClearAdaptiveData()
    {
        previousMap.Clear();
    }
}
