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
/// Parser for the dutch and spanish ner training files of the CONLL 2002 shared task.
/// <para/>
/// The dutch data has a -DOCSTART- tag to mark article boundaries,
/// adaptive data in the feature generators will be cleared before every article.<br/>
/// The spanish data does not contain article boundaries,
/// adaptive data will be cleared for every sentence.
/// <para/>
/// The data contains four named entity types: Person, Organization, Location and Misc.<br/>
/// <para/>
/// Data can be found on this web site:<br/>
/// http://www.cnts.ua.ac.be/conll2002/ner/
/// <para/>
/// <b>Note:</b> Do not use this class, internal use only!
/// </summary>
public class Conll02NameSampleStream : ObjectStreamBase<NameSample?>
{
    // NOpenNLP: upstream names this enum LANGUAGE; C# casing makes it Language.
    public enum Language
    {
        NLD,
        SPA
    }

    public const int GeneratePersonEntities = 0x01;
    public const int GenerateOrganizationEntities = 0x01 << 1;
    public const int GenerateLocationEntities = 0x01 << 2;
    public const int GenerateMiscEntities = 0x01 << 3;

    public const string DocStart = "-DOCSTART-";

    private readonly Language lang;
    private readonly IObjectStream<string?> lineStream;

    private readonly int types;

    public Conll02NameSampleStream(Language lang, IObjectStream<string?> lineStream, int types)
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
    public Conll02NameSampleStream(Language lang, IInputStreamFactory @in, int types)
    {
        this.lang = lang;
        lineStream = new PlainTextByLineStream(@in, Encoding.UTF8);
        this.types = types;
    }

    /// <exception cref="InvalidFormatException">if the tag carries an unknown type</exception>
    internal static Span Extract(int begin, int end, string beginTag)
    {
        string type = beginTag[2..] switch
        {
            "PER" => "person",
            "LOC" => "location",
            "MISC" => "misc",
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
                if (Language.NLD.Equals(lang) && line.StartsWith(DocStart, StringComparison.Ordinal))
                {
                    isClearAdaptiveData = true;
                    continue;
                }

                string[] fields = line.Split(' ');

                if (fields.Length == 3)
                {
                    sentence.Add(fields[0]);
                    tags.Add(fields[2]);
                }
                else
                {
                    throw new IOException(
                        $"Expected three fields per line in training data, got {fields.Length} for line '{line}'!");
                }
            }

            // Always clear adaptive data for spanish
            if (Language.SPA.Equals(lang))
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

                    if (tag.EndsWith("MISC", StringComparison.Ordinal) && (types & GenerateMiscEntities) == 0)
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
