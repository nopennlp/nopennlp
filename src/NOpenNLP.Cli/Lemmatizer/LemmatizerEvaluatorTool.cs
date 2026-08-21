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
using System.Globalization;
using System.IO;
using NOpenNLP.Tools.Formats;
using NOpenNLP.Tools.Lemmatizer;

namespace NOpenNLP.Tools.Cmdline.Lemmatizer;

public sealed class LemmatizerEvaluatorTool : AbstractEvaluatorTool<LemmaSample?>
{
    // NOpenNLP: upstream's EvalToolParams interface extends EvaluatorParams and
    // FineGrainedEvaluatorParams; the options those declare are created here instead.
    private readonly Option<FileInfo> model = ToolParams.ModelForEvaluation();
    private readonly Option<bool> misclassified = ToolParams.Misclassified();
    private readonly Option<FileInfo?> reportOutputFile = ToolParams.ReportOutputFile();

    /// <inheritdoc/>
    protected override IEnumerable<Option> GetToolOptions() =>
        [model, misclassified, reportOutputFile];

    /// <inheritdoc/>
    public override string ShortDescription =>
        "Measures the performance of the Lemmatizer model with the reference data";

    /// <inheritdoc/>
    public override string GetHelp(string format) =>
        "Usage: " + CLI.Cmd + " " + Name + GetFormatsHelp(format)
            + OptionUsage.CreateUsage(GetToolOptions(), GetStreamFactory(format).Parameters);

    /// <inheritdoc/>
    protected override void Run(ParseResult parseResult)
    {
        LemmatizerModel model = new LemmatizerModelLoader().Load(parseResult.GetValue(this.model)!);

        ILemmatizerEvaluationMonitor? missclassifiedListener = null;
        if (parseResult.GetValue(misclassified))
        {
            missclassifiedListener = new LemmaEvaluationErrorListener();
        }

        LemmatizerFineGrainedReportListener? reportListener = null;
        FileInfo? reportFile = parseResult.GetValue(reportOutputFile);
        Stream? reportOutputStream = null;
        if (reportFile != null)
        {
            CmdLineUtil.CheckOutputFile("Report Output File", reportFile);
            try
            {
                reportOutputStream = reportFile.Create();
                reportListener = new LemmatizerFineGrainedReportListener(reportOutputStream);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                throw new TerminateToolException(-1,
                    "IO error while creating Lemmatizer fine-grained report file: "
                        + e.Message);
            }
        }

        var evaluator = new LemmatizerEvaluator(
            new LemmatizerME(model), missclassifiedListener, reportListener);

        Console.Write("Evaluating ... ");
        try
        {
            evaluator.Evaluate(sampleStream!);
        }
        catch (IOException e)
        {
            Console.Error.WriteLine("failed");
            throw new TerminateToolException(-1,
                "IO error while reading test data: " + e.Message, e);
        }
        finally
        {
            try
            {
                sampleStream!.Dispose();
            }
            catch (IOException)
            {
                // sorry that this can fail
            }
        }

        Console.WriteLine("done");

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

        Console.WriteLine();

        // NOpenNLP: upstream concatenates a double, which Java renders with
        // Double.toString; J2N's "J" format reproduces that, as it does elsewhere in
        // the port.
        Console.WriteLine("Accuracy: " + J2N.Numerics.Double.ToString(
            evaluator.WordAccuracy, "J", CultureInfo.InvariantCulture));
    }
}
