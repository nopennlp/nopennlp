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
using NOpenNLP.Tools.Util.Eval;

namespace NOpenNLP.Tools.Tokenize;

/// <summary>
/// The <see cref="TokenizerEvaluator"/> measures the performance of
/// the given <see cref="ITokenizer"/> with the provided reference
/// <see cref="TokenSample"/>s.
/// </summary>
/// <seealso cref="Evaluator{T}"/>
/// <seealso cref="ITokenizer"/>
/// <seealso cref="TokenSample"/>
public class TokenizerEvaluator : Evaluator<TokenSample>
{
    // NOpenNLP: made readonly
    private readonly FMeasure fmeasure = new();

    /// <summary>
    /// The <see cref="ITokenizer"/> used to create the predicted tokens.
    /// </summary>
    // NOpenNLP: made readonly
    private readonly ITokenizer tokenizer;

    /// <summary>
    /// Initializes the current instance with the given <see cref="ITokenizer"/>.
    /// </summary>
    /// <param name="tokenizer">the <see cref="ITokenizer"/> to evaluate.</param>
    /// <param name="listeners">evaluation sample listeners</param>
    public TokenizerEvaluator(ITokenizer tokenizer, params ITokenizerEvaluationMonitor?[]? listeners)
        : base(listeners)
        => this.tokenizer = tokenizer;

    protected override TokenSample ProcessSample(TokenSample reference)
    {
        var predictions = tokenizer.TokenizePos(reference.Text);
        fmeasure.UpdateScores(reference.TokenSpans, predictions);

        return new TokenSample(reference.Text, predictions);
    }

    public virtual FMeasure FMeasure => fmeasure;
}
