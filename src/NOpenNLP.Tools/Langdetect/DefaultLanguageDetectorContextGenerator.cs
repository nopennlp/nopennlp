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

using NOpenNLP.Tools.Ngram;
using NOpenNLP.Tools.Util.Normalizer;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Langdetect;

/// <summary>
/// A context generator for language detector.
/// </summary>
public class DefaultLanguageDetectorContextGenerator : ILanguageDetectorContextGenerator
{
    protected readonly int minLength;
    protected readonly int maxLength;
    protected readonly ICharSequenceNormalizer normalizer;

    /// <summary>
    /// Creates a customizable <see cref="DefaultLanguageDetectorContextGenerator"/>
    /// that computes ngrams from text.
    /// </summary>
    /// <param name="minLength">min ngrams chars</param>
    /// <param name="maxLength">max ngrams chars</param>
    /// <param name="normalizers">
    /// zero or more normalizers to be applied in to the text before extracting ngrams
    /// </param>
    public DefaultLanguageDetectorContextGenerator(int minLength, int maxLength,
        params ICharSequenceNormalizer[] normalizers)
    {
        this.minLength = minLength;
        this.maxLength = maxLength;

        this.normalizer = new AggregateCharSequenceNormalizer(normalizers);
    }

    /// <summary>
    /// Generates the context for a document using character ngrams.
    /// </summary>
    /// <param name="document">document to extract context from</param>
    /// <returns>the generated context</returns>
    public virtual string[] GetContext(string document)
    {
        JCG.List<string> context = [];

        NGramCharModel model = [];
        model.Add(normalizer.Normalize(document), minLength, maxLength);

        foreach (string token in model)
        {
            context.Add(token);
        }

        return [.. context];
    }
}
