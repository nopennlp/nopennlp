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
using NOpenNLP.Tools.Ml.Model;
using NOpenNLP.Tools.Util;
using NOpenNLP.Tools.Util.Model;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Ml.Maxent;

public class ScaleDoesntMatterTest
{
    private IDataIndexer testDataIndexer = null!;

    [SetUp]
    public void InitIndexer()
    {
        TrainingParameters trainingParameters = new();
        trainingParameters.Put(AbstractTrainer.CUTOFF_PARAM, 0);
        testDataIndexer = new OnePassRealValueDataIndexer();
        testDataIndexer.Init(trainingParameters, new Dictionary<string, string>());
    }

    /// <summary>
    /// This test sets out to prove that the scale you use on real valued
    /// predicates doesn't matter when it comes to the probability assigned to each
    /// outcome. Strangely, if we use (1,2) and (10,20) there's no difference. If
    /// we use (0.1,0.2) and (10,20) there is a difference.
    /// </summary>
    [Test]
    public void TestScaleResults()
    {
        string smallValues = "predA=0.1 predB=0.2 A\n" + "predB=0.3 predA=0.1 B\n";

        string smallTest = "predA=0.2 predB=0.2";

        string largeValues = "predA=10 predB=20 A\n" + "predB=30 predA=10 B\n";

        string largeTest = "predA=20 predB=20";

        IObjectStream<Event?> smallEventStream = new RealBasicEventStream(
            new PlainTextByLineStream(new MockInputStreamFactory(smallValues), Encoding.UTF8));

        testDataIndexer.Index(smallEventStream);

        IEventTrainer smallModelTrainer = TrainerFactory.GetEventTrainer(
            ModelUtil.CreateDefaultTrainingParameters(), null);

        IMaxentModel smallModel = smallModelTrainer.Train(testDataIndexer);
        string[] contexts = smallTest.Split(' ');
        float[]? values = RealValueFileEventStream.ParseContexts(contexts);
        double[] smallResults = smallModel.Eval(contexts, values);

        string smallResultString = smallModel.GetAllOutcomes(smallResults);
        Console.WriteLine("smallResults: " + smallResultString);

        IObjectStream<Event?> largeEventStream = new RealBasicEventStream(
            new PlainTextByLineStream(new MockInputStreamFactory(largeValues), Encoding.UTF8));

        testDataIndexer.Index(largeEventStream);

        IEventTrainer largeModelTrainer = TrainerFactory.GetEventTrainer(
            ModelUtil.CreateDefaultTrainingParameters(), null);

        IMaxentModel largeModel = largeModelTrainer.Train(testDataIndexer);
        contexts = largeTest.Split(' ');
        values = RealValueFileEventStream.ParseContexts(contexts);
        double[] largeResults = largeModel.Eval(contexts, values);

        string largeResultString = largeModel.GetAllOutcomes(largeResults);
        Console.WriteLine("largeResults: " + largeResultString);

        ClassicAssert.AreEqual(smallResults.Length, largeResults.Length);
        for (int i = 0; i < smallResults.Length; i++)
        {
            Console.WriteLine(
                $"classifiy with smallModel: {smallModel.GetOutcome(i)} = {smallResults[i]:F6}");
            Console.WriteLine(
                $"classifiy with largeModel: {largeModel.GetOutcome(i)} = {largeResults[i]:F6}");
            ClassicAssert.AreEqual(smallResults[i], largeResults[i], 0.01f);
        }
    }
}
