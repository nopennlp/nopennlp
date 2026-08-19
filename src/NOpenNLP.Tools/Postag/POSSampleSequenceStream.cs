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

using System.IO;
using NOpenNLP.Tools.Ml.Model;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Postag;

/// <summary>
/// A <see cref="ISequenceStream{T}"/> over <see cref="POSSample"/>s.
/// </summary>
public class POSSampleSequenceStream : ObjectStreamBase<Sequence<POSSample>?>, ISequenceStream<POSSample>
{
    private readonly IPOSContextGenerator pcg; // NOpenNLP: made readonly
    private readonly IObjectStream<POSSample?> psi; // NOpenNLP: made readonly

    /// <exception cref="IOException">if there is an error during reading</exception>
    public POSSampleSequenceStream(IObjectStream<POSSample?> psi)
        : this(psi, new DefaultPOSContextGenerator(null))
    {
    }

    /// <exception cref="IOException">if there is an error during reading</exception>
    public POSSampleSequenceStream(IObjectStream<POSSample?> psi, IPOSContextGenerator pcg)
    {
        this.psi = psi;
        this.pcg = pcg;
    }

    public virtual Event[] UpdateContext(Sequence<POSSample> sequence, AbstractModel model)
    {
        // NOpenNLP: upstream casts the raw Sequence back to Sequence<POSSample> under
        // @SuppressWarnings("unchecked"); ISequenceStream<T> is generic here, so the
        // parameter already has the right type.
        IPOSTagger tagger = new POSTaggerME(
            new POSModel("x-unspecified", model, null, new POSTaggerFactory()));
        string[] sentence = sequence.Source.Sentence;
        object[]? ac = sequence.Source.AddictionalContext;
        string[] tags = tagger.Tag(sequence.Source.Sentence);
        Event[] events = new Event[sentence.Length];
        POSSampleEventStream.GenerateEvents(sentence, tags, ac, pcg).CopyTo(events, 0);
        return events;
    }

    /// <inheritdoc/>
    /// <exception cref="IOException">if there is an error during reading</exception>
    public override Sequence<POSSample>? Read()
    {
        POSSample? sample = psi.Read();

        if (sample != null)
        {
            string[] sentence = sample.Sentence;
            string[] tags = sample.Tags;
            Event[] events = new Event[sentence.Length];

            for (int i = 0; i < sentence.Length; i++)
            {
                // it is safe to pass the tags as previous tags because
                // the context generator does not look for non predicted tags
                string[] context = pcg.GetContext(i, sentence, tags, null);

                events[i] = new Event(tags[i], context);
            }

            return new Sequence<POSSample>(events, sample);
        }

        return null;
    }

    /// <inheritdoc/>
    /// <exception cref="IOException">if there is an error during reading</exception>
    public override void Reset() => psi.Reset();

    /// <inheritdoc/>
    protected override void Dispose(bool disposing) => psi.Dispose();
}
