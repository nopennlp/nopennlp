/*
 * Copyright 2026 NOpenNLP Contributors
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;
using NOpenNLP.Tools.Support;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Util.Wordvector;

/// <summary>
/// Tests for the Glove word vector parser.
/// </summary>
/// <remarks>
/// Apache OpenNLP 1.9.4 ships no tests for opennlp.tools.util.wordvector, so these
/// were authored for the port to pin the parsing behavior the Java code implies.
/// </remarks>
[NOpenNLPSpecific]
[Experimental("NONLPEXP0001")]
public class GloveTest
{
    private static Stream AsStream(string text) =>
        new MemoryStream(Encoding.UTF8.GetBytes(text));

    [Test]
    public void TestParseReadsTokensAndVectors()
    {
        using var input = AsStream("the 0.1 0.2 0.3\ncat -1.5 2.0 0.0\n");

        IWordVectorTable table = Glove.Parse(input);

        ClassicAssert.AreEqual(2, table.Count);
        ClassicAssert.AreEqual(3, table.Dimension);

        IWordVector? the = table.Get("the");
        ClassicAssert.NotNull(the);
        ClassicAssert.AreEqual(WordVectorType.Float, the!.DataType);
        ClassicAssert.AreEqual(3, the.Dimension);
        ClassicAssert.AreEqual(0.1f, the.GetAsSingle(0), 0.0001f);
        ClassicAssert.AreEqual(0.3f, the.GetAsSingle(2), 0.0001f);

        IWordVector? cat = table.Get("cat");
        ClassicAssert.NotNull(cat);
        ClassicAssert.AreEqual(-1.5d, cat!.GetAsDouble(0), 0.0001d);
    }

    [Test]
    public void TestGetReturnsNullForUnknownToken()
    {
        using var input = AsStream("the 0.1 0.2\n");

        IWordVectorTable table = Glove.Parse(input);

        // Java's Map.get returns null rather than throwing for an absent key.
        ClassicAssert.IsNull(table.Get("missing"));
    }

    [Test]
    public void TestTokensEnumeratesEveryToken()
    {
        using var input = AsStream("a 1.0\nb 2.0\nc 3.0\n");

        IWordVectorTable table = Glove.Parse(input);

        var tokens = new List<string>();
        using var enumerator = table.GetEnumerator();
        while (enumerator.MoveNext())
        {
            tokens.Add(enumerator.Current);
        }

        CollectionAssert.AreEquivalent(new[] { "a", "b", "c" }, tokens);
    }

    [Test]
    public void TestParseThrowsOnInconsistentDimension()
    {
        using var input = AsStream("the 0.1 0.2 0.3\ncat 1.0 2.0\n");

        Assert.Throws<IOException>((Action)(() => Glove.Parse(input)));
    }

    /// <summary>
    /// Java's String.split(" ") drops trailing empty fields, so a line ending in a space parses
    /// to the same dimension as one that does not. C#'s Split keeps them, which inferred a
    /// dimension one too large and then failed to parse the empty final field.
    /// </summary>
    [Test]
    public void TestParseIgnoresTrailingWhitespace()
    {
        using var input = AsStream("the 0.1 0.2 \ncat 1.0 2.0\n");

        IWordVectorTable table = Glove.Parse(input);

        ClassicAssert.AreEqual(2, table.Count);
        ClassicAssert.AreEqual(2, table.Dimension);
        ClassicAssert.AreEqual(0.2f, table.Get("the")!.GetAsSingle(1), 0.0001f);
    }

    [Test]
    public void TestDimensionIsNegativeOneWhenEmpty()
    {
        using var input = AsStream("");

        IWordVectorTable table = Glove.Parse(input);

        ClassicAssert.AreEqual(0, table.Count);
        ClassicAssert.AreEqual(-1, table.Dimension);
    }

    /// <summary>
    /// Java's Float.parseFloat is culture-invariant. Parsing with the ambient culture
    /// would misread "0.1" under a locale whose decimal separator is a comma.
    /// </summary>
    [Test]
    public void TestParseIsCultureInvariant()
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            using var input = AsStream("the 0.5 1.25\n");
            IWordVectorTable table = Glove.Parse(input);

            IWordVector? the = table.Get("the");
            ClassicAssert.NotNull(the);
            ClassicAssert.AreEqual(0.5f, the!.GetAsSingle(0), 0.0001f);
            ClassicAssert.AreEqual(1.25f, the.GetAsSingle(1), 0.0001f);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    /// <summary>
    /// The documented contract of the underlying reader is that the caller keeps
    /// ownership of the stream.
    /// </summary>
    [Test]
    public void TestParseLeavesStreamOpen()
    {
        using var input = AsStream("the 0.1 0.2\n");

        Glove.Parse(input);

        ClassicAssert.IsTrue(input.CanRead, "Glove.Parse must not close the caller's stream.");
    }

    [Test]
    public void TestToSingleBufferRoundTrips()
    {
        using var input = AsStream("the 0.5 1.5 2.5\n");

        IWordVector? the = Glove.Parse(input).Get("the");

        ClassicAssert.NotNull(the);
        CollectionAssert.AreEqual(new[] { 0.5f, 1.5f, 2.5f }, the!.ToSingleBuffer().ToArray());
        CollectionAssert.AreEqual(new[] { 0.5d, 1.5d, 2.5d }, the.ToDoubleBuffer().ToArray());
    }
}
