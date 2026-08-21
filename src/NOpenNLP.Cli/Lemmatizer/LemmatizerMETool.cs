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
using System.IO;
using NOpenNLP.Tools.Lemmatizer;
using NOpenNLP.Tools.Postag;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Cmdline.Lemmatizer;

public class LemmatizerMETool : BasicCmdLineTool
{
    /// <inheritdoc/>
    public override string ShortDescription => "learnable lemmatizer";

    /// <inheritdoc/>
    public override string GetHelp() => "Usage: " + CLI.Cmd + " " + Name + " model < sentences";

    /// <inheritdoc/>
    public override void Run(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine(GetHelp());
        }
        else
        {
            LemmatizerModel model = new LemmatizerModelLoader().Load(new FileInfo(args[0]));

            var lemmatizer = new LemmatizerME(model);

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
                    POSSample posSample;
                    try
                    {
                        posSample = POSSample.Parse(line);
                    }
                    catch (InvalidFormatException)
                    {
                        Console.Error.WriteLine("Invalid format:");
                        Console.Error.WriteLine(line);
                        continue;
                    }

                    string[] lemmas = lemmatizer.Lemmatize(posSample.Sentence, posSample.Tags);

                    Console.WriteLine(new LemmaSample(posSample.Sentence, posSample.Tags, lemmas));

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
