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
using NOpenNLP.Tools.Ml.Model;
using NOpenNLP.Tools.Util;
using OpenNlpDictionary = NOpenNLP.Tools.Dictionary.Dictionary;

namespace NOpenNLP.Tools.Parser.Chunking;

/// <summary>
/// Wrapper class for one of four parser event streams. The particular event stream is specified
/// at construction.
/// </summary>
public class ParserEventStream : AbstractParserEventStream
{
    protected BuildContextGenerator bcg = null!;
    protected CheckContextGenerator kcg = null!;

    /// <summary>
    /// Create an event stream based on the specified data stream of the specified type using
    /// the specified head rules.
    /// </summary>
    /// <param name="d">A 1-parse-per-line Penn Treebank Style parse.</param>
    /// <param name="rules">The head rules.</param>
    /// <param name="etype">The type of events desired (tag, chunk, build, or check).</param>
    /// <param name="dict">A tri-gram dictionary to reduce feature generation.</param>
    public ParserEventStream(IObjectStream<Parse?> d, IHeadRules rules,
        ParserEventTypeEnum etype, OpenNlpDictionary? dict = null)
        : base(d, rules, etype, dict)
    {
    }

    /// <inheritdoc/>
    protected override void Init()
    {
        if (etype == ParserEventTypeEnum.BUILD)
        {
            this.bcg = dict != null ? new BuildContextGenerator(dict) : new BuildContextGenerator();
        }
        else if (etype == ParserEventTypeEnum.CHECK)
        {
            this.kcg = new CheckContextGenerator();
        }
    }

    /// <summary>
    /// Returns true if the specified child is the first child of the specified parent.
    /// </summary>
    /// <param name="child">The child parse.</param>
    /// <param name="parent">The parent parse.</param>
    /// <returns>true if the specified child is the first child of the specified parent;
    /// false otherwise.</returns>
    protected virtual bool FirstChild(Parse child, Parse parent) =>
        AbstractBottomUpParser.CollapsePunctuation(parent.GetChildren(), punctSet)[0] == child;

    public static Parse[] ReduceChunks(Parse[] chunks, int ci, Parse parent)
    {
        string type = parent.Type;
        //  perform reduce
        int reduceStart = ci;
        int reduceEnd = ci;
        while (reduceStart >= 0 && chunks[reduceStart].Parent == parent)
        {
            reduceStart--;
        }

        reduceStart++;
        Parse[] reducedChunks;
        if (!type.Equals(AbstractBottomUpParser.TOP_NODE))
        {
            //total - num_removed + 1 (for new node)
            reducedChunks = new Parse[chunks.Length - (reduceEnd - reduceStart + 1) + 1];
            //insert nodes before reduction
            Array.Copy(chunks, 0, reducedChunks, 0, reduceStart);
            //insert reduced node
            reducedChunks[reduceStart] = parent;
            //propagate punctuation sets
            parent.SetPrevPunctuation(chunks[reduceStart].PreviousPunctuationSet);
            parent.SetNextPunctuation(chunks[reduceEnd].NextPunctuationSet);
            //insert nodes after reduction
            int ri = reduceStart + 1;
            for (int rci = reduceEnd + 1; rci < chunks.Length; rci++)
            {
                reducedChunks[ri] = chunks[rci];
                ri++;
            }

            // NOpenNLP: upstream assigns ci = reduceStart - 1 here, but ci is a
            // by-value parameter and the result is never read, so the assignment
            // is dropped. The caller recomputes the same value itself.
        }
        else
        {
            reducedChunks = [];
        }

        return reducedChunks;
    }

    /// <summary>
    /// Adds events for parsing (post tagging and chunking to the specified list of events for
    /// the specified parse chunks.
    /// </summary>
    /// <param name="parseEvents">The events for the specified chunks.</param>
    /// <param name="chunks">The incomplete parses to be parsed.</param>
    protected override void AddParseEvents(IList<Event> parseEvents, Parse[] chunks)
    {
        int ci = 0;
        while (ci < chunks.Length)
        {
            var c = chunks[ci];
            var parent = c.Parent;
            if (parent != null)
            {
                string type = parent.Type;
                string outcome;
                if (FirstChild(c, parent))
                {
                    outcome = AbstractBottomUpParser.START + type;
                }
                else
                {
                    outcome = AbstractBottomUpParser.CONT + type;
                }

                c.Label = outcome;
                if (etype == ParserEventTypeEnum.BUILD)
                {
                    parseEvents.Add(new Event(outcome, bcg.GetContext(chunks, ci)));
                }

                int start = ci - 1;
                while (start >= 0 && chunks[start].Parent == parent)
                {
                    start--;
                }

                if (LastChild(c, parent))
                {
                    if (etype == ParserEventTypeEnum.CHECK)
                    {
                        parseEvents.Add(new Event(Parser.COMPLETE,
                            kcg.GetContext(chunks, type, start + 1, ci)));
                    }

                    //perform reduce
                    int reduceStart = ci;
                    while (reduceStart >= 0 && chunks[reduceStart].Parent == parent)
                    {
                        reduceStart--;
                    }

                    reduceStart++;
                    chunks = ReduceChunks(chunks, ci, parent);
                    ci = reduceStart - 1; //ci will be incremented at end of loop
                }
                else
                {
                    if (etype == ParserEventTypeEnum.CHECK)
                    {
                        parseEvents.Add(new Event(Parser.INCOMPLETE,
                            kcg.GetContext(chunks, type, start + 1, ci)));
                    }
                }
            }

            ci++;
        }
    }
}
