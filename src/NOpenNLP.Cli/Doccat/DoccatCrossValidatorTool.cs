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
using NOpenNLP.Tools.Doccat;
using NOpenNLP.Tools.Util.Model;

namespace NOpenNLP.Tools.Cmdline.Doccat;

public sealed class DoccatCrossValidatorTool : AbstractCrossValidatorTool<DocumentSample?>
{
    private readonly Option<int> folds = ToolParams.Folds();
    private readonly Option<bool> misclassified = ToolParams.Misclassified();
    private readonly Option<string> lang = ToolParams.Lang();
    private readonly Option<string?> @params = ToolParams.Params();
    private readonly Option<string?> featureGenerators = TrainingParams.FeatureGenerators();
    private readonly Option<string?> factoryName = TrainingParams.Factory();
    private readonly Option<FileInfo?> reportOutputFile = ToolParams.ReportOutputFile();

    /// <inheritdoc/>
    public override string ShortDescription =>
        "K-fold cross validator for the learnable Document Categorizer";

    /// <inheritdoc/>
    protected override IEnumerable<Option> GetToolOptions() =>
        [folds, misclassified, lang, @params, featureGenerators, factoryName, reportOutputFile];

    /// <inheritdoc/>
    public override string GetHelp(string format) =>
        "Usage: " + CLI.Cmd + " " + Name + GetFormatsHelp(format) +
        OptionUsage.CreateUsage(GetToolOptions(), GetStreamFactory(format).Parameters);

    /// <inheritdoc/>
    protected override void Run(ParseResult parseResult)
    {
        mlParams = CmdLineUtil.LoadTrainingParameters(parseResult.GetValue(@params), false);
        if (mlParams == null)
        {
            mlParams = ModelUtil.CreateDefaultTrainingParameters();
        }

        var listeners = new List<IDoccatEvaluationMonitor>();
        if (parseResult.GetValue(misclassified))
        {
            listeners.Add(new DoccatEvaluationErrorListener());
        }

        DoccatFineGrainedReportListener? reportListener = null;
        FileInfo? reportFile = parseResult.GetValue(reportOutputFile);
        Stream? reportOutputStream = null;
        if (reportFile != null)
        {
            CmdLineUtil.CheckOutputFile("Report Output File", reportFile);
            try
            {
                reportOutputStream = reportFile.Create();
                reportListener = new DoccatFineGrainedReportListener(reportOutputStream);
                listeners.Add(reportListener);
            }
            catch (IOException e)
            {
                // NOpenNLP: upstream catches FileNotFoundException, which is what
                // `new FileOutputStream(File)` throws when the file cannot be created.
                // FileInfo.Create surfaces the same conditions as IOException.
                throw CreateTerminationIOException(e);
            }
        }

        IFeatureGenerator[] featureGeneratorsArr =
            DoccatTrainerTool.CreateFeatureGenerators(parseResult.GetValue(featureGenerators));

        IDoccatEvaluationMonitor[] listenersArr = listeners.ToArray();

        DoccatCrossValidator validator;
        try
        {
            DoccatFactory factory = DoccatFactory.Create(parseResult.GetValue(factoryName),
                featureGeneratorsArr);
            validator = new DoccatCrossValidator(parseResult.GetRequiredValue(lang), mlParams,
                factory, listenersArr);

            validator.Evaluate(sampleStream!, parseResult.GetRequiredValue(folds));
        }
        catch (IOException e)
        {
            throw new Formats.TerminateToolException(-1,
                "IO error while reading training data or indexing data: " + e.Message, e);
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
            Console.WriteLine("Writing fine-grained report to " + reportFile!.FullName);
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
            validator.DocumentAccuracy, "J", CultureInfo.InvariantCulture) + "\n" +
            "Number of documents: " + validator.DocumentCount);
    }
}
