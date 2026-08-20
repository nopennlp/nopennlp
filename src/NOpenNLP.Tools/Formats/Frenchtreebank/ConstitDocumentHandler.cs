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
using System.Text;
using System.Xml;
using NOpenNLP.Tools.Parser;
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Formats.Frenchtreebank;

// NOpenNLP: upstream extends org.xml.sax.helpers.DefaultHandler and is driven by a
// SAXParser pushing startElement/characters/endElement at it. .NET has no SAX parser, so
// the same three methods are kept as-is and an XmlReader pull loop calls them -- the
// approach LetsmtDocument already takes in this port. Keeping the handler's shape means
// its state machine, and in particular when tokenBuffer is and is not cleared, stays a
// line-for-line match for upstream.
internal class ConstitDocumentHandler(IList<Parse> parses)
{
    private const string SENT_ELEMENT_NAME = "SENT";
    private const string WORD_ELEMENT_NAME = "w";

    private const string SENT_TYPE_NAME = "S";

    private readonly IList<Parse> parses = parses;

    private bool insideSentenceElement;

    /// <summary>
    /// A token buffer, a token might be build up by multiple
    /// <see cref="Characters(string)"/> calls.
    /// </summary>
    private readonly StringBuilder tokenBuffer = new StringBuilder();

    private readonly StringBuilder text = new StringBuilder();

    private int offset;
    // NOpenNLP: J2N has no Stack<T>; the BCL one is used. Only Push/Pop/Clear are
    // called, which behave identically.
    private readonly Stack<Constituent> stack = new Stack<Constituent>();
    private readonly IList<Constituent> cons = new JCG.List<Constituent>();

    private string? cat;
    private string? subcat;

    public void StartElement(string qName, Func<string, string?> getAttributeValue)
    {
        string? type = qName;

        if (SENT_ELEMENT_NAME.Equals(qName, StringComparison.Ordinal))
        {
            // Clear everything to be ready for the next sentence
            text.Length = 0;
            offset = 0;
            stack.Clear();
            cons.Clear();

            type = SENT_TYPE_NAME;

            insideSentenceElement = true;
        }
        else if (WORD_ELEMENT_NAME.Equals(qName, StringComparison.Ordinal))
        {
            // Note:
            // If there are compound words they are represented in a couple
            // of ways in the training data.
            // Many of them are marked with the compound attribute, but not
            // all of them. Thats why it is not used in the code to detect
            // a compound word.
            // Compounds are detected by the fact that a w tag is appearing
            // inside a w tag.
            //
            // The type of a compound word can be encoded either cat of the compound
            // plus the catint of each word of the compound.
            // Or all compound words have the cat plus subcat of the compound, in this
            // case they have an empty cat attribute.
            //
            // This implementation hopefully decodes these cases correctly!

            string? newCat = getAttributeValue("cat");
            if (newCat != null && newCat.Length > 0)
            {
                cat = newCat;
            }

            string? newSubcat = getAttributeValue("subcat");
            if (newSubcat != null && newSubcat.Length > 0)
            {
                subcat = newSubcat;
            }

            if (cat != null)
            {
                type = cat + (subcat ?? "");
            }
            else
            {
                // NOpenNLP: `cat` is null in this branch, so upstream's string concatenation
                // renders it as the text "null" -- Java's behaviour for a null operand of +.
                // C# renders a null string as "" instead, so the "null" is spelled out here
                // to keep the produced type names identical.
                string? catint = getAttributeValue("catint");
                if (catint != null)
                {
                    type = "null" + catint;
                }
                else
                {
                    type = "null" + (subcat ?? "null");
                }
            }
        }

        stack.Push(new Constituent(type, new Span(offset, offset)));

        tokenBuffer.Length = 0;
    }

    public void Characters(string ch) => tokenBuffer.Append(ch);

    public void EndElement(string qName)
    {
        bool isCreateConstituent = true;

        if (insideSentenceElement)
        {
            if (WORD_ELEMENT_NAME.Equals(qName, StringComparison.Ordinal))
            {
                string token = tokenBuffer.ToString().Trim();

                if (token.Length > 0)
                {
                    cons.Add(new Constituent(AbstractBottomUpParser.TOK_NODE,
                        new Span(offset, offset + token.Length)));

                    text.Append(token).Append(" ");
                    offset += token.Length + 1;
                }
                else
                {
                    isCreateConstituent = false;
                }
            }

            Constituent unfinishedCon = stack.Pop();

            if (isCreateConstituent)
            {
                int start = unfinishedCon.Span.Start;
                if (start < offset)
                {
                    cons.Add(new Constituent(unfinishedCon.Label, new Span(start, offset - 1)));
                }
            }

            if (SENT_ELEMENT_NAME.Equals(qName, StringComparison.Ordinal))
            {
                // Finished parsing sentence, now put everything together and create
                // a Parse object

                string txt = text.ToString();
                int tokenIndex = -1;
                Parse p = new Parse(txt, new Span(0, txt.Length), AbstractBottomUpParser.TOP_NODE, 1, 0);
                foreach (Constituent con in cons)
                {
                    string type = con.Label;
                    if (!type.Equals(AbstractBottomUpParser.TOP_NODE, StringComparison.Ordinal))
                    {
                        if (AbstractBottomUpParser.TOK_NODE.Equals(type, StringComparison.Ordinal))
                        {
                            tokenIndex++;
                        }
                        Parse c = new Parse(txt, con.Span, type, 1, tokenIndex);
                        p.Insert(c);
                    }
                }
                parses.Add(p);

                insideSentenceElement = false;
            }

            tokenBuffer.Length = 0;
        }
    }
}
