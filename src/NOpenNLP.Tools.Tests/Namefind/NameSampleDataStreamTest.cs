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
using System.IO;
using System.Text;
using NOpenNLP.Tools.Support;
using NOpenNLP.Tools.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Namefind;

/// <summary>
/// This is the test class for <see cref="NameSampleDataStream"/>.
/// </summary>
public class NameSampleDataStreamTest
{
    private const string person = "person";
    private const string date = "date";
    private const string location = "location";
    private const string organization = "organization";

    /// <summary>
    /// Create a string from a array section.
    /// </summary>
    /// <param name="tokens">the tokens</param>
    /// <param name="nameSpan">the section</param>
    /// <returns>the string</returns>
    private static string SublistToString(string[] tokens, Span nameSpan)
    {
        var sb = new StringBuilder();
        for (int i = nameSpan.Start; i < nameSpan.End; i++)
        {
            sb.Append(tokens[i]).Append(' ');
        }

        return sb.ToString().Trim();
    }

    /// <summary>
    /// Create a NameSampleDataStream from a corpus with entities annotated but
    /// without nameType and validate it.
    /// </summary>
    [Test]
    public void TestWithoutNameTypes()
    {
        // NOpenNLP: upstream uses opennlp.tools.formats.ResourceAsStreamFactory,
        // which lives in the not-yet-ported formats package; the test-side
        // ResourceAsStreamFactory in Support does the same job over an embedded
        // resource.
        IInputStreamFactory @in = new ResourceAsStreamFactory(
            "/opennlp/tools/namefind/AnnotatedSentences.txt");

        NameSampleDataStream ds = new(
            new PlainTextByLineStream(@in, Latin1));

        NameSample? ns = ds.Read();

        string[] expectedNames = ["Alan McKennedy", "Julie", "Marie Clara",
            "Stefanie Schmidt", "Mike", "Stefanie Schmidt", "George", "Luise",
            "George Bauer", "Alisa Fernandes", "Alisa", "Mike Sander",
            "Stefan Miller", "Stefan Miller", "Stefan Miller", "Elenor Meier",
            "Gina Schneider", "Bruno Schulz", "Michel Seile", "George Miller",
            "Miller", "Peter Schubert", "Natalie"];

        List<string> names = [];
        List<Span> spans = [];

        while (ns != null)
        {
            foreach (var nameSpan in ns.Names)
            {
                names.Add(SublistToString(ns.Sentence, nameSpan));
                spans.Add(nameSpan);
            }

            ns = ds.Read();
        }

        ds.Dispose();

        ClassicAssert.AreEqual(expectedNames.Length, names.Count);
        ClassicAssert.AreEqual(CreateDefaultSpan(6, 8), spans[0]);
        ClassicAssert.AreEqual(CreateDefaultSpan(3, 4), spans[1]);
        ClassicAssert.AreEqual(CreateDefaultSpan(1, 3), spans[2]);
        ClassicAssert.AreEqual(CreateDefaultSpan(4, 6), spans[3]);
        ClassicAssert.AreEqual(CreateDefaultSpan(1, 2), spans[4]);
        ClassicAssert.AreEqual(CreateDefaultSpan(4, 6), spans[5]);
        ClassicAssert.AreEqual(CreateDefaultSpan(2, 3), spans[6]);
        ClassicAssert.AreEqual(CreateDefaultSpan(16, 17), spans[7]);
        ClassicAssert.AreEqual(CreateDefaultSpan(18, 20), spans[8]);
        ClassicAssert.AreEqual(CreateDefaultSpan(0, 2), spans[9]);
        ClassicAssert.AreEqual(CreateDefaultSpan(0, 1), spans[10]);
        ClassicAssert.AreEqual(CreateDefaultSpan(3, 5), spans[11]);
        ClassicAssert.AreEqual(CreateDefaultSpan(3, 5), spans[12]);
        ClassicAssert.AreEqual(CreateDefaultSpan(10, 12), spans[13]);
        ClassicAssert.AreEqual(CreateDefaultSpan(1, 3), spans[14]);
        ClassicAssert.AreEqual(CreateDefaultSpan(6, 8), spans[15]);
        ClassicAssert.AreEqual(CreateDefaultSpan(6, 8), spans[16]);
        ClassicAssert.AreEqual(CreateDefaultSpan(8, 10), spans[17]);
        ClassicAssert.AreEqual(CreateDefaultSpan(12, 14), spans[18]);
        ClassicAssert.AreEqual(CreateDefaultSpan(1, 3), spans[19]);
        ClassicAssert.AreEqual(CreateDefaultSpan(0, 1), spans[20]);
        ClassicAssert.AreEqual(CreateDefaultSpan(2, 4), spans[21]);
        ClassicAssert.AreEqual(CreateDefaultSpan(5, 6), spans[22]);
    }

