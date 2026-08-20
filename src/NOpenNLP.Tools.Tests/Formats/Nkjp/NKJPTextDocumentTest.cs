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
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Formats.Nkjp;

public class NKJPTextDocumentTest
{
    [Test]
    public void TestParsingSimpleDoc()
    {
        using (Stream nkjpTextXmlIn = TestResources.OpenResource("/opennlp/tools/formats/nkjp/text_structure.xml"))
        {
            NKJPTextDocument doc = NKJPTextDocument.Parse(nkjpTextXmlIn);

            ClassicAssert.AreEqual(1, doc.Divtypes.Count);
            ClassicAssert.AreEqual("article", doc.Divtypes["div-1"]);

            ClassicAssert.AreEqual(1, doc.Texts.Count);
            ClassicAssert.AreEqual(1, doc.Texts["text-1"].Count);
            ClassicAssert.AreEqual(2, doc.Texts["text-1"]["div-1"].Count);

            string exp = "To krótki tekst w formacie NKJP. Zawiera dwa zdania.";
            ClassicAssert.AreEqual(exp, doc.Texts["text-1"]["div-1"]["p-1"]);
        }
    }

    [Test]
    public void TestGetParagraphs()
    {
        using (Stream nkjpTextXmlIn = TestResources.OpenResource("/opennlp/tools/formats/nkjp/text_structure.xml"))
        {
            NKJPTextDocument doc = NKJPTextDocument.Parse(nkjpTextXmlIn);
            IDictionary<string, string> paras = doc.GetParagraphs();
            ClassicAssert.AreEqual("To krótkie zdanie w drugim akapicie.", paras["ab-1"]);
        }
    }
}
