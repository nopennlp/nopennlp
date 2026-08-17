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

using System;
using System.Collections.Generic;
using System.Text;
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Doccat;

/// <summary>
/// Generates ngram features for a document.
/// n-gram <see cref="IFeatureGenerator"/>
/// </summary>
public class NGramFeatureGenerator : IFeatureGenerator
{
    private readonly int minGram;
    private readonly int maxGram;

    /// <summary>
    /// Constructor for ngrams.
    /// </summary>
    /// <param name="minGram">minGram value - which means minimum words in ngram features</param>
    /// <param name="maxGram">maxGram value - which means maximum words in ngram features</param>
    /// <exception cref="InvalidFormatException"></exception>
    public NGramFeatureGenerator(int minGram, int maxGram)
    {
        if (minGram > 0 && maxGram > 0)
        {
            if (minGram <= maxGram)
            {
                this.minGram = minGram;
                this.maxGram = maxGram;
            }
            else
            {
                throw new InvalidFormatException(
                    "Minimum range value (minGram) should be less than or equal to maximum range value (maxGram)!");
            }
        }
        else
        {
            throw new InvalidFormatException("Both minimum range value (minGram) & maximum " +
                "range value (maxGram) should be greater than or equal to 1!");
        }
    }

    /// <summary>
    /// Default constructor for Bi grams
    /// </summary>
    public NGramFeatureGenerator()
        : this(2, 2)
    {
    }

    /// <summary>
    /// Extract ngram features from given text fragments
    /// </summary>
    /// <param name="text">the text fragments to extract features from</param>
    /// <param name="extraInfo">optional extra information</param>
    /// <returns>a collection of n gram features</returns>
    public virtual ICollection<string> ExtractFeatures(string[] text, IDictionary<string, object> extraInfo)
    {
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text), "text must not be null");
        }

        JCG.List<string> features = [];

        for (int i = 0; i <= text.Length - minGram; i++)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("ng=");
            for (int y = 0; y < maxGram && i + y < text.Length; y++)
            {
                sb.Append(':');
                sb.Append(text[i + y]);
                int gramCount = y + 1;
                if (maxGram >= gramCount && gramCount >= minGram)
                {
                    features.Add(sb.ToString());
                }
            }
        }

        return features;
    }
}
