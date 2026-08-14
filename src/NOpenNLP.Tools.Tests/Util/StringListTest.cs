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

// This file has been modified from the original Apache OpenNLP 1.9.1 source:
// translated from Java to C# and adapted for .NET. See NOTICE.
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Util;

/// <summary>
/// Tests for the <see cref="StringList"/> class.
/// </summary>
public class StringListTest
{
    /// <summary>
    /// Tests <see cref="StringList"/> which uses <see cref="string.Intern(string)"/>.
    /// </summary>
    [Test]
    public void TestIntern()
    {
        StringList l1 = new StringList("a");
        StringList l2 = new StringList("a", "b");
        ClassicAssert.IsTrue(ReferenceEquals(l1.GetToken(0), l2.GetToken(0)));
    }

    /// <summary>
    /// Tests <see cref="StringList.GetToken(int)"/>.
    /// </summary>
    [Test]
    public void TestGetToken()
    {
        StringList l = new StringList("a", "b");
        ClassicAssert.AreEqual(2, l.Count);
        ClassicAssert.AreEqual("a", l.GetToken(0));
        ClassicAssert.AreEqual("b", l.GetToken(1));
    }

    /// <summary>
    /// Tests <see cref="StringList.GetEnumerator()"/>.
    /// </summary>
    [Test]
    public void TestIterator()
    {
        // NOpenNLP: IEnumerator has no HasNext; MoveNext advances and reports
        // whether an element was available, so upstream's hasNext/next pairs
        // collapse into a MoveNext check followed by reading Current.
        StringList l = new StringList("a");
        IEnumerator<string> it = l.GetEnumerator();
        ClassicAssert.IsTrue(it.MoveNext());
        ClassicAssert.AreEqual("a", it.Current);
        ClassicAssert.IsFalse(it.MoveNext());

        // now test with more than one string
        l = new StringList("a", "b", "c");
        it = l.GetEnumerator();

        ClassicAssert.IsTrue(it.MoveNext());
        ClassicAssert.AreEqual("a", it.Current);
        ClassicAssert.IsTrue(it.MoveNext());
        ClassicAssert.AreEqual("b", it.Current);
        ClassicAssert.IsTrue(it.MoveNext());
        ClassicAssert.AreEqual("c", it.Current);
        ClassicAssert.IsFalse(it.MoveNext());
    }

    /// <summary>
    /// Tests <see cref="StringList.CompareToIgnoreCase(StringList)"/>.
    /// </summary>
    [Test]
    public void TestCompareToIgnoreCase()
    {
        ClassicAssert.IsTrue(new StringList("a", "b").CompareToIgnoreCase(
            new StringList("A", "B")));
    }

    /// <summary>
    /// Tests <see cref="StringList.Equals(object)"/>.
    /// </summary>
    [Test]
    public void TestEquals()
    {
        ClassicAssert.AreEqual(new StringList("a", "b"),
            new StringList("a", "b"));

        ClassicAssert.IsFalse(new StringList("a", "b").Equals(
            new StringList("A", "B")));
    }

    /// <summary>
    /// Tests <see cref="StringList.GetHashCode()"/>.
    /// </summary>
    [Test]
    public void TestHashCode()
    {
        ClassicAssert.AreEqual(new StringList("a", "b").GetHashCode(),
            new StringList("a", "b").GetHashCode());
        ClassicAssert.AreNotEqual(new StringList("a", "b").GetHashCode(),
            new StringList("a", "c").GetHashCode());
    }

    /// <summary>
    /// Tests <see cref="StringList.ToString()"/>.
    /// </summary>
    [Test]
    public void TestToString()
    {
        ClassicAssert.AreEqual("[a]", new StringList("a").ToString());
        ClassicAssert.AreEqual("[a,b]", new StringList("a", "b").ToString());
    }
}
