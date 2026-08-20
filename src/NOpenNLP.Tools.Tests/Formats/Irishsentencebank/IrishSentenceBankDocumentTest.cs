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

using System.Collections.Generic;
using System.IO;
using NOpenNLP.Tools.Support;
using NOpenNLP.Tools.Tokenize;
using NOpenNLP.Tools.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Formats.Irishsentencebank;

public class IrishSentenceBankDocumentTest
{
    [Test]
    public void TestParsingSimpleDoc()
    {
        using (Stream irishSBXmlIn =
            TestResources.OpenResource("/opennlp/tools/formats/irishsentencebank/irishsentencebank-sample.xml"))
        {
            IrishSentenceBankDocument doc = IrishSentenceBankDocument.Parse(irishSBXmlIn);

            IList<IrishSentenceBankDocument.IrishSentenceBankSentence> sents = doc.Sentences;

            ClassicAssert.AreEqual(2, sents.Count);

            IrishSentenceBankDocument.IrishSentenceBankSentence sent1 = sents[0];
            IrishSentenceBankDocument.IrishSentenceBankSentence sent2 = sents[1];

            ClassicAssert.AreEqual("A Dhia, tá mé ag iompar clainne!", sent1.Original);

            IrishSentenceBankDocument.IrishSentenceBankFlex[]? flex = sent1.Flex;
            ClassicAssert.AreEqual(7, flex!.Length);
            ClassicAssert.AreEqual("A", flex[0].Surface);
            CollectionAssert.AreEqual(new string[] { "a" }, flex[0].Flex);

            IrishSentenceBankDocument.IrishSentenceBankFlex[]? flex2 = sent2.Flex;
            ClassicAssert.AreEqual("ón", flex2![4].Surface);
            CollectionAssert.AreEqual(new string[] { "ó", "an" }, flex2[4].Flex);

            ClassicAssert.AreEqual("Excuse me, are you from the stone age?", sent2.Translation);

            TokenSample ts = sent1.GetTokenSample();
            Span[] spans = ts.TokenSpans;
            ClassicAssert.AreEqual(9, spans.Length);
            ClassicAssert.AreEqual(24, spans[7].Start);
            ClassicAssert.AreEqual(31, spans[7].End);
            ClassicAssert.AreEqual("clainne", ts.Text.Substring(spans[7].Start, spans[7].End - spans[7].Start));
        }
    }
}
