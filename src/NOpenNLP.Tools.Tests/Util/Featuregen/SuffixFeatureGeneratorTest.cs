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

public class SuffixFeatureGeneratorTest
{
    private List<string> features;

    [SetUp]
    public void SetUp()
    {
        features = [];
    }

    [Test]
    public void LengthTest1()
    {
        string[] testSentence = ["This", "is", "an", "example", "sentence"];

        int testTokenIndex = 0;
        int suffixLength = 2;

        IAdaptiveFeatureGenerator generator = new SuffixFeatureGenerator(suffixLength);

        generator.CreateFeatures(features, testSentence, testTokenIndex, null);

        ClassicAssert.AreEqual(2, features.Count);
        ClassicAssert.AreEqual("suf=s", features[0]);
        ClassicAssert.AreEqual("suf=is", features[1]);
    }

    [Test]
    public void LengthTest2()
    {
        string[] testSentence = ["This", "is", "an", "example", "sentence"];

        int testTokenIndex = 3;
        int suffixLength = 5;

        IAdaptiveFeatureGenerator generator = new SuffixFeatureGenerator(suffixLength);

        generator.CreateFeatures(features, testSentence, testTokenIndex, null);

        ClassicAssert.AreEqual(5, features.Count);
        ClassicAssert.AreEqual("suf=e", features[0]);
        ClassicAssert.AreEqual("suf=le", features[1]);
        ClassicAssert.AreEqual("suf=ple", features[2]);
        ClassicAssert.AreEqual("suf=mple", features[3]);
        ClassicAssert.AreEqual("suf=ample", features[4]);
    }

    [Test]
    public void LengthTest3()
    {
        string[] testSentence = ["This", "is", "an", "example", "sentence"];

        int testTokenIndex = 1;
        int suffixLength = 5;

        IAdaptiveFeatureGenerator generator = new SuffixFeatureGenerator(suffixLength);

        generator.CreateFeatures(features, testSentence, testTokenIndex, null);

        ClassicAssert.AreEqual(2, features.Count);
        ClassicAssert.AreEqual("suf=s", features[0]);
        ClassicAssert.AreEqual("suf=is", features[1]);
    }
}
