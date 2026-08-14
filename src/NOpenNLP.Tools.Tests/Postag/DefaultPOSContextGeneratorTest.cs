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

// This file has been modified from the original Apache OpenNLP 1.9.4 source:
// translated from Java to C# and adapted for .NET. See NOTICE.
using NOpenNLP.Tools.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Postag;

/// <summary>
/// We encountered a concurrency issue in the pos tagger module in the class
/// DefaultPOSContextGenerator.
/// <para/>
/// The issue is demonstrated in DefaultPOSContextGeneratorTest.java. The test "multithreading()"
/// consistently fails on our system with the current code if the number of threads
/// (NUMBER_OF_THREADS) is set to 10. If the number of threads is set to 1 (effectively disabling
/// multithreading), the test consistently passes.
/// <para/>
/// We resolved the issue by removing a field in DefaultPOSContextGenerator.java.
/// </summary>
public class DefaultPOSContextGeneratorTest
{
    private static object[] tokens;
    private static DefaultPOSContextGenerator defaultPOSContextGenerator;
    private static string[] tags;

    [OneTimeSetUp]
    public void SetUp()
    {
        const string matchingToken = "tokenC";

        tokens = ["tokenA", "tokenB", matchingToken, "tokenD"];

        StringList stringList = new StringList([matchingToken]);

        Dictionary.Dictionary dictionary = new Dictionary.Dictionary();
        dictionary.Put(stringList);

        defaultPOSContextGenerator = new DefaultPOSContextGenerator(dictionary);

        tags = ["tagA", "tagB", "tagC", "tagD"];
    }

    [Test]
    public void NoDictionaryMatch()
    {
        int index = 1;

        string[] actual = defaultPOSContextGenerator.GetContext(index, tokens, tags);

        string[] expected =
        [
            "default",
            "w=tokenB",
            "suf=B",
            "suf=nB",
            "suf=enB",
            "suf=kenB",
            "pre=t",
            "pre=to",
            "pre=tok",
            "pre=toke",
            "c",
            "p=tokenA",
            "t=tagA",
            "pp=*SB*",
            "n=tokenC",
            "nn=tokenD"
        ];

        CollectionAssert.AreEqual(expected, actual,
            "Calling with not matching index at: " + index);
    }

    [Test]
    public void DictionaryMatch()
    {
        int indexWithDictionaryMatch = 2;

        string[] actual =
            defaultPOSContextGenerator.GetContext(indexWithDictionaryMatch, tokens, tags);

        string[] expected =
        [
            "default",
            "w=tokenC",
            "p=tokenB",
            "t=tagB",
            "pp=tokenA",
            "t2=tagA,tagB",
            "n=tokenD",
            "nn=*SE*"
        ];

        CollectionAssert.AreEqual(expected, actual,
            "Calling with index matching dictionary entry at: " + indexWithDictionaryMatch);
    }

    // NOpenNLP: upstream's multithreading() test demonstrates a concurrency defect
    // caused by the mutable wordsKey field, and was committed together with the fix
    // that removes it. OpenNLP 1.9.4, the version this port targets, still has that
    // field, so the port reproduces it faithfully and this test would fail by
    // design. Port it when the upstream fix is picked up.
}
