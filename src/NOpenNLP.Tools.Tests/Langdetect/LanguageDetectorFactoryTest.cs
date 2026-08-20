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
using System.Text;
using NOpenNLP.Tools.Formats;
using NOpenNLP.Tools.Support;
using NOpenNLP.Tools.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Langdetect;

public class LanguageDetectorFactoryTest
{
    private static LanguageDetectorModel model = null!;

    [OneTimeSetUp]
    public static void Train()
    {
        // NOpenNLP: upstream uses opennlp.tools.formats.ResourceAsStreamFactory,
        // which is not ported; the ResourceAsStreamFactory in Support does the
        // same job over an embedded resource.
        ResourceAsStreamFactory streamFactory = new("/opennlp/tools/doccat/DoccatSample.txt");

        PlainTextByLineStream lineStream = new(streamFactory, Encoding.UTF8);

        LanguageDetectorSampleStream sampleStream = new(lineStream);

        TrainingParameters @params = new();
        @params.Put(TrainingParameters.ITERATIONS_PARAM, "100");
        @params.Put(TrainingParameters.CUTOFF_PARAM, "5");
        @params.Put(TrainingParameters.ALGORITHM_PARAM, "NAIVEBAYES");

        model = LanguageDetectorME.Train(sampleStream, @params, new DummyFactory());
    }

    [Test]
    public void TestCorrectFactory()
    {
        byte[] serialized = LanguageDetectorMETest.SerializeModel(model);

        LanguageDetectorModel myModel = new(new MemoryStream(serialized));

        ClassicAssert.IsTrue(myModel.Factory is DummyFactory);
    }

    [Test]
    public void TestDummyFactory()
    {
        byte[] serialized = LanguageDetectorMETest.SerializeModel(model);

        LanguageDetectorModel myModel = new(new MemoryStream(serialized));

        ClassicAssert.IsTrue(myModel.Factory is DummyFactory);
    }

    [Test]
    public void TestDummyFactoryContextGenerator()
    {
        ILanguageDetectorContextGenerator cg = model.Factory.GetContextGenerator();
        string[] context = cg.GetContext(
            "a dummy text phrase to test if the context generator works!!!!!!!!!!!!");

        ISet<string> set = new JCG.HashSet<string>(context);

        ClassicAssert.IsTrue(set.Contains("!!!!!")); // default normalizer would remove the repeated !
        ClassicAssert.IsTrue(set.Contains("a dum"));
        ClassicAssert.IsTrue(set.Contains("tg=[THE,CONTEXT,GENERATOR]"));
    }
}
