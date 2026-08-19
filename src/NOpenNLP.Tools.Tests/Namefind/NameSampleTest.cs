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
using NOpenNLP.Tools.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Namefind;

/// <summary>
/// This is the test class for <see cref="NameSample"/>.
/// </summary>
public class NameSampleTest
{
    /// <summary>
    /// Create a NameSample from scratch and validate it.
    /// </summary>
    /// <param name="useTypes">if to use nametypes</param>
    /// <returns>the NameSample</returns>
    private static NameSample CreateSimpleNameSample(bool useTypes)
    {
        string[] sentence = ["U", ".", "S", ".", "President", "Barack", "Obama", "is",
            "considering", "sending", "additional", "American", "forces",
            "to", "Afghanistan", "."];

        Span[] names = [new Span(0, 4, "Location"), new Span(5, 7, "Person"),
            new Span(14, 15, "Location")];

        NameSample nameSample;
        if (useTypes)
        {
            nameSample = new NameSample(sentence, names, false);
        }
        else
        {
            Span[] namesWithoutType = new Span[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                namesWithoutType[i] = new Span(names[i].Start, names[i].End);
            }

            nameSample = new NameSample(sentence, namesWithoutType, false);
        }

        return nameSample;
    }

    // NOpenNLP: upstream's testNameSampleSerDe round-trips the sample through Java
    // object serialization, which the port deliberately does not implement (see the
    // note on NameSample). There is no .NET counterpart to assert, so the test is
    // omitted rather than rewritten into something upstream does not check.

    /// <summary>
    /// Test serialization of sequential spans.
    /// </summary>
    [Test]
    public void TestSequentialSpans()
    {
        string[] sentence = ["A", "Place", "a", "time", "A", "Person", "."];

        Span[] names = [new Span(0, 2, "Place"), new Span(2, 4, "Time"),
            new Span(4, 6, "Person")];

        NameSample nameSample = new(sentence, names, false);

        ClassicAssert.AreEqual(
            "<START:Place> A Place <END> <START:Time> a time <END> <START:Person> A Person <END> .",
            nameSample.ToString());
    }

    /// <summary>
    /// Test serialization of unsorted sequential spans.
    /// </summary>
    [Test]
    public void TestUnsortedSequentialSpans()
    {
        string[] sentence = ["A", "Place", "a", "time", "A", "Person", "."];

        Span[] names = [new Span(0, 2, "Place"), new Span(4, 6, "Person"),
            new Span(2, 4, "Time")];

        NameSample nameSample = new(sentence, names, false);

        ClassicAssert.AreEqual(
            "<START:Place> A Place <END> <START:Time> a time <END> <START:Person> A Person <END> .",
            nameSample.ToString());
    }

    /// <summary>
    /// Test if it fails to name spans are overlapping
    /// </summary>
    [Test]
    public void TestOverlappingNameSpans()
    {
        string[] sentence = ["A", "Place", "a", "time", "A", "Person", "."];

        Span[] names = [new Span(0, 2, "Place"), new Span(3, 5, "Person"),
            new Span(2, 4, "Time")];

        // NOpenNLP: upstream expects RuntimeException; the port throws
        // InvalidOperationException, the closest .NET counterpart.
        Assert.Throws<InvalidOperationException>((Action)(() => new NameSample(sentence, names, false)));
    }

    /// <summary>
    /// Checks if could create a NameSample without NameTypes, generate the
    /// string representation and validate it.
    /// </summary>
    [Test]
    public void TestNoTypesToString()
    {
        string nameSampleStr = CreateSimpleNameSample(false).ToString();

        ClassicAssert.AreEqual("<START> U . S . <END> President <START> Barack Obama <END>" +
            " is considering " +
            "sending additional American forces to <START> Afghanistan <END> .", nameSampleStr);
    }

    /// <summary>
    /// Checks if could create a NameSample with NameTypes, generate the
    /// string representation and validate it.
    /// </summary>
    [Test]
    public void TestWithTypesToString()
    {
        string nameSampleStr = CreateSimpleNameSample(true).ToString();
        ClassicAssert.AreEqual("<START:Location> U . S . <END> President <START:Person>" +
                " Barack Obama <END> " +
            "is considering sending additional American forces to <START:Location> Afghanistan <END> .",
            nameSampleStr);

        NameSample parsedSample = NameSample.Parse("<START:Location> U . S . <END> " +
            "President <START:Person> Barack Obama <END> is considering sending " +
            "additional American forces to <START:Location> Afghanistan <END> .",
            false);

        ClassicAssert.AreEqual(CreateSimpleNameSample(true), parsedSample);
    }

