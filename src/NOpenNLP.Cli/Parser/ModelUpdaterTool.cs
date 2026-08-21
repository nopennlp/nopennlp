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
using NOpenNLP.Tools.Parser;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Cmdline.Parser;

/// <summary>
/// Abstract base class for tools which update the parser model.
/// </summary>
// NOpenNLP: upstream extends AbstractTypedParamTool, which contributes only the format
// handling and the ModelUpdaterParams (a bare TrainingToolParams). AbstractEvaluatorTool
// is this port's equivalent of that layer, so it is the base here; the tool takes no
// training parameters, so AbstractTrainerTool would add nothing.
// Upstream declares this class package-private; C# has no equivalent of Java's default
// access, and its public subclasses cannot be less accessible than their base, so it is
// public here.
public abstract class ModelUpdaterTool : AbstractEvaluatorTool<Parse?>
{
    // NOpenNLP: upstream's ModelUpdaterParams extends TrainingToolParams, so these tools
    // accept -lang and -params as well as -model even though they only read -model. The
    // options are declared here so the accepted argument list and the help match
    // upstream; nothing reads them, as nothing does upstream either.
    private readonly Option<FileInfo> model = ToolParams.ModelForTraining();
    private readonly Option<string> lang = ToolParams.Lang();
    private readonly Option<string?> @params = ToolParams.Params();

    /// <inheritdoc/>
    protected override IEnumerable<Option> GetToolOptions() => [model, @params, lang];

    /// <inheritdoc/>
    public override string GetHelp(string format) =>
        "Usage: " + CLI.Cmd + " " + Name + GetFormatsHelp(format) +
        OptionUsage.CreateUsage(GetToolOptions(), GetStreamFactory(format).Parameters);

    /// <exception cref="IOException">if reading the samples fails</exception>
    protected abstract ParserModel TrainAndUpdate(ParserModel originalModel,
        IObjectStream<Parse?> parseSamples);

    /// <inheritdoc/>
    // NOpenNLP: upstream declares run() final; sealed is the C# counterpart, and it
    // keeps the "rewrite the model file in place" contract with the subclasses.
    protected sealed override void Run(ParseResult parseResult)
    {
        // Load model to be updated
        FileInfo modelFile = parseResult.GetRequiredValue(model);
        ParserModel originalParserModel = new ParserModelLoader().Load(modelFile);

        ParserModel updatedParserModel;
        try
        {
            updatedParserModel = TrainAndUpdate(originalParserModel, sampleStream!);
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

        CmdLineUtil.WriteModel("parser", modelFile, updatedParserModel);
    }
}
