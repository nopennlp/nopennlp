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
using System;
using System.Collections.Generic;
using System.Text;
using J2N;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Util;

/// <summary>
/// Tests for the <see cref="StringUtil"/> class.
/// </summary>
public class StringUtilTest
{
    [Test]
    public void TestNoBreakSpace()
    {
        ClassicAssert.IsTrue(StringUtil.IsWhitespace(0x00A0));
        ClassicAssert.IsTrue(StringUtil.IsWhitespace(0x2007));
        ClassicAssert.IsTrue(StringUtil.IsWhitespace(0x202F));

        ClassicAssert.IsTrue(StringUtil.IsWhitespace((char)0x00A0));
        ClassicAssert.IsTrue(StringUtil.IsWhitespace((char)0x2007));
        ClassicAssert.IsTrue(StringUtil.IsWhitespace((char)0x202F));
    }

    [Test]
    public void TestToLowerCase()
    {
        ClassicAssert.AreEqual("test", StringUtil.ToLowerCase("TEST"));
        ClassicAssert.AreEqual("simple", StringUtil.ToLowerCase("SIMPLE"));
    }

    [Test]
    public void TestToUpperCase()
    {
        ClassicAssert.AreEqual("TEST", StringUtil.ToUpperCase("test"));
        ClassicAssert.AreEqual("SIMPLE", StringUtil.ToUpperCase("simple"));
    }

    [Test]
    public void TestIsEmpty()
    {
        ClassicAssert.IsTrue(StringUtil.IsEmpty(""));
        ClassicAssert.IsTrue(!StringUtil.IsEmpty("a"));
    }

    [Test]
    public void TestIsEmptyWithNullString()
    {
        // NOpenNLP: upstream declares @Test(expected = NullPointerException.class);
        // C# has no attribute equivalent, so the throw is asserted explicitly.
        // NullReferenceException is the .NET counterpart of NullPointerException.
        Assert.Throws<NullReferenceException>((Action)(() => StringUtil.IsEmpty(null)));
    }

    [Test]
    public void TestLowercaseBeyondBMP()
    {
        int[] codePoints = [65, 66578, 67]; // A, Deseret capital BEE, C
        int[] expectedCodePoints = [97, 66618, 99]; // a, Deseret lowercase b, c
        string input = CodePointsToString(codePoints);
        string lc = StringUtil.ToLowerCase(input);
        CollectionAssert.AreEqual(expectedCodePoints, StringToCodePoints(lc));
    }

    // NOpenNLP: stands in for Java's new String(int[], int, int) and
    // String.codePoints(), which have no direct .NET counterpart.
    private static string CodePointsToString(int[] codePoints)
    {
        var buffer = new StringBuilder(codePoints.Length);
        foreach (int codePoint in codePoints)
        {
            buffer.Append(Character.ToChars(codePoint));
        }

        return buffer.ToString();
    }

    private static int[] StringToCodePoints(string value)
    {
        var codePoints = new List<int>();
        for (int i = 0; i < value.Length; i += Character.CharCount(Character.CodePointAt(value, i)))
        {
            codePoints.Add(Character.CodePointAt(value, i));
        }

        return codePoints.ToArray();
    }
}
