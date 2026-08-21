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

using System.Text;
using NOpenNLP.Tools.Formats;
using NOpenNLP.Tools.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Chunker;

/// <summary>
/// Tests for <see cref="ChunkerEvaluator"/>.
/// </summary>
/// <seealso cref="ChunkerEvaluator"/>
public class ChunkerEvaluatorTest
{
    private const double DELTA = 1.0E-9d;

    /// <summary>
    /// Checks the evaluator results against the results got using the conlleval,
    /// available at http://www.cnts.ua.ac.be/conll2000/chunking/output.html
    /// The output.txt file has only 3 sentences, but can be replaced by the one
    /// available at the conll2000 site to validate using a bigger sample.
    /// </summary>
    [Test]
    public void TestEvaluator()
    {
        IInputStreamFactory inPredicted = new ResourceAsStreamFactory("/opennlp/tools/chunker/output.txt");
        IInputStreamFactory inExpected = new ResourceAsStreamFactory("/opennlp/tools/chunker/output.txt");

        var predictedSample = new DummyChunkSampleStream(
            new PlainTextByLineStream(inPredicted, Encoding.UTF8), true);

        var expectedSample = new DummyChunkSampleStream(
            new PlainTextByLineStream(inExpected, Encoding.UTF8), false);

        IChunker dummyChunker = new DummyChunker(predictedSample);

        var stream = new StringBuilder();
        IChunkerEvaluationMonitor listener = new ChunkEvaluationErrorListener(stream);
        var evaluator = new ChunkerEvaluator(dummyChunker, listener);

        evaluator.Evaluate(expectedSample);

        var fm = evaluator.FMeasure;

        ClassicAssert.AreEqual(0.8d, fm.PrecisionScore, DELTA);
        ClassicAssert.AreEqual(0.875d, fm.RecallScore, DELTA);

        ClassicAssert.AreNotSame(0, stream.ToString().Length);
    }

    [Test]
    public void TestEvaluatorNoError()
    {
        IInputStreamFactory inPredicted = new ResourceAsStreamFactory("/opennlp/tools/chunker/output.txt");
        IInputStreamFactory inExpected = new ResourceAsStreamFactory("/opennlp/tools/chunker/output.txt");

        var predictedSample = new DummyChunkSampleStream(
            new PlainTextByLineStream(inPredicted, Encoding.UTF8), true);

        var expectedSample = new DummyChunkSampleStream(
            new PlainTextByLineStream(inExpected, Encoding.UTF8), true);

        IChunker dummyChunker = new DummyChunker(predictedSample);

        var stream = new StringBuilder();
        IChunkerEvaluationMonitor listener = new ChunkEvaluationErrorListener(stream);
        var evaluator = new ChunkerEvaluator(dummyChunker, listener);

        evaluator.Evaluate(expectedSample);

        var fm = evaluator.FMeasure;

        ClassicAssert.AreEqual(1d, fm.PrecisionScore, DELTA);
        ClassicAssert.AreEqual(1d, fm.RecallScore, DELTA);

        ClassicAssert.AreEqual(stream.ToString().Length, 0);
    }

    /// <summary>
    /// NOpenNLP: upstream uses <c>opennlp.tools.cmdline.chunker.ChunkEvaluationErrorListener</c>,
    /// which writes a formatted error report to an <c>OutputStream</c>. The <c>cmdline</c>
    /// package is not ported, so this test-local stand-in takes its place. It records
    /// misclassifications and writes nothing for correct ones, which is all the two
    /// assertions above -- an empty buffer on a match, a non-empty one on a mismatch --
    /// actually observe about the upstream listener.
    /// </summary>
    private sealed class ChunkEvaluationErrorListener(StringBuilder output) : IChunkerEvaluationMonitor
    {
        public void CorrectlyClassified(ChunkSample reference, ChunkSample prediction)
        {
        }

        public void Misclassified(ChunkSample reference, ChunkSample prediction)
            => output.Append(reference.ToString()).Append('\n');
    }
}
