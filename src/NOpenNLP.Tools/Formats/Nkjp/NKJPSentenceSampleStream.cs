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

namespace NOpenNLP.Tools.Formats.Nkjp;

public class NKJPSentenceSampleStream : ObjectStreamBase<SentenceSample?>
{
    private readonly NKJPSegmentationDocument segments;
    private readonly NKJPTextDocument text;

    // NOpenNLP: upstream holds a java.util.Iterator here. This is one of the cases
    // CLAUDE.md reserves for IEnumerator<T>: Read() needs manual control over advancing,
    // and Reset() has to be able to restart the walk from the beginning.
    private IEnumerator<KeyValuePair<string, IDictionary<string, NKJPSegmentationDocument.Pointer>>>
        segmentIt;

    internal NKJPSentenceSampleStream(NKJPSegmentationDocument segments, NKJPTextDocument text)
    {
        this.segments = segments;
        this.text = text;
        segmentIt = this.segments.Segments.GetEnumerator();
        Reset();
    }

    /// <inheritdoc/>
    /// <exception cref="IOException">if there is an error during reading</exception>
    public override SentenceSample? Read()
    {
        var sentencesString = new StringBuilder();
        IList<Span> sentenceSpans = new JCG.List<Span>();
        IDictionary<string, string> paragraphs = text.GetParagraphs();

        while (segmentIt.MoveNext())
        {
            KeyValuePair<string, IDictionary<string, NKJPSegmentationDocument.Pointer>> segment =
                segmentIt.Current;
            int start = 0;
            int end = 0;
            bool started = false;
            string lastParagraphId = "";
            string currentParagraph = "";

            foreach (string s in segment.Value.Keys)
            {
                NKJPSegmentationDocument.Pointer currentPointer = segment.Value[s];
                Span currentSpan = currentPointer.ToSpan();

                if (!started)
                {
                    start = currentSpan.Start;
                    started = true;
                    lastParagraphId = currentPointer.id;
                    currentParagraph = Paragraph(paragraphs, currentPointer.id);
                }

                if (!lastParagraphId.Equals(currentPointer.id, System.StringComparison.Ordinal))
                {
                    int new_start = sentencesString.Length;
                    sentencesString.Append(currentParagraph.Substring(start, end - start));
                    int new_end = sentencesString.Length;
                    sentenceSpans.Add(new Span(new_start, new_end));
                    sentencesString.Append(' ');

                    start = currentSpan.Start;
                    end = currentSpan.End;
                    lastParagraphId = currentPointer.id;
                    currentParagraph = Paragraph(paragraphs, currentPointer.id);
                }
                else
                {
                    end = currentSpan.End;
                }
            }

            int new_start2 = sentencesString.Length;
            sentencesString.Append(currentParagraph.Substring(start, end - start));
            int new_end2 = sentencesString.Length;
            sentenceSpans.Add(new Span(new_start2, new_end2));
            sentencesString.Append(' ');
        }

        // end of stream is reached, indicate that with null return value
        if (sentenceSpans.Count == 0)
        {
            return null;
        }

        Span[] spans = new Span[sentenceSpans.Count];
        sentenceSpans.CopyTo(spans, 0);
        return new SentenceSample(sentencesString.ToString(), spans);
    }

    // NOpenNLP: upstream calls paragraphs.get(id), which yields null for a segment
    // pointing at a paragraph the text document does not contain; the C# indexer would
    // throw KeyNotFoundException instead. Upstream then dereferences that null in
    // substring() and fails with a NullPointerException, so the mismatch is a broken
    // document either way -- but this keeps the failure at the same place and lets a
    // paragraph that is present but empty behave identically.
    private static string Paragraph(IDictionary<string, string> paragraphs, string id) =>
        paragraphs.TryGetValue(id, out string? paragraph) ? paragraph : null!;

    /// <inheritdoc/>
    public override void Reset() => segmentIt = segments.Segments.GetEnumerator();

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            segmentIt.Dispose();
        }
    }
}