    private static Span CreateDefaultSpan(int s, int e) => new(s, e, NameSample.DEFAULT_TYPE);

    /// <summary>
    /// Checks that invalid spans cause an <see cref="IOException"/> to be thrown.
    /// </summary>
    [Test]
    public void TestWithoutNameTypeAndInvalidData()
    {
        using (NameSampleDataStream sampleStream = new(
            ObjectStreamUtils.CreateObjectStream("<START> <START> Name <END>")))
        {
            Assert.Throws<IOException>((Action)(() => sampleStream.Read()));
        }

        using (NameSampleDataStream sampleStream = new(
            ObjectStreamUtils.CreateObjectStream("<START> Name <END> <END>")))
        {
            Assert.Throws<IOException>((Action)(() => sampleStream.Read()));
        }

        using (NameSampleDataStream sampleStream = new(
            ObjectStreamUtils.CreateObjectStream(
                "<START> <START> Person <END> Street <END>")))
        {
            Assert.Throws<IOException>((Action)(() => sampleStream.Read()));
        }
    }

    /// <summary>
    /// Create a NameSampleDataStream from a corpus with entities annotated
    /// with multiple nameTypes, like person, date, location and organization, and validate it.
    /// </summary>
    [Test]
    public void TestWithNameTypes()
    {
        IInputStreamFactory @in = new ResourceAsStreamFactory(
            "/opennlp/tools/namefind/voa1.train");

        NameSampleDataStream ds = new(
            new PlainTextByLineStream(@in, Encoding.UTF8));

        Dictionary<string, List<string>> names = [];
        Dictionary<string, List<Span>> spans = [];

        NameSample? ns;
        while ((ns = ds.Read()) != null)
        {
            Span[] nameSpans = ns.Names;

            foreach (var nameSpan in nameSpans)
            {
                string type = nameSpan.Type!;
                if (!names.ContainsKey(type))
                {
                    names[type] = [];
                    spans[type] = [];
                }

                names[type].Add(SublistToString(ns.Sentence, nameSpan));
                spans[type].Add(nameSpan);
            }
        }

        ds.Dispose();

        string[] expectedPerson = ["Barack Obama", "Obama", "Obama",
            "Lee Myung - bak", "Obama", "Obama", "Scott Snyder", "Snyder", "Obama",
            "Obama", "Obama", "Tim Peters", "Obama", "Peters"];

        string[] expectedDate = ["Wednesday", "Thursday", "Wednesday"];

        string[] expectedLocation = ["U . S .", "South Korea", "North Korea",
            "China", "South Korea", "North Korea", "North Korea", "U . S .",
            "South Korea", "United States", "Pyongyang", "North Korea",
            "South Korea", "Afghanistan", "Seoul", "U . S .", "China"];

        string[] expectedOrganization = ["Center for U . S . Korea Policy"];

        ClassicAssert.AreEqual(expectedPerson.Length, names[person].Count);
        ClassicAssert.AreEqual(expectedDate.Length, names[date].Count);
        ClassicAssert.AreEqual(expectedLocation.Length, names[location].Count);
        ClassicAssert.AreEqual(expectedOrganization.Length, names[organization].Count);

        ClassicAssert.AreEqual(new Span(5, 7, person), spans[person][0]);
        ClassicAssert.AreEqual(expectedPerson[0], names[person][0]);
        ClassicAssert.AreEqual(new Span(10, 11, person), spans[person][1]);
        ClassicAssert.AreEqual(expectedPerson[1], names[person][1]);
        ClassicAssert.AreEqual(new Span(29, 30, person), spans[person][2]);
        ClassicAssert.AreEqual(expectedPerson[2], names[person][2]);
        ClassicAssert.AreEqual(new Span(23, 27, person), spans[person][3]);
        ClassicAssert.AreEqual(expectedPerson[3], names[person][3]);
        ClassicAssert.AreEqual(new Span(1, 2, person), spans[person][4]);
        ClassicAssert.AreEqual(expectedPerson[4], names[person][4]);
        ClassicAssert.AreEqual(new Span(8, 9, person), spans[person][5]);
        ClassicAssert.AreEqual(expectedPerson[5], names[person][5]);
        ClassicAssert.AreEqual(new Span(0, 2, person), spans[person][6]);
        ClassicAssert.AreEqual(expectedPerson[6], names[person][6]);
        ClassicAssert.AreEqual(new Span(25, 26, person), spans[person][7]);
        ClassicAssert.AreEqual(expectedPerson[7], names[person][7]);
        ClassicAssert.AreEqual(new Span(1, 2, person), spans[person][8]);
        ClassicAssert.AreEqual(expectedPerson[8], names[person][8]);
        ClassicAssert.AreEqual(new Span(6, 7, person), spans[person][9]);
        ClassicAssert.AreEqual(expectedPerson[9], names[person][9]);
        ClassicAssert.AreEqual(new Span(14, 15, person), spans[person][10]);
        ClassicAssert.AreEqual(expectedPerson[10], names[person][10]);
        ClassicAssert.AreEqual(new Span(0, 2, person), spans[person][11]);
        ClassicAssert.AreEqual(expectedPerson[11], names[person][11]);
        ClassicAssert.AreEqual(new Span(12, 13, person), spans[person][12]);
        ClassicAssert.AreEqual(expectedPerson[12], names[person][12]);
        ClassicAssert.AreEqual(new Span(12, 13, person), spans[person][13]);
        ClassicAssert.AreEqual(expectedPerson[13], names[person][13]);

        ClassicAssert.AreEqual(new Span(7, 8, date), spans[date][0]);
        ClassicAssert.AreEqual(expectedDate[0], names[date][0]);
        ClassicAssert.AreEqual(new Span(27, 28, date), spans[date][1]);
        ClassicAssert.AreEqual(expectedDate[1], names[date][1]);
        ClassicAssert.AreEqual(new Span(15, 16, date), spans[date][2]);
        ClassicAssert.AreEqual(expectedDate[2], names[date][2]);

        ClassicAssert.AreEqual(new Span(0, 4, location), spans[location][0]);
        ClassicAssert.AreEqual(expectedLocation[0], names[location][0]);
        ClassicAssert.AreEqual(new Span(10, 12, location), spans[location][1]);
        ClassicAssert.AreEqual(expectedLocation[1], names[location][1]);
        ClassicAssert.AreEqual(new Span(28, 30, location), spans[location][2]);
        ClassicAssert.AreEqual(expectedLocation[2], names[location][2]);
        ClassicAssert.AreEqual(new Span(3, 4, location), spans[location][3]);
        ClassicAssert.AreEqual(expectedLocation[3], names[location][3]);
        ClassicAssert.AreEqual(new Span(5, 7, location), spans[location][4]);
        ClassicAssert.AreEqual(expectedLocation[4], names[location][4]);
        ClassicAssert.AreEqual(new Span(16, 18, location), spans[location][5]);
        ClassicAssert.AreEqual(expectedLocation[5], names[location][5]);
        ClassicAssert.AreEqual(new Span(1, 3, location), spans[location][6]);
        ClassicAssert.AreEqual(expectedLocation[6], names[location][6]);
        ClassicAssert.AreEqual(new Span(5, 9, location), spans[location][7]);
        ClassicAssert.AreEqual(expectedLocation[7], names[location][7]);
        ClassicAssert.AreEqual(new Span(0, 2, location), spans[location][8]);
        ClassicAssert.AreEqual(expectedLocation[8], names[location][8]);
        ClassicAssert.AreEqual(new Span(4, 6, location), spans[location][9]);
        ClassicAssert.AreEqual(expectedLocation[9], names[location][9]);
        ClassicAssert.AreEqual(new Span(10, 11, location), spans[location][10]);
        ClassicAssert.AreEqual(expectedLocation[10], names[location][10]);
        ClassicAssert.AreEqual(new Span(6, 8, location), spans[location][11]);
        ClassicAssert.AreEqual(expectedLocation[11], names[location][11]);
        ClassicAssert.AreEqual(new Span(4, 6, location), spans[location][12]);
        ClassicAssert.AreEqual(expectedLocation[12], names[location][12]);
        ClassicAssert.AreEqual(new Span(10, 11, location), spans[location][13]);
        ClassicAssert.AreEqual(expectedLocation[13], names[location][13]);
        ClassicAssert.AreEqual(new Span(12, 13, location), spans[location][14]);
        ClassicAssert.AreEqual(expectedLocation[14], names[location][14]);
        ClassicAssert.AreEqual(new Span(5, 9, location), spans[location][15]);
        ClassicAssert.AreEqual(expectedLocation[15], names[location][15]);
        ClassicAssert.AreEqual(new Span(11, 12, location), spans[location][16]);
        ClassicAssert.AreEqual(expectedLocation[16], names[location][16]);

        ClassicAssert.AreEqual(new Span(7, 15, organization), spans[organization][0]);
        ClassicAssert.AreEqual(expectedOrganization[0], names[organization][0]);
    }

