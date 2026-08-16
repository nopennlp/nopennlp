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

public class UrlCharSequenceNormalizerTest
{
    public UrlCharSequenceNormalizer normalizer = UrlCharSequenceNormalizer.GetInstance();

    [Test]
    public void NormalizeUrl()
    {
        ClassicAssert.AreEqual(
            "asdf   2nnfdf", normalizer.Normalize("asdf http://asdf.com/dfa/cxs 2nnfdf"));

        ClassicAssert.AreEqual(
            "asdf   2nnfdf  ", normalizer.Normalize("asdf http://asdf.com/dfa/cx" +
                "s 2nnfdf http://asdf.com/dfa/cxs"));
    }

    [Test]
    public void NormalizeEmail()
    {
        ClassicAssert.AreEqual(
            "asdf   2nnfdf", normalizer.Normalize("asdf asd.fdfa@hasdk23.com.br 2nnfdf"));
        ClassicAssert.AreEqual(
            "asdf   2nnfdf  ", normalizer.Normalize("asdf asd.fdfa@hasdk23.com.br" +
                " 2nnfdf asd.fdfa@hasdk23.com.br"));
    }
}
