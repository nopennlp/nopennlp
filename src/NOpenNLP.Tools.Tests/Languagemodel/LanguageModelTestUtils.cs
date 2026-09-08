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
using System.Collections.Generic;
using NOpenNLP.Tools.Ngram;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Languagemodel;

/// <summary>
/// Utility class for language models tests.
/// </summary>
/// <remarks>
/// NOpenNLP: upstream marks this class @Ignore so JUnit does not treat the utility
/// class as a suite. NUnit only collects classes with test methods, so there is
/// nothing to suppress here and no [Ignore] is applied.
/// </remarks>
public static class LanguageModelTestUtils
{
    private static readonly Random r = new();

    private static readonly char[] chars = ['a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j'];

    public static ICollection<string[]> GenerateRandomVocabulary(int size)
    {
        LinkedList<string[]> vocabulary = [];
        for (int i = 0; i < size; i++)
        {
            string[] sentence = GenerateRandomSentence();
            vocabulary.AddLast(sentence);
        }
        return vocabulary;
    }

    public static string[] GenerateRandomSentence()
    {
        int dimension = r.Next(10) + 1;
        string[] sentence = new string[dimension];
        for (int j = 0; j < dimension; j++)
        {
            int i = r.Next(10);
            char c = chars[i];
            sentence[j] = c + "-" + c + "-" + c;
        }
        return sentence;
    }

    /// <summary>
    /// Computes the perplexity of the given language model over the test set.
    /// </summary>
    /// <remarks>
    /// NOpenNLP: upstream accumulates the product in a java.math.BigDecimal with
    /// MathContext.DECIMAL128. .NET has no BigDecimal, so this accumulates in
    /// double. The result feeds a log and an exponentiation that both collapse to
    /// double anyway, and the over/underflow guard below is what the extra
    /// precision was protecting against.
    /// </remarks>
    public static double GetPerplexity(ILanguageModel lm, ICollection<string[]> testSet, int ngramSize)
    {
        double perplexity = 1d;

        foreach (var sentence in testSet)
        {
            foreach (var ngram in NGramUtils.GetNGrams(sentence, ngramSize))
            {
                double ngramProbability = lm.CalculateProbability(ngram);
                perplexity *= 1d / ngramProbability;
            }
        }

        double p = Math.Log(perplexity);
        if (double.IsInfinity(p) || double.IsNaN(p))
        {
            return double.PositiveInfinity; // over/underflow -> too high perplexity
        }

        return Math.Pow(Math.E, p / testSet.Count);
    }
}
