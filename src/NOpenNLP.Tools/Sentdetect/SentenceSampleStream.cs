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

using NOpenNLP.Tools.Util;
using System.Text;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Sentdetect;

/// <summary>
/// This class is a stream filter which reads a sentence by line samples from
/// a <c>Reader</c> and converts them into <see cref="SentenceSample"/> objects.
/// An empty line indicates the begin of a new document.
/// </summary>
public class SentenceSampleStream(IObjectStream<string?> sentences)
    : FilterObjectStream<string?, SentenceSample?>(new EmptyLinePreprocessorStream(sentences))
{
    public static string ReplaceNewLineEscapeTags(string s) => s.Replace("<LF>", "\n").Replace("<CR>", "\r");

    public override SentenceSample? Read()
    {
        var sentencesString = new StringBuilder();

        // NOpenNLP: upstream uses a LinkedList purely as an append-only list; J2N's
        // List provides the same ordering with an O(1) conversion to an array.
        JCG.List<Span> sentenceSpans = [];

        while (samples.Read() is { } sentence && !sentence.Equals(""))
        {
            int begin = sentencesString.Length;
            sentence = sentence.Trim();
            sentence = ReplaceNewLineEscapeTags(sentence);
            sentencesString.Append(sentence);
            int end = sentencesString.Length;
            sentenceSpans.Add(new Span(begin, end));
            sentencesString.Append(' ');
        }

        if (sentenceSpans.Count > 0)
        {
            return new SentenceSample(sentencesString.ToString(), sentenceSpans.ToArray());
        }

        return null;
    }
}
