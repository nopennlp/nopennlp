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

namespace NOpenNLP.Tools.Sentdetect;

/// <summary>
/// Tests for the <see cref="NewlineSentenceDetector"/> class.
/// </summary>
public class NewlineSentenceDetectorTest
{
    private static void TestSentenceValues(string sentences)
    {
        NewlineSentenceDetector sd = new NewlineSentenceDetector();

        string[] results = sd.SentDetect(sentences);

        ClassicAssert.AreEqual(3, results.Length);
        ClassicAssert.AreEqual("one.", results[0]);
        ClassicAssert.AreEqual("two.", results[1]);
        ClassicAssert.AreEqual("three.", results[2]);
    }

    [Test]
    public void TestNewlineCr()
    {
        TestSentenceValues("one.\rtwo. \r\r three.\r");
    }

    [Test]
    public void TestNewlineLf()
    {
        TestSentenceValues("one.\ntwo. \n\n three.\n");
    }

    [Test]
    public void TestNewlineCrLf()
    {
        TestSentenceValues("one.\r\ntwo. \r\n\r\n three.\r\n");
    }
}
