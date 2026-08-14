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

namespace NOpenNLP.Tools.Namefind;

/// <summary>
/// This is the test class for <see cref="BioCodec"/>.
/// </summary>
/// <remarks>
/// NOpenNLP: upstream wraps the sentence and spans in a NameSample and then calls
/// Encode with nameSample.getNames() and nameSample.getSentence().length. NameSample
/// is not ported yet, so the encode tests pass the same spans and token count to
/// Encode directly, which is what NameSample forwarded anyway.
/// </remarks>
public class BioCodecTest
{
    private static readonly BioCodec codec = new BioCodec();

    private const string A_TYPE = "atype";
    private const string A_START = A_TYPE + "-" + BioCodec.START;
    private const string A_CONTINUE = A_TYPE + "-" + BioCodec.CONTINUE;

    private const string B_TYPE = "btype";
    private const string B_START = B_TYPE + "-" + BioCodec.START;
    private const string B_CONTINUE = B_TYPE + "-" + BioCodec.CONTINUE;

    private const string C_TYPE = "ctype";
    private const string C_START = C_TYPE + "-" + BioCodec.START;

    private const string OTHER = BioCodec.OTHER;

    [Test]
    public void TestEncodeNoNames()
    {
        string[] sentence = "Once upon a time.".Split(' ');
        Span[] spans = [];
        string[] expected = [OTHER, OTHER, OTHER, OTHER];
        string[] actual = codec.Encode(spans, sentence.Length);
        CollectionAssert.AreEqual(expected, actual, "Only 'Other' is expected.");
    }

    [Test]
    public void TestEncodeSingleTokenSpan()
    {
        string[] sentence = "I called Julie again.".Split(' ');
        Span[] spans = [new Span(2, 3, A_TYPE)];
        string[] expected = [OTHER, OTHER, A_START, OTHER];
        string[] actual = codec.Encode(spans, sentence.Length);
        CollectionAssert.AreEqual(expected, actual,
            "'Julie' should be 'start' only, the rest should be 'other'.");
    }

    [Test]
    public void TestEncodeDoubleTokenSpan()
    {
        string[] sentence = "I saw Stefanie Schmidt today.".Split(' ');
        Span[] span = [new Span(2, 4, A_TYPE)];
        string[] expected = [OTHER, OTHER, A_START, A_CONTINUE, OTHER];
        string[] actual = codec.Encode(span, sentence.Length);
        CollectionAssert.AreEqual(expected, actual, "'Stefanie' should be 'start' only, 'Schmidt' is " +
            "'continue' and the rest should be 'other'.");
    }

    [Test]
    public void TestEncodeDoubleTokenSpanNoType()
    {
        const string DEFAULT_START = "default" + "-" + BioCodec.START;
        const string DEFAULT_CONTINUE = "default" + "-" + BioCodec.CONTINUE;
        string[] sentence = "I saw Stefanie Schmidt today.".Split(' ');
        Span[] span = [new Span(2, 4, null)];
        string[] expected = [OTHER, OTHER, DEFAULT_START, DEFAULT_CONTINUE, OTHER];
        string[] actual = codec.Encode(span, sentence.Length);
        CollectionAssert.AreEqual(expected, actual, "'Stefanie' should be 'start' only, 'Schmidt' is " +
            "'continue' and the rest should be 'other'.");
    }

    [Test]
    public void TestEncodeAdjacentSingleSpans()
    {
        string[] sentence = "something PersonA PersonB Something".Split(' ');
        Span[] span = [new Span(1, 2, A_TYPE), new Span(2, 3, A_TYPE)];
        string[] expected = [OTHER, A_START, A_START, OTHER];
        string[] actual = codec.Encode(span, sentence.Length);
        CollectionAssert.AreEqual(expected, actual);
    }

    [Test]
    public void TestEncodeAdjacentSpans()
    {
        string[] sentence = "something PersonA PersonA PersonB Something".Split(' ');
        Span[] span = [new Span(1, 3, A_TYPE), new Span(3, 4, A_TYPE)];
        string[] expected = [OTHER, A_START, A_CONTINUE, A_START, OTHER];
        string[] actual = codec.Encode(span, sentence.Length);
        CollectionAssert.AreEqual(expected, actual);
    }

    [Test]
    public void TestCreateSequenceValidator()
    {
        ClassicAssert.IsTrue(codec.CreateSequenceValidator() is NameFinderSequenceValidator);
    }

