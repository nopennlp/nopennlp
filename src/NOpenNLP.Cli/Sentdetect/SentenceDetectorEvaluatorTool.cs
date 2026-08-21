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
using NOpenNLP.Tools.Sentdetect;

namespace NOpenNLP.Tools.Cmdline.Sentdetect;

public sealed class SentenceDetectorEvaluatorTool : AbstractEvaluatorTool<SentenceSample?>
{
    // NOpenNLP: upstream's EvalToolParams interface extends EvaluatorParams; the
    // options it declares are created here instead.
    private readonly Option<FileInfo> model = ToolParams.ModelForEvaluation();
    private readonly Option<string?> misclassified = ToolParams.Misclassified();

    /// <inheritdoc/>
    protected override IEnumerable<Option> GetToolOptions() => [model, misclassified];

    /// <inheritdoc/>
    public override string ShortDescription => "evaluator for the learnable sentence detector";

    /// <inheritdoc/>
    public override string GetHelp(string format) =>
        "Usage: " + CLI.Cmd + " " + Name + GetFormatsHelp(format)
            + OptionUsage.CreateUsage(GetToolOptions(), GetStreamFactory(format).Parameters);

    /// <inheritdoc/>
    protected override void Run(ParseResult parseResult)
    {
        SentenceModel model = new SentenceModelLoader().Load(parseResult.GetValue(this.model)!);

        ISentenceDetectorEvaluationMonitor? errorListener = null;
        if (ToolParams.JavaBooleanValue(parseResult.GetValue(misclassified)))
        {
            errorListener = new SentenceEvaluationErrorListener();
        }

        var evaluator = new SentenceDetectorEvaluator(
            new SentenceDetectorME(model), errorListener);

        Console.Write("Evaluating ... ");
        try
        {
            evaluator.Evaluate(sampleStream!);
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

        // NOpenNLP: upstream prints this one "done" to System.err, unlike the other
        // evaluator tools, which print it to System.out. It is reproduced as it stands.
        Console.Error.WriteLine("done");

        Console.WriteLine();

        Console.WriteLine(evaluator.FMeasure);
    }
}
