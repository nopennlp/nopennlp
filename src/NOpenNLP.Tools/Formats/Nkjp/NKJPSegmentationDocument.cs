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
using System.Globalization;
using System.IO;
using System.Xml;
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Formats.Nkjp;

public class NKJPSegmentationDocument
{
    public class Pointer(string doc, string id, int offset, int length, bool spaceAfter)
    {
        public string Doc => doc;

        public string Id => id;

        public int Offset => offset;

        public int Length => length;

        public bool SpaceAfter => spaceAfter;

        public Span ToSpan() => new Span(offset, offset + length);

        public override string ToString() =>
            $"{doc}#string-range({id},{offset.ToString(CultureInfo.InvariantCulture)},{length.ToString(CultureInfo.InvariantCulture)})";
    }

    // NOpenNLP: upstream exposes getSegments(); the outer map preserves insertion order
    // (LinkedHashMap upstream), which NKJPSentenceSampleStream relies on when it walks the
    // sentences in document order. J2N's Dictionary does not preserve insertion order, so
    // this uses OrderedDictionary, J2N's replacement for LinkedHashMap. The inner maps are
    // ordered for the same reason: NKJPSentenceSampleStream accumulates each sentence's
    // character offsets by walking the segment keys in order, so reordering them would
    // shift the resulting spans rather than merely reorder the output.
    public IDictionary<string, IDictionary<string, Pointer>> Segments => segments;

    internal readonly JCG.OrderedDictionary<string, IDictionary<string, Pointer>> segments;

    internal NKJPSegmentationDocument()
        : this([])
    {
    }

    internal NKJPSegmentationDocument(JCG.OrderedDictionary<string, IDictionary<string, Pointer>> segments)
    {
        this.segments = segments;
    }

    // NOpenNLP: upstream compiles these with javax.xml.xpath against a DOM built by a
    // DocumentBuilderFactory left at its default setNamespaceAware(false). That DOM puts
    // every element in the null namespace and keeps its qName verbatim, so upstream's
    // unprefixed XPath steps match regardless of what namespace the file declares -- and
    // the NKJP fixtures do declare a TEI default namespace. XmlDocument is always
    // namespace-aware, so the same literal expressions would match nothing. local-name()
    // reproduces upstream's namespace-blindness exactly; binding a prefix to the TEI URI
    // instead would hardcode one namespace and fail on documents upstream still reads.
    private const string SENT_NODES =
        "/*[local-name()='teiCorpus']/*[local-name()='TEI']/*[local-name()='text']"
        + "/*[local-name()='body']/*[local-name()='p']/*[local-name()='s']";

    private const string SEG_NODES = "./*[local-name()='seg']|./*[local-name()='choice']";

    private const string SEG_NODES_ONLY = "./*[local-name()='seg']";

