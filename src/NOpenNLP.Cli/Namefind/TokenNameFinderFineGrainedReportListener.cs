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
using NOpenNLP.Tools.Namefind;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Cmdline.Namefind;

/// <summary>
/// Generates a detailed report for the NameFinder.
/// <para/>
/// It is possible to use it from an API and access the statistics using the
/// provided getters.
/// </summary>
public class TokenNameFinderFineGrainedReportListener : FineGrainedReportListener,
    ITokenNameFinderEvaluationMonitor
{
    private readonly ISequenceCodec<string> sequenceCodec; // NOpenNLP: made readonly

    /// <summary>
    /// Creates a listener that will print to <see cref="Console.Error"/>.
    /// </summary>
    public TokenNameFinderFineGrainedReportListener(ISequenceCodec<string> seqCodec)
        : this(seqCodec, Console.Error)
    {
    }

    /// <summary>
    /// Creates a listener that prints to a given <see cref="TextWriter"/>.
    /// </summary>
    public TokenNameFinderFineGrainedReportListener(ISequenceCodec<string> seqCodec,
        TextWriter outputStream)
        : base(outputStream)
    {
        this.sequenceCodec = seqCodec;
    }

    /// <summary>
    /// Creates a listener that prints to a given <see cref="Stream"/>.
    /// </summary>
    public TokenNameFinderFineGrainedReportListener(ISequenceCodec<string> seqCodec,
        Stream outputStream)
        : base(outputStream)
    {
        this.sequenceCodec = seqCodec;
    }

    // methods inherited from EvaluationMonitor

    /// <inheritdoc/>
    public void Misclassified(NameSample reference, NameSample prediction) =>
        StatsAdd(reference, prediction);

    /// <inheritdoc/>
    public void CorrectlyClassified(NameSample reference, NameSample prediction) =>
        StatsAdd(reference, prediction);

    private void StatsAdd(NameSample reference, NameSample prediction)
    {
        string[] refTags = sequenceCodec.Encode(reference.Names, reference.Sentence.Length);
        string[] predTags = sequenceCodec.Encode(prediction.Names, prediction.Sentence.Length);

        // we don' want it to compute token frequency, so we pass an array of empty strings instead
        // of tokens
        // NOpenNLP: upstream passes `new String[length]`, whose elements are all null;
        // the stats keep them as dictionary keys, and a null key is not allowed here, so
        // the array is filled with the empty string -- the "array of empty strings" the
        // comment above describes.
        string[] emptyTokens = new string[reference.Sentence.Length];
        for (int i = 0; i < emptyTokens.Length; i++)
        {
            emptyTokens[i] = string.Empty;
        }

        GetStats().Add(emptyTokens, refTags, predTags);
    }

    /// <inheritdoc/>
    public override IComparer<string> GetMatrixLabelComparator(
        IDictionary<string, ConfusionMatrixLine> confusionMatrix) =>
        new GroupedMatrixLabelComparator(confusionMatrix);

    /// <inheritdoc/>
    public override IComparer<string> GetLabelComparator(IDictionary<string, Counter> map) =>
        new GroupedLabelComparator(map);

    /// <inheritdoc/>
    public override void WriteReport()
    {
        PrintGeneralStatistics();
        PrintTagsErrorRank();
        PrintGeneralConfusionTable();
    }
}
