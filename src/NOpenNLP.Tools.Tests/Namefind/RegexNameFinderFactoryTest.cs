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
using NOpenNLP.Tools.Tokenize;
using NOpenNLP.Tools.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System.Collections.Generic;

namespace NOpenNLP.Tools.Namefind;

public class RegexNameFinderFactoryTest
{
    private static RegexNameFinder regexNameFinder;

    private static readonly string text = "my email is opennlp@gmail.com and my phone num is" +
        " 123-234-5678 and i like" +
        " https://www.google.com and I visited MGRS  11sku528111 AKA  11S KU 528 111 and" +
        " DMS 45N 123W AKA" +
        "  +45.1234, -123.12 AKA  45.1234N 123.12W AKA 45 30 N 50 30 W";

    [SetUp]
    public void SetUp()
    {
        regexNameFinder = RegexNameFinderFactory.GetDefaultRegexNameFinders(
            RegexNameFinderFactory.DefaultRegexNameFinder.DEGREES_MIN_SEC_LAT_LON,
            RegexNameFinderFactory.DefaultRegexNameFinder.EMAIL,
            RegexNameFinderFactory.DefaultRegexNameFinder.MGRS,
            RegexNameFinderFactory.DefaultRegexNameFinder.USA_PHONE_NUM,
            RegexNameFinderFactory.DefaultRegexNameFinder.URL);
    }

    [Test]
    public void TestEmail()
    {
        string[] tokens = WhitespaceTokenizer.INSTANCE.Tokenize(text);
        Span[] find = regexNameFinder.Find(tokens);
        IList<Span> spanList = find;
        ClassicAssert.IsTrue(spanList.Contains(new Span(3, 4, "EMAIL")));
        Span emailSpan = new Span(3, 4, "EMAIL");
        ClassicAssert.AreEqual("opennlp@gmail.com", tokens[emailSpan.Start]);
    }

    [Test]
    public void TestPhoneNumber()
    {
        string[] tokens = WhitespaceTokenizer.INSTANCE.Tokenize(text);
        Span[] find = regexNameFinder.Find(tokens);
        IList<Span> spanList = find;
        Span phoneSpan = new Span(9, 10, "PHONE_NUM");
        ClassicAssert.IsTrue(spanList.Contains(phoneSpan));
        ClassicAssert.AreEqual("123-234-5678", tokens[phoneSpan.Start]);
    }

    [Test]
    public void TestURL()
    {
        string[] tokens = WhitespaceTokenizer.INSTANCE.Tokenize(text);
        Span[] find = regexNameFinder.Find(tokens);
        IList<Span> spanList = find;
        Span urlSpan = new Span(13, 14, "URL");
        ClassicAssert.IsTrue(spanList.Contains(urlSpan));
        ClassicAssert.AreEqual("https://www.google.com", tokens[urlSpan.Start]);
    }

    [Test]
    public void TestLatLong()
    {
        string[] tokens = WhitespaceTokenizer.INSTANCE.Tokenize(text);
        Span[] find = regexNameFinder.Find(tokens);
        IList<Span> spanList = find;
        Span latLongSpan1 = new Span(22, 24, "DEGREES_MIN_SEC_LAT_LON");
        Span latLongSpan2 = new Span(35, 41, "DEGREES_MIN_SEC_LAT_LON");
        ClassicAssert.IsTrue(spanList.Contains(latLongSpan1));
        ClassicAssert.IsTrue(spanList.Contains(latLongSpan2));
        ClassicAssert.AreEqual("528", tokens[latLongSpan1.Start]);
        ClassicAssert.AreEqual("45", tokens[latLongSpan2.Start]);
    }

    [Test]
    public void TestMgrs()
    {
        string[] tokens = WhitespaceTokenizer.INSTANCE.Tokenize(text);
        Span[] find = regexNameFinder.Find(tokens);
        IList<Span> spanList = find;
        Span mgrsSpan1 = new Span(18, 19, "MGRS");
        Span mgrsSpan2 = new Span(20, 24, "MGRS");
        ClassicAssert.IsTrue(spanList.Contains(mgrsSpan1));
        ClassicAssert.IsTrue(spanList.Contains(mgrsSpan2));
        ClassicAssert.AreEqual("11SKU528111".ToLowerInvariant(), tokens[mgrsSpan1.Start]);
        ClassicAssert.AreEqual("11S", tokens[mgrsSpan2.Start]);
    }
}
