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
/// Parser for the Italian NER training files of the Evalita 2007 and 2009 NER shared tasks.
/// <para/>
/// The data does not contain article boundaries,
/// adaptive data will be cleared for every sentence.
/// <para/>
/// Named Entities are annotated in the IOB2 format (as used in CoNLL 2002 shared task)
/// <para/>
/// The Named Entity tag consists of two parts:
/// <list type="number">
/// <item><description>The IOB2 tag: 'B' (for 'begin') denotes the first token of a
/// Named Entity, I (for 'inside') is used for all other tokens in a
/// Named Entity, and 'O' (for 'outside') is used for all other words;</description></item>
/// <item><description>The Entity type tag: PER (for Person), ORG (for Organization),
/// GPE (for Geo-Political Entity), or LOC (for Location).</description></item>
/// </list>
/// <para/>
/// Each file consists of four columns separated by a blank, containing
/// respectively the token, the Elsnet PoS-tag, the Adige news story to
/// which the token belongs, and the Named Entity tag.
/// <para/>
/// Data can be found on this web site:<br/>
/// http://www.evalita.it
/// <para/>
/// <b>Note:</b> Do not use this class, internal use only!
/// </summary>
public class EvalitaNameSampleStream : ObjectStreamBase<NameSample?>
{
    // NOpenNLP: upstream names this enum LANGUAGE; C# casing makes it Language.
    public enum Language
    {
        IT
    }

    public const int GeneratePersonEntities = 0x01;
    public const int GenerateOrganizationEntities = 0x01 << 1;
    public const int GenerateLocationEntities = 0x01 << 2;
    public const int GenerateGpeEntities = 0x01 << 3;

    public const string DocStart = "-DOCSTART-";

    private readonly Language lang;
    private readonly IObjectStream<string?> lineStream;

    private readonly int types;

    public EvalitaNameSampleStream(Language lang, IObjectStream<string?> lineStream, int types)
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
    public EvalitaNameSampleStream(Language lang, IInputStreamFactory @in, int types)
    {
        this.lang = lang;
        lineStream = new PlainTextByLineStream(@in, Encoding.UTF8);
        this.types = types;
    }

    /// <exception cref="InvalidFormatException">if the tag carries an unknown type</exception>
    private static Span Extract(int begin, int end, string beginTag)
    {
        string type = beginTag[2..] switch
        {
            "PER" => "person",
            "LOC" => "location",
            "GPE" => "gpe",
            "ORG" => "organization",
            var unknown => throw new InvalidFormatException($"Unknown type: {unknown}")
        };

        return new Span(begin, end, type);
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
                if (line.StartsWith(DocStart, StringComparison.Ordinal))
                {
                    isClearAdaptiveData = true;
                    string? emptyLine = lineStream.Read();

                    // NOpenNLP: upstream passes the line straight to StringUtil.isEmpty, which
                    // throws a NullPointerException when -DOCSTART- is the last line of the
                    // stream. Treating end of stream as "not the expected empty line" reports
                    // the same malformed input through the IOException this branch already
                    // throws, rather than through an unrelated exception type.
                    if (emptyLine is null || !StringUtil.IsEmpty(emptyLine))
                        throw new IOException($"Empty line after -DOCSTART- not empty: '{emptyLine}'!");

                    continue;
                }

                string[] fields = line.Split(' ');

                // For Italian: WORD  POS-TAG SC-TAG NE-TAG
                if (Language.IT.Equals(lang) && fields.Length == 4)
                {
                    sentence.Add(fields[0]);
                    tags.Add(fields[3]); // 3 is NE-TAG
                }
                else
                {
                    throw new IOException($"Incorrect number of fields per line for language: '{line}'!");
                }
            }

            // Always clear adaptive data for Italian
            if (Language.IT.Equals(lang))
                isClearAdaptiveData = true;

            if (sentence.Count > 0)
            {
                // convert name tags into spans
                JCG.List<Span> names = [];

                int beginIndex = -1;
                int endIndex = -1;
                for (int i = 0; i < tags.Count; i++)
                {
                    string tag = tags[i];

                    if (tag.EndsWith("PER", StringComparison.Ordinal) && (types & GeneratePersonEntities) == 0)
                        tag = "O";

                    if (tag.EndsWith("ORG", StringComparison.Ordinal) && (types & GenerateOrganizationEntities) == 0)
                        tag = "O";

                    if (tag.EndsWith("LOC", StringComparison.Ordinal) && (types & GenerateLocationEntities) == 0)
                        tag = "O";

                    if (tag.EndsWith("GPE", StringComparison.Ordinal) && (types & GenerateGpeEntities) == 0)
                        tag = "O";

                    if (tag.StartsWith("B-", StringComparison.Ordinal))
                    {
                        if (beginIndex != -1)
                        {
                            names.Add(Extract(beginIndex, endIndex, tags[beginIndex]));
                            beginIndex = -1;
                            endIndex = -1;
                        }

                        beginIndex = i;
                        endIndex = i + 1;
                    }
                    else if (tag.StartsWith("I-", StringComparison.Ordinal))
                    {
                        endIndex++;
                    }
                    else if (tag.Equals("O", StringComparison.Ordinal))
                    {
                        if (beginIndex != -1)
                        {
                            names.Add(Extract(beginIndex, endIndex, tags[beginIndex]));
                            beginIndex = -1;
                            endIndex = -1;
                        }
                    }
                    else
                    {
                        throw new IOException($"Invalid tag: {tag}");
                    }
                }

                // if one span remains, create it here
                if (beginIndex != -1)
                    names.Add(Extract(beginIndex, endIndex, tags[beginIndex]));

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
