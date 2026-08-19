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
using NOpenNLP.Tools.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Tokenize;

public class TokenSampleTest
{
    public static TokenSample CreateGoldSample() =>
        new TokenSample("A test.", [new Span(0, 1), new Span(2, 6)]);

    public static TokenSample CreatePredSample() =>
        new TokenSample("A test.", [new Span(0, 3), new Span(2, 6)]);

    public static TokenSample CreatePredSilverSample() =>
        new TokenSample("A t st.", [new Span(0, 1), new Span(2, 6)]);

    [Test]
    public void TestRetrievingContent()
    {
        string sentence = "A test";

        TokenSample sample = new TokenSample(sentence, [new Span(0, 1), new Span(2, 6)]);

        ClassicAssert.AreEqual("A test", sample.Text);

        ClassicAssert.AreEqual(new Span(0, 1), sample.TokenSpans[0]);
        ClassicAssert.AreEqual(new Span(2, 6), sample.TokenSpans[1]);
    }

    // NOpenNLP: upstream's testTokenSampleSerDe round-trips the sample through
    // Java object serialization. TokenSample does not implement a .NET
    // equivalent of java.io.Serializable (see the note on the ported class), so
    // there is nothing to exercise and the test is omitted.

    [Test]
    public void TestCreationWithDetokenizer()
    {
        IDetokenizer detokenizer = DictionaryDetokenizerTest.CreateLatinDetokenizer();

        string[] tokens = [
            "start",
            "(", // move right
            ")", // move left
            "end",
            ".", // move left
            "hyphen",
            "-", // move both
            "string",
            "."
        ];

        TokenSample a = new TokenSample(detokenizer, tokens);

        ClassicAssert.AreEqual("start () end. hyphen-string.", a.Text);
        ClassicAssert.AreEqual("start (" + TokenSample.DEFAULT_SEPARATOR_CHARS + ") end"
            + TokenSample.DEFAULT_SEPARATOR_CHARS + "."
            + " hyphen" + TokenSample.DEFAULT_SEPARATOR_CHARS + "-" + TokenSample.DEFAULT_SEPARATOR_CHARS
            + "string" + TokenSample.DEFAULT_SEPARATOR_CHARS + ".", a.ToString());

        ClassicAssert.AreEqual(9, a.TokenSpans.Length);

        ClassicAssert.AreEqual(new Span(0, 5), a.TokenSpans[0]);
        ClassicAssert.AreEqual(new Span(6, 7), a.TokenSpans[1]);
        ClassicAssert.AreEqual(new Span(7, 8), a.TokenSpans[2]);
        ClassicAssert.AreEqual(new Span(9, 12), a.TokenSpans[3]);
        ClassicAssert.AreEqual(new Span(12, 13), a.TokenSpans[4]);

        ClassicAssert.AreEqual(new Span(14, 20), a.TokenSpans[5]);
        ClassicAssert.AreEqual(new Span(20, 21), a.TokenSpans[6]);
        ClassicAssert.AreEqual(new Span(21, 27), a.TokenSpans[7]);
        ClassicAssert.AreEqual(new Span(27, 28), a.TokenSpans[8]);
    }

    [Test]
    public void TestEquals()
    {
        ClassicAssert.IsFalse(ReferenceEquals(CreateGoldSample(), CreateGoldSample()));
        ClassicAssert.IsTrue(CreateGoldSample().Equals(CreateGoldSample()));
        ClassicAssert.IsFalse(CreatePredSample().Equals(CreateGoldSample()));
        ClassicAssert.IsFalse(CreatePredSample().Equals(new object()));
    }
}
