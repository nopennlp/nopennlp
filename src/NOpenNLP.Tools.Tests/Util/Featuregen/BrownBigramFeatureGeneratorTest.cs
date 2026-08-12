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
using System.IO;
using NOpenNLP.Tools.Support;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Util.Featuregen;

public class BrownBigramFeatureGeneratorTest
{
    private IAdaptiveFeatureGenerator generator;

    [SetUp]
    public void Setup()
    {
        // NOpenNLP: upstream wraps the resource in a ResourceAsStreamFactory, which
        // is part of the unported formats package. BrownCluster takes the stream
        // directly, so the resource is opened here.
        using Stream stream = TestResources.OpenResource("/opennlp/tools/formats/brown-cluster.txt");

        BrownCluster brownCluster = new BrownCluster(stream);

        generator = new BrownBigramFeatureGenerator(brownCluster);
    }

    [Test]
    public void CreateFeaturesTest()
    {
        string[] tokens = ["he", "went", "with", "you"];

        List<string> features = [];
        generator.CreateFeatures(features, tokens, 3, null);

        ClassicAssert.AreEqual(2, features.Count);
        ClassicAssert.IsTrue(features.Contains("pbrowncluster,browncluster=0101,0010"));
        ClassicAssert.IsTrue(features.Contains("pbrowncluster,browncluster=01010,00101"));
    }

    [Test]
    public void CreateFeaturesSuccessiveTokensTest()
    {
        string[] testSentence = ["he", "went", "with", "you", "in", "town"];

        List<string> features = [];
        generator.CreateFeatures(features, testSentence, 3, null);

        ClassicAssert.AreEqual(3, features.Count);
        ClassicAssert.IsTrue(features.Contains("pbrowncluster,browncluster=0101,0010"));
        ClassicAssert.IsTrue(features.Contains("pbrowncluster,browncluster=01010,00101"));
        ClassicAssert.IsTrue(features.Contains("browncluster,nbrowncluster=0010,0000"));
    }

    [Test]
    public void NoFeaturesTest()
    {
        string[] testSentence = ["he", "went", "with", "you"];

        List<string> features = [];
        generator.CreateFeatures(features, testSentence, 0, null);

        ClassicAssert.AreEqual(0, features.Count);
    }
}
