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
using System.Xml;

namespace NOpenNLP.Tools.Util;

public class XmlUtil
{
    /// <summary>
    /// Create a new <see cref="XmlDocument"/> from a stream, processing XML securely.
    /// </summary>
    /// <param name="input">the stream to read the document from</param>
    /// <returns>an <see cref="XmlDocument"/></returns>
    // NOpenNLP: upstream exposes createDocumentBuilder()/createSaxParser(), which
    // return the JAXP factory objects a caller then parses with. .NET has no
    // DocumentBuilder equivalent -- XmlDocument parses directly -- so the secure
    // settings and the parse are combined here. See CreateSecureReaderSettings for
    // why the DTD is parsed rather than prohibited.
    public static XmlDocument CreateDocument(Stream input)
    {
        var settings = CreateSecureReaderSettings();

        // NOpenNLP: Java's DocumentBuilder keeps every text node, including one that is
        // only whitespace; XmlDocument drops those unless PreserveWhitespace is set. The
        // readers walk ChildNodes by index and concatenate the text they find, so a
        // dropped node both shifts the child indices and loses a character from the
        // reconstructed text -- in the Irish Sentence Bank reader, "<token>A</token>
        // <token>B</token>" yielded "AB" rather than "A B", moving every Span after it.
        var document = new XmlDocument
        {
            XmlResolver = null,
            PreserveWhitespace = true,
        };

        using var reader = XmlReader.Create(input, settings);
        document.Load(reader);

        return document;
    }

    /// <summary>
    /// Creates <see cref="XmlReaderSettings"/> which process XML securely.
    /// </summary>
    /// <returns>settings that resolve no external entities</returns>
    // NOpenNLP: Java sets FEATURE_SECURE_PROCESSING, which blocks EXTERNAL entity
    // resolution but still parses an internal DTD subset and expands the entities it
    // declares. DtdProcessing.Prohibit is stricter than that, not equivalent: it throws
    // on any DOCTYPE at all, so a corpus upstream reads happily -- a French Treebank
    // distribution declaring <!ENTITY oe "oe">, for instance -- failed here with an
    // XmlException. DtdProcessing.Parse restores upstream's behaviour, and the null
    // XmlResolver is what actually supplies the security property, refusing to fetch
    // any external DTD or entity the document points at.
    public static XmlReaderSettings CreateSecureReaderSettings()
        => new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Parse,
            XmlResolver = null,

            // NOpenNLP: 1.9.5 added a second layer of hardening on top of
            // FEATURE_SECURE_PROCESSING -- ACCESS_EXTERNAL_DTD and ACCESS_EXTERNAL_SCHEMA set
            // to "", external-general-entities and external-parameter-entities off,
            // load-external-dtd off, and XInclude off. In .NET the first five are all one
            // knob: a null XmlResolver, already set above, is what refuses to fetch an
            // external DTD, schema, or entity. XInclude has no counterpart at all -- the BCL
            // XmlReader does not implement XInclude, so there is nothing to switch off.
            //
            // What .NET does have and Java's flags do not cover is the billion-laughs
            // amplification an internal DTD subset still permits once DtdProcessing.Parse
            // allows one. MaxCharactersFromEntities caps the characters an entity expansion
            // may produce and already defaults to this value, so it is restated rather than
            // introduced -- pinning it here keeps the guarantee from resting on a framework
            // default that a future runtime could change. MaxCharactersInDocument defaults to
            // 0, meaning unbounded, and is the one genuinely new limit: it bounds the parsed
            // document itself, so a small compressed model artifact cannot inflate without
            // limit. Both are far above any real corpus; the largest here are a few MB.
            MaxCharactersFromEntities = 10_000_000,
            MaxCharactersInDocument = 512L * 1024 * 1024,
        };
}
