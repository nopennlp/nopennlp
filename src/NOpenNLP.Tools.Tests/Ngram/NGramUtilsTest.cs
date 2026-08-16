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
using NOpenNLP.Tools.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Ngram;

/// <summary>
/// Tests for <see cref="NGramUtils"/>.
/// </summary>
public class NGramUtilsTest
{
    [Test]
    public void TestBigramMLProbability()
    {
        ICollection<StringList> set = new List<StringList>
        {
            new StringList("<s>", "I", "am", "Sam", "</s>"),
            new StringList("<s>", "Sam", "I", "am", "</s>"),
            new StringList("<s>", "I", "do", "not", "like", "green", "eggs", "and", "ham", "</s>"),
            new StringList("")
        };
        double d = NGramUtils.CalculateBigramMLProbability("<s>", "I", set);
        ClassicAssert.AreEqual(0.6666666666666666d, d);
        d = NGramUtils.CalculateBigramMLProbability("Sam", "</s>", set);
        ClassicAssert.AreEqual(0.5d, d);
        d = NGramUtils.CalculateBigramMLProbability("<s>", "Sam", set);
        ClassicAssert.AreEqual(0.3333333333333333d, d);
    }

    [Test]
    public void TestTrigramMLProbability()
    {
        ICollection<StringList> set = new List<StringList>
        {
            new StringList("<s>", "I", "am", "Sam", "</s>"),
            new StringList("<s>", "Sam", "I", "am", "</s>"),
            new StringList("<s>", "I", "do", "not", "like", "green", "eggs", "and", "ham", "</s>"),
            new StringList("")
        };
        double d = NGramUtils.CalculateTrigramMLProbability("I", "am", "Sam", set);
        ClassicAssert.AreEqual(0.5d, d);
        d = NGramUtils.CalculateTrigramMLProbability("Sam", "I", "am", set);
        ClassicAssert.AreEqual(1d, d);
    }

    [Test]
    public void TestNgramMLProbability()
    {
        ICollection<StringList> set = new List<StringList>
        {
            new StringList("<s>", "I", "am", "Sam", "</s>"),
            new StringList("<s>", "Sam", "I", "am", "</s>"),
            new StringList("<s>", "I", "do", "not", "like", "green", "eggs", "and", "ham", "</s>"),
            new StringList("")
        };
        double d = NGramUtils.CalculateNgramMLProbability(new StringList("I", "am", "Sam"), set);
        ClassicAssert.AreEqual(0.5d, d);
        d = NGramUtils.CalculateNgramMLProbability(new StringList("Sam", "I", "am"), set);
        ClassicAssert.AreEqual(1d, d);
    }

    [Test]
    public void TestLinearInterpolation()
    {
        ICollection<StringList> set = new List<StringList>
        {
            new StringList("the", "green", "book", "STOP"),
            new StringList("my", "blue", "book", "STOP"),
            new StringList("his", "green", "house", "STOP"),
            new StringList("book", "STOP")
        };
        double lambda = 1d / 3d;
        double d = NGramUtils.CalculateTrigramLinearInterpolationProbability("the", "green",
            "book", set, lambda, lambda, lambda);
        ClassicAssert.AreEqual(0.5714285714285714d, d, "wrong result");
    }

    [Test]
    public void TestLinearInterpolation2()
    {
        ICollection<StringList> set = new List<StringList>
        {
            new StringList("D", "N", "V", "STOP"),
            new StringList("D", "N", "V", "STOP")
        };
        double lambda = 1d / 3d;
        double d = NGramUtils.CalculateTrigramLinearInterpolationProbability("N", "V",
            "STOP", set, lambda, lambda, lambda);
        ClassicAssert.AreEqual(0.75d, d, "wrong result");
    }

    [Test]
    public void TestGetNGrams()
    {
        var nGrams = NGramUtils.GetNGrams(new StringList("I", "saw", "brown", "fox"), 2);
        ClassicAssert.AreEqual(3, nGrams.Count);
        nGrams = NGramUtils.GetNGrams(new StringList("I", "saw", "brown", "fox"), 3);
        ClassicAssert.AreEqual(2, nGrams.Count);
    }
}
