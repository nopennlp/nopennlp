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
using JCG = J2N.Collections.Generic;
using OpenNlpDictionary = NOpenNLP.Tools.Dictionary.Dictionary;

namespace NOpenNLP.Tools.Parser.Treeinsert;

public class ParserEventStream : AbstractParserEventStream
{
    protected AttachContextGenerator attachContextGenerator = null!;
    protected BuildContextGenerator buildContextGenerator = null!;
    protected CheckContextGenerator checkContextGenerator = null!;

    private const bool debug = false;

    public ParserEventStream(IObjectStream<Parse?> d, IHeadRules rules,
        ParserEventTypeEnum etype, OpenNlpDictionary? dict)
        : base(d, rules, etype, dict)
    {
    }

    public ParserEventStream(IObjectStream<Parse?> d, IHeadRules rules, ParserEventTypeEnum etype)
        : base(d, rules, etype)
    {
    }

    /// <inheritdoc/>
    protected override void Init()
    {
        buildContextGenerator = new BuildContextGenerator();
        attachContextGenerator = new AttachContextGenerator(punctSet);
        checkContextGenerator = new CheckContextGenerator(punctSet);
    }

    /// <summary>
    /// Returns a set of parent nodes which consist of the immediate
    /// parent of the specified node and any of its parent which
    /// share the same syntactic type.
    /// </summary>
    /// <param name="node">The node whose parents are to be returned.</param>
    /// <returns>a set of parent nodes.</returns>
    private IDictionary<Parse, int> GetNonAdjoinedParent(Parse node)
    {
        IDictionary<Parse, int> parents = new JCG.Dictionary<Parse, int>();
        Parse parent = node.Parent!;
        int index = IndexOf(node, parent);
        parents[parent] = index;
        while (parent.Type.Equals(node.Type))
        {
            node = parent;
            parent = parent.Parent!;
            index = IndexOf(node, parent);
            parents[parent] = index;
        }

        return parents;
    }

    private int IndexOf(Parse child, Parse parent)
    {
        Parse[] kids = Parser.CollapsePunctuation(parent.GetChildren(), punctSet);
        for (int ki = 0; ki < kids.Length; ki++)
        {
            if (child == kids[ki])
            {
                return ki;
            }
        }

        return -1;
    }

    private int NonPunctChildCount(Parse node) =>
        Parser.CollapsePunctuation(node.GetChildren(), punctSet).Length;

    /// <inheritdoc/>
    protected override bool LastChild(Parse child, Parse parent)
    {
        bool lc = base.LastChild(child, parent);
        while (!lc)
        {
            Parse cp = child.Parent!;
            if (cp != parent && cp.Type.Equals(child.Type))
            {
                lc = base.LastChild(cp, parent);
                child = cp;
            }
            else
            {
                break;
            }
        }

        return lc;
    }

