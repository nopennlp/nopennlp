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
using System.Text.RegularExpressions;
using NOpenNLP.Tools.Formats;
using NOpenNLP.Tools.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Chunker;

public class ChunkSampleTest
{
    private static string[] CreateSentence() =>
    [
        "Forecasts",
        "for",
        "the",
        "trade",
        "figures",
        "range",
        "widely",
        ",",
        "Forecasts",
        "for",
        "the",
        "trade",
        "figures",
        "range",
        "widely",
        "."
    ];

    private static string[] CreateTags() =>
    [
        "NNS",
        "IN",
        "DT",
        "NN",
        "NNS",
        "VBP",
        "RB",
        ",",
        "NNS",
        "IN",
        "DT",
        "NN",
        "NNS",
        "VBP",
        "RB",
        "."
    ];

    private static string[] CreateChunks() =>
    [
        "B-NP",
        "B-PP",
        "B-NP",
        "I-NP",
        "I-NP",
        "B-VP",
        "B-ADVP",
        "O",
        "B-NP",
        "B-PP",
        "B-NP",
        "I-NP",
        "I-NP",
        "B-VP",
        "B-ADVP",
        "O"
    ];

    public static ChunkSample CreateGoldSample() => new(CreateSentence(), CreateTags(), CreateChunks());

    public static ChunkSample CreatePredSample()
    {
        string[] chunks = CreateChunks();
        chunks[5] = "B-NP";
        return new ChunkSample(CreateSentence(), CreateTags(), chunks);
    }

    // NOpenNLP: upstream's testChunkSampleSerDe round-trips the sample through
    // Java object serialization. ChunkSample does not implement a .NET
    // equivalent of java.io.Serializable (see the note on the ported class), so
    // there is nothing to exercise and the test is omitted.

    [Test]
    public void TestParameterValidation()
    {
        // NOpenNLP: upstream declares @Test(expected = IllegalArgumentException.class);
        // ArgumentException is the .NET counterpart.
        Assert.Throws<ArgumentException>((Action)(() =>
            new ChunkSample([""], [""], ["test", "one element to much"])));
    }

    [Test]
    public void TestRetrievingContent()
    {
        ChunkSample sample = new ChunkSample(CreateSentence(), CreateTags(), CreateChunks());

        CollectionAssert.AreEqual(CreateSentence(), sample.Sentence);
        CollectionAssert.AreEqual(CreateTags(), sample.Tags);
        CollectionAssert.AreEqual(CreateChunks(), sample.Preds);
    }

    [Test]
    public void TestToString()
    {
        ChunkSample sample = new ChunkSample(CreateSentence(), CreateTags(), CreateChunks());
        string[] sentence = CreateSentence();
        string[] tags = CreateTags();
        string[] chunks = CreateChunks();

        StringReader reader = new StringReader(sample.ToString());
        for (int i = 0; i < sentence.Length; i++)
        {
            string line = reader.ReadLine()!;
            string[] parts = Regex.Split(line, "\\s+");
            ClassicAssert.AreEqual(3, parts.Length);
            ClassicAssert.AreEqual(sentence[i], parts[0]);
            ClassicAssert.AreEqual(tags[i], parts[1]);
            ClassicAssert.AreEqual(chunks[i], parts[2]);
        }
    }

    [Test]
    public void TestNicePrint()
    {
        ChunkSample sample = new ChunkSample(CreateSentence(), CreateTags(), CreateChunks());

        ClassicAssert.AreEqual(" [NP Forecasts_NNS ] [PP for_IN ] [NP the_DT trade_NN figures_NNS ] "
            + "[VP range_VBP ] [ADVP widely_RB ] ,_, [NP Forecasts_NNS ] [PP for_IN ] "
            + "[NP the_DT trade_NN figures_NNS ] "
            + "[VP range_VBP ] [ADVP widely_RB ] ._.", sample.NicePrint());
    }

    [Test]
    public void TestAsSpan()
    {
        ChunkSample sample = new ChunkSample(CreateSentence(), CreateTags(), CreateChunks());
        Span[] spans = sample.PhrasesAsSpanList;

        ClassicAssert.AreEqual(10, spans.Length);
        ClassicAssert.AreEqual(new Span(0, 1, "NP"), spans[0]);
        ClassicAssert.AreEqual(new Span(1, 2, "PP"), spans[1]);
        ClassicAssert.AreEqual(new Span(2, 5, "NP"), spans[2]);
        ClassicAssert.AreEqual(new Span(5, 6, "VP"), spans[3]);
        ClassicAssert.AreEqual(new Span(6, 7, "ADVP"), spans[4]);
        ClassicAssert.AreEqual(new Span(8, 9, "NP"), spans[5]);
        ClassicAssert.AreEqual(new Span(9, 10, "PP"), spans[6]);
        ClassicAssert.AreEqual(new Span(10, 13, "NP"), spans[7]);
        ClassicAssert.AreEqual(new Span(13, 14, "VP"), spans[8]);
        ClassicAssert.AreEqual(new Span(14, 15, "ADVP"), spans[9]);
    }

    // following are some tests to check the argument validation. Since all uses
    // the same validateArguments method, we do a deeper test only once

