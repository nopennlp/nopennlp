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
using NOpenNLP.Tools.Langdetect;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Cmdline.Langdetect;

public class LanguageDetectorTool : BasicCmdLineTool
{
    /// <inheritdoc/>
    public override string ShortDescription => "learned language detector";

    /// <inheritdoc/>
    public override string GetHelp() => "Usage: " + CLI.Cmd + " " + Name + " model < documents";

    /// <inheritdoc/>
    public override void Run(string[] args)
    {
        if (0 == args.Length)
        {
            Console.WriteLine(GetHelp());
        }
        else
        {
            LanguageDetectorModel model = new LanguageDetectorModelLoader().Load(new FileInfo(args[0]));

            ILanguageDetector langDetectME = new LanguageDetectorME(model);

            /*
             * moved initialization to the try block to catch new IOException
             */
            IObjectStream<string?> documentStream;

            using var perfMon = new PerformanceMonitor(Console.Error, "doc");
            perfMon.Start();

            try
            {
                documentStream = new ParagraphStream(new PlainTextByLineStream(
                    new SystemInputStreamFactory(), SystemInputStreamFactory.Encoding));
                string? document;
                while ((document = documentStream.Read()) != null)
                {
                    Language lang = langDetectME.PredictLanguage(document);

                    var sample = new LanguageSample(lang, document);
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
