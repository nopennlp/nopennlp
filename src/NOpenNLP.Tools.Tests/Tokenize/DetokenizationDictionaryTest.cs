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
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Tokenize;

public class DetokenizationDictionaryTest
{
    private DetokenizationDictionary dict;

    [SetUp]
    public void SetUp()
    {
        string[] tokens = ["\"", "(", ")", "-"];

        DetokenizationOperationType[] operations = [
            DetokenizationOperationType.RightLeftMatching,
            DetokenizationOperationType.MoveRight,
            DetokenizationOperationType.MoveLeft,
            DetokenizationOperationType.MoveBoth];

        dict = new DetokenizationDictionary(tokens, operations);
    }

    private static void TestEntries(DetokenizationDictionary dict)
    {
        ClassicAssert.AreEqual(DetokenizationOperationType.RightLeftMatching, dict.GetOperation("\""));
        ClassicAssert.AreEqual(DetokenizationOperationType.MoveRight, dict.GetOperation("("));
        ClassicAssert.AreEqual(DetokenizationOperationType.MoveLeft, dict.GetOperation(")"));
        ClassicAssert.AreEqual(DetokenizationOperationType.MoveBoth, dict.GetOperation("-"));
    }

    [Test]
    public void TestSimpleDict()
    {
        TestEntries(dict);
    }

    [Test]
    public void TestSerialization()
    {
        using var @out = new MemoryStream();
        dict.Serialize(@out);

        using var @in = new MemoryStream(@out.ToArray());
        var parsedDict = new DetokenizationDictionary(@in);

        // should contain the same entries like the original
        TestEntries(parsedDict);
    }
}
