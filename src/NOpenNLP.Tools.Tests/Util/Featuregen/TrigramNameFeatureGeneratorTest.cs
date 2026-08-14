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

// This file has been modified from the original Apache OpenNLP 1.9.4 source:
// translated from Java to C# and adapted for .NET. See NOTICE.
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Util.Featuregen;

public class TrigramNameFeatureGeneratorTest
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

        IAdaptiveFeatureGenerator generator = new TrigramNameFeatureGenerator();

        generator.CreateFeatures(features, testSentence, testTokenIndex, null);

        ClassicAssert.AreEqual(2, features.Count);
        ClassicAssert.AreEqual("w,nw,nnw=This,is,an", features[0]);
        ClassicAssert.AreEqual("wc,nwc,nnwc=ic,lc,lc", features[1]);
    }

    [Test]
    public void TestNextOfBegin()
    {
        const int testTokenIndex = 1;

        IAdaptiveFeatureGenerator generator = new TrigramNameFeatureGenerator();

        generator.CreateFeatures(features, testSentence, testTokenIndex, null);

        ClassicAssert.AreEqual(2, features.Count);
        ClassicAssert.AreEqual("w,nw,nnw=is,an,example", features[0]);
        ClassicAssert.AreEqual("wc,nwc,nnwc=lc,lc,lc", features[1]);
    }

    [Test]
    public void TestMiddle()
    {
        const int testTokenIndex = 2;

        IAdaptiveFeatureGenerator generator = new TrigramNameFeatureGenerator();

        generator.CreateFeatures(features, testSentence, testTokenIndex, null);

        ClassicAssert.AreEqual(4, features.Count);
        ClassicAssert.AreEqual("ppw,pw,w=This,is,an", features[0]);
        ClassicAssert.AreEqual("ppwc,pwc,wc=ic,lc,lc", features[1]);
        ClassicAssert.AreEqual("w,nw,nnw=an,example,sentence", features[2]);
        ClassicAssert.AreEqual("wc,nwc,nnwc=lc,lc,lc", features[3]);
    }

    [Test]
    public void TestEnd()
    {
        const int testTokenIndex = 4;

        IAdaptiveFeatureGenerator generator = new TrigramNameFeatureGenerator();

        generator.CreateFeatures(features, testSentence, testTokenIndex, null);

        ClassicAssert.AreEqual(2, features.Count);
        ClassicAssert.AreEqual("ppw,pw,w=an,example,sentence", features[0]);
        ClassicAssert.AreEqual("ppwc,pwc,wc=lc,lc,lc", features[1]);
    }

    [Test]
    public void TestShort()
    {
        string[] shortSentence = ["I", "know", "it"];

        const int testTokenIndex = 1;

        IAdaptiveFeatureGenerator generator = new TrigramNameFeatureGenerator();

        generator.CreateFeatures(features, shortSentence, testTokenIndex, null);

        ClassicAssert.AreEqual(0, features.Count);
    }
}
