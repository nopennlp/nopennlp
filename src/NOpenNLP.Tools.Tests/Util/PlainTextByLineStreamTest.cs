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
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Util;

/// <summary>
/// Tests for the <see cref="PlainTextByLineStream"/> class.
/// </summary>
public class PlainTextByLineStreamTest
{
    // NOpenNLP: upstream concatenates char literals for the newlines; C# cannot
    // fold string + char into a constant, so they are string literals here.
    internal const string TestString = "line1" +
            "\n" +
            "line2" +
            "\n" +
            "line3" +
            "\r\n" +
            "line4" +
            "\n";

    [Test]
    public void TestLineSegmentation()
    {
        using IObjectStream<string?> stream =
                new PlainTextByLineStream(new MockInputStreamFactory(TestString), Encoding.UTF8);

        ClassicAssert.AreEqual("line1", stream.Read());
        ClassicAssert.AreEqual("line2", stream.Read());
        ClassicAssert.AreEqual("line3", stream.Read());
        ClassicAssert.AreEqual("line4", stream.Read());
        ClassicAssert.IsNull(stream.Read());
    }

    [Test]
    public void TestReset()
    {
        using IObjectStream<string?> stream =
                new PlainTextByLineStream(new MockInputStreamFactory(TestString), Encoding.UTF8);

        ClassicAssert.AreEqual("line1", stream.Read());
        ClassicAssert.AreEqual("line2", stream.Read());
        ClassicAssert.AreEqual("line3", stream.Read());
        stream.Reset();

        ClassicAssert.AreEqual("line1", stream.Read());
        ClassicAssert.AreEqual("line2", stream.Read());
        ClassicAssert.AreEqual("line3", stream.Read());
        ClassicAssert.AreEqual("line4", stream.Read());
        ClassicAssert.IsNull(stream.Read());
    }
}
