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
using NOpenNLP.Tools.Namefind;
using NOpenNLP.Tools.Sentdetect;
using NOpenNLP.Tools.Support;
using NOpenNLP.Tools.Tokenize;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Formats.Brat;

public class BratDocumentParserTest
{
    [Test]
    public void TestParse()
    {
        IDictionary<string, string> typeToClassMap = new Dictionary<string, string>();
        BratAnnotationStreamTest.AddEntityTypes(typeToClassMap);
        AnnotationConfiguration config = new(typeToClassMap);

        Stream txtIn = TestResources.OpenResource(
            "/opennlp/tools/formats/brat/opennlp-1193.txt");

        Stream annIn = TestResources.OpenResource(
            "/opennlp/tools/formats/brat/opennlp-1193.ann");

        BratDocument doc = BratDocument.ParseDocument(config, "opennlp-1193", txtIn, annIn);

        BratDocumentParser parser = new(new NewlineSentenceDetector(),
            WhitespaceTokenizer.INSTANCE);

        IList<NameSample> names = parser.Parse(doc);

        ClassicAssert.AreEqual(3, names.Count);

        NameSample sample1 = names[0];

        ClassicAssert.AreEqual(1, sample1.Names.Length);
        ClassicAssert.AreEqual(0, sample1.Names[0].Start);
        ClassicAssert.AreEqual(2, sample1.Names[0].End);

        NameSample sample2 = names[1];
        ClassicAssert.AreEqual(1, sample2.Names.Length);
        ClassicAssert.AreEqual(0, sample2.Names[0].Start);
        ClassicAssert.AreEqual(1, sample2.Names[0].End);

        NameSample sample3 = names[2];
        ClassicAssert.AreEqual(3, sample3.Names.Length);
        ClassicAssert.AreEqual(0, sample3.Names[0].Start);
        ClassicAssert.AreEqual(1, sample3.Names[0].End);
        ClassicAssert.AreEqual(1, sample3.Names[1].Start);
        ClassicAssert.AreEqual(2, sample3.Names[1].End);
        ClassicAssert.AreEqual(2, sample3.Names[2].Start);
        ClassicAssert.AreEqual(3, sample3.Names[2].End);
    }
}
