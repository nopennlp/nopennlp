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
using J2N.Text;
using NOpenNLP.Tools.Support;
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Tokenize;

/// <summary>
/// A <see cref="TokenSample"/> is text with token spans.
/// </summary>
// NOpenNLP: upstream implements java.io.Serializable, which has no .NET
// counterpart the port needs; model artifacts are written by the serializers in
// NOpenNLP.Tools.Util.Model instead.
public class TokenSample
{
    public const string DEFAULT_SEPARATOR_CHARS = "<SPLIT>";

    private const string separatorChars = DEFAULT_SEPARATOR_CHARS;

    private readonly string text;

    private readonly IList<Span> tokenSpans;

    /// <summary>
    /// Initializes the current instance.
    /// </summary>
    /// <param name="text">the text which contains the tokens.</param>
    /// <param name="tokenSpans">the spans which mark the begin and end of the tokens.</param>
    public TokenSample(string text, Span[] tokenSpans)
    {
        if (tokenSpans is null)
        {
            throw new ArgumentNullException(nameof(tokenSpans), "tokenSpans must not be null");
        }

        this.text = text ?? throw new ArgumentNullException(nameof(text), "text must not be null");
        this.tokenSpans = new JCG.List<Span>(tokenSpans).AsReadOnly();

        foreach (var tokenSpan in tokenSpans)
        {
            if (tokenSpan.Start < 0 || tokenSpan.Start > text.Length ||
                tokenSpan.End > text.Length || tokenSpan.End < 0)
            {
                throw new ArgumentException("Span " + tokenSpan +
                    " is out of bounds, text length: " + text.Length + "!");
            }
        }
    }

    public TokenSample(IDetokenizer detokenizer, string[] tokens)
    {
        var sentence = new StringBuilder();

        var operations = detokenizer.Detokenize(tokens);

        JCG.List<Span> mergedTokenSpans = [];

        for (int i = 0; i < operations.Length; i++)
        {
            bool isSeparateFromPreviousToken = i > 0 &&
                !IsMergeToRight(operations[i - 1]) &&
                !IsMergeToLeft(operations[i]);

            if (isSeparateFromPreviousToken)
            {
                sentence.Append(' ');
            }

            int beginIndex = sentence.Length;
            sentence.Append(tokens[i]);
            mergedTokenSpans.Add(new Span(beginIndex, sentence.Length));
        }

        text = sentence.ToString();
        tokenSpans = mergedTokenSpans.AsReadOnly();
    }

    private static bool IsMergeToRight(DetokenizationOperation operation) =>
        DetokenizationOperation.MergeToRight.Equals(operation)
            || DetokenizationOperation.MergeBoth.Equals(operation);

    private static bool IsMergeToLeft(DetokenizationOperation operation) =>
        DetokenizationOperation.MergeToLeft.Equals(operation)
            || DetokenizationOperation.MergeBoth.Equals(operation);

    /// <summary>
    /// Retrieves the text.
    /// </summary>
    public virtual string Text => text;

    /// <summary>
    /// Retrieves the token spans.
    /// </summary>
    public virtual Span[] TokenSpans => [.. tokenSpans];

    public override string ToString()
    {
        var sentence = new StringBuilder();

        int lastEndIndex = -1;
        foreach (var token in tokenSpans)
        {
            if (lastEndIndex != -1)
            {
                // If there are no chars between last token
                // and this token insert the separator chars
                // otherwise insert a space

                string separator;
                if (lastEndIndex == token.Start)
                    separator = separatorChars;
                else
                    separator = " ";

                sentence.Append(separator);
            }

            sentence.Append(token.GetCoveredText(text.AsCharSequence()));

            lastEndIndex = token.End;
        }

        return sentence.ToString();
    }

    private static void AddToken(StringBuilder sample, IList<Span> tokenSpans,
        string token, bool isNextMerged)
    {
        int tokenSpanStart = sample.Length;
        sample.Append(token);
        int tokenSpanEnd = sample.Length;

        tokenSpans.Add(new Span(tokenSpanStart, tokenSpanEnd));

        if (!isNextMerged)
            sample.Append(" ");
    }

    public static TokenSample Parse(string sampleString, string separatorChars)
    {
        if (sampleString is null)
        {
            throw new ArgumentNullException(nameof(sampleString), "sampleString must not be null");
        }

        if (separatorChars is null)
        {
            throw new ArgumentNullException(nameof(separatorChars), "separatorChars must not be null");
        }

        var whitespaceTokenSpans = WhitespaceTokenizer.INSTANCE.TokenizePos(sampleString);

        // Pre-allocate 20% for newly created tokens
        var realTokenSpans = new JCG.List<Span>((int)(whitespaceTokenSpans.Length * 1.2d));

        var untaggedSampleString = new StringBuilder();

        foreach (var whiteSpaceTokenSpan in whitespaceTokenSpans)
        {
            string whitespaceToken = whiteSpaceTokenSpan.GetCoveredText(sampleString.AsCharSequence()).ToString();

            bool wasTokenReplaced = false;

            int tokStart = 0;
            int tokEnd;
            while ((tokEnd = whitespaceToken.IndexOf(separatorChars, tokStart, StringComparison.Ordinal)) > -1)
            {
                // NOpenNLP: Java substring(begin, end) takes an end index; .NET takes a length.
                string token = whitespaceToken.Substring(tokStart, tokEnd - tokStart);

                AddToken(untaggedSampleString, realTokenSpans, token, true);

                tokStart = tokEnd + separatorChars.Length;
                wasTokenReplaced = true;
            }

            if (wasTokenReplaced)
            {
                // If the token contains the split chars at least once
                // a span for the last token must still be added
                string token = whitespaceToken.Substring(tokStart);

                AddToken(untaggedSampleString, realTokenSpans, token, false);
            }
            else
            {
                // If it does not contain the split chars at lest once
                // just copy the original token span

                AddToken(untaggedSampleString, realTokenSpans, whitespaceToken, false);
            }
        }

        return new TokenSample(untaggedSampleString.ToString(), [.. realTokenSpans]);
    }

    public override int GetHashCode() => HashCode.Combine(Text, Arrays.GetHashCode(TokenSpans));

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj is TokenSample a)
        {
            return Text.Equals(a.Text)
                && Arrays.Equals(TokenSpans, a.TokenSpans);
        }

        return false;
    }
}
