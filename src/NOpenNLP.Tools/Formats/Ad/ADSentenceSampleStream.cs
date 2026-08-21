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
using System.Text.RegularExpressions;
using NOpenNLP.Tools.Sentdetect;
using NOpenNLP.Tools.Sentdetect.Lang;
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Formats.Ad;

/// <summary>
/// <b>Note:</b> Do not use this class, internal use only!
/// </summary>
public class ADSentenceSampleStream : ObjectStreamBase<SentenceSample?>
{
    private readonly IObjectStream<ADSentenceStream.Sentence?> adSentenceStream;

    private int text = -1;
    private int para = -1;
    private bool isSameText;
    private bool isSamePara;
    private ADSentenceStream.Sentence? sent;
    private readonly bool isIncludeTitles = true; // NOpenNLP: made readonly
    private bool isTitle;

    private readonly char[] ptEosCharacters;

    /// <summary>
    /// Creates a new <see cref="SentenceSample"/> stream from a line stream, i.e.
    /// <see cref="IObjectStream{T}"/> of <see cref="string"/>, that could be a
    /// <see cref="PlainTextByLineStream"/> object.
    /// </summary>
    /// <param name="lineStream">a stream of lines as <see cref="string"/></param>
    /// <param name="includeHeadlines">if true will output the sentences marked as news headlines</param>
    public ADSentenceSampleStream(IObjectStream<string?> lineStream, bool includeHeadlines)
    {
        adSentenceStream = new ADSentenceStream(lineStream);
        ptEosCharacters = SortedEosCharacters();
        isIncludeTitles = includeHeadlines;
    }

    /// <summary>
    /// Creates a new <see cref="SentenceSample"/> stream from a <see cref="Stream"/>.
    /// </summary>
    /// <param name="in">input stream from the corpus</param>
    /// <param name="charsetName">the charset to use while reading the corpus</param>
    /// <param name="includeHeadlines">if true will output the sentences marked as news headlines</param>
    /// <exception cref="IOException">if there is an error during reading</exception>
    // NOpenNLP: upstream wraps this in a catch for UnsupportedEncodingException that its own
    // comment notes can never happen; the wrapper is dropped here.
    public ADSentenceSampleStream(IInputStreamFactory @in, string charsetName, bool includeHeadlines)
    {
        adSentenceStream = new ADSentenceStream(new PlainTextByLineStream(@in, charsetName));
        ptEosCharacters = SortedEosCharacters();
        isIncludeTitles = includeHeadlines;
    }

    // NOpenNLP: upstream assigns Factory.ptEosCharacters straight into the field and then calls
    // Arrays.sort on it, which sorts that shared static array in place and so mutates state every
    // other Factory consumer sees. The array is copied before sorting here so the shared one is
    // left alone; the sorted order is required by the binary search in HasPunctuation.
    private static char[] SortedEosCharacters()
    {
        char[] eosCharacters = (char[])Factory.ptEosCharacters.Clone();
        Array.Sort(eosCharacters);
        return eosCharacters;
    }

    // The Arvores Deitadas Corpus has information about texts and paragraphs.
    /// <inheritdoc/>
    /// <exception cref="IOException">if there is an error during reading</exception>
    public override SentenceSample? Read()
    {
        if (sent == null)
        {
            sent = adSentenceStream.Read();
            UpdateMeta();
            if (sent == null)
            {
                return null;
            }
        }

        var document = new StringBuilder();
        JCG.List<Span> sentences = [];
        do
        {
            do
            {
                if (!isTitle || isIncludeTitles)
                {
                    if (HasPunctuation(sent!.Text!))
                    {
                        int start = document.Length;
                        document.Append(sent.Text);
                        sentences.Add(new Span(start, document.Length));
                        document.Append(' ');
                    }
                }
                sent = adSentenceStream.Read();
                UpdateMeta();
            }
            while (isSamePara);
            // break; // got one paragraph!
        }
        while (isSameText);

        string doc;
        if (document.Length > 0)
        {
            doc = document.ToString(0, document.Length - 1);
        }
        else
        {
            doc = document.ToString();
        }

        return new SentenceSample(doc, [.. sentences]);
    }

    private bool HasPunctuation(string text)
    {
        text = text.Trim();
        if (text.Length > 0)
        {
            char lastChar = text[^1];
            return Array.BinarySearch(ptEosCharacters, lastChar) >= 0;
        }
        return false;
    }

    // there are some different types of metadata depending on the corpus.
    // todo: merge this patterns
    // NOpenNLP: upstream declares this as an instance field, recompiling the pattern per stream.
    private static readonly Regex meta1 = new("^(?:[a-zA-Z\\-]*(\\d+)).*?p=(\\d+).*");

    private void UpdateMeta()
    {
        if (sent != null)
        {
            string meta = sent.Metadata!;
            // NOpenNLP: Java's Matcher.matches() anchors the whole input. meta1 starts with ^ but
            // ends with .* rather than $, so an unanchored .NET match would differ on input
            // containing a newline -- .NET's `.` stops at \n while the trailing .* must still reach
            // the end for matches() to succeed. The explicit full-length check keeps them aligned.
            var m = MatchWholeString(meta1, meta);
            int currentText;
            int currentPara;
            if (m.Success)
            {
                // NOpenNLP: parsing must be culture-invariant; a bare int.Parse is culture-sensitive.
                currentText = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                currentPara = int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
            }
            else
            {
                throw new InvalidOperationException("Invalid metadata: " + meta);
            }
            isSamePara = isSameText = false;
            if (currentText == text)
                isSameText = true;

            if (isSameText && currentPara == para)
                isSamePara = true;

            isTitle = meta.Contains("title");

            text = currentText;
            para = currentPara;
        }
        else
        {
            isSamePara = isSameText = false;
        }
    }

    // NOpenNLP-specific: Java's Matcher.matches() requires the entire input to match, while .NET's
    // Regex.Match finds a match anywhere. Anchoring to the full input length reproduces matches().
    private static Match MatchWholeString(Regex regex, string input)
    {
        var match = regex.Match(input);
        if (match is { Success: true, Index: 0 } && match.Length == input.Length)
        {
            return match;
        }
        return Match.Empty;
    }

    /// <inheritdoc/>
    /// <exception cref="IOException">if there is an error during resetting the stream</exception>
    /// <exception cref="NotSupportedException">if the underlying stream does not support resetting</exception>
    public override void Reset() => adSentenceStream.Reset();

    protected override void Dispose(bool disposing) => adSentenceStream.Dispose();
}
