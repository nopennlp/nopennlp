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

public class SentenceFeatureGeneratorTest
{
    private List<string> features;
    private static readonly string[] testSentence = ["This", "is", "an", "example", "sentence"];
    private static readonly string[] testShort = ["word"];

    [SetUp]
    public void SetUp()
    {
        features = [];
    }

    [Test]
    public void TestTT()
    {
        IAdaptiveFeatureGenerator generator = new SentenceFeatureGenerator(true, true);

        generator.CreateFeatures(features, testSentence, 2, null);
        ClassicAssert.AreEqual(0, features.Count);

        generator.CreateFeatures(features, testSentence, 0, null);
        ClassicAssert.AreEqual(1, features.Count);
        ClassicAssert.AreEqual("S=begin", features[0]);

        features.Clear();

        generator.CreateFeatures(features, testSentence, testSentence.Length - 1, null);
        ClassicAssert.AreEqual(1, features.Count);
        ClassicAssert.AreEqual("S=end", features[0]);

        features.Clear();

        generator.CreateFeatures(features, testShort, 0, null);
        ClassicAssert.AreEqual(2, features.Count);
        ClassicAssert.AreEqual("S=begin", features[0]);
        ClassicAssert.AreEqual("S=end", features[1]);
    }

    [Test]
    public void TestTF()
    {
        IAdaptiveFeatureGenerator generator = new SentenceFeatureGenerator(true, false);

        generator.CreateFeatures(features, testSentence, 2, null);
        ClassicAssert.AreEqual(0, features.Count);

        generator.CreateFeatures(features, testSentence, 0, null);
        ClassicAssert.AreEqual(1, features.Count);
        ClassicAssert.AreEqual("S=begin", features[0]);

        features.Clear();

        generator.CreateFeatures(features, testSentence, testSentence.Length - 1, null);
        ClassicAssert.AreEqual(0, features.Count);

        features.Clear();

        generator.CreateFeatures(features, testShort, 0, null);
        ClassicAssert.AreEqual(1, features.Count);
        ClassicAssert.AreEqual("S=begin", features[0]);
    }

    [Test]
    public void TestFT()
    {
        IAdaptiveFeatureGenerator generator = new SentenceFeatureGenerator(false, true);

        generator.CreateFeatures(features, testSentence, 2, null);
        ClassicAssert.AreEqual(0, features.Count);

        generator.CreateFeatures(features, testSentence, 0, null);
        ClassicAssert.AreEqual(0, features.Count);

        generator.CreateFeatures(features, testSentence, testSentence.Length - 1, null);
        ClassicAssert.AreEqual(1, features.Count);
        ClassicAssert.AreEqual("S=end", features[0]);

        features.Clear();

        generator.CreateFeatures(features, testShort, 0, null);
        ClassicAssert.AreEqual(1, features.Count);
        ClassicAssert.AreEqual("S=end", features[0]);
    }

    [Test]
    public void TestFF()
    {
        IAdaptiveFeatureGenerator generator = new SentenceFeatureGenerator(false, false);

        generator.CreateFeatures(features, testSentence, 2, null);
        ClassicAssert.AreEqual(0, features.Count);

        generator.CreateFeatures(features, testSentence, 0, null);
        ClassicAssert.AreEqual(0, features.Count);

        generator.CreateFeatures(features, testSentence, testSentence.Length - 1, null);
        ClassicAssert.AreEqual(0, features.Count);

        generator.CreateFeatures(features, testShort, 0, null);
        ClassicAssert.AreEqual(0, features.Count);
    }
}
