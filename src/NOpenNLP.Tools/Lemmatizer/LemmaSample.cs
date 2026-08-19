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
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Lemmatizer;

/// <summary>
/// Represents an lemmatized sentence.
/// </summary>
// NOpenNLP: upstream implements java.io.Serializable, which has no .NET
// counterpart the port needs; model artifacts are written by the serializers in
// NOpenNLP.Tools.Util.Model instead.
public class LemmaSample
{
    private readonly IList<string> tokens; // NOpenNLP: made readonly
    private readonly IList<string> tags; // NOpenNLP: made readonly
    private readonly IList<string> lemmas;

    /// <summary>
    /// Represents one lemma sample.
    /// </summary>
    /// <param name="tokens">the token</param>
    /// <param name="tags">the postags</param>
    /// <param name="lemmas">the lemmas</param>
    public LemmaSample(string[] tokens, string[] tags, string[] lemmas)
    {
        ValidateArguments(tokens.Length, tags.Length, lemmas.Length);

        this.tokens = new JCG.List<string>(tokens).AsReadOnly();
        this.tags = new JCG.List<string>(tags).AsReadOnly();
        this.lemmas = new JCG.List<string>(lemmas).AsReadOnly();
    }

    /// <summary>
    /// Lemma Sample constructor.
    /// </summary>
    /// <param name="tokens">the tokens</param>
    /// <param name="tags">the postags</param>
    /// <param name="lemmas">the lemmas</param>
    public LemmaSample(IList<string> tokens, IList<string> tags, IList<string> lemmas)
    {
        ValidateArguments(tokens.Count, tags.Count, lemmas.Count);

        this.tokens = new JCG.List<string>(tokens).AsReadOnly();
        this.tags = new JCG.List<string>(tags).AsReadOnly();
        this.lemmas = new JCG.List<string>(lemmas).AsReadOnly();
    }

    public virtual string[] Tokens => [.. tokens];

    public virtual string[] Tags => [.. tags];

    public virtual string[] Lemmas => [.. lemmas];

    /// <exception cref="ArgumentException">if the three arrays do not have the same length</exception>
    private static void ValidateArguments(int tokensSize, int tagsSize, int lemmasSize)
    {
        if (tokensSize != tagsSize || tagsSize != lemmasSize)
        {
            throw new ArgumentException(
                "All arrays must have the same length: " +
                    "sentenceSize: " + tokensSize +
                    ", tagsSize: " + tagsSize +
                    ", predsSize: " + lemmasSize + "!");
        }
    }

    public override string ToString()
    {
        var lemmaString = new StringBuilder();

        for (int ci = 0; ci < lemmas.Count; ci++)
        {
            lemmaString.Append(tokens[ci]).Append('\t').Append(tags[ci])
                .Append('\t').Append(lemmas[ci]).Append('\n');
        }

        return lemmaString.ToString();
    }

    public override int GetHashCode() =>
        HashCode.Combine(Arrays.GetHashCode(Tokens), Arrays.GetHashCode(Tags), Arrays.GetHashCode(Lemmas));

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj is LemmaSample a)
        {
            return Arrays.Equals(Tokens, a.Tokens)
                && Arrays.Equals(Tags, a.Tags)
                && Arrays.Equals(Lemmas, a.Lemmas);
        }

        return false;
    }
}
