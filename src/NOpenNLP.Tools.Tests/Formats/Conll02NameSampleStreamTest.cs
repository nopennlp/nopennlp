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
using NOpenNLP.Tools.Namefind;
using NOpenNLP.Tools.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Formats;

/// <summary>
/// Note:
/// Sample training data must be UTF-8 encoded and uncompressed!
/// </summary>
public class Conll02NameSampleStreamTest
{
    /// <exception cref="IOException">if the stream cannot be created</exception>
    private static IObjectStream<NameSample?> OpenData(Conll02NameSampleStream.Language lang, string name)
    {
        IInputStreamFactory @in = new ResourceAsStreamFactory("/opennlp/tools/formats/" + name);

        return new Conll02NameSampleStream(lang, @in, Conll02NameSampleStream.GeneratePersonEntities);
    }

    [Test]
    public void TestParsingSpanishSample()
    {
        IObjectStream<NameSample?> sampleStream =
            OpenData(Conll02NameSampleStream.Language.SPA, "conll2002-es.sample");

        NameSample? personName = sampleStream.Read();

        ClassicAssert.NotNull(personName);

        ClassicAssert.AreEqual(5, personName!.Sentence.Length);
        ClassicAssert.AreEqual(1, personName.Names.Length);
        ClassicAssert.AreEqual(true, personName.IsClearAdaptiveDataSet);

        Span nameSpan = personName.Names[0];
        ClassicAssert.AreEqual(0, nameSpan.Start);
        ClassicAssert.AreEqual(4, nameSpan.End);
        ClassicAssert.AreEqual(true, personName.IsClearAdaptiveDataSet);

        ClassicAssert.AreEqual(0, sampleStream.Read()!.Names.Length);

        ClassicAssert.Null(sampleStream.Read());
    }

    [Test]
    public void TestParsingDutchSample()
    {
        IObjectStream<NameSample?> sampleStream =
            OpenData(Conll02NameSampleStream.Language.NLD, "conll2002-nl.sample");

        NameSample? personName = sampleStream.Read();

        ClassicAssert.AreEqual(0, personName!.Names.Length);
        ClassicAssert.IsTrue(personName.IsClearAdaptiveDataSet);

        personName = sampleStream.Read();

        ClassicAssert.IsFalse(personName!.IsClearAdaptiveDataSet);

        ClassicAssert.Null(sampleStream.Read());
    }

    [Test]
    public void TestReset()
    {
        IObjectStream<NameSample?> sampleStream =
            OpenData(Conll02NameSampleStream.Language.NLD, "conll2002-nl.sample");

        NameSample? sample = sampleStream.Read();

        sampleStream.Reset();

        ClassicAssert.AreEqual(sample, sampleStream.Read());
    }
}
