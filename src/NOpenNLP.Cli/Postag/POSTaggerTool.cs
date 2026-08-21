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
using NOpenNLP.Tools.Postag;
using NOpenNLP.Tools.Tokenize;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Cmdline.Postag;

public sealed class POSTaggerTool : BasicCmdLineTool
{
    /// <inheritdoc/>
    public override string ShortDescription => "learnable part of speech tagger";

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
            POSModel model = new POSModelLoader().Load(new FileInfo(args[0]));

            var tagger = new POSTaggerME(model);

            IObjectStream<string?> lineStream;

            // NOpenNLP: upstream declares perfMon null before the try and calls
            // stopAndPrintFinalResult() after the catch, so an IOException from the
            // stream construction leaves it null and that call throws a
            // NullPointerException over the real error. Constructing it first keeps the
            // final report on the success path and lets the IO error surface as itself.
            using var perfMon = new PerformanceMonitor(Console.Error, "sent");

            try
            {
                lineStream = new PlainTextByLineStream(new SystemInputStreamFactory(),
                    SystemInputStreamFactory.Encoding);
                perfMon.Start();
                string? line;
                while ((line = lineStream.Read()) != null)
                {
                    string[] whitespaceTokenizerLine = WhitespaceTokenizer.INSTANCE.Tokenize(line);
                    string[] tags = tagger.Tag(whitespaceTokenizerLine);

                    var sample = new POSSample(whitespaceTokenizerLine, tags);
                    Console.WriteLine(sample);

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
