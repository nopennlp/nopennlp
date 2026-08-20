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
using NOpenNLP.Tools.Namefind;
using NOpenNLP.Tools.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Formats;

/// <summary>
/// Test for the <see cref="Conll03NameSampleStream"/> class.
/// </summary>
public class Conll03NameSampleStreamTest
{
    private const string ENGLISH_SAMPLE = "conll2003-en.sample";
    private const string GERMAN_SAMPLE = "conll2003-de.sample";

    /// <exception cref="IOException">if the stream cannot be created</exception>
    private static IObjectStream<NameSample?> OpenData(Conll03NameSampleStream.Language lang, string name)
    {
        IInputStreamFactory @in = new ResourceAsStreamFactory("/opennlp/tools/formats/" + name);

        return new Conll03NameSampleStream(lang, @in, Conll02NameSampleStream.GeneratePersonEntities);
    }

    [Test]
    public void TestParsingEnglishSample()
    {
        IObjectStream<NameSample?> sampleStream =
            OpenData(Conll03NameSampleStream.Language.EN, ENGLISH_SAMPLE);

        NameSample? personName = sampleStream.Read();
        ClassicAssert.NotNull(personName);

        ClassicAssert.AreEqual(9, personName!.Sentence.Length);
        ClassicAssert.AreEqual(0, personName.Names.Length);
        ClassicAssert.AreEqual(true, personName.IsClearAdaptiveDataSet);

        personName = sampleStream.Read();

        ClassicAssert.NotNull(personName);

        ClassicAssert.AreEqual(2, personName!.Sentence.Length);
        ClassicAssert.AreEqual(1, personName.Names.Length);
        ClassicAssert.AreEqual(false, personName.IsClearAdaptiveDataSet);

        Span nameSpan = personName.Names[0];
        ClassicAssert.AreEqual(0, nameSpan.Start);
        ClassicAssert.AreEqual(2, nameSpan.End);

        ClassicAssert.Null(sampleStream.Read());
    }

    [Test]
    public void TestParsingEnglishSampleWithGermanAsLanguage()
    {
        IObjectStream<NameSample?> sampleStream =
            OpenData(Conll03NameSampleStream.Language.DE, ENGLISH_SAMPLE);
        Assert.Throws<IOException>((Action)(() => sampleStream.Read()));
    }

    [Test]
    public void TestParsingGermanSampleWithEnglishAsLanguage()
    {
        IObjectStream<NameSample?> sampleStream =
            OpenData(Conll03NameSampleStream.Language.EN, GERMAN_SAMPLE);
        Assert.Throws<IOException>((Action)(() => sampleStream.Read()));
    }

    [Test]
    public void TestParsingGermanSample()
    {
        IObjectStream<NameSample?> sampleStream =
            OpenData(Conll03NameSampleStream.Language.DE, GERMAN_SAMPLE);

        NameSample? personName = sampleStream.Read();
        ClassicAssert.NotNull(personName);

        ClassicAssert.AreEqual(5, personName!.Sentence.Length);
        ClassicAssert.AreEqual(0, personName.Names.Length);
        ClassicAssert.AreEqual(true, personName.IsClearAdaptiveDataSet);
    }

    [Test]
    public void TestReset()
    {
        IObjectStream<NameSample?> sampleStream =
            OpenData(Conll03NameSampleStream.Language.DE, GERMAN_SAMPLE);

        NameSample? sample = sampleStream.Read();

        sampleStream.Reset();

        ClassicAssert.AreEqual(sample, sampleStream.Read());
    }
}
