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
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;

namespace NOpenNLP.Tools.Sentdetect;

/// <summary>
/// Tests for the <see cref="SentenceSample"/> class.
/// </summary>
/// <remarks>
/// NOpenNLP: upstream's <c>testSentenceSampleSerDe</c> round-trips a
/// <c>SentenceSample</c> through <c>ObjectOutputStream</c>/<c>ObjectInputStream</c>.
/// Java object serialization has no .NET counterpart -- <c>BinaryFormatter</c> is
/// removed from .NET 9 and later, which two of this project's three test legs run
/// on -- and <c>SentenceSample</c> is not serializable in the port, so that test is
/// omitted rather than reimplemented against a different mechanism.
/// </remarks>
public class SentenceSampleTest
{
    [Test]
    public void TestRetrievingContent()
    {
        SentenceSample sample = new SentenceSample("1. 2.",
            new Span(0, 2), new Span(3, 5));

        ClassicAssert.AreEqual("1. 2.", sample.Document);
        ClassicAssert.AreEqual(new Span(0, 2), sample.GetSentences()[0]);
        ClassicAssert.AreEqual(new Span(3, 5), sample.GetSentences()[1]);
    }

    [Test]
    public void TestInvalidSpansFailFast()
    {
        Assert.Throws<ArgumentException>((Action)(() => _ = new SentenceSample("1. 2.",
            new Span(0, 2), new Span(5, 7))));
    }

    [Test]
    public void TestEquals()
    {
        ClassicAssert.IsFalse(ReferenceEquals(CreateGoldSample(), CreateGoldSample()));
        ClassicAssert.IsTrue(CreateGoldSample().Equals(CreateGoldSample()));
        ClassicAssert.IsFalse(CreatePredSample().Equals(CreateGoldSample()));
        ClassicAssert.IsFalse(CreatePredSample().Equals(new object()));
    }

    public static SentenceSample CreateGoldSample() =>
        new SentenceSample("1. 2.", new Span(0, 2), new Span(3, 5));

    public static SentenceSample CreatePredSample() =>
        new SentenceSample("1. 2.", new Span(0, 1), new Span(4, 5));
}
