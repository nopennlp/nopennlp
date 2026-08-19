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

using J2N.Text;
using NOpenNLP.Tools.Support;
using NOpenNLP.Tools.Tokenize;
using NOpenNLP.Tools.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Sentdetect;

/// <summary>
/// A <see cref="SentenceSample"/> contains a document with
/// begin indexes of the individual sentences.
/// </summary>
public class SentenceSample
{
    private readonly string document;
    private readonly IList<Span> sentences;

    /// <summary>
    /// Initializes the current instance.
    /// </summary>
    /// <param name="document"></param>
    /// <param name="sentences"></param>
    public SentenceSample(string document, params Span[] sentences)
    {
        this.document = document;
        this.sentences = new ReadOnlyCollection<Span>(new JCG.List<Span>(sentences));

        // validate that all spans are inside the document text
        foreach (Span sentence in sentences)
        {
            if (sentence.End > document.Length)
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture,
                    "Sentence span is outside of document text [len {0}] and span {1}",
                    document.Length, sentence));
            }
        }
    }

    public SentenceSample(IDetokenizer detokenizer, string[][] sentences)
    {
        JCG.List<Span> spans = new JCG.List<Span>(sentences.Length);

        StringBuilder documentBuilder = new StringBuilder();

        foreach (string[] sentenceTokens in sentences)
        {
            string sampleSentence = detokenizer.Detokenize(sentenceTokens, null);

            int beginIndex = documentBuilder.Length;
            documentBuilder.Append(sampleSentence);

            spans.Add(new Span(beginIndex, documentBuilder.Length));
        }

        document = documentBuilder.ToString();
        this.sentences = new ReadOnlyCollection<Span>(spans);
    }

    /// <summary>
    /// Retrieves the document.
    /// </summary>
    public virtual string Document => document;

    /// <summary>
    /// Retrieves the sentences, that is the begin indexes of the sentences in the document.
    /// </summary>
    public virtual Span[] GetSentences()
    {
        Span[] result = new Span[sentences.Count];
        sentences.CopyTo(result, 0);
        return result;
    }

    // TODO: This one must output the tags!
    public override string ToString()
    {
        StringBuilder documentBuilder = new StringBuilder();
        foreach (Span sentSpan in sentences)
        {
            documentBuilder.Append(sentSpan.GetCoveredText(document.AsCharSequence()).ToString()
                .Replace("\r", "<CR>").Replace("\n", "<LF>"));
            documentBuilder.Append('\n');
        }

        return documentBuilder.ToString();
    }

    public override int GetHashCode() => HashCode.Combine(Document, Arrays.GetHashCode(GetSentences()));

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj is SentenceSample a)
        {
            return Document.Equals(a.Document, StringComparison.Ordinal)
                   && Arrays.Equals(GetSentences(), a.GetSentences());
        }

        return false;
    }
}
