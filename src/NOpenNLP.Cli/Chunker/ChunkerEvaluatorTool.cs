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
using System.CommandLine;
using System.IO;
using NOpenNLP.Tools.Chunker;
using NOpenNLP.Tools.Formats;
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Cmdline.Chunker;

public sealed class ChunkerEvaluatorTool : AbstractEvaluatorTool<ChunkSample?>
{
    // NOpenNLP: upstream's EvalToolParams interface extends EvaluatorParams and
    // DetailedFMeasureEvaluatorParams; the options those declare are created here.
    private readonly Option<FileInfo> model = ToolParams.ModelForEvaluation();
    private readonly Option<bool> misclassified = ToolParams.Misclassified();
    private readonly Option<bool> detailedF = ToolParams.DetailedF();

    /// <inheritdoc/>
    protected override IEnumerable<Option> GetToolOptions() => [model, misclassified, detailedF];

    /// <inheritdoc/>
    public override string ShortDescription =>
        "Measures the performance of the Chunker model with the reference data";

    /// <inheritdoc/>
    public override string GetHelp(string format) =>
        "Usage: " + CLI.Cmd + " " + Name + GetFormatsHelp(format)
            + OptionUsage.CreateUsage(GetToolOptions(), GetStreamFactory(format).Parameters);

    /// <inheritdoc/>
    protected override void Run(ParseResult parseResult)
    {
        ChunkerModel model = new ChunkerModelLoader().Load(parseResult.GetValue(this.model)!);

        var listeners = new JCG.List<IChunkerEvaluationMonitor>();
        ChunkerDetailedFMeasureListener? detailedFMeasureListener = null;
        if (parseResult.GetValue(misclassified))
        {
            listeners.Add(new ChunkEvaluationErrorListener());
        }

        if (parseResult.GetValue(detailedF))
        {
            detailedFMeasureListener = new ChunkerDetailedFMeasureListener();
            listeners.Add(detailedFMeasureListener);
        }

        var evaluator = new ChunkerEvaluator(new ChunkerME(model), [.. listeners]);

        using var monitor = new PerformanceMonitor("sent");

        try
        {
            using IObjectStream<ChunkSample?> measuredSampleStream =
                new MeasuredObjectStream(monitor, sampleStream!);

            monitor.StartAndPrintThroughput();
            evaluator.Evaluate(measuredSampleStream);
        }
        catch (IOException e)
        {
            Console.Error.WriteLine("failed");
            throw new TerminateToolException(-1, "IO error while reading test data: "
                + e.Message, e);
        }

        // sorry that this can fail

        monitor.StopAndPrintFinalResult();

        Console.WriteLine();

        if (detailedFMeasureListener == null)
        {
            Console.WriteLine(evaluator.FMeasure);
        }
        else
        {
            Console.WriteLine(detailedFMeasureListener);
        }
    }

    // NOpenNLP: stands in for the anonymous ObjectStream upstream wraps the sample
    // stream in so the performance monitor counts each read.
    private sealed class MeasuredObjectStream(
        PerformanceMonitor monitor, IObjectStream<ChunkSample?> sampleStream)
        : IObjectStream<ChunkSample?>
    {
        public ChunkSample? Read()
        {
            monitor.IncrementCounter();
            return sampleStream.Read();
        }

        public void Reset() => sampleStream.Reset();

        public void Dispose() => sampleStream.Dispose();
    }
}
