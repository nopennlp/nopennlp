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
using System.IO;
using NOpenNLP.Tools.Support;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Ml.Model;

/// <summary>
/// Verifies that crafted model files with oversized count fields are rejected before array
/// allocation occurs, preventing OOM DoS. See OPENNLP-1821.
/// </summary>
public class AbstractModelReaderOomTest
{
    /// <summary>
    /// Minimal concrete subclass that exposes the three protected members under test.
    /// </summary>
    private sealed class TestableReader(IDataReader dr) : AbstractModelReader(dr)
    {
        public override void CheckModelType()
        {
        }

        public override AbstractModel ConstructModel() => null!;

        public string[] ReadOutcomes() => Outcomes;

        public int[][] ReadOutcomePatterns() => OutcomePatterns;

        public string[] ReadPredicates() => Predicates;
    }

    /// <summary>Reader whose stream starts with a single int (the count field).</summary>
    private static TestableReader ReaderFor(int countValue)
    {
        var buffer = new MemoryStream();
        buffer.WriteJavaInt32(countValue);
        buffer.Position = 0;
        return new TestableReader(new BinaryFileDataReader(buffer));
    }

    // NOpenNLP: upstream throws IllegalArgumentException; the .NET counterpart is
    // ArgumentException. This applies to every rejection test below.

    [Test]
    public void TestGetOutcomes_RejectsMaxValue()
    {
        var reader = ReaderFor(int.MaxValue);
        Assert.Throws<ArgumentException>((Action)(() => reader.ReadOutcomes()));
    }

    [Test]
    public void TestGetOutcomePatterns_RejectsMaxValue()
    {
        var reader = ReaderFor(int.MaxValue);
        Assert.Throws<ArgumentException>((Action)(() => reader.ReadOutcomePatterns()));
    }

    [Test]
    public void TestGetPredicates_RejectsMaxValue()
    {
        var reader = ReaderFor(int.MaxValue);
        Assert.Throws<ArgumentException>((Action)(() => reader.ReadPredicates()));
    }

    [Test]
    public void TestGetOutcomes_RejectsNegativeCount()
    {
        var reader = ReaderFor(-1);
        Assert.Throws<ArgumentException>((Action)(() => reader.ReadOutcomes()));
    }

    [Test]
    public void TestGetOutcomePatterns_RejectsNegativeCount()
    {
        var reader = ReaderFor(-1);
        Assert.Throws<ArgumentException>((Action)(() => reader.ReadOutcomePatterns()));
    }

    [Test]
    public void TestGetPredicates_RejectsNegativeCount()
    {
        var reader = ReaderFor(-1);
        Assert.Throws<ArgumentException>((Action)(() => reader.ReadPredicates()));
    }

    [Test]
    public void TestGetOutcomes_ValidCountReturnsLabels()
    {
        var buffer = new MemoryStream();
        buffer.WriteJavaInt32(2);
        buffer.WriteJavaUTF("label-A");
        buffer.WriteJavaUTF("label-B");
        buffer.Position = 0;

        var reader = new TestableReader(new BinaryFileDataReader(buffer));
        CollectionAssert.AreEqual(new[] { "label-A", "label-B" }, reader.ReadOutcomes());
    }

    [Test]
    public void TestGetPredicates_ValidCountReturnsLabels()
    {
        var buffer = new MemoryStream();
        buffer.WriteJavaInt32(3);
        buffer.WriteJavaUTF("pred-X");
        buffer.WriteJavaUTF("pred-Y");
        buffer.WriteJavaUTF("pred-Z");
        buffer.Position = 0;

        var reader = new TestableReader(new BinaryFileDataReader(buffer));
        CollectionAssert.AreEqual(new[] { "pred-X", "pred-Y", "pred-Z" }, reader.ReadPredicates());
    }

    /// <summary>
    /// The limit itself is inclusive: a count equal to <c>MAX_ENTRIES</c> passes the bound and
    /// fails later, on the truncated stream, rather than being rejected outright. The check
    /// must not turn a legitimate large model into an error one entry before the documented
    /// ceiling.
    /// </summary>
    [Test]
    [NOpenNLPSpecific]
    public void TestGetOutcomes_LimitItselfIsNotRejected()
    {
        var reader = ReaderFor(AbstractModelReader.MAX_ENTRIES);
        Assert.Throws<EndOfStreamException>((Action)(() => reader.ReadOutcomes()));
    }

    /// <summary>
    /// One past the limit is rejected, and the message names the count and the ceiling so a
    /// caller can tell a hostile model from one that is merely larger than the default.
    /// </summary>
    [Test]
    [NOpenNLPSpecific]
    public void TestGetOutcomes_JustAboveLimitIsRejected()
    {
        var reader = ReaderFor(AbstractModelReader.MAX_ENTRIES + 1);
        var ex = Assert.Throws<ArgumentException>((Action)(() => reader.ReadOutcomes()));

        ClassicAssert.IsTrue(ex!.Message.Contains(AbstractModelReader.MAX_ENTRIES.ToString()),
            $"message should name the limit; got: {ex.Message}");
    }
}
