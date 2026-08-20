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
using System.IO;
using System.Text;
using NOpenNLP.Tools.Formats;
using NOpenNLP.Tools.Support;
using NOpenNLP.Tools.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using static NOpenNLP.Tools.Postag.DummyPOSTaggerFactory;

namespace NOpenNLP.Tools.Postag;

/// <summary>
/// Tests for the <see cref="POSTaggerFactory"/> class.
/// </summary>
public class POSTaggerFactoryTest
{
    private static IObjectStream<POSSample?> CreateSampleStream()
    {
        IInputStreamFactory @in = new ResourceAsStreamFactory(
            "/opennlp/tools/postag/AnnotatedSentences.txt");

        return new WordTagSampleStream(new PlainTextByLineStream(@in, Encoding.UTF8));
    }

    private static POSModel TrainPOSModel(POSTaggerFactory factory) =>
        POSTaggerME.Train("eng", CreateSampleStream(), TrainingParameters.DefaultParams(), factory);

    [Test]
    public void TestPOSTaggerWithCustomFactory()
    {
        using Stream dictIn = TestResources.OpenResource(
            "/opennlp/tools/postag/TagDictionaryCaseSensitive.xml");
        DummyPOSDictionary posDict = new DummyPOSDictionary(POSDictionary.Create(dictIn));

        POSModel posModel = TrainPOSModel(new DummyPOSTaggerFactory(posDict));

        POSTaggerFactory factory = posModel.Factory;
        ClassicAssert.IsTrue(factory.TagDictionary is DummyPOSDictionary);
        ClassicAssert.IsTrue(factory.POSContextGenerator is DummyPOSContextGenerator);
        ClassicAssert.IsTrue(factory.SequenceValidator is DummyPOSSequenceValidator);

        MemoryStream @out = new MemoryStream();
        posModel.Serialize(@out);
        MemoryStream @in = new MemoryStream(@out.ToArray());

        POSModel fromSerialized = new POSModel(@in);

        factory = fromSerialized.Factory;
        ClassicAssert.IsTrue(factory.TagDictionary is DummyPOSDictionary);
        ClassicAssert.IsTrue(factory.POSContextGenerator is DummyPOSContextGenerator);
        ClassicAssert.IsTrue(factory.SequenceValidator is DummyPOSSequenceValidator);
    }

    [Test]
    public void TestPOSTaggerWithDefaultFactory()
    {
        using Stream dictIn = TestResources.OpenResource(
            "/opennlp/tools/postag/TagDictionaryCaseSensitive.xml");
        POSDictionary posDict = POSDictionary.Create(dictIn);
        POSModel posModel = TrainPOSModel(new POSTaggerFactory(null, null, posDict));

        POSTaggerFactory factory = posModel.Factory;
        ClassicAssert.IsTrue(factory.TagDictionary is POSDictionary);
        ClassicAssert.IsTrue(factory.POSContextGenerator != null);
        ClassicAssert.IsTrue(factory.SequenceValidator is DefaultPOSSequenceValidator);

        MemoryStream @out = new MemoryStream();
        posModel.Serialize(@out);
        MemoryStream @in = new MemoryStream(@out.ToArray());

        POSModel fromSerialized = new POSModel(@in);

        factory = fromSerialized.Factory;
        ClassicAssert.IsTrue(factory.TagDictionary is POSDictionary);
        ClassicAssert.IsTrue(factory.POSContextGenerator != null);
        ClassicAssert.IsTrue(factory.SequenceValidator is DefaultPOSSequenceValidator);
    }

    [Test]
    public void TestCreateWithInvalidName() =>
        Assert.Throws<InvalidFormatException>((Action)(() => BaseToolFactory.Create("X", null!)));

    [Test]
    public void TestCreateWithInvalidName2() =>
        Assert.Throws<InvalidFormatException>((Action)(() => POSTaggerFactory.Create("X", null, null)));

    [Test]
    public void TestCreateWithHierarchy()
    {
        // NOpenNLP: upstream passes Object.class.getCanonicalName(); the .NET
        // counterpart is the assembly-qualified name the ExtensionLoader resolves.
        Assert.Throws<InvalidFormatException>((Action)(() =>
            BaseToolFactory.Create(typeof(object).AssemblyQualifiedName!, null!)));
    }

    [Test]
    public void TestCreateWithHierarchy2()
    {
        Assert.Throws<InvalidFormatException>((Action)(() =>
            POSTaggerFactory.Create(GetType().AssemblyQualifiedName!, null, null)));
    }
}
