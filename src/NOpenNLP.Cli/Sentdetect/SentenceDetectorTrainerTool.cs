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
using NOpenNLP.Tools.Formats;
using NOpenNLP.Tools.Ml;
using NOpenNLP.Tools.Sentdetect;
using NOpenNLP.Tools.Util.Model;
using OpenNlpDictionary = NOpenNLP.Tools.Dictionary.Dictionary;

namespace NOpenNLP.Tools.Cmdline.Sentdetect;

public sealed class SentenceDetectorTrainerTool : AbstractTrainerTool<SentenceSample?>
{
    private readonly Option<FileInfo?> abbDict = TrainingParams.AbbDict();
    private readonly Option<string?> eosChars = TrainingParams.EosChars();
    private readonly Option<string?> factoryName = TrainingParams.Factory();
    private readonly Option<string> lang = ToolParams.Lang();
    private readonly Option<string?> @params = ToolParams.Params();
    private readonly Option<FileInfo> model = ToolParams.ModelForTraining();

    /// <inheritdoc/>
    public override string ShortDescription => "trainer for the learnable sentence detector";

    /// <inheritdoc/>
    protected override IEnumerable<Option> GetToolOptions() =>
        [abbDict, eosChars, factoryName, lang, @params, model];

    /// <inheritdoc/>
    public override string GetHelp(string format) =>
        "Usage: " + CLI.Cmd + " " + Name + GetFormatsHelp(format) +
        OptionUsage.CreateUsage(GetToolOptions(), GetStreamFactory(format).Parameters);

    /// <exception cref="IOException">if the dictionary cannot be read</exception>
    internal static OpenNlpDictionary? LoadDict(FileInfo? f)
    {
        OpenNlpDictionary? dict = null;
        if (f != null)
        {
            CmdLineUtil.CheckInputFile("abb dict", f);
            using Stream dictIn = f.OpenRead();
            dict = new OpenNlpDictionary(dictIn);
        }

        return dict;
    }

    /// <inheritdoc/>
    protected override void Run(ParseResult parseResult)
    {
        mlParams = CmdLineUtil.LoadTrainingParameters(parseResult.GetValue(@params), false);

        if (mlParams != null)
        {
            if (TrainerFactory.TrainerType.EVENT_MODEL_TRAINER
                != TrainerFactory.GetTrainerType(mlParams))
            {
                throw new TerminateToolException(1, "Sequence training is not supported!");
            }
        }

        if (mlParams == null)
        {
            mlParams = ModelUtil.CreateDefaultTrainingParameters();
        }

        FileInfo modelOutFile = parseResult.GetRequiredValue(model);
        CmdLineUtil.CheckOutputFile("sentence detector model", modelOutFile);

        char[]? eos = null;
        string? eosCharsValue = parseResult.GetValue(eosChars);
        if (eosCharsValue != null)
        {
            string eosString = SentenceSampleStream.ReplaceNewLineEscapeTags(eosCharsValue);
            eos = eosString.ToCharArray();
        }

        SentenceModel sentenceModel;

        try
        {
            OpenNlpDictionary? dict = LoadDict(parseResult.GetValue(abbDict));
            SentenceDetectorFactory sdFactory = SentenceDetectorFactory.Create(
                parseResult.GetValue(factoryName), parseResult.GetRequiredValue(lang), true,
                dict!, eos!);
            sentenceModel = SentenceDetectorME.Train(parseResult.GetRequiredValue(lang),
                sampleStream!, sdFactory, mlParams);
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

        CmdLineUtil.WriteModel("sentence detector", modelOutFile, sentenceModel);
    }
}
