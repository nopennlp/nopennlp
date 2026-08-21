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
using System.IO;
using NOpenNLP.Tools.Postag;

namespace NOpenNLP.Tools.Cmdline.Postag;

/// <summary>
/// Generates a detailed report for the POS Tagger.
/// <para/>
/// It is possible to use it from an API and access the statistics using the
/// provided getters.
/// </summary>
public class POSTaggerFineGrainedReportListener : FineGrainedReportListener,
    IPOSTaggerEvaluationMonitor
{
    /// <summary>
    /// Creates a listener that will print to <see cref="Console.Error"/>.
    /// </summary>
    public POSTaggerFineGrainedReportListener()
        : this(Console.Error)
    {
    }

    /// <summary>
    /// Creates a listener that prints to a given <see cref="TextWriter"/>.
    /// </summary>
    public POSTaggerFineGrainedReportListener(TextWriter outputStream)
        : base(outputStream)
    {
    }

    /// <summary>
    /// Creates a listener that prints to a given <see cref="Stream"/>.
    /// </summary>
    public POSTaggerFineGrainedReportListener(Stream outputStream)
        : base(outputStream)
    {
    }

    // methods inherited from EvaluationMonitor

    /// <inheritdoc/>
    public void Misclassified(POSSample reference, POSSample prediction) =>
        StatsAdd(reference, prediction);

    /// <inheritdoc/>
    public void CorrectlyClassified(POSSample reference, POSSample prediction) =>
        StatsAdd(reference, prediction);

    private void StatsAdd(POSSample reference, POSSample prediction) =>
        GetStats().Add(reference.Sentence, reference.Tags, prediction.Tags);

    /// <inheritdoc/>
    public override void WriteReport()
    {
        PrintGeneralStatistics();
        // token stats
        PrintTokenErrorRank();
        PrintTokenOcurrenciesRank();
        // tag stats
        PrintTagsErrorRank();
        // confusion tables
        PrintGeneralConfusionTable();
        PrintDetailedConfusionMatrix();
    }
}
