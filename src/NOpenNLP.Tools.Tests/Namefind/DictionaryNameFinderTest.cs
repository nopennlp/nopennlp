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
using NOpenNLP.Tools.Tokenize;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Namefind;

/// <summary>
/// Tests for the <see cref="DictionaryNameFinder"/> class.
/// </summary>
public class DictionaryNameFinderTest
{
    private readonly Dictionary.Dictionary mDictionary = new();

    private ITokenNameFinder mNameFinder;

    public DictionaryNameFinderTest()
    {
        var vanessa = new StringList(["Vanessa"]);
        mDictionary.Put(vanessa);

        var vanessaWilliams = new StringList("Vanessa", "Williams");
        mDictionary.Put(vanessaWilliams);

        var max = new StringList(["Max"]);
        mDictionary.Put(max);

        var michaelJordan = new StringList("Michael", "Jordan");
        mDictionary.Put(michaelJordan);
    }

    [SetUp]
    public void SetUp()
    {
        mNameFinder = new DictionaryNameFinder(mDictionary);
    }

    [Test]
    public void TestSingleTokeNameAtSentenceStart()
    {
        const string sentence = "Max a b c d";
        var tokenizer = SimpleTokenizer.INSTANCE;
        var tokens = tokenizer.Tokenize(sentence);
        var names = mNameFinder.Find(tokens);
        ClassicAssert.IsTrue(names.Length == 1);
        ClassicAssert.IsTrue(names[0].Start == 0 && names[0].End == 1);
    }

    [Test]
    public void TestSingleTokeNameInsideSentence()
    {
        const string sentence = "a b  Max c d";
        var tokenizer = SimpleTokenizer.INSTANCE;
        var tokens = tokenizer.Tokenize(sentence);
        var names = mNameFinder.Find(tokens);
        ClassicAssert.IsTrue(names.Length == 1);
        ClassicAssert.IsTrue(names[0].Start == 2 && names[0].End == 3);
    }

    [Test]
    public void TestSingleTokeNameAtSentenceEnd()
    {
        const string sentence = "a b c Max";

        var tokenizer = SimpleTokenizer.INSTANCE;
        var tokens = tokenizer.Tokenize(sentence);
        var names = mNameFinder.Find(tokens);
        ClassicAssert.IsTrue(names.Length == 1);
        ClassicAssert.IsTrue(names[0].Start == 3 && names[0].End == 4);
    }

    [Test]
    public void TestLastMatchingTokenNameIsChoosen()
    {
        string[] sentence = ["a", "b", "c", "Vanessa"];
        var names = mNameFinder.Find(sentence);
        ClassicAssert.IsTrue(names.Length == 1);
        ClassicAssert.IsTrue(names[0].Start == 3 && names[0].End == 4);
    }

    [Test]
    public void TestLongerTokenNameIsPreferred()
    {
        string[] sentence = ["a", "b", "c", "Vanessa", "Williams"];
        var names = mNameFinder.Find(sentence);
        ClassicAssert.IsTrue(names.Length == 1);
        ClassicAssert.IsTrue(names[0].Start == 3 && names[0].End == 5);
    }

    [Test]
    public void TestCaseSensitivity()
    {
        string[] sentence = ["a", "b", "c", "vanessa", "williams"];
        var names = mNameFinder.Find(sentence);
        ClassicAssert.IsTrue(names.Length == 1);
        ClassicAssert.IsTrue(names[0].Start == 3 && names[0].End == 5);
    }

    [Test]
    public void TestCaseLongerEntry()
    {
        string[] sentence = ["a", "b", "michael", "jordan"];
        var names = mNameFinder.Find(sentence);
        ClassicAssert.IsTrue(names.Length == 1);
        ClassicAssert.IsTrue(names[0].Length == 2);
    }
}
