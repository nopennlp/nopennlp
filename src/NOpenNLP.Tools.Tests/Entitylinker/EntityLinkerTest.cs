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
using System.IO;
using System.Text;
using NOpenNLP.Tools.Support;
using NOpenNLP.Tools.Util;
using NOpenNLP.Tools.Util.Ext;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Entitylinker;

/// <summary>
/// Tests for the entity linker framework.
/// </summary>
/// <remarks>
/// Apache OpenNLP 1.9.4 ships no tests for opennlp.tools.entitylinker, so these were
/// authored for the port to pin the behavior the Java code implies.
/// </remarks>
[NOpenNLPSpecific]
public class EntityLinkerTest
{
    private sealed class TestLink : BaseLink
    {
        public TestLink()
        {
        }

        public TestLink(string? itemParentID, string? itemID, string? itemName, string? itemType)
            : base(itemParentID, itemID, itemName, itemType)
        {
        }
    }

    private static Stream AsStream(string text) =>
        new MemoryStream(Encoding.UTF8.GetBytes(text));

    [Test]
    public void TestBaseLinkEqualsAndHashCode()
    {
        var a = new TestLink("p", "i", "n", "t");
        var b = new TestLink("p", "i", "n", "t");
        var c = new TestLink("p", "i", "n", "other");

        ClassicAssert.AreEqual(a, b);
        ClassicAssert.AreEqual(a.GetHashCode(), b.GetHashCode());
        ClassicAssert.AreNotEqual(a, c);
        ClassicAssert.AreEqual(a, a);
        ClassicAssert.AreNotEqual(a, null);
    }

    [Test]
    public void TestBaseLinkScoreMapDefaultsToEmpty()
    {
        var link = new TestLink();

        ClassicAssert.NotNull(link.ScoreMap);
        ClassicAssert.AreEqual(0, link.ScoreMap.Count);

        link.ScoreMap["confidence"] = 0.75d;
        ClassicAssert.AreEqual(0.75d, link.ScoreMap["confidence"], 0.0001d);
    }

    [Test]
    public void TestLinkedSpanRetainsSpanSemantics()
    {
        var entries = new List<TestLink> { new("p", "i", "n", "t") };
        var span = new LinkedSpan<TestLink>(entries, 3, 7, "person");

        ClassicAssert.AreEqual(3, span.Start);
        ClassicAssert.AreEqual(7, span.End);
        ClassicAssert.AreEqual("person", span.Type);
        ClassicAssert.AreEqual(entries, span.LinkedEntries);
        ClassicAssert.AreEqual(0, span.SentenceId);
        ClassicAssert.IsNull(span.SearchTerm);
    }

    [Test]
    public void TestLinkedSpanOffsetConstructor()
    {
        var entries = new List<TestLink>();
        var span = new LinkedSpan<TestLink>(entries, new Span(2, 5, "loc"), 10);

        ClassicAssert.AreEqual(12, span.Start);
        ClassicAssert.AreEqual(15, span.End);
        ClassicAssert.AreEqual("loc", span.Type);
    }

    /// <summary>
    /// Java compares the linked entries with ArrayList.equals, which is element-wise.
    /// Comparing by reference instead would report two equal spans as different.
    /// </summary>
    [Test]
    public void TestLinkedSpanEqualsComparesEntriesElementWise()
    {
        var a = new LinkedSpan<TestLink>([new TestLink("p", "i", "n", "t")], 0, 4)
        {
            SentenceId = 2,
            SearchTerm = "term"
        };

        var b = new LinkedSpan<TestLink>([new TestLink("p", "i", "n", "t")], 0, 4)
        {
            SentenceId = 2,
            SearchTerm = "term"
        };

        ClassicAssert.AreEqual(a, b);
        ClassicAssert.AreEqual(a.GetHashCode(), b.GetHashCode());

        b.SentenceId = 3;
        ClassicAssert.AreNotEqual(a, b);
    }

    /// <summary>
    /// Java renders the entries with ArrayList.toString, as "[a, b]". Concatenating a BCL
    /// List&lt;T&gt; would emit its type name instead.
    /// </summary>
    [Test]
    public void TestLinkedSpanToStringRendersEntries()
    {
        var span = new LinkedSpan<TestLink>([new TestLink("p", "i", "n", "t")], 0, 4)
        {
            SentenceId = 1,
            SearchTerm = "term"
        };

        string text = span.ToString();

        StringAssert.Contains("sentenceid=1", text);
        StringAssert.Contains("searchTerm=term", text);
        StringAssert.Contains("itemName=n", text);
        StringAssert.DoesNotContain("System.Collections.Generic.List", text);
    }

    [Test]
    public void TestEntityLinkerPropertiesReadsValues()
    {
        using var input = AsStream("linker=Some.Linker.Type\nlinker.person=Other.Type\n");

        var properties = new EntityLinkerProperties(input);

        ClassicAssert.AreEqual("Some.Linker.Type", properties.GetProperty("linker", ""));
        ClassicAssert.AreEqual("Other.Type", properties.GetProperty("linker.person", ""));
        ClassicAssert.AreEqual("fallback", properties.GetProperty("absent", "fallback"));
    }

    /// <summary>
    /// The constructor documents that the stream is not closed, matching Java, where
    /// Properties.load leaves it open.
    /// </summary>
    [Test]
    public void TestEntityLinkerPropertiesLeavesStreamOpen()
    {
        using var input = AsStream("linker=Some.Linker.Type\n");

        _ = new EntityLinkerProperties(input);

        ClassicAssert.IsTrue(input.CanRead, "EntityLinkerProperties must not close the caller's stream.");
    }

    [Test]
    public void TestGetLinkerThrowsWhenPropertyMissing()
    {
        using var input = AsStream("unrelated=value\n");
        var properties = new EntityLinkerProperties(input);

        ClassicAssert.Throws<ArgumentException>((Action)(() => EntityLinkerFactory.GetLinker(properties)));
        ClassicAssert.Throws<ArgumentException>((Action)(() => EntityLinkerFactory.GetLinker("person", properties)));
    }

    [Test]
    public void TestGetLinkerThrowsOnNullProperties()
    {
        ClassicAssert.Throws<ArgumentNullException>((Action)(() => EntityLinkerFactory.GetLinker(null!)));
    }

    [Test]
    public void TestGetLinkerThrowsWhenImplementationCannotBeResolved()
    {
        using var input = AsStream("linker=Nonexistent.Linker.Type\n");
        var properties = new EntityLinkerProperties(input);

        // ExtensionLoader reports an unresolvable or non-conforming implementation this way,
        // matching Java, where the loader throws rather than yielding a linker.
        ClassicAssert.Throws<ExtensionNotLoadedException>(
            (Action)(() => EntityLinkerFactory.GetLinker(properties)));
    }
}
