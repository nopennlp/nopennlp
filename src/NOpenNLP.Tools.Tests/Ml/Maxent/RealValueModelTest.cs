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
using NOpenNLP.Tools.Ml.Model;
using NOpenNLP.Tools.Support;
using NOpenNLP.Tools.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Ml.Maxent;

public class RealValueModelTest
{
    private IDataIndexer testDataIndexer = null!;

    [SetUp]
    public void InitIndexer()
    {
        TrainingParameters trainingParameters = new();
        trainingParameters.Put(AbstractTrainer.CUTOFF_PARAM, 1);
        testDataIndexer = new OnePassRealValueDataIndexer();
        testDataIndexer.Init(trainingParameters, new Dictionary<string, string>());
    }

    [Test]
    public void TestRealValuedWeightsVsRepeatWeighting()
    {
        GISModel realModel;
        GISTrainer gisTrainer = new();

        // NOpenNLP: upstream reads these two corpora from a path under
        // src/test/resources. They are embedded resources here, so each is copied
        // to a temporary file for the file-based event streams to open.
        using (TempResourceFile realData =
            new("/data/opennlp/maxent/real-valued-weights-training-data.txt"))
        using (RealValueFileEventStream rvfes1 = new(realData.Path))
        {
            testDataIndexer.Index(rvfes1);
            realModel = gisTrainer.TrainModel(100, testDataIndexer);
        }

        GISModel repeatModel;
        using (TempResourceFile repeatData =
            new("/data/opennlp/maxent/repeat-weighting-training-data.txt"))
        using (FileEventStream rvfes2 = new(repeatData.Path))
        {
            testDataIndexer.Index(rvfes2);
            repeatModel = gisTrainer.TrainModel(100, testDataIndexer);
        }

        string[] features2Classify = ["feature2", "feature5"];
        double[] realResults = realModel.Eval(features2Classify);
        double[] repeatResults = repeatModel.Eval(features2Classify);

        ClassicAssert.AreEqual(realResults.Length, repeatResults.Length);
        for (int i = 0; i < realResults.Length; i++)
        {
            Console.WriteLine($"classifiy with realModel: {realModel.GetOutcome(i)} = {realResults[i]:F6}");
            Console.WriteLine(
                $"classifiy with repeatModel: {repeatModel.GetOutcome(i)} = {repeatResults[i]:F6}");
            ClassicAssert.AreEqual(realResults[i], repeatResults[i], 0.01f);
        }

        features2Classify = ["feature1", "feature2", "feature3", "feature4", "feature5"];
        // NOpenNLP: the collection expression cannot pick between the float[] and
        // double[] Eval overloads, so the array type is stated explicitly. Upstream
        // has only the float[] overload here.
        float[] evalValues = [5.5f, 6.1f, 9.1f, 4.0f, 1.8f];
        realResults = realModel.Eval(features2Classify, evalValues);
        repeatResults = repeatModel.Eval(features2Classify, evalValues);

        Console.WriteLine();
        ClassicAssert.AreEqual(realResults.Length, repeatResults.Length);
        for (int i = 0; i < realResults.Length; i++)
        {
            Console.WriteLine($"classifiy with realModel: {realModel.GetOutcome(i)} = {realResults[i]:F6}");
            Console.WriteLine(
                $"classifiy with repeatModel: {repeatModel.GetOutcome(i)} = {repeatResults[i]:F6}");
            ClassicAssert.AreEqual(realResults[i], repeatResults[i], 0.01f);
        }
    }

    // NOpenNLP-specific: the file-based event streams take a path, but the test
    // corpora are embedded resources here, so one is materialized to a temp file
    // for the duration of the test.
    private sealed class TempResourceFile : IDisposable
    {
        public TempResourceFile(string resourcePath)
        {
            Path = System.IO.Path.GetTempFileName();
            using Stream source = TestResources.OpenResource(resourcePath);
            using FileStream target = File.Create(Path);
            source.CopyTo(target);
        }

        public string Path { get; }

        public void Dispose() => File.Delete(Path);
    }
}
