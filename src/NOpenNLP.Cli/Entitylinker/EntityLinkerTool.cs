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
using System.Text;
using NOpenNLP.Tools.Entitylinker;
using NOpenNLP.Tools.Formats;
using NOpenNLP.Tools.Namefind;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Cmdline.Entitylinker;

public class EntityLinkerTool : BasicCmdLineTool
{
    /// <inheritdoc/>
    public override string ShortDescription => "links an entity to an external data set";

    /// <inheritdoc/>
    public override void Run(string[] args)
    {
        if (0 == args.Length)
        {
            Console.WriteLine(GetHelp());
        }
        else
        {
            // TODO: Ask Mark if we can remove the type, the user knows upfront if he tries
            // to link place names or company mentions ...
            string entityType = "location";

            // Load the properties, they should contain everything that is necessary to instantiate
            // the component

            // TODO: Entity Linker Properties constructor should not duplicate code
            EntityLinkerProperties properties;
            try
            {
                properties = new EntityLinkerProperties(new FileInfo(args[0]).FullName);
            }
            catch (IOException)
            {
                throw new TerminateToolException(-1, "Failed to load the properties file!");
            }

            // TODO: It should not just throw Exception.

            // NOpenNLP: Java loads the linker against the raw EntityLinker type and calls
            // find() on it. C# has no raw generic type, so EntityLinkerFactory returns the
            // non-generic IEntityLinker and the span type the tool needs -- the default
            // documented on IEntityLinker<T> -- is recovered with a cast. A linker built
            // over a different span type fails here rather than at the find() call, with
            // the same message and exit code upstream uses for a linker it cannot
            // instantiate.
            IEntityLinker<LinkedSpan<BaseLink>> entityLinker;
            try
            {
                entityLinker = (IEntityLinker<LinkedSpan<BaseLink>>)
                    EntityLinkerFactory.GetLinker(entityType, properties);
            }
            catch (Exception e)
            {
                throw new TerminateToolException(-1,
                    "Failed to instantiate the Entity Linker: " + e.Message);
            }

            using var perfMon = new PerformanceMonitor(Console.Error, "sent");
            perfMon.Start();

            try
            {
                using IObjectStream<string?> untokenizedLineStream = new PlainTextByLineStream(
                    new SystemInputStreamFactory(), SystemInputStreamFactory.Encoding);

                List<NameSample> document = [];

                string? line;
                while ((line = untokenizedLineStream.Read()) != null)
                {
                    if (line.Trim().Length == 0)
                    {
                        // Run entity linker ... and output result ...

                        var text = new StringBuilder();
                        Span[] sentences = new Span[document.Count];
                        Span[][] tokensBySentence = new Span[document.Count][];
                        Span[][] namesBySentence = new Span[document.Count][];

                        for (int i = 0; i < document.Count; i++)
                        {
                            NameSample sample = document[i];

                            namesBySentence[i] = sample.Names;

                            int sentenceBegin = text.Length;

                            Span[] tokens = new Span[sample.Sentence.Length];

                            // for all tokens
                            for (int ti = 0; ti < sample.Sentence.Length; ti++)
                            {
                                int tokenBegin = text.Length;
                                text.Append(sample.Sentence[ti]);
                                text.Append(' ');
                                tokens[ti] = new Span(tokenBegin, text.Length);
                            }

                            tokensBySentence[i] = tokens;

                            sentences[i] = new Span(sentenceBegin, text.Length);
                            text.Append('\n');
                        }

                        IList<LinkedSpan<BaseLink>> linkedSpans = entityLinker.Find(
                            text.ToString(), sentences, tokensBySentence, namesBySentence);

                        foreach (LinkedSpan<BaseLink> linkedSpan in linkedSpans)
                        {
                            Console.WriteLine(linkedSpan);
                        }

                        perfMon.IncrementCounter(document.Count);
                        document.Clear();
                    }
                    else
                    {
                        document.Add(NameSample.Parse(line, false));
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

    /// <inheritdoc/>
    public override string GetHelp() => "Usage: " + CLI.Cmd + " " + Name + " model < sentences";
}
