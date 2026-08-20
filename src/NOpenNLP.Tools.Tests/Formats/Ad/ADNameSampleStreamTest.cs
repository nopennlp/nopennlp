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
using System.Text;
using NOpenNLP.Tools.Formats;
using NOpenNLP.Tools.Namefind;
using NOpenNLP.Tools.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Formats.Ad;

public class ADNameSampleStreamTest
{
    private readonly List<NameSample> samples = [];

    [Test]
    public void TestSimpleCount()
    {
        ClassicAssert.AreEqual(ADParagraphStreamTest.NumSentences, samples.Count);
    }

    [Test]
    public void TestCheckMergedContractions()
    {
        ClassicAssert.AreEqual("no", samples[0].Sentence[1]);
        ClassicAssert.AreEqual("no", samples[0].Sentence[11]);
        ClassicAssert.AreEqual("Com", samples[1].Sentence[0]);
        ClassicAssert.AreEqual("relação", samples[1].Sentence[1]);
        ClassicAssert.AreEqual("à", samples[1].Sentence[2]);
        ClassicAssert.AreEqual("mais", samples[2].Sentence[4]);
        ClassicAssert.AreEqual("de", samples[2].Sentence[5]);
        ClassicAssert.AreEqual("da", samples[2].Sentence[8]);
        ClassicAssert.AreEqual("num", samples[3].Sentence[26]);
    }

    [Test]
    public void TestSize()
    {
        ClassicAssert.AreEqual(25, samples[0].Sentence.Length);
        ClassicAssert.AreEqual(12, samples[1].Sentence.Length);
        ClassicAssert.AreEqual(59, samples[2].Sentence.Length);
        ClassicAssert.AreEqual(33, samples[3].Sentence.Length);
    }

    [Test]
    public void TestNames()
    {
        ClassicAssert.AreEqual(new Span(4, 7, "time"), samples[0].Names[0]);
        ClassicAssert.AreEqual(new Span(8, 10, "place"), samples[0].Names[1]);
        ClassicAssert.AreEqual(new Span(12, 14, "place"), samples[0].Names[2]);
        ClassicAssert.AreEqual(new Span(15, 17, "person"), samples[0].Names[3]);
        ClassicAssert.AreEqual(new Span(18, 19, "numeric"), samples[0].Names[4]);
        ClassicAssert.AreEqual(new Span(20, 22, "place"), samples[0].Names[5]);
        ClassicAssert.AreEqual(new Span(23, 24, "place"), samples[0].Names[6]);

        ClassicAssert.AreEqual(new Span(22, 24, "person"), samples[2].Names[0]);//    22..24
        ClassicAssert.AreEqual(new Span(25, 27, "person"), samples[2].Names[1]);//    25..27
        ClassicAssert.AreEqual(new Span(28, 30, "person"), samples[2].Names[2]);//    28..30
        ClassicAssert.AreEqual(new Span(31, 34, "person"), samples[2].Names[3]);//    31..34
        ClassicAssert.AreEqual(new Span(35, 37, "person"), samples[2].Names[4]);//    35..37
        ClassicAssert.AreEqual(new Span(38, 40, "person"), samples[2].Names[5]);//    38..40
        ClassicAssert.AreEqual(new Span(41, 43, "person"), samples[2].Names[6]);//    41..43
        ClassicAssert.AreEqual(new Span(44, 46, "person"), samples[2].Names[7]);//    44..46
        ClassicAssert.AreEqual(new Span(47, 49, "person"), samples[2].Names[8]);//    47..49
        ClassicAssert.AreEqual(new Span(50, 52, "person"), samples[2].Names[9]);//    50..52
        ClassicAssert.AreEqual(new Span(53, 55, "person"), samples[2].Names[10]);//    53..55

        ClassicAssert.AreEqual(new Span(0, 1, "place"), samples[3].Names[0]);//    0..1
        ClassicAssert.AreEqual(new Span(6, 7, "event"), samples[3].Names[1]);//    6..7
        ClassicAssert.AreEqual(new Span(15, 16, "organization"), samples[3].Names[2]);//    15..16
        ClassicAssert.AreEqual(new Span(18, 19, "event"), samples[3].Names[3]);//    18..19
        ClassicAssert.AreEqual(new Span(27, 28, "event"), samples[3].Names[4]);//    27..28
        ClassicAssert.AreEqual(new Span(29, 30, "event"), samples[3].Names[5]);//    29..30

        ClassicAssert.AreEqual(new Span(1, 6, "time"), samples[4].Names[0]);//    0..1
        ClassicAssert.AreEqual(new Span(0, 3, "person"), samples[5].Names[0]);//    0..1
    }

    [Test]
    public void TestSmallSentence()
    {
        ClassicAssert.AreEqual(2, samples[6].Sentence.Length);
    }

    [Test]
    public void TestMissingRightContraction()
    {
        ClassicAssert.AreEqual(new Span(0, 1, "person"), samples[7].Names[0]);
        ClassicAssert.AreEqual(new Span(3, 4, "person"), samples[7].Names[1]);
        ClassicAssert.AreEqual(new Span(5, 6, "person"), samples[7].Names[2]);
    }

    [SetUp]
    public void Setup()
    {
        // NOpenNLP: JUnit constructs a new test instance per test, so upstream's field
        // starts empty each time. NUnit reuses one fixture instance, so clear it here.
        samples.Clear();

        IInputStreamFactory @in = new ResourceAsStreamFactory("/opennlp/tools/formats/ad.sample");

        using (ADNameSampleStream stream = new ADNameSampleStream(
            new PlainTextByLineStream(@in, Encoding.UTF8), true))
        {
            NameSample? sample;
            while ((sample = stream.Read()) != null)
            {
                samples.Add(sample);
            }
        }
    }
}
