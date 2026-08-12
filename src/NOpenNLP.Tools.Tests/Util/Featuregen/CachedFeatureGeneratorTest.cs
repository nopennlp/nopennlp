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

/// <summary>
/// Test for the <see cref="CachedFeatureGenerator"/> class.
/// </summary>
public class CachedFeatureGeneratorTest
{
    private readonly IAdaptiveFeatureGenerator[] identityGenerator = // NOpenNLP: made readonly
        [new IdentityFeatureGenerator()];

    private string[] testSentence1;

    private string[] testSentence2;

    private List<string> features;

    [SetUp]
    public void SetUp()
    {
        testSentence1 = ["a1", "b1", "c1", "d1"];

        testSentence2 = ["a2", "b2", "c2", "d2"];

        features = [];
    }

    /// <summary>
    /// Tests if cache works for one sentence and two different token indexes.
    /// </summary>
    [Test]
    public void TestCachingOfSentence()
    {
        CachedFeatureGenerator generator = new CachedFeatureGenerator(identityGenerator);

        int testIndex = 0;

        // after this call features are cached for testIndex
        generator.CreateFeatures(features, testSentence1, testIndex, null);

        ClassicAssert.AreEqual(1, generator.NumberOfCacheMisses);
        ClassicAssert.AreEqual(0, generator.NumberOfCacheHits);

        ClassicAssert.IsTrue(features.Contains(testSentence1[testIndex]));

        features.Clear();

        // check if features are really cached

        string expectedToken = testSentence1[testIndex];

        testSentence1[testIndex] = null;

        generator.CreateFeatures(features, testSentence1, testIndex, null);

        ClassicAssert.AreEqual(1, generator.NumberOfCacheMisses);
        ClassicAssert.AreEqual(1, generator.NumberOfCacheHits);

        ClassicAssert.IsTrue(features.Contains(expectedToken));
        ClassicAssert.AreEqual(1, features.Count);

        features.Clear();

        // try caching with an other index

        int testIndex2 = testIndex + 1;

        generator.CreateFeatures(features, testSentence1, testIndex2, null);

        ClassicAssert.AreEqual(2, generator.NumberOfCacheMisses);
        ClassicAssert.AreEqual(1, generator.NumberOfCacheHits);
        ClassicAssert.IsTrue(features.Contains(testSentence1[testIndex2]));

        features.Clear();

        // now check if cache still contains feature for testIndex

        generator.CreateFeatures(features, testSentence1, testIndex, null);

        ClassicAssert.IsTrue(features.Contains(expectedToken));
    }

    /// <summary>
    /// Tests if the cache was cleared after the sentence changed.
    /// </summary>
    [Test]
    public void TestCacheClearAfterSentenceChange()
    {
        CachedFeatureGenerator generator = new CachedFeatureGenerator(identityGenerator);

        int testIndex = 0;

        // use generator with sentence 1
        generator.CreateFeatures(features, testSentence1, testIndex, null);

        features.Clear();

        // use another sentence but same index
        generator.CreateFeatures(features, testSentence2, testIndex, null);

        ClassicAssert.AreEqual(2, generator.NumberOfCacheMisses);
        ClassicAssert.AreEqual(0, generator.NumberOfCacheHits);

        ClassicAssert.IsTrue(features.Contains(testSentence2[testIndex]));
        ClassicAssert.AreEqual(1, features.Count);

        features.Clear();

        // check if features are really cached
        string expectedToken = testSentence2[testIndex];

        testSentence2[testIndex] = null;

        generator.CreateFeatures(features, testSentence2, testIndex, null);

        ClassicAssert.IsTrue(features.Contains(expectedToken));
        ClassicAssert.AreEqual(1, features.Count);
    }
}
