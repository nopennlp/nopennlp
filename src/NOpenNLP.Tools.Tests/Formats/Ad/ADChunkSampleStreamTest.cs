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
using NOpenNLP.Tools.Chunker;
using NOpenNLP.Tools.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Formats.Ad;

public class ADChunkSampleStreamTest
{
    private readonly List<ChunkSample> samples = [];

    [Test]
    public void TestSimpleCount()
    {
        ClassicAssert.AreEqual(ADParagraphStreamTest.NumSentences, samples.Count);
    }

    [Test]
    public void TestChunks()
    {
        ClassicAssert.AreEqual("Inicia", samples[0].Sentence[0]);
        ClassicAssert.AreEqual("v-fin", samples[0].Tags[0]);
        ClassicAssert.AreEqual("B-VP", samples[0].Preds[0]);

        ClassicAssert.AreEqual("em", samples[0].Sentence[1]);
        ClassicAssert.AreEqual("prp", samples[0].Tags[1]);
        ClassicAssert.AreEqual("B-PP", samples[0].Preds[1]);

        ClassicAssert.AreEqual("o", samples[0].Sentence[2]);
        ClassicAssert.AreEqual("art", samples[0].Tags[2]);
        ClassicAssert.AreEqual("B-NP", samples[0].Preds[2]);

        ClassicAssert.AreEqual("próximo", samples[0].Sentence[3]);
        ClassicAssert.AreEqual("adj", samples[0].Tags[3]);
        ClassicAssert.AreEqual("I-NP", samples[0].Preds[3]);

        ClassicAssert.AreEqual("Casas", samples[3].Sentence[0]);
        ClassicAssert.AreEqual("n", samples[3].Tags[0]);
        ClassicAssert.AreEqual("B-NP", samples[3].Preds[0]);
    }

    [SetUp]
    public void Setup()
    {
        // NOpenNLP: JUnit constructs a new test instance per test, so upstream's field
        // starts empty each time. NUnit reuses one fixture instance, so clear it here.
        samples.Clear();

        IInputStreamFactory @in = new ResourceAsStreamFactory("/opennlp/tools/formats/ad.sample");

        using var stream = new ADChunkSampleStream(new PlainTextByLineStream(@in, Encoding.UTF8));
        while (stream.Read() is { } sample)
        {
            samples.Add(sample);
        }
    }
}
