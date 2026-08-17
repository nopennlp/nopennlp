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
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Dictionary;

public class DictionaryAsSetCaseInsensitiveTest
{
    private static Dictionary GetDict() => new(false);

    private static StringList AsSL(string str) => new(str);

    /// <summary>
    /// Tests a basic lookup.
    /// </summary>
    [Test]
    public void TestLookup()
    {
        const string a = "a";
        const string b = "b";

        var dict = GetDict();

        dict.Put(AsSL(a));

        var set = dict.AsStringSet();

        ClassicAssert.IsTrue(set.Contains(a));
        ClassicAssert.IsFalse(set.Contains(b));

        ClassicAssert.IsTrue(set.Contains(a.ToUpperInvariant()));
    }

    /// <summary>
    /// Tests set.
    /// </summary>
    [Test]
    public void TestSet()
    {
        const string a = "a";
        const string a1 = "a";

        var dict = GetDict();

        dict.Put(AsSL(a));
        dict.Put(AsSL(a1));

        var set = dict.AsStringSet();

        ClassicAssert.IsTrue(set.Contains(a));
        ClassicAssert.AreEqual(1, set.Count);
    }

    /// <summary>
    /// Tests set.
    /// </summary>
    [Test]
    public void TestSetDiffCase()
    {
        const string a = "a";
        const string a1 = "A";

        var dict = GetDict();

        dict.Put(AsSL(a));
        dict.Put(AsSL(a1));

        var set = dict.AsStringSet();

        ClassicAssert.IsTrue(set.Contains(a));
        ClassicAssert.AreEqual(1, set.Count);
    }

    /// <summary>
    /// Tests for the <see cref="Dictionary.Equals(object)"/> method.
    /// </summary>
    [Test]
    public void TestEquals()
    {
        const string entry1 = "1a";
        const string entry2 = "1b";

        var dictA = GetDict();
        dictA.Put(AsSL(entry1));
        dictA.Put(AsSL(entry2));

        var setA = dictA.AsStringSet();

        var dictB = GetDict();
        dictB.Put(AsSL(entry1));
        dictB.Put(AsSL(entry2));

        var setB = dictB.AsStringSet();

        ClassicAssert.IsTrue(setA.Equals(setB));
    }

    /// <summary>
    /// Tests for the <see cref="Dictionary.Equals(object)"/> method.
    /// </summary>
    [Test]
    public void TestEqualsDifferentCase()
    {
        var dictA = GetDict();
        dictA.Put(AsSL("1a"));
        dictA.Put(AsSL("1b"));

        var setA = dictA.AsStringSet();

        var dictB = GetDict();
        dictB.Put(AsSL("1A"));
        dictB.Put(AsSL("1B"));

        var setB = dictB.AsStringSet();

        ClassicAssert.IsTrue(setA.Equals(setB));
    }

    /// <summary>
    /// Tests the <see cref="Dictionary.GetHashCode"/> method.
    /// </summary>
    [Test]
    public void TestHashCode()
    {
        const string entry1 = "a1";

        var dictA = GetDict();
        dictA.Put(AsSL(entry1));

        var setA = dictA.AsStringSet();

        var dictB = GetDict();
        dictB.Put(AsSL(entry1));

        var setB = dictB.AsStringSet();

        ClassicAssert.AreEqual(setA.GetHashCode(), setB.GetHashCode());
    }

    /// <summary>
    /// Tests the <see cref="Dictionary.GetHashCode"/> method.
    /// </summary>
    [Test]
    public void TestHashCodeDifferentCase()
    {
        const string entry1 = "a1";

        var dictA = GetDict();
        dictA.Put(AsSL(entry1));

        var setA = dictA.AsStringSet();

        var dictB = GetDict();
        dictB.Put(AsSL(entry1.ToUpperInvariant()));

        var setB = dictB.AsStringSet();

        // TODO: should it be equal??
        // NOpenNLP: upstream uses assertNotSame, which on Java's boxed Integers compares
        // references. The .NET counterpart of that intent is that the two hash codes differ.
        ClassicAssert.AreNotEqual(setA.GetHashCode(), setB.GetHashCode());
    }

    /// <summary>
    /// Tests the lookup of tokens of different case.
    /// </summary>
    [Test]
    public void TestDifferentCaseLookup()
    {
        const string entry1 = "1a";
        const string entry2 = "1A";

        var dict = GetDict();

        dict.Put(AsSL(entry1));

        var set = dict.AsStringSet();

        ClassicAssert.IsTrue(set.Contains(entry2));
    }

    /// <summary>
    /// Tests the iterator implementation.
    /// </summary>
    [Test]
    public void TestIterator()
    {
        const string entry1 = "1a";
        const string entry2 = "1b";

        var dictA = GetDict();
        dictA.Put(AsSL(entry1));
        dictA.Put(AsSL(entry2));
        dictA.Put(AsSL(entry1.ToUpperInvariant()));
        dictA.Put(AsSL(entry2.ToUpperInvariant()));

        List<string> elements = [];
        foreach (string element in dictA.AsStringSet())
        {
            elements.Add(element);
        }

        ClassicAssert.AreEqual(2, elements.Count);
        ClassicAssert.IsTrue(elements.Contains(entry1));
        ClassicAssert.IsTrue(elements.Contains(entry2));
    }
}
