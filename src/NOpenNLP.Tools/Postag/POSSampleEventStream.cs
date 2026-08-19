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

namespace NOpenNLP.Tools.Postag;

/// <summary>
/// This class reads the <see cref="POSSample"/>s from the given
/// <see cref="IObjectStream{T}"/> and converts the <see cref="POSSample"/>s into
/// <see cref="Event"/>s which can be used by the maxent library for training.
/// </summary>
public class POSSampleEventStream : AbstractEventStream<POSSample>
{
    /// <summary>
    /// The <see cref="IPOSContextGenerator"/> used
    /// to create the training <see cref="Event"/>s.
    /// </summary>
    private readonly IPOSContextGenerator cg; // NOpenNLP: made readonly

    /// <summary>
    /// Initializes the current instance with the given samples and the
    /// given <see cref="IPOSContextGenerator"/>.
    /// </summary>
    /// <param name="samples">the samples to create events from</param>
    /// <param name="cg">the context generator</param>
    public POSSampleEventStream(IObjectStream<POSSample?> samples, IPOSContextGenerator cg)
        : base(samples)
        => this.cg = cg;

    /// <summary>
    /// Initializes the current instance with given samples
    /// and a <see cref="DefaultPOSContextGenerator"/>.
    /// </summary>
    /// <param name="samples">the samples to create events from</param>
    public POSSampleEventStream(IObjectStream<POSSample?> samples)
        : this(samples, new DefaultPOSContextGenerator(null))
    {
    }

    protected override IEnumerable<Event> CreateEvents(POSSample sample)
    {
        string[] sentence = sample.Sentence;
        string[] tags = sample.Tags;
        object[]? ac = sample.AddictionalContext;
        return GenerateEvents(sentence, tags, ac, cg);
    }

    public static IList<Event> GenerateEvents(string[] sentence, string[] tags,
        object[]? additionalContext, IPOSContextGenerator cg)
    {
        var events = new JCG.List<Event>(sentence.Length);

        for (int i = 0; i < sentence.Length; i++)
        {
            // it is safe to pass the tags as previous tags because
            // the context generator does not look for non predicted tags
            string[] context = cg.GetContext(i, sentence, tags, additionalContext);

            events.Add(new Event(tags[i], context));
        }

        return events;
    }

    public static IList<Event> GenerateEvents(string[] sentence, string[] tags,
        IPOSContextGenerator cg) =>
        GenerateEvents(sentence, tags, null, cg);
}