    /// <exception cref="IOException">Thrown if the XML cannot be parsed.</exception>
    public static NKJPSegmentationDocument Parse(Stream isStream)
    {
        JCG.OrderedDictionary<string, IDictionary<string, Pointer>> sentences = [];

        try
        {
            var doc = XmlUtil.CreateDocument(isStream);

            var nl = doc.SelectNodes(SENT_NODES)!;

            for (int i = 0; i < nl.Count; i++)
            {
                var sentnode = nl[i]!;

                string? sentid = null;
                if (sentnode.Attributes?.GetNamedItem("xml:id") != null)
                {
                    sentid = sentnode.Attributes.GetNamedItem("xml:id")!.InnerText;
                }

                JCG.OrderedDictionary<string, Pointer> segments = [];
                var segnl = sentnode.SelectNodes(SEG_NODES)!;

                for (int j = 0; j < segnl.Count; j++)
                {
                    var n = segnl[j]!;
                    if (NodeName(n).Equals("seg", StringComparison.Ordinal))
                    {
                        string segid = XmlID(n);
                        var pointer = FromSeg(n);
                        segments[segid] = pointer;
                    }
                    else if (NodeName(n).Equals("choice", StringComparison.Ordinal))
                    {
                        var choices = n.ChildNodes;

                        for (int k = 0; k < choices.Count; k++)
                        {
                            if (NodeName(choices[k]!).Equals("nkjp:paren", StringComparison.Ordinal))
                            {
                                if (!CheckRejectedParen(choices[k]!))
                                {
                                    var paren_segs = choices[k]!.SelectNodes(SEG_NODES_ONLY)!;

                                    for (int l = 0; l < paren_segs.Count; l++)
                                    {
                                        string segid = XmlID(paren_segs[l]!);
                                        var pointer = FromSeg(paren_segs[l]!);
                                        segments[segid] = pointer;
                                    }
                                }
                            }
                            else if (NodeName(choices[k]!).Equals("seg", StringComparison.Ordinal))
                            {
                                if (!CheckRejected(choices[k]!))
                                {
                                    string segid = XmlID(choices[k]!);
                                    var pointer = FromSeg(choices[k]!);
                                    segments[segid] = pointer;
                                }
                            }
                        }
                    }
                }

                // NOpenNLP: upstream does sentences.put(sentid, segments) with a sentid that
                // is null when the <s> carries no xml:id. A .NET dictionary rejects a null
                // key, so the null is rendered as the text "null" -- the string Java would
                // print for it -- keeping one bucket for the unnamed sentences the way
                // LinkedHashMap's single null key does.
                sentences[sentid ?? "null"] = segments;
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

        return new NKJPSegmentationDocument(sentences);
    }

    /// <summary>
    /// Reports the node name upstream's DOM walk would see for <paramref name="node"/>.
    /// </summary>
    /// <remarks>
    /// Authored for NOpenNLP; not part of the Apache OpenNLP source. Java's DOM exposes every
    /// run of character data as a single "#text" node, whereas <see cref="XmlDocument"/>
    /// reports a run that is entirely whitespace as "#significant-whitespace" or "#whitespace".
    /// The NKJP files are indented, so the children of a &lt;choice&gt; element include such
    /// runs; normalizing them back to "#text" keeps them out of the seg/nkjp:paren branches
    /// exactly as upstream leaves them out.
    /// </remarks>
    private static string NodeName(XmlNode node) =>
        node.NodeType is XmlNodeType.Whitespace or XmlNodeType.SignificantWhitespace
            ? "#text"
            : node.Name;

    internal static bool CheckRejected(XmlNode n)
    {
        if (n.Attributes == null)
        {
            return false;
        }
        if (n.Attributes.GetNamedItem("nkjp:rejected") == null)
        {
            return false;
        }
        string rejected = n.Attributes.GetNamedItem("nkjp:rejected")!.InnerText;
        return rejected.Equals("true", StringComparison.Ordinal);
    }

    internal static bool CheckRejectedParen(XmlNode n)
    {
        if (n.ChildNodes.Count == 0)
        {
            return false;
        }
        else
        {
            for (int i = 0; i < n.ChildNodes.Count; i++)
            {
                var m = n.ChildNodes[i]!;
                if (NodeName(m).Equals("seg", StringComparison.Ordinal))
                {
                    if (!CheckRejected(m))
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }

    /// <exception cref="IOException">Thrown if the node has no xml:id attribute.</exception>
    internal static string XmlID(XmlNode n)
    {
        if (n.Attributes == null || n.Attributes.Count < 1)
        {
            throw new IOException("Missing required attributes");
        }

        string? id = null;
        if (n.Attributes.GetNamedItem("xml:id") != null)
        {
            id = n.Attributes.GetNamedItem("xml:id")!.InnerText;
        }

        if (id == null)
        {
            throw new IOException("Missing xml:id attribute");
        }

        return id;
    }

    /// <exception cref="IOException">Thrown if the seg node is not a valid NKJP pointer.</exception>
    internal static Pointer FromSeg(XmlNode n)
    {
        if (n.Attributes == null || n.Attributes.Count < 2)
        {
            throw new IOException("Missing required attributes");
        }

        string? ptr = null;
        if (n.Attributes.GetNamedItem("corresp") != null)
        {
            ptr = n.Attributes.GetNamedItem("corresp")!.InnerText;
        }
        string spacing = "";
        if (n.Attributes.GetNamedItem("nkjp:nps") != null)
        {
            spacing = n.Attributes.GetNamedItem("nkjp:nps")!.InnerText;
        }

        if (ptr == null)
        {
            throw new IOException("Missing required attribute");
        }

        // NOpenNLP: reproduced verbatim from upstream, which tests `ptr` -- the corresp
        // pointer -- against "yes" rather than testing `spacing`, the nkjp:nps value it
        // just read. A corresp attribute is a string-range() pointer and can never equal
        // "yes", so space_after is always false. This looks like an upstream defect, but
        // Pointer.space_after is never read anywhere in OpenNLP, so the behaviour is not
        // observable; it is left as-is to keep the port faithful.
        bool space_after = ptr.Equals("yes", StringComparison.Ordinal);

        if (!ptr.Contains("#") || !ptr.Contains("(") || ptr[^1] != ')')
        {
            throw new IOException("String " + ptr + " does not appear to be a valid NKJP corresp attribute");
        }

        int docend = ptr.IndexOf('#');
        string document = ptr[..docend];

        int pointer_start = ptr.IndexOf('(') + 1;
        string[] pieces = ptr.Substring(pointer_start, ptr.Length - 1 - pointer_start).Split(',');

        if (pieces.Length is < 3 or > 4)
        {
            throw new IOException("String " + ptr + " does not appear to be a valid NKJP corresp attribute");
        }

        string docid = pieces[0];
        int offset;
        int length;
        if (pieces.Length == 3)
        {
            offset = int.Parse(pieces[1], CultureInfo.InvariantCulture);
            length = int.Parse(pieces[2], CultureInfo.InvariantCulture);
        }
        else
        {
            int os1 = int.Parse(pieces[1], CultureInfo.InvariantCulture);
            int os2 = int.Parse(pieces[2], CultureInfo.InvariantCulture);
            offset = (os1 * 1000) + os2;
            length = int.Parse(pieces[3], CultureInfo.InvariantCulture);
        }

        return new Pointer(document, docid, offset, length, space_after);
    }

    /// <exception cref="IOException">Thrown if the file cannot be read or parsed.</exception>
    internal static NKJPSegmentationDocument Parse(FileInfo file)
    {
        using Stream @in = file.OpenRead();
        return Parse(@in);
    }
}
