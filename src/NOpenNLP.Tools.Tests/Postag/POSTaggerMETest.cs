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
using NOpenNLP.Tools.Util.Model;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Postag;

/// <summary>
/// Tests for the <see cref="POSTaggerME"/> class.
/// </summary>
public class POSTaggerMETest
{
    private static IObjectStream<POSSample?> CreateSampleStream()
    {
        IInputStreamFactory @in = new ResourceAsStreamFactory(
            "/opennlp/tools/postag/AnnotatedSentences.txt");

        return new WordTagSampleStream(new PlainTextByLineStream(@in, Encoding.UTF8));
    }

    /// <summary>
    /// Trains a POSModel from the annotated test data.
    /// </summary>
    /// <returns><see cref="POSModel"/></returns>
    public static POSModel TrainPOSModel(ModelType type)
    {
        TrainingParameters @params = new TrainingParameters();
        @params.Put(TrainingParameters.ALGORITHM_PARAM, type.ToString());
        @params.Put(TrainingParameters.ITERATIONS_PARAM, 100);
        @params.Put(TrainingParameters.CUTOFF_PARAM, 5);

        return POSTaggerME.Train("eng", CreateSampleStream(), @params, new POSTaggerFactory());
    }

    [Test]
    public void TestPOSTagger()
    {
        POSModel posModel = TrainPOSModel(ModelType.MAXENT);

        IPOSTagger tagger = new POSTaggerME(posModel);

        string[] tags = tagger.Tag([
            "The",
            "driver",
            "got",
            "badly",
            "injured",
            "."]);

        ClassicAssert.AreEqual(6, tags.Length);
        ClassicAssert.AreEqual("DT", tags[0]);
        ClassicAssert.AreEqual("NN", tags[1]);
        ClassicAssert.AreEqual("VBD", tags[2]);
        ClassicAssert.AreEqual("RB", tags[3]);
        ClassicAssert.AreEqual("VBN", tags[4]);
        ClassicAssert.AreEqual(".", tags[5]);
    }

    [Test]
    public void TestBuildNGramDictionary()
    {
        IObjectStream<POSSample?> samples = CreateSampleStream();
        POSTaggerME.BuildNGramDictionary(samples, 0);
    }

    [Test]
    public void InsufficientTestData()
    {
        IInputStreamFactory @in = new ResourceAsStreamFactory(
            "/opennlp/tools/postag/AnnotatedSentencesInsufficient.txt");

        IObjectStream<POSSample?> stream = new WordTagSampleStream(
            new PlainTextByLineStream(@in, Encoding.UTF8));

        TrainingParameters @params = new TrainingParameters();
        @params.Put(TrainingParameters.ALGORITHM_PARAM, ModelType.MAXENT.ToString());
        @params.Put(TrainingParameters.ITERATIONS_PARAM, 100);
        @params.Put(TrainingParameters.CUTOFF_PARAM, 5);

        Assert.Throws<InsufficientTrainingDataException>((Action)(() =>
            POSTaggerME.Train("eng", stream, @params, new POSTaggerFactory())));
    }
}
