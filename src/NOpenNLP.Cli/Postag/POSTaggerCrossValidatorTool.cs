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
using NOpenNLP.Tools.Cmdline.Namefind;
using NOpenNLP.Tools.Formats;
using NOpenNLP.Tools.Postag;
using NOpenNLP.Tools.Util.Model;

namespace NOpenNLP.Tools.Cmdline.Postag;

public sealed class POSTaggerCrossValidatorTool : AbstractCrossValidatorTool<POSSample?>
{
    private readonly Option<int> folds = ToolParams.Folds();
    private readonly Option<bool> misclassified = ToolParams.Misclassified();
    private readonly Option<FileInfo?> featuregen = TrainingParams.Featuregen();
    private readonly Option<DirectoryInfo?> resources = TrainingParams.Resources();
    private readonly Option<FileInfo?> dict = TrainingParams.Dict();
    private readonly Option<int?> tagDictCutoff = TrainingParams.TagDictCutoff();
    private readonly Option<string?> factoryName = TrainingParams.Factory();
    private readonly Option<string> lang = ToolParams.Lang();
    private readonly Option<string?> @params = ToolParams.Params();
    private readonly Option<FileInfo?> reportOutputFile = ToolParams.ReportOutputFile();

    /// <inheritdoc/>
    public override string ShortDescription => "K-fold cross validator for the learnable POS tagger";

    /// <inheritdoc/>
    protected override IEnumerable<Option> GetToolOptions() =>
    [
        folds, misclassified, featuregen, resources, dict, tagDictCutoff, factoryName, lang,
        @params, reportOutputFile,
    ];

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

        IPOSTaggerEvaluationMonitor? missclassifiedListener = null;
        if (parseResult.GetValue(misclassified))
        {
            missclassifiedListener = new POSEvaluationErrorListener();
        }

        POSTaggerFineGrainedReportListener? reportListener = null;
        FileInfo? reportFile = parseResult.GetValue(reportOutputFile);
        Stream? reportOutputStream = null;
        if (reportFile != null)
        {
            CmdLineUtil.CheckOutputFile("Report Output File", reportFile);
            try
            {
                reportOutputStream = reportFile.Create();
                reportListener = new POSTaggerFineGrainedReportListener(reportOutputStream);
            }
            catch (IOException e)
            {
                // NOpenNLP: upstream catches FileNotFoundException from
                // `new FileOutputStream(File)`; FileInfo.Create reports the same
                // conditions as IOException.
                throw CreateTerminationIOException(e);
            }
        }

        FileInfo? featuregenFile = parseResult.GetValue(featuregen);

        IDictionary<string, object> resourcesMap;
        try
        {
            resourcesMap = TokenNameFinderTrainerTool.LoadResources(
                parseResult.GetValue(resources), featuregenFile);
        }
        catch (IOException e)
        {
            throw new TerminateToolException(-1, "IO error while loading resources", e);
        }

        byte[]? featureGeneratorBytes =
            TokenNameFinderTrainerTool.OpenFeatureGeneratorBytes(featuregenFile);

        POSTaggerCrossValidator validator;
        try
        {
            // NOpenNLP: the validator takes a concrete Dictionary<string, object>, so
            // the map LoadResources returns is copied into one here.
            validator = new POSTaggerCrossValidator(parseResult.GetRequiredValue(lang), mlParams,
                parseResult.GetValue(dict), featureGeneratorBytes,
                new Dictionary<string, object>(resourcesMap), parseResult.GetValue(tagDictCutoff),
                parseResult.GetValue(factoryName), missclassifiedListener, reportListener);

            validator.Evaluate(sampleStream!, parseResult.GetRequiredValue(folds));
        }
        catch (IOException e)
        {
            throw new TerminateToolException(-1,
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
            validator.WordAccuracy, "J", CultureInfo.InvariantCulture));
    }
}
