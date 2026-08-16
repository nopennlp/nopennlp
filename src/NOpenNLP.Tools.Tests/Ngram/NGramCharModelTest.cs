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

namespace NOpenNLP.Tools.Ngram;

/// <summary>
/// Tests for <see cref="NGramCharModel"/>.
/// </summary>
public class NGramCharModelTest
{
    [Test]
    public void TestZeroGetCount()
    {
        var ngramModel = new NGramCharModel();
        int count = ngramModel.GetCount("");
        ClassicAssert.AreEqual(0, count);
        ClassicAssert.AreEqual(0, ngramModel.Count);
    }

    [Test]
    public void TestZeroGetCount2()
    {
        var ngramModel = new NGramCharModel();
        ngramModel.Add("the");
        int count = ngramModel.GetCount("fox");
        ClassicAssert.AreEqual(0, count);
        ClassicAssert.AreEqual(1, ngramModel.Count);
    }

    [Test]
    public void TestAdd()
    {
        var ngramModel = new NGramCharModel();
        ngramModel.Add("fox");
        int count = ngramModel.GetCount("the");
        ClassicAssert.AreEqual(0, count);
        ClassicAssert.AreEqual(1, ngramModel.Count);
    }

    [Test]
    public void TestAdd1()
    {
        var ngramModel = new NGramCharModel();
        ngramModel.Add("the");
        int count = ngramModel.GetCount("the");
        ClassicAssert.AreEqual(1, count);
        ClassicAssert.AreEqual(1, ngramModel.Count);
    }

    [Test]
    public void TestAdd2()
    {
        var ngramModel = new NGramCharModel();
        ngramModel.Add("the", 1, 3);
        int count = ngramModel.GetCount("th");
        ClassicAssert.AreEqual(1, count);
        ClassicAssert.AreEqual(6, ngramModel.Count);
    }

    [Test]
    public void TestRemove()
    {
        var ngramModel = new NGramCharModel();
        string ngram = "the";
        ngramModel.Add(ngram);
        ngramModel.Remove(ngram);
        ClassicAssert.AreEqual(0, ngramModel.Count);
    }

    [Test]
    public void TestContains()
    {
        var ngramModel = new NGramCharModel();
        string token = "the";
        ngramModel.Add(token);
        ClassicAssert.IsFalse(ngramModel.Contains("fox"));
    }

    [Test]
    public void TestContains2()
    {
        var ngramModel = new NGramCharModel();
        string token = "the";
        ngramModel.Add(token, 1, 3);
        ClassicAssert.IsTrue(ngramModel.Contains("the"));
    }

    [Test]
    public void TestCutoff1()
    {
        var ngramModel = new NGramCharModel();
        string token = "the";
        ngramModel.Add(token, 1, 3);
        ngramModel.Cutoff(2, 4);
        ClassicAssert.AreEqual(0, ngramModel.Count);
    }
}
