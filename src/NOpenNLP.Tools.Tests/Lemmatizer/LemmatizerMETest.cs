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
using NOpenNLP.Tools.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Lemmatizer;

/// <summary>
/// This is the test class for <see cref="LemmatizerME"/>.
/// <para/>
/// A proper testing and evaluation of the name finder is only possible with a
/// large corpus which contains a huge amount of test sentences.
/// <para/>
/// The scope of this test is to make sure that the name finder code can be
/// executed. This test can not detect mistakes which lead to incorrect feature
/// generation or other mistakes which decrease the tagging performance of the
/// name finder.
/// <para/>
/// In this test the <see cref="LemmatizerME"/> is trained with a small amount of
/// training sentences and then the computed model is used to predict sentences
/// from the training sentences.
/// </summary>
public class LemmatizerMETest
{
    private LemmatizerME lemmatizer = null!;

    private static readonly string[] tokens = ["Rockwell", "said", "the", "agreement", "calls", "for",
        "it", "to", "supply", "200", "additional", "so-called", "shipsets", "for",
        "the", "planes", "."];

    private static readonly string[] postags = ["NNP", "VBD", "DT", "NN", "VBZ", "IN", "PRP", "TO", "VB",
        "CD", "JJ", "JJ", "NNS", "IN", "DT", "NNS", "."];

    private static readonly string[] expect = ["rockwell", "say", "the", "agreement", "call", "for",
        "it", "to", "supply", "200", "additional", "so-called", "shipset", "for",
        "the", "plane", "."];

    [SetUp]
    public void Startup()
    {
        // train the lemmatizer

        // NOpenNLP: upstream uses MockInputStreamFactory over a file resolved from
        // the test classpath; the test-side ResourceAsStreamFactory in Support does
        // the same job over an embedded resource.
        IObjectStream<LemmaSample?> sampleStream = new LemmaSampleStream(
            new PlainTextByLineStream(
                new ResourceAsStreamFactory("/opennlp/tools/lemmatizer/trial.old.tsv"), Encoding.UTF8));

        var parameters = new TrainingParameters();
        parameters.Put(TrainingParameters.ITERATIONS_PARAM, 100);
        parameters.Put(TrainingParameters.CUTOFF_PARAM, 5);

        var lemmatizerModel = LemmatizerME.Train("eng", sampleStream,
            parameters, new LemmatizerFactory());

        lemmatizer = new LemmatizerME(lemmatizerModel);
    }

    [Test]
    public void TestLemmasAsArray()
    {
        var lemmas = lemmatizer.Lemmatize(tokens, postags);

        CollectionAssert.AreEqual(expect, lemmas);
    }

    [Test]
    public void TestInsufficientData()
    {
        IObjectStream<LemmaSample?> sampleStream = new LemmaSampleStream(
            new PlainTextByLineStream(
                new ResourceAsStreamFactory("/opennlp/tools/lemmatizer/trial.old-insufficient.tsv"),
                Encoding.UTF8));

        var parameters = new TrainingParameters();
        parameters.Put(TrainingParameters.ITERATIONS_PARAM, 100);
        parameters.Put(TrainingParameters.CUTOFF_PARAM, 5);

        Assert.Throws<InsufficientTrainingDataException>((Action)(() =>
            LemmatizerME.Train("eng", sampleStream, parameters, new LemmatizerFactory())));
    }
}
