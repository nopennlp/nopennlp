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

internal sealed class CommandLineTokenizer(ITokenizer tokenizer)
{
    private readonly ITokenizer tokenizer = tokenizer;

    internal void Process()
    {
        // NOpenNLP: upstream declares perfMon outside the try and calls
        // stopAndPrintFinalResult() after the catch, so an IOException from the stream
        // construction leaves it null and the call throws NullPointerException over the
        // real error. Constructing it first keeps the final report on the success path
        // and lets the IO error surface as itself.
        using var perfMon = new PerformanceMonitor(Console.Error, "sent");

        try
        {
            IObjectStream<string?> untokenizedLineStream =
                new PlainTextByLineStream(new SystemInputStreamFactory(),
                    SystemInputStreamFactory.Encoding);

            IObjectStream<string?> tokenizedLineStream = new WhitespaceTokenStream(
                new TokenizerStream(tokenizer, untokenizedLineStream));

            perfMon.Start();

            string? tokenizedLine;
            while ((tokenizedLine = tokenizedLineStream.Read()) != null)
            {
                Console.WriteLine(tokenizedLine);
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