    /// <inheritdoc/>
    protected override void AddParseEvents(IList<Event> parseEvents, Parse[] chunks)
    {
        /* Frontier nodes built from node in a completed parse.  Specifically,
         * they have all their children regardless of the stage of parsing.*/
        IList<Parse> rightFrontier = new JCG.List<Parse>();
        IList<Parse> builtNodes = new JCG.List<Parse>();
        /* Nodes which characterize what the parse looks like to the parser as its being built.
         * Specifically, these nodes don't have all their children attached like the parents of
         * the chunk nodes do.*/
        Parse[] currentChunks = new Parse[chunks.Length];
        for (int ci = 0; ci < chunks.Length; ci++)
        {
            currentChunks[ci] = (Parse)chunks[ci].Clone();
            currentChunks[ci].SetPrevPunctuation(chunks[ci].PreviousPunctuationSet);
            currentChunks[ci].SetNextPunctuation(chunks[ci].NextPunctuationSet);
            currentChunks[ci].Label = Parser.COMPLETE;
            chunks[ci].Label = Parser.COMPLETE;
        }

        for (int ci = 0; ci < chunks.Length; ci++)
        {
            Parse parent = chunks[ci].Parent!;
            Parse prevParent = chunks[ci];
            int off = 0;
            //build un-built parents
            if (!chunks[ci].IsPosTag)
            {
                builtNodes.Insert(off++, chunks[ci]);
            }

            //perform build stages
            while (!parent.Type.Equals(AbstractBottomUpParser.TOP_NODE) && parent.Label == null)
            {
                if (!prevParent.Type.Equals(parent.Type))
                {
                    //build level
                    if (etype == ParserEventTypeEnum.BUILD)
                    {
                        parseEvents.Add(new Event(parent.Type,
                            buildContextGenerator.GetContext(currentChunks, ci)));
                    }

                    builtNodes.Insert(off++, parent);
                    Parse newParent = new(currentChunks[ci].Text,
                        currentChunks[ci].Span, parent.Type, 1, 0);
                    newParent.Add(currentChunks[ci], rules);
                    newParent.SetPrevPunctuation(currentChunks[ci].PreviousPunctuationSet);
                    newParent.SetNextPunctuation(currentChunks[ci].NextPunctuationSet);
                    currentChunks[ci].Parent = newParent;
                    currentChunks[ci] = newParent;
                    newParent.Label = Parser.BUILT;
                    //see if chunk is complete
                    if (LastChild(chunks[ci], parent))
                    {
                        if (etype == ParserEventTypeEnum.CHECK)
                        {
                            parseEvents.Add(new Event(Parser.COMPLETE,
                                checkContextGenerator.GetContext(currentChunks[ci], currentChunks, ci, false)));
                        }

                        currentChunks[ci].Label = Parser.COMPLETE;
                        parent.Label = Parser.COMPLETE;
                    }
                    else
                    {
                        if (etype == ParserEventTypeEnum.CHECK)
                        {
                            parseEvents.Add(new Event(Parser.INCOMPLETE,
                                checkContextGenerator.GetContext(currentChunks[ci], currentChunks, ci, false)));
                        }

                        currentChunks[ci].Label = Parser.INCOMPLETE;
                        parent.Label = Parser.COMPLETE;
                    }

                    chunks[ci] = parent;
                }

                //TODO: Consider whether we need to set this label or train parses at all.
                parent.Label = Parser.BUILT;
                prevParent = parent;
                parent = parent.Parent!;
            }

            //decide to attach
            if (etype == ParserEventTypeEnum.BUILD)
            {
                parseEvents.Add(new Event(Parser.DONE, buildContextGenerator.GetContext(currentChunks, ci)));
            }

            //attach node
            string? attachType = null;
            /* Node selected for attachment. */
            Parse? attachNode = null;
            int attachNodeIndex = -1;
            if (ci == 0)
            {
                Parse top = new(currentChunks[ci].Text,
                    new Span(0, currentChunks[ci].Text.Length), AbstractBottomUpParser.TOP_NODE, 1, 0);
                top.Insert(currentChunks[ci]);
            }
            else
            {
                /* Right frontier consisting of partially-built nodes based on current state of the parse.*/
                IList<Parse> currentRightFrontier = Parser.GetRightFrontier(currentChunks[0], punctSet);
                if (currentRightFrontier.Count != rightFrontier.Count)
                {
                    // NOpenNLP: upstream prints to stderr and calls System.exit(1). A library
                    // must not terminate the host process, so the mis-alignment is raised as
                    // an exception instead.
                    throw new InvalidOperationException("frontiers mis-aligned: " +
                        currentRightFrontier.Count + " != " + rightFrontier.Count + " " +
                        currentRightFrontier + " " + rightFrontier);
                }

                IDictionary<Parse, int> parents = GetNonAdjoinedParent(chunks[ci]);
                //try daughters first.
                for (int cfi = 0; cfi < currentRightFrontier.Count; cfi++)
                {
                    Parse frontierNode = rightFrontier[cfi];
                    Parse cfn = currentRightFrontier[cfi];
                    if (!Parser.checkComplete || !Parser.COMPLETE.Equals(cfn.Label))
                    {
                        bool hasParent = parents.TryGetValue(frontierNode, out int i);

                        if (attachNode == null && hasParent && i == NonPunctChildCount(cfn))
                        {
                            attachType = Parser.ATTACH_DAUGHTER;
                            attachNodeIndex = cfi;
                            attachNode = cfn;
                            if (etype == ParserEventTypeEnum.ATTACH)
                            {
                                parseEvents.Add(new Event(attachType,
                                    attachContextGenerator.GetContext(currentChunks,
                                        ci, currentRightFrontier, attachNodeIndex)));
                            }
                        }
                    }

                    // Can't attach past first incomplete node.
                    if (Parser.checkComplete && cfn.Label!.Equals(Parser.INCOMPLETE))
                    {
                        break;
                    }
                }

                //try sisters, and generate non-attach events.
                for (int cfi = 0; cfi < currentRightFrontier.Count; cfi++)
                {
                    Parse frontierNode = rightFrontier[cfi];
                    Parse cfn = currentRightFrontier[cfi];
                    if (attachNode == null && frontierNode.Parent != null
                        && parents.ContainsKey(frontierNode.Parent)
                        && frontierNode.Type.Equals(frontierNode.Parent.Type))
                    {
                        attachType = Parser.ATTACH_SISTER;
                        attachNode = cfn;
                        attachNodeIndex = cfi;
                        if (etype == ParserEventTypeEnum.ATTACH)
                        {
                            parseEvents.Add(new Event(Parser.ATTACH_SISTER,
                                attachContextGenerator.GetContext(currentChunks, ci, currentRightFrontier, cfi)));
                        }

                        chunks[ci].Parent!.Label = Parser.BUILT;
                    }
                    else if (cfi == attachNodeIndex)
                    {
                        //skip over previously attached daughter.
                    }
                    else
                    {
                        if (etype == ParserEventTypeEnum.ATTACH)
                        {
                            parseEvents.Add(new Event(Parser.NON_ATTACH,
                                attachContextGenerator.GetContext(currentChunks, ci, currentRightFrontier, cfi)));
                        }
                    }

                    //Can't attach past first incomplete node.
                    if (Parser.checkComplete && cfn.Label!.Equals(Parser.INCOMPLETE))
                    {
                        break;
                    }
                }

                //attach Node
                if (attachNode != null)
                {
                    if (Parser.ATTACH_DAUGHTER.Equals(attachType))
                    {
                        Parse daughter = currentChunks[ci];
                        attachNode.Add(daughter, rules);
                        daughter.Parent = attachNode;
                        if (LastChild(chunks[ci], rightFrontier[attachNodeIndex]))
                        {
                            if (etype == ParserEventTypeEnum.CHECK)
                            {
                                parseEvents.Add(new Event(Parser.COMPLETE,
                                    checkContextGenerator.GetContext(attachNode, currentChunks, ci, true)));
                            }

                            attachNode.Label = Parser.COMPLETE;
                        }
                        else
                        {
                            if (etype == ParserEventTypeEnum.CHECK)
                            {
                                parseEvents.Add(new Event(Parser.INCOMPLETE,
                                    checkContextGenerator.GetContext(attachNode, currentChunks, ci, true)));
                            }
                        }
                    }
                    else if (Parser.ATTACH_SISTER.Equals(attachType))
                    {
                        Parse frontierNode = rightFrontier[attachNodeIndex];
                        rightFrontier[attachNodeIndex] = frontierNode.Parent!;
                        Parse sister = currentChunks[ci];

                        Parse newParent = attachNode.Parent!.Adjoin(sister, rules);

                        newParent.Parent = attachNode.Parent;
                        attachNode.Parent = newParent;
                        sister.Parent = newParent;
                        if (attachNode == currentChunks[0])
                        {
                            currentChunks[0] = newParent;
                        }

                        if (LastChild(chunks[ci], rightFrontier[attachNodeIndex]))
                        {
                            if (etype == ParserEventTypeEnum.CHECK)
                            {
                                parseEvents.Add(new Event(Parser.COMPLETE,
                                    checkContextGenerator.GetContext(newParent, currentChunks, ci, true)));
                            }

                            newParent.Label = Parser.COMPLETE;
                        }
                        else
                        {
                            if (etype == ParserEventTypeEnum.CHECK)
                            {
                                parseEvents.Add(new Event(Parser.INCOMPLETE,
                                    checkContextGenerator.GetContext(newParent, currentChunks, ci, true)));
                            }

                            newParent.Label = Parser.INCOMPLETE;
                        }
                    }

                    //update right frontier
                    for (int ni = 0; ni < attachNodeIndex; ni++)
                    {
                        rightFrontier.RemoveAt(0);
                    }
                }
                else
                {
                    throw new InvalidOperationException("No Attachment: " + chunks[ci]);
                }
            }

            // NOpenNLP: upstream calls rightFrontier.addAll(0, builtNodes), which
            // inserts the whole list at the front in order.
            for (int bi = builtNodes.Count - 1; bi >= 0; bi--)
            {
                rightFrontier.Insert(0, builtNodes[bi]);
            }

            builtNodes.Clear();
        }
    }
}
