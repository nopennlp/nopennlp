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

namespace NOpenNLP.Tools.Ml.Maxent;

public class GISTrainerTest
{
    [Test]
    public void TestGaussianSmoothing()
    {
        TrainingParameters @params = new();
        @params.Put("Algorithm", "MAXENT");
        @params.Put("DataIndexer", "OnePass");
        @params.Put("Cutoff", 0);
        @params.Put("Iterations", 5);
        @params.Put("GaussianSmoothing", true);

        Dictionary<string, string> reportMap = [];
        IEventTrainer trainer = TrainerFactory.GetEventTrainer(@params, reportMap);

        using IObjectStream<Event?> eventStream = new FootballEventStream();
        AbstractModel smoothedModel = (AbstractModel)trainer.Train(eventStream);
        IDictionary<string, Context> predMap =
            (IDictionary<string, Context>)smoothedModel.GetDataStructures()[1];

        double[] nevilleFalseExpected = [-0.17, .10, 0.05];
        double[] nevilleTrueExpected = [0.080, -0.047, -0.080];

        string predicateToTest = "Neville=false";
        Assert.That(predMap[predicateToTest].Parameters,
            Is.EqualTo(nevilleFalseExpected).Within(0.01));
        predicateToTest = "Neville=true";
        Assert.That(predMap[predicateToTest].Parameters,
            Is.EqualTo(nevilleTrueExpected).Within(0.001));

        eventStream.Reset();
        @params.Put("GaussianSmoothing", false);
        trainer = TrainerFactory.GetEventTrainer(@params, reportMap);
        AbstractModel unsmoothedModel = (AbstractModel)trainer.Train(eventStream);
        predMap = (IDictionary<string, Context>)unsmoothedModel.GetDataStructures()[1];

        nevilleFalseExpected = [-0.19, 0.11, 0.06];
        nevilleTrueExpected = [0.081, -0.050, -0.084];

        predicateToTest = "Neville=false";
        Assert.That(predMap[predicateToTest].Parameters,
            Is.EqualTo(nevilleFalseExpected).Within(0.01));
        predicateToTest = "Neville=true";
        Assert.That(predMap[predicateToTest].Parameters,
            Is.EqualTo(nevilleTrueExpected).Within(0.001));
    }
}
