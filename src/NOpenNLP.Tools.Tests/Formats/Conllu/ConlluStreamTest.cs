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

using NOpenNLP.Tools.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Formats.Conllu;

public class ConlluStreamTest
{
    [Test]
    public void TestParseTwoSentences()
    {
        IInputStreamFactory streamFactory =
            new ResourceAsStreamFactory("/opennlp/tools/formats/conllu/de-ud-train-sample.conllu");

        using var stream = new ConlluStream(streamFactory);

        var sent1 = stream.Read();

        ClassicAssert.AreEqual("train-s21", sent1!.SentenceIdComment);
        ClassicAssert.AreEqual("Fachlich kompetent, sehr gute Beratung und ein freundliches Team.",
            sent1.TextComment);
        ClassicAssert.AreEqual(11, sent1.WordLines.Count);

        var sent2 = stream.Read();

        ClassicAssert.AreEqual("train-s22", sent2!.SentenceIdComment);
        ClassicAssert.AreEqual(
            "Beiden Zahnärzten verdanke ich einen neuen Biss und dadurch endlich keine Rückenschmerzen mehr.",
            sent2.TextComment);
        ClassicAssert.AreEqual(14, sent2.WordLines.Count);

        ClassicAssert.IsNull(stream.Read(), "Stream must be exhausted");
    }
}
