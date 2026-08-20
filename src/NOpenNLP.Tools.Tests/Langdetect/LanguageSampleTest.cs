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

public class LanguageSampleTest
{
    [Test]
    public void TestConstructor()
    {
        Language lang = new("aLang");
        string context = "aContext";

        LanguageSample sample = new(lang, context);

        ClassicAssert.AreEqual(lang, sample.Language);
        ClassicAssert.AreEqual(context, sample.Context);
    }

    // NOpenNLP: upstream's testLanguageSampleSerDe round-trips the sample through
    // Java object serialization. LanguageSample does not implement a .NET
    // equivalent of java.io.Serializable (see the note on the ported class), so
    // there is nothing to exercise and the test is omitted.

    [Test]
    public void TestNullLang()
    {
        string context = "aContext";

        // NOpenNLP: upstream expects NullPointerException; the .NET counterpart
        // for a rejected null argument is ArgumentNullException.
        Assert.Throws<ArgumentNullException>((Action)(() => _ = new LanguageSample(null!, context)));
    }

    [Test]
    public void TestNullContext()
    {
        Language lang = new("aLang");

        // NOpenNLP: upstream expects NullPointerException; the .NET counterpart
        // for a rejected null argument is ArgumentNullException.
        Assert.Throws<ArgumentNullException>((Action)(() => _ = new LanguageSample(lang, null!)));
    }

    [Test]
    public void TestToString()
    {
        Language lang = new("aLang");
        string context = "aContext";

        LanguageSample sample = new(lang, context);

        ClassicAssert.AreEqual(lang.Lang + "\t" + context, sample.ToString());
    }

    [Test]
    public void TestHash()
    {
        int hashA = new LanguageSample(new Language("aLang"), "aContext").GetHashCode();
        int hashB = new LanguageSample(new Language("bLang"), "aContext").GetHashCode();
        int hashC = new LanguageSample(new Language("aLang"), "bContext").GetHashCode();

        ClassicAssert.AreNotEqual(hashA, hashB);
        ClassicAssert.AreNotEqual(hashA, hashC);
        ClassicAssert.AreNotEqual(hashB, hashC);
    }

    [Test]
    public void TestEquals()
    {
        LanguageSample sampleA = new(new Language("aLang"), "aContext");
        LanguageSample sampleA1 = new(new Language("aLang"), "aContext");
        LanguageSample sampleB = new(new Language("bLang"), "aContext");
        LanguageSample sampleC = new(new Language("aLang"), "bContext");

        ClassicAssert.AreEqual(sampleA, sampleA);
        ClassicAssert.AreEqual(sampleA, sampleA1);
        ClassicAssert.AreNotEqual(sampleA, sampleB);
        ClassicAssert.AreNotEqual(sampleA, sampleC);
        ClassicAssert.AreNotEqual(sampleB, sampleC);
        ClassicAssert.AreNotEqual(sampleA, "something else");
    }
}
