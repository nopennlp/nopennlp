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
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Util.Featuregen;

public class FeatureGeneratorUtilTest
{
    [Test]
    public void Test()
    {
        // digits
        ClassicAssert.AreEqual("2d", FeatureGeneratorUtil.TokenFeature("12"));
        ClassicAssert.AreEqual("4d", FeatureGeneratorUtil.TokenFeature("1234"));
        ClassicAssert.AreEqual("an", FeatureGeneratorUtil.TokenFeature("abcd234"));
        ClassicAssert.AreEqual("dd", FeatureGeneratorUtil.TokenFeature("1234-56"));
        ClassicAssert.AreEqual("ds", FeatureGeneratorUtil.TokenFeature("4/6/2017"));
        ClassicAssert.AreEqual("dc", FeatureGeneratorUtil.TokenFeature("1,234,567"));
        ClassicAssert.AreEqual("dp", FeatureGeneratorUtil.TokenFeature("12.34567"));
        ClassicAssert.AreEqual("num", FeatureGeneratorUtil.TokenFeature("123(456)7890"));

        // letters
        ClassicAssert.AreEqual("lc", FeatureGeneratorUtil.TokenFeature("opennlp"));
        ClassicAssert.AreEqual("sc", FeatureGeneratorUtil.TokenFeature("O"));
        ClassicAssert.AreEqual("ac", FeatureGeneratorUtil.TokenFeature("OPENNLP"));
        ClassicAssert.AreEqual("cp", FeatureGeneratorUtil.TokenFeature("A."));
        ClassicAssert.AreEqual("ic", FeatureGeneratorUtil.TokenFeature("Mike"));
        ClassicAssert.AreEqual("other", FeatureGeneratorUtil.TokenFeature("somethingStupid"));

        // symbols
        ClassicAssert.AreEqual("other", FeatureGeneratorUtil.TokenFeature(","));
        ClassicAssert.AreEqual("other", FeatureGeneratorUtil.TokenFeature("."));
        ClassicAssert.AreEqual("other", FeatureGeneratorUtil.TokenFeature("?"));
        ClassicAssert.AreEqual("other", FeatureGeneratorUtil.TokenFeature("!"));
        ClassicAssert.AreEqual("other", FeatureGeneratorUtil.TokenFeature("#"));
        ClassicAssert.AreEqual("other", FeatureGeneratorUtil.TokenFeature("%"));
        ClassicAssert.AreEqual("other", FeatureGeneratorUtil.TokenFeature("&"));
    }

    [Test]
    public void TestJapanese()
    {
        // Hiragana
        ClassicAssert.AreEqual("jah", FeatureGeneratorUtil.TokenFeature("そういえば"));
        ClassicAssert.AreEqual("jah", FeatureGeneratorUtil.TokenFeature("おーぷん・そ〜す・そふとうぇあ"));
        ClassicAssert.AreEqual("other", FeatureGeneratorUtil.TokenFeature("あぱっち・そふとうぇあ財団"));

        // Katakana
        ClassicAssert.AreEqual("jak", FeatureGeneratorUtil.TokenFeature("ジャパン"));
        ClassicAssert.AreEqual("jak", FeatureGeneratorUtil.TokenFeature("オープン・ソ〜ス・ソフトウェア"));
        ClassicAssert.AreEqual("other", FeatureGeneratorUtil.TokenFeature("アパッチ・ソフトウェア財団"));
    }
}
