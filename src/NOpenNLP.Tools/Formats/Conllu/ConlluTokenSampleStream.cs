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
using System.Globalization;
using System.IO;
using System.Text;
using NOpenNLP.Tools.Tokenize;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Formats.Conllu;

public class ConlluTokenSampleStream(IObjectStream<ConlluSentence?> samples)
    : FilterObjectStream<ConlluSentence?, TokenSample?>(samples)
{
    /// <inheritdoc/>
    /// <exception cref="IOException">if there is an error during reading</exception>
    public override TokenSample? Read()
    {
        var sentence = samples.Read();
        if (sentence != null)
        {
            if (sentence.TextComment != null)
            {
                var text = new StringBuilder(sentence.TextComment);
                int searchIndex = 0;

                foreach (var wordLine in sentence.WordLines)
                {
                    // skip over inserted words which are not in the source text
                    if (wordLine.Id.Contains("."))
                    {
                        continue;
                    }

                    string token = wordLine.Form;

                    // NOpenNLP: upstream calls StringBuilder.indexOf(String, int), which
                    // System.Text.StringBuilder does not offer; searching the current
                    // contents as a string gives the same result.
                    int tokenIndex = text.ToString().IndexOf(token, searchIndex, StringComparison.Ordinal);

                    if (tokenIndex == -1)
                    {
                        throw new IOException(string.Format(CultureInfo.InvariantCulture,
                            "Failed to match token [{0}] in sentence [{1}] with text [{2}]",
                            token, sentence.SentenceIdComment, text));
                    }

                    searchIndex = tokenIndex + token.Length;
                    if (searchIndex < text.Length)
                    {
                        if (!StringUtil.IsWhitespace(text[searchIndex]))
                        {
                            text.Insert(searchIndex, TokenSample.DEFAULT_SEPARATOR_CHARS);
                        }
                    }
                }

                return TokenSample.Parse(text.ToString(), TokenSample.DEFAULT_SEPARATOR_CHARS);
            }
            else
            {
                throw new IOException("Sentence is missing raw text sample!");
            }
        }

        return null;
    }
}
