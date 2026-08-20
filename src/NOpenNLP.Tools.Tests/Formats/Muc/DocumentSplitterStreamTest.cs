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

namespace NOpenNLP.Tools.Formats.Muc;

public class DocumentSplitterStreamTest
{
    [Test]
    public void TestSplitTwoDocuments()
    {
        StringBuilder docsString = new StringBuilder();

        for (int i = 0; i < 2; i++)
        {
            docsString.Append("<DOC>\n");
            docsString.Append("test document #").Append(i).Append("\n");
            docsString.Append("</DOC>\n");
        }

        using (IObjectStream<string?> docs = new DocumentSplitterStream(
            ObjectStreamUtils.CreateObjectStream(docsString.ToString())))
        {
            string? doc1 = docs.Read();
            ClassicAssert.AreEqual(docsString.Length / 2, doc1!.Length + 1);
            ClassicAssert.IsTrue(doc1.Contains("#0"));

            string? doc2 = docs.Read();
            ClassicAssert.AreEqual(docsString.Length / 2, doc2!.Length + 1);
            ClassicAssert.IsTrue(doc2.Contains("#1"));

            ClassicAssert.IsNull(docs.Read());
            ClassicAssert.IsNull(docs.Read());
        }
    }
}
