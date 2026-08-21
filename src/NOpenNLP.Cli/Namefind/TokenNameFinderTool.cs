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
using System.IO;
using NOpenNLP.Tools.Namefind;
using NOpenNLP.Tools.Tokenize;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Cmdline.Namefind;

public sealed class TokenNameFinderTool : BasicCmdLineTool
{
    /// <inheritdoc/>
    public override string ShortDescription => "learnable name finder";

    /// <inheritdoc/>
    public override string GetHelp() =>
        "Usage: " + CLI.Cmd + " " + Name + " model1 model2 ... modelN < sentences";

    /// <inheritdoc/>
    public override void Run(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine(GetHelp());
        }
        else
        {
            NameFinderME[] nameFinders = new NameFinderME[args.Length];

            for (int i = 0; i < nameFinders.Length; i++)
            {
                TokenNameFinderModel model = new TokenNameFinderModelLoader().Load(new FileInfo(args[i]));
                nameFinders[i] = new NameFinderME(model);
            }

            IObjectStream<string?> untokenizedLineStream;
            using var perfMon = new PerformanceMonitor(Console.Error, "sent");
            perfMon.Start();

            try
            {
                untokenizedLineStream = new PlainTextByLineStream(
                    new SystemInputStreamFactory(), SystemInputStreamFactory.Encoding);
                string? line;
                while ((line = untokenizedLineStream.Read()) != null)
                {
                    string[] whitespaceTokenizerLine = WhitespaceTokenizer.INSTANCE.Tokenize(line);

                    // A new line indicates a new document,
                    // adaptive data must be cleared for a new document

                    if (whitespaceTokenizerLine.Length == 0)
                    {
                        foreach (NameFinderME nameFinder in nameFinders)
                        {
                            nameFinder.ClearAdaptiveData();
                        }
                    }

                    List<Span> names = [];

                    foreach (ITokenNameFinder nameFinder in nameFinders)
                    {
                        names.AddRange(nameFinder.Find(whitespaceTokenizerLine));
                    }

                    // Simple way to drop intersecting spans, otherwise the
                    // NameSample is invalid
                    Span[] reducedNames = NameFinderME.DropOverlappingSpans([.. names]);

                    var nameSample = new NameSample(whitespaceTokenizerLine, reducedNames, false);

                    Console.WriteLine(nameSample);

                    perfMon.IncrementCounter();
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
