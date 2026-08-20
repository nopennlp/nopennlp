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

using System.Collections.Generic;
using System.IO;
using System.Text;
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Formats.Muc;

/// <summary>
/// SAX style SGML parser.
/// <para/>
/// Note:<br/>
/// The implementation is very limited, but good enough to
/// parse the MUC corpora. Its must very likely be extended/improved/fixed to parse
/// a different SGML corpora.
/// </summary>
public class SgmlParser
{
    public abstract class ContentHandler
    {
        /// <exception cref="InvalidFormatException">if the element cannot be handled</exception>
        public virtual void StartElement(string name, IDictionary<string, string> attributes)
        {
        }

        /// <exception cref="InvalidFormatException">if the characters cannot be handled</exception>
        public virtual void Characters(string chars)
        {
        }

        /// <exception cref="InvalidFormatException">if the element cannot be handled</exception>
        public virtual void EndElement(string name)
        {
        }
    }

    /// <exception cref="InvalidFormatException">if the tag name cannot be extracted</exception>
    // NOpenNLP: upstream takes a CharSequence; the only caller passes the StringBuilder
    // it is accumulating into, so StringBuilder is passed directly here.
    private static string ExtractTagName(StringBuilder tagChars)
    {
        int fromOffset = 1;

        if (tagChars.Length > 1 && tagChars[1] == '/')
        {
            fromOffset = 2;
        }

        for (int ci = 1; ci < tagChars.Length; ci++)
        {
            if (tagChars[ci] == '>' || StringUtil.IsWhitespace(tagChars[ci]))
            {
                return tagChars.ToString(fromOffset, ci - fromOffset);
            }
        }

        throw new InvalidFormatException("Failed to extract tag name!");
    }

    // NOpenNLP: upstream takes a CharSequence; see ExtractTagName.
    private static IDictionary<string, string> GetAttributes(StringBuilder tagChars)
    {
        // format:
        // space
        // key
        // =
        // " <- begin
        // value chars
        // " <- end

        IDictionary<string, string> attributes = new JCG.Dictionary<string, string>();

        StringBuilder key = new StringBuilder();
        StringBuilder value = new StringBuilder();

        bool extractKey = false;
        bool extractValue = false;

        for (int i = 0; i < tagChars.Length; i++)
        {
            // White space indicates begin of new key name
            if (StringUtil.IsWhitespace(tagChars[i]) && !extractValue)
            {
                extractKey = true;
            }
            // Equals sign indicated end of key name
            else if (extractKey && ('=' == tagChars[i] || StringUtil.IsWhitespace(tagChars[i])))
            {
                extractKey = false;
            }
            // Inside key name, extract all chars
            else if (extractKey)
            {
                key.Append(tagChars[i]);
            }
            // " Indicates begin or end of value chars
            else if ('"' == tagChars[i])
            {
                if (extractValue)
                {
                    // NOpenNLP: Map.put overwrites an existing key; the indexer does the
                    // same, while Add would throw on a repeated attribute name.
                    attributes[key.ToString()] = value.ToString();

                    // clear key and value buffers
                    key.Length = 0;
                    value.Length = 0;
                }

                extractValue = !extractValue;
            }
            // Inside value, extract all chars
            else if (extractValue)
            {
                value.Append(tagChars[i]);
            }
        }

        return attributes;
    }

    /// <exception cref="IOException">if there is an error during reading</exception>
    public void Parse(TextReader @in, ContentHandler handler)
    {
        StringBuilder buffer = new StringBuilder();

        bool isInsideTag = false;
        bool isStartTag = true;

        int lastChar = -1;
        int c;
        while ((c = @in.Read()) != -1)
        {
            if ('<' == c)
            {
                if (isInsideTag)
                {
                    throw new InvalidFormatException("Did not expect < char!");
                }

                if (buffer.ToString().Trim().Length > 0)
                {
                    handler.Characters(buffer.ToString().Trim());
                }

                buffer.Length = 0;

                isInsideTag = true;
                isStartTag = true;
            }

            // NOpenNLP: upstream calls StringBuilder.appendCodePoint(c), but Reader.read()
            // returns a single UTF-16 code unit rather than a code point, so the value is
            // always in the BMP and appending the char is equivalent.
            buffer.Append((char)c);

            if ('/' == c && lastChar == '<')
            {
                isStartTag = false;
            }

            if ('>' == c)
            {
                if (!isInsideTag)
                {
                    throw new InvalidFormatException("Did not expect > char!");
                }

                if (isStartTag)
                {
                    handler.StartElement(ExtractTagName(buffer), GetAttributes(buffer));
                }
                else
                {
                    handler.EndElement(ExtractTagName(buffer));
                }

                buffer.Length = 0;

                isInsideTag = false;
            }

            lastChar = c;
        }

        if (isInsideTag)
        {
            throw new InvalidFormatException("Did not find matching > char!");
        }
    }
}
