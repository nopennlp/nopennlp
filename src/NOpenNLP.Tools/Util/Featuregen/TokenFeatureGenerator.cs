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

using System.Collections.Generic;

namespace NOpenNLP.Tools.Util.Featuregen;

/// <summary>
/// Generates a feature which contains the token itself.
/// </summary>
public class TokenFeatureGenerator(bool lowercase) : IAdaptiveFeatureGenerator
{
    private const string WORD_PREFIX = "w";

    public TokenFeatureGenerator() : this(true)
    {
    }

    public virtual void CreateFeatures(IList<string> features, string[] tokens, int index, string[] preds)
    {
        if (lowercase)
        {
            features.Add(WORD_PREFIX + "=" + StringUtil.ToLowerCase(tokens[index]));
        }
        else
        {
            features.Add(WORD_PREFIX + "=" + tokens[index]);
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
