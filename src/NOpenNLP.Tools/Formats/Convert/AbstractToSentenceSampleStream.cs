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
using System.IO;
using NOpenNLP.Tools.Sentdetect;
using NOpenNLP.Tools.Tokenize;
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Formats.Convert;

public abstract class AbstractToSentenceSampleStream<T> : FilterObjectStream<T?, SentenceSample?>
    where T : class
{
    private readonly IDetokenizer detokenizer;

    private readonly int chunkSize;

    internal AbstractToSentenceSampleStream(IDetokenizer detokenizer,
        IObjectStream<T?> samples, int chunkSize)
        : base(samples)
    {
        this.detokenizer = detokenizer
            ?? throw new ArgumentNullException(nameof(detokenizer), "detokenizer must not be null");

        if (chunkSize < 0)
        {
            throw new ArgumentException(
                "chunkSize must be zero or larger but was " + chunkSize + "!", nameof(chunkSize));
        }

        this.chunkSize = chunkSize > 0 ? chunkSize : int.MaxValue;
    }

    protected abstract string[] ToSentence(T sample);

    /// <inheritdoc/>
    /// <exception cref="IOException">if there is an error during reading</exception>
    public override SentenceSample? Read()
    {
        IList<string[]> sentences = new JCG.List<string[]>();

        T? posSample;
        int chunks = 0;
        while ((posSample = samples.Read()) != null && chunks < chunkSize)
        {
            sentences.Add(ToSentence(posSample));
            chunks++;
        }

        if (sentences.Count > 0)
        {
            return new SentenceSample(detokenizer, [.. sentences]);
        }
        else if (posSample != null)
        {
            return Read(); // filter out empty line
        }

        return null; // last sample was read
    }
}
