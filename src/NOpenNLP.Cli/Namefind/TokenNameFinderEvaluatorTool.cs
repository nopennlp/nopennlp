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
using NOpenNLP.Tools.Formats;
using NOpenNLP.Tools.Namefind;
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Cmdline.Namefind;

public sealed class TokenNameFinderEvaluatorTool : AbstractEvaluatorTool<NameSample?>
{
    // NOpenNLP: upstream's EvalToolParams interface extends EvaluatorParams,
    // DetailedFMeasureEvaluatorParams and FineGrainedEvaluatorParams, and adds
    // getNameTypes() of its own; the options those declare are created here instead.
    private readonly Option<FileInfo> model = ToolParams.ModelForEvaluation();
    private readonly Option<string?> misclassified = ToolParams.Misclassified();
    private readonly Option<string?> detailedF = ToolParams.DetailedF();
    private readonly Option<FileInfo?> reportOutputFile = ToolParams.ReportOutputFile();

    private readonly Option<string?> nameTypes = new Option<string?>("-nameTypes")
    {
        Description = "name types to use for evaluation",
        HelpName = "types",
    };

    /// <inheritdoc/>
    protected override IEnumerable<Option> GetToolOptions() =>
        [model, misclassified, detailedF, reportOutputFile, nameTypes];

    /// <inheritdoc/>
    public override string ShortDescription =>
        "Measures the performance of the NameFinder model with the reference data";

    /// <inheritdoc/>
    public override string GetHelp(string format) =>
        "Usage: " + CLI.Cmd + " " + Name + GetFormatsHelp(format)
            + OptionUsage.CreateUsage(GetToolOptions(), GetStreamFactory(format).Parameters);

    /// <inheritdoc/>
    protected override void Run(ParseResult parseResult)
    {
        TokenNameFinderModel model =
            new TokenNameFinderModelLoader().Load(parseResult.GetValue(this.model)!);

        var listeners = new JCG.List<ITokenNameFinderEvaluationMonitor>();
        if (ToolParams.JavaBooleanValue(parseResult.GetValue(misclassified)))
        {
            listeners.Add(new NameEvaluationErrorListener());
        }

        TokenNameFinderDetailedFMeasureListener? detailedFListener = null;
        if (ToolParams.JavaBooleanValue(parseResult.GetValue(detailedF)))
        {
            detailedFListener = new TokenNameFinderDetailedFMeasureListener();
            listeners.Add(detailedFListener);
        }

        TokenNameFinderFineGrainedReportListener? reportListener = null;
        FileInfo? reportFile = parseResult.GetValue(reportOutputFile);
        Stream? reportOutputStream = null;

        if (reportFile != null)
        {
            CmdLineUtil.CheckOutputFile("Report Output File", reportFile);
            try
            {
                reportOutputStream = reportFile.Create();
                reportListener = new TokenNameFinderFineGrainedReportListener(
                    model.SequenceCodec, reportOutputStream);
                listeners.Add(reportListener);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                throw new TerminateToolException(-1,
                    "IO error while creating Name Finder fine-grained report file: "
                        + e.Message);
            }
        }

        string? nameTypesValue = parseResult.GetValue(nameTypes);
        if (nameTypesValue != null)
        {
            string[] types = StringUtil.SplitDroppingTrailingEmpty(nameTypesValue, ',');
            sampleStream = new NameSampleTypeFilter(types, sampleStream!);
        }

        var evaluator = new TokenNameFinderEvaluator(
            new NameFinderME(model), [.. listeners]);

        using var monitor = new PerformanceMonitor("sent");

        try
        {
            using IObjectStream<NameSample?> measuredSampleStream =
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

        if (reportFile != null)
        {
            reportListener!.WriteReport();

            // NOpenNLP: upstream never closes reportOutputStream here, unlike the other
            // evaluator tools. The StreamWriter the listener wraps it in auto-flushes,
            // so the report still reaches the file; the stream is disposed so the handle
            // is not left open for the life of the process.
            reportOutputStream!.Dispose();
        }

        if (detailedFListener == null)
        {
            Console.WriteLine(evaluator.FMeasure);
        }
        else
        {
            Console.WriteLine(detailedFListener);
        }
    }

    // NOpenNLP: stands in for the anonymous ObjectStream upstream wraps the sample
    // stream in so the performance monitor counts each read.
    private sealed class MeasuredObjectStream(
        PerformanceMonitor monitor, IObjectStream<NameSample?> sampleStream)
        : IObjectStream<NameSample?>
    {
        public NameSample? Read()
        {
            monitor.IncrementCounter();
            return sampleStream.Read();
        }

        public void Reset() => sampleStream.Reset();

        public void Dispose() => sampleStream.Dispose();
    }
}
