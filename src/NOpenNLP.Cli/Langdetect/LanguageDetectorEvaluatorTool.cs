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
using NOpenNLP.Tools.Langdetect;
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Cmdline.Langdetect;

public sealed class LanguageDetectorEvaluatorTool : AbstractEvaluatorTool<LanguageSample?>
{
    // NOpenNLP: upstream's EvalToolParams interface extends EvaluatorParams and
    // FineGrainedEvaluatorParams; the options those declare are created here instead.
    private readonly Option<FileInfo> model = ToolParams.ModelForEvaluation();
    private readonly Option<string?> misclassified = ToolParams.Misclassified();
    private readonly Option<FileInfo?> reportOutputFile = ToolParams.ReportOutputFile();

    /// <inheritdoc/>
    protected override IEnumerable<Option> GetToolOptions() =>
        [model, misclassified, reportOutputFile];

    /// <inheritdoc/>
    public override string ShortDescription =>
        "Measures the performance of the Language Detector model with the reference data";

    /// <inheritdoc/>
    public override string GetHelp(string format) =>
        "Usage: " + CLI.Cmd + " " + Name + GetFormatsHelp(format)
            + OptionUsage.CreateUsage(GetToolOptions(), GetStreamFactory(format).Parameters);

    /// <inheritdoc/>
    protected override void Run(ParseResult parseResult)
    {
        LanguageDetectorModel model =
            new LanguageDetectorModelLoader().Load(parseResult.GetValue(this.model)!);

        var listeners = new JCG.List<ILanguageDetectorEvaluationMonitor>();
        if (ToolParams.JavaBooleanValue(parseResult.GetValue(misclassified)))
        {
            listeners.Add(new LanguageDetectorEvaluationErrorListener());
        }

        LanguageDetectorFineGrainedReportListener? reportListener = null;
        FileInfo? reportFile = parseResult.GetValue(reportOutputFile);
        Stream? reportOutputStream = null;
        if (reportFile != null)
        {
            CmdLineUtil.CheckOutputFile("Report Output File", reportFile);
            try
            {
                reportOutputStream = reportFile.Create();
                reportListener = new LanguageDetectorFineGrainedReportListener(reportOutputStream);
                listeners.Add(reportListener);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                throw new TerminateToolException(-1,
                    "IO error while creating LanguageDetector fine-grained report file: "
                        + e.Message);
            }
        }

        var evaluator = new LanguageDetectorEvaluator(
            new LanguageDetectorME(model), [.. listeners]);

        using var monitor = new PerformanceMonitor("doc");

        try
        {
            using IObjectStream<LanguageSample?> measuredSampleStream =
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

        Console.WriteLine(evaluator);

        if (reportListener != null)
        {
            Console.WriteLine("Writing fine-grained report to "
                + reportFile!.FullName);
            reportListener.WriteReport();

            try
            {
                // TODO: is it a problem to close the stream now?
                reportOutputStream!.Dispose();
            }
            catch (IOException)
            {
                // nothing to do
            }
        }
    }

    // NOpenNLP: stands in for the anonymous ObjectStream upstream wraps the sample
    // stream in so the performance monitor counts each read.
    private sealed class MeasuredObjectStream(
        PerformanceMonitor monitor, IObjectStream<LanguageSample?> sampleStream)
        : IObjectStream<LanguageSample?>
    {
        public LanguageSample? Read()
        {
            monitor.IncrementCounter();
            return sampleStream.Read();
        }

        public void Reset() => sampleStream.Reset();

        public void Dispose() => sampleStream.Dispose();
    }
}
