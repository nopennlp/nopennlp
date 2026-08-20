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

using System.Collections.Generic;
using System.IO;
using System.Xml;
using NOpenNLP.Tools.Parser;
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Formats.Frenchtreebank;

public class ConstitParseSampleStream : FilterObjectStream<byte[]?, Parse?>
{
    private readonly IList<Parse> parses = new JCG.List<Parse>(); // NOpenNLP: made readonly

    // NOpenNLP: upstream creates the SAXParser once in the constructor and reuses it. There
    // is no .NET counterpart to hold onto -- an XmlReader is created per document inside
    // Read() -- so the field is dropped and the secure settings are applied at each parse.
    protected internal ConstitParseSampleStream(IObjectStream<byte[]?> samples)
        : base(samples)
    {
    }

    /// <inheritdoc/>
    /// <exception cref="IOException">if there is an error during reading</exception>
    public override Parse? Read()
    {
        if (parses.Count == 0)
        {
            byte[]? xmlbytes = samples.Read();

            if (xmlbytes != null)
            {
                IList<Parse> producedParses = new JCG.List<Parse>();
                try
                {
                    ParseXml(xmlbytes, new ConstitDocumentHandler(producedParses));
                }
                catch (XmlException e)
                {
                    throw new IOException(e.Message, e);
                }

                foreach (Parse parse in producedParses)
                {
                    parses.Add(parse);
                }
            }
        }

        if (parses.Count > 0)
        {
            Parse first = parses[0];
            parses.RemoveAt(0);
            return first;
        }
        return null;
    }

    // NOpenNLP: stands in for upstream's saxParser.parse(InputStream, DefaultHandler). It
    // pushes the same three events at the handler in the same order a SAX parser would,
    // so the handler's accumulated-character semantics are unchanged.
    private static void ParseXml(byte[] xmlbytes, ConstitDocumentHandler handler)
    {
        using var input = new MemoryStream(xmlbytes);
        using var reader = XmlReader.Create(input, XmlUtil.CreateSecureReaderSettings());

        string? GetAttributeValue(string name) => reader.GetAttribute(name);

        while (reader.Read())
        {
            switch (reader.NodeType)
            {
                case XmlNodeType.Element:
                    // The attribute lookups must happen before the reader moves on, so the
                    // handler is handed a delegate that reads from the reader in place --
                    // the equivalent of the Attributes object SAX passes to startElement.
                    bool isEmpty = reader.IsEmptyElement;
                    string name = reader.Name;
                    handler.StartElement(name, GetAttributeValue);

                    // An empty element (<w/>) raises no EndElement of its own, so the
                    // end-tag handling has to run here too -- otherwise its Constituent is
                    // never popped off the stack and every later span is misaligned.
                    if (isEmpty)
                    {
                        handler.EndElement(name);
                    }
                    break;

                case XmlNodeType.Text:
                case XmlNodeType.CDATA:
                case XmlNodeType.SignificantWhitespace:
                case XmlNodeType.Whitespace:
                    handler.Characters(reader.Value);
                    break;

                case XmlNodeType.EndElement:
                    handler.EndElement(reader.Name);
                    break;
            }
        }
    }
}
