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
public class EvalitaNameSampleStreamTest
{
    /// <exception cref="IOException">if the stream cannot be created</exception>
    private static IObjectStream<NameSample?> OpenData(EvalitaNameSampleStream.Language lang, string name)
    {
        IInputStreamFactory @in = new ResourceAsStreamFactory($"/opennlp/tools/formats/{name}");

        return new EvalitaNameSampleStream(lang, @in, EvalitaNameSampleStream.GeneratePersonEntities);
    }

    [Test]
    public void TestParsingItalianSample()
    {
        var sampleStream = OpenData(EvalitaNameSampleStream.Language.IT, "evalita-ner-it.sample");

        var personName = sampleStream.Read();

        ClassicAssert.NotNull(personName);

        ClassicAssert.AreEqual(11, personName!.Sentence.Length);
        ClassicAssert.AreEqual(1, personName.Names.Length);
        ClassicAssert.AreEqual(true, personName.IsClearAdaptiveDataSet);

        var nameSpan = personName.Names[0];
        ClassicAssert.AreEqual(8, nameSpan.Start);
        ClassicAssert.AreEqual(10, nameSpan.End);
        ClassicAssert.AreEqual(true, personName.IsClearAdaptiveDataSet);

        ClassicAssert.AreEqual(0, sampleStream.Read()!.Names.Length);

        ClassicAssert.Null(sampleStream.Read());
    }

    [Test]
    public void TestReset()
    {
        var sampleStream = OpenData(EvalitaNameSampleStream.Language.IT, "evalita-ner-it.sample");
        var sample = sampleStream.Read();
        sampleStream.Reset();
        ClassicAssert.AreEqual(sample, sampleStream.Read());
    }
}
