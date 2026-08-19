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

public class WhitespaceTokenStreamTest
{
    /// <summary>
    /// Tests for the <see cref="WhitespaceTokenStream"/> class.
    /// </summary>
    [Test]
    public void TestWhitespace()
    {
        string text = " a b c  d    e        f     ";
        IObjectStream<TokenSample?> sampleStream = new TokenSampleStream(
            ObjectStreamUtils.CreateObjectStream(text));
        WhitespaceTokenStream tokenStream = new WhitespaceTokenStream(sampleStream);
        string? read = tokenStream.Read();
        ClassicAssert.AreEqual("a b c d e f", read);
    }

    [Test]
    public void TestSeparatedString()
    {
        string text = " a b<SPLIT>c   d<SPLIT>e   ";
        IObjectStream<TokenSample?> sampleStream = new TokenSampleStream(
            ObjectStreamUtils.CreateObjectStream(text));
        WhitespaceTokenStream tokenStream = new WhitespaceTokenStream(sampleStream);
        string? read = tokenStream.Read();
        ClassicAssert.AreEqual("a b c d e", read);
    }

    /// <summary>
    /// Tests for the <see cref="TokenizerStream"/> correctly tokenizes whitespace separated tokens.
    /// </summary>
    [Test]
    public void TestTokenizerStream()
    {
        string text = " a b c  d    e      ";
        WhitespaceTokenizer instance = WhitespaceTokenizer.INSTANCE;
        TokenizerStream stream = new TokenizerStream(instance, ObjectStreamUtils.CreateObjectStream(text));
        TokenSample read = stream.Read()!;
        Span[] tokenSpans = read.TokenSpans;

        ClassicAssert.AreEqual(5, tokenSpans.Length);

        // NOpenNLP: GetCoveredText takes an ICharSequence and returns one, so the
        // string is adapted on the way in and back on the way out.
        ClassicAssert.AreEqual("a", tokenSpans[0].GetCoveredText(read.Text.AsCharSequence()).ToString());
        ClassicAssert.AreEqual(new Span(1, 2), tokenSpans[0]);

        ClassicAssert.AreEqual("b", tokenSpans[1].GetCoveredText(read.Text.AsCharSequence()).ToString());
        ClassicAssert.AreEqual(new Span(3, 4), tokenSpans[1]);

        ClassicAssert.AreEqual("c", tokenSpans[2].GetCoveredText(read.Text.AsCharSequence()).ToString());
        ClassicAssert.AreEqual(new Span(5, 6), tokenSpans[2]);

        ClassicAssert.AreEqual("d", tokenSpans[3].GetCoveredText(read.Text.AsCharSequence()).ToString());
        ClassicAssert.AreEqual(new Span(8, 9), tokenSpans[3]);

        ClassicAssert.AreEqual("e", tokenSpans[4].GetCoveredText(read.Text.AsCharSequence()).ToString());
        ClassicAssert.AreEqual(new Span(13, 14), tokenSpans[4]);
    }
}
