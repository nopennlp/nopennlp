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

// This file has been modified from the original Apache OpenNLP 1.9.1 source:
// translated from Java to C# and adapted for .NET. See NOTICE.
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Util.Featuregen;

/// <summary>
/// Tests for the <see cref="StringPattern"/> class.
/// </summary>
public class StringPatternTest
{
    [Test]
    public void TestIsAllLetters()
    {
        ClassicAssert.IsTrue(StringPattern.Recognize("test").IsAllLetter());
        ClassicAssert.IsTrue(StringPattern.Recognize("TEST").IsAllLetter());
        ClassicAssert.IsTrue(StringPattern.Recognize("TesT").IsAllLetter());
        ClassicAssert.IsTrue(StringPattern.Recognize("grün").IsAllLetter());
        ClassicAssert.IsTrue(StringPattern.Recognize("üäöæß").IsAllLetter());
        ClassicAssert.IsTrue(StringPattern.Recognize("あア亜Ａａ").IsAllLetter());
    }

    [Test]
    public void TestIsInitialCapitalLetter()
    {
        ClassicAssert.IsTrue(StringPattern.Recognize("Test").IsInitialCapitalLetter());
        ClassicAssert.IsFalse(StringPattern.Recognize("tEST").IsInitialCapitalLetter());
        ClassicAssert.IsTrue(StringPattern.Recognize("TesT").IsInitialCapitalLetter());
        ClassicAssert.IsTrue(StringPattern.Recognize("Üäöæß").IsInitialCapitalLetter());
        ClassicAssert.IsFalse(StringPattern.Recognize("いイ井").IsInitialCapitalLetter());
        ClassicAssert.IsTrue(StringPattern.Recognize("Iいイ井").IsInitialCapitalLetter());
        ClassicAssert.IsTrue(StringPattern.Recognize("Ｉいイ井").IsInitialCapitalLetter());
    }

    [Test]
    public void TestIsAllCapitalLetter()
    {
        ClassicAssert.IsTrue(StringPattern.Recognize("TEST").IsAllCapitalLetter());
        ClassicAssert.IsTrue(StringPattern.Recognize("ÄÄÄÜÜÜÖÖÖÖ").IsAllCapitalLetter());
        ClassicAssert.IsFalse(StringPattern.Recognize("ÄÄÄÜÜÜÖÖä").IsAllCapitalLetter());
        ClassicAssert.IsFalse(StringPattern.Recognize("ÄÄÄÜÜdÜÖÖ").IsAllCapitalLetter());
        ClassicAssert.IsTrue(StringPattern.Recognize("ＡＢＣ").IsAllCapitalLetter());
        ClassicAssert.IsFalse(StringPattern.Recognize("うウ宇").IsAllCapitalLetter());
    }

    [Test]
    public void TestIsAllLowerCaseLetter()
    {
        ClassicAssert.IsTrue(StringPattern.Recognize("test").IsAllLowerCaseLetter());
        ClassicAssert.IsTrue(StringPattern.Recognize("öäü").IsAllLowerCaseLetter());
        ClassicAssert.IsTrue(StringPattern.Recognize("öäüßßß").IsAllLowerCaseLetter());
        ClassicAssert.IsFalse(StringPattern.Recognize("Test").IsAllLowerCaseLetter());
        ClassicAssert.IsFalse(StringPattern.Recognize("TEST").IsAllLowerCaseLetter());
        ClassicAssert.IsFalse(StringPattern.Recognize("testT").IsAllLowerCaseLetter());
        ClassicAssert.IsFalse(StringPattern.Recognize("tesÖt").IsAllLowerCaseLetter());
        ClassicAssert.IsTrue(StringPattern.Recognize("ａｂｃ").IsAllLowerCaseLetter());
        ClassicAssert.IsFalse(StringPattern.Recognize("えエ絵").IsAllLowerCaseLetter());
    }

