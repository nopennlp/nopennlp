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

using NOpenNLP.Tools.Ml.Maxent;
using NOpenNLP.Tools.Ml.Perceptron;
using NOpenNLP.Tools.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Ml;

public class TrainerFactoryTest
{
    private TrainingParameters mlParams = null!;

    [SetUp]
    public void Setup()
    {
        mlParams = new TrainingParameters();
        mlParams.Put(TrainingParameters.ALGORITHM_PARAM, GISTrainer.MAXENT_VALUE);
        mlParams.Put(TrainingParameters.ITERATIONS_PARAM, 10);
        mlParams.Put(TrainingParameters.CUTOFF_PARAM, 5);
    }

    [Test]
    public void TestBuiltInValid()
    {
        ClassicAssert.IsTrue(TrainerFactory.IsValid(mlParams));
    }

    [Test]
    public void TestSequenceTrainerValid()
    {
        mlParams.Put(TrainingParameters.ALGORITHM_PARAM, typeof(MockSequenceTrainer).FullName!);
        ClassicAssert.IsTrue(TrainerFactory.IsValid(mlParams));
    }

    [Test]
    public void TestEventTrainerValid()
    {
        mlParams.Put(TrainingParameters.ALGORITHM_PARAM, typeof(MockEventTrainer).FullName!);
        ClassicAssert.IsTrue(TrainerFactory.IsValid(mlParams));
    }

    [Test]
    public void TestInvalidTrainer()
    {
        mlParams.Put(TrainingParameters.ALGORITHM_PARAM, "xyz");
        ClassicAssert.IsFalse(TrainerFactory.IsValid(mlParams));
    }

    [Test]
    public void TestIsSequenceTrainerTrue()
    {
        mlParams.Put(AbstractTrainer.ALGORITHM_PARAM,
            SimplePerceptronSequenceTrainer<object>.PERCEPTRON_SEQUENCE_VALUE);

        TrainerFactory.TrainerType? trainerType = TrainerFactory.GetTrainerType(mlParams);

        ClassicAssert.IsTrue(TrainerFactory.TrainerType.EVENT_MODEL_SEQUENCE_TRAINER.Equals(trainerType));
    }

    [Test]
    public void TestIsSequenceTrainerFalse()
    {
        mlParams.Put(AbstractTrainer.ALGORITHM_PARAM, GISTrainer.MAXENT_VALUE);
        TrainerFactory.TrainerType? trainerType = TrainerFactory.GetTrainerType(mlParams);
        ClassicAssert.IsFalse(TrainerFactory.TrainerType.EVENT_MODEL_SEQUENCE_TRAINER.Equals(trainerType));
    }
}
