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
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Ngram;

/// <summary>
/// Tests for <see cref="NGramModel"/>.
/// </summary>
/// <remarks>
/// NOpenNLP: upstream's <c>testInvalidFormat</c>, <c>testFromFile</c>, <c>testSerialize</c>,
/// <c>testFromInvalidFileMissingCount</c> and <c>testFromInvalidFileNotANumber</c> are not ported
/// yet. They need the <c>NGramModel(Stream)</c> constructor and <c>Serialize</c>, which are
/// commented out in the port because XML dictionary serialization has not been ported.
/// </remarks>
public class NGramModelTest
{
    [Test]
    public void TestZeroGetCount()
    {
        var ngramModel = new NGramModel();
        int count = ngramModel.GetCount(new StringList(""));
        ClassicAssert.AreEqual(0, count);
        ClassicAssert.AreEqual(0, ngramModel.Count);
    }

    [Test]
    public void TestZeroGetCount2()
    {
        var ngramModel = new NGramModel();
        ngramModel.Add(new StringList("the", "bro", "wn"));
        int count = ngramModel.GetCount(new StringList("fox"));
        ClassicAssert.AreEqual(0, count);
        ClassicAssert.AreEqual(1, ngramModel.Count);
    }

    [Test]
    public void TestAdd()
    {
        var ngramModel = new NGramModel();
        ngramModel.Add(new StringList("the", "bro", "wn"));
        int count = ngramModel.GetCount(new StringList("the"));
        ClassicAssert.AreEqual(0, count);
        ClassicAssert.AreEqual(1, ngramModel.Count);
    }

    [Test]
    public void TestAdd1()
    {
        var ngramModel = new NGramModel();
        ngramModel.Add(new StringList("the", "bro", "wn"));
        int count = ngramModel.GetCount(new StringList("the", "bro", "wn"));
        ClassicAssert.AreEqual(1, count);
        ClassicAssert.AreEqual(1, ngramModel.Count);
    }

    [Test]
    public void TestAdd2()
    {
        var ngramModel = new NGramModel();
        ngramModel.Add(new StringList("the", "bro", "wn"), 2, 3);
        int count = ngramModel.GetCount(new StringList("the", "bro", "wn"));
        ClassicAssert.AreEqual(1, count);
        ClassicAssert.AreEqual(3, ngramModel.Count);
    }

    [Test]
    public void TestAdd3()
    {
        var ngramModel = new NGramModel();
        ngramModel.Add(new StringList("the", "brown", "fox"), 2, 3);
        int count = ngramModel.GetCount(new StringList("the", "brown", "fox"));
        ClassicAssert.AreEqual(1, count);
        count = ngramModel.GetCount(new StringList("the", "brown"));
        ClassicAssert.AreEqual(1, count);
        count = ngramModel.GetCount(new StringList("brown", "fox"));
        ClassicAssert.AreEqual(1, count);
        ClassicAssert.AreEqual(3, ngramModel.Count);
    }

    [Test]
    public void TestRemove()
    {
        var ngramModel = new NGramModel();
        var tokens = new StringList("the", "bro", "wn");
        ngramModel.Add(tokens);
        ngramModel.Remove(tokens);
        ClassicAssert.AreEqual(0, ngramModel.Count);
    }

    [Test]
    public void TestContains()
    {
        var ngramModel = new NGramModel();
        var tokens = new StringList("the", "bro", "wn");
        ngramModel.Add(tokens);
        ClassicAssert.IsFalse(ngramModel.Contains(new StringList("the")));
    }

    [Test]
    public void TestContains2()
    {
        var ngramModel = new NGramModel();
        var tokens = new StringList("the", "bro", "wn");
        ngramModel.Add(tokens, 1, 3);
        ClassicAssert.IsTrue(ngramModel.Contains(new StringList("the")));
    }

    [Test]
    public void TestNumberOfGrams()
    {
        var ngramModel = new NGramModel();
        var tokens = new StringList("the", "bro", "wn");
        ngramModel.Add(tokens, 1, 3);
        ClassicAssert.AreEqual(6, ngramModel.NumberOfGrams);
    }

    [Test]
    public void TestCutoff1()
    {
        var ngramModel = new NGramModel();
        var tokens = new StringList("the", "brown", "fox", "jumped");
        ngramModel.Add(tokens, 1, 3);
        ngramModel.Cutoff(2, 4);
        ClassicAssert.AreEqual(0, ngramModel.Count);
    }

    [Test]
    public void TestCutoff2()
    {
        var ngramModel = new NGramModel();
        var tokens = new StringList("the", "brown", "fox", "jumped");
        ngramModel.Add(tokens, 1, 3);
        ngramModel.Cutoff(1, 3);
        ClassicAssert.AreEqual(9, ngramModel.Count);
    }

    [Test]
    public void TestToDictionary()
    {
        var ngramModel = new NGramModel();
        var tokens = new StringList("the", "brown", "fox", "jumped");
        ngramModel.Add(tokens, 1, 3);
        tokens = new StringList("the", "brown", "Fox", "jumped");
        ngramModel.Add(tokens, 1, 3);
        var dictionary = ngramModel.ToDictionary();
        ClassicAssert.NotNull(dictionary);
        ClassicAssert.AreEqual(9, dictionary.Count);
        ClassicAssert.AreEqual(1, dictionary.MinTokenCount);
        ClassicAssert.AreEqual(3, dictionary.MaxTokenCount);
    }

    [Test]
    public void TestToDictionary1()
    {
        var ngramModel = new NGramModel();
        var tokens = new StringList("the", "brown", "fox", "jumped");
        ngramModel.Add(tokens, 1, 3);
        tokens = new StringList("the", "brown", "Fox", "jumped");
        ngramModel.Add(tokens, 1, 3);
        var dictionary = ngramModel.ToDictionary(true);
        ClassicAssert.NotNull(dictionary);
        ClassicAssert.AreEqual(14, dictionary.Count);
        ClassicAssert.AreEqual(1, dictionary.MinTokenCount);
        ClassicAssert.AreEqual(3, dictionary.MaxTokenCount);
    }
}
