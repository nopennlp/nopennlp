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
using System.Text;
using NOpenNLP.Tools.Namefind;
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Formats;

/// <summary>
/// An import stream which can parse the CONLL03 data.
/// </summary>
public class Conll03NameSampleStream : ObjectStreamBase<NameSample?>
{
    // NOpenNLP: upstream names this enum LANGUAGE; C# casing makes it Language.
    public enum Language
    {
        EN,
        DE
    }

    private readonly Language lang;
    private readonly IObjectStream<string?> lineStream;

    private readonly int types;

    /// <param name="lang">the language of the CONLL 03 data</param>
    /// <param name="lineStream">an Object Stream over the lines in the CONLL 03 data file</param>
    /// <param name="types">the entity types to include in the Name Sample object stream</param>
    public Conll03NameSampleStream(Language lang, IObjectStream<string?> lineStream, int types)
    {
        this.lang = lang;
        this.lineStream = lineStream;
        this.types = types;
    }

    /// <exception cref="IOException">if there is an error during reading</exception>
    // NOpenNLP: upstream also calls System.setOut to reconfigure stdout to UTF-8 here,
    // and wraps the constructor in a catch for UnsupportedEncodingException that its own
    // comment notes can never happen. Reconfiguring the process's console from a corpus
    // reader is an incidental side effect rather than part of parsing, so it is omitted.
    public Conll03NameSampleStream(Language lang, IInputStreamFactory @in, int types)
    {
        this.lang = lang;
        lineStream = new PlainTextByLineStream(@in, Encoding.UTF8);
        this.types = types;
    }

    /// <inheritdoc/>
    /// <exception cref="IOException">if there is an error during reading</exception>
    public override NameSample? Read()
    {
        // NOpenNLP: converted recursion to iteration
        while (true)
        {
            JCG.List<string> sentence = [];
            JCG.List<string> tags = [];

            bool isClearAdaptiveData = false;

            // Empty line indicates end of sentence

            string? line;
            while ((line = lineStream.Read()) != null && !StringUtil.IsEmpty(line))
            {
                if (line.StartsWith(Conll02NameSampleStream.DocStart, StringComparison.Ordinal))
                {
                    isClearAdaptiveData = true;
                    string? emptyLine = lineStream.Read();

                    // NOpenNLP: the line after -DOCSTART- is null when the stream ends there.
                    // Upstream passes it straight to StringUtil.isEmpty, which dereferences it
                    // and throws NullPointerException; IsEmpty would likewise throw here. A
                    // truncated file is a malformed corpus rather than a programming error, so
                    // it reports the IOException this branch already raises for a non-empty
                    // line, which names the offending file position.
                    if (emptyLine is null || !StringUtil.IsEmpty(emptyLine))
                        throw new IOException($"Empty line after -DOCSTART- not empty: '{emptyLine}'!");

                    continue;
                }

                string[] fields = line.Split(' ');

                // For English: WORD  POS-TAG SC-TAG NE-TAG
                if (Language.EN.Equals(lang) && fields.Length == 4)
                {
                    sentence.Add(fields[0]);
                    tags.Add(fields[3]); // 3 is NE-TAG
                }
                // For German: WORD  LEMA-TAG POS-TAG SC-TAG NE-TAG
                else if (Language.DE.Equals(lang) && fields.Length == 5)
                {
                    sentence.Add(fields[0]);
                    tags.Add(fields[4]); // 4 is NE-TAG
                }
                else
                {
                    throw new IOException($"Incorrect number of fields per line for language: '{line}'!");
                }
            }

            if (sentence.Count > 0)
            {
                // convert name tags into spans
                JCG.List<Span> names = [];

                int beginIndex = -1;
                int endIndex = -1;
                for (int i = 0; i < tags.Count; i++)
                {
                    string tag = tags[i];

                    if (tag.EndsWith("PER", StringComparison.Ordinal) &&
                        (types & Conll02NameSampleStream.GeneratePersonEntities) == 0)
                        tag = "O";

                    if (tag.EndsWith("ORG", StringComparison.Ordinal) &&
                        (types & Conll02NameSampleStream.GenerateOrganizationEntities) == 0)
                        tag = "O";

                    if (tag.EndsWith("LOC", StringComparison.Ordinal) &&
                        (types & Conll02NameSampleStream.GenerateLocationEntities) == 0)
                        tag = "O";

                    if (tag.EndsWith("MISC", StringComparison.Ordinal) &&
                        (types & Conll02NameSampleStream.GenerateMiscEntities) == 0)
                        tag = "O";

                    if (tag.Equals("O", StringComparison.Ordinal))
                    {
                        // O means we don't have anything this round.
                        if (beginIndex != -1)
                        {
                            names.Add(Conll02NameSampleStream.Extract(beginIndex, endIndex, tags[beginIndex]));
                            beginIndex = -1;
                            endIndex = -1;
                        }
                    }
                    else if (tag.StartsWith("B-", StringComparison.Ordinal))
                    {
                        // B- prefix means we have two same entities next to each other
                        if (beginIndex != -1)
                        {
                            names.Add(Conll02NameSampleStream.Extract(beginIndex, endIndex, tags[beginIndex]));
                        }
                        beginIndex = i;
                        endIndex = i + 1;
                    }
                    else if (tag.StartsWith("I-", StringComparison.Ordinal))
                    {
                        // I- starts or continues a current name entity
                        if (beginIndex == -1)
                        {
                            beginIndex = i;
                            endIndex = i + 1;
                        }
                        else if (!tag.EndsWith(tags[beginIndex][1..], StringComparison.Ordinal))
                        {
                            // we have a new tag type following a tagged word series
                            // also may not have the same I- starting the previous!
                            names.Add(Conll02NameSampleStream.Extract(beginIndex, endIndex, tags[beginIndex]));
                            beginIndex = i;
                            endIndex = i + 1;
                        }
                        else
                        {
                            endIndex++;
                        }
                    }
                    else
                    {
                        throw new IOException($"Invalid tag: {tag}");
                    }
                }

                // if one span remains, create it here
                if (beginIndex != -1)
                    names.Add(Conll02NameSampleStream.Extract(beginIndex, endIndex, tags[beginIndex]));

                return new NameSample([.. sentence], [.. names], isClearAdaptiveData);
            }
            else if (line != null)
            {
                // Just filter out empty events, if two lines in a row are empty
                continue;
            }
            else
            {
                // source stream is not returning anymore lines
                return null;
            }
        }
    }

    /// <inheritdoc/>
    /// <exception cref="IOException">if there is an error during resetting the stream</exception>
    public override void Reset() => lineStream.Reset();

    protected override void Dispose(bool disposing) => lineStream.Dispose();
}