    [Test]
    public void TestPhraseAsSpan()
    {
        // NOpenNLP: the static ChunkSample.phrasesAsSpanList is named
        // GetPhrasesAsSpanList in the port; the instance getter took the
        // PhrasesAsSpanList name. See the note on the ported class.
        Span[] spans = ChunkSample.GetPhrasesAsSpanList(CreateSentence(), CreateTags(), CreateChunks());

        ClassicAssert.AreEqual(10, spans.Length);
        ClassicAssert.AreEqual(new Span(0, 1, "NP"), spans[0]);
        ClassicAssert.AreEqual(new Span(1, 2, "PP"), spans[1]);
        ClassicAssert.AreEqual(new Span(2, 5, "NP"), spans[2]);
        ClassicAssert.AreEqual(new Span(5, 6, "VP"), spans[3]);
        ClassicAssert.AreEqual(new Span(6, 7, "ADVP"), spans[4]);
        ClassicAssert.AreEqual(new Span(8, 9, "NP"), spans[5]);
        ClassicAssert.AreEqual(new Span(9, 10, "PP"), spans[6]);
        ClassicAssert.AreEqual(new Span(10, 13, "NP"), spans[7]);
        ClassicAssert.AreEqual(new Span(13, 14, "VP"), spans[8]);
        ClassicAssert.AreEqual(new Span(14, 15, "ADVP"), spans[9]);
    }

    [Test]
    public void TestRegions()
    {
        IInputStreamFactory @in = new ResourceAsStreamFactory("/opennlp/tools/chunker/output.txt");

        DummyChunkSampleStream predictedSample = new DummyChunkSampleStream(
            new PlainTextByLineStream(@in, Encoding.UTF8), false);

        ChunkSample cs1 = predictedSample.Read()!;
        string[] g1 = Span.SpansToStrings(cs1.PhrasesAsSpanList, cs1.Sentence);
        ClassicAssert.AreEqual(15, g1.Length);

        ChunkSample cs2 = predictedSample.Read()!;
        string[] g2 = Span.SpansToStrings(cs2.PhrasesAsSpanList, cs2.Sentence);
        ClassicAssert.AreEqual(10, g2.Length);

        ChunkSample cs3 = predictedSample.Read()!;
        string[] g3 = Span.SpansToStrings(cs3.PhrasesAsSpanList, cs3.Sentence);
        ClassicAssert.AreEqual(7, g3.Length);
        ClassicAssert.AreEqual("United", g3[0]);
        ClassicAssert.AreEqual("'s directors", g3[1]);
        ClassicAssert.AreEqual("voted", g3[2]);
        ClassicAssert.AreEqual("themselves", g3[3]);
        ClassicAssert.AreEqual("their spouses", g3[4]);
        ClassicAssert.AreEqual("lifetime access", g3[5]);
        ClassicAssert.AreEqual("to", g3[6]);

        // NOpenNLP: upstream calls close(); the port maps IObjectStream's close()
        // onto IDisposable.Dispose.
        predictedSample.Dispose();
    }

    [Test]
    public void TestInvalidPhraseAsSpan1()
    {
        // NOpenNLP: upstream declares @Test(expected = IllegalArgumentException.class);
        // ArgumentException is the .NET counterpart.
        Assert.Throws<ArgumentException>((Action)(() =>
            ChunkSample.GetPhrasesAsSpanList(new string[2], new string[1], new string[1])));
    }

    [Test]
    public void TestInvalidPhraseAsSpan2()
    {
        // NOpenNLP: upstream declares @Test(expected = IllegalArgumentException.class);
        // ArgumentException is the .NET counterpart.
        Assert.Throws<ArgumentException>((Action)(() =>
            ChunkSample.GetPhrasesAsSpanList(new string[1], new string[2], new string[1])));
    }

    [Test]
    public void TestInvalidPhraseAsSpan3()
    {
        // NOpenNLP: upstream declares @Test(expected = IllegalArgumentException.class);
        // ArgumentException is the .NET counterpart.
        Assert.Throws<ArgumentException>((Action)(() =>
            ChunkSample.GetPhrasesAsSpanList(new string[1], new string[1], new string[2])));
    }

    [Test]
    public void TestInvalidChunkSampleArray()
    {
        // NOpenNLP: upstream declares @Test(expected = IllegalArgumentException.class);
        // ArgumentException is the .NET counterpart.
        Assert.Throws<ArgumentException>((Action)(() =>
            new ChunkSample(new string[1], new string[1], new string[2])));
    }

    [Test]
    public void TestInvalidChunkSampleList()
    {
        // NOpenNLP: upstream declares @Test(expected = IllegalArgumentException.class);
        // ArgumentException is the .NET counterpart.
        Assert.Throws<ArgumentException>((Action)(() =>
            new ChunkSample((IList<string>)new string[1], (IList<string>)new string[1],
                (IList<string>)new string[2])));
    }

    [Test]
    public void TestEquals()
    {
        ClassicAssert.IsFalse(CreateGoldSample() == CreateGoldSample());
        ClassicAssert.IsTrue(CreateGoldSample().Equals(CreateGoldSample()));
        ClassicAssert.IsFalse(CreatePredSample().Equals(CreateGoldSample()));
        ClassicAssert.IsFalse(CreatePredSample().Equals(new object()));
    }
}
