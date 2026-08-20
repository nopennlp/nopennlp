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
using System.IO;
using NOpenNLP.Tools.Chunker;
using NOpenNLP.Tools.Ml;
using NOpenNLP.Tools.Ml.Model;
using NOpenNLP.Tools.Postag;
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Parser.Chunking;

/// <summary>
/// Class for a shift reduce style parser based on Adwait Ratnaparkhi's 1998 thesis.
/// </summary>
public class Parser : AbstractBottomUpParser
{
    private readonly IMaxentModel buildModel; // NOpenNLP: made readonly
    private readonly IMaxentModel checkModel; // NOpenNLP: made readonly

    private readonly BuildContextGenerator buildContextGenerator; // NOpenNLP: made readonly
    private readonly CheckContextGenerator checkContextGenerator; // NOpenNLP: made readonly

    private readonly double[] bprobs; // NOpenNLP: made readonly
    private readonly double[] cprobs; // NOpenNLP: made readonly

    private const string TOP_START = START + TOP_NODE;
    private readonly int topStartIndex; // NOpenNLP: made readonly
    private readonly JCG.Dictionary<string, string> startTypeMap; // NOpenNLP: made readonly
    private readonly JCG.Dictionary<string, string> contTypeMap; // NOpenNLP: made readonly

    private readonly int completeIndex; // NOpenNLP: made readonly
    private readonly int incompleteIndex; // NOpenNLP: made readonly

    public Parser(ParserModel model, int beamSize = defaultBeamSize, double advancePercentage = defaultAdvancePercentage)
        : this(model.BuildModel, model.CheckModel,
            new POSTaggerME(model.ParserTaggerModel),
            new ChunkerME(model.ParserChunkerModel),
            model.HeadRules, beamSize, advancePercentage)
    {
    }

    /// <summary>
    /// Creates a new parser using the specified models and head rules using the specified beam
    /// size and advance percentage.
    /// </summary>
    /// <param name="buildModel">The model to assign constituent labels.</param>
    /// <param name="checkModel">The model to determine a constituent is complete.</param>
    /// <param name="tagger">The model to assign pos-tags.</param>
    /// <param name="chunker">The model to assign flat constituent labels.</param>
    /// <param name="headRules">The head rules for head word perculation.</param>
    /// <param name="beamSize">The number of different parses kept during parsing.</param>
    /// <param name="advancePercentage">
    /// The minimal amount of probability mass which advanced outcomes must represent.
    /// Only outcomes which contribute to the top "advancePercentage" will be explored.
    /// </param>
    private Parser(IMaxentModel buildModel, IMaxentModel checkModel, IPOSTagger tagger,
        IChunker chunker, IHeadRules headRules, int beamSize, double advancePercentage)
        : base(tagger, chunker, headRules, beamSize, advancePercentage)
    {
        this.buildModel = buildModel;
        this.checkModel = checkModel;
        bprobs = new double[buildModel.NumOutcomes];
        cprobs = new double[checkModel.NumOutcomes];
        this.buildContextGenerator = new BuildContextGenerator();
        this.checkContextGenerator = new CheckContextGenerator();
        startTypeMap = [];
        contTypeMap = [];
        for (int boi = 0, bon = buildModel.NumOutcomes; boi < bon; boi++)
        {
            string outcome = buildModel.GetOutcome(boi);
            if (outcome.StartsWith(START, StringComparison.Ordinal))
            {
                startTypeMap[outcome] = outcome[START.Length..];
            }
            else if (outcome.StartsWith(CONT, StringComparison.Ordinal))
            {
                contTypeMap[outcome] = outcome[CONT.Length..];
            }
        }

        topStartIndex = buildModel.GetIndex(TOP_START);
        completeIndex = checkModel.GetIndex(COMPLETE);
        incompleteIndex = checkModel.GetIndex(INCOMPLETE);
    }

