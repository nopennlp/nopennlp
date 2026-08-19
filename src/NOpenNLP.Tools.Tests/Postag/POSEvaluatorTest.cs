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
using NOpenNLP.Tools.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Postag;

public class POSEvaluatorTest
{
    [Test]
    public void TestPositive()
    {
        StringBuilder stream = new StringBuilder();
        IPOSTaggerEvaluationMonitor listener = new POSEvaluationErrorListener(stream);

        POSEvaluator eval = new POSEvaluator(new DummyPOSTagger(
            POSSampleTest.CreateGoldSample()), listener);

        eval.EvaluateSample(POSSampleTest.CreateGoldSample());
        ClassicAssert.AreEqual(1.0, eval.WordAccuracy, 0.0);
        ClassicAssert.AreEqual(0, stream.ToString().Length);
    }

    [Test]
    public void TestNegative()
    {
        StringBuilder stream = new StringBuilder();
        IPOSTaggerEvaluationMonitor listener = new POSEvaluationErrorListener(stream);

        POSEvaluator eval = new POSEvaluator(
            new DummyPOSTagger(POSSampleTest.CreateGoldSample()), listener);

        eval.EvaluateSample(POSSampleTest.CreatePredSample());
        ClassicAssert.AreEqual(.7, eval.WordAccuracy, .1d);
        ClassicAssert.AreNotSame(0, stream.ToString().Length);
    }

    /// <summary>
    /// NOpenNLP: upstream uses <c>opennlp.tools.cmdline.postag.POSEvaluationErrorListener</c>,
    /// which writes a formatted error report to an <c>OutputStream</c>. The <c>cmdline</c>
    /// package is not ported, so this test-local stand-in takes its place. It records
    /// misclassifications and writes nothing for correct ones, which is all the two
    /// assertions above -- an empty buffer on a match, a non-empty one on a mismatch --
    /// actually observe about the upstream listener.
    /// </summary>
    private sealed class POSEvaluationErrorListener(StringBuilder output)
        : IPOSTaggerEvaluationMonitor
    {
        public void CorrectlyClassified(POSSample reference, POSSample prediction)
        {
        }

        public void Misclassified(POSSample reference, POSSample prediction)
        {
            output.Append(reference.ToString()).Append('\n');
        }
    }

    private sealed class DummyPOSTagger(POSSample sample) : IPOSTagger
    {
        public string[] Tag(string[] sentence) => sample.Tags;

        public string[] Tag(string[] sentence, object[]? additionaContext) => Tag(sentence);

        // NOpenNLP: upstream returns null here; the ported interface declares a
        // non-nullable Sequence[], and no test calls these methods.
        public Sequence[] TopKSequences(string[] sentence) => [];

        public Sequence[] TopKSequences(string[] sentence, object[]? additionaContext) =>
            TopKSequences(sentence);
    }
}
