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
using NOpenNLP.Tools.Support;
using NOpenNLP.Tools.Util;
using NOpenNLP.Tools.Util.Model;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Namefind;

public class TokenNameFinderCrossValidatorTest
{
    private const string? TYPE = null;

    /// <summary>
    /// Test that reproduces jira OPENNLP-463
    /// </summary>
    [Test]
    public void TestWithNullResources()
    {
        // NOpenNLP: upstream uses opennlp.tools.formats.ResourceAsStreamFactory,
        // which lives in the not-yet-ported formats package; the test-side
        // ResourceAsStreamFactory in Support does the same job over an embedded
        // resource.
        IInputStreamFactory @in = new ResourceAsStreamFactory(
            "/opennlp/tools/namefind/AnnotatedSentences.txt");

        IObjectStream<NameSample?> sampleStream = new NameSampleDataStream(
            new PlainTextByLineStream(@in, Latin1));

        TrainingParameters mlParams = new();
        mlParams.Put(TrainingParameters.ITERATIONS_PARAM, 70);
        mlParams.Put(TrainingParameters.CUTOFF_PARAM, 1);

        mlParams.Put(TrainingParameters.ALGORITHM_PARAM, ModelType.MAXENT.ToString());

        // NOpenNLP: upstream's bare `null` binds to the TokenNameFinderFactory
        // overload, since its byte[] overload also requires a resources map. The
        // cast pins the same overload here, where both would otherwise apply.
        TokenNameFinderCrossValidator cv = new("eng",
            TYPE, mlParams, (TokenNameFinderFactory)null!, (ITokenNameFinderEvaluationMonitor?)null);

        cv.Evaluate(sampleStream, 2);

        ClassicAssert.IsNotNull(cv.FMeasure);
    }

    /// <summary>
    /// Test that tries to reproduce jira OPENNLP-466
    /// </summary>
    [Test]
    public void TestWithNameEvaluationErrorListener()
    {
        IInputStreamFactory @in = new ResourceAsStreamFactory(
            "/opennlp/tools/namefind/AnnotatedSentences.txt");

        IObjectStream<NameSample?> sampleStream = new NameSampleDataStream(
            new PlainTextByLineStream(@in, Latin1));

        TrainingParameters mlParams = new();
        mlParams.Put(TrainingParameters.ITERATIONS_PARAM, 70);
        mlParams.Put(TrainingParameters.CUTOFF_PARAM, 1);

        mlParams.Put(TrainingParameters.ALGORITHM_PARAM, ModelType.MAXENT.ToString());

        StringBuilder @out = new();
        // NOpenNLP: upstream uses opennlp.tools.cmdline.namefind.NameEvaluationErrorListener;
        // the cmdline package is not ported, so the test-local stand-in in
        // TokenNameFinderEvaluatorTest is reused here. The assertion below only
        // observes that the listener wrote something for a misclassification.
        var listener = new CrossValidatorErrorListener(@out);

        IDictionary<string, object> resources = new JCG.Dictionary<string, object>();
        TokenNameFinderCrossValidator cv = new("eng",
            TYPE, mlParams, null, resources, listener);

        cv.Evaluate(sampleStream, 2);

        ClassicAssert.IsTrue(@out.Length > 0);
        ClassicAssert.IsNotNull(cv.FMeasure);
    }

    [Test]
    public void TestWithInsufficientData()
    {
        IInputStreamFactory @in = new ResourceAsStreamFactory(
            "/opennlp/tools/namefind/AnnotatedSentencesInsufficient.txt");

        IObjectStream<NameSample?> sampleStream = new NameSampleDataStream(
            new PlainTextByLineStream(@in, Latin1));

        TrainingParameters mlParams = new();
        mlParams.Put(TrainingParameters.ITERATIONS_PARAM, 70);
        mlParams.Put(TrainingParameters.CUTOFF_PARAM, 1);

        mlParams.Put(TrainingParameters.ALGORITHM_PARAM, ModelType.MAXENT.ToString());

        // NOpenNLP: upstream's bare `null` binds to the TokenNameFinderFactory
        // overload, since its byte[] overload also requires a resources map. The
        // cast pins the same overload here, where both would otherwise apply.
        TokenNameFinderCrossValidator cv = new("eng",
            TYPE, mlParams, (TokenNameFinderFactory)null!, (ITokenNameFinderEvaluationMonitor?)null);

        Assert.Throws<InsufficientTrainingDataException>((Action)(() => cv.Evaluate(sampleStream, 2)));
    }

    /// <summary>
    /// NOpenNLP: stands in for the upstream cmdline NameEvaluationErrorListener; see
    /// the note on <see cref="TestWithNameEvaluationErrorListener"/>.
    /// </summary>
    private sealed class CrossValidatorErrorListener(StringBuilder output)
        : ITokenNameFinderEvaluationMonitor
    {
        public void CorrectlyClassified(NameSample reference, NameSample prediction)
        {
        }

        public void Misclassified(NameSample reference, NameSample prediction) =>
            output.Append(reference).Append('\n');
    }

    // NOpenNLP: StandardCharsets.ISO_8859_1 has no named BCL counterpart that is
    // registered on every target, so the code page is used directly.
    private static Encoding Latin1 => Encoding.GetEncoding(28591);
}
