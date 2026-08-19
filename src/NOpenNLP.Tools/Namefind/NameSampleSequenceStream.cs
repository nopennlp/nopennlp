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

using System.Collections.Generic;
using System.IO;
using NOpenNLP.Tools.Ml.Model;
using NOpenNLP.Tools.Util;
using NOpenNLP.Tools.Util.Featuregen;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Namefind;

public class NameSampleSequenceStream : ObjectStreamBase<Sequence<NameSample>?>, ISequenceStream<NameSample>
{
    // NOpenNLP: made readonly
    private readonly INameContextGenerator pcg;
    private readonly bool useOutcomes;
    private readonly IObjectStream<NameSample?> psi;
    private readonly ISequenceCodec<string> seqCodec;

    /// <exception cref="IOException">if there is an error during reading</exception>
    public NameSampleSequenceStream(IObjectStream<NameSample?> psi)
        : this(psi, new DefaultNameContextGenerator((IAdaptiveFeatureGenerator[]?)null), true)
    {
    }

    /// <exception cref="IOException">if there is an error during reading</exception>
    public NameSampleSequenceStream(IObjectStream<NameSample?> psi, IAdaptiveFeatureGenerator featureGen)
        : this(psi, new DefaultNameContextGenerator(featureGen), true)
    {
    }

    /// <exception cref="IOException">if there is an error during reading</exception>
    public NameSampleSequenceStream(IObjectStream<NameSample?> psi,
        IAdaptiveFeatureGenerator featureGen, bool useOutcomes)
        : this(psi, new DefaultNameContextGenerator(featureGen), useOutcomes)
    {
    }

    /// <exception cref="IOException">if there is an error during reading</exception>
    public NameSampleSequenceStream(IObjectStream<NameSample?> psi, INameContextGenerator pcg)
        : this(psi, pcg, true)
    {
    }

    /// <exception cref="IOException">if there is an error during reading</exception>
    public NameSampleSequenceStream(IObjectStream<NameSample?> psi, INameContextGenerator pcg, bool useOutcomes)
        : this(psi, pcg, useOutcomes, new BioCodec())
    {
    }

    /// <exception cref="IOException">if there is an error during reading</exception>
    public NameSampleSequenceStream(IObjectStream<NameSample?> psi, INameContextGenerator pcg,
        bool useOutcomes, ISequenceCodec<string> seqCodec)
    {
        this.psi = psi;
        this.useOutcomes = useOutcomes;
        this.pcg = pcg;
        this.seqCodec = seqCodec;
    }

    /// <inheritdoc/>
    public virtual Event[] UpdateContext(Sequence<NameSample> sequence, AbstractModel model)
    {
        ITokenNameFinder tagger = new NameFinderME(new TokenNameFinderModel(
            "x-unspecified", model, new JCG.Dictionary<string, object>(), new JCG.Dictionary<string, string>()));
        string[] sentence = sequence.Source.Sentence;
        string[] tags = seqCodec.Encode(tagger.Find(sentence), sentence.Length);
        var events = new Event[sentence.Length];

        NameFinderEventStream.GenerateEvents(sentence, tags, pcg).CopyTo(events, 0);

        return events;
    }

    /// <inheritdoc/>
    /// <exception cref="IOException">if there is an error during reading</exception>
    public override Sequence<NameSample>? Read()
    {
        NameSample? sample = psi.Read();
        if (sample != null)
        {
            string[] sentence = sample.Sentence;
            string[] tags = seqCodec.Encode(sample.Names, sentence.Length);
            var events = new Event[sentence.Length];

            for (int i = 0; i < sentence.Length; i++)
            {
                // it is safe to pass the tags as previous tags because
                // the context generator does not look for non predicted tags
                string[] context;
                // NOpenNLP: IBeamSearchContextGenerator.GetContext does not annotate
                // priorDecisions or additionalContext as nullable, but upstream passes
                // null for them here.
                if (useOutcomes)
                {
                    context = pcg.GetContext(i, sentence, tags, null!);
                }
                else
                {
                    context = pcg.GetContext(i, sentence, null!, null!);
                }

                events[i] = new Event(tags[i], context);
            }

            return new Sequence<NameSample>(events, sample);
        }
        else
        {
            return null;
        }
    }

    /// <inheritdoc/>
    /// <exception cref="IOException">if there is an error during reading</exception>
    public override void Reset() => psi.Reset();

    /// <inheritdoc/>
    protected override void Dispose(bool disposing) => psi.Dispose();
}
