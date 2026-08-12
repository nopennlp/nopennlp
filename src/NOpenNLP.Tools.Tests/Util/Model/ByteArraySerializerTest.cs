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

// This file has been modified from the original Apache OpenNLP 1.9.1 source:
// translated from Java to C# and adapted for .NET. See NOTICE.
using System.IO;
using J2N;
using NUnit.Framework;

namespace NOpenNLP.Tools.Util.Model;

public class ByteArraySerializerTest
{
    [Test]
    public void TestSerialization()
    {
        // NOpenNLP: J2N's Randomizer reproduces java.util.Random, so seed 23
        // yields the same bytes the upstream test exercises.
        byte[] b = new byte[1024];
        new Randomizer(23).NextBytes(b);

        ByteArraySerializer serializer = new ByteArraySerializer();

        // NOpenNLP: upstream also round-trips through serializer.serialize(...) and
        // asserts the written bytes match. IArtifactSerializer.Serialize is not
        // ported yet (it is commented out in the port), so only Create is covered
        // here. Restore the serialize half of this test when Serialize is ported.
        Assert.That(serializer.Create(new MemoryStream(b)), Is.EqualTo(b));
    }
}
