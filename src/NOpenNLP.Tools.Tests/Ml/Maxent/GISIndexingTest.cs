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
using System.Linq;
using NOpenNLP.Tools.Ml.Model;
using NOpenNLP.Tools.Util;
using NOpenNLP.Tools.Util.Model;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Ml.Maxent;

public class GISIndexingTest
{
    private static readonly string[][] cntx =
    [
        ["dog", "cat", "mouse"],
        ["text", "print", "mouse"],
        ["dog", "pig", "cat", "mouse"]
    ];

    private static readonly string[] outputs = ["A", "B", "A"];

    private static IObjectStream<Event?> CreateEventStream()
    {
        List<Event> events = [];
        for (int i = 0; i < cntx.Length; i++)
        {
            events.Add(new Event(outputs[i], cntx[i]));
        }

        return ObjectStreamUtils.CreateObjectStream(events.ToArray());
    }

    /// <summary>
    /// Tests training through the event trainer with default parameters.
    /// </summary>
    [Test]
    public void TestGISTrainSignature1()
    {
        using IObjectStream<Event?> eventStream = CreateEventStream();
        TrainingParameters @params = ModelUtil.CreateDefaultTrainingParameters();
        @params.Put(AbstractTrainer.CUTOFF_PARAM, 1);

        IEventTrainer trainer = TrainerFactory.GetEventTrainer(@params, null);

        ClassicAssert.NotNull(trainer.Train(eventStream));
    }

    /// <summary>
    /// Tests training with smoothing enabled.
    /// </summary>
    [Test]
    public void TestGISTrainSignature2()
    {
        using IObjectStream<Event?> eventStream = CreateEventStream();
        TrainingParameters @params = ModelUtil.CreateDefaultTrainingParameters();
        @params.Put(AbstractTrainer.CUTOFF_PARAM, 1);
        @params.Put("smoothing", true);
        IEventTrainer trainer = TrainerFactory.GetEventTrainer(@params, null);

        ClassicAssert.NotNull(trainer.Train(eventStream));
    }

    /// <summary>
    /// Tests training with an explicit iteration count and cutoff.
    /// </summary>
    [Test]
    public void TestGISTrainSignature3()
    {
        using IObjectStream<Event?> eventStream = CreateEventStream();
        TrainingParameters @params = ModelUtil.CreateDefaultTrainingParameters();

        @params.Put(AbstractTrainer.ITERATIONS_PARAM, 10);
        @params.Put(AbstractTrainer.CUTOFF_PARAM, 1);

        IEventTrainer trainer = TrainerFactory.GetEventTrainer(@params, null);

        ClassicAssert.NotNull(trainer.Train(eventStream));
    }

    /// <summary>
    /// Tests training with a Gaussian sigma.
    /// </summary>
    [Test]
    public void TestGISTrainSignature4()
    {
        using IObjectStream<Event?> eventStream = CreateEventStream();
        TrainingParameters @params = ModelUtil.CreateDefaultTrainingParameters();
        @params.Put(AbstractTrainer.ITERATIONS_PARAM, 10);
        @params.Put(AbstractTrainer.CUTOFF_PARAM, 1);
        GISTrainer trainer = (GISTrainer)TrainerFactory.GetEventTrainer(@params, null);
        trainer.GaussianSigma = 0.01;

        ClassicAssert.NotNull(trainer.TrainModel(eventStream));
    }

    /// <summary>
    /// Tests training with smoothing off and messages suppressed.
    /// </summary>
    [Test]
    public void TestGISTrainSignature5()
    {
        using IObjectStream<Event?> eventStream = CreateEventStream();
        TrainingParameters @params = ModelUtil.CreateDefaultTrainingParameters();

        @params.Put(AbstractTrainer.ITERATIONS_PARAM, 10);
        @params.Put(AbstractTrainer.CUTOFF_PARAM, 1);
        @params.Put("smoothing", false);
        @params.Put(AbstractTrainer.VERBOSE_PARAM, false);

        IEventTrainer trainer = TrainerFactory.GetEventTrainer(@params, null);
        ClassicAssert.NotNull(trainer.Train(eventStream));
    }

