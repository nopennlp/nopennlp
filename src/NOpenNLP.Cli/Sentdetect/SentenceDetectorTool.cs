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
using NOpenNLP.Tools.Sentdetect;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Cmdline.Sentdetect;

/// <summary>
/// A sentence detector which uses a maxent model to predict the sentences.
/// </summary>
public sealed class SentenceDetectorTool : BasicCmdLineTool
{
    /// <inheritdoc/>
    public override string ShortDescription => "learnable sentence detector";

    /// <inheritdoc/>
    public override string GetHelp() => "Usage: " + CLI.Cmd + " " + Name + " model < sentences";

    /// <summary>
    /// Perform sentence detection the input stream.
    /// <para/>
    /// A newline will be treated as a paragraph boundary.
    /// </summary>
    public override void Run(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine(GetHelp());
        }
        else
        {
            SentenceModel model = new SentenceModelLoader().Load(new FileInfo(args[0]));

            var sdetector = new SentenceDetectorME(model);

            using var perfMon = new PerformanceMonitor(Console.Error, "sent");
            perfMon.Start();

            try
            {
                using IObjectStream<string?> paraStream = new ParagraphStream(
                    new PlainTextByLineStream(new SystemInputStreamFactory(),
                        SystemInputStreamFactory.Encoding));

                string? para;
                while ((para = paraStream.Read()) != null)
                {
                    string[] sents = sdetector.SentDetect(para);
                    foreach (string sentence in sents)
                    {
                        Console.WriteLine(sentence);
                    }

                    perfMon.IncrementCounter(sents.Length);

                    Console.WriteLine();
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
