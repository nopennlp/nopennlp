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

namespace NOpenNLP.Tools.Tokenize;

public class DictionaryDetokenizerTest
{
    [Test]
    public void TestDetokenizer()
    {
        string[] tokens = [".", "!", "(", ")", "\"", "-"];

        DetokenizationOperationType[] operations = [
            DetokenizationOperationType.MoveLeft,
            DetokenizationOperationType.MoveLeft,
            DetokenizationOperationType.MoveRight,
            DetokenizationOperationType.MoveLeft,
            DetokenizationOperationType.RightLeftMatching,
            DetokenizationOperationType.MoveBoth];

        DetokenizationDictionary dict = new DetokenizationDictionary(tokens, operations);
        IDetokenizer detokenizer = new DictionaryDetokenizer(dict);

        DetokenizationOperation[] detokenizeOperations =
            detokenizer.Detokenize(["Simple", "test", ".", "co", "-", "worker"]);

        ClassicAssert.AreEqual(DetokenizationOperation.NoOperation, detokenizeOperations[0]);
        ClassicAssert.AreEqual(DetokenizationOperation.NoOperation, detokenizeOperations[1]);
        ClassicAssert.AreEqual(DetokenizationOperation.MergeToLeft, detokenizeOperations[2]);
        ClassicAssert.AreEqual(DetokenizationOperation.NoOperation, detokenizeOperations[3]);
        ClassicAssert.AreEqual(DetokenizationOperation.MergeBoth, detokenizeOperations[4]);
        ClassicAssert.AreEqual(DetokenizationOperation.NoOperation, detokenizeOperations[5]);
    }

    internal static IDetokenizer CreateLatinDetokenizer()
    {
        // NOpenNLP: upstream calls getResourceAsStream; the .NET counterpart is
        // an embedded resource, opened through the shared TestResources helper.
        using Stream dictIn = TestResources.OpenResource("/opennlp/tools/tokenize/latin-detokenizer.xml");

        DetokenizationDictionary dict = new DetokenizationDictionary(dictIn);

        return new DictionaryDetokenizer(dict);
    }

    [Test]
    public void TestDetokenizeToString()
    {
        IDetokenizer detokenizer = CreateLatinDetokenizer();

        string[] tokens = ["A", "test", ",", "(", "string", ")", "."];

        string sentence = detokenizer.Detokenize(tokens, null);

        ClassicAssert.AreEqual("A test, (string).", sentence);
    }

    [Test]
    public void TestDetokenizeToString2()
    {
        IDetokenizer detokenizer = CreateLatinDetokenizer();

        string[] tokens = ["A", "co", "-", "worker", "helped", "."];

        string sentence = detokenizer.Detokenize(tokens, null);

        ClassicAssert.AreEqual("A co-worker helped.", sentence);
    }
}
