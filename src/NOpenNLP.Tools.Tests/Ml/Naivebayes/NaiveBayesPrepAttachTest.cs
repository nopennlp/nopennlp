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
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Ml.Naivebayes;

/// <summary>
/// Test for Naive Bayes training and use with the ppa data.
/// </summary>
public class NaiveBayesPrepAttachTest
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
    public void TestNaiveBayesOnPrepAttachData()
    {
        testDataIndexer.Index(PrepAttachDataUtil.CreateTrainingStream());
        IMaxentModel model = new NaiveBayesTrainer().TrainModel(testDataIndexer);
        ClassicAssert.IsTrue(model is NaiveBayesModel);
        PrepAttachDataUtil.TestModel(model, 0.7897994553107205);
    }

    [Test]
    public void TestNaiveBayesOnPrepAttachDataUsingTrainUtil()
    {
        TrainingParameters trainParams = new();
        trainParams.Put(AbstractTrainer.ALGORITHM_PARAM, NaiveBayesTrainer.NAIVE_BAYES_VALUE);
        trainParams.Put(AbstractTrainer.CUTOFF_PARAM, 1);

        IEventTrainer trainer = TrainerFactory.GetEventTrainer(trainParams, null);
        IMaxentModel model = trainer.Train(PrepAttachDataUtil.CreateTrainingStream());
        ClassicAssert.IsTrue(model is NaiveBayesModel);
        PrepAttachDataUtil.TestModel(model, 0.7897994553107205);
    }

    [Test]
    public void TestNaiveBayesOnPrepAttachDataUsingTrainUtilWithCutoff5()
    {
        TrainingParameters trainParams = new();
        trainParams.Put(AbstractTrainer.ALGORITHM_PARAM, NaiveBayesTrainer.NAIVE_BAYES_VALUE);
        trainParams.Put(AbstractTrainer.CUTOFF_PARAM, 5);

        IEventTrainer trainer = TrainerFactory.GetEventTrainer(trainParams, null);
        IMaxentModel model = trainer.Train(PrepAttachDataUtil.CreateTrainingStream());
        ClassicAssert.IsTrue(model is NaiveBayesModel);
        PrepAttachDataUtil.TestModel(model, 0.7945035899975241);
    }
}
