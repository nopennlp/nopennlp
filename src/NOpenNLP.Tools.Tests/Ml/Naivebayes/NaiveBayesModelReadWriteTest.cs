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
using System.IO;
using NOpenNLP.Tools.Ml.Model;
using NOpenNLP.Tools.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Ml.Naivebayes;

/// <summary>
/// Tests for persisting and reading naive bayes models.
/// </summary>
public class NaiveBayesModelReadWriteTest
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
    public void TestBinaryModelPersistence()
    {
        testDataIndexer.Index(NaiveBayesCorrectnessTest.CreateTrainingStream());
        NaiveBayesModel model = (NaiveBayesModel)new NaiveBayesTrainer().TrainModel(testDataIndexer);
        // NOpenNLP: the extension matters. AbstractModelReader dispatches on ".bin"
        // to pick the binary reader, exactly as upstream does, so the temp file
        // needs that suffix rather than the ".tmp" Path.GetTempFileName() gives.
        FileInfo file = new(Path.ChangeExtension(Path.GetTempFileName(), ".bin"));
        try
        {
            NaiveBayesModelWriter modelWriter = new BinaryNaiveBayesModelWriter(model, file);
            modelWriter.Persist();
            NaiveBayesModelReader reader = new BinaryNaiveBayesModelReader(file);
            reader.CheckModelType();
            AbstractModel abstractModel = reader.ConstructModel();
            ClassicAssert.NotNull(abstractModel);
        }
        finally
        {
            file.Delete();
        }
    }

    [Test]
    public void TestTextModelPersistence()
    {
        testDataIndexer.Index(NaiveBayesCorrectnessTest.CreateTrainingStream());
        NaiveBayesModel model = (NaiveBayesModel)new NaiveBayesTrainer().TrainModel(testDataIndexer);
        FileInfo file = new(Path.ChangeExtension(Path.GetTempFileName(), ".txt"));
        try
        {
            NaiveBayesModelWriter modelWriter = new PlainTextNaiveBayesModelWriter(model, file);
            modelWriter.Persist();
            NaiveBayesModelReader reader = new PlainTextNaiveBayesModelReader(file);
            reader.CheckModelType();
            AbstractModel abstractModel = reader.ConstructModel();
            ClassicAssert.NotNull(abstractModel);
        }
        finally
        {
            file.Delete();
        }
    }
}
