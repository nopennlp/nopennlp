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
using System.Globalization;
using System.IO;
using System.Text;
using J2N.Text;
using NOpenNLP.Tools.Tokenize;
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Formats.Brat;

/// <summary>
/// Reads the annotations from the brat .ann annotation file.
/// </summary>
public class BratAnnotationStream : ObjectStreamBase<BratAnnotation?>
{
    internal abstract class BratAnnotationParser
    {
        internal const int ID_OFFSET = 0;
        internal const int TYPE_OFFSET = 1;
        internal const string NOTES_TYPE = "AnnotatorNotes";

        /// <exception cref="IOException">if there is an error during parsing</exception>
        internal virtual BratAnnotation? Parse(Span[] tokens, string line) => null;

        /// <exception cref="InvalidFormatException">if the value is not a valid integer</exception>
        protected int ParseInt32(string intString)
        {
            // NOpenNLP: parsed with the invariant culture so the result does not depend on
            // the ambient culture the way a bare int.Parse would.
            if (!int.TryParse(intString, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
            {
                throw new InvalidFormatException(new FormatException(
                    "Failed to parse integer: " + intString));
            }

            return value;
        }
    }

    internal class SpanAnnotationParser : BratAnnotationParser
    {
        private const int BEGIN_OFFSET = 2;
        private const int END_OFFSET = 3;

        /// <inheritdoc/>
        internal override BratAnnotation Parse(Span[] values, string line)
        {
            if (values.Length > 4)
            {
                string type = values[TYPE_OFFSET].GetCoveredText(line.AsCharSequence()).ToString();

                int firstTextTokenIndex = -1;

                int beginIndex = ParseInt32(values[BEGIN_OFFSET].GetCoveredText(line.AsCharSequence()).ToString());

                IList<Span> fragments = new JCG.List<Span>();

                for (int i = END_OFFSET; i < values.Length; i++)
                {
                    int endOffset;
                    string value = values[i].GetCoveredText(line.AsCharSequence()).ToString();

                    if (value.IndexOf(';') >= 0)
                    {
                        // NOpenNLP: Java's String.split(";") drops trailing empty strings; the
                        // parts below are only ever indexed at 0 and 1, so a plain split gives
                        // the same values, and a missing second part throws
                        // IndexOutOfRangeException where Java throws
                        // ArrayIndexOutOfBoundsException.
                        string[] parts = value.Split(';');
                        endOffset = ParseInt32(parts[0]);
                        fragments.Add(new Span(beginIndex, endOffset, type));
                        beginIndex = ParseInt32(parts[1]);
                    }
                    else
                    {
                        endOffset = ParseInt32(value);
                        firstTextTokenIndex = i + 1;
                        fragments.Add(new Span(beginIndex, endOffset, type));
                        break;
                    }
                }

                string id = values[ID_OFFSET].GetCoveredText(line.AsCharSequence()).ToString();

                string coveredText = line.Substring(values[firstTextTokenIndex].Start,
                    values[values.Length - 1].End - values[firstTextTokenIndex].Start);

                try
                {
                    return new SpanAnnotation(id, type, [.. fragments], coveredText);
                }
                catch (ArgumentException e)
                {
                    throw new InvalidFormatException(e);
                }
            }
            else
            {
                throw new InvalidFormatException("Line must have at least 5 fields");
            }
        }
    }

    internal class RelationAnnotationParser : BratAnnotationParser
    {
        private const int ARG1_OFFSET = 2;
        private const int ARG2_OFFSET = 3;

        /// <exception cref="InvalidFormatException">if the argument cannot be parsed</exception>
        private string ParseArg(string arg)
        {
            if (arg.Length > 4)
            {
                return arg.Substring(5).Trim();
            }
            else
            {
                throw new InvalidFormatException("Failed to parse argument: " + arg);
            }
        }

        /// <inheritdoc/>
        internal override BratAnnotation Parse(Span[] tokens, string line) =>
            new RelationAnnotation(tokens[ID_OFFSET].GetCoveredText(line.AsCharSequence()).ToString(),
                tokens[TYPE_OFFSET].GetCoveredText(line.AsCharSequence()).ToString(),
                ParseArg(tokens[ARG1_OFFSET].GetCoveredText(line.AsCharSequence()).ToString()),
                ParseArg(tokens[ARG2_OFFSET].GetCoveredText(line.AsCharSequence()).ToString()));
    }

    internal class EventAnnotationParser : BratAnnotationParser
    {
        /// <inheritdoc/>
        internal override BratAnnotation Parse(Span[] tokens, string line)
        {
            string[] typeParts = tokens[TYPE_OFFSET].GetCoveredText(line.AsCharSequence()).ToString().Split(':');

            if (typeParts.Length != 2)
            {
                throw new InvalidFormatException(string.Format(CultureInfo.InvariantCulture,
                    "Failed to parse [{0}], type part must be in the format type:trigger", line));
            }

            string type = typeParts[0];
            string eventTrigger = typeParts[1];

            IDictionary<string, string> arguments = new JCG.Dictionary<string, string>();

            for (int i = TYPE_OFFSET + 1; i < tokens.Length; i++)
            {
                string[] parts = tokens[i].GetCoveredText(line.AsCharSequence()).ToString().Split(':');

                if (parts.Length != 2)
                {
                    throw new InvalidFormatException(string.Format(CultureInfo.InvariantCulture,
                        "Failed to parse [{0}], argument parts must be in form argument:value", line));
                }

                arguments[parts[0]] = parts[1];
            }

            return new EventAnnotation(tokens[ID_OFFSET].GetCoveredText(line.AsCharSequence()).ToString(),
                type, eventTrigger, arguments);
        }
    }

    internal class AttributeAnnotationParser : BratAnnotationParser
    {
        private const int ATTACHED_TO_OFFSET = 2;
        private const int VALUE_OFFSET = 3;

        /// <inheritdoc/>
        internal override BratAnnotation Parse(Span[] values, string line)
        {
            if (values.Length == 3 || values.Length == 4)
            {
                string? value = null;

                if (values.Length == 4)
                {
                    value = values[VALUE_OFFSET].GetCoveredText(line.AsCharSequence()).ToString();
                }

                return new AttributeAnnotation(values[ID_OFFSET].GetCoveredText(line.AsCharSequence()).ToString(),
                    values[TYPE_OFFSET].GetCoveredText(line.AsCharSequence()).ToString(),
                    values[ATTACHED_TO_OFFSET].GetCoveredText(line.AsCharSequence()).ToString(), value);
            }
            else
            {
                throw new InvalidFormatException("Line must have 3 or 4 fields");
            }
        }
    }

    internal class AnnotatorNoteParser : BratAnnotationParser
    {
        private const int ATTACH_TO_OFFSET = 2;
        private const int START_VALUE_OFFSET = 3;

        /// <inheritdoc/>
        internal override BratAnnotation Parse(Span[] tokens, string line)
        {
            Span noteSpan = new Span(tokens[START_VALUE_OFFSET].Start,
                tokens[tokens.Length - 1].End);

            return new AnnotatorNoteAnnotation(tokens[ID_OFFSET].GetCoveredText(line.AsCharSequence()).ToString(),
                tokens[ATTACH_TO_OFFSET].GetCoveredText(line.AsCharSequence()).ToString(),
                noteSpan.GetCoveredText(line.AsCharSequence()).ToString());
        }
    }

    private readonly AnnotationConfiguration config;
    private readonly StreamReader reader;
    private readonly string id;

    internal BratAnnotationStream(AnnotationConfiguration config, string id, Stream @in)
    {
        this.config = config;
        this.id = id;

        reader = new StreamReader(@in, Encoding.UTF8);
    }

    /// <inheritdoc/>
    /// <exception cref="IOException">if there is an error during reading</exception>
    public override BratAnnotation? Read()
    {
        string? line = reader.ReadLine();

        if (line != null)
        {
            Span[] tokens = WhitespaceTokenizer.INSTANCE.TokenizePos(line);

            if (tokens.Length > 2)
            {
                string annId = tokens[BratAnnotationParser.ID_OFFSET].GetCoveredText(line.AsCharSequence()).ToString();

                if (annId.Length == 0)
                {
                    throw new InvalidFormatException("annotation id is empty");
                }

                // The first leter of the annotation id marks the annotation type

                BratAnnotationParser parser;
                switch (annId[0])
                {
                    case 'T':
                        parser = new SpanAnnotationParser();
                        break;
                    case 'R':
                        parser = new RelationAnnotationParser();
                        break;
                    case 'A':
                        parser = new AttributeAnnotationParser();
                        break;
                    case 'E':
                        parser = new EventAnnotationParser();
                        break;
                    case '#':
                        // the # can be a Note or a comment... if a note, handle it,
                        // otherwise skip the unsupported type..
                        if (tokens[BratAnnotationParser.TYPE_OFFSET].GetCoveredText(line.AsCharSequence())
                            .ToString().Equals(BratAnnotationParser.NOTES_TYPE, StringComparison.Ordinal))
                        {
                            parser = new AnnotatorNoteParser();
                        }
                        else
                        {
                            return Read();
                        }
                        break;
                    default:
                        // Skip it, do that for everything unsupported (e.g. "*" id)
                        return Read();
                }

                try
                {
                    return parser.Parse(tokens, line);
                }
                catch (IOException e)
                {
                    throw new IOException(string.Format(CultureInfo.InvariantCulture,
                        "Failed to parse ann document with id [{0}.ann]", id), e);
                }
            }
        }

        return null;
    }

    /// <inheritdoc/>
    // NOpenNLP: upstream calls BufferedReader.reset(), which restores a previously
    // marked position; without a preceding mark() it throws IOException. StreamReader
    // has no mark/reset, so this seeks the underlying stream back to the beginning --
    // the position upstream's reader would have been marked at -- and discards the
    // reader's buffer. A stream that cannot seek throws, as upstream's unmarked
    // reader does.
    public override void Reset()
    {
        if (!reader.BaseStream.CanSeek)
        {
            throw new IOException("Stream not marked");
        }

        reader.BaseStream.Seek(0, SeekOrigin.Begin);
        reader.DiscardBufferedData();
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            reader.Dispose();
        }
    }
}
