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

namespace NOpenNLP.Tools.Tokenize;

/// <summary>
/// The <see cref="DetokenizerEvaluator"/> measures the performance of
/// the given <see cref="IDetokenizer"/> with the provided reference
/// <see cref="TokenSample"/>s.
/// </summary>
/// <seealso cref="DetokenizerEvaluator"/>
/// <seealso cref="IDetokenizer"/>
/// <seealso cref="TokenSample"/>
public class DetokenizerEvaluator : Evaluator<TokenSample>
{
    // NOpenNLP: made readonly
    private readonly FMeasure fmeasure = new();

    /// <summary>
    /// The <see cref="IDetokenizer"/> used to create the predicted tokens.
    /// </summary>
    // NOpenNLP: made readonly
    private readonly IDetokenizer detokenizer;

    /// <summary>
    /// Initializes the current instance with the given <see cref="IDetokenizer"/>.
    /// </summary>
    /// <param name="detokenizer">the <see cref="IDetokenizer"/> to evaluate.</param>
    /// <param name="listeners">evaluation sample listeners</param>
    // NOpenNLP: upstream types the listeners as DetokenEvaluationErrorListener,
    // which lives in the not-yet-ported cmdline package. The base Evaluator only
    // needs the monitor interface that listener implements, so the tokenize-side
    // monitor is used here; it accepts the cmdline listener once that is ported.
    public DetokenizerEvaluator(IDetokenizer detokenizer, params ITokenizerEvaluationMonitor?[]? listeners)
        : base(listeners)
        => this.detokenizer = detokenizer;

    protected override TokenSample ProcessSample(TokenSample reference)
    {
        string[] tokens = Span.SpansToStrings(reference.TokenSpans, reference.Text.AsCharSequence());
        string tokensstring = detokenizer.Detokenize(tokens, null);

        object[] references = [reference.Text];
        object[] predictions = [tokensstring];

        fmeasure.UpdateScores(references, predictions);

        return new TokenSample(tokensstring, reference.TokenSpans);
    }

    public virtual FMeasure FMeasure => fmeasure;
}
