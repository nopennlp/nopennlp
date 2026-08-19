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
using NOpenNLP.Tools.Ml.Model;
using NOpenNLP.Tools.Util;
using NUnit.Framework;

namespace NOpenNLP.Tools.Ml.Maxent;

public class MaxentPrepAttachTest
{
    private IDataIndexer testDataIndexer = null!;

    [SetUp]
    public void InitIndexer()
    {
        TrainingParameters trainingParameters = new();
        trainingParameters.Put(AbstractTrainer.CUTOFF_PARAM, 1);
        trainingParameters.Put(AbstractDataIndexer.SORT_PARAM, false);
        testDataIndexer = new TwoPassDataIndexer();
        testDataIndexer.Init(trainingParameters, new Dictionary<string, string>());
    }

    [Test]
    public void TestMaxentOnPrepAttachData()
    {
        testDataIndexer.Index(PrepAttachDataUtil.CreateTrainingStream());
        AbstractModel model = new GISTrainer(true).TrainModel(100,
            testDataIndexer, new UniformPrior(), 1);
        PrepAttachDataUtil.TestModel(model, 0.7997028967566229);
    }

    [Test]
    public void TestMaxentOnPrepAttachData2Threads()
    {
        testDataIndexer.Index(PrepAttachDataUtil.CreateTrainingStream());
        AbstractModel model = new GISTrainer(true).TrainModel(100,
            testDataIndexer, new UniformPrior(), 2);
        PrepAttachDataUtil.TestModel(model, 0.7997028967566229);
    }

    [Test]
    public void TestMaxentOnPrepAttachDataWithParams()
    {
        TrainingParameters trainParams = new();
        trainParams.Put(AbstractTrainer.ALGORITHM_PARAM, GISTrainer.MAXENT_VALUE);
        trainParams.Put(AbstractEventTrainer.DATA_INDEXER_PARAM,
            AbstractEventTrainer.DATA_INDEXER_TWO_PASS_VALUE);
        trainParams.Put(AbstractTrainer.CUTOFF_PARAM, 1);

        IEventTrainer trainer = TrainerFactory.GetEventTrainer(trainParams, null);
        IMaxentModel model = trainer.Train(PrepAttachDataUtil.CreateTrainingStream());

        PrepAttachDataUtil.TestModel(model, 0.7997028967566229);
    }

    [Test]
    public void TestMaxentOnPrepAttachDataWithParamsDefault()
    {
        TrainingParameters trainParams = new();
        trainParams.Put(AbstractTrainer.ALGORITHM_PARAM, GISTrainer.MAXENT_VALUE);

        IEventTrainer trainer = TrainerFactory.GetEventTrainer(trainParams, null);
        IMaxentModel model = trainer.Train(PrepAttachDataUtil.CreateTrainingStream());

        PrepAttachDataUtil.TestModel(model, 0.8086159940579352);
    }

    [Test]
    public void TestMaxentOnPrepAttachDataWithParamsLLThreshold()
    {
        TrainingParameters trainParams = new();
        trainParams.Put(AbstractTrainer.ALGORITHM_PARAM, GISTrainer.MAXENT_VALUE);
        trainParams.Put(GISTrainer.LOG_LIKELIHOOD_THRESHOLD_PARAM, 5.0);

        IEventTrainer trainer = TrainerFactory.GetEventTrainer(trainParams, null);
        IMaxentModel model = trainer.Train(PrepAttachDataUtil.CreateTrainingStream());

        PrepAttachDataUtil.TestModel(model, 0.8103490963109681);
    }
}
