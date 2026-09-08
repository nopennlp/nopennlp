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
    // how its settings map onto the JAXP features upstream sets.
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
    /// <returns>settings that reject a DOCTYPE and resolve no external entities</returns>
    // NOpenNLP: this mirrors what 1.9.5's createDocumentBuilder() and createSaxParser()
    // both configure. Their settings map onto .NET as follows:
    //
    //   disallow-doctype-decl=true             -> DtdProcessing.Prohibit
    //   ACCESS_EXTERNAL_DTD/SCHEMA=""          -> XmlResolver = null
    //   external-general-entities=false        -> XmlResolver = null
    //   external-parameter-entities=false      -> XmlResolver = null
    //   nonvalidating/load-external-dtd=false  -> XmlResolver = null
    //   setExpandEntityReferences(false)       -> implied by Prohibit
    //   setXIncludeAware(false)                -> no counterpart needed
    //
    // The five external-resource flags collapse into one knob here: a null XmlResolver is
    // what refuses to fetch any external DTD, schema, or entity. XInclude needs nothing --
    // the BCL XmlReader does not implement it, so there is nothing to switch off.
    //
    // Prohibit is what upstream's disallow-doctype-decl does, so no document upstream 1.9.5
    // accepts is rejected here. It also removes the billion-laughs amplification entirely,
    // since there is no internal subset left to expand.
    public static XmlReaderSettings CreateSecureReaderSettings()
        => new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,

            // NOpenNLP: belt and braces behind Prohibit, which already forecloses entity
            // expansion. MaxCharactersFromEntities is restated at its own default rather than
            // introduced, so the guarantee does not rest on a framework default alone;
            // MaxCharactersInDocument defaults to 0, meaning unbounded, and bounds the parsed
            // document so a small compressed artifact cannot inflate without limit. Both sit
            // far above any real corpus; the largest here are a few MB.
            MaxCharactersFromEntities = 10_000_000,
            MaxCharactersInDocument = 512L * 1024 * 1024,
        };
}