    [Test]
    public void TestWithNameTypeAndInvalidData()
    {
        using (NameSampleDataStream sampleStream = new(
            ObjectStreamUtils.CreateObjectStream("<START:> Name <END>")))
        {
            Assert.Throws<IOException>((Action)(() => sampleStream.Read()));
        }

        using (NameSampleDataStream sampleStream = new(
            ObjectStreamUtils.CreateObjectStream(
                "<START:street> <START:person> Name <END> <END>")))
        {
            Assert.Throws<IOException>((Action)(() => sampleStream.Read()));
        }
    }

    [Test]
    public void TestClearAdaptiveData()
    {
        string trainingData = "a\n" +
            "b\n" +
            "c\n" +
            "\n" +
            "d\n";

        IObjectStream<string?> untokenizedLineStream = new PlainTextByLineStream(
            new MockInputStreamFactory(trainingData), Encoding.UTF8);

        IObjectStream<NameSample?> trainingStream = new NameSampleDataStream(untokenizedLineStream);

        ClassicAssert.IsFalse(trainingStream.Read()!.IsClearAdaptiveDataSet);
        ClassicAssert.IsFalse(trainingStream.Read()!.IsClearAdaptiveDataSet);
        ClassicAssert.IsFalse(trainingStream.Read()!.IsClearAdaptiveDataSet);
        ClassicAssert.IsTrue(trainingStream.Read()!.IsClearAdaptiveDataSet);
        ClassicAssert.IsNull(trainingStream.Read());

        trainingStream.Dispose();
    }

