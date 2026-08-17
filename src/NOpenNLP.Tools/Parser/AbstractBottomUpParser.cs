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
using System.Text;
using NOpenNLP.Tools.Chunker;
using NOpenNLP.Tools.Postag;
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Parser;

/// <summary>
/// Abstract class which contains code to tag and chunk parses for bottom up parsing and
/// leaves implementation of advancing parses and completing parses to extend class.
/// <para/>
/// <b>Note:</b> The nodes within the returned parses are shared with other parses
/// and therefore their parent node references will not be consistent with their child
/// node reference. <see cref="SetParents"/> can be used to make the parents consistent
/// with a particular parse, but subsequent calls to <c>SetParents</c> can invalidate
/// the results of earlier calls.
/// </summary>
public abstract class AbstractBottomUpParser : IParser
{
    /// <summary>
    /// The maximum number of parses advanced from all preceding
    /// parses at each derivation step.
    /// </summary>
    protected int M;

    /// <summary>
    /// The maximum number of parses to advance from a single preceding parse.
    /// </summary>
    protected int K;

    /// <summary>
    /// The minimum total probability mass of advanced outcomes.
    /// </summary>
    protected double Q;

    /// <summary>
    /// The default beam size used if no beam size is given.
    /// </summary>
    public const int defaultBeamSize = 20;

    /// <summary>
    /// The default amount of probability mass required of advanced outcomes.
    /// </summary>
    public const double defaultAdvancePercentage = 0.95;

    /// <summary>
    /// Completed parses.
    /// </summary>
    // NOpenNLP: upstream uses a TreeSet, which both orders by Parse.compareTo
    // (descending probability) and drops entries comparing equal. J2N's SortedSet
    // preserves both behaviors, which the beam search depends on.
    private readonly JCG.SortedSet<Parse> completeParses; // NOpenNLP: made readonly

    /// <summary>
    /// Incomplete parses which will be advanced.
    /// </summary>
    private JCG.SortedSet<Parse> odh;

    /// <summary>
    /// Incomplete parses which have been advanced.
    /// </summary>
    private JCG.SortedSet<Parse> ndh;

    /// <summary>
    /// The head rules for the parser.
    /// </summary>
    protected IHeadRules headRules;

    /// <summary>
    /// The set strings which are considered punctuation for the parser.
    /// Punctuation is not attached, but floats to the top of the parse as attachment
    /// decisions are made about its non-punctuation sister nodes.
    /// </summary>
    protected ISet<string> punctSet;

    /// <summary>
    /// The label for the top node.
    /// </summary>
    public const string TOP_NODE = "TOP";

    /// <summary>
    /// The label for the top if an incomplete node.
    /// </summary>
    public const string INC_NODE = "INC";

    /// <summary>
    /// The label for a token node.
    /// </summary>
    public const string TOK_NODE = "TK";

    /// <summary>
    /// The integer 0.
    /// </summary>
    public const int ZERO = 0;

    /// <summary>
    /// Prefix for outcomes starting a constituent.
    /// </summary>
    public const string START = "S-";

    /// <summary>
    /// Prefix for outcomes continuing a constituent.
    /// </summary>
    public const string CONT = "C-";

    /// <summary>
    /// Outcome for token which is not contained in a basal constituent.
    /// </summary>
    public const string OTHER = "O";

    /// <summary>
    /// Outcome used when a constituent is complete.
    /// </summary>
    public const string COMPLETE = "c";

    /// <summary>
    /// Outcome used when a constituent is incomplete.
    /// </summary>
    public const string INCOMPLETE = "i";

    /// <summary>
    /// The pos-tagger that the parser uses.
    /// </summary>
    protected IPOSTagger tagger;

    /// <summary>
    /// The chunker that the parser uses to chunk non-recursive structures.
    /// </summary>
    protected IChunker chunker;

    /// <summary>
    /// Specifies whether failed parses should be reported to standard error.
    /// </summary>
    protected bool reportFailedParse;

    /// <summary>
    /// Specifies whether a derivation string should be created during parsing.
    /// This is useful for debugging.
    /// </summary>
    protected bool createDerivationString = false;

