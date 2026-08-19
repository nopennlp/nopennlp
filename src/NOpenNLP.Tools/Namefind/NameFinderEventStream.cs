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
using NOpenNLP.Tools.Ml.Model;
using NOpenNLP.Tools.Util;
using NOpenNLP.Tools.Util.Featuregen;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Namefind;

/// <summary>
/// Class for creating an event stream out of data files for training a name
/// finder.
/// </summary>
public class NameFinderEventStream : AbstractEventStream<NameSample>
{
    // NOpenNLP: made readonly
    private readonly INameContextGenerator contextGenerator;

    private readonly AdditionalContextFeatureGenerator additionalContextFeatureGenerator = new();

    private readonly ISequenceCodec<string> codec;

    private readonly string? defaultType;

    /// <summary>
    /// Creates a new name finder event stream using the specified data stream and context generator.
    /// </summary>
    /// <param name="dataStream">The data stream of events.</param>
    /// <param name="type">null or overrides the type parameter in the provided samples</param>
    /// <param name="contextGenerator">The context generator used to generate features for the event stream.</param>
    /// <param name="codec">the sequence codec, or null for a <see cref="BioCodec"/></param>
    public NameFinderEventStream(IObjectStream<NameSample?> dataStream, string? type,
        INameContextGenerator contextGenerator, ISequenceCodec<string>? codec)
        : base(dataStream)
    {
        this.codec = codec ?? new BioCodec();

        this.contextGenerator = contextGenerator;
        this.contextGenerator.AddFeatureGenerator(
            new WindowFeatureGenerator(additionalContextFeatureGenerator, 8, 8));

        defaultType = type;
    }

    public NameFinderEventStream(IObjectStream<NameSample?> dataStream)
        : this(dataStream, null, new DefaultNameContextGenerator(), null)
    {
    }

    /// <summary>
    /// Generates the name tag outcomes (start, continue, other) for each token in a sentence
    /// with the specified length using the specified name spans.
    /// </summary>
    /// <param name="names">Token spans for each of the names.</param>
    /// <param name="type">null or overrides the type parameter in the provided samples</param>
    /// <param name="length">The length of the sentence.</param>
    /// <returns>An array of start, continue, other outcomes based on the specified
    ///     names and sentence length.</returns>
    /// <remarks>Deprecated: use the <see cref="BioCodec"/> implementation of the
    ///     <see cref="ISequenceValidator{T}"/> instead!</remarks>
    public static string[] GenerateOutcomes(Span[] names, string? type, int length)
    {
        string[] outcomes = new string[length];
        for (int i = 0; i < outcomes.Length; i++)
        {
            outcomes[i] = NameFinderME.OTHER;
        }

        foreach (var name in names)
        {
            if (name.Type == null)
            {
                outcomes[name.Start] = type + "-" + NameFinderME.START;
            }
            else
            {
                outcomes[name.Start] = name.Type + "-" + NameFinderME.START;
            }

            // now iterate from begin + 1 till end
            for (int i = name.Start + 1; i < name.End; i++)
            {
                if (name.Type == null)
                {
                    outcomes[i] = type + "-" + NameFinderME.CONTINUE;
                }
                else
                {
                    outcomes[i] = name.Type + "-" + NameFinderME.CONTINUE;
                }
            }
        }

        return outcomes;
    }

    public static IList<Event> GenerateEvents(string[] sentence, string[] outcomes,
        INameContextGenerator cg)
    {
        var events = new JCG.List<Event>(outcomes.Length);
        for (int i = 0; i < outcomes.Length; i++)
        {
            // NOpenNLP: IBeamSearchContextGenerator.GetContext does not annotate
            // additionalContext as nullable, but upstream passes null here.
            events.Add(new Event(outcomes[i], cg.GetContext(i, sentence, outcomes, null!)));
        }

        cg.UpdateAdaptiveData(sentence, outcomes);

        return events;
    }

    /// <inheritdoc/>
    protected override IEnumerable<Event> CreateEvents(NameSample sample)
    {
        if (sample.IsClearAdaptiveDataSet)
        {
            contextGenerator.ClearAdaptiveData();
        }

        var names = sample.Names;
        if (defaultType != null)
        {
            OverrideType(names);
        }

        string[] outcomes = codec.Encode(names, sample.Sentence.Length);
        // string[] outcomes = GenerateOutcomes(sample.Names, type, sample.Sentence.Length);
        // NOpenNLP: AdditionalContextFeatureGenerator holds the context in a nullable
        // field but does not annotate the setter's parameter as nullable.
        additionalContextFeatureGenerator.SetCurrentContext(sample.AdditionalContext!);
        string[] tokens = new string[sample.Sentence.Length];

        for (int i = 0; i < sample.Sentence.Length; i++)
        {
            tokens[i] = sample.Sentence[i];
        }

        return GenerateEvents(tokens, outcomes, contextGenerator);
    }

    private void OverrideType(Span[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            var n = names[i];
            names[i] = new Span(n.Start, n.End, defaultType, n.Prob);
        }
    }

    /// <summary>
    /// Generated previous decision features for each token based on contents of the specified map.
    /// </summary>
    /// <param name="tokens">The token for which the context is generated.</param>
    /// <param name="prevMap">A mapping of tokens to their previous decisions.</param>
    /// <returns>An additional context array with features for each token.</returns>
    public static string[][] AdditionalContext(string[] tokens, IDictionary<string, string> prevMap)
    {
        string[][] ac = new string[tokens.Length][];
        for (int ti = 0; ti < tokens.Length; ti++)
        {
            // NOpenNLP: Java's Map.get returns null for an absent key, and the
            // concatenation below renders that as the literal text "null", which is
            // emitted verbatim as a feature. Preserved here.
            string pt = prevMap.TryGetValue(tokens[ti], out string? value) ? value : "null";
            ac[ti] = ["pd=" + pt];
        }

        return ac;
    }
}
