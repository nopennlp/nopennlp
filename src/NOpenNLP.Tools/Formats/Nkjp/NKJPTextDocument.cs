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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Xml;
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Formats.Nkjp;

public class NKJPTextDocument
{
    internal IDictionary<string, string> divtypes;
    internal IDictionary<string, IDictionary<string, IDictionary<string, string>>> texts;

    internal NKJPTextDocument()
    {
        divtypes = new JCG.Dictionary<string, string>();
        texts = new JCG.Dictionary<string, IDictionary<string, IDictionary<string, string>>>();
    }

    internal NKJPTextDocument(IDictionary<string, string> divtypes,
        IDictionary<string, IDictionary<string, IDictionary<string, string>>> texts)
        : this()
    {
        this.divtypes = divtypes;
        this.texts = texts;
    }

    // NOpenNLP: see the note in NKJPSegmentationDocument -- upstream's DOM is
    // namespace-unaware, so its unprefixed XPath steps match whatever namespace the
    // document declares. local-name() is what reproduces that under XmlDocument, which
    // is always namespace-aware.
    private const string TEXT_NODES_EXAMPLE =
        "/*[local-name()='teiCorpus']/*[local-name()='TEI']/*[local-name()='text']"
        + "/*[local-name()='group']/*[local-name()='text']";

    private const string TEXT_NODES_SAMPLE =
        "/*[local-name()='teiCorpus']/*[local-name()='TEI']/*[local-name()='text']";

    private const string DIV_NODES = "./*[local-name()='body']/*[local-name()='div']";

    private const string PARA_NODES = "./*[local-name()='p']|./*[local-name()='ab']";

    /// <exception cref="IOException">Thrown if the XML cannot be parsed.</exception>
    public static NKJPTextDocument Parse(Stream isStream)
    {
        IDictionary<string, string> divtypes = new JCG.Dictionary<string, string>();
        IDictionary<string, IDictionary<string, IDictionary<string, string>>> texts =
            new JCG.Dictionary<string, IDictionary<string, IDictionary<string, string>>>();

        try
        {
            XmlDocument doc = XmlUtil.CreateDocument(isStream);

            doc.DocumentElement!.Normalize();
            string root = doc.DocumentElement.Name;

            if (!root.Equals("teiCorpus", StringComparison.OrdinalIgnoreCase))
            {
                throw new IOException("Expected root node " + root);
            }

            XmlNodeList textnl = doc.SelectNodes(TEXT_NODES_EXAMPLE)!;
            if (textnl.Count == 0)
            {
                textnl = doc.SelectNodes(TEXT_NODES_SAMPLE)!;
            }

            for (int i = 0; i < textnl.Count; i++)
            {
                XmlNode textnode = textnl[i]!;
                string current_text = Attrib(textnode, "xml:id", true)!;

                IDictionary<string, IDictionary<string, string>> current_divs =
                    new JCG.Dictionary<string, IDictionary<string, string>>();
                XmlNodeList divnl = textnode.SelectNodes(DIV_NODES)!;
                for (int j = 0; j < divnl.Count; j++)
                {
                    XmlNode divnode = divnl[j]!;
                    string? divtype = Attrib(divnode, "type", false);
                    string divid = Attrib(divnode, "xml:id", true)!;
                    // NOpenNLP: upstream puts a possibly-null divtype into a HashMap, which
                    // permits null values; a .NET dictionary does too, so the null is kept
                    // rather than substituted -- a div with no type attribute maps to null
                    // here exactly as it does upstream.
                    divtypes[divid] = divtype!;

                    IDictionary<string, string> current_paras = new JCG.Dictionary<string, string>();
                    XmlNodeList paranl = divnode.SelectNodes(PARA_NODES)!;

                    for (int k = 0; k < paranl.Count; k++)
                    {
                        XmlNode pnode = paranl[k]!;
                        string pid = Attrib(pnode, "xml:id", true)!;

                        // NOpenNLP: the && is upstream's, not a typo for || -- this throws
                        // only when the element has a child count other than one AND its
                        // first child is not character data.
                        if (pnode.ChildNodes.Count != 1
                            && !NodeName(pnode.FirstChild!).Equals("#text", StringComparison.Ordinal))
                        {
                            throw new IOException("Unexpected content in p element " + pid);
                        }

                        string ptext = pnode.InnerText;
                        current_paras[pid] = ptext;
                    }

                    current_divs[divid] = current_paras;
                }

                texts[current_text] = current_divs;
            }
        }
        catch (XmlException e)
        {
            throw new IOException("Failed to parse NKJP document", e);
        }
        catch (System.Xml.XPath.XPathException e)
        {
            throw new IOException("Failed to parse NKJP document", e);
        }

        return new NKJPTextDocument(divtypes, texts);
    }

