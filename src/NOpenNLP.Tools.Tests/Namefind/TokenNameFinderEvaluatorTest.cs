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

namespace NOpenNLP.Tools.Namefind;

/// <summary>
/// This is the test class for <see cref="TokenNameFinderEvaluator"/>.
/// </summary>
public class TokenNameFinderEvaluatorTest
{
    /// <summary>
    /// Return a dummy name finder that always return something expected
    /// </summary>
    // NOpenNLP: upstream builds this with Mockito; the test project has no mocking
    // library, so the stand-in below plays the same role.
    public static ITokenNameFinder MockTokenNameFinder(Span[] ret) => new DummyTokenNameFinder(ret);

    [Test]
    public void TestPositive()
    {
        StringBuilder stream = new();
        ITokenNameFinderEvaluationMonitor listener = new NameEvaluationErrorListener(stream);

        Span[] pred = CreateSimpleNameSampleA().Names;
        // Construct mock object
        TokenNameFinderEvaluator eval = new(MockTokenNameFinder(pred), listener);

        eval.EvaluateSample(CreateSimpleNameSampleA());

        ClassicAssert.AreEqual(1.0, eval.FMeasure.Value, 0.0);

        ClassicAssert.AreEqual(0, stream.ToString().Length);
    }

    [Test]
    public void TestNegative()
    {
        StringBuilder stream = new();
        ITokenNameFinderEvaluationMonitor listener = new NameEvaluationErrorListener(stream);

        Span[] pred = CreateSimpleNameSampleB().Names;
        // Construct mock object
        TokenNameFinderEvaluator eval = new(MockTokenNameFinder(pred), listener);

        eval.EvaluateSample(CreateSimpleNameSampleA());

        ClassicAssert.AreEqual(0.8, eval.FMeasure.Value, 0.0);

        ClassicAssert.AreNotSame(0, stream.ToString().Length);
    }

    private static readonly string[] sentence = ["U", ".", "S", ".", "President", "Barack", "Obama", "is",
        "considering", "sending", "additional", "American", "forces",
        "to", "Afghanistan", "."];

    private static NameSample CreateSimpleNameSampleA()
    {
        Span[] names = [new Span(0, 4, "Location"), new Span(5, 7, "Person"),
            new Span(14, 15, "Location")];

        return new NameSample(sentence, names, false);
    }

    private static NameSample CreateSimpleNameSampleB()
    {
        Span[] names = [new Span(0, 4, "Location"), new Span(14, 15, "Location")];

        return new NameSample(sentence, names, false);
    }

    /// <summary>
    /// NOpenNLP: upstream uses <c>opennlp.tools.cmdline.namefind.NameEvaluationErrorListener</c>,
    /// which writes a formatted error report to an <c>OutputStream</c>. The <c>cmdline</c>
    /// package is not ported, so this test-local stand-in takes its place. It records
    /// misclassifications and writes nothing for correct ones, which is all the two
    /// assertions above -- an empty buffer on a match, a non-empty one on a mismatch --
    /// actually observe about the upstream listener.
    /// </summary>
    private sealed class NameEvaluationErrorListener(StringBuilder output)
        : ITokenNameFinderEvaluationMonitor
    {
        public void CorrectlyClassified(NameSample reference, NameSample prediction)
        {
        }

        public void Misclassified(NameSample reference, NameSample prediction) =>
            output.Append(reference).Append('\n');
    }

    /// <summary>
    /// NOpenNLP: stands in for the Mockito mock upstream builds, which is stubbed to
    /// return a fixed Span[] from find() for any input.
    /// </summary>
    private sealed class DummyTokenNameFinder(Span[] ret) : ITokenNameFinder
    {
        public Span[] Find(string[] tokens) => ret;

        public void ClearAdaptiveData()
        {
        }
    }
}
