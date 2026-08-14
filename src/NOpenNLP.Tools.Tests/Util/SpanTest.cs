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
using J2N.Text;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Util;

/// <summary>
/// Tests for the <see cref="Span"/> class.
/// </summary>
public class SpanTest
{
    /// <summary>
    /// Test for <see cref="Span.Start"/>.
    /// </summary>
    [Test]
    public void TestGetStart()
    {
        ClassicAssert.AreEqual(5, new Span(5, 6).Start);
    }

    /// <summary>
    /// Test for <see cref="Span.End"/>.
    /// </summary>
    [Test]
    public void TestGetEnd()
    {
        ClassicAssert.AreEqual(6, new Span(5, 6).End);
    }

    /// <summary>
    /// Test for <see cref="Span.Length"/>.
    /// </summary>
    [Test]
    public void TestLength()
    {
        ClassicAssert.AreEqual(11, new Span(10, 21).Length);
    }

    /// <summary>
    /// Test for <see cref="Span.Contains(Span)"/>.
    /// </summary>
    [Test]
    public void TestContains()
    {
        Span a = new Span(500, 900);
        Span b = new Span(520, 600);

        ClassicAssert.AreEqual(true, a.Contains(b));
    }

    /// <summary>
    /// Test for <see cref="Span.Contains(Span)"/>.
    /// </summary>
    [Test]
    public void TestContainsWithEqual()
    {
        Span a = new Span(500, 900);
        ClassicAssert.AreEqual(true, a.Contains(a));
    }

    /// <summary>
    /// Test for <see cref="Span.Contains(Span)"/>.
    /// </summary>
    [Test]
    public void TestContainsWithLowerIntersect()
    {
        Span a = new Span(500, 900);
        Span b = new Span(450, 1000);
        ClassicAssert.AreEqual(false, a.Contains(b));
    }

    /// <summary>
    /// Test for <see cref="Span.Contains(Span)"/>.
    /// </summary>
    [Test]
    public void TestContainsWithHigherIntersect()
    {
        Span a = new Span(500, 900);
        Span b = new Span(500, 1000);
        ClassicAssert.AreEqual(false, a.Contains(b));
    }

    /// <summary>
    /// Test for <see cref="Span.Contains(int)"/>.
    /// </summary>
    [Test]
    public void TestContainsInt32()
    {
        Span a = new Span(10, 300);

        /* NOTE: here the span does not contain the endpoint marked as the end
         * for the span.  This is because the end should be placed one past the
         * true end for the span.  The indexes used must observe the same
         * requirements for the contains function.
         */
        ClassicAssert.IsFalse(a.Contains(9));
        ClassicAssert.IsTrue(a.Contains(10));
        ClassicAssert.IsTrue(a.Contains(200));
        ClassicAssert.IsTrue(a.Contains(299));
        ClassicAssert.IsFalse(a.Contains(300));
    }

    /// <summary>
    /// Test for <see cref="Span.StartsWith(Span)"/>.
    /// </summary>
    [Test]
    public void TestStartsWith()
    {
        Span a = new Span(10, 50);
        Span b = new Span(10, 12);

        ClassicAssert.IsTrue(a.StartsWith(a));
        ClassicAssert.IsTrue(a.StartsWith(b));
        ClassicAssert.IsFalse(b.StartsWith(a));
    }

    /// <summary>
    /// Test for <see cref="Span.Intersects(Span)"/>.
    /// </summary>
    [Test]
    public void TestIntersects()
    {
        Span a = new Span(10, 50);
        Span b = new Span(40, 100);

        ClassicAssert.IsTrue(a.Intersects(b));
        ClassicAssert.IsTrue(b.Intersects(a));

        Span c = new Span(10, 20);
        Span d = new Span(40, 50);

        ClassicAssert.IsFalse(c.Intersects(d));
        ClassicAssert.IsFalse(d.Intersects(c));
        ClassicAssert.IsTrue(b.Intersects(d));
    }

    /// <summary>
    /// Test for <see cref="Span.Crosses(Span)"/>.
    /// </summary>
    [Test]
    public void TestCrosses()
    {
        Span a = new Span(10, 50);
        Span b = new Span(40, 100);

        ClassicAssert.IsTrue(a.Crosses(b));
        ClassicAssert.IsTrue(b.Crosses(a));

        Span c = new Span(10, 20);
        Span d = new Span(40, 50);

        ClassicAssert.IsFalse(c.Crosses(d));
        ClassicAssert.IsFalse(d.Crosses(c));
        ClassicAssert.IsFalse(b.Crosses(d));
    }

    /// <summary>
    /// Test for <see cref="Span.CompareTo(Span)"/>.
    /// </summary>
    [Test]
    public void TestCompareToLower()
    {
        Span a = new Span(100, 1000);
        Span b = new Span(10, 50);
        ClassicAssert.AreEqual(true, a.CompareTo(b) > 0);
    }

    /// <summary>
    /// Test for <see cref="Span.CompareTo(Span)"/>.
    /// </summary>
    [Test]
    public void TestCompareToHigher()
    {
        Span a = new Span(100, 200);
        Span b = new Span(300, 400);
        ClassicAssert.AreEqual(true, a.CompareTo(b) < 0);
    }

    /// <summary>
    /// Test for <see cref="Span.CompareTo(Span)"/>.
    /// </summary>
    [Test]
    public void TestCompareToEquals()
    {
        Span a = new Span(30, 1000);
        Span b = new Span(30, 1000);
        ClassicAssert.AreEqual(true, a.CompareTo(b) == 0);
    }