    /// <summary>
    /// Turns debug print on or off.
    /// </summary>
    protected bool debugOn = false;

    protected AbstractBottomUpParser(IPOSTagger tagger, IChunker chunker, IHeadRules headRules,
        int beamSize, double advancePercentage)
    {
        this.tagger = tagger;
        this.chunker = chunker;
        this.M = beamSize;
        this.K = beamSize;
        this.Q = advancePercentage;
        reportFailedParse = true;
        this.headRules = headRules;
        this.punctSet = headRules.PunctuationTags;
        odh = [];
        ndh = [];
        completeParses = [];
    }

    /// <summary>
    /// Specifies whether the parser should report when it was unable to find a parse for
    /// a particular sentence.
    /// </summary>
    /// <param name="errorReporting">If true then un-parsed sentences are reported, false otherwise.</param>
    public void SetErrorReporting(bool errorReporting) => this.reportFailedParse = errorReporting;

    /// <summary>
    /// Assigns parent references for the specified parse so that they
    /// are consistent with the children references.
    /// </summary>
    /// <param name="p">The parse whose parent references need to be assigned.</param>
    public static void SetParents(Parse p)
    {
        Parse[] children = p.GetChildren();
        foreach (Parse child in children)
        {
            child.Parent = p;
            SetParents(child);
        }
    }

    /// <summary>
    /// Removes the punctuation from the specified set of chunks, adds it to the parses
    /// adjacent to the punctuation is specified, and returns a new array of parses with
    /// the punctuation removed.
    /// </summary>
    /// <param name="chunks">A set of parses.</param>
    /// <param name="punctSet">The set of punctuation which is to be removed.</param>
    /// <returns>An array of parses which is a subset of chunks with punctuation removed.</returns>
    public static Parse[] CollapsePunctuation(Parse[] chunks, ISet<string> punctSet)
    {
        JCG.List<Parse> collapsedParses = new(chunks.Length);
        int lastNonPunct = -1;
        int nextNonPunct;
        for (int ci = 0, cn = chunks.Length; ci < cn; ci++)
        {
            if (punctSet.Contains(chunks[ci].Type))
            {
                if (lastNonPunct >= 0)
                {
                    chunks[lastNonPunct].AddNextPunctuation(chunks[ci]);
                }

                for (nextNonPunct = ci + 1; nextNonPunct < cn; nextNonPunct++)
                {
                    if (!punctSet.Contains(chunks[nextNonPunct].Type))
                    {
                        break;
                    }
                }

                if (nextNonPunct < cn)
                {
                    chunks[nextNonPunct].AddPreviousPunctuation(chunks[ci]);
                }
            }
            else
            {
                collapsedParses.Add(chunks[ci]);
                lastNonPunct = ci;
            }
        }

        if (collapsedParses.Count == chunks.Length)
        {
            return chunks;
        }

        return [.. collapsedParses];
    }

    /// <summary>
    /// Advances the specified parse and returns the an array advanced parses whose
    /// probability accounts for more than the specified amount of probability mass.
    /// </summary>
    /// <param name="p">The parse to advance.</param>
    /// <param name="probMass">
    /// The amount of probability mass that should be accounted for by the advanced parses.
    /// </param>
    protected abstract Parse[]? AdvanceParses(Parse p, double probMass);

    /// <summary>
    /// Adds the "TOP" node to the specified parse.
    /// </summary>
    /// <param name="p">The complete parse.</param>
    protected abstract void AdvanceTop(Parse p);

