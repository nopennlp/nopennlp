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
using NOpenNLP.Tools.Ml;
using NOpenNLP.Tools.Parser;
using NOpenNLP.Tools.Util;
using NOpenNLP.Tools.Util.Ext;
using NOpenNLP.Tools.Util.Model;
using ChunkingParser = NOpenNLP.Tools.Parser.Chunking.Parser;
using OpenNlpDictionary = NOpenNLP.Tools.Dictionary.Dictionary;
using TreeinsertParser = NOpenNLP.Tools.Parser.Treeinsert.Parser;

namespace NOpenNLP.Tools.Cmdline.Parser;

public sealed class ParserTrainerTool : AbstractTrainerTool<Parse?>
{
    private readonly Option<string> parserType = TrainingParams.ParserType();
    private readonly Option<string?> headRulesSerializerImpl = TrainingParams.HeadRulesSerializerImpl();
    private readonly Option<FileInfo> headRules = TrainingParams.HeadRules();
    private readonly Option<bool> fun = TrainingParams.Fun();
    private readonly Option<string> lang = ToolParams.Lang();
    private readonly Option<string?> @params = ToolParams.Params();
    private readonly Option<FileInfo> model = ToolParams.ModelForTraining();

    // NOpenNLP: upstream's TrainerToolParams extends EncodingParameter, so -encoding is
    // accepted even for a format that does not declare it (the OntoNotes parse format is
    // one). It is declared here for the same reason. The tool never reads it -- upstream
    // does not either -- and for a format that does declare -encoding the format's own
    // option is the one that carries the value.
    private readonly Option<string> encoding = ToolParams.Encoding();

    /// <inheritdoc/>
    public override string ShortDescription => "trains the learnable parser";

    /// <inheritdoc/>
    protected override IEnumerable<Option> GetToolOptions() =>
        [parserType, headRulesSerializerImpl, headRules, fun, lang, @params, model, encoding];

    /// <inheritdoc/>
    public override string GetHelp(string format) =>
        "Usage: " + CLI.Cmd + " " + Name + GetFormatsHelp(format) +
        OptionUsage.CreateUsage(GetToolOptions(), GetStreamFactory(format).Parameters);

    internal static OpenNlpDictionary? BuildDictionary(IObjectStream<Parse?> parseSamples,
        IHeadRules rules, int cutoff)
    {
        Console.Error.Write("Building dictionary ...");

        OpenNlpDictionary? mdict;
        try
        {
            mdict = AbstractBottomUpParser.BuildDictionary(parseSamples, rules, cutoff);
        }
        catch (IOException e)
        {
            Console.Error.WriteLine("Error while building dictionary: " + e.Message);
            mdict = null;
        }

        Console.Error.WriteLine("done");

        return mdict;
    }

    internal static ParserType? ParseParserType(string? typeAsString)
    {
        ParserType? type = null;
        if (typeAsString != null && typeAsString.Length > 0)
        {
            type = ParserTypeExtensions.Parse(typeAsString);
            if (type == null)
            {
                throw new TerminateToolException(1, "ParserType training parameter '" + typeAsString +
                    "' is invalid!");
            }
        }

        return type;
    }

    // NOpenNLP: upstream misspells this "creaeHeadRules"; the typo is not reproduced
    // because the method is not part of the user-facing contract, unlike the option
    // names and help text.
    /// <exception cref="IOException">if the head rules cannot be read</exception>
    internal static IHeadRules CreateHeadRules(string? serializerImpl, string language,
        FileInfo headRulesFile)
    {
        IArtifactSerializer headRulesSerializer;

        if (serializerImpl != null)
        {
            headRulesSerializer =
                ExtensionLoader.InstantiateExtension<IArtifactSerializer>(serializerImpl)!;
        }
        else
        {
            if ("en".Equals(language, StringComparison.Ordinal)
                || "eng".Equals(language, StringComparison.Ordinal))
            {
                headRulesSerializer = new Tools.Parser.Lang.En.HeadRules.HeadRulesSerializer();
            }
            else if ("es".Equals(language, StringComparison.Ordinal)
                || "spa".Equals(language, StringComparison.Ordinal))
            {
                headRulesSerializer =
                    new Tools.Parser.Lang.Es.AncoraSpanishHeadRules.HeadRulesSerializer();
            }
            else
            {
                // default for now, this case should probably cause an error ...
                headRulesSerializer = new Tools.Parser.Lang.En.HeadRules.HeadRulesSerializer();
            }
        }

        using Stream headRulesIn = headRulesFile.OpenRead();
        object? headRulesObject = headRulesSerializer.Create(headRulesIn);

        if (headRulesObject is IHeadRules rules)
        {
            return rules;
        }
        else
        {
            throw new TerminateToolException(-1,
                "HeadRules Artifact Serializer must create an object of type HeadRules!");
        }
    }

    // TODO: Add param to train tree insert parser
    /// <inheritdoc/>
    protected override void Run(ParseResult parseResult)
    {
        mlParams = CmdLineUtil.LoadTrainingParameters(parseResult.GetValue(@params), true);

        if (mlParams != null)
        {
            if (!TrainerFactory.IsValid(mlParams.GetParameters("build")))
            {
                throw new TerminateToolException(1, "Build training parameters are invalid!");
            }

            if (!TrainerFactory.IsValid(mlParams.GetParameters("check")))
            {
                throw new TerminateToolException(1, "Check training parameters are invalid!");
            }

            if (!TrainerFactory.IsValid(mlParams.GetParameters("attach")))
            {
                throw new TerminateToolException(1, "Attach training parameters are invalid!");
            }

            if (!TrainerFactory.IsValid(mlParams.GetParameters("tagger")))
            {
                throw new TerminateToolException(1, "Tagger training parameters are invalid!");
            }

            if (!TrainerFactory.IsValid(mlParams.GetParameters("chunker")))
            {
                throw new TerminateToolException(1, "Chunker training parameters are invalid!");
            }
        }

        if (mlParams == null)
        {
            mlParams = ModelUtil.CreateDefaultTrainingParameters();
        }

        FileInfo modelOutFile = parseResult.GetRequiredValue(model);
        CmdLineUtil.CheckOutputFile("parser model", modelOutFile);

        ParserModel parserModel;
        try
        {
            IHeadRules rules = CreateHeadRules(parseResult.GetValue(headRulesSerializerImpl),
                parseResult.GetRequiredValue(lang), parseResult.GetRequiredValue(headRules));

            ParserType? type = ParseParserType(parseResult.GetValue(parserType));
            if (parseResult.GetValue(fun))
            {
                Parse.UseFunctionTags(true);
            }

            if (ParserType.CHUNKING == type)
            {
                parserModel = ChunkingParser.Train(parseResult.GetRequiredValue(lang),
                    sampleStream!, rules, mlParams);
            }
            else if (ParserType.TREEINSERT == type)
            {
                parserModel = TreeinsertParser.Train(parseResult.GetRequiredValue(lang),
                    sampleStream!, rules, mlParams);
            }
            else
            {
                throw new InvalidOperationException();
            }
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

        CmdLineUtil.WriteModel("parser", modelOutFile, parserModel);
    }
}
