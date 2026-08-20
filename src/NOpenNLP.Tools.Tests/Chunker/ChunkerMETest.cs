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
using System.Text;
using NOpenNLP.Tools.Formats;
using NOpenNLP.Tools.Support;
using NOpenNLP.Tools.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Chunker;

/// <summary>
/// This is the test class for <see cref="ChunkerME"/>.
/// <para/>
/// A proper testing and evaluation of the name finder is only possible with a
/// large corpus which contains a huge amount of test sentences.
/// <para/>
/// The scope of this test is to make sure that the name finder code can be
/// executed. This test can not detect mistakes which lead to incorrect feature
/// generation or other mistakes which decrease the tagging performance of the
/// name finder.
/// <para/>
/// In this test the name finder is trained with a small amount of
/// training sentences and then the computed model is used to predict sentences
/// from the training sentences.
/// </summary>
public class ChunkerMETest
{
    private IChunker chunker = null!;

    private static readonly string[] toks1 =
    [
        "Rockwell", "said", "the", "agreement", "calls", "for",
        "it", "to", "supply", "200", "additional", "so-called", "shipsets",
        "for", "the", "planes", "."
    ];

    private static readonly string[] tags1 =
    [
        "NNP", "VBD", "DT", "NN", "VBZ", "IN", "PRP", "TO", "VB",
        "CD", "JJ", "JJ", "NNS", "IN", "DT", "NNS", "."
    ];

    private static readonly string[] expect1 =
    [
        "B-NP", "B-VP", "B-NP", "I-NP", "B-VP", "B-SBAR",
        "B-NP", "B-VP", "I-VP", "B-NP", "I-NP", "I-NP", "I-NP", "B-PP", "B-NP",
        "I-NP", "O"
    ];

    [SetUp]
    public void Startup()
    {
        // train the chunker

        IInputStreamFactory @in = new ResourceAsStreamFactory("/opennlp/tools/chunker/test.txt");

        IObjectStream<ChunkSample?> sampleStream = new ChunkSampleStream(
            new PlainTextByLineStream(@in, Encoding.UTF8));

        TrainingParameters @params = new TrainingParameters();
        @params.Put(TrainingParameters.ITERATIONS_PARAM, 70);
        @params.Put(TrainingParameters.CUTOFF_PARAM, 1);

        ChunkerModel chunkerModel = ChunkerME.Train("eng", sampleStream, @params, new ChunkerFactory());

        this.chunker = new ChunkerME(chunkerModel);
    }

    [Test]
    public void TestChunkAsArray()
    {
        string[] preds = chunker.Chunk(toks1, tags1);

        CollectionAssert.AreEqual(expect1, preds);
    }

    [Test]
    public void TestChunkAsSpan()
    {
        Span[] preds = chunker.ChunkAsSpans(toks1, tags1);
        Console.Out.WriteLine("[" + string.Join(", ", (object[])preds) + "]");

        ClassicAssert.AreEqual(10, preds.Length);
        ClassicAssert.AreEqual(new Span(0, 1, "NP"), preds[0]);
        ClassicAssert.AreEqual(new Span(1, 2, "VP"), preds[1]);
        ClassicAssert.AreEqual(new Span(2, 4, "NP"), preds[2]);
        ClassicAssert.AreEqual(new Span(4, 5, "VP"), preds[3]);
        ClassicAssert.AreEqual(new Span(5, 6, "SBAR"), preds[4]);
        ClassicAssert.AreEqual(new Span(6, 7, "NP"), preds[5]);
        ClassicAssert.AreEqual(new Span(7, 9, "VP"), preds[6]);
        ClassicAssert.AreEqual(new Span(9, 13, "NP"), preds[7]);
        ClassicAssert.AreEqual(new Span(13, 14, "PP"), preds[8]);
        ClassicAssert.AreEqual(new Span(14, 16, "NP"), preds[9]);
    }

    [Test]
    public void TestTokenProbArray()
    {
        Sequence[] preds = chunker.TopKSequences(toks1, tags1);

        ClassicAssert.IsTrue(preds.Length > 0);
        ClassicAssert.AreEqual(expect1.Length, preds[0].Probs.Length);
        CollectionAssert.AreEqual(expect1, preds[0].Outcomes);
        ClassicAssert.AreNotSame(expect1, preds[1].Outcomes);
    }

    [Test]
    public void TestTokenProbMinScore()
    {
        Sequence[] preds = chunker.TopKSequences(toks1, tags1, -5.55);

        ClassicAssert.AreEqual(4, preds.Length);
        ClassicAssert.AreEqual(expect1.Length, preds[0].Probs.Length);
        CollectionAssert.AreEqual(expect1, preds[0].Outcomes);
        ClassicAssert.AreNotSame(expect1, preds[1].Outcomes);
    }

    [Test]
    public void TestInsufficientData()
    {
        IInputStreamFactory @in = new ResourceAsStreamFactory(
            "/opennlp/tools/chunker/test-insufficient.txt");

        IObjectStream<ChunkSample?> sampleStream = new ChunkSampleStream(
            new PlainTextByLineStream(@in, Encoding.UTF8));

        TrainingParameters @params = new TrainingParameters();
        @params.Put(TrainingParameters.ITERATIONS_PARAM, 70);
        @params.Put(TrainingParameters.CUTOFF_PARAM, 1);

        Assert.Throws<InsufficientTrainingDataException>((Action)(() =>
            ChunkerME.Train("eng", sampleStream, @params, new ChunkerFactory())));
    }
}
