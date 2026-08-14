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

// This file has been modified from the original Apache OpenNLP 1.9.1 source:
// translated from Java to C# and adapted for .NET. See NOTICE.
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Util.Featuregen;

public class BigramNameFeatureGeneratorTest
{
    private List<string> features;
    private static readonly string[] testSentence = ["This", "is", "an", "example", "sentence"];

    [SetUp]
    public void SetUp()
    {
        features = [];
    }

    [Test]
    public void TestBegin()
    {
        const int testTokenIndex = 0;

        IAdaptiveFeatureGenerator generator = new BigramNameFeatureGenerator();

        generator.CreateFeatures(features, testSentence, testTokenIndex, null);

        ClassicAssert.AreEqual(2, features.Count);
        ClassicAssert.AreEqual("w,nw=This,is", features[0]);
        ClassicAssert.AreEqual("wc,nc=ic,lc", features[1]);
    }

    [Test]
    public void TestMiddle()
    {
        const int testTokenIndex = 2;

        IAdaptiveFeatureGenerator generator = new BigramNameFeatureGenerator();

        generator.CreateFeatures(features, testSentence, testTokenIndex, null);

        ClassicAssert.AreEqual(4, features.Count);
        ClassicAssert.AreEqual("pw,w=is,an", features[0]);
        ClassicAssert.AreEqual("pwc,wc=lc,lc", features[1]);
        ClassicAssert.AreEqual("w,nw=an,example", features[2]);
        ClassicAssert.AreEqual("wc,nc=lc,lc", features[3]);
    }

    [Test]
    public void TestEnd()
    {
        const int testTokenIndex = 4;

        IAdaptiveFeatureGenerator generator = new BigramNameFeatureGenerator();

        generator.CreateFeatures(features, testSentence, testTokenIndex, null);

        ClassicAssert.AreEqual(2, features.Count);
        ClassicAssert.AreEqual("pw,w=example,sentence", features[0]);
        ClassicAssert.AreEqual("pwc,wc=lc,lc", features[1]);
    }

    [Test]
    public void TestShort()
    {
        string[] shortSentence = ["word"];

        const int testTokenIndex = 0;

        IAdaptiveFeatureGenerator generator = new BigramNameFeatureGenerator();

        generator.CreateFeatures(features, shortSentence, testTokenIndex, null);

        ClassicAssert.AreEqual(0, features.Count);
    }
}
