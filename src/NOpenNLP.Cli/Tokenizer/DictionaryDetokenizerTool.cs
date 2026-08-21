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
using NOpenNLP.Tools.Tokenize;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Cmdline.Tokenizer;

// NOpenNLP: upstream does not override getShortDescription(), so this tool really does
// show an empty description in the usage listing. It is left as inherited rather than
// invented.
public sealed class DictionaryDetokenizerTool : BasicCmdLineTool
{
    /// <inheritdoc/>
    public override string GetHelp() => "Usage: " + CLI.Cmd + " " + Name + " detokenizerDictionary";

    /// <inheritdoc/>
    public override void Run(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine(GetHelp());
        }
        else
        {
            try
            {
                IDetokenizer detokenizer = new DictionaryDetokenizer(
                    new DetokenizationDictionaryLoader().Load(new FileInfo(args[0])));

                using IObjectStream<string?> tokenizedLineStream =
                    new PlainTextByLineStream(new SystemInputStreamFactory(),
                        SystemInputStreamFactory.Encoding);

                using var perfMon = new PerformanceMonitor(Console.Error, "sent");
                perfMon.Start();

                string? tokenizedLine;
                while ((tokenizedLine = tokenizedLineStream.Read()) != null)
                {
                    // white space tokenize line
                    string[] tokens = WhitespaceTokenizer.INSTANCE.Tokenize(tokenizedLine);

                    Console.WriteLine(detokenizer.Detokenize(tokens, null));

                    perfMon.IncrementCounter();
                }

                perfMon.StopAndPrintFinalResult();
            }
            catch (IOException e)
            {
                CmdLineUtil.HandleStdinIoError(e);
            }
        }
    }
}
