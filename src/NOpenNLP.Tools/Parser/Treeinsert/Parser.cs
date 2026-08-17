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
using NOpenNLP.Tools.Chunker;
using NOpenNLP.Tools.Ml.Model;
using NOpenNLP.Tools.Postag;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Parser.Treeinsert;

/// <summary>
/// Built/attach parser. Nodes are built when their left-most
/// child is encountered. Subsequent children are attached as
/// daughters. Attachment is based on node in the right-frontier
/// of the tree. After each attachment or building, nodes are
/// assesed as either complete or incomplete.  Complete nodes
/// are no longer elligable for daughter attachment.
/// Complex modifiers which produce additional node
/// levels of the same type are attached with sister-adjunction.
/// Attachment can not take place higher in the right-frontier
/// than an incomplete node.
/// </summary>
public class Parser : AbstractBottomUpParser
{
    /// <summary>Outcome used when a constituent needs an no additional parent node/building.</summary>
    public const string DONE = "d";

    /// <summary>Outcome used when a node should be attached as a sister to another node.</summary>
    public const string ATTACH_SISTER = "s";

    /// <summary>Outcome used when a node should be attached as a daughter to another node.</summary>
    // NOpenNLP: upstream deliberately gives ATTACH_DAUGHTER the same value as DONE.
    public const string ATTACH_DAUGHTER = "d";

    /// <summary>Outcome used when a node should not be attached to another node.</summary>
    public const string NON_ATTACH = "n";

    /// <summary>Label used to distinguish build nodes from non-built nodes.</summary>
    public const string BUILT = "built";

    private readonly IMaxentModel buildModel; // NOpenNLP: made readonly
    private readonly IMaxentModel attachModel; // NOpenNLP: made readonly
    private readonly IMaxentModel checkModel; // NOpenNLP: made readonly

    internal static bool checkComplete = false;

    private readonly BuildContextGenerator buildContextGenerator; // NOpenNLP: made readonly
    private readonly AttachContextGenerator attachContextGenerator; // NOpenNLP: made readonly
    private readonly CheckContextGenerator checkContextGenerator; // NOpenNLP: made readonly

    private readonly double[] bprobs; // NOpenNLP: made readonly
    private readonly double[] aprobs; // NOpenNLP: made readonly
    private double[] cprobs;

    private readonly int doneIndex; // NOpenNLP: made readonly
    private readonly int sisterAttachIndex; // NOpenNLP: made readonly
    private readonly int daughterAttachIndex; // NOpenNLP: made readonly
    private readonly int nonAttachIndex; // NOpenNLP: made readonly
    private readonly int completeIndex; // NOpenNLP: made readonly

    private readonly int[] attachments; // NOpenNLP: made readonly

    public Parser(ParserModel model, int beamSize, double advancePercentage)
        : this(model.BuildModel, model.AttachModel, model.CheckModel,
            new POSTaggerME(model.ParserTaggerModel),
            new ChunkerME(model.ParserChunkerModel),
            model.HeadRules, beamSize, advancePercentage)
    {
    }

    public Parser(ParserModel model)
        : this(model, defaultBeamSize, defaultAdvancePercentage)
    {
    }

    private Parser(IMaxentModel buildModel, IMaxentModel attachModel, IMaxentModel checkModel,
        IPOSTagger tagger, IChunker chunker, IHeadRules headRules, int beamSize,
        double advancePercentage)
        : base(tagger, chunker, headRules, beamSize, advancePercentage)
    {
        this.buildModel = buildModel;
        this.attachModel = attachModel;
        this.checkModel = checkModel;

        this.buildContextGenerator = new BuildContextGenerator();
        this.attachContextGenerator = new AttachContextGenerator(punctSet);
        this.checkContextGenerator = new CheckContextGenerator(punctSet);

        this.bprobs = new double[buildModel.NumOutcomes];
        this.aprobs = new double[attachModel.NumOutcomes];
        this.cprobs = new double[checkModel.NumOutcomes];

        this.doneIndex = buildModel.GetIndex(DONE);
        this.sisterAttachIndex = attachModel.GetIndex(ATTACH_SISTER);
        this.daughterAttachIndex = attachModel.GetIndex(ATTACH_DAUGHTER);
        this.nonAttachIndex = attachModel.GetIndex(NON_ATTACH);
        attachments = [daughterAttachIndex, sisterAttachIndex];
        this.completeIndex = checkModel.GetIndex(COMPLETE);
    }

