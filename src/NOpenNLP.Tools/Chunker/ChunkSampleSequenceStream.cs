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
using System.IO;
using NOpenNLP.Tools.Ml.Model;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Chunker;

public class ChunkSampleSequenceStream(IObjectStream<ChunkSample?> samples,
    IChunkerContextGenerator contextGenerator)
    : ObjectStreamBase<Sequence<ChunkSample>?>, ISequenceStream<ChunkSample>
{
    /// <inheritdoc/>
    /// <exception cref="IOException">if there is an error during reading</exception>
    public override Sequence<ChunkSample>? Read()
    {
        ChunkSample? sample = samples.Read();

        if (sample != null)
        {
            string[] sentence = sample.Sentence;
            string[] tags = sample.Tags;
            Event[] events = new Event[sentence.Length];

            for (int i = 0; i < sentence.Length; i++)
            {
                // it is safe to pass the tags as previous tags because
                // the context generator does not look for non predicted tags
                // NOpenNLP: upstream passes a literal null for preds here. The
                // parameter is non-nullable, so the null is forgiven rather than
                // widening the interface for a path upstream itself flags as
                // unfinished (see the TODO in ChunkerME.Train).
                string[] context = contextGenerator.GetContext(i, sentence, tags, null!);

                events[i] = new Event(tags[i], context);
            }

            return new Sequence<ChunkSample>(events, sample);
        }

        return null;
    }

    /// <inheritdoc/>
    // NOpenNLP: upstream returns null here, which the ported interface does not
    // allow. Perceptron sequence learning is the only caller, and upstream has a
    // TODO saying it should be implemented for it; throwing makes the gap loud
    // rather than surfacing as a NullReferenceException inside the trainer.
    public virtual Event[] UpdateContext(Sequence<ChunkSample> sequence, AbstractModel model) =>
        throw new NotSupportedException(
            "UpdateContext is not implemented for the chunker sequence stream.");

    /// <inheritdoc/>
    public override void Reset() => samples.Reset();

    /// <inheritdoc/>
    protected override void Dispose(bool disposing) => samples.Dispose();
}