    [Test]
    public void TestHtmlNameSampleParsing()
    {
        IInputStreamFactory @in = new ResourceAsStreamFactory(
            "/opennlp/tools/namefind/html1.train");

        NameSampleDataStream ds = new(
            new PlainTextByLineStream(@in, Encoding.UTF8));

        NameSample? ns = ds.Read();

        ClassicAssert.AreEqual(1, ns!.Sentence.Length);
        ClassicAssert.AreEqual("<html>", ns.Sentence[0]);

        ns = ds.Read();
        ClassicAssert.AreEqual(1, ns!.Sentence.Length);
        ClassicAssert.AreEqual("<head/>", ns.Sentence[0]);

        ns = ds.Read();
        ClassicAssert.AreEqual(1, ns!.Sentence.Length);
        ClassicAssert.AreEqual("<body>", ns.Sentence[0]);

        ns = ds.Read();
        ClassicAssert.AreEqual(1, ns!.Sentence.Length);
        ClassicAssert.AreEqual("<ul>", ns.Sentence[0]);

        // <li> <START:organization> Advanced Integrated Pest Management <END> </li>
        ns = ds.Read();
        ClassicAssert.AreEqual(6, ns!.Sentence.Length);
        ClassicAssert.AreEqual("<li>", ns.Sentence[0]);
        ClassicAssert.AreEqual("Advanced", ns.Sentence[1]);
        ClassicAssert.AreEqual("Integrated", ns.Sentence[2]);
        ClassicAssert.AreEqual("Pest", ns.Sentence[3]);
        ClassicAssert.AreEqual("Management", ns.Sentence[4]);
        ClassicAssert.AreEqual("</li>", ns.Sentence[5]);
        ClassicAssert.AreEqual(new Span(1, 5, organization), ns.Names[0]);

        // <li> <START:organization> Bay Cities Produce Co., Inc. <END> </li>
        ns = ds.Read();
        ClassicAssert.AreEqual(7, ns!.Sentence.Length);
        ClassicAssert.AreEqual("<li>", ns.Sentence[0]);
        ClassicAssert.AreEqual("Bay", ns.Sentence[1]);
        ClassicAssert.AreEqual("Cities", ns.Sentence[2]);
        ClassicAssert.AreEqual("Produce", ns.Sentence[3]);
        ClassicAssert.AreEqual("Co.,", ns.Sentence[4]);
        ClassicAssert.AreEqual("Inc.", ns.Sentence[5]);
        ClassicAssert.AreEqual("</li>", ns.Sentence[6]);
        ClassicAssert.AreEqual(new Span(1, 6, organization), ns.Names[0]);

        ns = ds.Read();
        ClassicAssert.AreEqual(1, ns!.Sentence.Length);
        ClassicAssert.AreEqual("</ul>", ns.Sentence[0]);

        ns = ds.Read();
        ClassicAssert.AreEqual(1, ns!.Sentence.Length);
        ClassicAssert.AreEqual("</body>", ns.Sentence[0]);

        ns = ds.Read();
        ClassicAssert.AreEqual(1, ns!.Sentence.Length);
        ClassicAssert.AreEqual("</html>", ns.Sentence[0]);

        ClassicAssert.IsNull(ds.Read());

        ds.Dispose();
    }

    // NOpenNLP: StandardCharsets.ISO_8859_1 has no named BCL counterpart that is
    // registered on every target, so the code page is used directly.
    private static Encoding Latin1 => Encoding.GetEncoding(28591);
}
