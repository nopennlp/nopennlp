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

using System.IO;
using NOpenNLP.Tools.Support;
using NOpenNLP.Tools.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Formats.Frenchtreebank;

public class ConstitParseSampleStreamTest
{
    private readonly string[] sample1Tokens =
    [
        "L'",
        "autonomie",
        "de",
        "la",
        "Bundesbank",
        ",",
        "la",
        "politique",
        "de",
        "stabilité",
        "qu'",
        "elle",
        "a",
        "fait",
        "prévaloir",
        "(",
        "avec",
        "moins",
        "de",
        "succès",
        "et",
        "de",
        "sévérité",
        "qu'",
        "on",
        "ne",
        "le",
        "dit",
        ",",
        "mais",
        "tout",
        "est",
        "relatif",
        ")",
        ",",
        "est",
        "une",
        "pièce",
        "essentielle",
        "de",
        "la",
        "division",
        "des",
        "pouvoirs",
        "en",
        "Allemagne",
        "."
    ];

    /// <summary>
    /// Reads sample1.xml into a byte array.
    /// </summary>
    /// <returns>byte array containing sample1.xml.</returns>
    private static byte[] GetSample1()
    {
        var @out = new MemoryStream();

        var buffer = new byte[1024];
        using (var sampleIn = TestResources.OpenResource("/opennlp/tools/formats/frenchtreebank/sample1.xml"))
        {
            int length;
            while ((length = sampleIn.Read(buffer, 0, buffer.Length)) > 0)
            {
                @out.Write(buffer, 0, length);
            }
        }

        return @out.ToArray();
    }

    // NOpenNLP: the type argument to CreateObjectStream is spelled out at both call sites
    // below. Upstream passes one byte[] to a byte[]... varargs, giving a stream of a single
    // byte[] sample. Left to infer, C# expands `params T[]` over the byte[] instead and
    // binds T to byte, which is a stream of 30-odd thousand individual bytes -- and which
    // fails to compile on netstandard2.0, where CreateObjectStream constrains T to a
    // reference type.
    [Test]
    public void TestThereIsExactlyOneSent()
    {
        using var samples =
            new ConstitParseSampleStream(ObjectStreamUtils.CreateObjectStream<byte[]?>(GetSample1()));

        ClassicAssert.IsNotNull(samples.Read());
        ClassicAssert.IsNull(samples.Read());
        ClassicAssert.IsNull(samples.Read());
    }

    [Test]
    public void TestTokensAreCorrect()
    {
        using var samples =
            new ConstitParseSampleStream(ObjectStreamUtils.CreateObjectStream<byte[]?>(GetSample1()));

        var p = samples.Read();

        var tagNodes = p!.GetTagNodes();
        var tokens = new string[tagNodes.Length];
        for (int ti = 0; ti < tagNodes.Length; ti++)
        {
            tokens[ti] = tagNodes[ti].CoveredText;
        }

        CollectionAssert.AreEqual(sample1Tokens, tokens);
    }
}
