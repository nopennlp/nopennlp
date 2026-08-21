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

using System.IO;
using System.Text;
using NOpenNLP.Tools.Formats;
using NOpenNLP.Tools.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Langdetect;

public class LanguageDetectorMETest
{
    private LanguageDetectorModel model = null!;

    [SetUp]
    public void Init() => this.model = TrainModel();

    [Test]
    public void TestPredictLanguages()
    {
        ILanguageDetector ld = new LanguageDetectorME(this.model);
        var languages = ld.PredictLanguages("estava em uma marcenaria na Rua Bruno");

        ClassicAssert.AreEqual(4, languages.Length);
        ClassicAssert.AreEqual("pob", languages[0].Lang);
        ClassicAssert.AreEqual("ita", languages[1].Lang);
        ClassicAssert.AreEqual("spa", languages[2].Lang);
        ClassicAssert.AreEqual("fra", languages[3].Lang);
    }

    [Test]
    public void TestProbingPredictLanguages()
    {
        LanguageDetectorME ld = new(this.model);
        for (int i = 0; i < 10000; i += 1000)
        {
            StringBuilder sb = new();
            for (int j = 0; j <= i; j++)
            {
                sb.Append("estava em uma marcenaria na Rua Bruno ");
            }

            var result = ld.ProbingPredictLanguages(sb.ToString());
            ClassicAssert.IsTrue(result.Length <= 600);
            var languages = result.Languages;
            ClassicAssert.AreEqual(4, languages.Length);
            ClassicAssert.AreEqual("pob", languages[0].Lang);
            ClassicAssert.AreEqual("ita", languages[1].Lang);
            ClassicAssert.AreEqual("spa", languages[2].Lang);
            ClassicAssert.AreEqual("fra", languages[3].Lang);
        }
    }

    [Test]
    public void TestPredictLanguage()
    {
        ILanguageDetector ld = new LanguageDetectorME(this.model);
        var language = ld.PredictLanguage("Dove è meglio che giochi");

        ClassicAssert.AreEqual("ita", language.Lang);
    }

    [Test]
    public void TestSupportedLanguages()
    {
        ILanguageDetector ld = new LanguageDetectorME(this.model);
        var supportedLanguages = ld.SupportedLanguages;

        ClassicAssert.AreEqual(4, supportedLanguages.Length);
    }

    [Test]
    public void TestLoadFromSerialized()
    {
        var serialized = SerializeModel(model);

        LanguageDetectorModel myModel = new(new MemoryStream(serialized));

        ClassicAssert.NotNull(myModel);
    }

    protected internal static byte[] SerializeModel(LanguageDetectorModel model)
    {
        MemoryStream @out = new();
        model.Serialize(@out);
        return @out.ToArray();
    }

    public static LanguageDetectorModel TrainModel() => TrainModel(new LanguageDetectorFactory());

    public static LanguageDetectorModel TrainModel(LanguageDetectorFactory factory)
    {
        var sampleStream = CreateSampleStream();

        TrainingParameters @params = new();
        @params.Put(TrainingParameters.ITERATIONS_PARAM, 100);
        @params.Put(TrainingParameters.CUTOFF_PARAM, 5);
        @params.Put("DataIndexer", "TwoPass");
        @params.Put(TrainingParameters.ALGORITHM_PARAM, "NAIVEBAYES");

        return LanguageDetectorME.Train(sampleStream, @params, factory);
    }

    public static LanguageDetectorSampleStream CreateSampleStream()
    {
        // NOpenNLP: upstream uses opennlp.tools.formats.ResourceAsStreamFactory,
        // which is not ported; the ResourceAsStreamFactory in Support does the
        // same job over an embedded resource.
        ResourceAsStreamFactory streamFactory = new("/opennlp/tools/doccat/DoccatSample.txt");

        PlainTextByLineStream lineStream = new(streamFactory, Encoding.UTF8);

        return new LanguageDetectorSampleStream(lineStream);
    }
}
