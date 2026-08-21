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

using NOpenNLP.Tools.Sentdetect.Lang;
using NOpenNLP.Tools.Formats;
using NOpenNLP.Tools.Support;
using NOpenNLP.Tools.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System.IO;
using System.Text;
using static NOpenNLP.Tools.Sentdetect.DummySentenceDetectorFactory;

namespace NOpenNLP.Tools.Sentdetect;

/// <summary>
/// Tests for the <see cref="SentenceDetectorME"/> class.
/// </summary>
public class SentenceDetectorFactoryTest
{
    private static IObjectStream<SentenceSample?> CreateSampleStream()
    {
        IInputStreamFactory @in = new ResourceAsStreamFactory(
            "/opennlp/tools/sentdetect/Sentences.txt");

        return new SentenceSampleStream(new PlainTextByLineStream(
            @in, Encoding.UTF8));
    }

    private static SentenceModel Train(SentenceDetectorFactory factory) =>
        SentenceDetectorME.Train("eng", CreateSampleStream(), factory,
            TrainingParameters.DefaultParams());

    private static NOpenNLP.Tools.Dictionary.Dictionary LoadAbbDictionary()
    {
        using var @in = TestResources.OpenResource("/opennlp/tools/sentdetect/abb.xml");

        return new NOpenNLP.Tools.Dictionary.Dictionary(@in);
    }

    [Test]
    public void TestDefault()
    {
        var dic = LoadAbbDictionary();

        char[] eos = ['.', '?'];
        var sdModel = Train(new SentenceDetectorFactory("eng", true, dic, eos));

        SentenceDetectorFactory? factory = sdModel.Factory;
        ClassicAssert.IsTrue(factory!.GetSDContextGenerator() is DefaultSDContextGenerator);
        ClassicAssert.IsTrue(factory.EndOfSentenceScanner is DefaultEndOfSentenceScanner);
        CollectionAssert.AreEqual(eos, factory.EOSCharacters);

        var @out = new MemoryStream();
        sdModel.Serialize(@out);
        var @in = new MemoryStream(@out.ToArray());

        var fromSerialized = new SentenceModel(@in);

        factory = fromSerialized.Factory;
        ClassicAssert.IsTrue(factory!.GetSDContextGenerator() is DefaultSDContextGenerator);
        ClassicAssert.IsTrue(factory.EndOfSentenceScanner is DefaultEndOfSentenceScanner);
        CollectionAssert.AreEqual(eos, factory.EOSCharacters);
    }

    [Test]
    public void TestNullDict()
    {
        NOpenNLP.Tools.Dictionary.Dictionary? dic = null;

        char[] eos = ['.', '?'];
        var sdModel = Train(new SentenceDetectorFactory("eng", true, dic, eos));

        SentenceDetectorFactory? factory = sdModel.Factory;
        ClassicAssert.IsNull(factory!.AbbreviationDictionary);
        ClassicAssert.IsTrue(factory.GetSDContextGenerator() is DefaultSDContextGenerator);
        ClassicAssert.IsTrue(factory.EndOfSentenceScanner is DefaultEndOfSentenceScanner);
        CollectionAssert.AreEqual(eos, factory.EOSCharacters);

        var @out = new MemoryStream();
        sdModel.Serialize(@out);
        var @in = new MemoryStream(@out.ToArray());

        var fromSerialized = new SentenceModel(@in);

        factory = fromSerialized.Factory;
        ClassicAssert.IsNull(factory!.AbbreviationDictionary);
        ClassicAssert.IsTrue(factory.GetSDContextGenerator() is DefaultSDContextGenerator);
        ClassicAssert.IsTrue(factory.EndOfSentenceScanner is DefaultEndOfSentenceScanner);
        CollectionAssert.AreEqual(eos, factory.EOSCharacters);
    }

    [Test]
    public void TestDefaultEOS()
    {
        NOpenNLP.Tools.Dictionary.Dictionary? dic = null;

        char[]? eos = null;
        var sdModel = Train(new SentenceDetectorFactory("eng", true, dic, eos));

        SentenceDetectorFactory? factory = sdModel.Factory;
        ClassicAssert.IsNull(factory!.AbbreviationDictionary);
        ClassicAssert.IsTrue(factory.GetSDContextGenerator() is DefaultSDContextGenerator);
        ClassicAssert.IsTrue(factory.EndOfSentenceScanner is DefaultEndOfSentenceScanner);
        CollectionAssert.AreEqual(Factory.defaultEosCharacters, factory.EOSCharacters);

        var @out = new MemoryStream();
        sdModel.Serialize(@out);
        var @in = new MemoryStream(@out.ToArray());

        var fromSerialized = new SentenceModel(@in);

        factory = fromSerialized.Factory;
        ClassicAssert.IsNull(factory!.AbbreviationDictionary);
        ClassicAssert.IsTrue(factory.GetSDContextGenerator() is DefaultSDContextGenerator);
        ClassicAssert.IsTrue(factory.EndOfSentenceScanner is DefaultEndOfSentenceScanner);
        CollectionAssert.AreEqual(Factory.defaultEosCharacters, factory.EOSCharacters);
    }

    [Test]
    public void TestDummyFactory()
    {
        var dic = LoadAbbDictionary();

        char[] eos = ['.', '?'];
        var sdModel = Train(new DummySentenceDetectorFactory("eng", true, dic, eos));

        SentenceDetectorFactory? factory = sdModel.Factory;
        ClassicAssert.IsTrue(factory!.AbbreviationDictionary is DummyDictionary);
        ClassicAssert.IsTrue(factory.GetSDContextGenerator() is DummySDContextGenerator);
        ClassicAssert.IsTrue(factory.EndOfSentenceScanner is DummyEOSScanner);
        CollectionAssert.AreEqual(eos, factory.EOSCharacters);

        var @out = new MemoryStream();
        sdModel.Serialize(@out);
        var @in = new MemoryStream(@out.ToArray());

        var fromSerialized = new SentenceModel(@in);

        factory = fromSerialized.Factory;
        ClassicAssert.IsTrue(factory!.AbbreviationDictionary is DummyDictionary);
        ClassicAssert.IsTrue(factory.GetSDContextGenerator() is DummySDContextGenerator);
        ClassicAssert.IsTrue(factory.EndOfSentenceScanner is DummyEOSScanner);
        CollectionAssert.AreEqual(eos, factory.EOSCharacters);

        ClassicAssert.AreEqual(factory.AbbreviationDictionary, sdModel.Abbreviations);
        CollectionAssert.AreEqual(factory.EOSCharacters, sdModel.EosCharacters);
    }

    [Test]
    public void TestCreateDummyFactory()
    {
        var dic = LoadAbbDictionary();
        char[] eos = ['.', '?'];

        var factory = SentenceDetectorFactory.Create(
            typeof(DummySentenceDetectorFactory).FullName, "spa", false,
            dic, eos);

        ClassicAssert.IsTrue(factory.AbbreviationDictionary is DummyDictionary);
        ClassicAssert.IsTrue(factory.GetSDContextGenerator() is DummySDContextGenerator);
        ClassicAssert.IsTrue(factory.EndOfSentenceScanner is DummyEOSScanner);
        CollectionAssert.AreEqual(eos, factory.EOSCharacters);
    }
}
