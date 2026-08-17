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
using System.Collections.Generic;
using System.Linq;
using NOpenNLP.Tools.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Doccat;

public class NGramFeatureGeneratorTest
{
    internal static readonly string[] TOKENS = ["a", "b", "c", "d", "e", "f", "g"];

    [Test]
    public void TestNull()
    {
        NGramFeatureGenerator generator = new NGramFeatureGenerator();

        // NOpenNLP: upstream expects NullPointerException; the .NET counterpart is ArgumentNullException.
        Assert.Throws<ArgumentNullException>((Action)(() =>
            generator.ExtractFeatures(null!, new Dictionary<string, object>())));
    }

    [Test]
    public void TestEmpty()
    {
        NGramFeatureGenerator generator = new NGramFeatureGenerator();

        ClassicAssert.AreEqual(0, generator.ExtractFeatures([], new Dictionary<string, object>()).Count);
    }

    [Test]
    public void TestInvalidGramSize1()
    {
        Assert.Throws<InvalidFormatException>((Action)(() => new NGramFeatureGenerator(0, 1)));
    }

    [Test]
    public void TestInvalidGramSize2()
    {
        Assert.Throws<InvalidFormatException>((Action)(() => new NGramFeatureGenerator(2, 1)));
    }

    [Test]
    public void TestUnigram()
    {
        NGramFeatureGenerator generator = new NGramFeatureGenerator(1, 1);

        CollectionAssert.AreEqual(
            new[] { "ng=:a", "ng=:b", "ng=:c", "ng=:d", "ng=:e", "ng=:f", "ng=:g" },
            generator.ExtractFeatures(TOKENS, new Dictionary<string, object>()).ToArray());
    }

    [Test]
    public void TestBigram()
    {
        NGramFeatureGenerator generator = new NGramFeatureGenerator(2, 2);

        CollectionAssert.AreEqual(
            new[] { "ng=:a:b", "ng=:b:c", "ng=:c:d", "ng=:d:e", "ng=:e:f", "ng=:f:g" },
            generator.ExtractFeatures(TOKENS, new Dictionary<string, object>()).ToArray());
    }

    [Test]
    public void TestTrigram()
    {
        NGramFeatureGenerator generator = new NGramFeatureGenerator(3, 3);

        CollectionAssert.AreEqual(
            new[] { "ng=:a:b:c", "ng=:b:c:d", "ng=:c:d:e", "ng=:d:e:f", "ng=:e:f:g" },
            generator.ExtractFeatures(TOKENS, new Dictionary<string, object>()).ToArray());
    }

    [Test]
    public void Test12gram()
    {
        NGramFeatureGenerator generator = new NGramFeatureGenerator(1, 2);

        CollectionAssert.AreEqual(
            new[]
            {
                "ng=:a", "ng=:a:b",
                "ng=:b", "ng=:b:c",
                "ng=:c", "ng=:c:d",
                "ng=:d", "ng=:d:e",
                "ng=:e", "ng=:e:f",
                "ng=:f", "ng=:f:g",
                "ng=:g"
            },
            generator.ExtractFeatures(TOKENS, new Dictionary<string, object>()).ToArray());
    }

    [Test]
    public void Test13gram()
    {
        NGramFeatureGenerator generator = new NGramFeatureGenerator(1, 3);

        CollectionAssert.AreEqual(
            new[]
            {
                "ng=:a", "ng=:a:b", "ng=:a:b:c",
                "ng=:b", "ng=:b:c", "ng=:b:c:d",
                "ng=:c", "ng=:c:d", "ng=:c:d:e",
                "ng=:d", "ng=:d:e", "ng=:d:e:f",
                "ng=:e", "ng=:e:f", "ng=:e:f:g",
                "ng=:f", "ng=:f:g",
                "ng=:g"
            },
            generator.ExtractFeatures(TOKENS, new Dictionary<string, object>()).ToArray());
    }
}
