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
using System.Text.RegularExpressions;
using NOpenNLP.Tools.Support;
using NOpenNLP.Tools.Tokenize;
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Namefind;

/// <summary>
/// Class for holding names for a single unit of text.
/// </summary>
// NOpenNLP: upstream implements java.io.Serializable, which has no .NET
// counterpart the port needs.
public class NameSample
{
    private readonly string? id;
    private readonly IList<string> sentence;
    private readonly IList<Span> names;
    private readonly string[][]? additionalContext;
    private readonly bool isClearAdaptiveData;

    /// <summary>
    /// The default type value when there is no type in training data.
    /// </summary>
    public const string DEFAULT_TYPE = "default";

    public NameSample(string? id, string[] sentence, Span[]? names,
        string[][]? additionalContext, bool clearAdaptiveData)
    {
        this.id = id;

        if (sentence is null)
        {
            throw new ArgumentNullException(nameof(sentence), "sentence must not be null");
        }

        names ??= [];

        this.sentence = new JCG.List<string>(sentence).AsReadOnly();
        var namesList = new JCG.List<Span>(names);
        namesList.Sort();
        this.names = namesList.AsReadOnly();

        if (additionalContext != null)
        {
            this.additionalContext = new string[additionalContext.Length][];

            for (int i = 0; i < additionalContext.Length; i++)
            {
                this.additionalContext[i] = new string[additionalContext[i].Length];
                Array.Copy(additionalContext[i], 0, this.additionalContext[i], 0, additionalContext[i].Length);
            }
        }
        else
        {
            this.additionalContext = null;
        }

        isClearAdaptiveData = clearAdaptiveData;

        // Check that name spans are not overlapping, otherwise throw exception
        if (this.names.Count > 1)
        {
            for (int i = 1; i < this.names.Count; i++)
            {
                if (this.names[i].Start < this.names[i - 1].End)
                {
                    // NOpenNLP: upstream throws RuntimeException; InvalidOperationException is
                    // the closest .NET counterpart for an unchecked programming-error throw.
                    throw new InvalidOperationException(
                        string.Format("name spans {0} and {1} are overlapped in file: {2}",
                            this.names[i - 1], this.names[i], id));
                }
            }
        }
    }

    /// <summary>
    /// Initializes the current instance.
    /// </summary>
    /// <param name="sentence">training sentence</param>
    /// <param name="names">the names contained in the sentence</param>
    /// <param name="additionalContext">the additional context or <c>null</c></param>
    /// <param name="clearAdaptiveData">if true the adaptive data of the
    ///     feature generators is cleared</param>
    public NameSample(string[] sentence, Span[]? names,
        string[][]? additionalContext, bool clearAdaptiveData)
        : this(null, sentence, names, additionalContext, clearAdaptiveData)
    {
    }

    public NameSample(string[] sentence, Span[]? names, bool clearAdaptiveData)
        : this(sentence, names, null, clearAdaptiveData)
    {
    }

    public virtual string? Id => id;

    public virtual string[] Sentence => [.. sentence];

    public virtual Span[] Names => [.. names];

    public virtual string[][]? AdditionalContext => additionalContext;

    public virtual bool IsClearAdaptiveDataSet => isClearAdaptiveData;

