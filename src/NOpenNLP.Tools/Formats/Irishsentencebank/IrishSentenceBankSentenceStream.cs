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

namespace NOpenNLP.Tools.Formats.Irishsentencebank;

internal class IrishSentenceBankSentenceStream : ObjectStreamBase<SentenceSample?>
{
    private readonly IrishSentenceBankDocument source;

    // NOpenNLP: the position has to survive across Read() calls, which is the manual
    // hasNext()/next() case CLAUDE.md keeps IEnumerator<T> for.
    private IEnumerator<IrishSentenceBankDocument.IrishSentenceBankSentence> sentenceIt;

    internal IrishSentenceBankSentenceStream(IrishSentenceBankDocument source)
    {
        this.source = source;

        // NOpenNLP: upstream ends the constructor with reset(); calling a virtual member
        // from a constructor would run a derived override against a half-built object, so
        // the one assignment reset() performs is inlined here instead.
        sentenceIt = source.Sentences.GetEnumerator();
    }

    /// <inheritdoc/>
    /// <exception cref="IOException">if there is an error during reading</exception>
    public override SentenceSample? Read()
    {
        var sentencesString = new StringBuilder();
        IList<Span> sentenceSpans = new JCG.List<Span>();

        while (sentenceIt.MoveNext())
        {
            IrishSentenceBankDocument.IrishSentenceBankSentence sentence = sentenceIt.Current;

            int begin = sentencesString.Length;

            if (sentence.Original != null)
            {
                sentencesString.Append(sentence.Original);
            }

            sentenceSpans.Add(new Span(begin, sentencesString.Length));
            sentencesString.Append(' ');
        }

        // end of stream is reached, indicate that with null return value
        if (sentenceSpans.Count == 0)
        {
            return null;
        }

        return new SentenceSample(sentencesString.ToString(), [.. sentenceSpans]);
    }

    /// <inheritdoc/>
    public override void Reset()
    {
        sentenceIt.Dispose();
        sentenceIt = source.Sentences.GetEnumerator();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            sentenceIt.Dispose();
        }
    }
}
