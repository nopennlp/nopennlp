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

namespace NOpenNLP.Tools.Stemmer.Snowball;

/// <summary>
/// The stemming algorithms supported by <see cref="SnowballStemmer"/>.
/// </summary>
public enum ALGORITHM
{
    ARABIC,
    DANISH,
    DUTCH,
    CATALAN,
    ENGLISH,
    FINNISH,
    FRENCH,
    GERMAN,
    GREEK,
    HUNGARIAN,
    INDONESIAN,
    IRISH,
    ITALIAN,
    NORWEGIAN,
    PORTER,
    PORTUGUESE,
    ROMANIAN,
    RUSSIAN,
    SPANISH,
    SWEDISH,
    TURKISH
}

/// <summary>
/// A <see cref="IStemmer"/> backed by one of the Snowball stemming algorithms.
/// </summary>
public class SnowballStemmer : IStemmer
{
    private readonly AbstractSnowballStemmer stemmer;
    private readonly int repeat;

    /// <summary>
    /// Creates a stemmer for the given algorithm, applying it <paramref name="repeat"/> times.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="algorithm"/> is not a known algorithm.
    /// </exception>
    public SnowballStemmer(ALGORITHM algorithm, int repeat)
    {
        this.repeat = repeat;

        // NOpenNLP: upstream is a chain of if/else over ALGORITHM.equals, which
        // silently leaves the stemmer field null for an unrecognized value and
        // fails later with a NullPointerException. A switch expression cannot
        // leave a readonly field unassigned, so an out-of-range value is
        // rejected here instead, where the argument that caused it is still in
        // scope. Reaching the default requires casting an undefined value to
        // ALGORITHM, which upstream's chain would not have handled either.
        stemmer = algorithm switch
        {
            ALGORITHM.ARABIC => new ArabicStemmer(),
            ALGORITHM.DANISH => new DanishStemmer(),
            ALGORITHM.DUTCH => new DutchStemmer(),
            ALGORITHM.CATALAN => new CatalanStemmer(),
            ALGORITHM.ENGLISH => new EnglishStemmer(),
            ALGORITHM.FINNISH => new FinnishStemmer(),
            ALGORITHM.FRENCH => new FrenchStemmer(),
            ALGORITHM.GERMAN => new GermanStemmer(),
            ALGORITHM.GREEK => new GreekStemmer(),
            ALGORITHM.HUNGARIAN => new HungarianStemmer(),
            ALGORITHM.INDONESIAN => new IndonesianStemmer(),
            ALGORITHM.IRISH => new IrishStemmer(),
            ALGORITHM.ITALIAN => new ItalianStemmer(),
            ALGORITHM.NORWEGIAN => new NorwegianStemmer(),
            ALGORITHM.PORTER => new PorterStemmer(),
            ALGORITHM.PORTUGUESE => new PortugueseStemmer(),
            ALGORITHM.ROMANIAN => new RomanianStemmer(),
            ALGORITHM.RUSSIAN => new RussianStemmer(),
            ALGORITHM.SPANISH => new SpanishStemmer(),
            ALGORITHM.SWEDISH => new SwedishStemmer(),
            ALGORITHM.TURKISH => new TurkishStemmer(),
            _ => throw new ArgumentException($"Unknown algorithm: {algorithm}", nameof(algorithm))
        };
    }

    /// <summary>
    /// Creates a stemmer for the given algorithm, applying it once.
    /// </summary>
    public SnowballStemmer(ALGORITHM algorithm)
        : this(algorithm, 1)
    {
    }

    public string Stem(string word)
    {
        // NOpenNLP: upstream calls setCurrent/stem/getCurrent on the shared
        // stemmer. The vendored runtime exposes the same buffer through the
        // Current property, so the repeat loop reads back what the previous
        // iteration wrote, exactly as upstream's does.
        stemmer.Current = word;

        for (int i = 0; i < repeat; i++)
        {
            stemmer.Stem();
        }

        return stemmer.Current;
    }
}
