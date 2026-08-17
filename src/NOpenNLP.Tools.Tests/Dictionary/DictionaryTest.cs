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

using System.IO;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Dictionary;

/// <summary>
/// Tests for the <see cref="Dictionary"/> class.
/// </summary>
/// <remarks>
/// NOpenNLP: upstream's <c>testSerialization</c> is not ported yet. It needs
/// <c>Dictionary.Serialize</c> and the <c>Dictionary(Stream)</c> constructor, which are
/// commented out in the port because XML dictionary serialization has not been ported.
/// </remarks>
public class DictionaryTest
{
    /// <summary>
    /// Returns a case sensitive Dictionary.
    /// </summary>
    private static Dictionary GetCaseSensitive() => new(true);

    /// <summary>
    /// Returns a case insensitive Dictionary.
    /// </summary>
    private static Dictionary GetCaseInsensitive() => new(false);

    /// <summary>
    /// Tests a basic lookup.
    /// </summary>
    [Test]
    public void TestLookup()
    {
        var entry1 = new StringList("1a", "1b");
        var entry1u = new StringList("1A", "1B");
        var entry2 = new StringList("1A", "1C");

        var dict = GetCaseInsensitive();

        dict.Put(entry1);

        ClassicAssert.IsTrue(dict.Contains(entry1));
        ClassicAssert.IsTrue(dict.Contains(entry1u));
        ClassicAssert.IsTrue(!dict.Contains(entry2));
    }

    /// <summary>
    /// Test lookup with case sensitive dictionary.
    /// </summary>
    [Test]
    public void TestLookupCaseSensitive()
    {
        var entry1 = new StringList("1a", "1b");
        var entry1u = new StringList("1A", "1B");
        var entry2 = new StringList("1A", "1C");

        var dict = GetCaseSensitive();

        dict.Put(entry1);

        ClassicAssert.IsTrue(dict.Contains(entry1));
        ClassicAssert.IsTrue(!dict.Contains(entry1u));
        ClassicAssert.IsTrue(!dict.Contains(entry2));
    }

    /// <summary>
    /// Tests for the <see cref="Dictionary.ParseOneEntryPerLine(TextReader)"/> method.
    /// </summary>
    [Test]
    public void TestParseOneEntryPerLine()
    {
        const string testDictionary = "1a 1b 1c 1d \n 2a 2b 2c \n 3a \n 4a    4b   ";

        var dictionay = Dictionary.ParseOneEntryPerLine(new StringReader(testDictionary));

        ClassicAssert.IsTrue(dictionay.Count == 4);
        ClassicAssert.IsTrue(dictionay.Contains(new StringList("1a", "1b", "1c", "1d")));
        ClassicAssert.IsTrue(dictionay.Contains(new StringList("2a", "2b", "2c")));
        ClassicAssert.IsTrue(dictionay.Contains(new StringList(["3a"])));
        ClassicAssert.IsTrue(dictionay.Contains(new StringList("4a", "4b")));
    }

    /// <summary>
    /// Tests for the <see cref="Dictionary.Equals(object)"/> method.
    /// </summary>
    [Test]
    public void TestEquals()
    {
        var entry1 = new StringList("1a", "1b");
        var entry2 = new StringList("2a", "2b");

        var dictA = GetCaseInsensitive();
        dictA.Put(entry1);
        dictA.Put(entry2);

        var dictB = GetCaseInsensitive();
        dictB.Put(entry1);
        dictB.Put(entry2);

        var dictC = GetCaseSensitive();
        dictC.Put(entry1);
        dictC.Put(entry2);

        ClassicAssert.IsTrue(dictA.Equals(dictB));
        ClassicAssert.IsTrue(dictC.Equals(dictA));
        ClassicAssert.IsTrue(dictB.Equals(dictC));
    }

    /// <summary>
    /// Tests the <see cref="Dictionary.GetHashCode"/> method.
    /// </summary>
    [Test]
    public void TestHashCode()
    {
        var entry1 = new StringList("1a", "1b");
        var entry2 = new StringList("1A", "1B");

        var dictA = GetCaseInsensitive();
        dictA.Put(entry1);

        var dictB = GetCaseInsensitive();
        dictB.Put(entry2);

        var dictC = GetCaseSensitive();
        dictC.Put(entry1);

        var dictD = GetCaseSensitive();
        dictD.Put(entry2);

        ClassicAssert.AreEqual(dictA.GetHashCode(), dictB.GetHashCode());
        ClassicAssert.AreEqual(dictB.GetHashCode(), dictC.GetHashCode());
        ClassicAssert.AreEqual(dictC.GetHashCode(), dictD.GetHashCode());
    }

    /// <summary>
    /// Tests for the <see cref="Dictionary.ToString"/> method.
    /// </summary>
    [Test]
    public void TestToString()
    {
        var entry1 = new StringList("1a", "1b");

        var dictA = GetCaseInsensitive();

        dictA.ToString();

        dictA.Put(entry1);

        dictA.ToString();
    }

    /// <summary>
    /// Tests the lookup of tokens of different case.
    /// </summary>
    [Test]
    public void TestDifferentCaseLookup()
    {
        var entry1 = new StringList("1a", "1b");
        var entry2 = new StringList("1A", "1B");

        var dict = GetCaseInsensitive();

        dict.Put(entry1);

        ClassicAssert.IsTrue(dict.Contains(entry2));
    }

    /// <summary>
    /// Tests the lookup of tokens of different case.
    /// </summary>
    [Test]
    public void TestDifferentCaseLookupCaseSensitive()
    {
        var entry1 = new StringList("1a", "1b");
        var entry2 = new StringList("1A", "1B");

        var dict = GetCaseSensitive();

        dict.Put(entry1);

        ClassicAssert.IsTrue(!dict.Contains(entry2));
    }
}
