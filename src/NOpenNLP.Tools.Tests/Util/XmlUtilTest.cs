/*
 * Copyright 2026 NOpenNLP Contributors
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.IO;
using System.Text;
using System.Xml;
using NOpenNLP.Tools.Support;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Util;

/// <summary>
/// Upstream's <c>XmlUtilTest</c> checks one thing: that <c>createDocumentBuilder()</c> still
/// returns a working parser when the underlying JAXP factory rejects a security option
/// (OPENNLP-1835, for Android and other platforms with a partial implementation). That test
/// has no counterpart here. It works by swapping in a
/// <c>DocumentBuilderFactory</c> subclass through a system property, and .NET has no such
/// pluggable factory: <c>XmlReaderSettings</c> is a sealed set of properties that either exist
/// on the runtime or do not compile, so there is no partial-support case to tolerate.
/// <para/>
/// What the 1.9.5 options were there to guarantee does carry over, so this file asserts the
/// guarantees rather than the mechanism: a DOCTYPE is refused outright, as upstream's
/// <c>disallow-doctype-decl</c> does, and with it every entity vector a DOCTYPE would carry.
/// </summary>
[NOpenNLPSpecific]
public class XmlUtilTest
{
    private static XmlDocument Parse(string xml) =>
        XmlUtil.CreateDocument(new MemoryStream(Encoding.UTF8.GetBytes(xml)));

    /// <summary>
    /// An external general entity pointing at a local file must not be resolved. This is the
    /// classic XXE read primitive, and the counterpart of
    /// <c>external-general-entities</c>/<c>ACCESS_EXTERNAL_DTD</c> being switched off upstream.
    /// The DOCTYPE that would declare it is refused first, so the entity never exists.
    /// </summary>
    [Test]
    public void TestExternalGeneralEntityIsNotResolved()
    {
        string secret = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        File.WriteAllText(secret, "TOP-SECRET");

        try
        {
            Assert.Throws<XmlException>((Action)(() => Parse(
                $"<!DOCTYPE r [ <!ENTITY x SYSTEM \"file://{secret}\"> ]><r>&x;</r>")));
        }
        finally
        {
            File.Delete(secret);
        }
    }

    /// <summary>
    /// An external DTD subset must not be loaded, the counterpart of
    /// <c>nonvalidating/load-external-dtd</c> being switched off upstream.
    /// </summary>
    [Test]
    public void TestExternalDtdIsNotLoaded()
    {
        string secret = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        File.WriteAllText(secret, "<!ENTITY x \"TOP-SECRET\">");

        try
        {
            Assert.Throws<XmlException>((Action)(() =>
                Parse($"<!DOCTYPE r SYSTEM \"file://{secret}\"><r>hi</r>")));
        }
        finally
        {
            File.Delete(secret);
        }
    }

    /// <summary>
    /// An external parameter entity must not be resolved, the counterpart of
    /// <c>external-parameter-entities</c> being switched off upstream. This is the vector
    /// behind out-of-band XXE, where the parameter entity is what reaches the network.
    /// </summary>
    [Test]
    public void TestExternalParameterEntityIsNotResolved()
    {
        string secret = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        File.WriteAllText(secret, "<!ENTITY x \"TOP-SECRET\">");

        try
        {
            Assert.Throws<XmlException>((Action)(() => Parse(
                $"<!DOCTYPE r [ <!ENTITY % p SYSTEM \"file://{secret}\"> %p; ]><r>hi</r>")));
        }
        finally
        {
            File.Delete(secret);
        }
    }

    /// <summary>
    /// A nested-entity bomb must be rejected rather than expanded. Prohibiting the DOCTYPE
    /// forecloses this outright, since the declarations the bomb needs can never be made.
    /// </summary>
    [Test]
    public void TestBillionLaughsIsRejected()
    {
        var bomb = new StringBuilder();
        bomb.Append("<!DOCTYPE lolz [<!ENTITY lol \"lol\">");

        for (int i = 1; i <= 9; i++)
        {
            bomb.Append($"<!ENTITY lol{i} \"");
            string previous = i == 1 ? "&lol;" : $"&lol{i - 1};";
            for (int j = 0; j < 10; j++)
            {
                bomb.Append(previous);
            }

            bomb.Append("\">");
        }

        bomb.Append("]><lolz>&lol9;</lolz>");

        Assert.Throws<XmlException>((Action)(() => Parse(bomb.ToString())));
    }

    /// <summary>
    /// An internal DTD subset is refused too, matching upstream's <c>disallow-doctype-decl</c>.
    /// The port previously parsed one, to keep 1.9.4's <c>FEATURE_SECURE_PROCESSING</c>
    /// behaviour; 1.9.5 turned the feature on for both its parsers, so no document upstream
    /// accepts is rejected by this being stricter.
    /// </summary>
    [Test]
    public void TestInternalDtdSubsetIsRefused()
    {
        Assert.Throws<XmlException>((Action)(() =>
            Parse("<!DOCTYPE r [ <!ENTITY oe \"oe\" > ]><r>c&oe;ur</r>")));
    }

    /// <summary>
    /// A document without a DOCTYPE -- which is every corpus in the test suite, and every XML
    /// file in upstream's own test resources -- parses normally.
    /// </summary>
    [Test]
    public void TestDocumentWithoutDoctypeParses()
    {
        var doc = Parse("<sentences><s>c\u0153ur</s></sentences>");

        ClassicAssert.AreEqual("c\u0153ur", doc.DocumentElement!.InnerText);
    }

    /// <summary>
    /// <see cref="XmlUtil.CreateSecureReaderSettings"/> is what the streaming readers use in
    /// place of <see cref="XmlUtil.CreateDocument"/>, so it must carry the same guarantees.
    /// </summary>
    [Test]
    public void TestSecureReaderSettingsRefuseExternalResources()
    {
        var settings = XmlUtil.CreateSecureReaderSettings();

        // NOpenNLP: XmlReaderSettings.XmlResolver is write-only, so the null resolver cannot
        // be read back and asserted. The behaviour it produces is covered by the parse tests
        // above, which drive CreateDocument through these same settings.
        ClassicAssert.AreEqual(DtdProcessing.Prohibit, settings.DtdProcessing);
        ClassicAssert.Greater(settings.MaxCharactersFromEntities, 0,
            "entity expansion must stay bounded");
        ClassicAssert.Greater(settings.MaxCharactersInDocument, 0,
            "document size must stay bounded");
    }
}
