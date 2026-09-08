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
using System.IO;
using NOpenNLP.Tools.Ngram;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Languagemodel;

/// <summary>
/// A <see cref="ILanguageModel"/> based on a <see cref="NGramModel"/>
/// using Stupid Backoff to get the probabilities of the ngrams.
/// </summary>
public class NGramLanguageModel : NGramModel, ILanguageModel
{
    private const int DEFAULT_N = 3;

    private readonly int n;

    public NGramLanguageModel()
        : this(DEFAULT_N)
    {
    }

    public NGramLanguageModel(int n)
    {
        this.n = n;
    }

    /// <exception cref="IOException"/>
    public NGramLanguageModel(Stream @in)
        : this(@in, DEFAULT_N)
    {
    }

    /// <exception cref="IOException"/>
    public NGramLanguageModel(Stream @in, int n)
        : base(@in)
    {
        this.n = n;
    }

    public virtual void Add(params string[] tokens)
    {
        Add(new StringList(tokens), 1, n);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Deprecated: Use <see cref="CalculateProbability(string[])"/> instead.
    /// </remarks>
    [Obsolete("Use CalculateProbability(string[]) instead.")]
    public virtual double CalculateProbability(StringList tokens)
    {
        double probability = 0d;
        if (Count > 0)
        {
            foreach (StringList ngram in NGramUtils.GetNGrams(tokens, n))
            {
                double score = StupidBackoff(ngram);
                probability += Math.Log(score);
                if (double.IsNaN(probability))
                {
                    probability = 0d;
                    break;
                }
            }

            probability = Math.Exp(probability);
        }

        return probability;
    }

    /// <inheritdoc/>
    public virtual double CalculateProbability(params string[] tokens)
    {
        double probability = 0d;
        if (Count > 0)
        {
            foreach (string[] ngram in NGramUtils.GetNGrams(tokens, n))
            {
                double score = StupidBackoff(new StringList(ngram));
                probability += Math.Log(score);
                if (double.IsNaN(probability))
                {
                    probability = 0d;
                    break;
                }
            }

            probability = Math.Exp(probability);
        }

        return probability;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Deprecated: Use <see cref="PredictNextTokens(string[])"/> instead.
    /// </remarks>
    [Obsolete("Use PredictNextTokens(string[]) instead.")]
    public virtual StringList? PredictNextTokens(StringList tokens)
    {
        double maxProb = double.NegativeInfinity;
        StringList? token = null;

        foreach (StringList ngram in this)
        {
            string[] sequence = new string[ngram.Count + tokens.Count];
            for (int i = 0; i < tokens.Count; i++)
            {
                sequence[i] = tokens.GetToken(i);
            }

            for (int i = 0; i < ngram.Count; i++)
            {
                sequence[i + tokens.Count] = ngram.GetToken(i);
            }

            StringList sample = new StringList(sequence);
            double v = CalculateProbability(sample);
            if (v > maxProb)
            {
                maxProb = v;
                token = ngram;
            }
        }

        return token;
    }

    /// <inheritdoc/>
    public virtual string[]? PredictNextTokens(params string[] tokens)
    {
        double maxProb = double.NegativeInfinity;
        string[]? token = null;

        foreach (StringList ngram in this)
        {
            string[] sequence = new string[ngram.Count + tokens.Length];
            for (int i = 0; i < tokens.Length; i++)
            {
                sequence[i] = tokens[i];
            }

            for (int i = 0; i < ngram.Count; i++)
            {
                sequence[i + tokens.Length] = ngram.GetToken(i);
            }

            double v = CalculateProbability(sequence);
            if (v > maxProb)
            {
                maxProb = v;
                token = new string[ngram.Count];
                for (int i = 0; i < ngram.Count; i++)
                {
                    token[i] = ngram.GetToken(i);
                }
            }
        }

        return token;
    }

    private double StupidBackoff(StringList ngram)
    {
        int count = GetCount(ngram);
        StringList? nMinusOneToken = NGramUtils.GetNMinusOneTokenFirst(ngram);
        if (nMinusOneToken == null || nMinusOneToken.Count == 0)
        {
            return (double)count / (double)Count;
        }
        else if (count > 0)
        {
            double countM1 = GetCount(nMinusOneToken);
            if (countM1 == 0d)
            {
                countM1 = Count; // to avoid Infinite if n-1grams do not exist
            }

            return (double)count / countM1;
        }
        else
        {
            // NOpenNLP: GetNMinusOneTokenLast returns null for a single-token ngram, which upstream
            // passes straight back into stupidBackoff and dereferences, throwing NullPointerException.
            // The null-forgiving operator preserves that behavior rather than silently changing it.
            return 0.4 * StupidBackoff(NGramUtils.GetNMinusOneTokenLast(ngram)!);
        }
    }
}
