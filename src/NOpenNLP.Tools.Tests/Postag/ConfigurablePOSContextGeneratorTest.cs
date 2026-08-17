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

using NUnit.Framework;
using NUnit.Framework.Legacy;
using NOpenNLP.Tools.Util.Featuregen;

namespace NOpenNLP.Tools.Postag;

public class ConfigurablePOSContextGeneratorTest
{
    private void TestContextGeneration(int cacheSize)
    {
        IAdaptiveFeatureGenerator fg = new TokenFeatureGenerator();
        var cg = new ConfigurablePOSContextGenerator(cacheSize, fg);

        string[] tokens = ["a", "b", "c", "d", "e"];
        string[] tags = ["t_a", "t_b", "t_c", "t_d", "t_e"];

        cg.GetContext(0, tokens, tags, null);

        ClassicAssert.AreEqual(1, cg.GetContext(0, tokens, tags, null).Length);
        ClassicAssert.AreEqual("w=a", cg.GetContext(0, tokens, tags, null)[0]);
        ClassicAssert.AreEqual("w=b", cg.GetContext(1, tokens, tags, null)[0]);
        ClassicAssert.AreEqual("w=c", cg.GetContext(2, tokens, tags, null)[0]);
        ClassicAssert.AreEqual("w=d", cg.GetContext(3, tokens, tags, null)[0]);
        ClassicAssert.AreEqual("w=e", cg.GetContext(4, tokens, tags, null)[0]);
    }

    [Test]
    public void TestWithoutCache()
    {
        TestContextGeneration(0);
    }

    [Test]
    public void TestWithCache()
    {
        TestContextGeneration(3);
    }
}
