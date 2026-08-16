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
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Sentdetect;

/// <summary>
/// The Newline Sentence Detector assumes that sentences are line delimited and
/// recognizes one sentence per non-empty line.
/// </summary>
public class NewlineSentenceDetector : ISentenceDetector
{
    public virtual string[] SentDetect(string s) => Span.SpansToStrings(SentPosDetect(s), s.AsCharSequence());

    public virtual Span[] SentPosDetect(string s)
    {
        JCG.List<Span> sentences = new JCG.List<Span>();

        int start = 0;

        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];

            if (c == '\n' || c == '\r')
            {
                if (i - start > 0)
                {
                    Span span = new Span(start, i).Trim(s.AsCharSequence());
                    if (span.Length > 0)
                    {
                        sentences.Add(span);
                    }

                    start = i + 1;
                }
            }
        }

        if (s.Length - start > 0)
        {
            Span span = new Span(start, s.Length).Trim(s.AsCharSequence());
            if (span.Length > 0)
            {
                sentences.Add(span);
            }
        }

        return sentences.ToArray();
    }
}
