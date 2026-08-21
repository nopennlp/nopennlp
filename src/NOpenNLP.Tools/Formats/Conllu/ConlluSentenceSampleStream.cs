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

using System.Collections.Generic;
using System.IO;
using System.Text;
using NOpenNLP.Tools.Sentdetect;
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Formats.Conllu;

public class ConlluSentenceSampleStream(IObjectStream<ConlluSentence?> samples, int sentencesPerSample)
    : FilterObjectStream<ConlluSentence?, SentenceSample?>(samples)
{
    /// <inheritdoc/>
    /// <exception cref="IOException">if there is an error during reading</exception>
    public override SentenceSample? Read()
    {
        var documentText = new StringBuilder();

        JCG.List<Span> sentenceSpans = [];

        for (int i = 0; i < sentencesPerSample && samples.Read() is { } sentence; i++)
        {
            int startIndex = documentText.Length;

            // NOpenNLP: StringBuilder.Append(null) appends nothing, while Java's
            // appends the text "null". A sentence with no text comment must keep
            // producing the four-character span upstream produces.
            documentText.Append(sentence.TextComment ?? "null").Append(' ');
            sentenceSpans.Add(new Span(startIndex, documentText.Length - 1));
        }

        if (documentText.Length > 0)
        {
            documentText.Length -= 1;
            return new SentenceSample(documentText.ToString(), [.. sentenceSpans]);
        }

        return null;
    }
}
