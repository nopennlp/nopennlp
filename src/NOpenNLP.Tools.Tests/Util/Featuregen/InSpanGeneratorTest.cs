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
using NOpenNLP.Tools.Namefind;

namespace NOpenNLP.Tools.Util.Featuregen;

public class InSpanGeneratorTest
{
    private sealed class SimpleSpecificPersonFinder(string theName) : ITokenNameFinder
    {
        public Span[] Find(string[] tokens)
        {
            for (int i = 0; i < tokens.Length; i++)
            {
                if (theName.Equals(tokens[i]))
                {
                    return [new Span(i, i + 1, "person")];
                }
            }

            return [];
        }

        public void ClearAdaptiveData()
        {
        }
    }

    [Test]
    public void Test()
    {
        List<string> features = [];

        string[] testSentence = ["Every", "John", "has", "its", "day", "."];

        IAdaptiveFeatureGenerator generator = new InSpanGenerator("john", new SimpleSpecificPersonFinder("John"));

        generator.CreateFeatures(features, testSentence, 0, null);
        ClassicAssert.AreEqual(0, features.Count);

        features.Clear();
        generator.CreateFeatures(features, testSentence, 1, null);
        ClassicAssert.AreEqual(2, features.Count);
        ClassicAssert.AreEqual("john:w=dic", features[0]);
        ClassicAssert.AreEqual("john:w=dic=John", features[1]);
    }
}
