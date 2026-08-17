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

namespace NOpenNLP.Tools.Langdetect;

public class LanguageTest
{
    [Test]
    public void EmptyConfidence()
    {
        string languageCode = "aLanguage";
        Language lang = new(languageCode);

        ClassicAssert.AreEqual(languageCode, lang.Lang);
        ClassicAssert.AreEqual(0, lang.Confidence, 0);
    }

    [Test]
    public void NonEmptyConfidence()
    {
        string languageCode = "aLanguage";
        double confidence = 0.05;
        Language lang = new(languageCode, confidence);

        ClassicAssert.AreEqual(languageCode, lang.Lang);
        ClassicAssert.AreEqual(confidence, lang.Confidence, 0);
    }

    // NOpenNLP: upstream expects NullPointerException, whose .NET counterpart
    // is ArgumentNullException.
    [Test]
    public void EmptyLanguage() =>
        Assert.Throws<ArgumentNullException>((Action)(() => new Language(null!)));

    // NOpenNLP: upstream expects NullPointerException, whose .NET counterpart
    // is ArgumentNullException.
    [Test]
    public void EmptyLanguageConfidence() =>
        Assert.Throws<ArgumentNullException>((Action)(() => new Language(null!, 0.05)));

    [Test]
    public void TestToString()
    {
        Language lang = new("aLang");

        ClassicAssert.AreEqual("aLang (0.0)", lang.ToString());

        lang = new Language("aLang", 0.0886678);

        ClassicAssert.AreEqual("aLang (0.0886678)", lang.ToString());
    }

    [Test]
    public void TestHash()
    {
        int hashA = new Language("aLang").GetHashCode();
        int hashAA = new Language("aLang").GetHashCode();
        int hashB = new Language("BLang").GetHashCode();
        int hashA5 = new Language("aLang", 5.0).GetHashCode();
        int hashA6 = new Language("BLang", 6.0).GetHashCode();

        ClassicAssert.AreEqual(hashA, hashAA);

        ClassicAssert.AreNotEqual(hashA, hashB);
        ClassicAssert.AreNotEqual(hashA, hashA5);
        ClassicAssert.AreNotEqual(hashB, hashA5);
        ClassicAssert.AreNotEqual(hashA5, hashA6);
    }

    [Test]
    public void TestEquals()
    {
        Language langA = new("langA");
        Language langB = new("langB");
        Language langA5 = new("langA5", 5.0);
        Language langA6 = new("langA5", 6.0);

        ClassicAssert.AreEqual(langA, langA);
        ClassicAssert.AreEqual(langA5, langA5);

        ClassicAssert.AreNotEqual(langA, langA5);
        ClassicAssert.AreNotEqual(langA, langB);

        ClassicAssert.AreEqual(langA6, langA5);

        ClassicAssert.AreNotEqual(langA, "something else");
    }
}
