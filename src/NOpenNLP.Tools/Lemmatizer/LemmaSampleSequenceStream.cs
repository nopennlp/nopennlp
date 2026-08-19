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

namespace NOpenNLP.Tools.Lemmatizer;

public class LemmaSampleSequenceStream(IObjectStream<LemmaSample?> samples,
    ILemmatizerContextGenerator contextGenerator)
    : ObjectStreamBase<Sequence<LemmaSample>?>, ISequenceStream<LemmaSample>
{
    /// <inheritdoc/>
    /// <exception cref="IOException">if there is an error during reading</exception>
    public override Sequence<LemmaSample>? Read()
    {
        LemmaSample? sample = samples.Read();

        if (sample != null)
        {
            string[] sentence = sample.Tokens;
            string[] tags = sample.Tags;
            string[] preds = sample.Lemmas;
            Event[] events = new Event[sentence.Length];

            for (int i = 0; i < sentence.Length; i++)
            {
                // it is safe to pass the tags as previous tags because
                // the context generator does not look for non predicted tags
                string[] context = contextGenerator.GetContext(i, sentence, tags, preds);

                events[i] = new Event(tags[i], context);
            }

            return new Sequence<LemmaSample>(events, sample);
        }

        return null;
    }

    /// <inheritdoc/>
    // NOpenNLP: upstream returns null here, which the ported interface does not
    // allow. Perceptron sequence learning is the only caller, and upstream has a
    // TODO saying it should be implemented for it; throwing makes the gap loud
    // rather than surfacing as a NullReferenceException inside the trainer.
    public virtual Event[] UpdateContext(Sequence<LemmaSample> sequence, AbstractModel model) =>
        throw new NotSupportedException(
            "UpdateContext is not implemented for the lemmatizer sequence stream.");

    /// <inheritdoc/>
    public override void Reset() => samples.Reset();

    /// <inheritdoc/>
    protected override void Dispose(bool disposing) => samples.Dispose();
}
