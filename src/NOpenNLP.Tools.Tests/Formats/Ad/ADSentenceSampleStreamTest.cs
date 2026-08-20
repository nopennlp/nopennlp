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
using System.Text;
using NOpenNLP.Tools.Formats;
using NOpenNLP.Tools.Sentdetect;
using NOpenNLP.Tools.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Formats.Ad;

public class ADSentenceSampleStreamTest
{
    private readonly List<SentenceSample> samples = [];

    [Test]
    public void TestSimpleCount()
    {
        ClassicAssert.AreEqual(5, samples.Count);
    }

    [Test]
    public void TestSentences()
    {
        ClassicAssert.NotNull(samples[0].Document);
        ClassicAssert.AreEqual(3, samples[0].GetSentences().Length);
        ClassicAssert.AreEqual(new Span(0, 119), samples[0].GetSentences()[0]);
        ClassicAssert.AreEqual(new Span(120, 180), samples[0].GetSentences()[1]);
    }

    [SetUp]
    public void Setup()
    {
        // NOpenNLP: JUnit constructs a new test instance per test, so upstream's field
        // starts empty each time. NUnit reuses one fixture instance, so clear it here.
        samples.Clear();

        IInputStreamFactory @in = new ResourceAsStreamFactory("/opennlp/tools/formats/ad.sample");

        using (ADSentenceSampleStream stream = new ADSentenceSampleStream(
            new PlainTextByLineStream(@in, Encoding.UTF8), true))
        {
            SentenceSample? sample;

            while ((sample = stream.Read()) != null)
            {
                Console.WriteLine(sample.Document);
                Console.WriteLine("<fim>");
                samples.Add(sample);
            }
        }
    }
}
