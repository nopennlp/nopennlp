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
using System.Text;
using System.Xml;
using NOpenNLP.Tools.Tokenize;
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Formats.Irishsentencebank;

/// <summary>
/// A structure to hold an Irish Sentence Bank document, which is a collection
/// of tokenized sentences.
/// <para/>
/// The sentence bank can be downloaded from, and is described
/// <a href="http://www.lexiconista.com/datasets/sentencebank-ga/">here</a>
/// </summary>
public class IrishSentenceBankDocument
{
    public class IrishSentenceBankFlex(string sf, string[] fl)
    {
        internal string surface = sf;
        internal string[] flex = fl;

        public string Surface => surface;

        public string[] Flex => flex;
    }

    public class IrishSentenceBankSentence(string src, string trans, string orig,
        Span[] toks, IrishSentenceBankFlex[]? flx)
    {
        private readonly string source = src;
        private readonly string translation = trans;
        private readonly string original = orig;
        private readonly Span[] tokens = toks;
        private readonly IrishSentenceBankFlex[]? flex = flx;

        public string Source => source;

        public string Translation => translation;

        public string Original => original;

        public Span[] Tokens => tokens;

        public IrishSentenceBankFlex[]? Flex => flex;

        public TokenSample GetTokenSample() => new TokenSample(original, tokens);
    }

    private readonly JCG.List<IrishSentenceBankSentence> sentences; // NOpenNLP: made readonly

    public IrishSentenceBankDocument()
    {
        sentences = new JCG.List<IrishSentenceBankSentence>();
    }

    public void Add(IrishSentenceBankSentence sent) => sentences.Add(sent);

    public IList<IrishSentenceBankSentence> Sentences => sentences.AsReadOnly();

    /// <summary>
    /// Helper to adjust the span of punctuation tokens: ignores spaces to the left of the string
    /// </summary>
    /// <param name="s">the string to check</param>
    /// <param name="start">the offset of the start of the string</param>
    /// <returns>the offset adjusted to ignore spaces to the left</returns>
    private static int AdvanceLeft(string s, int start)
    {
        int ret = start;
        foreach (char c in s)
        {
            if (c == ' ')
            {
                ret++;
            }
            else
            {
                return ret;
            }
        }
        return ret;
    }

    /// <summary>
    /// Helper to adjust the span of punctuation tokens: ignores spaces to the right of the string
    /// </summary>
    /// <param name="s">the string to check</param>
    /// <param name="start">the offset of the start of the string</param>
    /// <returns>the offset of the end of the string, adjusted to ignore spaces to the right</returns>
    private static int AdvanceRight(string s, int start)
    {
        int end = s.Length - 1;
        int ret = start + end + 1;
        for (int i = end; i > 0; i--)
        {
            if (s[i] == ' ')
            {
                ret--;
            }
            else
            {
                return ret;
            }
        }
        return ret;
    }

    /// <summary>
    /// Reports the node name upstream's DOM walk would see for <paramref name="node"/>.
    /// </summary>
    /// <remarks>
    /// Authored for NOpenNLP; not part of the Apache OpenNLP source. Java's DOM exposes
    /// every run of character data as a single "#text" node, whereas
    /// <see cref="XmlDocument"/> reports a run that is entirely whitespace as
    /// "#significant-whitespace" or "#whitespace" depending on the surrounding content
    /// model. Normalizing those back to "#text" is what lets the ported switch statements
    /// stay a line-for-line match for upstream, and keeps whitespace between elements from
    /// falling into the "Unexpected node" branch.
    /// </remarks>
    private static string NodeName(XmlNode node) =>
        node.NodeType is XmlNodeType.Whitespace or XmlNodeType.SignificantWhitespace
            ? "#text"
            : node.Name;

