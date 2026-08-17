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

public class PosTaggerFeatureGeneratorTest
{
    private List<string> features;

    private static readonly string[] testSentence = ["This", "is", "an", "example", "sentence"];

    private static readonly string[] testTags = ["DT", "VBZ", "DT", "NN", "NN"];

    [SetUp]
    public void SetUp()
    {
        features = [];
    }

    [Test]
    public void TestBegin()
    {
        const int testTokenIndex = 0;

        IAdaptiveFeatureGenerator generator = new PosTaggerFeatureGenerator();

        generator.CreateFeatures(features, testSentence, testTokenIndex, testTags);

        ClassicAssert.AreEqual(0, features.Count);
    }

    [Test]
    public void TestNext()
    {
        const int testTokenIndex = 1;

        IAdaptiveFeatureGenerator generator = new PosTaggerFeatureGenerator();

        generator.CreateFeatures(features, testSentence, testTokenIndex, testTags);

        ClassicAssert.AreEqual(1, features.Count);
        ClassicAssert.AreEqual("t=DT", features[0]);
    }

    [Test]
    public void TestMiddle()
    {
        const int testTokenIndex = 3;

        IAdaptiveFeatureGenerator generator = new PosTaggerFeatureGenerator();

        generator.CreateFeatures(features, testSentence, testTokenIndex, testTags);

        ClassicAssert.AreEqual(2, features.Count);
        ClassicAssert.AreEqual("t=DT", features[0]);
        ClassicAssert.AreEqual("t2=VBZ,DT", features[1]);
    }
}
