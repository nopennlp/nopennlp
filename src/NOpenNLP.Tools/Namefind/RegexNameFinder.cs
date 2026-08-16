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
using NOpenNLP.Tools.Support;
using NOpenNLP.Tools.Util;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Namefind;

/// <summary>
/// Name finder based on a series of regular expressions.
/// </summary>
public sealed class RegexNameFinder : ITokenNameFinder
{
    private readonly IDictionary<string, Regex[]>? regexMap;

    public RegexNameFinder(IDictionary<string, Regex[]> regexMap)
    {
        this.regexMap = regexMap ?? throw new ArgumentNullException(nameof(regexMap), "regexMap must not be null");
    }

    public RegexNameFinder(Regex[] patterns, string? type)
    {
        if (patterns == null || patterns.Length == 0)
        {
            throw new ArgumentException("patterns must not be null or empty!");
        }

        Patterns = patterns;
        Type = type;
    }

    /// <summary>
    /// Use the <see cref="RegexNameFinder(Regex[], string)"/> constructor
    /// for single types, and/or the <see cref="RegexNameFinder(IDictionary{string, Regex[]})"/>
    /// constructor.
    /// </summary>
    [Obsolete]
    public RegexNameFinder(Regex[] patterns)
    {
        if (patterns == null || patterns.Length == 0)
        {
            throw new ArgumentException("patterns must not be null or empty!");
        }

        Patterns = patterns;
        Type = null;
    }

    public Span[] Find(string[] tokens)
    {
        JCG.Dictionary<int, int> sentencePosTokenMap = new();

        StringBuilder sentenceString = new(tokens.Length * 10);

        for (int i = 0; i < tokens.Length; i++)
        {
            int startIndex = sentenceString.Length;
            sentencePosTokenMap.Put(startIndex, i);

            sentenceString.Append(tokens[i]);

            int endIndex = sentenceString.Length;
            sentencePosTokenMap.Put(endIndex, i + 1);

            if (i < tokens.Length - 1)
            {
                sentenceString.Append(' ');
            }
        }

        JCG.List<Span> annotations = [];

        string text = sentenceString.ToString();

        if (regexMap != null)
        {
            foreach (var entry in regexMap)
            {
                foreach (var mPattern in entry.Value)
                {
                    foreach (Match match in mPattern.Matches(text))
                    {
                        if (sentencePosTokenMap.TryGetValue(match.Index, out int tokenStartIndex)
                            && sentencePosTokenMap.TryGetValue(match.Index + match.Length, out int tokenEndIndex))
                        {
                            var annotation = new Span(tokenStartIndex, tokenEndIndex, entry.Key);
                            annotations.Add(annotation);
                        }
                    }
                }
            }
        }
        else
        {
            foreach (var mPattern in Patterns!)
            {
                foreach (Match match in mPattern.Matches(text))
                {
                    if (sentencePosTokenMap.TryGetValue(match.Index, out int tokenStartIndex)
                        && sentencePosTokenMap.TryGetValue(match.Index + match.Length, out int tokenEndIndex))
                    {
                        var annotation = new Span(tokenStartIndex, tokenEndIndex, Type);
                        annotations.Add(annotation);
                    }
                }
            }
        }

        var result = new Span[annotations.Count];
        annotations.CopyTo(result, 0);
        return result;
    }

    /// <summary>
    /// NEW. This method removes the need for tokenization, but returns the Span
    /// with character indices, rather than word.
    /// </summary>
    /// <param name="text">The text to search.</param>
    /// <returns>The character-index spans of the matches.</returns>
    public Span[] Find(string text) => GetAnnotations(text);

    private Span[] GetAnnotations(string text)
    {
        JCG.List<Span> annotations = [];

        if (regexMap != null)
        {
            foreach (var entry in regexMap)
            {
                foreach (var mPattern in entry.Value)
                {
                    foreach (Match match in mPattern.Matches(text))
                    {
                        var annotation = new Span(match.Index, match.Index + match.Length, entry.Key);
                        annotations.Add(annotation);
                    }
                }
            }
        }
        else
        {
            foreach (var mPattern in Patterns!)
            {
                foreach (Match match in mPattern.Matches(text))
                {
                    var annotation = new Span(match.Index, match.Index + match.Length, Type);
                    annotations.Add(annotation);
                }
            }
        }

        var result = new Span[annotations.Count];
        annotations.CopyTo(result, 0);
        return result;
    }

    public void ClearAdaptiveData()
    {
        // nothing to clear
    }

    public Regex[]? Patterns { get; set; }

    public string? Type { get; set; }
}
