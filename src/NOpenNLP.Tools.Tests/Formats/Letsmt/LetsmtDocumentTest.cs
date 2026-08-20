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

namespace NOpenNLP.Tools.Formats.Letsmt;

public class LetsmtDocumentTest
{
    [Test]
    public void TestParsingSimpleDoc()
    {
        using (Stream letsmtXmlIn = TestResources.OpenResource("/opennlp/tools/formats/letsmt/letsmt-with-words.xml"))
        {
            LetsmtDocument doc = LetsmtDocument.Parse(letsmtXmlIn);

            IList<LetsmtDocument.LetsmtSentence> sents = doc.Sentences;

            ClassicAssert.AreEqual(2, sents.Count);

            LetsmtDocument.LetsmtSentence sent1 = sents[0];
            ClassicAssert.IsNull(sent1.NonTokenizedText);

            CollectionAssert.AreEqual(new string[]
            {
                "The",
                "Apache",
                "Software",
                "Foundation",
                "uses",
                "various",
                "licenses",
                "to",
                "distribute",
                "software",
                "and",
                "documentation",
                ",",
                "to",
                "accept",
                "regular",
                "contributions",
                "from",
                "individuals",
                "and",
                "corporations",
                ",",
                "and",
                "to",
                "accept",
                "larger",
                "grants",
                "of",
                "existing",
                "software",
                "products",
                "."
            }, sent1.Tokens);

            LetsmtDocument.LetsmtSentence sent2 = sents[1];
            ClassicAssert.IsNull(sent2.NonTokenizedText);

            CollectionAssert.AreEqual(new string[]
            {
                "All",
                "software",
                "produced",
                "by",
                "The",
                "Apache",
                "Software",
                "Foundation",
                "or",
                "any",
                "of",
                "its",
                "projects",
                "or",
                "subjects",
                "is",
                "licensed",
                "according",
                "to",
                "the",
                "terms",
                "of",
                "the",
                "documents",
                "listed",
                "below",
                "."
            }, sent2.Tokens);
        }
    }
}
