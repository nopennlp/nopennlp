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

namespace NOpenNLP.Tools.Formats;

public class ConllXPOSSampleStreamTest
{
    [Test]
    public void TestParsingSample()
    {
        IInputStreamFactory @in = new ResourceAsStreamFactory("/opennlp/tools/formats/conllx.sample");

        using var sampleStream = new ConllXPOSSampleStream(@in, Encoding.UTF8);

        var a = sampleStream.Read();

        var aSentence = a!.Sentence;
        var aTags = a.Tags;

        ClassicAssert.AreEqual(22, aSentence.Length);
        ClassicAssert.AreEqual(22, aTags.Length);

        ClassicAssert.AreEqual("To", aSentence[0]);
        ClassicAssert.AreEqual("AC", aTags[0]);

        ClassicAssert.AreEqual("kendte", aSentence[1]);
        ClassicAssert.AreEqual("AN", aTags[1]);

        ClassicAssert.AreEqual("russiske", aSentence[2]);
        ClassicAssert.AreEqual("AN", aTags[2]);

        ClassicAssert.AreEqual("historikere", aSentence[3]);
        ClassicAssert.AreEqual("NC", aTags[3]);

        ClassicAssert.AreEqual("Andronik", aSentence[4]);
        ClassicAssert.AreEqual("NP", aTags[4]);

        ClassicAssert.AreEqual("Andronik", aSentence[5]);
        ClassicAssert.AreEqual("NP", aTags[5]);

        ClassicAssert.AreEqual("og", aSentence[6]);
        ClassicAssert.AreEqual("CC", aTags[6]);

        ClassicAssert.AreEqual("Igor", aSentence[7]);
        ClassicAssert.AreEqual("NP", aTags[7]);

        ClassicAssert.AreEqual("Klamkin", aSentence[8]);
        ClassicAssert.AreEqual("NP", aTags[8]);

        ClassicAssert.AreEqual("tror", aSentence[9]);
        ClassicAssert.AreEqual("VA", aTags[9]);

        ClassicAssert.AreEqual("ikke", aSentence[10]);
        ClassicAssert.AreEqual("RG", aTags[10]);

        ClassicAssert.AreEqual(",", aSentence[11]);
        ClassicAssert.AreEqual("XP", aTags[11]);

        ClassicAssert.AreEqual("at", aSentence[12]);
        ClassicAssert.AreEqual("CS", aTags[12]);

        ClassicAssert.AreEqual("Rusland", aSentence[13]);
        ClassicAssert.AreEqual("NP", aTags[13]);

        ClassicAssert.AreEqual("kan", aSentence[14]);
        ClassicAssert.AreEqual("VA", aTags[14]);

        ClassicAssert.AreEqual("udvikles", aSentence[15]);
        ClassicAssert.AreEqual("VA", aTags[15]);

        ClassicAssert.AreEqual("uden", aSentence[16]);
        ClassicAssert.AreEqual("SP", aTags[16]);

        ClassicAssert.AreEqual("en", aSentence[17]);
        ClassicAssert.AreEqual("PI", aTags[17]);

        ClassicAssert.AreEqual("\"", aSentence[18]);
        ClassicAssert.AreEqual("XP", aTags[18]);

        ClassicAssert.AreEqual("jernnæve", aSentence[19]);
        ClassicAssert.AreEqual("NC", aTags[19]);

        ClassicAssert.AreEqual("\"", aSentence[20]);
        ClassicAssert.AreEqual("XP", aTags[20]);

        ClassicAssert.AreEqual(".", aSentence[21]);
        ClassicAssert.AreEqual("XP", aTags[21]);

        var b = sampleStream.Read();

        var bSentence = b!.Sentence;
        var bTags = b.Tags;

        ClassicAssert.AreEqual(12, bSentence.Length);
        ClassicAssert.AreEqual(12, bTags.Length);

        ClassicAssert.AreEqual("De", bSentence[0]);
        ClassicAssert.AreEqual("PP", bTags[0]);

        ClassicAssert.AreEqual("hævder", bSentence[1]);
        ClassicAssert.AreEqual("VA", bTags[1]);

        ClassicAssert.AreEqual(",", bSentence[2]);
        ClassicAssert.AreEqual("XP", bTags[2]);

        ClassicAssert.AreEqual("at", bSentence[3]);
        ClassicAssert.AreEqual("CS", bTags[3]);

        ClassicAssert.AreEqual("Ruslands", bSentence[4]);
        ClassicAssert.AreEqual("NP", bTags[4]);

        ClassicAssert.AreEqual("vej", bSentence[5]);
        ClassicAssert.AreEqual("NC", bTags[5]);

        ClassicAssert.AreEqual("til", bSentence[6]);
        ClassicAssert.AreEqual("SP", bTags[6]);

        ClassicAssert.AreEqual("demokrati", bSentence[7]);
        ClassicAssert.AreEqual("NC", bTags[7]);

        ClassicAssert.AreEqual("går", bSentence[8]);
        ClassicAssert.AreEqual("VA", bTags[8]);

        ClassicAssert.AreEqual("gennem", bSentence[9]);
        ClassicAssert.AreEqual("SP", bTags[9]);

        ClassicAssert.AreEqual("diktatur", bSentence[10]);
        ClassicAssert.AreEqual("NC", bTags[10]);

        ClassicAssert.AreEqual(".", bSentence[11]);
        ClassicAssert.AreEqual("XP", bTags[11]);

        ClassicAssert.Null(sampleStream.Read());
    }
}
