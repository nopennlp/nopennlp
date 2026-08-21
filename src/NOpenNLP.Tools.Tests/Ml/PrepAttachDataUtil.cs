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
using System.IO;
using System.Text;
using NOpenNLP.Tools.Ml.Model;
using NOpenNLP.Tools.Support;
using NOpenNLP.Tools.Util;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Ml;

public class PrepAttachDataUtil
{
    private static List<Event> ReadPpaFile(string filename)
    {
        List<Event> events = [];

        using Stream @in = TestResources.OpenResource("/data/ppa/" + filename);
        using StreamReader reader = new(@in, Encoding.UTF8);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            string[] items = line.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
            string label = items[5];
            string[] context = ["verb=" + items[1], "noun=" + items[2],
                "prep=" + items[3], "prep_obj=" + items[4]];
            events.Add(new Event(label, context));
        }

        return events;
    }

    public static IObjectStream<Event?> CreateTrainingStream()
    {
        List<Event> trainingEvents = ReadPpaFile("training");
        return ObjectStreamUtils.CreateObjectStream(trainingEvents.ToArray());
    }

    public static void TestModel(IMaxentModel model, double expecedAccuracy)
    {
        List<Event> devEvents = ReadPpaFile("devset");

        int total = 0;
        int correct = 0;
        foreach (Event ev in devEvents)
        {
            string targetLabel = ev.Outcome;
            double[] ocs = model.Eval(ev.Context);

            int best = 0;
            for (int i = 1; i < ocs.Length; i++)
            {
                if (ocs[i] > ocs[best])
                {
                    best = i;
                }
            }

            string predictedLabel = model.GetOutcome(best);

            if (targetLabel.Equals(predictedLabel, StringComparison.Ordinal))
            {
                correct++;
            }

            total++;
        }

        double accuracy = correct / (double)total;
        Console.WriteLine("Accuracy on PPA devset: (" + correct + "/" + total + ") " + accuracy);

        ClassicAssert.AreEqual(expecedAccuracy, accuracy, .00001);
    }
}
