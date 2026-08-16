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

using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Util;

public class ParagraphStreamTest
{
    [Test]
    public void TestSimpleReading()
    {
        using (ParagraphStream paraStream = new ParagraphStream(
                ObjectStreamUtils.CreateObjectStream("1", "2", "", "", "4", "5")))
        {
            ClassicAssert.AreEqual("1\n2\n", paraStream.Read());
            ClassicAssert.AreEqual("4\n5\n", paraStream.Read());
            ClassicAssert.IsNull(paraStream.Read());
        }

        using (ParagraphStream paraStream = new ParagraphStream(
                ObjectStreamUtils.CreateObjectStream("1", "2", "", "", "4", "5", "")))
        {
            ClassicAssert.AreEqual("1\n2\n", paraStream.Read());
            ClassicAssert.AreEqual("4\n5\n", paraStream.Read());
            ClassicAssert.IsNull(paraStream.Read());
        }
    }

    [Test]
    public void TestReset()
    {
        using (ParagraphStream paraStream = new ParagraphStream(
                ObjectStreamUtils.CreateObjectStream("1", "2", "", "", "4", "5", "")))
        {
            ClassicAssert.AreEqual("1\n2\n", paraStream.Read());
            paraStream.Reset();

            ClassicAssert.AreEqual("1\n2\n", paraStream.Read());
            ClassicAssert.AreEqual("4\n5\n", paraStream.Read());
            ClassicAssert.IsNull(paraStream.Read());
        }
    }
}
