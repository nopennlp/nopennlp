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

public class TokenPatternFeatureGeneratorTest
{
    private List<string> features;

    [SetUp]
    public void SetUp()
    {
        features = [];
    }

    [Test]
    public void TestSingleToken()
    {
        string[] testSentence = ["This", "is", "an", "example", "sentence"];
        const int testTokenIndex = 3;

        IAdaptiveFeatureGenerator generator = new TokenPatternFeatureGenerator();

        generator.CreateFeatures(features, testSentence, testTokenIndex, null);
        ClassicAssert.AreEqual(1, features.Count);
        ClassicAssert.AreEqual("st=example", features[0]);
    }

    [Test]
    public void TestSentence()
    {
        string[] testSentence = ["This is an example sentence"];
        const int testTokenIndex = 0;

        IAdaptiveFeatureGenerator generator = new TokenPatternFeatureGenerator();

        generator.CreateFeatures(features, testSentence, testTokenIndex, null);
        ClassicAssert.AreEqual(14, features.Count);
        ClassicAssert.AreEqual("stn=5", features[0]);
        ClassicAssert.AreEqual("pt2=iclc", features[1]);
        ClassicAssert.AreEqual("pt3=iclclc", features[2]);
        ClassicAssert.AreEqual("st=this", features[3]);
        ClassicAssert.AreEqual("pt2=lclc", features[4]);
        ClassicAssert.AreEqual("pt3=lclclc", features[5]);
        ClassicAssert.AreEqual("st=is", features[6]);
        ClassicAssert.AreEqual("pt2=lclc", features[7]);
        ClassicAssert.AreEqual("pt3=lclclc", features[8]);
        ClassicAssert.AreEqual("st=an", features[9]);
        ClassicAssert.AreEqual("pt2=lclc", features[10]);
        ClassicAssert.AreEqual("st=example", features[11]);
        ClassicAssert.AreEqual("st=sentence", features[12]);
        ClassicAssert.AreEqual("pta=iclclclclc", features[13]);
    }
}
