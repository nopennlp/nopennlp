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
using J2N.Text;
using NOpenNLP.Tools.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Tokenize;

/// <summary>
/// Tests for the <see cref="TokenSampleStream"/> class.
/// </summary>
public class TokenSampleStreamTest
{
    /// <summary>
    /// Tests if the <see cref="TokenSample"/> correctly tokenizes tokens which
    /// are separated by a whitespace.
    /// </summary>
    [Test]
    public void TestParsingWhitespaceSeparatedTokens()
    {
        string sampleTokens = "Slave to the wage";

        IObjectStream<TokenSample?> sampleTokenStream = new TokenSampleStream(
            ObjectStreamUtils.CreateObjectStream(sampleTokens));

        TokenSample tokenSample = sampleTokenStream.Read()!;

        Span[] tokenSpans = tokenSample.TokenSpans;

        ClassicAssert.AreEqual(4, tokenSpans.Length);

        // NOpenNLP: GetCoveredText takes an ICharSequence and returns one, so the
        // string is adapted on the way in and back on the way out.
        ClassicAssert.AreEqual("Slave", tokenSpans[0].GetCoveredText(sampleTokens.AsCharSequence()).ToString());
        ClassicAssert.AreEqual("to", tokenSpans[1].GetCoveredText(sampleTokens.AsCharSequence()).ToString());
        ClassicAssert.AreEqual("the", tokenSpans[2].GetCoveredText(sampleTokens.AsCharSequence()).ToString());
        ClassicAssert.AreEqual("wage", tokenSpans[3].GetCoveredText(sampleTokens.AsCharSequence()).ToString());
    }

    /// <summary>
    /// Tests if the <see cref="TokenSample"/> correctly tokenizes tokens which
    /// are separated by the split chars.
    /// </summary>
    [Test]
    public void TestParsingSeparatedString()
    {
        string sampleTokens = "a<SPLIT>b<SPLIT>c<SPLIT>d";

        IObjectStream<TokenSample?> sampleTokenStream = new TokenSampleStream(
            ObjectStreamUtils.CreateObjectStream(sampleTokens));

        TokenSample tokenSample = sampleTokenStream.Read()!;

        Span[] tokenSpans = tokenSample.TokenSpans;

        ClassicAssert.AreEqual(4, tokenSpans.Length);

        ClassicAssert.AreEqual("a", tokenSpans[0].GetCoveredText(tokenSample.Text.AsCharSequence()).ToString());
        ClassicAssert.AreEqual(new Span(0, 1), tokenSpans[0]);

        ClassicAssert.AreEqual("b", tokenSpans[1].GetCoveredText(tokenSample.Text.AsCharSequence()).ToString());
        ClassicAssert.AreEqual(new Span(1, 2), tokenSpans[1]);

        ClassicAssert.AreEqual("c", tokenSpans[2].GetCoveredText(tokenSample.Text.AsCharSequence()).ToString());
        ClassicAssert.AreEqual(new Span(2, 3), tokenSpans[2]);

        ClassicAssert.AreEqual("d", tokenSpans[3].GetCoveredText(tokenSample.Text.AsCharSequence()).ToString());
        ClassicAssert.AreEqual(new Span(3, 4), tokenSpans[3]);
    }

    /// <summary>
    /// Tests if the <see cref="TokenSample"/> correctly tokenizes tokens which
    /// are separated by whitespace and by the split chars.
    /// </summary>
    [Test]
    public void TestParsingWhitespaceAndSeparatedString()
    {
        string sampleTokens = "a b<SPLIT>c d<SPLIT>e";

        using (IObjectStream<TokenSample?> sampleTokenStream = new TokenSampleStream(
            ObjectStreamUtils.CreateObjectStream(sampleTokens)))
        {
            TokenSample tokenSample = sampleTokenStream.Read()!;

            Span[] tokenSpans = tokenSample.TokenSpans;

            ClassicAssert.AreEqual(5, tokenSpans.Length);

            ClassicAssert.AreEqual("a", tokenSpans[0].GetCoveredText(tokenSample.Text.AsCharSequence()).ToString());
            ClassicAssert.AreEqual("b", tokenSpans[1].GetCoveredText(tokenSample.Text.AsCharSequence()).ToString());
            ClassicAssert.AreEqual("c", tokenSpans[2].GetCoveredText(tokenSample.Text.AsCharSequence()).ToString());
            ClassicAssert.AreEqual("d", tokenSpans[3].GetCoveredText(tokenSample.Text.AsCharSequence()).ToString());
            ClassicAssert.AreEqual("e", tokenSpans[4].GetCoveredText(tokenSample.Text.AsCharSequence()).ToString());
        }
    }
}
