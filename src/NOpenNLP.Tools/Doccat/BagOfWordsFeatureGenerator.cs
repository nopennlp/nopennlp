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
using NOpenNLP.Tools.Util.Featuregen;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Doccat;

/// <summary>
/// Generates a feature for each word in a document.
/// </summary>
public class BagOfWordsFeatureGenerator : IFeatureGenerator
{
    private readonly bool useOnlyAllLetterTokens;

    public BagOfWordsFeatureGenerator()
        : this(false)
    {
    }

    internal BagOfWordsFeatureGenerator(bool useOnlyAllLetterTokens)
    {
        this.useOnlyAllLetterTokens = useOnlyAllLetterTokens;
    }

    public virtual ICollection<string> ExtractFeatures(string[] text, IDictionary<string, object> extraInformation)
    {
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text), "text must not be null");
        }

        ICollection<string> bagOfWords = new JCG.List<string>(text.Length);

        foreach (string word in text)
        {
            if (useOnlyAllLetterTokens)
            {
                StringPattern pattern = StringPattern.Recognize(word);

                if (pattern.IsAllLetter)
                {
                    bagOfWords.Add("bow=" + word);
                }
            }
            else
            {
                bagOfWords.Add("bow=" + word);
            }
        }

        return bagOfWords;
    }
}
