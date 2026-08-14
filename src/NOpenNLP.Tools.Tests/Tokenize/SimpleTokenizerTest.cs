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

// This file has been modified from the original Apache OpenNLP 1.9.4 source:
// translated from Java to C# and adapted for .NET. See NOTICE.
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Tokenize;

/// <summary>
/// Tests for the <see cref="SimpleTokenizer"/> class.
/// </summary>
public class SimpleTokenizerTest
{
    // The SimpleTokenizer is thread safe
    private readonly SimpleTokenizer mTokenizer = SimpleTokenizer.INSTANCE; // NOpenNLP: made readonly

    /// <summary>
    /// Tests if it can tokenize whitespace separated tokens.
    /// </summary>
    [Test]
    public void TestWhitespaceTokenization()
    {
        const string text = "a b c  d     e                f    ";

        string[] tokenizedText = mTokenizer.Tokenize(text);

        ClassicAssert.IsTrue("a".Equals(tokenizedText[0]));
        ClassicAssert.IsTrue("b".Equals(tokenizedText[1]));
        ClassicAssert.IsTrue("c".Equals(tokenizedText[2]));
        ClassicAssert.IsTrue("d".Equals(tokenizedText[3]));
        ClassicAssert.IsTrue("e".Equals(tokenizedText[4]));
        ClassicAssert.IsTrue("f".Equals(tokenizedText[5]));

        ClassicAssert.IsTrue(tokenizedText.Length == 6);
    }

    /// <summary>
    /// Tests if it can tokenize a word and a dot.
    /// </summary>
    [Test]
    public void TestWordDotTokenization()
    {
        const string text = "a.";

        string[] tokenizedText = mTokenizer.Tokenize(text);

        ClassicAssert.IsTrue("a".Equals(tokenizedText[0]));
        ClassicAssert.IsTrue(".".Equals(tokenizedText[1]));
        ClassicAssert.IsTrue(tokenizedText.Length == 2);
    }

    /// <summary>
    /// Tests if it can tokenize a word and numeric.
    /// </summary>
    [Test]
    public void TestWordNumericTokeniztation()
    {
        const string text = "305KW";

        string[] tokenizedText = mTokenizer.Tokenize(text);

        ClassicAssert.IsTrue("305".Equals(tokenizedText[0]));
        ClassicAssert.IsTrue("KW".Equals(tokenizedText[1]));
        ClassicAssert.IsTrue(tokenizedText.Length == 2);
    }

    [Test]
    public void TestWordWithOtherTokenization()
    {
        const string text = "rebecca.sleep()";

        string[] tokenizedText = mTokenizer.Tokenize(text);

        ClassicAssert.IsTrue("rebecca".Equals(tokenizedText[0]));
        ClassicAssert.IsTrue(".".Equals(tokenizedText[1]));
        ClassicAssert.IsTrue("sleep".Equals(tokenizedText[2]));
        ClassicAssert.IsTrue("(".Equals(tokenizedText[3]));
        ClassicAssert.IsTrue(")".Equals(tokenizedText[4]));
        ClassicAssert.IsTrue(tokenizedText.Length == 5);
    }
}
