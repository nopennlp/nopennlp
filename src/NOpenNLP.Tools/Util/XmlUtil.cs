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
    // settings and the parse are combined here. Java sets
    // FEATURE_SECURE_PROCESSING, whose relevant effect is disabling DTD and
    // external entity resolution; DtdProcessing.Prohibit and a null XmlResolver
    // are the .NET counterparts.
    public static XmlDocument CreateDocument(Stream input)
    {
        var settings = CreateSecureReaderSettings();

        var document = new XmlDocument
        {
            XmlResolver = null,
        };

        using var reader = XmlReader.Create(input, settings);
        document.Load(reader);

        return document;
    }

    /// <summary>
    /// Creates <see cref="XmlReaderSettings"/> which process XML securely.
    /// </summary>
    /// <returns>settings that disable DTD and external entity resolution</returns>
    public static XmlReaderSettings CreateSecureReaderSettings() =>
        new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
        };
}
