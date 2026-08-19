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

using J2N.Text;
using NOpenNLP.Tools.Ml.Model;
using NOpenNLP.Tools.Util;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Sentdetect;

public class SDEventStream : AbstractEventStream<SentenceSample>
{
    private readonly ISDContextGenerator cg; // NOpenNLP: made readonly
    private readonly IEndOfSentenceScanner scanner; // NOpenNLP: made readonly

    /// <summary>
    /// Initializes the current instance.
    /// </summary>
    /// <param name="samples">the sample stream.</param>
    /// <param name="cg">the context generator.</param>
    /// <param name="scanner">the end of sentence scanner.</param>
    public SDEventStream(IObjectStream<SentenceSample?> samples, ISDContextGenerator cg,
        IEndOfSentenceScanner scanner)
        : base(samples)
    {
        this.cg = cg;
        this.scanner = scanner;
    }

    protected override IEnumerable<Event> CreateEvents(SentenceSample sample)
    {
        ICollection<Event> events = new JCG.List<Event>();

        foreach (Span sentenceSpan in sample.GetSentences())
        {
            string sentenceString = sentenceSpan.GetCoveredText(sample.Document.AsCharSequence()).ToString();

            // NOpenNLP: upstream drives an Iterator and calls hasNext() to detect the
            // last candidate. GetPositions returns an IList, so the same "is this the
            // last one" test is an index comparison here.
            IList<int> positions = scanner.GetPositions(sentenceString);

            for (int i = 0; i < positions.Count; i++)
            {
                int candidate = positions[i];
                string type = SentenceDetectorME.NO_SPLIT;
                if (i == positions.Count - 1)
                {
                    type = SentenceDetectorME.SPLIT;
                }

                events.Add(new Event(type, cg.GetContext(sample.Document,
                    sentenceSpan.Start + candidate)));
            }
        }

        return events;
    }
}
