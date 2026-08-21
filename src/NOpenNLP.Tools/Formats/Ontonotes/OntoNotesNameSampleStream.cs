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
using NOpenNLP.Tools.Tokenize;
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Formats.Ontonotes;

/// <summary>
/// Name Sample Stream parser for the OntoNotes 4.0 corpus.
/// </summary>
public class OntoNotesNameSampleStream(IObjectStream<string?> samples) : FilterObjectStream<string?, NameSample?>(samples)
{
    // NOpenNLP: upstream wraps the map with Collections.unmodifiableMap; the field is
    // private and never mutated after construction, so the wrapper is dropped.
    private readonly JCG.Dictionary<string, string> tokenConversionMap = new()
    {
        ["-LRB-"] = "(",
        ["-RRB-"] = ")",
        ["-LSB-"] = "[",
        ["-RSB-"] = "]",
        ["-LCB-"] = "{",
        ["-RCB-"] = "}",
        ["-AMP-"] = "&",
    };

    private readonly JCG.List<NameSample> nameSamples = []; // NOpenNLP: made readonly

    private string ConvertToken(string token)
    {
        var convertedToken = new StringBuilder(token);

        // NOpenNLP: upstream calls StringBuilder.indexOf, which System.Text.StringBuilder
        // does not offer; the builder still holds exactly `token` here, so searching it
        // gives the same result.
        int startTagEndIndex = token.IndexOf('>');

        if (token.Contains("=\"") && startTagEndIndex != -1)
        {
            convertedToken.Remove(0, startTagEndIndex + 1);
        }

        // NOpenNLP: upstream calls StringBuilder.indexOf, which System.Text.StringBuilder
        // does not offer; searching the current contents as a string gives the same result.
        string current = convertedToken.ToString();
        int endTagBeginIndex = current.IndexOf('<');
        int endTagEndIndex = current.IndexOf('>');

        if (endTagBeginIndex != -1 && endTagEndIndex != -1)
        {
            convertedToken.Remove(endTagBeginIndex, endTagEndIndex + 1 - endTagBeginIndex);
        }

        string cleanedToken = convertedToken.ToString();

        if (tokenConversionMap.TryGetValue(cleanedToken, out string? replacement))
        {
            cleanedToken = replacement;
        }

        return cleanedToken;
    }

    /// <inheritdoc/>
    /// <exception cref="IOException">if there is an error during reading</exception>
    public override NameSample? Read()
    {
        if (nameSamples.Count == 0)
        {
            string? doc = samples.Read();

            if (doc != null)
            {
                var docIn = new StringReader(doc);

                bool clearAdaptiveData = true;

                while (docIn.ReadLine() is { } line)
                {
                    if (line.StartsWith("<DOC", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (line.Equals("</DOC>", StringComparison.Ordinal))
                    {
                        break;
                    }

                    string[] tokens = WhitespaceTokenizer.INSTANCE.Tokenize(line);

                    JCG.List<Span> entities = [];
                    var cleanedTokens = new JCG.List<string>(tokens.Length);

                    int tokenIndex = 0;
                    int entityBeginIndex = -1;
                    string? entityType = null;
                    bool insideStartEnmaxTag = false;
                    foreach (string token in tokens)
                    {
                        // Split here, next part of tag is in new token
                        if (token.StartsWith("<ENAMEX", StringComparison.Ordinal))
                        {
                            insideStartEnmaxTag = true;
                            continue;
                        }

                        if (insideStartEnmaxTag)
                        {
                            const string typeBegin = "TYPE=\"";

                            if (token.StartsWith(typeBegin, StringComparison.Ordinal))
                            {
                                int typeEnd = token.IndexOf("\"", typeBegin.Length, StringComparison.Ordinal);

                                entityType = StringUtil.ToLowerCase(token[typeBegin.Length..typeEnd]);
                            }

                            if (token.Contains(">"))
                            {
                                entityBeginIndex = tokenIndex;
                                insideStartEnmaxTag = false;
                            }
                            else
                            {
                                continue;
                            }
                        }

                        if (token.EndsWith("</ENAMEX>", StringComparison.Ordinal))
                        {
                            entities.Add(new Span(entityBeginIndex, tokenIndex + 1, entityType));
                            entityBeginIndex = -1;
                        }

                        cleanedTokens.Add(ConvertToken(token));
                        tokenIndex++;
                    }

                    nameSamples.Add(new NameSample([.. cleanedTokens], [.. entities], clearAdaptiveData));

                    clearAdaptiveData = false;
                }
            }
        }

        if (nameSamples.Count != 0)
        {
            var first = nameSamples[0];
            nameSamples.RemoveAt(0);
            return first;
        }
        else
        {
            return null;
        }
    }
}