    protected override void AdvanceTop(Parse p)
    {
        buildModel.Eval(buildContextGenerator.GetContext(p.GetChildren(), 0), bprobs);
        p.AddProb(Math.Log(bprobs[topStartIndex]));
        checkModel.Eval(checkContextGenerator.GetContext(p.GetChildren(), TOP_NODE, 0, 0), cprobs);
        p.AddProb(Math.Log(cprobs[completeIndex]));
        p.Type = TOP_NODE;
    }

    protected override Parse[]? AdvanceParses(Parse p, double probMass)
    {
        double q = 1 - probMass;
        /* The closest previous node which has been labeled as a start node. */
        Parse? lastStartNode = null;
        /* The index of the closest previous node which has been labeled as a start node. */
        int lastStartIndex = -1;
        /* The type of the closest previous node which has been labeled as a start node. */
        string? lastStartType = null;
        /* The index of the node which will be labeled in this iteration of advancing the parse. */
        int advanceNodeIndex;
        /* The node which will be labeled in this iteration of advancing the parse. */
        Parse? advanceNode = null;
        var originalChildren = p.GetChildren();
        var children = CollapsePunctuation(originalChildren, punctSet);
        int numNodes = children.Length;
        if (numNodes == 0)
        {
            return null;
        }

        // determines which node needs to be labeled and prior labels.
        for (advanceNodeIndex = 0; advanceNodeIndex < numNodes; advanceNodeIndex++)
        {
            advanceNode = children[advanceNodeIndex];
            if (advanceNode.Label == null)
            {
                break;
            }
            else if (startTypeMap.TryGetValue(advanceNode.Label, out string? startType))
            {
                lastStartType = startType;
                lastStartNode = advanceNode;
                lastStartIndex = advanceNodeIndex;
            }
        }

        int originalAdvanceIndex = MapParseIndex(advanceNodeIndex, children, originalChildren);
        JCG.List<Parse> newParsesList = new(buildModel.NumOutcomes);
        // call build
        buildModel.Eval(buildContextGenerator.GetContext(children, advanceNodeIndex), bprobs);
        double bprobSum = 0;
        while (bprobSum < probMass)
        {
            // The largest unadvanced labeling.
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
            if (max == topStartIndex)
            {
                // can't have top until complete
                continue;
            }

            if (startTypeMap.TryGetValue(tag, out string? startTypeForTag))
            {
                // update last start
                lastStartIndex = advanceNodeIndex;
                lastStartNode = advanceNode;
                lastStartType = startTypeForTag;
            }
            else if (contTypeMap.TryGetValue(tag, out string? contTypeForTag))
            {
                if (lastStartNode == null || !lastStartType!.Equals(contTypeForTag))
                {
                    continue; // Cont must match previous start or continue
                }
            }

            var newParse1 = (Parse)p.Clone(); // clone parse
            if (createDerivationString)
            {
                newParse1.Derivation!.Append(max).Append('-');
            }

            // replace constituent being labeled to create new derivation
            newParse1.SetChild(originalAdvanceIndex, tag);
            newParse1.AddProb(Math.Log(bprob));
            // check
            checkModel.Eval(checkContextGenerator.GetContext(
                CollapsePunctuation(newParse1.GetChildren(), punctSet), lastStartType!,
                lastStartIndex, advanceNodeIndex), cprobs);
            if (cprobs[completeIndex] > q)
            {
                // make sure a reduce is likely
                var newParse2 = (Parse)newParse1.Clone();
                if (createDerivationString)
                {
                    newParse2.Derivation!.Append(1).Append('.');
                }

                newParse2.AddProb(Math.Log(cprobs[completeIndex]));
                var cons = new Parse[advanceNodeIndex - lastStartIndex + 1];
                bool flat = true;
                // first
                cons[0] = lastStartNode!;
                flat &= cons[0].IsPosTag;
                // last
                cons[advanceNodeIndex - lastStartIndex] = advanceNode!;
                flat &= cons[advanceNodeIndex - lastStartIndex].IsPosTag;
                // middle
                for (int ci = 1; ci < advanceNodeIndex - lastStartIndex; ci++)
                {
                    cons[ci] = children[ci + lastStartIndex];
                    flat &= cons[ci].IsPosTag;
                }

                if (!flat)
                {
                    // flat chunks are done by chunker
                    // check for top node to include end and begining punctuation
                    if (lastStartIndex == 0 && advanceNodeIndex == numNodes - 1)
                    {
                        newParse2.Insert(new Parse(p.Text, p.Span, lastStartType!, cprobs[1],
                            headRules.GetHead(cons, lastStartType!)));
                    }
                    else
                    {
                        newParse2.Insert(new Parse(p.Text,
                            new Span(lastStartNode!.Span.Start, advanceNode!.Span.End),
                            lastStartType!, cprobs[1], headRules.GetHead(cons, lastStartType!)));
                    }

                    newParsesList.Add(newParse2);
                }
            }

            if (cprobs[incompleteIndex] > q)
            {
                // make sure a shift is likely
                if (createDerivationString)
                {
                    newParse1.Derivation!.Append(0).Append('.');
                }

                if (advanceNodeIndex != numNodes - 1)
                {
                    // can't shift last element
                    newParse1.AddProb(Math.Log(cprobs[incompleteIndex]));
                    newParsesList.Add(newParse1);
                }
            }
        }

        return [.. newParsesList];
    }

