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
using NOpenNLP.Tools.Util.Eval;

namespace NOpenNLP.Tools.Sentdetect;

/// <summary>
/// The <see cref="SentenceDetectorEvaluator"/> measures the performance of
/// the given <see cref="ISentenceDetector"/> with the provided reference
/// <see cref="SentenceSample"/>s.
/// </summary>
/// <seealso cref="Evaluator{T}"/>
/// <seealso cref="ISentenceDetector"/>
/// <seealso cref="SentenceSample"/>
public class SentenceDetectorEvaluator : Evaluator<SentenceSample>
{
    // NOpenNLP: made readonly
    private readonly FMeasure fmeasure = new();

    /// <summary>
    /// The <see cref="ISentenceDetector"/> used to predict sentences.
    /// </summary>
    // NOpenNLP: made readonly
    private readonly ISentenceDetector sentenceDetector;

    /// <summary>
    /// Initializes the current instance.
    /// </summary>
    /// <param name="sentenceDetector">the <see cref="ISentenceDetector"/> to evaluate.</param>
    /// <param name="listeners">evaluation sample listeners</param>
    public SentenceDetectorEvaluator(ISentenceDetector sentenceDetector,
        params ISentenceDetectorEvaluationMonitor?[]? listeners)
        : base(listeners)
        => this.sentenceDetector = sentenceDetector;

    private static Span[] TrimSpans(string document, Span[] spans)
    {
        var trimedSpans = new Span[spans.Length];

        for (int i = 0; i < spans.Length; i++)
        {
            trimedSpans[i] = spans[i].Trim(document.AsCharSequence());
        }

        return trimedSpans;
    }

    protected override SentenceSample ProcessSample(SentenceSample sample)
    {
        var predictions = TrimSpans(sample.Document, sentenceDetector.SentPosDetect(sample.Document));
        var references = TrimSpans(sample.Document, sample.GetSentences());

        fmeasure.UpdateScores(references, predictions);

        return new SentenceSample(sample.Document, predictions);
    }

    public virtual FMeasure FMeasure => fmeasure;
}