    [Test]
    public void TestIsAllDigit()
    {
        ClassicAssert.IsTrue(StringPattern.Recognize("123456").IsAllDigit());
        ClassicAssert.IsFalse(StringPattern.Recognize("123,56").IsAllDigit());
        ClassicAssert.IsFalse(StringPattern.Recognize("12356f").IsAllDigit());
        ClassicAssert.IsTrue(StringPattern.Recognize("１２３４５６").IsAllDigit());
    }

    [Test]
    public void TestIsAllHiragana()
    {
        ClassicAssert.IsTrue(StringPattern.Recognize("あぱっち・るしーん").IsAllHiragana());
        ClassicAssert.IsFalse(StringPattern.Recognize("あぱっち・そふとうぇあ財団").IsAllHiragana());
        ClassicAssert.IsFalse(StringPattern.Recognize("あぱっち・るしーんＶ１．０").IsAllHiragana());
    }

    [Test]
    public void TestIsAllKatakana()
    {
        ClassicAssert.IsTrue(StringPattern.Recognize("アパッチ・ルシーン").IsAllKatakana());
        ClassicAssert.IsFalse(StringPattern.Recognize("アパッチ・ソフトウェア財団").IsAllKatakana());
        ClassicAssert.IsFalse(StringPattern.Recognize("アパッチ・ルシーンＶ１．０").IsAllKatakana());
    }

    [Test]
    public void TestDigits()
    {
        ClassicAssert.AreEqual(6, StringPattern.Recognize("123456").Digits());
        ClassicAssert.AreEqual(3, StringPattern.Recognize("123fff").Digits());
        ClassicAssert.AreEqual(0, StringPattern.Recognize("test").Digits());
        ClassicAssert.AreEqual(3, StringPattern.Recognize("１２３ｆｆｆ").Digits());
    }

    [Test]
    public void TestContainsPeriod()
    {
        ClassicAssert.IsTrue(StringPattern.Recognize("test.").ContainsPeriod());
        ClassicAssert.IsTrue(StringPattern.Recognize("23.5").ContainsPeriod());
        ClassicAssert.IsFalse(StringPattern.Recognize("test,/-1").ContainsPeriod());
    }

    [Test]
    public void TestContainsComma()
    {
        ClassicAssert.IsTrue(StringPattern.Recognize("test,").ContainsComma());
        ClassicAssert.IsTrue(StringPattern.Recognize("23,5").ContainsComma());
        ClassicAssert.IsFalse(StringPattern.Recognize("test./-1").ContainsComma());
    }

    [Test]
    public void TestContainsSlash()
    {
        ClassicAssert.IsTrue(StringPattern.Recognize("test/").ContainsSlash());
        ClassicAssert.IsTrue(StringPattern.Recognize("23/5").ContainsSlash());
        ClassicAssert.IsFalse(StringPattern.Recognize("test.1-,").ContainsSlash());
    }

    [Test]
    public void TestContainsDigit()
    {
        ClassicAssert.IsTrue(StringPattern.Recognize("test1").ContainsDigit());
        ClassicAssert.IsTrue(StringPattern.Recognize("23,5").ContainsDigit());
        ClassicAssert.IsFalse(StringPattern.Recognize("test./-,").ContainsDigit());
        ClassicAssert.IsTrue(StringPattern.Recognize("テスト１").ContainsDigit());
        ClassicAssert.IsFalse(StringPattern.Recognize("テストＴＥＳＴ").ContainsDigit());
    }

    [Test]
    public void TestContainsHyphen()
    {
        ClassicAssert.IsTrue(StringPattern.Recognize("test--").ContainsHyphen());
        ClassicAssert.IsTrue(StringPattern.Recognize("23-5").ContainsHyphen());
        ClassicAssert.IsFalse(StringPattern.Recognize("test.1/,").ContainsHyphen());
    }

    [Test]
    public void TestContainsLetters()
    {
        ClassicAssert.IsTrue(StringPattern.Recognize("test--").ContainsLetters());
        ClassicAssert.IsTrue(StringPattern.Recognize("23h5ßm").ContainsLetters());
        ClassicAssert.IsFalse(StringPattern.Recognize("---.1/,").ContainsLetters());
    }
}
