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
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Lemmatizer;

/// <summary>
/// Class for creating an event stream out of data files for training a probabilistic lemmatizer.
/// </summary>
/// <param name="d">The data stream for this event stream.</param>
/// <param name="cg">The context generator which should be used in the creation of events
/// for this event stream.</param>
public class LemmaSampleEventStream(IObjectStream<LemmaSample?> d, ILemmatizerContextGenerator cg)
    : AbstractEventStream<LemmaSample>(d)
{
    private readonly ILemmatizerContextGenerator contextGenerator = cg; // NOpenNLP: made readonly

    /// <inheritdoc/>
    protected override IEnumerable<Event> CreateEvents(LemmaSample sample)
    {
        // NOpenNLP: upstream null-checks the sample and returns an empty iterator
        // otherwise. AbstractEventStream never passes null here, so the check is
        // dropped and the parameter stays non-nullable.
        var events = new JCG.List<Event>();
        string[] toksArray = sample.Tokens;
        string[] tagsArray = sample.Tags;
        string[] lemmasArray = LemmatizerME.EncodeLemmas(toksArray, sample.Lemmas);
        for (int ei = 0, el = sample.Tokens.Length; ei < el; ei++)
        {
            events.Add(new Event(lemmasArray[ei],
                contextGenerator.GetContext(ei, toksArray, tagsArray, lemmasArray)));
        }

        return events;
    }
}
