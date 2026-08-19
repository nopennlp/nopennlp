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
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Lemmatizer;

public class LemmaSampleTest
{
    [Test]
    public void TestParameterValidation()
    {
        // NOpenNLP: upstream declares @Test(expected = IllegalArgumentException.class);
        // ArgumentException is the .NET counterpart.
        Assert.Throws<ArgumentException>((Action)(() =>
            new LemmaSample([""], [""], ["test", "one element to much"])));
    }

    private static string[] CreateSentence() =>
        ["Forecasts", "for", "the", "trade", "figures", "range", "widely", "."];

    private static string[] CreateTags() =>
        ["NNS", "IN", "DT", "NN", "NNS", "VBP", "RB", "."];

    private static string[] CreateLemmas() =>
        ["Forecast", "for", "the", "trade", "figure", "range", "widely", "."];

    // NOpenNLP: upstream's testLemmaSampleSerDe round-trips the sample through
    // Java object serialization. LemmaSample does not implement a .NET
    // equivalent of java.io.Serializable (see the note on the ported class), so
    // there is nothing to exercise and the test is omitted.

    [Test]
    public void TestRetrievingContent()
    {
        LemmaSample sample = new LemmaSample(CreateSentence(), CreateTags(), CreateLemmas());

        CollectionAssert.AreEqual(CreateSentence(), sample.Tokens);
        CollectionAssert.AreEqual(CreateTags(), sample.Tags);
        CollectionAssert.AreEqual(CreateLemmas(), sample.Lemmas);
    }

    [Test]
    public void TestToString()
    {
        LemmaSample sample = new LemmaSample(CreateSentence(), CreateTags(), CreateLemmas());
        string[] sentence = CreateSentence();
        string[] tags = CreateTags();
        string[] lemmas = CreateLemmas();

        using StringReader reader = new StringReader(sample.ToString());
        for (int i = 0; i < sentence.Length; i++)
        {
            string line = reader.ReadLine()!;
            string[] parts = line.Split('\t');
            ClassicAssert.AreEqual(3, parts.Length);
            ClassicAssert.AreEqual(sentence[i], parts[0]);
            ClassicAssert.AreEqual(tags[i], parts[1]);
            ClassicAssert.AreEqual(lemmas[i], parts[2]);
        }
    }

    [Test]
    public void TestEquals()
    {
        ClassicAssert.IsFalse(ReferenceEquals(CreateGoldSample(), CreateGoldSample()));
        ClassicAssert.IsTrue(CreateGoldSample().Equals(CreateGoldSample()));
        ClassicAssert.IsFalse(CreatePredSample().Equals(CreateGoldSample()));
        ClassicAssert.IsFalse(CreatePredSample().Equals(new object()));
    }

    public static LemmaSample CreateGoldSample() =>
        new LemmaSample(CreateSentence(), CreateTags(), CreateLemmas());

    public static LemmaSample CreatePredSample()
    {
        string[] lemmas = CreateLemmas();
        lemmas[5] = "figure";
        return new LemmaSample(CreateSentence(), CreateTags(), lemmas);
    }
}
