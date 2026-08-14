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
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Util.Featuregen;

/// <summary>
/// Test for the <see cref="WindowFeatureGenerator"/> class.
/// </summary>
public class WindowFeatureGeneratorTest
{
    private readonly string[] testSentence = ["a", "b", "c", "d",
        "e", "f", "g", "h"]; // NOpenNLP: made readonly

    private List<string> features;

    [SetUp]
    public void SetUp()
    {
        features = [];
    }

    /// <summary>
    /// Tests if the <see cref="WindowFeatureGenerator"/> works as specified, with a previous
    /// and next window size of zero.
    /// </summary>
    [Test]
    public void TestWithoutWindow()
    {
        IAdaptiveFeatureGenerator windowFeatureGenerator = new WindowFeatureGenerator(
            new IdentityFeatureGenerator(), 0, 0);

        int testTokenIndex = 2;

        windowFeatureGenerator.CreateFeatures(features, testSentence, testTokenIndex, null);

        ClassicAssert.AreEqual(1, features.Count);

        ClassicAssert.AreEqual("c", features[0]);
    }

    [Test]
    public void TestWindowSizeOne()
    {
        IAdaptiveFeatureGenerator windowFeatureGenerator = new WindowFeatureGenerator(
            new IdentityFeatureGenerator(), 1, 1);

        int testTokenIndex = 2;

        windowFeatureGenerator.CreateFeatures(features, testSentence, testTokenIndex, null);

        ClassicAssert.AreEqual(3, features.Count);

        ClassicAssert.AreEqual("c", features[0]);
        ClassicAssert.AreEqual("p1b", features[1]);
        ClassicAssert.AreEqual("n1d", features[2]);
    }

    [Test]
    public void TestWindowAtBeginOfSentence()
    {
        IAdaptiveFeatureGenerator windowFeatureGenerator = new WindowFeatureGenerator(
            new IdentityFeatureGenerator(), 1, 0);

        int testTokenIndex = 0;
        windowFeatureGenerator.CreateFeatures(features, testSentence, testTokenIndex, null);
        ClassicAssert.AreEqual(1, features.Count);
        ClassicAssert.AreEqual("a", features[0]);
    }

    [Test]
    public void TestWindowAtEndOfSentence()
    {
        IAdaptiveFeatureGenerator windowFeatureGenerator = new WindowFeatureGenerator(
            new IdentityFeatureGenerator(), 0, 1);

        int testTokenIndex = testSentence.Length - 1;
        windowFeatureGenerator.CreateFeatures(features, testSentence, testTokenIndex, null);
        ClassicAssert.AreEqual(1, features.Count);
        ClassicAssert.AreEqual("h", features[0]);
    }

    /// <summary>
    /// Tests for a window size of previous and next 2 if the features are correct.
    /// </summary>
    [Test]
    public void TestForCorrectFeatures()
    {
        IAdaptiveFeatureGenerator windowFeatureGenerator = new WindowFeatureGenerator(
            new IdentityFeatureGenerator(), 2, 2);

        int testTokenIndex = 3;
        windowFeatureGenerator.CreateFeatures(features, testSentence, testTokenIndex, null);
        ClassicAssert.AreEqual(5, features.Count);

        ClassicAssert.AreEqual("d", features[0]);
        ClassicAssert.AreEqual("p1c", features[1]);
        ClassicAssert.AreEqual("p2b", features[2]);
        ClassicAssert.AreEqual("n1e", features[3]);
        ClassicAssert.AreEqual("n2f", features[4]);
    }
}
