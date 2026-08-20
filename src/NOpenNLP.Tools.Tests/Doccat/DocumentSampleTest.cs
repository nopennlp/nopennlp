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
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Doccat;

public class DocumentSampleTest
{
    [Test]
    public void TestEquals()
    {
        ClassicAssert.IsFalse(ReferenceEquals(CreateGoldSample(), CreateGoldSample()));
        ClassicAssert.IsTrue(CreateGoldSample().Equals(CreateGoldSample()));
        ClassicAssert.IsFalse(CreatePredSample().Equals(CreateGoldSample()));
        ClassicAssert.IsFalse(CreatePredSample().Equals(new object()));
    }

    // NOpenNLP: upstream's testDocumentSampleSerDe round-trips the sample through
    // Java object serialization. DocumentSample does not implement a .NET
    // equivalent of java.io.Serializable (see the note on the ported class), so
    // there is nothing to exercise and the test is omitted.

    public static DocumentSample CreateGoldSample() =>
        new DocumentSample("aCategory", ["a", "small", "text"]);

    public static DocumentSample CreatePredSample() =>
        new DocumentSample("anotherCategory", ["a", "small", "text"]);
}
