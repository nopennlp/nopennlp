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
using System.Text;
using NOpenNLP.Tools.Support;
using NOpenNLP.Tools.Tokenize;
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Postag;

/// <summary>
/// Represents an pos-tagged sentence.
/// </summary>
// NOpenNLP: upstream implements java.io.Serializable, which has no .NET
// counterpart the port needs; model artifacts are written by the serializers in
// NOpenNLP.Tools.Util.Model instead.
public class POSSample
{
    private readonly IList<string> sentence; // NOpenNLP: made readonly
    private readonly IList<string> tags; // NOpenNLP: made readonly
    private readonly string[][]? additionalContext;

    public POSSample(IList<string> sentence, IList<string> tags, string[][]? additionalContext = null)
    {
        this.sentence = new JCG.List<string>(sentence).AsReadOnly();
        this.tags = new JCG.List<string>(tags).AsReadOnly();

        CheckArguments();

        string[][]? ac;
        if (additionalContext != null)
        {
            ac = new string[additionalContext.Length][];

            for (int i = 0; i < additionalContext.Length; i++)
            {
                ac[i] = new string[additionalContext[i].Length];
                Array.Copy(additionalContext[i], 0, ac[i], 0, additionalContext[i].Length);
            }
        }
        else
        {
            ac = null;
        }

        this.additionalContext = ac;
    }

    public POSSample(string[] sentence, string[] tags, string[][]? additionalContext = null)
        : this((IList<string>)sentence, tags, additionalContext)
    {
    }

    private void CheckArguments()
    {
        if (sentence.Count != tags.Count)
        {
            throw new ArgumentException(
                "There must be exactly one tag for each token. tokens: " + sentence.Count +
                ", tags: " + tags.Count);
        }

        // NOpenNLP: the list element type is non-nullable here, so a null element can
        // only arrive through an unannotated caller; the checks are kept so such a
        // caller still gets upstream's exception rather than a later NullReferenceException.
        if (sentence.Contains(null!))
        {
            throw new ArgumentException("null elements are not allowed in sentence tokens!");
        }

        if (tags.Contains(null!))
        {
            throw new ArgumentException("null elements are not allowed in tags!");
        }
    }

    public virtual string[] Sentence => [.. sentence];

    public virtual string[] Tags => [.. tags];

    // NOpenNLP: the upstream name, getAddictionalContext, is a typo for "additional"
    // that the port keeps so the API stays recognizable against upstream.
    public virtual string[][]? AddictionalContext => additionalContext;

    public override string ToString()
    {
        var result = new StringBuilder();

        for (int i = 0; i < Sentence.Length; i++)
        {
            result.Append(Sentence[i]);
            result.Append('_');
            result.Append(Tags[i]);
            result.Append(' ');
        }

        if (result.Length > 0)
        {
            // get rid of last space
            result.Length -= 1;
        }

        return result.ToString();
    }

    /// <exception cref="InvalidFormatException">if the sentence is not in the
    ///     expected <c>token_tag</c> format.</exception>
    public static POSSample Parse(string sentenceString)
    {
        string[] tokenTags = WhitespaceTokenizer.INSTANCE.Tokenize(sentenceString);

        string[] sentence = new string[tokenTags.Length];
        string[] tags = new string[tokenTags.Length];

        for (int i = 0; i < tokenTags.Length; i++)
        {
            int split = tokenTags[i].LastIndexOf("_", StringComparison.Ordinal);

            if (split == -1)
            {
                throw new InvalidFormatException("Cannot find \"_\" inside token '" + tokenTags[i] + "'!");
            }

            // NOpenNLP: Java substring(begin, end) takes an end index; .NET takes a length.
            sentence[i] = tokenTags[i][..split];
            tags[i] = tokenTags[i][(split + 1)..];
        }

        return new POSSample(sentence, tags);
    }

    public override int GetHashCode() =>
        HashCode.Combine(Arrays.GetHashCode(Sentence), Arrays.GetHashCode(Tags));

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj is POSSample a)
        {
            return Arrays.Equals(Sentence, a.Sentence)
                && Arrays.Equals(Tags, a.Tags);
        }

        return false;
    }
}
