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

namespace NOpenNLP.Tools.Doccat;

/// <summary>
/// Tests for the <see cref="DoccatFactory"/> class.
/// </summary>
public class DoccatFactoryTest
{
    private static IObjectStream<DocumentSample?> CreateSampleStream()
    {
        // NOpenNLP: upstream uses opennlp.tools.formats.ResourceAsStreamFactory,
        // which is not ported; the ResourceAsStreamFactory in Support does the
        // same job over an embedded resource.
        IInputStreamFactory isf = new ResourceAsStreamFactory("/opennlp/tools/doccat/DoccatSample.txt");

        return new DocumentSampleStream(new PlainTextByLineStream(isf, Encoding.UTF8));
    }

    private static DoccatModel Train() =>
        DocumentCategorizerME.Train("x-unspecified", CreateSampleStream(),
            TrainingParameters.DefaultParams(), new DoccatFactory());

    private static DoccatModel Train(DoccatFactory factory) =>
        DocumentCategorizerME.Train("x-unspecified", CreateSampleStream(),
            TrainingParameters.DefaultParams(), factory);

    [Test]
    public void TestDefault()
    {
        var model = Train();

        ClassicAssert.NotNull(model);

        var @out = new MemoryStream();
        model.Serialize(@out);
        var @in = new MemoryStream(@out.ToArray());

        DoccatModel fromSerialized = new(@in);

        var factory = fromSerialized.Factory;

        ClassicAssert.NotNull(factory);

        ClassicAssert.AreEqual(1, factory.FeatureGenerators.Length);
        ClassicAssert.AreEqual(typeof(BagOfWordsFeatureGenerator),
            factory.FeatureGenerators[0].GetType());
    }

    [Test]
    public void TestCustom()
    {
        IFeatureGenerator[] featureGenerators =
        [
            new BagOfWordsFeatureGenerator(),
            new NGramFeatureGenerator(),
            new NGramFeatureGenerator(2, 3)
        ];

        DoccatFactory factory = new(featureGenerators);

        var model = Train(factory);

        ClassicAssert.NotNull(model);

        var @out = new MemoryStream();
        model.Serialize(@out);
        var @in = new MemoryStream(@out.ToArray());

        DoccatModel fromSerialized = new(@in);

        factory = fromSerialized.Factory;

        ClassicAssert.NotNull(factory);

        ClassicAssert.AreEqual(3, factory.FeatureGenerators.Length);
        ClassicAssert.AreEqual(typeof(BagOfWordsFeatureGenerator),
            factory.FeatureGenerators[0].GetType());
        ClassicAssert.AreEqual(typeof(NGramFeatureGenerator),
            factory.FeatureGenerators[1].GetType());
        ClassicAssert.AreEqual(typeof(NGramFeatureGenerator), factory.FeatureGenerators[2].GetType());
    }
}