    public override int GetHashCode() =>
        HashCode.Combine(Arrays.GetHashCode(Sentence), Arrays.GetHashCode(Names),
            Arrays.GetHashCode(AdditionalContext), IsClearAdaptiveDataSet);

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj is NameSample a)
        {
            return Arrays.Equals(Sentence, a.Sentence)
                && Arrays.Equals(Names, a.Names)
                && Arrays.Equals(AdditionalContext, a.AdditionalContext)
                && IsClearAdaptiveDataSet == a.IsClearAdaptiveDataSet;
        }

        return false;
    }

    public override string ToString()
    {
        var result = new StringBuilder();

        // If adaptive data must be cleared insert an empty line
        // before the sample sentence line
        if (IsClearAdaptiveDataSet)
            result.Append('\n');

        for (int tokenIndex = 0; tokenIndex < sentence.Count; tokenIndex++)
        {
            // token

            foreach (var name in names)
            {
                if (name.Start == tokenIndex)
                {
                    // check if nameTypes is null, or if the nameType for this specific
                    // entity is empty. If it is, we leave the nameType blank.
                    if (name.Type == null)
                    {
                        result.Append(NameSampleDataStream.START_TAG).Append(' ');
                    }
                    else
                    {
                        result.Append(NameSampleDataStream.START_TAG_PREFIX).Append(name.Type).Append("> ");
                    }
                }

                if (name.End == tokenIndex)
                {
                    result.Append(NameSampleDataStream.END_TAG).Append(' ');
                }
            }

            result.Append(sentence[tokenIndex]).Append(' ');
        }

        if (sentence.Count > 1)
            result.Length -= 1;

        foreach (var name in names)
        {
            if (name.End == sentence.Count)
            {
                result.Append(' ').Append(NameSampleDataStream.END_TAG);
            }
        }

        return result.ToString();
    }

    private static string ErrorTokenWithContext(string[] sentence, int index)
    {
        var errorString = new StringBuilder();

        // two token before
        if (index > 1)
            errorString.Append(sentence[index - 2]).Append(' ');

        if (index > 0)
            errorString.Append(sentence[index - 1]).Append(' ');

        // token itself
        errorString.Append("###");
        errorString.Append(sentence[index]);
        errorString.Append("###").Append(' ');

        // two token after
        if (index + 1 < sentence.Length)
            errorString.Append(sentence[index + 1]).Append(' ');

        if (index + 2 < sentence.Length)
            errorString.Append(sentence[index + 2]);

        return errorString.ToString();
    }

    // NOpenNLP: upstream uses Matcher.matches(), which anchors at both ends; the
    // anchors are added to the pattern here so IsMatch is equivalent.
    private static readonly Regex START_TAG_PATTERN = new("^<START(:([^:>\\s]*))?>$", RegexOptions.Compiled);

    /// <exception cref="IOException">if the tagged tokens cannot be parsed</exception>
    public static NameSample Parse(string taggedTokens, bool isClearAdaptiveData) =>
        Parse(taggedTokens, DEFAULT_TYPE, isClearAdaptiveData);

    /// <exception cref="IOException">if the tagged tokens cannot be parsed</exception>
    public static NameSample Parse(string taggedTokens, string defaultType, bool isClearAdaptiveData)
    {
        // TODO: Should throw another exception, and then convert it into an IOException in the stream

        string[] parts = WhitespaceTokenizer.INSTANCE.Tokenize(taggedTokens);

        var tokenList = new JCG.List<string>(parts.Length);
        var nameList = new JCG.List<Span>();

        string nameType = defaultType;
        int startIndex = -1;
        int wordIndex = 0;

        // we check if at least one name has the a type. If no one has, we will
        // leave the NameType property of NameSample null.
        bool catchingName = false;

        for (int pi = 0; pi < parts.Length; pi++)
        {
            var startMatcher = START_TAG_PATTERN.Match(parts[pi]);
            if (startMatcher.Success)
            {
                if (catchingName)
                {
                    throw new IOException("Found unexpected annotation" +
                        " while handling a name sequence: " + ErrorTokenWithContext(parts, pi));
                }

                catchingName = true;
                startIndex = wordIndex;

                // NOpenNLP: Java's Matcher.group(int) returns null for a group that did
                // not participate in the match; .NET reports that through Group.Success.
                var nameTypeGroup = startMatcher.Groups[2];
                if (nameTypeGroup.Success)
                {
                    if (nameTypeGroup.Value.Length == 0)
                    {
                        throw new IOException("Missing a name type: " + ErrorTokenWithContext(parts, pi));
                    }

                    nameType = nameTypeGroup.Value;
                }
            }
            else if (parts[pi].Equals(NameSampleDataStream.END_TAG, StringComparison.Ordinal))
            {
                if (!catchingName)
                {
                    throw new IOException("Found unexpected annotation: " + ErrorTokenWithContext(parts, pi));
                }

                catchingName = false;
                // create name
                nameList.Add(new Span(startIndex, wordIndex, nameType));
            }
            else
            {
                tokenList.Add(parts[pi]);
                wordIndex++;
            }
        }

        string[] sentence = [.. tokenList];
        Span[] names = [.. nameList];

        return new NameSample(sentence, names, isClearAdaptiveData);
    }
}
