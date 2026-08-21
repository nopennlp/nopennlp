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
using NOpenNLP.Tools.Tokenize;
using NOpenNLP.Tools.Util.Eval;
using NOpenNLP.Tools.Util.Model;
using OpenNlpDictionary = NOpenNLP.Tools.Dictionary.Dictionary;

namespace NOpenNLP.Tools.Cmdline.Tokenizer;

public sealed class TokenizerCrossValidatorTool : AbstractCrossValidatorTool<TokenSample?>
{
    private readonly Option<int> folds = ToolParams.Folds();
    private readonly Option<string?> misclassified = ToolParams.Misclassified();
    private readonly Option<bool> alphaNumOpt = TrainingParams.AlphaNumOpt();
    private readonly Option<FileInfo?> abbDict = TrainingParams.AbbDict();
    private readonly Option<string?> factoryName = TrainingParams.Factory();
    private readonly Option<string> lang = ToolParams.Lang();
    private readonly Option<string?> @params = ToolParams.Params();

    /// <inheritdoc/>
    public override string ShortDescription => "K-fold cross validator for the learnable tokenizer";

    /// <inheritdoc/>
    protected override IEnumerable<Option> GetToolOptions() =>
        [folds, misclassified, alphaNumOpt, abbDict, factoryName, lang, @params];

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

        TokenizerCrossValidator validator;

        ITokenizerEvaluationMonitor? listener = null;
        if (ToolParams.JavaBooleanValue(parseResult.GetValue(misclassified)))
        {
            listener = new TokenEvaluationErrorListener();
        }

        try
        {
            OpenNlpDictionary? dict = TokenizerTrainerTool.LoadDict(parseResult.GetValue(abbDict));

            TokenizerFactory? tokFactory = TokenizerFactory.Create(
                parseResult.GetValue(factoryName), parseResult.GetRequiredValue(lang), dict,
                parseResult.GetRequiredValue(alphaNumOpt), null!);
            validator = new TokenizerCrossValidator(mlParams, tokFactory!, listener);

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

        FMeasure result = validator.FMeasure;

        Console.WriteLine(result);
    }
}