    /// <exception cref="IOException">Thrown if the XML cannot be parsed.</exception>
    // NOpenNLP: upstream uses the JAXP DocumentBuilder; XmlUtil.CreateDocument is this
    // port's equivalent and applies the same secure settings. Node names go through
    // NodeName() rather than XmlNode.Name -- see the remarks there for why.
    public static IrishSentenceBankDocument Parse(Stream isStream)
    {
        var document = new IrishSentenceBankDocument();

        try
        {
            XmlDocument doc = XmlUtil.CreateDocument(isStream);

            string root = doc.DocumentElement!.Name;
            if (!root.Equals("sentences", StringComparison.OrdinalIgnoreCase))
            {
                throw new IOException("Expected root node " + root);
            }

            XmlNodeList nl = doc.DocumentElement.ChildNodes;
            for (int i = 0; i < nl.Count; i++)
            {
                XmlNode sentnode = nl[i]!;
                if (sentnode.Name.Equals("sentence", StringComparison.Ordinal))
                {
                    string src = sentnode.Attributes!.GetNamedItem("source")!.Value!;
                    string trans = "";
                    IDictionary<int, string> toks = new JCG.Dictionary<int, string>();
                    IDictionary<int, IList<string>> flx = new JCG.Dictionary<int, IList<string>>();
                    IList<Span> spans = new JCG.List<Span>();
                    XmlNodeList sentnl = sentnode.ChildNodes;
                    int flexes = 1;
                    var orig = new StringBuilder();

                    for (int j = 0; j < sentnl.Count; j++)
                    {
                        string name = NodeName(sentnl[j]!);
                        switch (name)
                        {
                            case "flex":
                                string slottmpa = sentnl[j]!.Attributes!.GetNamedItem("slot")!.Value!;
                                int flexslot = int.Parse(slottmpa, CultureInfo.InvariantCulture);
                                if (flexslot > flexes)
                                {
                                    flexes = flexslot;
                                }

                                if (!flx.ContainsKey(flexslot))
                                {
                                    flx[flexslot] = new JCG.List<string>();
                                }
                                string tkn = sentnl[j]!.Attributes!.GetNamedItem("lemma")!.Value!;
                                flx[flexslot].Add(tkn);
                                break;

                            case "translation":
                                trans = sentnl[j]!.FirstChild!.InnerText;
                                break;

                            case "original":
                                int last = 0;
                                XmlNodeList orignl = sentnl[j]!.ChildNodes;
                                for (int k = 0; k < orignl.Count; k++)
                                {
                                    // NOpenNLP: Java's DOM reports every run of character data
                                    // as a "#text" node. XmlDocument instead splits out runs
                                    // that are entirely whitespace as "#significant-whitespace"
                                    // (or "#whitespace"), so matching on the name alone would
                                    // send the single spaces between <token>s to the default
                                    // branch and throw. They are character data upstream and
                                    // must take the same path here: they advance `last`, and
                                    // the " " test below is what keeps them from adding a span.
                                    switch (NodeName(orignl[k]!))
                                    {
                                        case "token":
                                            string tmptok = orignl[k]!.FirstChild!.InnerText;
                                            spans.Add(new Span(last, last + tmptok.Length));

                                            string slottmpb = orignl[k]!.Attributes!.GetNamedItem("slot")!.Value!;
                                            int tokslot = int.Parse(slottmpb, CultureInfo.InvariantCulture);
                                            if (tokslot > flexes)
                                            {
                                                flexes = tokslot;
                                            }

                                            toks[tokslot] = tmptok;
                                            orig.Append(tmptok);
                                            last += tmptok.Length;
                                            break;

                                        case "#text":
                                            string tmptxt = orignl[k]!.InnerText;
                                            orig.Append(tmptxt);

                                            if (!" ".Equals(tmptxt, StringComparison.Ordinal))
                                            {
                                                spans.Add(new Span(
                                                    AdvanceLeft(tmptxt, last), AdvanceRight(tmptxt, last)));
                                            }

                                            last += tmptxt.Length;
                                            break;

                                        default:
                                            throw new IOException("Unexpected node: " + NodeName(orignl[k]!));
                                    }
                                }
                                break;

                            case "#text":
                            case "#comment":
                                break;

                            default:
                                throw new IOException("Unexpected node: " + name);
                        }
                    }

                    IrishSentenceBankFlex[]? flexa = new IrishSentenceBankFlex[flexes];
                    foreach (KeyValuePair<int, string> entry in toks)
                    {
                        int flexidx = entry.Key;
                        string left = entry.Value;
                        // NOpenNLP: upstream calls flx.get(flexidx), which yields null for an
                        // absent key; the C# indexer would throw instead, so this uses
                        // TryGetValue to keep the "no flex for this token" path.
                        if (!flx.TryGetValue(flexidx, out IList<string>? right0))
                        {
                            flexa = null;
                            break;
                        }
                        string[] right = new string[right0.Count];
                        right0.CopyTo(right, 0);
                        flexa![flexidx - 1] = new IrishSentenceBankFlex(left, right);
                    }

                    Span[] spanout = new Span[spans.Count];
                    spans.CopyTo(spanout, 0);
                    document.Add(new IrishSentenceBankSentence(src, trans, orig.ToString(), spanout, flexa));
                }
                else if (!NodeName(sentnode).Equals("#text", StringComparison.Ordinal)
                    && !NodeName(sentnode).Equals("#comment", StringComparison.Ordinal))
                {
                    throw new IOException("Unexpected node: " + NodeName(sentnode));
                }
            }
            return document;
        }
        catch (XmlException e)
        {
            throw new IOException("Failed to parse IrishSentenceBank document", e);
        }
    }

    /// <exception cref="IOException">Thrown if the file cannot be read or parsed.</exception>
    internal static IrishSentenceBankDocument Parse(FileInfo file)
    {
        using Stream @in = file.OpenRead();
        return Parse(@in);
    }
}