    /// <summary>
    /// Reports the node name upstream's DOM walk would see for <paramref name="node"/>.
    /// </summary>
    /// <remarks>
    /// Authored for NOpenNLP; not part of the Apache OpenNLP source. Java's DOM exposes every
    /// run of character data as a single "#text" node, whereas <see cref="XmlDocument"/>
    /// reports a run that is entirely whitespace as "#significant-whitespace" or "#whitespace".
    /// A paragraph holding only indentation would otherwise fail the "#text" test that
    /// upstream passes.
    /// </remarks>
    private static string NodeName(XmlNode node) =>
        node.NodeType is XmlNodeType.Whitespace or XmlNodeType.SignificantWhitespace
            ? "#text"
            : node.Name;

    /// <exception cref="IOException">Thrown if the file cannot be read or parsed.</exception>
    internal static NKJPTextDocument Parse(FileInfo file)
    {
        using Stream @in = file.OpenRead();
        return Parse(@in);
    }

    internal IDictionary<string, string> Divtypes =>
        new ReadOnlyDictionary<string, string>(this.divtypes);

    internal IDictionary<string, IDictionary<string, IDictionary<string, string>>> Texts =>
        new ReadOnlyDictionary<string, IDictionary<string, IDictionary<string, string>>>(this.texts);

    /// <summary>
    /// Segmentation etc. is done only in relation to the paragraph,
    /// which are unique within a document. This is to simplify
    /// working with the paragraphs within the document
    /// </summary>
    /// <returns>a map of paragaph IDs and their text</returns>
    internal IDictionary<string, string> GetParagraphs()
    {
        IDictionary<string, string> paragraphs = new JCG.Dictionary<string, string>();
        foreach (string dockey in texts.Keys)
        {
            foreach (string divkey in texts[dockey].Keys)
            {
                foreach (string pkey in texts[dockey][divkey].Keys)
                {
                    paragraphs[pkey] = texts[dockey][divkey][pkey];
                }
            }
        }
        return paragraphs;
    }

    /// <summary>
    /// Helper method to get the value of an attribute
    /// </summary>
    /// <param name="n">The node being processed</param>
    /// <param name="attrib">The name of the attribute</param>
    /// <param name="required">Whether or not the attribute is required</param>
    /// <returns>The value of the attribute, or null if not required and not present</returns>
    /// <exception cref="IOException">Thrown if a required attribute is missing.</exception>
    private static string? Attrib(XmlNode n, string attrib, bool required)
    {
        if (required && (n.Attributes == null || n.Attributes.Count == 0))
        {
            throw new IOException("Missing required attributes in node " + n.Name);
        }
        if (n.Attributes!.GetNamedItem(attrib) != null)
        {
            return n.Attributes.GetNamedItem(attrib)!.InnerText;
        }
        else
        {
            if (required)
            {
                throw new IOException("Required attribute \"" + attrib + "\" missing in node " + n.Name);
            }
            else
            {
                return null;
            }
        }
    }
}
