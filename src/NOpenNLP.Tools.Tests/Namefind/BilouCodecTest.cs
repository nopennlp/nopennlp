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
/// This is the test class for <see cref="BilouCodec"/>.
/// </summary>
/// <remarks>
/// NOpenNLP: upstream wraps the sentence and spans in a NameSample and then calls
/// Encode with nameSample.getNames() and nameSample.getSentence().length. NameSample
/// is not ported yet, so the encode tests pass the same spans and token count to
/// Encode directly, which is what NameSample forwarded anyway.
/// </remarks>
public class BilouCodecTest
{
    private static readonly BilouCodec codec = new BilouCodec();

    private const string A_TYPE = "atype";
    private const string A_START = A_TYPE + "-" + BilouCodec.START;
    private const string A_CONTINUE = A_TYPE + "-" + BilouCodec.CONTINUE;
    private const string A_LAST = A_TYPE + "-" + BilouCodec.LAST;
    private const string A_UNIT = A_TYPE + "-" + BilouCodec.UNIT;

    private const string B_TYPE = "btype";
    private const string B_START = B_TYPE + "-" + BilouCodec.START;
    private const string B_CONTINUE = B_TYPE + "-" + BilouCodec.CONTINUE;
    private const string B_LAST = B_TYPE + "-" + BilouCodec.LAST;
    private const string B_UNIT = B_TYPE + "-" + BilouCodec.UNIT;

    private const string C_TYPE = "ctype";
    private const string C_UNIT = C_TYPE + "-" + BilouCodec.UNIT;

    private const string OTHER = BilouCodec.OTHER;

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
    public void TestEncodeSingleUnitTokenSpan()
    {
        string[] sentence = "I called Julie again.".Split(' ');
        Span[] singleSpan = [new Span(2, 3, A_TYPE)];
        string[] expected = [OTHER, OTHER, A_UNIT, OTHER];
        string[] actual = codec.Encode(singleSpan, sentence.Length);
        CollectionAssert.AreEqual(expected, actual,
            "'Julie' should be 'unit' only, the rest should be 'other'.");
    }

    [Test]
    public void TestEncodeDoubleTokenSpan()
    {
        string[] sentence = "I saw Stefanie Schmidt today.".Split(' ');
        Span[] singleSpan = [new Span(2, 4, A_TYPE)];
        string[] expected = [OTHER, OTHER, A_START, A_LAST, OTHER];
        string[] actual = codec.Encode(singleSpan, sentence.Length);
        CollectionAssert.AreEqual(expected, actual, "'Stefanie' should be 'start' only, 'Schmidt' is 'last' " +
            "and the rest should be 'other'.");
    }

    [Test]
    public void TestEncodeTripleTokenSpan()
    {
        string[] sentence = "Secretary - General Anders Fogh Rasmussen is from Denmark.".Split(' ');
        Span[] singleSpan = [new Span(3, 6, A_TYPE)];
        string[] expected = [OTHER, OTHER, OTHER, A_START, A_CONTINUE, A_LAST, OTHER, OTHER, OTHER];
        string[] actual = codec.Encode(singleSpan, sentence.Length);
        CollectionAssert.AreEqual(expected, actual, "'Anders' should be 'start' only, 'Fogh' is 'inside', " +
            "'Rasmussen' is 'last' and the rest should be 'other'.");
    }

    [Test]
    public void TestEncodeAdjacentUnitSpans()
    {
        string[] sentence = "word PersonA PersonB word".Split(' ');
        Span[] singleSpan = [new Span(1, 2, A_TYPE), new Span(2, 3, A_TYPE)];
        string[] expected = [OTHER, A_UNIT, A_UNIT, OTHER];
        string[] actual = codec.Encode(singleSpan, sentence.Length);
        CollectionAssert.AreEqual(expected, actual, "Both PersonA and PersonB are 'unit' tags");
    }

    [Test]
    public void TestCreateSequenceValidator()
    {
        ClassicAssert.IsTrue(codec.CreateSequenceValidator() is BilouNameFinderSequenceValidator);
    }

