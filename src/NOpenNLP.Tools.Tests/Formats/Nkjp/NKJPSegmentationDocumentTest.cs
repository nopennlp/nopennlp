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

using System.IO;
using NOpenNLP.Tools.Support;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Formats.Nkjp;

public class NKJPSegmentationDocumentTest
{
    [Test]
    public void TestParsingSimpleDoc()
    {
        using (Stream nkjpSegXmlIn = TestResources.OpenResource("/opennlp/tools/formats/nkjp/ann_segmentation.xml"))
        {
            NKJPSegmentationDocument doc = NKJPSegmentationDocument.Parse(nkjpSegXmlIn);

            ClassicAssert.AreEqual(1, doc.Segments.Count);

            ClassicAssert.AreEqual(7, doc.Segments["segm_1.1-s"].Count);

            string src = "To krótkie zdanie w drugim akapicie.";

            // NOpenNLP: upstream reads the package-private fields `offset` and `length`
            // directly; the port exposes them as the Offset and Length properties.
            int offset = doc.Segments["segm_1.1-s"]["segm_1.1-seg"].Offset;
            ClassicAssert.AreEqual(0, offset);
            int length = doc.Segments["segm_1.1-s"]["segm_1.1-seg"].Length;
            ClassicAssert.AreEqual(2, length);
            // NOpenNLP: Java's String.substring(begin, end) takes an end index, while .NET's
            // Substring(start, length) takes a length. Upstream passes (offset, length) here,
            // which for offset 0 makes the two spellings coincide; the port keeps the same
            // characters by passing (offset, length - offset).
            ClassicAssert.AreEqual("To", src.Substring(offset, length - offset));
        }
    }
}
