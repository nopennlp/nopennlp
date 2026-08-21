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

using System;
using System.Collections.Generic;
using NOpenNLP.Tools.Support;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Formats.Brat;

public class BratDocumentTest
{
    [Test]
    public void TestDocumentWithEntitiesParsing()
    {
        var typeToClassMap = new Dictionary<string, string>();
        BratAnnotationStreamTest.AddEntityTypes(typeToClassMap);
        AnnotationConfiguration config = new(typeToClassMap);

        var txtIn = TestResources.OpenResource("/opennlp/tools/formats/brat/voa-with-entities.txt");

        var annIn = TestResources.OpenResource("/opennlp/tools/formats/brat/voa-with-entities.ann");

        var doc = BratDocument.ParseDocument(config, "voa-with-entities", txtIn, annIn);

        ClassicAssert.AreEqual("voa-with-entities", doc.Id);
        ClassicAssert.IsTrue(doc.Text.StartsWith(" U . S .  President ", StringComparison.Ordinal));
        ClassicAssert.IsTrue(doc.Text.EndsWith("multinational process . \n", StringComparison.Ordinal));

        ClassicAssert.AreEqual(18, doc.Annotations.Count);

        var annotation = doc.GetAnnotation("T2");
        CheckNote(annotation, "Barack Obama", "President Obama was the 44th U.S. president");
        annotation = doc.GetAnnotation("T3");
        CheckNote(annotation, "South Korea", "The capital of South Korea is Seoul");
    }

    private static void CheckNote(BratAnnotation? annotation, string expectedCoveredText, string expectedNote)
    {
        ClassicAssert.IsTrue(annotation is SpanAnnotation);
        var spanAnn = (SpanAnnotation)annotation!;
        ClassicAssert.AreEqual(expectedCoveredText, spanAnn.CoveredText);
        ClassicAssert.AreEqual(expectedNote, spanAnn.Note);
    }

    /// <summary>
    /// Parse spans that have multiple fragments and ensure they are matched to the correct tokens.
    /// <para/>
    /// Test to ensure OPENNLP-1193 works.
    /// </summary>
    [Test]
    public void TestSpanWithMultiFragments()
    {
        var typeToClassMap = new Dictionary<string, string>();
        BratAnnotationStreamTest.AddEntityTypes(typeToClassMap);
        AnnotationConfiguration config = new(typeToClassMap);

        var txtIn = TestResources.OpenResource("/opennlp/tools/formats/brat/opennlp-1193.txt");

        var annIn = TestResources.OpenResource("/opennlp/tools/formats/brat/opennlp-1193.ann");

        var doc = BratDocument.ParseDocument(config, "opennlp-1193", txtIn, annIn);

        var t1 = (SpanAnnotation)doc.GetAnnotation("T1")!;
        ClassicAssert.AreEqual(t1.Spans[0].Start, 0);
        ClassicAssert.AreEqual(t1.Spans[0].End, 7);
        ClassicAssert.AreEqual(t1.Spans[1].Start, 8);
        ClassicAssert.AreEqual(t1.Spans[1].End, 15);
        ClassicAssert.AreEqual(t1.Spans[2].Start, 17);
        ClassicAssert.AreEqual(t1.Spans[2].End, 24);

        var t2 = (SpanAnnotation)doc.GetAnnotation("T2")!;
        ClassicAssert.AreEqual(t2.Spans[0].Start, 26);
        ClassicAssert.AreEqual(t2.Spans[0].End, 33);
        ClassicAssert.AreEqual(t2.Spans[1].Start, 40);
        ClassicAssert.AreEqual(t2.Spans[1].End, 47);
    }
}
