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

using NOpenNLP.Tools.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System.IO;
using System.Text;

namespace NOpenNLP.Tools.Sentdetect;

public class SentenceDetectorEvaluatorTest
{
    [Test]
    public void TestPositive()
    {
        StringBuilder stream = new StringBuilder();
        ISentenceDetectorEvaluationMonitor listener = new SentenceEvaluationErrorListener(stream);

        SentenceDetectorEvaluator eval = new SentenceDetectorEvaluator(new DummySD(
            SentenceSampleTest.CreateGoldSample()), listener);

        eval.EvaluateSample(SentenceSampleTest.CreateGoldSample());

        ClassicAssert.AreEqual(1.0, eval.FMeasure.Value, 0.0);

        ClassicAssert.AreEqual(0, stream.ToString().Length);
    }

    [Test]
    public void TestNegative()
    {
        StringBuilder stream = new StringBuilder();
        ISentenceDetectorEvaluationMonitor listener = new SentenceEvaluationErrorListener(stream);

        SentenceDetectorEvaluator eval = new SentenceDetectorEvaluator(new DummySD(
            SentenceSampleTest.CreateGoldSample()), listener);

        eval.EvaluateSample(SentenceSampleTest.CreatePredSample());

        ClassicAssert.AreEqual(-1.0, eval.FMeasure.Value, .1d);

        ClassicAssert.AreNotSame(0, stream.ToString().Length);
    }

    /// <summary>
    /// NOpenNLP: upstream uses <c>opennlp.tools.cmdline.sentdetect.SentenceEvaluationErrorListener</c>,
    /// which writes a formatted error report to an <c>OutputStream</c>. The <c>cmdline</c>
    /// package is not ported, so this test-local stand-in takes its place. It records
    /// misclassifications and writes nothing for correct ones, which is all the two
    /// assertions above -- an empty buffer on a match, a non-empty one on a mismatch --
    /// actually observe about the upstream listener.
    /// </summary>
    private sealed class SentenceEvaluationErrorListener(StringBuilder output)
        : ISentenceDetectorEvaluationMonitor
    {
        public void CorrectlyClassified(SentenceSample reference, SentenceSample prediction)
        {
        }

        public void Misclassified(SentenceSample reference, SentenceSample prediction)
        {
            output.Append(reference.Document).Append('\n');
        }
    }

    /// <summary>
    /// a dummy sentence detector that always return something expected
    /// </summary>
    private sealed class DummySD(SentenceSample sample) : ISentenceDetector
    {
        // NOpenNLP: upstream returns null here; the ported interface declares a
        // non-nullable string[], and no test calls this method.
        public string[] SentDetect(string s) => [];

        public Span[] SentPosDetect(string s) => sample.GetSentences();
    }
}
