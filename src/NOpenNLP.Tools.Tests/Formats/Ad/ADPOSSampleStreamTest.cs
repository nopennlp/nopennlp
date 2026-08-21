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

using System.Text;
using NOpenNLP.Tools.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Formats.Ad;

public class ADPOSSampleStreamTest
{
    [Test]
    public void TestSimple()
    {
        // add one sentence with expandME = includeFeats = false
        using var stream = new ADPOSSampleStream(
            new PlainTextByLineStream(
                new ResourceAsStreamFactory("/opennlp/tools/formats/ad.sample"),
                Encoding.UTF8), false, false);

        var sample = stream.Read();

        ClassicAssert.AreEqual(23, sample!.Sentence.Length);

        ClassicAssert.AreEqual("Inicia", sample.Sentence[0]);
        ClassicAssert.AreEqual("v-fin", sample.Tags[0]);

        ClassicAssert.AreEqual("em", sample.Sentence[1]);
        ClassicAssert.AreEqual("prp", sample.Tags[1]);

        ClassicAssert.AreEqual("o", sample.Sentence[2]);
        ClassicAssert.AreEqual("art", sample.Tags[2]);

        ClassicAssert.AreEqual("Porto_Poesia", sample.Sentence[9]);
        ClassicAssert.AreEqual("prop", sample.Tags[9]);
    }

    [Test]
    public void TestExpandME()
    {
        // add one sentence with expandME = true
        using var stream = new ADPOSSampleStream(
            new PlainTextByLineStream(
                new ResourceAsStreamFactory("/opennlp/tools/formats/ad.sample"),
                Encoding.UTF8), true, false);

        var sample = stream.Read();

        ClassicAssert.AreEqual(27, sample!.Sentence.Length);

        ClassicAssert.AreEqual("Inicia", sample.Sentence[0]);
        ClassicAssert.AreEqual("v-fin", sample.Tags[0]);

        ClassicAssert.AreEqual("em", sample.Sentence[1]);
        ClassicAssert.AreEqual("prp", sample.Tags[1]);

        ClassicAssert.AreEqual("o", sample.Sentence[2]);
        ClassicAssert.AreEqual("art", sample.Tags[2]);

        ClassicAssert.AreEqual("Porto", sample.Sentence[9]);
        ClassicAssert.AreEqual("B-prop", sample.Tags[9]);

        ClassicAssert.AreEqual("Poesia", sample.Sentence[10]);
        ClassicAssert.AreEqual("I-prop", sample.Tags[10]);
    }

    [Test]
    public void TestIncludeFeats()
    {
        // add one sentence with includeFeats = true
        using var stream = new ADPOSSampleStream(
            new PlainTextByLineStream(
                new ResourceAsStreamFactory("/opennlp/tools/formats/ad.sample"),
                Encoding.UTF8), false, true);

        var sample = stream.Read();

        ClassicAssert.AreEqual(23, sample!.Sentence.Length);

        ClassicAssert.AreEqual("Inicia", sample.Sentence[0]);
        ClassicAssert.AreEqual("v-fin=PR=3S=IND=VFIN", sample.Tags[0]);

        ClassicAssert.AreEqual("em", sample.Sentence[1]);
        ClassicAssert.AreEqual("prp", sample.Tags[1]);

        ClassicAssert.AreEqual("o", sample.Sentence[2]);
        ClassicAssert.AreEqual("art=DET=M=S", sample.Tags[2]);

        ClassicAssert.AreEqual("Porto_Poesia", sample.Sentence[9]);
        ClassicAssert.AreEqual("prop=M=S", sample.Tags[9]);
    }
}
