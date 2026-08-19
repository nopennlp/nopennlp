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
using NOpenNLP.Tools.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Ml.Perceptron;

/// <summary>
/// Test for perceptron training and use with the ppa data.
/// </summary>
public class PerceptronPrepAttachTest
{
    [Test]
    public void TestPerceptronOnPrepAttachData()
    {
        TwoPassDataIndexer indexer = new();
        TrainingParameters indexingParameters = new();
        indexingParameters.Put(AbstractTrainer.CUTOFF_PARAM, 1);
        indexingParameters.Put(AbstractDataIndexer.SORT_PARAM, false);
        indexer.Init(indexingParameters, new Dictionary<string, string>());
        indexer.Index(PrepAttachDataUtil.CreateTrainingStream());
        IMaxentModel model = new PerceptronTrainer().TrainModel(400, indexer, 1);
        PrepAttachDataUtil.TestModel(model, 0.7650408516959644);
    }

    [Test]
    public void TestPerceptronOnPrepAttachDataWithSkippedAveraging()
    {
        TrainingParameters trainParams = new();
        trainParams.Put(AbstractTrainer.ALGORITHM_PARAM, PerceptronTrainer.PERCEPTRON_VALUE);
        trainParams.Put(AbstractTrainer.CUTOFF_PARAM, 1);
        trainParams.Put("UseSkippedAveraging", true);

        IEventTrainer trainer = TrainerFactory.GetEventTrainer(trainParams, null);
        IMaxentModel model = trainer.Train(PrepAttachDataUtil.CreateTrainingStream());
        PrepAttachDataUtil.TestModel(model, 0.773706362961129);
    }

    [Test]
    public void TestPerceptronOnPrepAttachDataWithTolerance()
    {
        TrainingParameters trainParams = new();
        trainParams.Put(AbstractTrainer.ALGORITHM_PARAM, PerceptronTrainer.PERCEPTRON_VALUE);
        trainParams.Put(AbstractTrainer.CUTOFF_PARAM, 1);
        trainParams.Put(AbstractTrainer.ITERATIONS_PARAM, 500);
        trainParams.Put("Tolerance", 0.0001d);

        IEventTrainer trainer = TrainerFactory.GetEventTrainer(trainParams, null);
        IMaxentModel model = trainer.Train(PrepAttachDataUtil.CreateTrainingStream());
        PrepAttachDataUtil.TestModel(model, 0.7677642980935875);
    }

    [Test]
    public void TestPerceptronOnPrepAttachDataWithStepSizeDecrease()
    {
        TrainingParameters trainParams = new();
        trainParams.Put(AbstractTrainer.ALGORITHM_PARAM, PerceptronTrainer.PERCEPTRON_VALUE);
        trainParams.Put(AbstractTrainer.CUTOFF_PARAM, 1);
        trainParams.Put(AbstractTrainer.ITERATIONS_PARAM, 500);
        trainParams.Put("StepSizeDecrease", 0.06d);

        IEventTrainer trainer = TrainerFactory.GetEventTrainer(trainParams, null);
        IMaxentModel model = trainer.Train(PrepAttachDataUtil.CreateTrainingStream());
        PrepAttachDataUtil.TestModel(model, 0.7791532557563754);
    }

    [Test]
    public void TestModelSerialization()
    {
        TrainingParameters trainParams = new();
        trainParams.Put(AbstractTrainer.ALGORITHM_PARAM, PerceptronTrainer.PERCEPTRON_VALUE);
        trainParams.Put(AbstractTrainer.CUTOFF_PARAM, 1);
        trainParams.Put("UseSkippedAveraging", true);

        IEventTrainer trainer = TrainerFactory.GetEventTrainer(trainParams, null);
        AbstractModel model = (AbstractModel)trainer.Train(PrepAttachDataUtil.CreateTrainingStream());

        PrepAttachDataUtil.TestModel(model, 0.773706362961129);

        // serialize and load model, then check if it still works as expected
        MemoryStream modelBytes = new();
        BinaryPerceptronModelWriter writer = new(model, modelBytes);
        writer.Persist();
        writer.Dispose();

        IMaxentModel restoredModel =
            new BinaryPerceptronModelReader(new MemoryStream(modelBytes.ToArray())).Model;
        PrepAttachDataUtil.TestModel(restoredModel, 0.773706362961129);
    }

    [Test]
    public void TestModelEquals()
    {
        TrainingParameters trainParams = new();
        trainParams.Put(AbstractTrainer.ALGORITHM_PARAM, PerceptronTrainer.PERCEPTRON_VALUE);
        trainParams.Put(AbstractTrainer.CUTOFF_PARAM, 1);
        trainParams.Put("UseSkippedAveraging", true);

        IEventTrainer trainer = TrainerFactory.GetEventTrainer(trainParams, null);
        AbstractModel modelA = (AbstractModel)trainer.Train(PrepAttachDataUtil.CreateTrainingStream());
        AbstractModel modelB = (AbstractModel)trainer.Train(PrepAttachDataUtil.CreateTrainingStream());

        ClassicAssert.AreEqual(modelA, modelB);
        ClassicAssert.AreEqual(modelA.GetHashCode(), modelB.GetHashCode());
    }

    [Test]
    public void VerifyReportMap()
    {
        TrainingParameters trainParams = new();
        trainParams.Put(AbstractTrainer.ALGORITHM_PARAM, PerceptronTrainer.PERCEPTRON_VALUE);
        trainParams.Put(AbstractTrainer.CUTOFF_PARAM, 1);
        // Since we are verifying the report map, we don't need to have more than 1 iteration
        trainParams.Put(AbstractTrainer.ITERATIONS_PARAM, 1);
        trainParams.Put("UseSkippedAveraging", true);

        Dictionary<string, string> reportMap = [];
        IEventTrainer trainer = TrainerFactory.GetEventTrainer(trainParams, reportMap);
        trainer.Train(PrepAttachDataUtil.CreateTrainingStream());
        ClassicAssert.IsTrue(reportMap.ContainsKey("Training-Eventhash"),
            "Report Map does not contain the training event hash");
    }
}
