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
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using NOpenNLP.Tools.Cmdline.Tokenizer;
using NOpenNLP.Tools.Parser;
using NOpenNLP.Tools.Tokenize;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Cmdline.Parser;

public sealed class ParserTool : BasicCmdLineTool
{
    /// <inheritdoc/>
    public override string ShortDescription => "performs full syntactic parsing";

    /// <inheritdoc/>
    // NOpenNLP: the text is reproduced verbatim, including upstream's "log-probablities"
    // typo and the trailing space after "sentences", because it is what users see.
    public override string GetHelp() =>
        "Usage: " + CLI.Cmd + " " + Name + " [-bs n -ap n -k n -tk tok_model] model < sentences \n"
            + "-bs n: Use a beam size of n.\n"
            + "-ap f: Advance outcomes in with at least f% of the probability mass.\n"
            + "-k n: Show the top n parses.  This will also display their log-probablities.\n"
            + "-tk tok_model: Use the specified tokenizer model to tokenize the sentences. "
            + "Defaults to a WhitespaceTokenizer.";

    private static readonly Regex untokenizedParenPattern1 = new Regex("([^ ])([({)}])");
    private static readonly Regex untokenizedParenPattern2 = new Regex("([({)}])([^ ])");

    public static Parse[] ParseLine(string line, IParser parser, int numParses) =>
        ParseLine(line, parser, WhitespaceTokenizer.INSTANCE, numParses);

    public static Parse[] ParseLine(string line, IParser parser, ITokenizer tokenizer, int numParses)
    {
        // fix some parens patterns
        line = untokenizedParenPattern1.Replace(line, "$1 $2");
        line = untokenizedParenPattern2.Replace(line, "$1 $2");

        // tokenize
        IList<string> tokens = tokenizer.Tokenize(line);
        string text = string.Join(" ", tokens);

        var p = new Parse(text, new Span(0, text.Length), AbstractBottomUpParser.INC_NODE, 0, 0);
        int start = 0;
        int i = 0;
        foreach (string tok in tokens)
        {
            p.Insert(new Parse(text, new Span(start, start + tok.Length),
                AbstractBottomUpParser.TOK_NODE, 0, i));
            start += tok.Length + 1;
            i++;
        }

        Parse[] parses;
        if (numParses == 1)
        {
            parses = [parser.Parse(p)];
        }
        else
        {
            parses = parser.Parse(p, numParses);
        }

        return parses;
    }

    /// <inheritdoc/>
    public override void Run(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine(GetHelp());
        }
        else
        {
            ParserModel model = new ParserModelLoader().Load(new FileInfo(args[args.Length - 1]));

            int? beamSize = CmdLineUtil.GetIntParameter("-bs", args);
            if (beamSize == null)
            {
                beamSize = AbstractBottomUpParser.defaultBeamSize;
            }

            int? numParses = CmdLineUtil.GetIntParameter("-k", args);
            bool showTopK;
            if (numParses == null)
            {
                numParses = 1;
                showTopK = false;
            }
            else
            {
                showTopK = true;
            }

            double? advancePercentage = CmdLineUtil.GetDoubleParameter("-ap", args);

            if (advancePercentage == null)
            {
                advancePercentage = AbstractBottomUpParser.defaultAdvancePercentage;
            }

            ITokenizer tokenizer = WhitespaceTokenizer.INSTANCE;
            string? tokenizerModelName = CmdLineUtil.GetParameter("-tk", args);
            if (tokenizerModelName != null)
            {
                TokenizerModel tokenizerModel =
                    new TokenizerModelLoader().Load(new FileInfo(tokenizerModelName));
                tokenizer = new TokenizerME(tokenizerModel);
            }

            IParser parser = ParserFactory.Create(model, beamSize.Value, advancePercentage.Value);

            IObjectStream<string?> lineStream;

            // NOpenNLP: upstream leaves perfMon null until inside the try, so an
            // IOException from the stream construction makes the
            // stopAndPrintFinalResult() call below throw a NullPointerException over the
            // real error. Constructing it first keeps the IO error as itself.
            using var perfMon = new PerformanceMonitor(Console.Error, "sent");

            try
            {
                lineStream = new PlainTextByLineStream(new SystemInputStreamFactory(),
                    SystemInputStreamFactory.Encoding);
                perfMon.Start();
                string? line;
                while ((line = lineStream.Read()) != null)
                {
                    if (line.Trim().Length == 0)
                    {
                        Console.WriteLine();
                    }
                    else
                    {
                        Parse[] parses = ParseLine(line, parser, tokenizer, numParses.Value);

                        for (int pi = 0, pn = parses.Length; pi < pn; pi++)
                        {
                            if (showTopK)
                            {
                                // NOpenNLP: Java concatenates the double with
                                // Double.toString, which always renders a decimal point
                                // and uses the invariant format. J2N's "J" format
                                // reproduces that, as elsewhere in the port.
                                Console.Write(pi + " " + J2N.Numerics.Double.ToString(
                                    parses[pi].Prob, "J", CultureInfo.InvariantCulture) + " ");
                            }

                            parses[pi].Show();

                            perfMon.IncrementCounter();
                        }
                    }
                }
            }
            catch (IOException e)
            {
                CmdLineUtil.HandleStdinIoError(e);
            }

            perfMon.StopAndPrintFinalResult();
        }
    }
}
