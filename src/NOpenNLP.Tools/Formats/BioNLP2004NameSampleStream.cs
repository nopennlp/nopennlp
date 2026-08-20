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
using NOpenNLP.Tools.Namefind;
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Formats;

/// <summary>
/// Parser for the training files of the BioNLP/NLPBA 2004 shared task.
/// <para/>
/// The data contains five named entity types: DNA, RNA, protein, cell_type and cell_line.<br/>
/// <para/>
/// Data can be found on this web site:<br/>
/// http://www-tsujii.is.s.u-tokyo.ac.jp/GENIA/ERtask/report.html
/// <para/>
/// <b>Note:</b> Do not use this class, internal use only!
/// </summary>
public class BioNLP2004NameSampleStream : ObjectStreamBase<NameSample?>
{
    public const int GenerateDnaEntities = 0x01;
    public const int GenerateProteinEntities = 0x01 << 1;
    public const int GenerateCelltypeEntities = 0x01 << 2;
    public const int GenerateCelllineEntities = 0x01 << 3;
    public const int GenerateRnaEntities = 0x01 << 4;

    private readonly int types;

    private readonly IObjectStream<string?> lineStream;

    /// <exception cref="IOException">if there is an error during reading</exception>
    // NOpenNLP: upstream also calls System.setOut to reconfigure stdout to UTF-8 here,
    // and wraps the constructor in a catch for UnsupportedEncodingException that its own
    // comment notes can never happen. Reconfiguring the process's console from a corpus
    // reader is an incidental side effect rather than part of parsing, so it is omitted.
    public BioNLP2004NameSampleStream(IInputStreamFactory @in, int types)
    {
        lineStream = new PlainTextByLineStream(@in, Encoding.UTF8);
        this.types = types;
    }

    /// <inheritdoc/>
    /// <exception cref="IOException">if there is an error during reading</exception>
    public override NameSample? Read()
    {
        IList<string> sentence = new JCG.List<string>();
        IList<string> tags = new JCG.List<string>();

        bool isClearAdaptiveData = false;

        // Empty line indicates end of sentence

        string? line;
        while ((line = lineStream.Read()) != null && !StringUtil.IsEmpty(line.Trim()))
        {
            if (line.StartsWith("###MEDLINE:", StringComparison.Ordinal))
            {
                isClearAdaptiveData = true;
                lineStream.Read();
                continue;
            }

            if (line.Contains("ABSTRACT TRUNCATED"))
                continue;

            string[] fields = line.Split('\t');

            if (fields.Length == 2)
            {
                sentence.Add(fields[0]);
                tags.Add(fields[1]);
            }
            else
            {
                throw new IOException("Expected two fields per line in training data, got " +
                    fields.Length + " for line '" + line + "'!");
            }
        }

        if (sentence.Count > 0)
        {
            // convert name tags into spans
            IList<Span> names = new JCG.List<Span>();

            int beginIndex = -1;
            int endIndex = -1;
            for (int i = 0; i < tags.Count; i++)
            {
                string tag = tags[i];

                if (tag.EndsWith("DNA", StringComparison.Ordinal) && (types & GenerateDnaEntities) == 0)
                    tag = "O";

                if (tag.EndsWith("protein", StringComparison.Ordinal) && (types & GenerateProteinEntities) == 0)
                    tag = "O";

                if (tag.EndsWith("cell_type", StringComparison.Ordinal) && (types & GenerateCelltypeEntities) == 0)
                    tag = "O";

                // NOpenNLP: upstream tests GENERATE_CELLTYPE_ENTITIES on this line rather
                // than GENERATE_CELLLINE_ENTITIES, so cell_line entities are governed by
                // the cell_type flag. Kept as-is to match upstream output; changing it
                // would make this reader disagree with Apache OpenNLP on the same corpus.
                if (tag.EndsWith("cell_line", StringComparison.Ordinal) && (types & GenerateCelltypeEntities) == 0)
                    tag = "O";
                if (tag.EndsWith("RNA", StringComparison.Ordinal) && (types & GenerateRnaEntities) == 0)
                    tag = "O";

                if (tag.StartsWith("B-", StringComparison.Ordinal))
                {
                    if (beginIndex != -1)
                    {
                        names.Add(new Span(beginIndex, endIndex, tags[beginIndex].Substring(2)));
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
                        names.Add(new Span(beginIndex, endIndex, tags[beginIndex].Substring(2)));
                        beginIndex = -1;
                        endIndex = -1;
                    }
                }
                else
                {
                    throw new IOException("Invalid tag: " + tag);
                }
            }

            // if one span remains, create it here
            if (beginIndex != -1)
                names.Add(new Span(beginIndex, endIndex, tags[beginIndex].Substring(2)));

            return new NameSample([.. sentence], [.. names], isClearAdaptiveData);
        }
        else if (line != null)
        {
            // Just filter out empty events, if two lines in a row are empty
            return Read();
        }
        else
        {
            // source stream is not returning anymore lines
            return null;
        }
    }

    /// <inheritdoc/>
    /// <exception cref="IOException">if there is an error during resetting the stream</exception>
    public override void Reset() => lineStream.Reset();

    protected override void Dispose(bool disposing) => lineStream.Dispose();
}
