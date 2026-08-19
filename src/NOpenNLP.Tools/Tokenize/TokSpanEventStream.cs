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
using System.Text.RegularExpressions;
using NOpenNLP.Tools.Ml.Model;
using NOpenNLP.Tools.Tokenize.Lang;
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Tokenize;

/// <summary>
/// This class reads the <see cref="TokenSample"/>s from the given
/// <see cref="IObjectStream{T}"/> and converts the <see cref="TokenSample"/>s into
/// <see cref="Event"/>s which can be used by the maxent library for training.
/// </summary>
public class TokSpanEventStream : AbstractEventStream<TokenSample>
{
    private readonly ITokenContextGenerator cg; // NOpenNLP: made readonly
    private readonly bool skipAlphaNumerics; // NOpenNLP: made readonly
    private readonly Regex alphaNumeric;

    /// <summary>
    /// Initializes the current instance.
    /// </summary>
    /// <param name="tokenSamples">the samples to create events from</param>
    /// <param name="skipAlphaNumerics">if true alpha numerics are skipped</param>
    /// <param name="alphaNumeric">the alpha numeric pattern</param>
    /// <param name="cg">the context generator</param>
    public TokSpanEventStream(IObjectStream<TokenSample?> tokenSamples,
        bool skipAlphaNumerics, Regex alphaNumeric, ITokenContextGenerator cg)
        : base(tokenSamples)
    {
        this.alphaNumeric = alphaNumeric;
        this.skipAlphaNumerics = skipAlphaNumerics;
        this.cg = cg;
    }

    /// <summary>
    /// Initializes the current instance.
    /// </summary>
    /// <param name="tokenSamples">the samples to create events from</param>
    /// <param name="skipAlphaNumerics">if true alpha numerics are skipped</param>
    /// <param name="cg">the context generator</param>
    public TokSpanEventStream(IObjectStream<TokenSample?> tokenSamples,
        bool skipAlphaNumerics, ITokenContextGenerator cg)
        : base(tokenSamples)
    {
        Factory factory = new Factory();
        this.alphaNumeric = factory.GetAlphanumeric(null);
        this.skipAlphaNumerics = skipAlphaNumerics;
        this.cg = cg;
    }

    /// <summary>
    /// Initializes the current instance.
    /// </summary>
    /// <param name="tokenSamples">the samples to create events from</param>
    /// <param name="skipAlphaNumerics">if true alpha numerics are skipped</param>
    public TokSpanEventStream(IObjectStream<TokenSample?> tokenSamples,
        bool skipAlphaNumerics)
        : this(tokenSamples, skipAlphaNumerics, new DefaultTokenContextGenerator())
    {
    }

    /// <summary>
    /// Adds training events to the event stream for each of the specified tokens.
    /// </summary>
    /// <param name="tokenSample">character offsets into the specified text.</param>
    /// <returns>The text of the tokens.</returns>
    protected override IEnumerable<Event> CreateEvents(TokenSample tokenSample)
    {
        JCG.List<Event> events = new JCG.List<Event>(50);

        Span[] tokens = tokenSample.TokenSpans;
        string text = tokenSample.Text;

        if (tokens.Length > 0)
        {
            int start = tokens[0].Start;
            int end = tokens[tokens.Length - 1].End;

            // NOpenNLP: Java substring(begin, end) takes an end index; .NET takes a length.
            string sent = text.Substring(start, end - start);

            Span[] candTokens = WhitespaceTokenizer.INSTANCE.TokenizePos(sent);

            int firstTrainingToken = -1;
            int lastTrainingToken = -1;
            foreach (Span candToken in candTokens)
            {
                Span cSpan = candToken;
                string ctok = sent.Substring(cSpan.Start, cSpan.End - cSpan.Start);
                //adjust cSpan to text offsets
                cSpan = new Span(cSpan.Start + start, cSpan.End + start);
                //should we skip this token
                // NOpenNLP: upstream uses Matcher.matches(), which anchors at both ends.
                // The alphanumeric patterns from Lang.Factory already carry ^ and $, so
                // IsMatch is equivalent, and this matches how TokenizerME applies them.
                if (ctok.Length > 1 && (!skipAlphaNumerics || !alphaNumeric.IsMatch(ctok)))
                {
                    //find offsets of annotated tokens inside of candidate tokens
                    bool foundTrainingTokens = false;
                    for (int ti = lastTrainingToken + 1; ti < tokens.Length; ti++)
                    {
                        if (cSpan.Contains(tokens[ti]))
                        {
                            if (!foundTrainingTokens)
                            {
                                firstTrainingToken = ti;
                                foundTrainingTokens = true;
                            }

                            lastTrainingToken = ti;
                        }
                        else if (cSpan.End < tokens[ti].End)
                        {
                            break;
                        }
                        else if (tokens[ti].End < cSpan.Start)
                        {
                            //keep looking
                        }
                        else
                        {
                            Console.Out.WriteLine("Bad training token: " + tokens[ti] + " cand: " + cSpan +
                                " token=" + text.Substring(tokens[ti].Start, tokens[ti].End - tokens[ti].Start));
                        }
                    }

                    // create training data
                    if (foundTrainingTokens)
                    {
                        for (int ti = firstTrainingToken; ti <= lastTrainingToken; ti++)
                        {
                            Span tSpan = tokens[ti];
                            int cStart = cSpan.Start;
                            for (int i = tSpan.Start + 1; i < tSpan.End; i++)
                            {
                                string[] context = cg.GetContext(ctok, i - cStart);
                                events.Add(new Event(TokenizerME.NO_SPLIT, context));
                            }

                            if (tSpan.End != cSpan.End)
                            {
                                string[] context = cg.GetContext(ctok, tSpan.End - cStart);
                                events.Add(new Event(TokenizerME.SPLIT, context));
                            }
                        }
                    }
                }
            }
        }

        return events;
    }
}
