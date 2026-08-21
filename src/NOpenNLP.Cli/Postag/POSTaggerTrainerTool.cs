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
using NOpenNLP.Tools.Cmdline.Namefind;
using NOpenNLP.Tools.Formats;
using NOpenNLP.Tools.Ml;
using NOpenNLP.Tools.Postag;
using NOpenNLP.Tools.Util;
using NOpenNLP.Tools.Util.Model;

namespace NOpenNLP.Tools.Cmdline.Postag;

public sealed class POSTaggerTrainerTool : AbstractTrainerTool<POSSample?>
{
    private readonly Option<FileInfo?> featuregen = TrainingParams.Featuregen();
    private readonly Option<DirectoryInfo?> resources = TrainingParams.Resources();
    private readonly Option<FileInfo?> dict = TrainingParams.Dict();
    private readonly Option<int?> tagDictCutoff = TrainingParams.TagDictCutoff();
    private readonly Option<string?> factoryName = TrainingParams.Factory();
    private readonly Option<string> lang = ToolParams.Lang();
    private readonly Option<string?> @params = ToolParams.Params();
    private readonly Option<FileInfo> model = ToolParams.ModelForTraining();

    /// <inheritdoc/>
    public override string ShortDescription => "trains a model for the part-of-speech tagger";

    /// <inheritdoc/>
    protected override IEnumerable<Option> GetToolOptions() =>
        [featuregen, resources, dict, tagDictCutoff, factoryName, lang, @params, model];

    /// <inheritdoc/>
    public override string GetHelp(string format) =>
        "Usage: " + CLI.Cmd + " " + Name + GetFormatsHelp(format) +
        OptionUsage.CreateUsage(GetToolOptions(), GetStreamFactory(format).Parameters);

    /// <inheritdoc/>
    protected override void Run(ParseResult parseResult)
    {
        string? paramsFile = parseResult.GetValue(@params);

        mlParams = CmdLineUtil.LoadTrainingParameters(paramsFile, true);
        if (mlParams != null && !TrainerFactory.IsValid(mlParams))
        {
            throw new TerminateToolException(1, "Training parameters file '" + paramsFile +
                "' is invalid!");
        }

        if (mlParams == null)
        {
            mlParams = ModelUtil.CreateDefaultTrainingParameters();
        }

        FileInfo modelOutFile = parseResult.GetRequiredValue(model);
        CmdLineUtil.CheckOutputFile("pos tagger model", modelOutFile);

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

        POSTaggerFactory postaggerFactory;
        try
        {
            // NOpenNLP: POSTaggerFactory.Create takes a concrete Dictionary<string,
            // object> where TokenNameFinderFactory.Create takes the interface, so the
            // map LoadResources returns is copied into one here.
            postaggerFactory = POSTaggerFactory.Create(parseResult.GetValue(factoryName),
                featureGeneratorBytes, new Dictionary<string, object>(resourcesMap), null);
        }
        catch (InvalidFormatException e)
        {
            throw new TerminateToolException(-1, e.Message, e);
        }

        FileInfo? dictFile = parseResult.GetValue(dict);
        if (dictFile != null)
        {
            try
            {
                postaggerFactory.TagDictionary = postaggerFactory.CreateTagDictionary(dictFile);
            }
            catch (IOException e)
            {
                throw new TerminateToolException(-1, "IO error while loading POS Dictionary", e);
            }
        }

        int? cutoff = parseResult.GetValue(tagDictCutoff);
        if (cutoff != null)
        {
            try
            {
                ITagDictionary? tagDict = postaggerFactory.TagDictionary;
                if (tagDict == null)
                {
                    tagDict = postaggerFactory.CreateEmptyTagDictionary();
                    postaggerFactory.TagDictionary = tagDict;
                }

                if (tagDict is IMutableTagDictionary mutableTagDict)
                {
                    POSTaggerME.PopulatePOSDictionary(sampleStream!, mutableTagDict, cutoff.Value);
                }
                else
                {
                    throw new ArgumentException(
                        "Can't extend a POSDictionary that does not implement MutableTagDictionary.");
                }

                sampleStream!.Reset();
            }
            catch (IOException e)
            {
                throw new TerminateToolException(-1,
                    "IO error while creating/extending POS Dictionary: " + e.Message, e);
            }
        }

        POSModel posModel;
        try
        {
            posModel = POSTaggerME.Train(parseResult.GetRequiredValue(lang), sampleStream!,
                mlParams, postaggerFactory);
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

        CmdLineUtil.WriteModel("pos tagger", modelOutFile, posModel);
    }
}