    [Test]
    public void TestIndexingWithTrainingParameters()
    {
        IObjectStream<Event?> eventStream = CreateEventStream();

        TrainingParameters parameters = TrainingParameters.DefaultParams();
        // by default we are using GIS/EventTrainer/Cutoff of 5/100 iterations
        parameters.Put(TrainingParameters.ITERATIONS_PARAM, 10);
        parameters.Put(AbstractEventTrainer.DATA_INDEXER_PARAM,
            AbstractEventTrainer.DATA_INDEXER_ONE_PASS_VALUE);
        parameters.Put(AbstractEventTrainer.CUTOFF_PARAM, 1);
        // note: setting the SORT_PARAM to true is the default, so it is not really needed
        parameters.Put(AbstractDataIndexer.SORT_PARAM, true);

        // guarantee that you have a GIS trainer...
        IEventTrainer trainer = TrainerFactory.GetEventTrainer(parameters, new Dictionary<string, string>());
        ClassicAssert.AreEqual("NOpenNLP.Tools.Ml.Maxent.GISTrainer", trainer.GetType().FullName);
        AbstractEventTrainer aeTrainer = (AbstractEventTrainer)trainer;
        // guarantee that you have a OnePassDataIndexer ...
        IDataIndexer di = aeTrainer.GetDataIndexer(eventStream);
        ClassicAssert.AreEqual("NOpenNLP.Tools.Ml.Model.OnePassDataIndexer", di.GetType().FullName);
        ClassicAssert.AreEqual(3, di.NumEvents);
        ClassicAssert.AreEqual(2, di.OutcomeLabels.Length);
        ClassicAssert.AreEqual(6, di.PredLabels.Length);

        // NOpenNLP: upstream continues here by switching the algorithm to
        // QNTrainer.MAXENT_QN_VALUE and the indexer to TwoPass, asserting that a
        // QNTrainer and a TwoPassDataIndexer come back. The quasi-newton trainer
        // is not ported yet, so that half of the test is left out; it belongs
        // with the QNTrainer port.

        eventStream.Dispose();
    }

    [Test]
    public void TestIndexingFactory()
    {
        Dictionary<string, string> myReportMap = [];
        IObjectStream<Event?> eventStream = CreateEventStream();

        // set the cutoff to 1 for this test.
        TrainingParameters parameters = new();
        parameters.Put(AbstractDataIndexer.CUTOFF_PARAM, 1);

        // test with a 1 pass data indexer...
        parameters.Put(AbstractEventTrainer.DATA_INDEXER_PARAM,
            AbstractEventTrainer.DATA_INDEXER_ONE_PASS_VALUE);
        IDataIndexer di = DataIndexerFactory.GetDataIndexer(parameters, myReportMap);
        ClassicAssert.AreEqual("NOpenNLP.Tools.Ml.Model.OnePassDataIndexer", di.GetType().FullName);
        di.Index(eventStream);
        ClassicAssert.AreEqual(3, di.NumEvents);
        ClassicAssert.AreEqual(2, di.OutcomeLabels.Length);
        ClassicAssert.AreEqual(6, di.PredLabels.Length);

        eventStream.Reset();

        // test with a 2-pass data indexer...
        parameters.Put(AbstractEventTrainer.DATA_INDEXER_PARAM,
            AbstractEventTrainer.DATA_INDEXER_TWO_PASS_VALUE);
        di = DataIndexerFactory.GetDataIndexer(parameters, myReportMap);
        ClassicAssert.AreEqual("NOpenNLP.Tools.Ml.Model.TwoPassDataIndexer", di.GetType().FullName);
        di.Index(eventStream);
        ClassicAssert.AreEqual(3, di.NumEvents);
        ClassicAssert.AreEqual(2, di.OutcomeLabels.Length);
        ClassicAssert.AreEqual(6, di.PredLabels.Length);

        // the rest of the test doesn't actually index, so we can close the eventstream.
        eventStream.Dispose();

        // test with a 1-pass Real value dataIndexer
        parameters.Put(AbstractEventTrainer.DATA_INDEXER_PARAM,
            AbstractEventTrainer.DATA_INDEXER_ONE_PASS_REAL_VALUE);
        di = DataIndexerFactory.GetDataIndexer(parameters, myReportMap);
        ClassicAssert.AreEqual("NOpenNLP.Tools.Ml.Model.OnePassRealValueDataIndexer",
            di.GetType().FullName);

        // test with an UNRegistered MockIndexer
        parameters.Put(AbstractEventTrainer.DATA_INDEXER_PARAM,
            "NOpenNLP.Tools.Ml.Maxent.MockDataIndexer");
        di = DataIndexerFactory.GetDataIndexer(parameters, myReportMap);
        ClassicAssert.AreEqual("NOpenNLP.Tools.Ml.Maxent.MockDataIndexer", di.GetType().FullName);
    }
}
