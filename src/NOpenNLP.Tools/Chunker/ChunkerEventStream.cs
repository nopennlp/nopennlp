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

namespace NOpenNLP.Tools.Chunker;

/// <summary>
/// Class for creating an event stream out of data files for training a chunker.
/// </summary>
public class ChunkerEventStream : AbstractEventStream<ChunkSample>
{
    private readonly IChunkerContextGenerator cg; // NOpenNLP: made readonly

    /// <summary>
    /// Creates a new event stream based on the specified data stream using the specified context generator.
    /// </summary>
    /// <param name="d">The data stream for this event stream.</param>
    /// <param name="cg">The context generator which should be used in the creation of events
    /// for this event stream.</param>
    public ChunkerEventStream(IObjectStream<ChunkSample?> d, IChunkerContextGenerator cg)
        : base(d)
        => this.cg = cg;

    /// <inheritdoc/>
    protected override IEnumerable<Event> CreateEvents(ChunkSample sample)
    {
        // NOpenNLP: upstream null-checks the sample here and returns an empty
        // iterator otherwise. AbstractEventStream never passes null, and the
        // parameter is non-nullable here, so the check is gone.
        var events = new JCG.List<Event>();
        string[] toksArray = sample.Sentence;
        string[] tagsArray = sample.Tags;
        string[] predsArray = sample.Preds;
        for (int ei = 0, el = sample.Sentence.Length; ei < el; ei++)
        {
            events.Add(new Event(predsArray[ei], cg.GetContext(ei, toksArray, tagsArray, predsArray)));
        }

        return events;
    }
}
