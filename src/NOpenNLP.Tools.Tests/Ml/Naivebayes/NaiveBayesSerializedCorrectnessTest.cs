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
using NOpenNLP.Tools.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Ml.Naivebayes;

/// <summary>
/// Test for naive bayes classification correctness after a serialize/deserialize
/// round trip.
/// </summary>
public class NaiveBayesSerializedCorrectnessTest
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
        testDataIndexer.Index(NaiveBayesCorrectnessTest.CreateTrainingStream());
        NaiveBayesModel model1 = (NaiveBayesModel)new NaiveBayesTrainer().TrainModel(testDataIndexer);

        NaiveBayesModel model2 = PersistedModel(model1);

        const string label = "politics";
        string[] context = ["bow=united", "bow=nations"];
        Event @event = new(label, context);

        TestModelOutcome(model1, model2, @event);
    }

    [Test]
    public void TestNaiveBayes2()
    {
        testDataIndexer.Index(NaiveBayesCorrectnessTest.CreateTrainingStream());
        NaiveBayesModel model1 = (NaiveBayesModel)new NaiveBayesTrainer().TrainModel(testDataIndexer);

        NaiveBayesModel model2 = PersistedModel(model1);

        const string label = "sports";
        string[] context = ["bow=manchester", "bow=united"];
        Event @event = new(label, context);

        TestModelOutcome(model1, model2, @event);
    }

    [Test]
    public void TestNaiveBayes3()
    {
        testDataIndexer.Index(NaiveBayesCorrectnessTest.CreateTrainingStream());
        NaiveBayesModel model1 = (NaiveBayesModel)new NaiveBayesTrainer().TrainModel(testDataIndexer);

        NaiveBayesModel model2 = PersistedModel(model1);

        const string label = "politics";
        string[] context = ["bow=united"];
        Event @event = new(label, context);

        TestModelOutcome(model1, model2, @event);
    }

    [Test]
    public void TestNaiveBayes5()
    {
        testDataIndexer.Index(NaiveBayesCorrectnessTest.CreateTrainingStream());
        NaiveBayesModel model1 = (NaiveBayesModel)new NaiveBayesTrainer().TrainModel(testDataIndexer);

        NaiveBayesModel model2 = PersistedModel(model1);

        const string label = "politics";
        string[] context = [];
        Event @event = new(label, context);

        TestModelOutcome(model1, model2, @event);
    }

    [Test]
    public void TestPlainTextModel()
    {
        testDataIndexer.Index(NaiveBayesCorrectnessTest.CreateTrainingStream());
        NaiveBayesModel model1 = (NaiveBayesModel)new NaiveBayesTrainer().TrainModel(testDataIndexer);

        // NOpenNLP: upstream round-trips through a StringWriter/StringReader pair.
        // PlainTextNaiveBayesModelReader takes a StreamReader rather than a
        // TextReader, so the round trip goes through a MemoryStream instead. The
        // writer is left open so the stream survives Persist() to be read back.
        using MemoryStream buffer1 = new();
        using (StreamWriter sw1 = new(buffer1, new UTF8Encoding(false), 1024, leaveOpen: true))
        {
            NaiveBayesModelWriter modelWriter = new PlainTextNaiveBayesModelWriter(model1, sw1);
            modelWriter.Persist();
        }

        buffer1.Position = 0;
        NaiveBayesModelReader reader = new PlainTextNaiveBayesModelReader(
            new StreamReader(buffer1, Encoding.UTF8));
        reader.CheckModelType();

        NaiveBayesModel model2 = (NaiveBayesModel)reader.ConstructModel();

        using MemoryStream buffer2 = new();
        using (StreamWriter sw2 = new(buffer2, new UTF8Encoding(false), 1024, leaveOpen: true))
        {
            NaiveBayesModelWriter modelWriter = new PlainTextNaiveBayesModelWriter(model2, sw2);
            modelWriter.Persist();
        }

        ClassicAssert.AreEqual(
            Encoding.UTF8.GetString(buffer1.ToArray()),
            Encoding.UTF8.GetString(buffer2.ToArray()));
    }

    protected static NaiveBayesModel PersistedModel(NaiveBayesModel model)
    {
        // NOpenNLP: the ".bin" extension is what makes AbstractModelReader pick the
        // binary reader, as upstream does; Path.GetTempFileName() alone gives ".tmp".
        FileInfo file = new(Path.ChangeExtension(Path.GetTempFileName(), ".bin"));
        try
        {
            NaiveBayesModelWriter modelWriter = new BinaryNaiveBayesModelWriter(model, file);
            modelWriter.Persist();
            NaiveBayesModelReader reader = new BinaryNaiveBayesModelReader(file);
            reader.CheckModelType();
            return (NaiveBayesModel)reader.ConstructModel();
        }
        finally
        {
            TryDelete(file);
        }
    }

    protected static void TestModelOutcome(NaiveBayesModel model1, NaiveBayesModel model2, Event @event)
    {
        string[] labels1 = ExtractLabels(model1);
        string[] labels2 = ExtractLabels(model2);

        CollectionAssert.AreEqual(labels1, labels2);

        double[] outcomes1 = model1.Eval(@event.Context);
        double[] outcomes2 = model2.Eval(@event.Context);

        Assert.That(outcomes2, Is.EqualTo(outcomes1).Within(0.000000000001));
    }

    private static string[] ExtractLabels(NaiveBayesModel model)
    {
        string[] labels = new string[model.NumOutcomes];
        for (int i = 0; i < model.NumOutcomes; i++)
        {
            labels[i] = model.GetOutcome(i);
        }
        return labels;
    }

    /// <summary>
    /// Deletes a temporary file, ignoring failure.
    /// </summary>
    /// <remarks>
    /// NOpenNLP: upstream calls File.delete(), which returns false rather than throwing
    /// when the file cannot be removed. The model readers hold their stream open (they
    /// are not disposable, here or upstream), so on Windows the file is still locked at
    /// this point and File.Delete throws IOException, failing an otherwise passing test.
    /// Swallowing the failure matches what upstream's delete() does. The file is in the
    /// system temp directory, so the OS reclaims it.
    /// </remarks>
    private static void TryDelete(FileInfo file)
    {
        try
        {
            file.Delete();
        }
        catch (IOException)
        {
            // The reader still holds the file open; leave it to the OS.
        }
        catch (UnauthorizedAccessException)
        {
            // As above.
        }
    }
}
