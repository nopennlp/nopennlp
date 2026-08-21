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
using System.Globalization;
using System.IO;
using NOpenNLP.Tools.Languagemodel;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Cmdline.Languagemodel;

/// <summary>
/// Command line tool for <see cref="NGramLanguageModel"/>.
/// </summary>
public class NGramLanguageModelTool : BasicCmdLineTool
{
    /// <inheritdoc/>
    public override string ShortDescription =>
        "gives the probability and most probable next token(s) of a sequence of tokens in a " +
        "language model";

    /// <inheritdoc/>
    public override void Run(string[] args)
    {
        var lmFile = new FileInfo(args[0]);
        try
        {
            using Stream stream = lmFile.OpenRead();
            var nGramLanguageModel = new NGramLanguageModel(stream);

            IObjectStream<string?> lineStream;

            // NOpenNLP: upstream leaves perfMon null until inside the try, so an
            // IOException from the stream construction makes the
            // stopAndPrintFinalResult() call below throw a NullPointerException over the
            // real error. Constructing it first keeps the IO error as itself.
            using var perfMon = new PerformanceMonitor(Console.Error, "nglm");

            try
            {
                lineStream = new PlainTextByLineStream(new SystemInputStreamFactory(),
                    SystemInputStreamFactory.Encoding);
                perfMon.Start();
                string? line;
                while ((line = lineStream.Read()) != null)
                {
                    double probability;
                    string[]? predicted;
                    // TODO : use a Tokenizer here
                    // NOpenNLP: upstream's String.split(" ") discards trailing empty
                    // strings, so a line ending in a space yields no empty final token.
                    // string.Split would keep one and score it as a real token.
                    string[] tokens = StringUtil.SplitDroppingTrailingEmpty(line, ' ');
                    try
                    {
                        probability = nGramLanguageModel.CalculateProbability(tokens);
                        predicted = nGramLanguageModel.PredictNextTokens(tokens);
                    }
                    catch (Exception e)
                    {
                        Console.Error.WriteLine("Error:" + e.Message);
                        Console.Error.WriteLine(line);
                        continue;
                    }

                    // NOpenNLP: Java's Arrays.toString renders "[a, b]" and its double
                    // concatenation always shows a decimal point; both are reproduced so
                    // the output line reads the same. A null from predictNextTokens
                    // renders as "null", which is what Arrays.toString(null) prints.
                    Console.WriteLine(ToStringJava(tokens) + " -> prob:"
                        + J2N.Numerics.Double.ToString(probability, "J", CultureInfo.InvariantCulture)
                        + ", " + "next:" + ToStringJava(predicted));

                    perfMon.IncrementCounter();
                }
            }
            catch (IOException e)
            {
                CmdLineUtil.HandleStdinIoError(e);
            }

            perfMon.StopAndPrintFinalResult();
        }
        catch (IOException e)
        {
            Console.Error.WriteLine(e.Message);
        }
        // do nothing
    }

    // NOpenNLP: stands in for java.util.Arrays.toString(Object[]).
    private static string ToStringJava(string[]? array) =>
        array is null ? "null" : "[" + string.Join(", ", array) + "]";

    /// <inheritdoc/>
    public override string GetHelp() => "Usage: " + CLI.Cmd + " " + Name + " model";
}