    [Test]
    public void TestDecodeEmpty()
    {
        Span[] expected = [];
        Span[] actual = codec.Decode(new List<string>());
        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Start, Other
    /// </summary>
    [Test]
    public void TestDecodeSingletonFirst()
    {
        IList<string> encoded = [B_START, OTHER];
        Span[] expected = [new Span(0, 1, B_TYPE)];
        Span[] actual = codec.Decode(encoded);
        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Start Start Other
    /// </summary>
    [Test]
    public void TestDecodeAdjacentSingletonFirst()
    {
        IList<string> encoded = [B_START, B_START, OTHER];
        Span[] expected = [new Span(0, 1, B_TYPE), new Span(1, 2, B_TYPE)];
        Span[] actual = codec.Decode(encoded);
        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Start Continue Other
    /// </summary>
    [Test]
    public void TestDecodePairFirst()
    {
        IList<string> encoded = [B_START, B_CONTINUE, OTHER];
        Span[] expected = [new Span(0, 2, B_TYPE)];
        Span[] actual = codec.Decode(encoded);
        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Start Continue Continue Other
    /// </summary>
    [Test]
    public void TestDecodeTripletFirst()
    {
        IList<string> encoded = [B_START, B_CONTINUE, B_CONTINUE, OTHER];
        Span[] expected = [new Span(0, 3, B_TYPE)];
        Span[] actual = codec.Decode(encoded);
        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Start Continue Start Other
    /// </summary>
    [Test]
    public void TestDecodeAdjacentPairSingleton()
    {
        IList<string> encoded = [B_START, B_CONTINUE, B_START, OTHER];
        Span[] expected = [new Span(0, 2, B_TYPE), new Span(2, 3, B_TYPE)];
        Span[] actual = codec.Decode(encoded);
        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Other Start Other
    /// </summary>
    [Test]
    public void TestDecodeOtherFirst()
    {
        IList<string> encoded = [OTHER, B_START, OTHER];
        Span[] expected = [new Span(1, 2, B_TYPE)];
        Span[] actual = codec.Decode(encoded);
        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// A-Start A-Continue, A-Continue, Other, B-Start, B-Continue, Other, C-Start, Other
    /// </summary>
    [Test]
    public void TestDecodeMultiClass()
    {
        IList<string> encoded = [OTHER, A_START, A_CONTINUE, A_CONTINUE,
            OTHER, B_START, B_CONTINUE, OTHER, C_START, OTHER];
        Span[] expected = [new Span(1, 4, A_TYPE),
            new Span(5, 7, B_TYPE), new Span(8, 9, C_TYPE)];
        Span[] actual = codec.Decode(encoded);
        CollectionAssert.AreEqual(expected, actual);
    }

    [Test]
    public void TestCompatibilityEmpty()
    {
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([]));
    }

    [Test]
    public void TestCompatibilitySingleStart()
    {
        ClassicAssert.IsTrue(codec.AreOutcomesCompatible([A_START]));
    }

    [Test]
    public void TestCompatibilitySingleContinue()
    {
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([A_CONTINUE]));
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([B_START, A_CONTINUE]));
    }

    [Test]
    public void TestCompatibilitySingleOther()
    {
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([OTHER]));
    }

    [Test]
    public void TestCompatibilityStartContinue()
    {
        ClassicAssert.IsTrue(codec.AreOutcomesCompatible([A_START, A_CONTINUE]));
    }

    [Test]
    public void TestCompatibilityStartOther()
    {
        ClassicAssert.IsTrue(codec.AreOutcomesCompatible([A_START, OTHER]));
    }

    [Test]
    public void TestCompatibilityContinueOther()
    {
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([A_CONTINUE, OTHER]));
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([B_START, A_CONTINUE, OTHER]));
    }

    [Test]
    public void TestCompatibilityStartContinueOther()
    {
        ClassicAssert.IsTrue(codec.AreOutcomesCompatible([A_START, A_CONTINUE, OTHER]));
    }

    [Test]
    public void TestCompatibilityMultiClass()
    {
        ClassicAssert.IsTrue(codec.AreOutcomesCompatible(
            [A_START, A_CONTINUE, B_START, OTHER]));
    }

    [Test]
    public void TestCompatibilityBadTag()
    {
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([A_START, A_CONTINUE, "BAD"]));
    }

    [Test]
    public void TestCompatibilityRepeated()
    {
        ClassicAssert.IsTrue(codec.AreOutcomesCompatible(
            [A_START, A_START, A_CONTINUE, A_CONTINUE, B_START, B_START, OTHER, OTHER]));
    }
}