    ///

    /// <summary>
    /// Test for <see cref="Span.CompareTo(Span)"/>.
    /// </summary>
    [Test]
    public void TestCompareToEqualsSameType()
    {
        Span a = new Span(30, 1000, "a");
        Span b = new Span(30, 1000, "a");
        ClassicAssert.AreEqual(true, a.CompareTo(b) == 0);
    }

    /// <summary>
    /// Test for <see cref="Span.CompareTo(Span)"/>.
    /// </summary>
    [Test]
    public void TestCompareToEqualsDiffType1()
    {
        Span a = new Span(30, 1000, "a");
        Span b = new Span(30, 1000, "b");
        ClassicAssert.AreEqual(true, a.CompareTo(b) == -1);
    }

    /// <summary>
    /// Test for <see cref="Span.CompareTo(Span)"/>.
    /// </summary>
    [Test]
    public void TestCompareToEqualsDiffType2()
    {
        Span a = new Span(30, 1000, "b");
        Span b = new Span(30, 1000, "a");
        ClassicAssert.AreEqual(true, a.CompareTo(b) == 1);
    }

    /// <summary>
    /// Test for <see cref="Span.CompareTo(Span)"/>.
    /// </summary>
    [Test]
    public void TestCompareToEqualsNullType1()
    {
        Span a = new Span(30, 1000);
        Span b = new Span(30, 1000, "b");
        ClassicAssert.AreEqual(true, a.CompareTo(b) == 1);
    }

    /// <summary>
    /// Test for <see cref="Span.CompareTo(Span)"/>.
    /// </summary>
    [Test]
    public void TestCompareToEqualsNullType2()
    {
        Span a = new Span(30, 1000, "b");
        Span b = new Span(30, 1000);
        ClassicAssert.AreEqual(true, a.CompareTo(b) == -1);
    }

    /// <summary>
    /// Test for <see cref="Span.GetHashCode()"/>.
    /// </summary>
    [Test]
    public void TesthHashCode()
    {
        ClassicAssert.AreEqual(new Span(10, 11), new Span(10, 11));
    }

    /// <summary>
    /// Test for <see cref="Span.Equals(object)"/>.
    /// </summary>
    [Test]
    public void TestEqualsWithNull()
    {
        Span a = new Span(0, 0);
        ClassicAssert.AreEqual(a.Equals(null), false);
    }

    /// <summary>
    /// Test for <see cref="Span.Equals(object)"/>.
    /// </summary>
    [Test]
    public void TestEquals()
    {
        Span a1 = new Span(100, 1000, "test");
        Span a2 = new Span(100, 1000, "test");
        ClassicAssert.IsTrue(a1.Equals(a2));

        // end is different
        Span b1 = new Span(100, 100, "test");
        ClassicAssert.IsFalse(a1.Equals(b1));

        // type is different
        Span c1 = new Span(100, 1000, "Test");
        ClassicAssert.IsFalse(a1.Equals(c1));

        Span d1 = new Span(100, 1000);
        ClassicAssert.IsFalse(d1.Equals(a1));
        ClassicAssert.IsFalse(a1.Equals(d1));
    }

    /// <summary>
    /// Test for <see cref="Span.ToString()"/>.
    /// </summary>
    [Test]
    public void TestToString()
    {
        ClassicAssert.AreEqual("[50..100)", new Span(50, 100).ToString());
        ClassicAssert.AreEqual("[50..100) myType", new Span(50, 100, "myType").ToString());
    }

    [Test]
    public void TestTrim()
    {
        // NOpenNLP: Trim and GetCoveredText take an ICharSequence, so the string
        // is adapted with J2N's AsCharSequence(); upstream passes the String directly.
        string string1 = "  12 34  ";
        Span span1 = new Span(0, string1.Length);
        ClassicAssert.AreEqual("12 34",
            span1.Trim(string1.AsCharSequence()).GetCoveredText(string1.AsCharSequence()).ToString());
    }

    [Test]
    public void TestTrimWhitespaceSpan()
    {
        // NOpenNLP: see TestTrim regarding AsCharSequence().
        string string1 = "              ";
        Span span1 = new Span(0, string1.Length);
        ClassicAssert.AreEqual("",
            span1.Trim(string1.AsCharSequence()).GetCoveredText(string1.AsCharSequence()).ToString());
    }

    /// <summary>
    /// Test if it fails to construct span with invalid start
    /// </summary>
    [Test]
    public void TestTooSmallStart()
    {
        // NOpenNLP: upstream declares @Test(expected = IllegalArgumentException.class);
        // ArgumentException is the .NET counterpart.
        Assert.Throws<ArgumentException>((Action)(() => new Span(-1, 100)));
    }

    /// <summary>
    /// Test if it fails to construct span with invalid end
    /// </summary>
    [Test]
    public void TestTooSmallEnd()
    {
        // NOpenNLP: see TestTooSmallStart regarding the exception type.
        Assert.Throws<ArgumentException>((Action)(() => new Span(50, -1)));
    }

    /// <summary>
    /// Test if it fails to construct span with start > end
    /// </summary>
    [Test]
    public void TestStartLargerThanEnd()
    {
        // NOpenNLP: see TestTooSmallStart regarding the exception type.
        Assert.Throws<ArgumentException>((Action)(() => new Span(100, 50)));
    }
}
