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
using NOpenNLP.Tools.Langdetect;
using NOpenNLP.Tools.Util.Model;

namespace NOpenNLP.Tools.Cmdline.Langdetect;

public class LanguageDetectorTrainerTool : AbstractTrainerTool<LanguageSample?>
{
    // NOpenNLP: upstream's TrainerToolParams re-declares -model and -params rather than
    // extending TrainingToolParams, because langdetect's TrainingParams does not extend
    // BasicTrainingParams and so must not pull in -lang. The two re-declared
    // descriptions are identical to TrainingToolParams', so the shared factories serve.
    private readonly Option<FileInfo> model = ToolParams.ModelForTraining();
    private readonly Option<string?> @params = TrainingParams.Params();
    private readonly Option<string?> factoryName = TrainingParams.Factory();

    /// <inheritdoc/>
    public override string ShortDescription => "trainer for the learnable language detector";

    /// <inheritdoc/>
    protected override IEnumerable<Option> GetToolOptions() => [model, @params, factoryName];

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

        CmdLineUtil.CheckOutputFile("language detector model", modelOutFile);

        LanguageDetectorModel languageDetectorModel;
        try
        {
            LanguageDetectorFactory factory =
                LanguageDetectorFactory.Create(parseResult.GetValue(factoryName));
            languageDetectorModel = LanguageDetectorME.Train(sampleStream!, mlParams, factory);
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

        CmdLineUtil.WriteModel("language detector", modelOutFile, languageDetectorModel);
    }
}