    public static void MergeReportIntoManifest(IDictionary<string, string> manifest,
        IDictionary<string, string> report, string @namespace)
    {
        foreach (var entry in report)
        {
            manifest[$"{@namespace}.{entry.Key}"] = entry.Value;
        }
    }

    /// <exception cref="IOException">if there is an error during reading</exception>
    public static ParserModel Train(string languageCode, IObjectStream<Parse?> parseSamples,
        IHeadRules rules, TrainingParameters mlParams)
    {
        Console.Error.WriteLine("Building dictionary");

        var mdict = BuildDictionary(parseSamples, rules, mlParams);

        parseSamples.Reset();

        Dictionary<string, string> manifestInfoEntries = [];

        // build
        Console.Error.WriteLine("Training builder");
        IObjectStream<Event?> bes = new ParserEventStream(parseSamples, rules,
            ParserEventTypeEnum.BUILD, mdict);
        IDictionary<string, string> buildReportMap = new JCG.Dictionary<string, string>();
        var buildTrainer = TrainerFactory.GetEventTrainer(mlParams.GetParameters("build"), buildReportMap);
        var buildModel = buildTrainer.Train(bes);
        MergeReportIntoManifest(manifestInfoEntries, buildReportMap, "build");

        parseSamples.Reset();

        // tag
        var posTaggerParams = mlParams.GetParameters("tagger");

        if (!posTaggerParams.GetObjectSettings().ContainsKey(BeamSearch.BEAM_SIZE_PARAMETER))
        {
            mlParams.Put("tagger", BeamSearch.BEAM_SIZE_PARAMETER, 10);
        }

        var posModel = POSTaggerME.Train(languageCode, new PosSampleStream(parseSamples),
            mlParams.GetParameters("tagger"), new POSTaggerFactory());

        parseSamples.Reset();

        // chunk
        var chunkModel = ChunkerME.Train(languageCode,
            new ChunkSampleStream(parseSamples), mlParams.GetParameters("chunker"),
            new ParserChunkerFactory());

        parseSamples.Reset();

        // check
        Console.Error.WriteLine("Training checker");
        IObjectStream<Event?> kes = new ParserEventStream(parseSamples, rules, ParserEventTypeEnum.CHECK);
        IDictionary<string, string> checkReportMap = new JCG.Dictionary<string, string>();
        var checkTrainer = TrainerFactory.GetEventTrainer(mlParams.GetParameters("check"), checkReportMap);
        var checkModel = checkTrainer.Train(kes);
        MergeReportIntoManifest(manifestInfoEntries, checkReportMap, "check");

        return new ParserModel(languageCode, buildModel, checkModel,
            posModel, chunkModel, rules,
            ParserType.CHUNKING, manifestInfoEntries);
    }
}
