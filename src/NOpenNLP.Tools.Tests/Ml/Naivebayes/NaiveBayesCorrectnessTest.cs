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
/// Test for naive bayes classification correctness without smoothing.
/// </summary>
public class NaiveBayesCorrectnessTest
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
    public void TestNaiveBayes1()
    {
        testDataIndexer.Index(CreateTrainingStream());
        NaiveBayesModel model = (NaiveBayesModel)new NaiveBayesTrainer().TrainModel(testDataIndexer);

        const string label = "politics";
        string[] context = ["bow=united", "bow=nations"];
        Event @event = new(label, context);

        // TestModel(model, @event, 1.0);  // Expected value without smoothing
        TestModel(model, @event, 0.9681650180264167);   // Expected value with smoothing
    }

    [Test]
    public void TestNaiveBayes2()
    {
        testDataIndexer.Index(CreateTrainingStream());
        NaiveBayesModel model = (NaiveBayesModel)new NaiveBayesTrainer().TrainModel(testDataIndexer);

        const string label = "sports";
        string[] context = ["bow=manchester", "bow=united"];
        Event @event = new(label, context);

        // TestModel(model, @event, 1.0);  // Expected value without smoothing
        TestModel(model, @event, 0.9658833555831029);   // Expected value with smoothing
    }

    [Test]
    public void TestNaiveBayes3()
    {
        testDataIndexer.Index(CreateTrainingStream());
        NaiveBayesModel model = (NaiveBayesModel)new NaiveBayesTrainer().TrainModel(testDataIndexer);

        const string label = "politics";
        string[] context = ["bow=united"];
        Event @event = new(label, context);

        // TestModel(model, @event, 2.0/3.0);  // Expected value without smoothing
        TestModel(model, @event, 0.6655036407766989);  // Expected value with smoothing
    }

    [Test]
    public void TestNaiveBayes4()
    {
        testDataIndexer.Index(CreateTrainingStream());
        NaiveBayesModel model = (NaiveBayesModel)new NaiveBayesTrainer().TrainModel(testDataIndexer);

        const string label = "politics";
        string[] context = [];
        Event @event = new(label, context);

        TestModel(model, @event, 7.0 / 12.0);
    }

    private static void TestModel(IMaxentModel model, Event @event, double higherProbability)
    {
        double[] outcomes = model.Eval(@event.Context);
        string outcome = model.GetBestOutcome(outcomes);
        ClassicAssert.AreEqual(2, outcomes.Length);
        ClassicAssert.AreEqual(@event.Outcome, outcome);
        if (@event.Outcome.Equals(model.GetOutcome(0)))
        {
            ClassicAssert.AreEqual(higherProbability, outcomes[0], 0.0001);
        }
        if (!@event.Outcome.Equals(model.GetOutcome(0)))
        {
            ClassicAssert.AreEqual(1.0 - higherProbability, outcomes[0], 0.0001);
        }
        if (@event.Outcome.Equals(model.GetOutcome(1)))
        {
            ClassicAssert.AreEqual(higherProbability, outcomes[1], 0.0001);
        }
        if (!@event.Outcome.Equals(model.GetOutcome(1)))
        {
            ClassicAssert.AreEqual(1.0 - higherProbability, outcomes[1], 0.0001);
        }
    }

    public static IObjectStream<Event?> CreateTrainingStream()
    {
        List<Event> trainingEvents = [];

        const string label1 = "politics";
        string[] context1 = ["bow=the", "bow=united", "bow=nations"];
        trainingEvents.Add(new Event(label1, context1));

        const string label2 = "politics";
        string[] context2 = ["bow=the", "bow=united", "bow=states", "bow=and"];
        trainingEvents.Add(new Event(label2, context2));

        const string label3 = "sports";
        string[] context3 = ["bow=manchester", "bow=united"];
        trainingEvents.Add(new Event(label3, context3));

        const string label4 = "sports";
        string[] context4 = ["bow=manchester", "bow=and", "bow=barca"];
        trainingEvents.Add(new Event(label4, context4));

        // NOpenNLP: upstream calls ObjectStreamUtils.createObjectStream(Collection).
        // That overload is omitted in the port (see ObjectStreamUtils), so the
        // CollectionObjectStream it delegates to is constructed directly.
        return new CollectionObjectStream<Event>(trainingEvents);
    }
}
