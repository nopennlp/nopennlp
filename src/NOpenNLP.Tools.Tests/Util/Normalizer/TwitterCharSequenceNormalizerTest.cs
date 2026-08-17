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

namespace NOpenNLP.Tools.Util.Normalizer;

public class TwitterCharSequenceNormalizerTest
{
    public TwitterCharSequenceNormalizer normalizer = TwitterCharSequenceNormalizer.GetInstance();

    [Test]
    public void NormalizeHashtag() =>
        ClassicAssert.AreEqual("asdf   2nnfdf", normalizer.Normalize("asdf #hasdk23 2nnfdf"));

    [Test]
    public void NormalizeUser() =>
        ClassicAssert.AreEqual("asdf   2nnfdf", normalizer.Normalize("asdf @hasdk23 2nnfdf"));

    [Test]
    public void NormalizeRT() =>
        ClassicAssert.AreEqual(" 2nnfdf", normalizer.Normalize("RT RT RT 2nnfdf"));

    [Test]
    public void NormalizeLaugh()
    {
        ClassicAssert.AreEqual("ahahah", normalizer.Normalize("ahahahah"));
        ClassicAssert.AreEqual("haha", normalizer.Normalize("hahha"));
        ClassicAssert.AreEqual("haha", normalizer.Normalize("hahaa"));
        ClassicAssert.AreEqual("ahaha", normalizer.Normalize("ahahahahhahahhahahaaaa"));
        ClassicAssert.AreEqual("jaja", normalizer.Normalize("jajjajajaja"));
    }

    [Test]
    public void NormalizeFace()
    {
        ClassicAssert.AreEqual("hello   hello", normalizer.Normalize("hello :-) hello"));
        ClassicAssert.AreEqual("hello   hello", normalizer.Normalize("hello ;) hello"));
        ClassicAssert.AreEqual("  hello", normalizer.Normalize(":) hello"));
        ClassicAssert.AreEqual("hello  ", normalizer.Normalize("hello :P"));
    }
}
