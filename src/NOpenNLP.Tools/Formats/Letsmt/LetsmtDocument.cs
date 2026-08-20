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
using System.Text;
using System.Xml;
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Formats.Letsmt;

/// <summary>
/// A structure to hold the letsmt document. The documents contains sentences and depending on the
/// source it either contains tokenized text (words) or an un-tokenized sentence string.
/// <para/>
/// The format specification can be found
/// <a href="http://project.letsmt.eu/uploads/Deliverables/D2.1%20%20Specification%20of%20data%20formats%20v1%20final.pdf">here</a>.
/// </summary>
public class LetsmtDocument
{
    public class LetsmtSentence
    {
        internal string? nonTokenizedText;
        internal string[]? tokens;

        public string? NonTokenizedText => nonTokenizedText;

        public string[]? Tokens => tokens is not null ? (string[])tokens.Clone() : null;
    }

    private readonly IList<LetsmtSentence> sentences; // NOpenNLP: made readonly

    private LetsmtDocument(IList<LetsmtSentence> sentences)
    {
        this.sentences = sentences;
    }

    public IList<LetsmtSentence> Sentences => ((JCG.List<LetsmtSentence>)sentences).AsReadOnly();

    /// <exception cref="IOException">Thrown if the XML cannot be parsed.</exception>
    // NOpenNLP: upstream registers a SAX DefaultHandler (LetsmtDocumentHandler) and lets
    // the parser push events at it. .NET has no SAX parser, so the same state machine is
    // driven by an XmlReader pull loop -- the approach DictionaryEntryPersistor already
    // takes in this port. The accumulated-characters behaviour is preserved exactly:
    // upstream never clears `chars` on a start tag, so text is accumulated across nested
    // elements and only reset when a <w> or <s> ends.
    internal static LetsmtDocument Parse(Stream letsmtXmlIn)
    {
        var sentences = new JCG.List<LetsmtSentence>();

        var chars = new StringBuilder();
        var tokens = new JCG.List<string>();

        // XmlUtil.CreateSecureReaderSettings sets DtdProcessing.Prohibit, which is the
        // counterpart of upstream's disallow-doctype-decl feature.
        var settings = XmlUtil.CreateSecureReaderSettings();

        try
        {
            using var reader = XmlReader.Create(letsmtXmlIn, settings);

            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Text:
                    case XmlNodeType.CDATA:
                    case XmlNodeType.SignificantWhitespace:
                    case XmlNodeType.Whitespace:
                        chars.Append(reader.Value);
                        break;

                    case XmlNodeType.Element when reader.IsEmptyElement:
                        // An empty element raises no EndElement node of its own, so the
                        // end-tag handling has to run here too for <w/> and <s/>.
                        EndElement(reader.Name);
                        break;

                    case XmlNodeType.EndElement:
                        EndElement(reader.Name);
                        break;
                }
            }
        }
        catch (XmlException e)
        {
            throw new IOException("Failed to parse letsmt xml!", e);
        }

        return new LetsmtDocument(sentences);

        void EndElement(string name)
        {
            // Note:
            // words are optional in sentences, if there are no words just the chars have to be captured

            switch (name)
            {
                case "w":
                    tokens.Add(chars.ToString().Trim());
                    chars.Length = 0;
                    break;

                // TODO: The sentence should contain the id, so it can be tracked back to the
                // place it came from
                case "s":
                    var sentence = new LetsmtSentence();

                    if (tokens.Count > 0)
                    {
                        sentence.tokens = tokens.ToArray();
                        tokens = new JCG.List<string>();
                    }
                    else
                    {
                        sentence.nonTokenizedText = chars.ToString().Trim();
                    }

                    sentences.Add(sentence);

                    chars.Length = 0;
                    break;
            }
        }
    }

    /// <exception cref="IOException">Thrown if the file cannot be read or parsed.</exception>
    internal static LetsmtDocument Parse(FileInfo file)
    {
        using Stream @in = file.OpenRead();
        return Parse(@in);
    }
}
