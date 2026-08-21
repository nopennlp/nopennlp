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

using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using NOpenNLP.Tools.Doccat;
using NOpenNLP.Tools.Util;
using NOpenNLP.Tools.Util.Ext;
using NOpenNLP.Tools.Util.Model;

namespace NOpenNLP.Tools.Cmdline.Doccat;

public class DoccatTrainerTool : AbstractTrainerTool<DocumentSample?>
{
    private readonly Option<string> lang = ToolParams.Lang();
    private readonly Option<string?> @params = ToolParams.Params();
    private readonly Option<string?> featureGenerators = TrainingParams.FeatureGenerators();
    private readonly Option<string?> factoryName = TrainingParams.Factory();
    private readonly Option<FileInfo> model = ToolParams.ModelForTraining();

    /// <inheritdoc/>
    public override string ShortDescription => "trainer for the learnable document categorizer";

    /// <inheritdoc/>
    protected override IEnumerable<Option> GetToolOptions() =>
        [lang, @params, featureGenerators, factoryName, model];

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

        FileInfo modelOutFile = parseResult.GetRequiredValue(model);

        CmdLineUtil.CheckOutputFile("document categorizer model", modelOutFile);

        IFeatureGenerator[] featureGeneratorsArr =
            CreateFeatureGenerators(parseResult.GetValue(featureGenerators));

        DoccatModel doccatModel;
        try
        {
            DoccatFactory factory = DoccatFactory.Create(parseResult.GetValue(factoryName),
                featureGeneratorsArr);
            doccatModel = DocumentCategorizerME.Train(parseResult.GetRequiredValue(lang),
                sampleStream!, mlParams, factory);
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

        CmdLineUtil.WriteModel("document categorizer", modelOutFile, doccatModel);
    }

    internal static IFeatureGenerator[] CreateFeatureGenerators(string? featureGeneratorsNames)
    {
        if (featureGeneratorsNames == null)
        {
            return [new BagOfWordsFeatureGenerator()];
        }

        // NOpenNLP: upstream's String.split(",") discards trailing empty strings, so a
        // trailing comma in -featureGenerators is ignored. string.Split would keep an
        // empty entry and hand "" to the extension loader, failing a command line that
        // works upstream.
        string[] classes = StringUtil.SplitDroppingTrailingEmpty(featureGeneratorsNames, ',');
        var featureGenerators = new IFeatureGenerator[classes.Length];
        for (int i = 0; i < featureGenerators.Length; i++)
        {
            featureGenerators[i] = ExtensionLoader.InstantiateExtension<IFeatureGenerator>(classes[i])!;
        }

        return featureGenerators;
    }
}
