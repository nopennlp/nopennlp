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
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Ngram;

public class NGramGeneratorTest
{
    [Test]
    public void GenerateListTest1()
    {
        IList<string> input = ["This", "is", "a", "sentence"];
        const int window = 1;
        const string separator = "-";

        var ngrams = NGramGenerator.Generate(input, window, separator);

        ClassicAssert.AreEqual(4, ngrams.Count);
        ClassicAssert.AreEqual("This", ngrams[0]);
        ClassicAssert.AreEqual("is", ngrams[1]);
        ClassicAssert.AreEqual("a", ngrams[2]);
        ClassicAssert.AreEqual("sentence", ngrams[3]);
    }

    [Test]
    public void GenerateListTest2()
    {
        IList<string> input = ["This", "is", "a", "sentence"];
        const int window = 2;
        const string separator = "-";

        var ngrams = NGramGenerator.Generate(input, window, separator);

        ClassicAssert.AreEqual(3, ngrams.Count);
        ClassicAssert.AreEqual("This-is", ngrams[0]);
        ClassicAssert.AreEqual("is-a", ngrams[1]);
        ClassicAssert.AreEqual("a-sentence", ngrams[2]);
    }

    [Test]
    public void GenerateListTest3()
    {
        IList<string> input = ["This", "is", "a", "sentence"];
        const int window = 3;
        const string separator = "-";

        var ngrams = NGramGenerator.Generate(input, window, separator);

        ClassicAssert.AreEqual(2, ngrams.Count);
        ClassicAssert.AreEqual("This-is-a", ngrams[0]);
        ClassicAssert.AreEqual("is-a-sentence", ngrams[1]);
    }

    [Test]
    public void GenerateListTest4()
    {
        IList<string> input = ["This", "is", "a", "sentence"];
        const int window = 4;
        const string separator = "-";

        var ngrams = NGramGenerator.Generate(input, window, separator);

        ClassicAssert.AreEqual(1, ngrams.Count);
        ClassicAssert.AreEqual("This-is-a-sentence", ngrams[0]);
    }

    [Test]
    public void GenerateCharTest1()
    {
        char[] input = "Test".ToCharArray();
        const int window = 1;
        const string separator = "-";

        var ngrams = NGramGenerator.Generate(input, window, separator);

        ClassicAssert.AreEqual(4, ngrams.Count);
        ClassicAssert.AreEqual("T", ngrams[0]);
        ClassicAssert.AreEqual("e", ngrams[1]);
        ClassicAssert.AreEqual("s", ngrams[2]);
        ClassicAssert.AreEqual("t", ngrams[3]);
    }

    [Test]
    public void GenerateCharTest2()
    {
        char[] input = "Test".ToCharArray();
        const int window = 2;
        const string separator = "-";

        var ngrams = NGramGenerator.Generate(input, window, separator);

        ClassicAssert.AreEqual(3, ngrams.Count);
        ClassicAssert.AreEqual("T-e", ngrams[0]);
        ClassicAssert.AreEqual("e-s", ngrams[1]);
        ClassicAssert.AreEqual("s-t", ngrams[2]);
    }

    [Test]
    public void GenerateCharTest3()
    {
        char[] input = "Test".ToCharArray();
        const int window = 3;
        const string separator = "-";

        var ngrams = NGramGenerator.Generate(input, window, separator);

        ClassicAssert.AreEqual(2, ngrams.Count);
        ClassicAssert.AreEqual("T-e-s", ngrams[0]);
        ClassicAssert.AreEqual("e-s-t", ngrams[1]);
    }

    [Test]
    public void GenerateCharTest4()
    {
        char[] input = "Test".ToCharArray();
        const int window = 4;
        const string separator = "-";

        var ngrams = NGramGenerator.Generate(input, window, separator);

        ClassicAssert.AreEqual(1, ngrams.Count);
        ClassicAssert.AreEqual("T-e-s-t", ngrams[0]);
    }

    [Test]
    public void GenerateCharTest()
    {
        char[] input = "Test again".ToCharArray();
        const int window = 4;
        const string separator = "-";

        var ngrams = NGramGenerator.Generate(input, window, separator);

        ClassicAssert.AreEqual(7, ngrams.Count);
        ClassicAssert.AreEqual("T-e-s-t", ngrams[0]);
        ClassicAssert.AreEqual("e-s-t- ", ngrams[1]);
        ClassicAssert.AreEqual("s-t- -a", ngrams[2]);
        ClassicAssert.AreEqual("t- -a-g", ngrams[3]);
        ClassicAssert.AreEqual(" -a-g-a", ngrams[4]);
        ClassicAssert.AreEqual("a-g-a-i", ngrams[5]);
        ClassicAssert.AreEqual("g-a-i-n", ngrams[6]);
    }

    [Test]
    public void GenerateLargerWindowThanListTest()
    {
        IList<string> input = ["One", "two"];
        const int window = 3;
        const string separator = "-";

        var ngrams = NGramGenerator.Generate(input, window, separator);

        ClassicAssert.IsEmpty(ngrams);
    }

    [Test]
    public void EmptyTest()
    {
        IList<string> input = [];
        const int window = 2;
        const string separator = "-";

        var ngrams = NGramGenerator.Generate(input, window, separator);

        ClassicAssert.IsEmpty(ngrams);
    }
}
