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

public class PreviousTwoMapFeatureGeneratorTest
{
    [Test]
    public void TestFeatureGeneration()
    {
        IAdaptiveFeatureGenerator fg = new PreviousTwoMapFeatureGenerator();

        string[] sentence = ["a", "b", "c"];

        List<string> features = [];

        // this should generate the no features
        fg.CreateFeatures(features, sentence, 0, null);
        ClassicAssert.AreEqual(0, features.Count);

        // this should generate the pd=null feature
        fg.CreateFeatures(features, sentence, 1, null);
        ClassicAssert.AreEqual(1, features.Count);
        ClassicAssert.AreEqual("ppd=null,null", features[0]);

        features.Clear();

        // this should generate the pd=1 feature
        fg.UpdateAdaptiveData(sentence, ["1", "2", "3"]);
        fg.CreateFeatures(features, sentence, 1, null);
        ClassicAssert.AreEqual(1, features.Count);
        ClassicAssert.AreEqual("ppd=2,1", features[0]);

        features.Clear();

        // this should generate the pd=null feature again after
        // the adaptive data was cleared
        fg.ClearAdaptiveData();
        fg.CreateFeatures(features, sentence, 1, null);
        ClassicAssert.AreEqual(1, features.Count);
        ClassicAssert.AreEqual("ppd=null,null", features[0]);
    }
}