    public Parse[] Parse(Parse tokens, int numParses)
    {
        if (createDerivationString)
        {
            tokens.Derivation = new StringBuilder(100);
        }

        odh.Clear();
        ndh.Clear();
        completeParses.Clear();
        int derivationStage = 0; // derivation length
        int maxDerivationLength = 2 * tokens.ChildCount + 3;
        odh.Add(tokens);
        Parse? guess = null;
        double minComplete = 2;
        double bestComplete = -100000; // approximating -infinity/0 in ln domain
        while (odh.Count > 0 && (completeParses.Count < M || odh.Min!.Prob < minComplete)
            && derivationStage < maxDerivationLength)
        {
            ndh = [];

            int derivationRank = 0;
            // NOpenNLP: upstream iterates the TreeSet directly while bounded by K. J2N's
            // SortedSet enumerates in the same sorted order, so this preserves the
            // upstream traversal.
            foreach (Parse tp in odh)
            {
                if (derivationRank >= K)
                {
                    break;
                }

                if (guess == null && derivationStage == 2)
                {
                    guess = tp;
                }

                if (debugOn)
                {
                    Console.Write(derivationStage + " " + derivationRank + " " + tp.Prob);
                    tp.Show();
                    Console.WriteLine();
                }

                Parse[]? nd;
                if (0 == derivationStage)
                {
                    nd = AdvanceTags(tp);
                }
                else if (1 == derivationStage)
                {
                    if (ndh.Count < K)
                    {
                        nd = AdvanceChunks(tp, bestComplete);
                    }
                    else
                    {
                        nd = AdvanceChunks(tp, ndh.Max!.Prob);
                    }
                }
                else
                {
                    // i > 1
                    nd = AdvanceParses(tp, Q);
                }

                if (nd != null)
                {
                    foreach (Parse parse in nd)
                    {
                        if (parse.Complete())
                        {
                            AdvanceTop(parse);
                            if (parse.Prob > bestComplete)
                            {
                                bestComplete = parse.Prob;
                            }

                            if (parse.Prob < minComplete)
                            {
                                minComplete = parse.Prob;
                            }

                            completeParses.Add(parse);
                        }
                        else
                        {
                            ndh.Add(parse);
                        }
                    }
                }
                else
                {
                    AdvanceTop(tp);
                    completeParses.Add(tp);
                }

                derivationRank++;
            }

            derivationStage++;
            odh = ndh;
        }

        if (completeParses.Count == 0)
        {
            return [guess!];
        }
        else if (numParses == 1)
        {
            return [completeParses.Min!];
        }
        else
        {
            JCG.List<Parse> topParses = new(numParses);
            while (completeParses.Count > 0 && topParses.Count < numParses)
            {
                Parse tp = completeParses.Min!;
                completeParses.Remove(tp);
                topParses.Add(tp);
            }

            return [.. topParses];
        }
    }

    public Parse Parse(Parse tokens)
    {
        if (tokens.ChildCount > 0)
        {
            Parse p = Parse(tokens, 1)[0];
            SetParents(p);
            return p;
        }
        else
        {
            return tokens;
        }
    }

    /// <summary>
    /// Returns the top chunk sequences for the specified parse.
    /// </summary>
    /// <param name="p">A pos-tag assigned parse.</param>
    /// <param name="minChunkScore">A minimum score below which chunks should not be advanced.</param>
    /// <returns>The top chunk assignments to the specified parse.</returns>
    protected virtual Parse[] AdvanceChunks(Parse p, double minChunkScore)
    {
        // chunk
        Parse[] children = p.GetChildren();
        string[] words = new string[children.Length];
        string[] ptags = new string[words.Length];
        double[] probs = new double[words.Length];

        for (int i = 0, il = children.Length; i < il; i++)
        {
            Parse sp = children[i];
            words[i] = sp.Head.CoveredText;
            ptags[i] = sp.Type;
        }

        Sequence[] cs = chunker.TopKSequences(words, ptags, minChunkScore - p.Prob);
        Parse[] newParses = new Parse[cs.Length];
        for (int si = 0, sl = cs.Length; si < sl; si++)
        {
            newParses[si] = (Parse)p.Clone(); // copies top level
            if (createDerivationString)
            {
                newParses[si].Derivation!.Append(si).Append('.');
            }

            string[] tags = [.. cs[si].Outcomes];
            cs[si].GetProbs(probs);
            int start = -1;
            int end = 0;
            string? type = null;
            for (int j = 0; j <= tags.Length; j++)
            {
                if (j != tags.Length)
                {
                    newParses[si].AddProb(Math.Log(probs[j]));
                }

                // if continue just update end chunking tag don't use contTypeMap
                if (j != tags.Length && tags[j].StartsWith(CONT, StringComparison.Ordinal))
                {
                    end = j;
                }
                else
                {
                    // make previous constituent if it exists
                    if (type != null)
                    {
                        Parse p1 = p.GetChildren()[start];
                        Parse p2 = p.GetChildren()[end];
                        Parse[] cons = new Parse[end - start + 1];
                        cons[0] = p1;
                        if (end - start != 0)
                        {
                            cons[end - start] = p2;
                            for (int ci = 1; ci < end - start; ci++)
                            {
                                cons[ci] = p.GetChildren()[ci + start];
                            }
                        }

                        Parse chunk = new(p1.Text,
                            new Span(p1.Span.Start, p2.Span.End), type, 1,
                            headRules.GetHead(cons, type));
                        chunk.IsChunk = true;
                        newParses[si].Insert(chunk);
                    }

                    if (j != tags.Length)
                    {
                        // update for new constituent
                        if (tags[j].StartsWith(START, StringComparison.Ordinal))
                        {
                            // don't use startTypeMap these are chunk tags
                            type = tags[j].Substring(START.Length);
                            start = j;
                            end = j;
                        }
                        else
                        {
                            // other
                            type = null;
                        }
                    }
                }
            }
        }

        return newParses;
    }

