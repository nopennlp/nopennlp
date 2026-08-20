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
using NOpenNLP.Tools.Sentdetect;
using NOpenNLP.Tools.Tokenize;
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Formats.Letsmt;

public class DetokenizeSentenceSampleStream(IDetokenizer detokenizer, IObjectStream<SentenceSample?> samples)
    : FilterObjectStream<SentenceSample?, SentenceSample?>(samples)
{
    private readonly IDetokenizer detokenizer = detokenizer ?? throw new ArgumentNullException(nameof(detokenizer));

    /// <inheritdoc/>
    /// <exception cref="IOException">if there is an error during reading</exception>
    public override SentenceSample? Read()
    {
        SentenceSample? sample = samples.Read();

        if (sample != null)
        {
            IList<string> sentenceTexts = new JCG.List<string>();

            foreach (Span sentenceSpan in sample.GetSentences())
            {
                sentenceTexts.Add(sample.Document.Substring(sentenceSpan.Start, sentenceSpan.End - sentenceSpan.Start));
            }

            var documentText = new StringBuilder();
            IList<Span> newSentenceSpans = new JCG.List<Span>();
            foreach (string sentenceText in sentenceTexts)
            {
                string[] tokens = WhitespaceTokenizer.INSTANCE.Tokenize(sentenceText);

                int begin = documentText.Length;

                documentText.Append(detokenizer.Detokenize(tokens, null));
                newSentenceSpans.Add(new Span(begin, documentText.Length));
                documentText.Append(' ');
            }

            return new SentenceSample(documentText.ToString(), [.. newSentenceSpans]);
        }

        return null;
    }
}