    /// <summary>
    /// Returns the right frontier of the specified parse tree with nodes ordered from deepest
    /// to shallowest.
    /// </summary>
    /// <param name="root">The root of the parse tree.</param>
    /// <param name="punctSet">The set of punctuation tags.</param>
    /// <returns>The right frontier of the specified parse tree.</returns>
    public static IList<Parse> GetRightFrontier(Parse root, ISet<string> punctSet)
    {
        JCG.List<Parse> rf = [];
        Parse top;
        if (TOP_NODE.Equals(root.Type) || INC_NODE.Equals(root.Type))
        {
            top = CollapsePunctuation(root.GetChildren(), punctSet)[0];
        }
        else
        {
            top = root;
        }

        while (!top.IsPosTag)
        {
            rf.Insert(0, top);
            Parse[] kids = top.GetChildren();
            top = kids[kids.Length - 1];
        }

        return new JCG.List<Parse>(rf);
    }

    private void SetBuilt(Parse p)
    {
        string? l = p.Label;
        if (l == null)
        {
            p.Label = BUILT;
        }
        else
        {
            if (IsComplete(p))
            {
                p.Label = BUILT + "." + COMPLETE;
            }
            else
            {
                p.Label = BUILT + "." + INCOMPLETE;
            }
        }
    }

    private void SetComplete(Parse p)
    {
        if (!IsBuilt(p))
        {
            p.Label = COMPLETE;
        }
        else
        {
            p.Label = BUILT + "." + COMPLETE;
        }
    }

    private void SetIncomplete(Parse p)
    {
        if (!IsBuilt(p))
        {
            p.Label = INCOMPLETE;
        }
        else
        {
            p.Label = BUILT + "." + INCOMPLETE;
        }
    }

    private bool IsBuilt(Parse p)
    {
        string? l = p.Label;
        return l != null && l.StartsWith(BUILT, StringComparison.Ordinal);
    }

    private bool IsComplete(Parse p)
    {
        string? l = p.Label;
        return l != null && l.EndsWith(COMPLETE, StringComparison.Ordinal);
    }

    protected override Parse[] AdvanceChunks(Parse p, double minChunkScore)
    {
        Parse[] parses = base.AdvanceChunks(p, minChunkScore);
        foreach (Parse parse in parses)
        {
            Parse[] chunks = parse.GetChildren();
            foreach (Parse chunk in chunks)
            {
                SetComplete(chunk);
            }
        }

        return parses;
    }

