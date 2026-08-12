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
/// Adds trigram features based on tokens and token classes.
/// </summary>
public class TrigramNameFeatureGenerator : IAdaptiveFeatureGenerator
{
    public virtual void CreateFeatures(IList<string> features, string[] tokens, int index, string[] previousOutcomes)
    {
        string wc = FeatureGeneratorUtil.TokenFeature(tokens[index]);

        // trigram features
        if (index > 1)
        {
            features.Add("ppw,pw,w=" + tokens[index - 2] + "," + tokens[index - 1] + "," + tokens[index]);
            string pwc = FeatureGeneratorUtil.TokenFeature(tokens[index - 1]);
            string ppwc = FeatureGeneratorUtil.TokenFeature(tokens[index - 2]);
            features.Add("ppwc,pwc,wc=" + ppwc + "," + pwc + "," + wc);
        }

        if (index + 2 < tokens.Length)
        {
            features.Add("w,nw,nnw=" + tokens[index] + "," + tokens[index + 1] + "," + tokens[index + 2]);
            string nwc = FeatureGeneratorUtil.TokenFeature(tokens[index + 1]);
            string nnwc = FeatureGeneratorUtil.TokenFeature(tokens[index + 2]);
            features.Add("wc,nwc,nnwc=" + wc + "," + nwc + "," + nnwc);
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
