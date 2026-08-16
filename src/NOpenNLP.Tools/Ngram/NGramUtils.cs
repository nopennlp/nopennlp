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
using System.Diagnostics;
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Ngram;

/// <summary>
/// Utility class for ngrams.
/// Some methods apply specifically to certain 'n' values, for e.g. tri/bi/uni-grams.
/// </summary>
public static class NGramUtils
{
    /// <summary>
    /// Calculate the probability of a ngram in a vocabulary using Laplace smoothing algorithm.
    /// </summary>
    /// <param name="ngram">the ngram to get the probability for</param>
    /// <param name="set">the vocabulary</param>
    /// <param name="k">the smoothing factor</param>
    /// <returns>the Laplace smoothing probability</returns>
    /// <seealso href="https://en.wikipedia.org/wiki/Additive_smoothing">Additive Smoothing</seealso>
    public static double CalculateLaplaceSmoothingProbability(StringList ngram,
        IEnumerable<StringList> set, double k) =>
        (Count(ngram, set) + k) / (Count(GetNMinusOneTokenFirst(ngram)!, set) + k * 1);

    /// <summary>
    /// Calculate the probability of a unigram in a vocabulary using maximum likelihood estimation.
    /// </summary>
    /// <param name="word">the only word in the unigram</param>
    /// <param name="set">the vocabulary</param>
    /// <returns>the maximum likelihood probability</returns>
    public static double CalculateUnigramMLProbability(string word, ICollection<StringList> set)
    {
        double vocSize = 0d;
        foreach (var s in set)
        {
            vocSize += s.Count;
        }

        return Count(new StringList(word), set) / vocSize;
    }

    /// <summary>
    /// Calculate the probability of a bigram in a vocabulary using maximum likelihood estimation.
    /// </summary>
    /// <param name="x0">first word in the bigram</param>
    /// <param name="x1">second word in the bigram</param>
    /// <param name="set">the vocabulary</param>
    /// <returns>the maximum likelihood probability</returns>
    public static double CalculateBigramMLProbability(string x0, string x1, ICollection<StringList> set) =>
        CalculateNgramMLProbability(new StringList(x0, x1), set);

    /// <summary>
    /// Calculate the probability of a trigram in a vocabulary using maximum likelihood estimation.
    /// </summary>
    /// <param name="x0">first word in the trigram</param>
    /// <param name="x1">second word in the trigram</param>
    /// <param name="x2">third word in the trigram</param>
    /// <param name="set">the vocabulary</param>
    /// <returns>the maximum likelihood probability</returns>
    public static double CalculateTrigramMLProbability(string x0, string x1, string x2,
        IEnumerable<StringList> set) =>
        CalculateNgramMLProbability(new StringList(x0, x1, x2), set);

    /// <summary>
    /// Calculate the probability of a ngram in a vocabulary using maximum likelihood estimation.
    /// </summary>
    /// <param name="ngram">a ngram</param>
    /// <param name="set">the vocabulary</param>
    /// <returns>the maximum likelihood probability</returns>
    public static double CalculateNgramMLProbability(StringList ngram, IEnumerable<StringList> set)
    {
        var ngramMinusOne = GetNMinusOneTokenFirst(ngram);
        return Count(ngram, set) / Count(ngramMinusOne!, set);
    }

    /// <summary>
    /// Calculate the probability of a bigram in a vocabulary using prior Laplace smoothing algorithm.
    /// </summary>
    /// <param name="x0">the first word in the bigram</param>
    /// <param name="x1">the second word in the bigram</param>
    /// <param name="set">the vocabulary</param>
    /// <param name="k">the smoothing factor</param>
    /// <returns>the prior Laplace smoothing probability</returns>
    public static double CalculateBigramPriorSmoothingProbability(string x0, string x1,
        ICollection<StringList> set, double k) =>
        (Count(new StringList(x0, x1), set) + k * CalculateUnigramMLProbability(x1, set)) /
            (Count(new StringList(x0), set) + k * set.Count);

    /// <summary>
    /// Calculate the probability of a trigram in a vocabulary using a linear interpolation algorithm.
    /// </summary>
    /// <param name="x0">the first word in the trigram</param>
    /// <param name="x1">the second word in the trigram</param>
    /// <param name="x2">the third word in the trigram</param>
    /// <param name="set">the vocabulary</param>
    /// <param name="lambda1">trigram interpolation factor</param>
    /// <param name="lambda2">bigram interpolation factor</param>
    /// <param name="lambda3">unigram interpolation factor</param>
    /// <returns>the linear interpolation probability</returns>
    public static double CalculateTrigramLinearInterpolationProbability(string x0, string x1,
        string x2, ICollection<StringList> set, double lambda1, double lambda2, double lambda3)
    {
        Debug.Assert(lambda1 + lambda2 + lambda3 == 1, "lambdas sum should be equals to 1");
        Debug.Assert(lambda1 > 0 && lambda2 > 0 && lambda3 > 0, "lambdas should all be greater than 0");

        return lambda1 * CalculateTrigramMLProbability(x0, x1, x2, set) +
            lambda2 * CalculateBigramMLProbability(x1, x2, set) +
            lambda3 * CalculateUnigramMLProbability(x2, set);
    }

