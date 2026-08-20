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

using NOpenNLP.Tools.Formats;
using NOpenNLP.Tools.Sentdetect;
using NOpenNLP.Tools.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Formats.Conllu;

public class ConlluSentenceSampleStreamTest
{
    [Test]
    public void TestParseTwoSentences()
    {
        IInputStreamFactory streamFactory =
            new ResourceAsStreamFactory("/opennlp/tools/formats/conllu/de-ud-train-sample.conllu");

        using (IObjectStream<SentenceSample?> stream =
            new ConlluSentenceSampleStream(new ConlluStream(streamFactory), 1))
        {
            SentenceSample? sample1 = stream.Read();

            ClassicAssert.AreEqual("Fachlich kompetent, sehr gute Beratung und ein freundliches Team.",
                sample1!.Document);

            ClassicAssert.AreEqual(new Span(0, 65), sample1.GetSentences()[0]);

            SentenceSample? sample2 = stream.Read();

            ClassicAssert.AreEqual("Beiden Zahnärzten verdanke ich einen neuen Biss und dadurch " +
                "endlich keine Rückenschmerzen mehr.", sample2!.Document);
            ClassicAssert.AreEqual(new Span(0, 95), sample2.GetSentences()[0]);

            ClassicAssert.IsNull(stream.Read(), "Stream must be exhausted");
        }

        using (IObjectStream<SentenceSample?> stream =
            new ConlluSentenceSampleStream(new ConlluStream(streamFactory), 3))
        {
            SentenceSample? sample = stream.Read();

            ClassicAssert.AreEqual("Fachlich kompetent, sehr gute Beratung und ein freundliches Team."
                + " Beiden Zahnärzten verdanke ich einen neuen Biss und dadurch endlich keine "
                + "Rückenschmerzen mehr.",
                sample!.Document);

            ClassicAssert.IsNull(stream.Read(), "Stream must be exhausted");
        }
    }
}