    [Test]
    public void TestDecodeEmpty()
    {
        Span[] expected = [];
        Span[] actual = codec.Decode(new List<string>());
        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Unit, Other
    /// </summary>
    [Test]
    public void TestDecodeSingletonFirst()
    {
        IList<string> encoded = [A_UNIT, OTHER];
        Span[] expected = [new Span(0, 1, A_TYPE)];
        Span[] actual = codec.Decode(encoded);
        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Unit, Unit, Other
    /// </summary>
    [Test]
    public void TestDecodeAdjacentSingletonFirst()
    {
        IList<string> encoded = [A_UNIT, A_UNIT, OTHER];
        Span[] expected = [new Span(0, 1, A_TYPE), new Span(1, 2, A_TYPE)];
        Span[] actual = codec.Decode(encoded);
        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Start, Last, Other
    /// </summary>
    [Test]
    public void TestDecodePairFirst()
    {
        IList<string> encoded = [A_START, A_LAST, OTHER];
        Span[] expected = [new Span(0, 2, A_TYPE)];
        Span[] actual = codec.Decode(encoded);
        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Start, Continue, Last, Other
    /// </summary>
    [Test]
    public void TestDecodeTripletFirst()
    {
        IList<string> encoded = [A_START, A_CONTINUE, A_LAST, OTHER];
        Span[] expected = [new Span(0, 3, A_TYPE)];
        Span[] actual = codec.Decode(encoded);
        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Start, Continue, Continue, Last, Other
    /// </summary>
    [Test]
    public void TestDecodeTripletContinuationFirst()
    {
        IList<string> encoded = [A_START, A_CONTINUE, A_CONTINUE, A_LAST, OTHER];
        Span[] expected = [new Span(0, 4, A_TYPE)];
        Span[] actual = codec.Decode(encoded);
        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Start, Last, Unit, Other
    /// </summary>
    [Test]
    public void TestDecodeAdjacentPairSingleton()
    {
        IList<string> encoded = [A_START, A_LAST, A_UNIT, OTHER];
        Span[] expected = [new Span(0, 2, A_TYPE), new Span(2, 3, A_TYPE)];
        Span[] actual = codec.Decode(encoded);
        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Other, Unit, Other
    /// </summary>
    [Test]
    public void TestDecodeOtherFirst()
    {
        IList<string> encoded = [OTHER, A_UNIT, OTHER];
        Span[] expected = [new Span(1, 2, A_TYPE)];
        Span[] actual = codec.Decode(encoded);
        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Other, A-Start, A-Continue, A-Last, Other, B-Start, B-Last, Other, C-Unit, Other
    /// </summary>
    [Test]
    public void TestDecodeMultiClass()
    {
        IList<string> encoded = [OTHER, A_START, A_CONTINUE, A_LAST, OTHER,
            B_START, B_LAST, OTHER, C_UNIT, OTHER];
        Span[] expected = [new Span(1, 4, A_TYPE), new Span(5, 7, B_TYPE), new Span(8, 9, C_TYPE)];
        Span[] actual = codec.Decode(encoded);
        CollectionAssert.AreEqual(expected, actual);
    }

    [Test]
    public void TestCompatibilityEmpty()
    {
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([]));
    }

    // Singles and singles in combination with other valid type (unit/start+last)

    /// <summary>
    /// B-Start =&gt; Fail
    /// <para/>
    /// A-Unit, B-Start =&gt; Fail
    /// <para/>
    /// A-Start, A-Last, B-Start =&gt; Fail
    /// </summary>
    [Test]
    public void TestCompatibilitySinglesStart()
    {
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([B_START]));
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([A_UNIT, B_START]));
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([A_START, A_LAST, B_START]));
    }

    /// <summary>
    /// B-Continue =&gt; Fail
    /// <para/>
    /// A-Unit, B-Continue =&gt; Fail
    /// <para/>
    /// A-Start, A-Last, B-Continue =&gt; Fail
    /// </summary>
    [Test]
    public void TestCompatibilitySinglesContinue()
    {
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([B_CONTINUE]));
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([A_UNIT, B_CONTINUE]));
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([A_START, A_LAST, B_CONTINUE]));
    }

    /// <summary>
    /// B-Last =&gt; Fail
    /// <para/>
    /// A-Unit, B-Last =&gt; Fail
    /// <para/>
    /// A-Start, A-Last, B-Last =&gt; Fail
    /// </summary>
    [Test]
    public void TestCompatibilitySinglesLast()
    {
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([B_LAST]));
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([A_UNIT, B_LAST]));
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([A_START, A_LAST, B_LAST]));
    }

    /// <summary>
    /// Other =&gt; Fail
    /// <para/>
    /// A-Unit, Other =&gt; Pass
    /// <para/>
    /// A-Start, A-Last, Other =&gt; Pass
    /// </summary>
    [Test]
    public void TestCompatibilitySinglesOther()
    {
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([OTHER]));
        ClassicAssert.IsTrue(codec.AreOutcomesCompatible([A_UNIT, OTHER]));
        ClassicAssert.IsTrue(codec.AreOutcomesCompatible([A_START, A_LAST, OTHER]));
    }

    /// <summary>
    /// B-Unit =&gt; Pass
    /// <para/>
    /// A-Unit, B-Unit =&gt; Pass
    /// <para/>
    /// A-Start, A-Last, B-Unit =&gt; Pass
    /// </summary>
    [Test]
    public void TestCompatibilitySinglesUnit()
    {
        ClassicAssert.IsTrue(codec.AreOutcomesCompatible([B_UNIT]));
        ClassicAssert.IsTrue(codec.AreOutcomesCompatible([A_UNIT, B_UNIT]));
        ClassicAssert.IsTrue(codec.AreOutcomesCompatible([A_START, A_LAST, B_UNIT]));
    }

    /// <summary>
    /// Doubles and doubles in combination with other valid type (unit/start+last)
    /// <para/>
    /// B-Start, B-Continue =&gt; Fail
    /// <para/>
    /// A-Unit, B-Start, B-Continue =&gt; Fail
    /// <para/>
    /// A-Start, A-Last, B-Start, B-Continue =&gt; Fail
    /// </summary>
    [Test]
    public void TestCompatibilityStartContinue()
    {
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([B_START, B_CONTINUE]));
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([A_UNIT, B_START, B_CONTINUE]));
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([A_START, A_LAST, B_START, B_CONTINUE]));
    }

    /// <summary>
    /// B-Start, B-Last =&gt; Pass
    /// <para/>
    /// A-Unit, B-Start, B-Last =&gt; Pass
    /// <para/>
    /// A-Start, A-Last, B-Start, B-Last =&gt; Pass
    /// </summary>
    [Test]
    public void TestCompatibilityStartLast()
    {
        ClassicAssert.IsTrue(codec.AreOutcomesCompatible([B_START, B_LAST]));
        ClassicAssert.IsTrue(codec.AreOutcomesCompatible([A_UNIT, B_START, B_LAST]));
        ClassicAssert.IsTrue(codec.AreOutcomesCompatible([A_START, A_LAST, B_START, B_LAST]));
    }

    /// <summary>
    /// B-Start, Other =&gt; Fail
    /// <para/>
    /// A-Unit, B-Start, Other =&gt; Fail
    /// <para/>
    /// A-Start, A-Last, B-Start, Other =&gt; Fail
    /// </summary>
    [Test]
    public void TestCompatibilityStartOther()
    {
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([B_START, OTHER]));
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([A_UNIT, B_START, OTHER]));
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([A_START, A_LAST, B_START, OTHER]));
    }

    /// <summary>
    /// B-Start, B-Unit =&gt; Fail
    /// <para/>
    /// A-Unit, B-Start, B-Unit =&gt; Fail
    /// <para/>
    /// A-Start, A-Last, B-Start, B-Unit =&gt; Fail
    /// </summary>
    [Test]
    public void TestCompatibilityStartUnit()
    {
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([B_START, B_UNIT]));
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([A_UNIT, B_START, B_UNIT]));
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([A_START, A_LAST, B_START, B_UNIT]));
    }

    /// <summary>
    /// B-Continue, C-Last =&gt; Fail
    /// <para/>
    /// A-Unit, B-Continue, C-Last =&gt; Fail
    /// <para/>
    /// A-Start, A-Last, B-Continue, B-Last =&gt; Fail
    /// </summary>
    [Test]
    public void TestCompatibilityContinueLast()
    {
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([B_CONTINUE, B_LAST]));
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([A_UNIT, B_CONTINUE, B_LAST]));
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([A_START, A_LAST, B_CONTINUE, B_LAST]));
    }

    /// <summary>
    /// B-Continue, Other =&gt; Fail
    /// <para/>
    /// A-Unit, B-Continue, Other =&gt; Fail
    /// <para/>
    /// A-Start, A-Last, B-Continue, Other =&gt; Fail
    /// </summary>
    [Test]
    public void TestCompatibilityContinueOther()
    {
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([B_CONTINUE, OTHER]));
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([A_UNIT, B_CONTINUE, OTHER]));
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([A_START, A_LAST, B_CONTINUE, OTHER]));
    }

    /// <summary>
    /// B-Continue, B-Unit =&gt; Fail
    /// <para/>
    /// A-Unit, B-Continue, B-Unit =&gt; Fail
    /// <para/>
    /// A-Start, A-Last, B-Continue, B-Unit =&gt; Fail
    /// </summary>
    [Test]
    public void TestCompatibilityContinueUnit()
    {
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([B_CONTINUE, B_UNIT]));
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([A_UNIT, B_CONTINUE, B_UNIT]));
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([A_START, A_LAST, B_CONTINUE, B_UNIT]));
    }

    /// <summary>
    /// B-Last, Other =&gt; Fail
    /// <para/>
    /// A-Unit, B-Last, Other =&gt; Fail
    /// <para/>
    /// A-Start, A-Last, B-Last, Other =&gt; Fail
    /// </summary>
    [Test]
    public void TestCompatibilityLastOther()
    {
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([B_LAST, OTHER]));
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([A_UNIT, B_LAST, OTHER]));
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([A_START, A_LAST, B_LAST, OTHER]));
    }

    /// <summary>
    /// B-Last, B-Unit =&gt; Fail
    /// <para/>
    /// A-Unit, B-Last, B-Unit =&gt; Fail
    /// <para/>
    /// A-Start, A-Last, B-Last, B-Unit =&gt; Fail
    /// </summary>
    [Test]
    public void TestCompatibilityLastUnit()
    {
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([B_LAST, B_UNIT]));
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([A_UNIT, B_LAST, B_UNIT]));
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([A_START, A_LAST, B_LAST, B_UNIT]));
    }

    /// <summary>
    /// Other, B-Unit =&gt; Pass
    /// <para/>
    /// A-Unit, Other, B-Unit =&gt; Pass
    /// <para/>
    /// A-Start, A-Last, Other, B-Unit =&gt; Pass
    /// </summary>
    [Test]
    public void TestCompatibilityOtherUnit()
    {
        ClassicAssert.IsTrue(codec.AreOutcomesCompatible([OTHER, B_UNIT]));
        ClassicAssert.IsTrue(codec.AreOutcomesCompatible([A_UNIT, OTHER, B_UNIT]));
        ClassicAssert.IsTrue(codec.AreOutcomesCompatible([A_START, A_LAST, OTHER, B_UNIT]));
    }

    /// <summary>
    /// Triples and triples in combination with other valid type (unit/start+last)
    /// <para/>
    /// B-Start, B-Continue, B-Last =&gt; Pass
    /// <para/>
    /// A-Unit, B-Start, B-Continue, B-Last =&gt; Pass
    /// <para/>
    /// A-Start, A-Last, B-Start, B-Continue, B-Last =&gt; Pass
    /// </summary>
    [Test]
    public void TestCompatibilityStartContinueLast()
    {
        ClassicAssert.IsTrue(codec.AreOutcomesCompatible([B_START, B_CONTINUE, B_LAST]));
        ClassicAssert.IsTrue(codec.AreOutcomesCompatible([A_UNIT, B_START, B_CONTINUE, B_LAST]));
        ClassicAssert.IsTrue(codec.AreOutcomesCompatible([A_START, A_LAST, B_START, B_CONTINUE, B_LAST]));
    }

    /// <summary>
    /// B-Start, B-Continue, Other =&gt; Fail
    /// <para/>
    /// A-Unit, B-Start, B-Continue, Other =&gt; Fail
    /// <para/>
    /// A-Start, A-Last, B-Start, B-Continue, Other =&gt; Fail
    /// </summary>
    [Test]
    public void TestCompatibilityStartContinueOther()
    {
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([B_START, B_CONTINUE, OTHER]));
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([A_UNIT, B_START, B_CONTINUE, OTHER]));
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([A_START, A_LAST, B_START, B_CONTINUE, OTHER]));
    }

    /// <summary>
    /// B-Start, B-Continue, B-Unit =&gt; Fail
    /// <para/>
    /// A-Unit, B-Start, B-Continue, B-Unit =&gt; Fail
    /// <para/>
    /// A-Start, A-Last, B-Start, B-Continue, B-Unit =&gt; Fail
    /// </summary>
    [Test]
    public void TestCompatibilityStartContinueUnit()
    {
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([B_START, B_CONTINUE, B_UNIT]));
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([A_UNIT, B_START, B_CONTINUE, B_UNIT]));
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([A_START, A_LAST, B_START, B_CONTINUE, B_UNIT]));
    }

    /// <summary>
    /// B-Continue, B-Last, Other =&gt; Fail
    /// <para/>
    /// A-Unit, B-Continue, B-Last, Other =&gt; Fail
    /// <para/>
    /// A-Start, A-Last, B-Continue, B-Last, Other =&gt; Fail
    /// </summary>
    [Test]
    public void TestCompatibilityContinueLastOther()
    {
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([B_CONTINUE, B_LAST, OTHER]));
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([A_UNIT, B_CONTINUE, B_LAST, OTHER]));
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([A_START, A_LAST, B_CONTINUE, B_LAST, OTHER]));
    }

    /// <summary>
    /// B-Continue, B-Last, B-Unit =&gt; Fail
    /// <para/>
    /// A-Unit, B-Continue, B-Last, B_Unit =&gt; Fail
    /// <para/>
    /// A-Start, A-Last, B-Continue, B-Last, B_Unit =&gt; Fail
    /// </summary>
    [Test]
    public void TestCompatibilityContinueLastUnit()
    {
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([B_CONTINUE, B_LAST, B_UNIT]));
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([A_UNIT, B_CONTINUE, B_LAST, B_UNIT]));
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([A_START, A_LAST, B_CONTINUE, B_LAST, B_UNIT]));
    }

    /// <summary>
    /// B-Last, Other, B-Unit =&gt; Fail
    /// <para/>
    /// A-Unit, B-Continue, B-Last, B_Unit =&gt; Fail
    /// <para/>
    /// A-Start, A-Last, B-Continue, B-Last, B_Unit =&gt; Fail
    /// </summary>
    [Test]
    public void TestCompatibilityLastOtherUnit()
    {
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([B_LAST, OTHER, B_UNIT]));
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([A_UNIT, B_LAST, OTHER, B_UNIT]));
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([A_START, A_LAST, B_LAST, OTHER, B_UNIT]));
    }

    /// <summary>
    /// Quadruples and quadruple in combination of unit/start+last
    /// <para/>
    /// B-Start, B-Continue, B-Last, Other =&gt; Pass
    /// <para/>
    /// A-Unit, B-Start, B-Continue, B-Last, Other =&gt; Pass
    /// <para/>
    /// A-Start, A-Last, B-Start, B-Continue, B-Last, Other =&gt; Pass
    /// </summary>
    [Test]
    public void TestCompatibilityStartContinueLastOther()
    {
        ClassicAssert.IsTrue(codec.AreOutcomesCompatible([B_START, B_CONTINUE, B_LAST, OTHER]));
        ClassicAssert.IsTrue(codec.AreOutcomesCompatible([A_UNIT, B_START, B_CONTINUE, B_LAST, OTHER]));
        ClassicAssert.IsTrue(codec.AreOutcomesCompatible([A_START, A_LAST, B_START, B_CONTINUE, B_LAST, OTHER]));
    }

    /// <summary>
    /// B-Start, B-Continue, B-Last, B-Unit =&gt; Pass
    /// <para/>
    /// A-Unit, B-Start, B-Continue, B-Last, B-Unit =&gt; Pass
    /// <para/>
    /// A-Start, A-Last, B-Start, B-Continue, B-Last, B-Unit =&gt; Pass
    /// </summary>
    [Test]
    public void TestCompatibilityStartContinueLastUnit()
    {
        ClassicAssert.IsTrue(codec.AreOutcomesCompatible([B_START, B_CONTINUE, B_LAST, B_UNIT]));
        ClassicAssert.IsTrue(codec.AreOutcomesCompatible([A_UNIT, B_START, B_CONTINUE, B_LAST, B_UNIT]));
        ClassicAssert.IsTrue(codec.AreOutcomesCompatible([A_START, A_LAST, B_START, B_CONTINUE, B_LAST, B_UNIT]));
    }

    /// <summary>
    /// B-Continue, B-Last, Other, B-Unit =&gt; Fail
    /// <para/>
    /// A-Unit, B-Continue, B-Last, Other, B-Unit =&gt; Fail
    /// <para/>
    /// A-Start, A-Last, B-Continue, B-Last, Other, B-Unit =&gt; Fail
    /// </summary>
    [Test]
    public void TestCompatibilityContinueLastOtherUnit()
    {
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([B_CONTINUE, B_LAST, OTHER, B_UNIT]));
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([A_UNIT, B_CONTINUE, B_LAST, OTHER, B_UNIT]));
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([A_START, A_LAST, B_CONTINUE, B_LAST, OTHER, B_UNIT]));
    }

    /// <summary>
    /// Quintuple
    /// <para/>
    /// B-Start, B-Continue, B-Last, Other, B-Unit =&gt; Pass
    /// <para/>
    /// A-Unit, B-Start, B-Continue, B-Last, Other, B-Unit =&gt; Pass
    /// <para/>
    /// A-Staart, A-Last, B-Start, B-Continue, B-Last, Other, B-Unit =&gt; Pass
    /// </summary>
    [Test]
    public void TestCompatibilityUnitOther()
    {
        ClassicAssert.IsTrue(codec.AreOutcomesCompatible([B_START, B_CONTINUE, B_LAST, OTHER, B_UNIT]));
        ClassicAssert.IsTrue(codec.AreOutcomesCompatible([A_UNIT, B_START, B_CONTINUE, B_LAST, OTHER, B_UNIT]));
        ClassicAssert.IsTrue(codec.AreOutcomesCompatible(
            [A_START, A_LAST, B_START, B_CONTINUE, B_LAST, OTHER, B_UNIT]));
    }

    /// <summary>
    /// Multiclass
    /// </summary>
    [Test]
    public void TestCompatibilityMultiClass()
    {
        ClassicAssert.IsTrue(codec.AreOutcomesCompatible(
            [B_UNIT, A_CONTINUE, A_LAST, A_UNIT, B_START, B_LAST, A_START, C_UNIT, OTHER]));
    }

    /// <summary>
    /// Bad combinations
    /// </summary>
    [Test]
    public void TestCompatibilityBadTag()
    {
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([A_START, A_CONTINUE, OTHER, "BAD"]));
    }

    [Test]
    public void TestCompatibilityWrongClass()
    {
        ClassicAssert.IsFalse(codec.AreOutcomesCompatible([A_START, B_LAST, OTHER]));
    }
}
