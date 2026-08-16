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
using NOpenNLP.Tools.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace NOpenNLP.Tools.Namefind;

/// <summary>
/// Tests for the <see cref="RegexNameFinder"/> class.
/// </summary>
public class RegexNameFinderTest
{
    [Test]
    public void TestFindSingleTokenPattern()
    {
        Regex testPattern = new Regex("test");
        string[] sentence = ["a", "test", "b", "c"];

        Regex[] patterns = [testPattern];
        IDictionary<string, Regex[]> regexMap = new Dictionary<string, Regex[]>();
        string type = "testtype";

        regexMap[type] = patterns;

        RegexNameFinder finder = new RegexNameFinder(regexMap);

        Span[] result = finder.Find(sentence);

        ClassicAssert.IsTrue(result.Length == 1);

        ClassicAssert.IsTrue(result[0].Start == 1);
        ClassicAssert.IsTrue(result[0].End == 2);
    }

    [Test]
    public void TestFindTokenizdPattern()
    {
        Regex testPattern = new Regex("[0-9]+ year");

        string[] sentence = ["a", "80", "year", "b", "c"];

        Regex[] patterns = [testPattern];
        IDictionary<string, Regex[]> regexMap = new Dictionary<string, Regex[]>();
        string type = "match";

        regexMap[type] = patterns;

        RegexNameFinder finder = new RegexNameFinder(regexMap);

        Span[] result = finder.Find(sentence);

        ClassicAssert.IsTrue(result.Length == 1);

        ClassicAssert.IsTrue(result[0].Start == 1);
        ClassicAssert.IsTrue(result[0].End == 3);
        ClassicAssert.IsTrue(result[0].Type!.Equals("match"));
    }

    [Test]
    public void TestFindMatchingPatternWithoutMatchingTokenBounds()
    {
        Regex testPattern = new Regex("[0-8] year"); // does match "0 year"

        string[] sentence = ["a", "80", "year", "c"];
        Regex[] patterns = [testPattern];
        IDictionary<string, Regex[]> regexMap = new Dictionary<string, Regex[]>();
        string type = "testtype";

        regexMap[type] = patterns;

        RegexNameFinder finder = new RegexNameFinder(regexMap);

        Span[] result = finder.Find(sentence);

        ClassicAssert.IsTrue(result.Length == 0);
    }
}