    /// <summary>
    /// Calculate the probability of a ngram in a vocabulary using the missing probability mass algorithm.
    /// </summary>
    /// <param name="ngram">the ngram</param>
    /// <param name="discount">discount factor</param>
    /// <param name="set">the vocabulary</param>
    /// <returns>the probability</returns>
    public static double CalculateMissingNgramProbabilityMass(StringList ngram, double discount,
        IEnumerable<StringList> set)
    {
        double missingMass = 0d;
        double countWord = Count(ngram, set);
        foreach (var word in FlatSet(set))
        {
            missingMass += (Count(GetNPlusOneNgram(ngram, word), set) - discount) / countWord;
        }

        return 1 - missingMass;
    }

    /// <summary>
    /// Get the (n-1)th ngram of a given ngram, that is the same ngram except the last word in the ngram.
    /// </summary>
    /// <param name="ngram">a ngram</param>
    /// <returns>a ngram, or <c>null</c> if the given ngram holds a single token</returns>
    public static StringList? GetNMinusOneTokenFirst(StringList ngram)
    {
        string[] tokens = new string[ngram.Count - 1];
        for (int i = 0; i < ngram.Count - 1; i++)
        {
            tokens[i] = ngram.GetToken(i);
        }

        return tokens.Length > 0 ? new StringList(tokens) : null;
    }

    /// <summary>
    /// Get the (n-1)th ngram of a given ngram, that is the same ngram except the first word in the ngram.
    /// </summary>
    /// <param name="ngram">a ngram</param>
    /// <returns>a ngram, or <c>null</c> if the given ngram holds a single token</returns>
    public static StringList? GetNMinusOneTokenLast(StringList ngram)
    {
        string[] tokens = new string[ngram.Count - 1];
        for (int i = 1; i < ngram.Count; i++)
        {
            tokens[i - 1] = ngram.GetToken(i);
        }

        return tokens.Length > 0 ? new StringList(tokens) : null;
    }

    private static StringList GetNPlusOneNgram(StringList ngram, string word)
    {
        string[] tokens = new string[ngram.Count + 1];
        for (int i = 0; i < ngram.Count; i++)
        {
            tokens[i] = ngram.GetToken(i);
        }

        tokens[^1] = word;
        return new StringList(tokens);
    }

    private static double Count(StringList ngram, IEnumerable<StringList> sentences)
    {
        double count = 0d;
        foreach (var sentence in sentences)
        {
            int idx0 = IndexOf(sentence, ngram.GetToken(0));
            if (idx0 >= 0 && sentence.Count >= idx0 + ngram.Count)
            {
                bool match = true;
                for (int i = 1; i < ngram.Count; i++)
                {
                    string sentenceToken = sentence.GetToken(idx0 + i);
                    string ngramToken = ngram.GetToken(i);
                    match &= sentenceToken.Equals(ngramToken, System.StringComparison.Ordinal);
                }

                if (match)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static int IndexOf(StringList sentence, string token)
    {
        for (int i = 0; i < sentence.Count; i++)
        {
            if (token.Equals(sentence.GetToken(i), System.StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    private static ICollection<string> FlatSet(IEnumerable<StringList> set)
    {
        ICollection<string> flatSet = new JCG.HashSet<string>();
        foreach (var sentence in set)
        {
            foreach (var word in sentence)
            {
                flatSet.Add(word);
            }
        }

        return flatSet;
    }

    /// <summary>
    /// Get the ngrams of dimension n of a certain input sequence of tokens.
    /// </summary>
    /// <param name="sequence">a sequence of tokens</param>
    /// <param name="size">the size of the resulting ngrams</param>
    /// <returns>all the possible ngrams of the given size derivable from the input sequence</returns>
    public static ICollection<StringList> GetNGrams(StringList sequence, int size)
    {
        // NOpenNLP: upstream uses a LinkedList; only insertion order matters here.
        ICollection<StringList> ngrams = new JCG.List<StringList>();
        if (size == -1 || size >= sequence.Count)
        {
            ngrams.Add(sequence);
        }
        else
        {
            string[] ngram = new string[size];
            for (int i = 0; i < sequence.Count - size + 1; i++)
            {
                ngram[0] = sequence.GetToken(i);
                for (int j = 1; j < size; j++)
                {
                    ngram[j] = sequence.GetToken(i + j);
                }

                ngrams.Add(new StringList(ngram));
            }
        }

        return ngrams;
    }

    /// <summary>
    /// Get the ngrams of dimension n of a certain input sequence of tokens.
    /// </summary>
    /// <param name="sequence">a sequence of tokens</param>
    /// <param name="size">the size of the resulting ngrams</param>
    /// <returns>all the possible ngrams of the given size derivable from the input sequence</returns>
    public static ICollection<string[]> GetNGrams(string[] sequence, int size)
    {
        // NOpenNLP: upstream uses a LinkedList; only insertion order matters here.
        ICollection<string[]> ngrams = new JCG.List<string[]>();
        if (size == -1 || size >= sequence.Length)
        {
            ngrams.Add(sequence);
        }
        else
        {
            for (int i = 0; i < sequence.Length - size + 1; i++)
            {
                string[] ngram = new string[size];
                ngram[0] = sequence[i];
                for (int j = 1; j < size; j++)
                {
                    ngram[j] = sequence[i + j];
                }

                ngrams.Add(ngram);
            }
        }

        return ngrams;
    }
}
