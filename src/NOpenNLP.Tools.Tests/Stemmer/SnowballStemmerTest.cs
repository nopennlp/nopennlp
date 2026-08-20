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

using NOpenNLP.Tools.Stemmer.Snowball;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Stemmer;

public class SnowballStemmerTest
{
    [Test]
    public void TestArabic()
    {
        SnowballStemmer stemmer = new SnowballStemmer(ALGORITHM.ARABIC);
        ClassicAssert.AreEqual(stemmer.Stem("أأباءاهم"), "اباء");
        ClassicAssert.AreEqual(stemmer.Stem("استفتياكما"), "استفتي");
        ClassicAssert.AreEqual(stemmer.Stem("استنتاجاتهما"), "استنتاجا");
    }

    [Test]
    public void TestDanish()
    {
        SnowballStemmer stemmer = new SnowballStemmer(ALGORITHM.DANISH);
        ClassicAssert.AreEqual(stemmer.Stem("aabenbaringen"), "aabenbaring");
        ClassicAssert.AreEqual(stemmer.Stem("skuebrødsbordene"), "skuebrødsbord");
        ClassicAssert.AreEqual(stemmer.Stem("skrøbeligheder"), "skrøb");
    }

    [Test]
    public void TestDutch()
    {
        SnowballStemmer stemmer = new SnowballStemmer(ALGORITHM.DUTCH);
        ClassicAssert.AreEqual(stemmer.Stem("vliegtuigtransport"), "vliegtuigtransport");
        ClassicAssert.AreEqual(stemmer.Stem("sterlabcertificaat"), "sterlabcertificat");
        ClassicAssert.AreEqual(stemmer.Stem("vollegrondsgroenteteelt"),
            "vollegrondsgroenteteelt");
    }

    [Test]
    public void TestCatalan()
    {
        SnowballStemmer stemmer = new SnowballStemmer(ALGORITHM.CATALAN);
        ClassicAssert.AreEqual(stemmer.Stem("importantíssimes"), "important");
        ClassicAssert.AreEqual(stemmer.Stem("besar"), "bes");
        ClassicAssert.AreEqual(stemmer.Stem("accidentalment"), "accidental");
    }

    [Test]
    public void TestEnglish()
    {
        SnowballStemmer stemmer = new SnowballStemmer(ALGORITHM.ENGLISH);
        ClassicAssert.AreEqual(stemmer.Stem("accompanying"), "accompani");
        ClassicAssert.AreEqual(stemmer.Stem("malediction"), "maledict");
        ClassicAssert.AreEqual(stemmer.Stem("softeners"), "soften");
    }

    [Test]
    public void TestFinnish()
    {
        SnowballStemmer stemmer = new SnowballStemmer(ALGORITHM.FINNISH);
        ClassicAssert.AreEqual(stemmer.Stem("esiintymispaikasta"), "esiintymispaik");
        ClassicAssert.AreEqual(stemmer.Stem("esiintyviätaiteilijaystäviään"),
            "esiintyviätaiteilijaystäviä");
        ClassicAssert.AreEqual(stemmer.Stem("hellbergiä"), "hellberg");
    }

    [Test]
    public void TestFrench()
    {
        SnowballStemmer stemmer = new SnowballStemmer(ALGORITHM.FRENCH);
        ClassicAssert.AreEqual(stemmer.Stem("accomplissaient"), "accompl");
        ClassicAssert.AreEqual(stemmer.Stem("examinateurs"), "examin");
        ClassicAssert.AreEqual(stemmer.Stem("prévoyant"), "prévoi");
    }

    [Test]
    public void TestGerman()
    {
        SnowballStemmer stemmer = new SnowballStemmer(ALGORITHM.GERMAN);
        ClassicAssert.AreEqual(stemmer.Stem("buchbindergesellen"), "buchbindergesell");
        ClassicAssert.AreEqual(stemmer.Stem("mindere"), "mind");
        ClassicAssert.AreEqual(stemmer.Stem("mitverursacht"), "mitverursacht");
    }

