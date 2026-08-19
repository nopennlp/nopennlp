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

using System;
using NOpenNLP.Tools.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Postag;

/// <summary>
/// Tests for the <see cref="POSSample"/> class.
/// </summary>
public class POSSampleTest
{
    [Test]
    public void TestEquals()
    {
        ClassicAssert.IsFalse(ReferenceEquals(CreateGoldSample(), CreateGoldSample()));
        ClassicAssert.IsTrue(CreateGoldSample().Equals(CreateGoldSample()));
        ClassicAssert.IsFalse(CreatePredSample().Equals(CreateGoldSample()));
        ClassicAssert.IsFalse(CreatePredSample().Equals(new object()));
    }

    public static POSSample CreateGoldSample()
    {
        string sentence = "the_DT stories_NNS about_IN well-heeled_JJ "
            + "communities_NNS and_CC developers_NNS";
        return POSSample.Parse(sentence);
    }

    public static POSSample CreatePredSample()
    {
        string sentence = "the_DT stories_NNS about_NNS well-heeled_JJ "
            + "communities_NNS and_CC developers_CC";
        return POSSample.Parse(sentence);
    }

    // NOpenNLP: upstream's testPOSSampleSerDe round-trips the sample through
    // java.io.ObjectOutputStream/ObjectInputStream. POSSample does not implement
    // Serializable in the port (see the note on the class), and .NET has no
    // equivalent built-in graph serializer, so the test has no counterpart here.

    /// <summary>
    /// Tests if it can parse a valid token_tag sentence.
    /// </summary>
    [Test]
    public void TestParse()
    {
        string sentence = "the_DT stories_NNS about_IN well-heeled_JJ " +
            "communities_NNS and_CC developers_NNS";
        POSSample sample = POSSample.Parse(sentence);
        ClassicAssert.AreEqual(sentence, sample.ToString());
    }

    /// <summary>
    /// Tests if it can parse an empty <see cref="string"/>.
    /// </summary>
    [Test]
    public void TestParseEmptyString()
    {
        string sentence = "";

        POSSample sample = POSSample.Parse(sentence);

        ClassicAssert.AreEqual(sample.Sentence.Length, 0);
        ClassicAssert.AreEqual(sample.Tags.Length, 0);
    }

    /// <summary>
    /// Tests if it can parse an empty token.
    /// </summary>
    [Test]
    public void TestParseEmtpyToken()
    {
        string sentence = "the_DT _NNS";
        POSSample sample = POSSample.Parse(sentence);
        ClassicAssert.AreEqual(sample.Sentence[1], "");
    }

    /// <summary>
    /// Tests if it can parse an empty tag.
    /// </summary>
    [Test]
    public void TestParseEmtpyTag()
    {
        string sentence = "the_DT stories_";
        POSSample sample = POSSample.Parse(sentence);
        ClassicAssert.AreEqual(sample.Tags[1], "");
    }

    /// <summary>
    /// Tests if an exception is thrown if there is only a token/tag
    /// in the sentence.
    /// </summary>
    [Test]
    public void TestParseWithError()
    {
        string sentence = "the_DT stories";

        try
        {
            POSSample.Parse(sentence);
        }
        catch (InvalidFormatException)
        {
            return;
        }

        Assert.Fail();
    }
}