    /// <summary>
    /// Advances the parse by assigning it POS tags and returns multiple tag sequences.
    /// </summary>
    /// <param name="p">The parse to be tagged.</param>
    /// <returns>Parses with different POS-tag sequence assignments.</returns>
    protected Parse[] AdvanceTags(Parse p)
    {
        Parse[] children = p.GetChildren();
        string[] words = new string[children.Length];
        double[] probs = new double[words.Length];
        for (int i = 0, il = children.Length; i < il; i++)
        {
            words[i] = children[i].CoveredText;
        }

        Sequence[] ts = tagger.TopKSequences(words);
        Parse[] newParses = new Parse[ts.Length];
        for (int i = 0; i < ts.Length; i++)
        {
            string[] tags = [.. ts[i].Outcomes];
            ts[i].GetProbs(probs);
            newParses[i] = (Parse)p.Clone(); // copies top level
            if (createDerivationString)
            {
                newParses[i].Derivation!.Append(i).Append('.');
            }

            for (int j = 0; j < words.Length; j++)
            {
                Parse word = children[j];
                double prob = probs[j];
                newParses[i].Insert(new Parse(word.Text, word.Span, tags[j], prob, j));
                newParses[i].AddProb(Math.Log(prob));
            }
        }

        return newParses;
    }

    /// <summary>
    /// Determines the mapping between the specified index into the specified parses without
    /// punctuation to the corresponding index into the specified parses.
    /// </summary>
    /// <param name="index">An index into the parses without punctuation.</param>
    /// <param name="nonPunctParses">The parses without punctuation.</param>
    /// <param name="parses">The parses wit punctuation.</param>
    /// <returns>
    /// An index into the specified parses which corresponds to the same node the specified index
    /// into the parses with punctuation.
    /// </returns>
    protected int MapParseIndex(int index, Parse[] nonPunctParses, Parse[] parses)
    {
        int parseIndex = index;
        while (parses[parseIndex] != nonPunctParses[index])
        {
            parseIndex++;
        }

        return parseIndex;
    }

    private static bool LastChild(Parse child, Parse? parent, ISet<string> punctSet)
    {
        if (parent == null)
        {
            return false;
        }

        Parse[] kids = CollapsePunctuation(parent.GetChildren(), punctSet);
        return kids[kids.Length - 1] == child;
    }

    // NOpenNLP: buildDictionary is training-only -- it consumes an ObjectStream<Parse> of
    // training samples and TrainingParameters, and depends on the parser event streams
    // which are outside the inference-only port.
    // public static Dictionary BuildDictionary(IObjectStream<Parse> data, IHeadRules rules,
    //     TrainingParameters parameters)
}
