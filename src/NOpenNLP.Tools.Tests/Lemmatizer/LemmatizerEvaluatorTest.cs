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

namespace NOpenNLP.Tools.Lemmatizer;

/// <summary>
/// Tests for <see cref="LemmatizerEvaluator"/>.
/// </summary>
public class LemmatizerEvaluatorTest
{
    private const double DELTA = 1.0E-9d;

    /// <summary>
    /// Checks the evaluator results against the results got using the conlleval,
    /// available at http://www.cnts.ua.ac.be/conll2000/chunking/output.html but
    /// containing lemmas instead of chunks.
    /// </summary>
    [Test]
    public void TestEvaluator()
    {
        // NOpenNLP: upstream uses MockInputStreamFactory over a file resolved from
        // the test classpath; the test-side ResourceAsStreamFactory in Support does
        // the same job over an embedded resource.
        const string inPredicted = "/opennlp/tools/lemmatizer/output.txt";
        const string inExpected = "/opennlp/tools/lemmatizer/output.txt";

        var encoding = Encoding.UTF8;

        var predictedSample = new DummyLemmaSampleStream(
            new PlainTextByLineStream(new ResourceAsStreamFactory(inPredicted), encoding), true);

        var expectedSample = new DummyLemmaSampleStream(
            new PlainTextByLineStream(new ResourceAsStreamFactory(inExpected), encoding), false);

        ILemmatizer dummyLemmatizer = new DummyLemmatizer(predictedSample);

        var stream = new StringBuilder();
        ILemmatizerEvaluationMonitor listener = new LemmaEvaluationErrorListener(stream);
        var evaluator = new LemmatizerEvaluator(dummyLemmatizer, listener);

        evaluator.Evaluate(expectedSample);

        ClassicAssert.AreEqual(0.9877049180327869, evaluator.WordAccuracy, DELTA);
        ClassicAssert.AreNotSame(0, stream.ToString().Length);
    }

    /// <summary>
    /// NOpenNLP: upstream uses <c>opennlp.tools.cmdline.lemmatizer.LemmaEvaluationErrorListener</c>,
    /// which writes a formatted error report to an <c>OutputStream</c>. The <c>cmdline</c>
    /// package is not ported, so this test-local stand-in takes its place. It records
    /// misclassifications and writes nothing for correct ones, which is all the
    /// assertion above -- a non-empty buffer once something is misclassified --
    /// actually observes about the upstream listener.
    /// </summary>
    private sealed class LemmaEvaluationErrorListener(StringBuilder output)
        : ILemmatizerEvaluationMonitor
    {
        public void CorrectlyClassified(LemmaSample reference, LemmaSample prediction)
        {
        }

        public void Misclassified(LemmaSample reference, LemmaSample prediction) =>
            output.Append(reference).Append('\n');
    }
}