    protected override Parse[]? AdvanceParses(Parse p, double probMass)
    {
        double q = 1 - probMass;
        /* The index of the node which will be labeled in this iteration of advancing the parse. */
        int advanceNodeIndex;
        /* The node which will be labeled in this iteration of advancing the parse. */
        Parse? advanceNode = null;
        Parse[] originalChildren = p.GetChildren();
        Parse[] children = CollapsePunctuation(originalChildren, punctSet);
        int numNodes = children.Length;
        if (numNodes == 0)
        {
            return null;
        }
        else if (numNodes == 1)
        {
            // put sentence initial and final punct in top node
            if (children[0].IsPosTag)
            {
                return null;
            }
            else
            {
                p.ExpandTopNode(children[0]);
                return [p];
            }
        }

        // determines which node needs to adanced.
        for (advanceNodeIndex = 0; advanceNodeIndex < numNodes; advanceNodeIndex++)
        {
            advanceNode = children[advanceNodeIndex];
            if (!IsBuilt(advanceNode))
            {
                break;
            }
        }

        int originalZeroIndex = MapParseIndex(0, children, originalChildren);
        int originalAdvanceIndex = MapParseIndex(advanceNodeIndex, children, originalChildren);
        JCG.List<Parse> newParsesList = [];
        // call build model
        buildModel.Eval(buildContextGenerator.GetContext(children, advanceNodeIndex), bprobs);
        double doneProb = bprobs[doneIndex];
        if (debugOn)
        {
            Console.WriteLine("adi=" + advanceNodeIndex + " " + advanceNode!.Type + "."
                + advanceNode.Label + " " + advanceNode + " choose build=" + (1 - doneProb)
                + " attach=" + doneProb);
        }

        if (1 - doneProb > q)
        {
            double bprobSum = 0;
            while (bprobSum < probMass)
            {
                /* The largest unadvanced labeling. */
                int max = 0;
                for (int pi = 1; pi < bprobs.Length; pi++)
                {
                    // for each build outcome
                    if (bprobs[pi] > bprobs[max])
                    {
                        max = pi;
                    }
                }

                if (bprobs[max] == 0)
                {
                    break;
                }

                double bprob = bprobs[max];
                bprobs[max] = 0; // zero out so new max can be found
                bprobSum += bprob;
                string tag = buildModel.GetOutcome(max);
                if (!tag.Equals(DONE))
                {
                    Parse newParse1 = (Parse)p.Clone();
                    Parse newNode = new(p.Text, advanceNode!.Span, tag, bprob, advanceNode.Head);
                    newParse1.Insert(newNode);
                    newParse1.AddProb(Math.Log(bprob));
                    newParsesList.Add(newParse1);
                    if (checkComplete)
                    {
                        cprobs = checkModel.Eval(checkContextGenerator.GetContext(newNode, children,
                            advanceNodeIndex, false));
                        if (debugOn)
                        {
                            Console.WriteLine("building " + tag + " " + bprob + " c="
                                + cprobs[completeIndex]);
                        }

                        if (cprobs[completeIndex] > probMass)
                        {
                            // just complete advances
                            SetComplete(newNode);
                            newParse1.AddProb(Math.Log(cprobs[completeIndex]));
                            if (debugOn)
                            {
                                Console.WriteLine("Only advancing complete node");
                            }
                        }
                        else if (1 - cprobs[completeIndex] > probMass)
                        {
                            // just incomplete advances
                            SetIncomplete(newNode);
                            newParse1.AddProb(Math.Log(1 - cprobs[completeIndex]));
                            if (debugOn)
                            {
                                Console.WriteLine("Only advancing incomplete node");
                            }
                        }
                        else
                        {
                            // both complete and incomplete advance
                            if (debugOn)
                            {
                                Console.WriteLine("Advancing both complete and incomplete nodes");
                            }

                            SetComplete(newNode);
                            newParse1.AddProb(Math.Log(cprobs[completeIndex]));

                            Parse newParse2 = (Parse)p.Clone();
                            Parse newNode2 = new(p.Text, advanceNode.Span, tag, bprob,
                                advanceNode.Head);
                            newParse2.Insert(newNode2);
                            newParse2.AddProb(Math.Log(bprob));
                            newParsesList.Add(newParse2);
                            newParse2.AddProb(Math.Log(1 - cprobs[completeIndex]));
                            SetIncomplete(newNode2); // set incomplete for non-clone
                        }
                    }
                    else
                    {
                        if (debugOn)
                        {
                            Console.WriteLine("building " + tag + " " + bprob);
                        }
                    }
                }
            }
        }

        // advance attaches
        if (doneProb > q)
        {
            Parse newParse1 = (Parse)p.Clone(); // clone parse
            // mark nodes as built
            if (checkComplete)
            {
                if (IsComplete(advanceNode!))
                {
                    // replace constituent being labeled to create new derivation
                    newParse1.SetChild(originalAdvanceIndex, BUILT + "." + COMPLETE);
                }
                else
                {
                    // replace constituent being labeled to create new derivation
                    newParse1.SetChild(originalAdvanceIndex, BUILT + "." + INCOMPLETE);
                }
            }
            else
            {
                // replace constituent being labeled to create new derivation
                newParse1.SetChild(originalAdvanceIndex, BUILT);
            }

            newParse1.AddProb(Math.Log(doneProb));
            if (advanceNodeIndex == 0)
            {
                // no attach if first node.
                newParsesList.Add(newParse1);
            }
            else
            {
                IList<Parse> rf = GetRightFrontier(p, punctSet);
                for (int fi = 0, fs = rf.Count; fi < fs; fi++)
                {
                    Parse fn = rf[fi];
                    attachModel.Eval(
                        attachContextGenerator.GetContext(children, advanceNodeIndex, rf, fi),
                        aprobs);
                    if (debugOn)
                    {
                        Console.WriteLine("Frontier node(" + fi + "): " + fn.Type + "." + fn.Label
                            + " " + fn + " <- " + advanceNode!.Type + " " + advanceNode + " d="
                            + aprobs[daughterAttachIndex] + " s=" + aprobs[sisterAttachIndex] + " ");
                    }

                    foreach (int attachment in attachments)
                    {
                        double prob = aprobs[attachment];
                        // should we try an attach if p > threshold and
                        // if !checkComplete then prevent daughter attaching to chunk
                        // if checkComplete then prevent daughter attacing to complete node or
                        //    sister attaching to an incomplete node
                        if (prob > q && (
                                (!checkComplete && (attachment != daughterAttachIndex || !IsComplete(fn)))
                                    ||
                                    (checkComplete && ((attachment == daughterAttachIndex && !IsComplete(fn))
                                        || (attachment == sisterAttachIndex && IsComplete(fn))))))
                        {
                            Parse newParse2 = newParse1.CloneRoot(fn, originalZeroIndex);
                            Parse[] newKids = CollapsePunctuation(newParse2.GetChildren(), punctSet);
                            // remove node from top level since were going to attach it (including punct)
                            for (int ri = originalZeroIndex + 1; ri <= originalAdvanceIndex; ri++)
                            {
                                newParse2.Remove(originalZeroIndex + 1);
                            }

                            IList<Parse> crf = GetRightFrontier(newParse2, punctSet);
                            Parse updatedNode;
                            if (attachment == daughterAttachIndex)
                            {
                                // attach daughter
                                updatedNode = crf[fi];
                                updatedNode.Add(advanceNode!, headRules);
                            }
                            else
                            {
                                // attach sister
                                Parse psite;
                                if (fi + 1 < crf.Count)
                                {
                                    psite = crf[fi + 1];
                                    updatedNode = psite.Adjoin(advanceNode!, headRules);
                                }
                                else
                                {
                                    psite = newParse2;
                                    updatedNode = psite.AdjoinRoot(advanceNode!, headRules,
                                        originalZeroIndex);
                                    newKids[0] = updatedNode;
                                }
                            }

                            // update spans affected by attachment
                            for (int ni = fi + 1; ni < crf.Count; ni++)
                            {
                                Parse node = crf[ni];
                                node.UpdateSpan();
                            }

                            newParse2.AddProb(Math.Log(prob));
                            newParsesList.Add(newParse2);
                            if (checkComplete)
                            {
                                cprobs = checkModel.Eval(
                                    checkContextGenerator.GetContext(updatedNode, newKids,
                                        advanceNodeIndex, true));
                                if (cprobs[completeIndex] > probMass)
                                {
                                    SetComplete(updatedNode);
                                    newParse2.AddProb(Math.Log(cprobs[completeIndex]));
                                    if (debugOn)
                                    {
                                        Console.WriteLine("Only advancing complete node");
                                    }
                                }
                                else if (1 - cprobs[completeIndex] > probMass)
                                {
                                    SetIncomplete(updatedNode);
                                    newParse2.AddProb(Math.Log(1 - cprobs[completeIndex]));
                                    if (debugOn)
                                    {
                                        Console.WriteLine("Only advancing incomplete node");
                                    }
                                }
                                else
                                {
                                    SetComplete(updatedNode);
                                    Parse newParse3 = newParse2.CloneRoot(updatedNode, originalZeroIndex);
                                    newParse3.AddProb(Math.Log(cprobs[completeIndex]));
                                    newParsesList.Add(newParse3);
                                    SetIncomplete(updatedNode);
                                    newParse2.AddProb(Math.Log(1 - cprobs[completeIndex]));
                                    if (debugOn)
                                    {
                                        Console.WriteLine("Advancing both complete and incomplete nodes; c="
                                            + cprobs[completeIndex]);
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (debugOn)
                            {
                                Console.WriteLine("Skipping " + fn.Type + "." + fn.Label + " "
                                    + fn + " daughter=" + (attachment == daughterAttachIndex)
                                    + " complete=" + IsComplete(fn) + " prob=" + prob);
                            }
                        }
                    }

                    if (checkComplete && !IsComplete(fn))
                    {
                        if (debugOn)
                        {
                            Console.WriteLine("Stopping at incomplete node(" + fi + "): "
                                + fn.Type + "." + fn.Label + " " + fn);
                        }

                        break;
                    }
                }
            }
        }

        return [.. newParsesList];
    }

    protected override void AdvanceTop(Parse p) => p.Type = TOP_NODE;

    // NOpenNLP: the train overloads are training-only -- they consume ObjectStream<Parse>
    // training samples and TrainingParameters, and depend on the parser event streams which
    // are outside the inference-only port.
    // public static ParserModel Train(string languageCode, IObjectStream<Parse> parseSamples,
    //     IHeadRules rules, TrainingParameters mlParams)
}
