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
using NOpenNLP.Tools.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Postag;

/// <summary>
/// Tests for the <see cref="WordTagSampleStream"/> class.
/// </summary>
public class WordTagSampleStreamTest
{
    [Test]
    public void TestParseSimpleSample()
    {
        ICollection<string> sampleString = new List<string>(1)
        {
            "This_x1 is_x2 a_x3 test_x4 sentence_x5 ._x6"
        };

        using WordTagSampleStream stream =
            new WordTagSampleStream(new CollectionObjectStream<string>(sampleString));

        POSSample? sample = stream.Read();
        string[] words = sample!.Sentence;

        ClassicAssert.AreEqual("This", words[0]);
        ClassicAssert.AreEqual("is", words[1]);
        ClassicAssert.AreEqual("a", words[2]);
        ClassicAssert.AreEqual("test", words[3]);
        ClassicAssert.AreEqual("sentence", words[4]);
        ClassicAssert.AreEqual(".", words[5]);

        string[] tags = sample.Tags;
        ClassicAssert.AreEqual("x1", tags[0]);
        ClassicAssert.AreEqual("x2", tags[1]);
        ClassicAssert.AreEqual("x3", tags[2]);
        ClassicAssert.AreEqual("x4", tags[3]);
        ClassicAssert.AreEqual("x5", tags[4]);
        ClassicAssert.AreEqual("x6", tags[5]);

        ClassicAssert.IsNull(stream.Read());
        stream.Reset();
        ClassicAssert.IsNotNull(stream.Read());
    }
}