    /// <summary>
    /// Checks that if the name is the last token in a sentence it is still outputed
    /// correctly.
    /// </summary>
    [Test]
    public void TestNameAtEnd()
    {
        string[] sentence =
        [
            "My",
            "name",
            "is",
            "Anna"
        ];

        NameSample sample = new(sentence, [new Span(3, 4)], false);

        ClassicAssert.AreEqual("My name is <START> Anna <END>", sample.ToString());
    }

    /// <summary>
    /// Tests if an additional space is correctly treated as one space.
    /// </summary>
    [Test]
    public void TestParseWithAdditionalSpace()
    {
        string line = "<START> M . K . <END> <START> Schwitters <END> ?  <START> Heartfield <END> ?";

        NameSample test = NameSample.Parse(line, false);

        ClassicAssert.AreEqual(8, test.Sentence.Length);
    }

    /// <summary>
    /// Checks if it accepts name type with some special characters
    /// </summary>
    [Test]
    public void TestTypeWithSpecialChars()
    {
        NameSample parsedSample = NameSample
            .Parse(
                "<START:type-1> U . S . <END> "
                    + "President <START:type_2> Barack Obama <END> is considering sending "
                    + "additional American forces to <START:type_3-/;.,&%$> Afghanistan <END> .",
                false);

        ClassicAssert.AreEqual(3, parsedSample.Names.Length);
        ClassicAssert.AreEqual("type-1", parsedSample.Names[0].Type);
        ClassicAssert.AreEqual("type_2", parsedSample.Names[1].Type);
        ClassicAssert.AreEqual("type_3-/;.,&%$", parsedSample.Names[2].Type);
    }

    /// <summary>
    /// Test if it fails to parse empty type
    /// </summary>
    [Test]
    public void TestMissingType() =>
        Assert.Throws<IOException>((Action)(() => NameSample.Parse("<START:> token <END>", false)));

    /// <summary>
    /// Test if it fails to parse type with space
    /// </summary>
    [Test]
    public void TestTypeWithSpace() =>
        Assert.Throws<IOException>((Action)(() => NameSample.Parse("<START:abc a> token <END>", false)));

    /// <summary>
    /// Test if it fails to parse type with new line
    /// </summary>
    [Test]
    public void TestTypeWithNewLine() =>
        Assert.Throws<IOException>((Action)(() => NameSample.Parse("<START:abc\na> token <END>", false)));

    /// <summary>
    /// Test if it fails to parse type with :
    /// </summary>
    [Test]
    public void TestTypeWithInvalidChar1() =>
        Assert.Throws<IOException>((Action)(() => NameSample.Parse("<START:abc:a> token <END>", false)));

    /// <summary>
    /// Test if it fails to parse type with &gt;
    /// </summary>
    [Test]
    public void TestTypeWithInvalidChar2() =>
        Assert.Throws<IOException>((Action)(() => NameSample.Parse("<START:abc>a> token <END>", false)));

    /// <summary>
    /// Test if it fails to parse nested names
    /// </summary>
    [Test]
    public void TestNestedNameSpans() =>
        Assert.Throws<IOException>((Action)(() =>
            NameSample.Parse("<START:Person> <START:Location> Kennedy <END> City <END>", false)));

    [Test]
    public void TestEquals()
    {
        ClassicAssert.IsFalse(ReferenceEquals(CreateGoldSample(), CreateGoldSample()));
        ClassicAssert.IsTrue(CreateGoldSample().Equals(CreateGoldSample()));
        ClassicAssert.IsFalse(CreateGoldSample().Equals(CreatePredSample()));
        ClassicAssert.IsFalse(CreatePredSample().Equals(new object()));
    }

    public static NameSample CreateGoldSample() => CreateSimpleNameSample(true);

    public static NameSample CreatePredSample() => CreateSimpleNameSample(false);
}
