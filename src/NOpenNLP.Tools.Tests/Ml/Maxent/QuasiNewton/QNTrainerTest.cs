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

using System.Collections.Generic;
using System.IO;
using NOpenNLP.Tools.Ml.Model;
using NOpenNLP.Tools.Support;
using NOpenNLP.Tools.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Ml.Maxent.Quasinewton;

public class QNTrainerTest
{
    private const int ITERATIONS = 50;

    private IDataIndexer testDataIndexer = null!;

    [SetUp]
    public void InitIndexer()
    {
        TrainingParameters trainingParameters = new();
        trainingParameters.Put(AbstractTrainer.CUTOFF_PARAM, 1);
        testDataIndexer = new OnePassRealValueDataIndexer();
        testDataIndexer.Init(trainingParameters, new Dictionary<string, string>());
    }

    // NOpenNLP: upstream reads the corpus from a path under src/test/resources. It is
    // an embedded resource here, so it is copied to a temporary file for the
    // file-based event stream to open.
    private void IndexTrainingData()
    {
        using TempResourceFile data =
            new("/data/opennlp/maxent/real-valued-weights-training-data.txt");
        using RealValueFileEventStream rvfes1 = new(data.Path);
        testDataIndexer.Index(rvfes1);
    }

    [Test]
    public void TestTrainModelReturnsAQNModel()
    {
        // given
        IndexTrainingData();
        // when
        QNModel trainedModel = new QNTrainer(false).TrainModel(ITERATIONS, testDataIndexer);
        // then
        ClassicAssert.NotNull(trainedModel);
    }

    [Test]
    public void TestInTinyDevSet()
    {
        // given
        IndexTrainingData();
        // when
        QNModel trainedModel = new QNTrainer(15, true).TrainModel(ITERATIONS, testDataIndexer);
        string[] features2Classify =
        [
            "feature2", "feature3", "feature3",
            "feature3", "feature3", "feature3",
            "feature3", "feature3", "feature3",
            "feature3", "feature3", "feature3"
        ];
        double[] eval = trainedModel.Eval(features2Classify);
        // then
        ClassicAssert.NotNull(eval);
    }

    [Test]
    public void TestModel()
    {
        // given
        IndexTrainingData();
        // when
        QNModel trainedModel = new QNTrainer(15, true).TrainModel(
            ITERATIONS, testDataIndexer);

        ClassicAssert.IsFalse(trainedModel.Equals(null));
    }

    [Test]
    public void TestSerdeModel()
    {
        // given
        IndexTrainingData();
        // when
        QNModel trainedModel = new QNTrainer(5, 700, true).TrainModel(ITERATIONS, testDataIndexer);

        MemoryStream modelBytes = new();
        GenericModelWriter modelWriter = new(trainedModel, modelBytes);
        modelWriter.Persist();
        modelWriter.Close();

        GenericModelReader modelReader = new(new BinaryFileDataReader(
            new MemoryStream(modelBytes.ToArray())));
        AbstractModel readModel = modelReader.Model;
        QNModel deserModel = (QNModel)readModel;

        ClassicAssert.IsTrue(trainedModel.Equals(deserModel));

        string[] features2Classify =
        [
            "feature2", "feature3", "feature3",
            "feature3", "feature3", "feature3",
            "feature3", "feature3", "feature3",
            "feature3", "feature3", "feature3"
        ];
        double[] eval01 = trainedModel.Eval(features2Classify);
        double[] eval02 = deserModel.Eval(features2Classify);

        ClassicAssert.AreEqual(eval01.Length, eval02.Length);
        for (int i = 0; i < eval01.Length; i++)
        {
            ClassicAssert.AreEqual(eval01[i], eval02[i], 0.00000001);
        }
    }
}
