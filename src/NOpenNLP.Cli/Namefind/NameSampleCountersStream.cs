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
using NOpenNLP.Tools.Namefind;
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Cmdline.Namefind;

/// <summary>
/// Counts tokens, sentences and names by type
/// </summary>
public class NameSampleCountersStream : FilterObjectStream<NameSample?, NameSample?>
{
    private int sentenceCount;
    private int tokenCount;

    // NOpenNLP: Span.Type is nullable and Java's HashMap admits a null key, so a J2N
    // dictionary is used -- the BCL Dictionary throws on a null key.
    private JCG.Dictionary<string?, int> nameCounters = new JCG.Dictionary<string?, int>();

    protected internal NameSampleCountersStream(IObjectStream<NameSample?> samples)
        : base(samples)
    {
    }

    /// <inheritdoc/>
    /// <exception cref="IOException">if reading from the underlying stream fails</exception>
    public override NameSample? Read()
    {
        NameSample? sample = samples.Read();

        if (sample != null)
        {
            sentenceCount++;
            tokenCount += sample.Sentence.Length;

            foreach (Span nameSpan in sample.Names)
            {
                // NOpenNLP: upstream reads the counter out of the map, which yields null
                // for an absent type, and substitutes zero. TryGetValue says the same
                // thing without the boxing.
                if (!nameCounters.TryGetValue(nameSpan.Type, out int nameCounter))
                {
                    nameCounter = 0;
                }

                nameCounters[nameSpan.Type] = nameCounter + 1;
            }
        }

        return sample;
    }

    /// <inheritdoc/>
    /// <exception cref="IOException">if resetting the underlying stream fails</exception>
    /// <exception cref="NotSupportedException">if the underlying stream cannot be reset</exception>
    public override void Reset()
    {
        base.Reset();

        sentenceCount = 0;
        tokenCount = 0;
        nameCounters = new JCG.Dictionary<string?, int>();
    }

    public virtual int SentenceCount => sentenceCount;

    public virtual int TokenCount => tokenCount;

    // NOpenNLP: upstream wraps the map in Collections.unmodifiableMap. The BCL's
    // ReadOnlyDictionary constrains its key to notnull, which a nullable Span.Type
    // cannot satisfy, so the defence is a copy instead -- callers still cannot reach
    // the counters this stream keeps updating.
    public virtual IDictionary<string?, int> NameCounters =>
        new JCG.Dictionary<string?, int>(nameCounters);

    public virtual void PrintSummary()
    {
        Console.WriteLine("Training data summary:");
        Console.WriteLine("#Sentences: " + SentenceCount);
        Console.WriteLine("#Tokens: " + TokenCount);

        // NOpenNLP: upstream accumulates totalNames here but never prints it; the
        // accumulation is dropped rather than kept as an unused local.
        foreach (KeyValuePair<string?, int> counter in NameCounters)
        {
            Console.WriteLine("#" + counter.Key + " entities: " + counter.Value);
        }
    }
}
