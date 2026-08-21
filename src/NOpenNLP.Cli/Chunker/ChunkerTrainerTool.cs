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
using NOpenNLP.Tools.Chunker;
using NOpenNLP.Tools.Util.Model;

namespace NOpenNLP.Tools.Cmdline.Chunker;

public class ChunkerTrainerTool : AbstractTrainerTool<ChunkSample?>
{
    private readonly Option<string?> factoryName = TrainingParams.Factory();
    private readonly Option<string> lang = ToolParams.Lang();
    private readonly Option<string?> @params = ToolParams.Params();
    private readonly Option<FileInfo> model = ToolParams.ModelForTraining();

    /// <inheritdoc/>
    public override string Name => "ChunkerTrainerME";

    /// <inheritdoc/>
    public override string ShortDescription => "trainer for the learnable chunker";

    /// <inheritdoc/>
    protected override IEnumerable<Option> GetToolOptions() => [factoryName, lang, @params, model];

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

        // NOpenNLP: upstream names this "sentence detector model" here, which is a
        // copy-paste slip -- the model is the chunker's. It is reproduced because the
        // name appears in the error message the user sees when the file is unwritable.
        CmdLineUtil.CheckOutputFile("sentence detector model", modelOutFile);

        ChunkerModel chunkerModel;
        try
        {
            ChunkerFactory chunkerFactory = ChunkerFactory.Create(parseResult.GetValue(factoryName));
            chunkerModel = ChunkerME.Train(parseResult.GetRequiredValue(lang), sampleStream!,
                mlParams, chunkerFactory);
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

        CmdLineUtil.WriteModel("chunker", modelOutFile, chunkerModel);
    }
}
