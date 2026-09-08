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
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Languagemodel;

/// <summary>
/// A language model can calculate the probability <i>p</i> (between 0 and 1) of a
/// certain <see cref="StringList">sequence of tokens</see>, given its underlying vocabulary.
/// </summary>
public interface ILanguageModel
{
    /// <summary>
    /// Calculate the probability of a series of tokens (e.g. a sentence), given a vocabulary.
    /// </summary>
    /// <param name="tokens">the text tokens to calculate the probability for</param>
    /// <returns>the probability of the given text tokens in the vocabulary</returns>
    /// <remarks>
    /// Deprecated: Use <see cref="CalculateProbability(string[])"/> instead.
    /// </remarks>
    [Obsolete("Use CalculateProbability(string[]) instead.")]
    double CalculateProbability(StringList tokens);

    /// <summary>
    /// Calculate the probability of a series of tokens (e.g. a sentence), given a vocabulary.
    /// </summary>
    /// <param name="tokens">the text tokens to calculate the probability for</param>
    /// <returns>the probability of the given text tokens in the vocabulary</returns>
    double CalculateProbability(params string[] tokens);

    /// <summary>
    /// Predict the most probable output sequence of tokens, given an input sequence of tokens.
    /// </summary>
    /// <param name="tokens">a sequence of tokens</param>
    /// <returns>the most probable subsequent token sequence</returns>
    /// <remarks>
    /// Deprecated: Use <see cref="PredictNextTokens(string[])"/> instead.
    /// </remarks>
    [Obsolete("Use PredictNextTokens(string[]) instead.")]
    StringList? PredictNextTokens(StringList tokens);

    /// <summary>
    /// Predict the most probable output sequence of tokens, given an input sequence of tokens.
    /// </summary>
    /// <param name="tokens">a sequence of tokens</param>
    /// <returns>the most probable subsequent token sequence</returns>
    string[]? PredictNextTokens(params string[] tokens);
}
