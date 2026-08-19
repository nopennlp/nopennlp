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

namespace NOpenNLP.Tools.Ml.Maxent.Quasinewton;

public class QNPrepAttachTest
{
    [Test]
    public void TestQNOnPrepAttachData()
    {
        IDataIndexer indexer = new TwoPassDataIndexer();
        TrainingParameters indexingParameters = new();
        indexingParameters.Put(AbstractTrainer.CUTOFF_PARAM, 1);
        indexingParameters.Put(AbstractDataIndexer.SORT_PARAM, false);
        indexer.Init(indexingParameters, new Dictionary<string, string>());
        indexer.Index(PrepAttachDataUtil.CreateTrainingStream());

        AbstractModel model = new QNTrainer(true).TrainModel(100, indexer);

        PrepAttachDataUtil.TestModel(model, 0.8155484030700668);
    }

    [Test]
    public void TestQNOnPrepAttachDataWithParamsDefault()
    {
        TrainingParameters trainParams = new();
        trainParams.Put(AbstractTrainer.ALGORITHM_PARAM, QNTrainer.MAXENT_QN_VALUE);

        IMaxentModel model = TrainerFactory.GetEventTrainer(trainParams, null)
            .Train(PrepAttachDataUtil.CreateTrainingStream());

        PrepAttachDataUtil.TestModel(model, 0.8115870264917059);
    }

    [Test]
    public void TestQNOnPrepAttachDataWithElasticNetParams()
    {
        TrainingParameters trainParams = new();
        trainParams.Put(AbstractTrainer.ALGORITHM_PARAM, QNTrainer.MAXENT_QN_VALUE);
        trainParams.Put(AbstractEventTrainer.DATA_INDEXER_PARAM,
            AbstractEventTrainer.DATA_INDEXER_TWO_PASS_VALUE);
        trainParams.Put(AbstractTrainer.CUTOFF_PARAM, 1);
        trainParams.Put(QNTrainer.L1COST_PARAM, 0.25);
        trainParams.Put(QNTrainer.L2COST_PARAM, 1.0d);

        IMaxentModel model = TrainerFactory.GetEventTrainer(trainParams, null)
            .Train(PrepAttachDataUtil.CreateTrainingStream());

        PrepAttachDataUtil.TestModel(model, 0.8229759841544937);
    }

    [Test]
    public void TestQNOnPrepAttachDataWithL1Params()
    {
        TrainingParameters trainParams = new();
        trainParams.Put(AbstractTrainer.ALGORITHM_PARAM, QNTrainer.MAXENT_QN_VALUE);
        trainParams.Put(AbstractEventTrainer.DATA_INDEXER_PARAM,
            AbstractEventTrainer.DATA_INDEXER_TWO_PASS_VALUE);
        trainParams.Put(AbstractTrainer.CUTOFF_PARAM, 1);
        trainParams.Put(QNTrainer.L1COST_PARAM, 1.0d);
        trainParams.Put(QNTrainer.L2COST_PARAM, 0d);

        IMaxentModel model = TrainerFactory.GetEventTrainer(trainParams, null)
            .Train(PrepAttachDataUtil.CreateTrainingStream());

        PrepAttachDataUtil.TestModel(model, 0.8180242634315424);
    }

    [Test]
    public void TestQNOnPrepAttachDataWithL2Params()
    {
        TrainingParameters trainParams = new();
        trainParams.Put(AbstractTrainer.ALGORITHM_PARAM, QNTrainer.MAXENT_QN_VALUE);
        trainParams.Put(AbstractEventTrainer.DATA_INDEXER_PARAM,
            AbstractEventTrainer.DATA_INDEXER_TWO_PASS_VALUE);
        trainParams.Put(AbstractTrainer.CUTOFF_PARAM, 1);
        trainParams.Put(QNTrainer.L1COST_PARAM, 0d);
        trainParams.Put(QNTrainer.L2COST_PARAM, 1.0d);

        IMaxentModel model = TrainerFactory.GetEventTrainer(trainParams, null)
            .Train(PrepAttachDataUtil.CreateTrainingStream());

        PrepAttachDataUtil.TestModel(model, 0.8227283981183461);
    }

    [Test]
    public void TestQNOnPrepAttachDataInParallel()
    {
        TrainingParameters trainParams = new();
        trainParams.Put(AbstractTrainer.ALGORITHM_PARAM, QNTrainer.MAXENT_QN_VALUE);
        trainParams.Put(QNTrainer.THREADS_PARAM, 2);

        IMaxentModel model = TrainerFactory.GetEventTrainer(trainParams, null)
            .Train(PrepAttachDataUtil.CreateTrainingStream());

        PrepAttachDataUtil.TestModel(model, 0.8115870264917059);
    }
}
