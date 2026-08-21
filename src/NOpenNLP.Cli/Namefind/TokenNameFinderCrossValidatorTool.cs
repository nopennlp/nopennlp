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

namespace NOpenNLP.Tools.Cmdline.Namefind;

public sealed class TokenNameFinderCrossValidatorTool : AbstractCrossValidatorTool<NameSample?>
{
    private readonly Option<string?> type = TrainingParams.Type();
    private readonly Option<DirectoryInfo?> resources = TrainingParams.Resources();
    private readonly Option<FileInfo?> featuregen = TrainingParams.Featuregen();
    private readonly Option<string?> nameTypes = TrainingParams.NameTypes();
    private readonly Option<string> sequenceCodec = TrainingParams.SequenceCodec();
    private readonly Option<string?> factoryName = TrainingParams.Factory();
    private readonly Option<string> lang = ToolParams.Lang();
    private readonly Option<string?> @params = ToolParams.Params();
    private readonly Option<int> folds = ToolParams.Folds();
    private readonly Option<string?> misclassified = ToolParams.Misclassified();
    private readonly Option<string?> detailedF = ToolParams.DetailedF();
    private readonly Option<FileInfo?> reportOutputFile = ToolParams.ReportOutputFile();

    /// <inheritdoc/>
    public override string ShortDescription => "K-fold cross validator for the learnable Name Finder";

    /// <inheritdoc/>
    protected override IEnumerable<Option> GetToolOptions() =>
    [
        type, resources, featuregen, nameTypes, sequenceCodec, factoryName, lang, @params,
        folds, misclassified, detailedF, reportOutputFile,
    ];

    /// <inheritdoc/>
    public override string GetHelp(string format) =>
        "Usage: " + CLI.Cmd + " " + Name + GetFormatsHelp(format) +
        OptionUsage.CreateUsage(GetToolOptions(), GetStreamFactory(format).Parameters);

    /// <inheritdoc/>
    protected override void Run(ParseResult parseResult)
    {
        mlParams = CmdLineUtil.LoadTrainingParameters(parseResult.GetValue(@params), true);
        if (mlParams == null)
        {
            mlParams = new TrainingParameters();
        }

        FileInfo? featuregenFile = parseResult.GetValue(featuregen);

        byte[]? featureGeneratorBytes =
            TokenNameFinderTrainerTool.OpenFeatureGeneratorBytes(featuregenFile);

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

        string? nameTypesValue = parseResult.GetValue(nameTypes);
        if (nameTypesValue != null)
        {
            string[] nameTypesArr = StringUtil.SplitDroppingTrailingEmpty(nameTypesValue, ',');
            sampleStream = new NameSampleTypeFilter(nameTypesArr, sampleStream!);
        }

        var listeners = new List<ITokenNameFinderEvaluationMonitor>();
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

        string? sequenceCodecImplName = parseResult.GetValue(sequenceCodec);

        if ("BIO".Equals(sequenceCodecImplName, StringComparison.Ordinal))
        {
            sequenceCodecImplName = typeof(BioCodec).FullName;
        }
        else if ("BILOU".Equals(sequenceCodecImplName, StringComparison.Ordinal))
        {
            sequenceCodecImplName = typeof(BilouCodec).FullName;
        }

        ISequenceCodec<string> codec =
            TokenNameFinderFactory.InstantiateSequenceCodec(sequenceCodecImplName);

        TokenNameFinderFineGrainedReportListener? reportListener = null;
        FileInfo? reportFile = parseResult.GetValue(reportOutputFile);
        Stream? reportOutputStream = null;

        if (reportFile != null)
        {
            CmdLineUtil.CheckOutputFile("Report Output File", reportFile);
            try
            {
                reportOutputStream = reportFile.Create();
                reportListener = new TokenNameFinderFineGrainedReportListener(codec,
                    reportOutputStream);
                listeners.Add(reportListener);
            }
            catch (IOException e)
            {
                // NOpenNLP: upstream catches FileNotFoundException from
                // `new FileOutputStream(File)`; FileInfo.Create reports the same
                // conditions as IOException. Upstream drops the cause here, so it is
                // dropped here too.
                throw new TerminateToolException(-1,
                    "IO error while creating Name Finder fine-grained report file: " + e.Message);
            }
        }

        TokenNameFinderFactory nameFinderFactory;
        try
        {
            nameFinderFactory = TokenNameFinderFactory.Create(parseResult.GetValue(factoryName),
                featureGeneratorBytes, resourcesMap, codec);
        }
        catch (InvalidFormatException e)
        {
            throw new TerminateToolException(-1, e.Message, e);
        }

        TokenNameFinderCrossValidator validator;
        try
        {
            validator = new TokenNameFinderCrossValidator(parseResult.GetRequiredValue(lang),
                parseResult.GetValue(type), mlParams, nameFinderFactory, listeners.ToArray());
            validator.Evaluate(sampleStream!, parseResult.GetRequiredValue(folds));
        }
        catch (IOException e)
        {
            throw CreateTerminationIOException(e);
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

        Console.WriteLine();

        if (reportFile != null)
        {
            reportListener!.WriteReport();
        }

        if (detailedFListener == null)
        {
            Console.WriteLine(validator.FMeasure);
        }
        else
        {
            Console.WriteLine(detailedFListener);
        }
    }
}