    [Test]
    public void TestGreek()
    {
        SnowballStemmer stemmer = new SnowballStemmer(ALGORITHM.GREEK);
        ClassicAssert.AreEqual(stemmer.Stem("επιστροφή"), "επιστροφ");
        ClassicAssert.AreEqual(stemmer.Stem("Αμερικανών"), "αμερικαν");
        ClassicAssert.AreEqual(stemmer.Stem("στρατιωτών"), "στρατιωτ");
    }

    [Test]
    public void TestHungarian()
    {
        SnowballStemmer stemmer = new SnowballStemmer(ALGORITHM.HUNGARIAN);
        ClassicAssert.AreEqual(stemmer.Stem("abbahagynám"), "abbahagyna");
        ClassicAssert.AreEqual(stemmer.Stem("konstrukciójából"), "konstrukció");
        ClassicAssert.AreEqual(stemmer.Stem("lopta"), "lopt");
    }

    [Test]
    public void TestIrish()
    {
        SnowballStemmer stemmer = new SnowballStemmer(ALGORITHM.IRISH);
        ClassicAssert.AreEqual(stemmer.Stem("bhfeidhm"), "feidhm");
        ClassicAssert.AreEqual(stemmer.Stem("feirmeoireacht"), "feirmeoir");
        ClassicAssert.AreEqual(stemmer.Stem("monarcacht"), "monarc");
    }

    [Test]
    public void TestItalian()
    {
        SnowballStemmer stemmer = new SnowballStemmer(ALGORITHM.ITALIAN);
        ClassicAssert.AreEqual(stemmer.Stem("abbattimento"), "abbatt");
        ClassicAssert.AreEqual(stemmer.Stem("dancer"), "dancer");
        ClassicAssert.AreEqual(stemmer.Stem("dance"), "danc");
    }

    [Test]
    public void TestIndonesian()
    {
        SnowballStemmer stemmer = new SnowballStemmer(ALGORITHM.INDONESIAN);
        ClassicAssert.AreEqual(stemmer.Stem("peledakan"), "ledak");
        ClassicAssert.AreEqual(stemmer.Stem("pelajaran"), "ajar");
        ClassicAssert.AreEqual(stemmer.Stem("perbaikan"), "baik");
    }

    [Test]
    public void TestPortuguese()
    {
        SnowballStemmer stemmer = new SnowballStemmer(ALGORITHM.PORTUGUESE);
        ClassicAssert.AreEqual(stemmer.Stem("aborrecimentos"), "aborrec");
        ClassicAssert.AreEqual(stemmer.Stem("aché"), "aché");
        ClassicAssert.AreEqual(stemmer.Stem("ache"), "ache");
    }

    [Test]
    public void TestRomanian()
    {
        SnowballStemmer stemmer = new SnowballStemmer(ALGORITHM.ROMANIAN);
        ClassicAssert.AreEqual(stemmer.Stem("absurdităţilor"), "absurd");
        ClassicAssert.AreEqual(stemmer.Stem("laşi"), "laş");
        ClassicAssert.AreEqual(stemmer.Stem("saracilor"), "sarac");
    }

    [Test]
    public void TestSpanish()
    {
        SnowballStemmer stemmer = new SnowballStemmer(ALGORITHM.SPANISH);
        ClassicAssert.AreEqual(stemmer.Stem("besó"), "bes");
        ClassicAssert.AreEqual(stemmer.Stem("importantísimas"), "importantisim");
        ClassicAssert.AreEqual(stemmer.Stem("incidental"), "incidental");
    }

    [Test]
    public void TestSwedish()
    {
        SnowballStemmer stemmer = new SnowballStemmer(ALGORITHM.SWEDISH);
        ClassicAssert.AreEqual(stemmer.Stem("aftonringningen"), "aftonringning");
        ClassicAssert.AreEqual(stemmer.Stem("andedrag"), "andedrag");
        ClassicAssert.AreEqual(stemmer.Stem("andedrägt"), "andedräg");
    }

    [Test]
    public void TestTurkish()
    {
        SnowballStemmer stemmer = new SnowballStemmer(ALGORITHM.TURKISH);
        ClassicAssert.AreEqual(stemmer.Stem("ab'yle"), "ab'yle");
        ClassicAssert.AreEqual(stemmer.Stem("kaçmamaktadır"), "kaçmamak");
        ClassicAssert.AreEqual(stemmer.Stem("sarayı'nı"), "sarayı'nı");
    }
}
