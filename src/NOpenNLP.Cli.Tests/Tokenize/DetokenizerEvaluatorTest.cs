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

using System.IO;
using NOpenNLP.Tools.Tokenize;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Cmdline.Tokenizer;

/// <remarks>
/// NOpenNLP: upstream keeps this test in opennlp-tools, beside the detokenizer it
/// evaluates, because the cmdline package ships there too. This port splits cmdline out
/// into NOpenNLP.Cli, which NOpenNLP.Tools.Tests does not reference, so the test lives
/// here instead of at its upstream-mirrored path.
/// </remarks>
public class DetokenizerEvaluatorTest
{
    [Test]
    public void TestPositive()
    {
        Stream stream = new MemoryStream();
        DetokenEvaluationErrorListener listener = new(stream);

        // NOpenNLP: upstream calls TokenSampleTest.createGoldSample(). Those factories are
        // split out into TokenSampleFixtures so this project can link them without also
        // re-running TokenSampleTest's own [Test] methods here.
        DetokenizerEvaluator eval = new(new DummyDetokenizer(
            TokenSampleFixtures.CreateGoldSample()), listener);

        eval.EvaluateSample(TokenSampleFixtures.CreateGoldSample());

        ClassicAssert.AreEqual(1.0, eval.FMeasure.Value, 0.0);

        ClassicAssert.AreEqual(0, StreamText(stream).Length);
    }

    [Test]
    public void TestNegative()
    {
        Stream stream = new MemoryStream();
        DetokenEvaluationErrorListener listener = new(stream);

        DetokenizerEvaluator eval = new(new DummyDetokenizer(
            TokenSampleFixtures.CreateGoldSample()), listener);

        eval.EvaluateSample(TokenSampleFixtures.CreatePredSilverSample());

        ClassicAssert.AreEqual(-1.0d, eval.FMeasure.Value, .1d);

        ClassicAssert.AreNotSame(0, StreamText(stream).Length);
    }

    // NOpenNLP: upstream reads the buffer back with OutputStream.toString(), which
    // ByteArrayOutputStream overrides to decode its bytes. A .NET MemoryStream has no
    // such override, so the bytes are decoded here instead.
    private static string StreamText(Stream stream) =>
        System.Text.Encoding.UTF8.GetString(((MemoryStream)stream).ToArray());

    /// <summary>
    /// a dummy tokenizer that always return something expected
    /// </summary>
    private sealed class DummyDetokenizer(TokenSample sample) : IDetokenizer
    {
        private readonly TokenSample sample = sample; // NOpenNLP: made readonly

        public DetokenizationOperation[] Detokenize(string[] tokens) => null!;

        public string Detokenize(string[] tokens, string? splitMarker) => sample.Text;
    }
}
