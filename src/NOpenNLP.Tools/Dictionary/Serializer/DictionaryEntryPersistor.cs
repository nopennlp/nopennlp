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
using NOpenNLP.Tools.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace NOpenNLP.Tools.Dictionary.Serializer;

/// <summary>
/// This class is used by for reading and writing dictionaries of all kinds.
/// </summary>
public static class DictionaryEntryPersistor // NOpenNLP-specific: made static
{
    // NOpenNLP: unused
    // private const string CHARSET = "UTF-8";

    private const string DICTIONARY_ELEMENT = "dictionary";
    private const string ENTRY_ELEMENT = "entry";
    private const string TOKEN_ELEMENT = "token";
    private const string ATTRIBUTE_CASE_SENSITIVE = "case_sensitive";

    /// <summary>
    /// Creates <see cref="Entry"/>s from the given <see cref="Stream"/> and
    /// forwards these <see cref="Entry"/>s to the <see cref="EntryInserter"/>.
    /// <para/>
    /// After creation is finished the provided <see cref="Stream"/> is closed.
    /// </summary>
    /// <param name="in">stream to read entries from</param>
    /// <param name="inserter">inserter to forward entries to</param>
    /// <returns>isCaseSensitive attribute for Dictionary</returns>
    /// <exception cref="IOException"/>
    /// <exception cref="InvalidFormatException"/>
    public static bool Create(Stream @in, EntryInserter inserter)
    {
        // NOpenNLP: the upstream SAX ContentHandler is replaced with an
        // XmlReader pull parser, which is the idiomatic .NET equivalent.
        // The element/attribute handling below mirrors DictionaryContenthandler.
        bool isCaseSensitiveDictionary = true;
        var tokenList = new List<string>();
        Attributes? attributes = null;

        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            IgnoreWhitespace = false,
            CloseInput = false,
        };

        try
        {
            using var reader = XmlReader.Create(@in, settings);

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    string localName = reader.LocalName;
                    bool isEmpty = reader.IsEmptyElement;

                    if (DICTIONARY_ELEMENT.Equals(localName, StringComparison.Ordinal))
                    {
                        attributes = ReadAttributes(reader);

                        /* get the attribute here ... */
                        string? caseSensitive = attributes.GetValue(ATTRIBUTE_CASE_SENSITIVE);
                        if (caseSensitive != null)
                        {
                            isCaseSensitiveDictionary = bool.TryParse(caseSensitive, out bool parsed) && parsed;
                        }

                        attributes = null;
                    }
                    else if (ENTRY_ELEMENT.Equals(localName, StringComparison.Ordinal))
                    {
                        attributes = ReadAttributes(reader);

                        if (isEmpty)
                        {
                            InsertEntry(inserter, tokenList, attributes);
                            attributes = null;
                        }
                    }
                    else if (TOKEN_ELEMENT.Equals(localName, StringComparison.Ordinal))
                    {
                        // ReadElementContentAsString advances past the end element,
                        // so the token text is captured here rather than in a
                        // separate characters()/endElement() pair.
                        tokenList.Add(isEmpty ? string.Empty : reader.ReadElementContentAsString().Trim());
                    }
                }
                else if (reader.NodeType == XmlNodeType.EndElement
                    && ENTRY_ELEMENT.Equals(reader.LocalName, StringComparison.Ordinal))
                {
                    InsertEntry(inserter, tokenList, attributes);
                    attributes = null;
                }
            }
        }
        catch (XmlException e)
        {
            throw new InvalidFormatException("The profile data stream has an invalid format!", e);
        }

        return isCaseSensitiveDictionary;
    }

    private static Attributes ReadAttributes(XmlReader reader)
    {
        var attributes = new Attributes();

        if (reader.HasAttributes)
        {
            for (int i = 0; i < reader.AttributeCount; i++)
            {
                reader.MoveToAttribute(i);
                attributes.SetValue(reader.LocalName, reader.Value);
            }

            reader.MoveToElement();
        }

        return attributes;
    }

    private static void InsertEntry(EntryInserter inserter, List<string> tokenList, Attributes? attributes)
    {
        string[] tokens = [.. tokenList];

        Entry entry = new Entry(new StringList(tokens), attributes);

        inserter(entry);

        tokenList.Clear();
    }

    /// <summary>
    /// Serializes the given entries to the given <see cref="Stream"/>.
    /// <para/>
    /// After the serialization is finished the provided
    /// <see cref="Stream"/> remains open.
    /// </summary>
    /// <param name="out">stream to serialize to</param>
    /// <param name="entries">entries to serialize</param>
    /// <exception cref="IOException">If an I/O error occurs</exception>
    /// <remarks>
    /// Deprecated: Use <see cref="Serialize(Stream, IEnumerable{Entry}, bool)"/> instead.
    /// </remarks>
    [Obsolete("Use Serialize(Stream, IEnumerable<Entry>, bool) instead.")]
    public static void Serialize(Stream @out, IEnumerable<Entry> entries)
    {
        Serialize(@out, entries, true);
    }

    /// <summary>
    /// Serializes the given entries to the given <see cref="Stream"/>.
    /// <para/>
    /// After the serialization is finished the provided
    /// <see cref="Stream"/> remains open.
    /// </summary>
    /// <param name="out">stream to serialize to</param>
    /// <param name="entries">entries to serialize</param>
    /// <param name="casesensitive">indicates if the written dictionary
    /// should be case sensitive or case insensitive.</param>
    /// <exception cref="IOException">If an I/O error occurs</exception>
    /// <remarks>
    /// NOpenNLP: It is more common in .NET to pass <see cref="IEnumerable{T}"/> than
    /// <see cref="IEnumerator{T}"/>, and this has the benefit of not making the caller
    /// clean up the enumerator. This was <c>Iterator&gt;Entry&lt;</c> upstream.
    /// </remarks>
    public static void Serialize(Stream @out, IEnumerable<Entry> entries, bool casesensitive)
    {
        // NOpenNLP: upstream drives a SAX TransformerHandler, whose output properties
        // are set to UTF-8 and indented. XmlWriter is the .NET counterpart, and mirrors
        // the XmlReader the read path above uses. CloseOutput is false because this
        // method documents that the stream stays open.
        var settings = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            Indent = true,
            CloseOutput = false,
        };

        using var writer = XmlWriter.Create(@out, settings);

        writer.WriteStartDocument();
        writer.WriteStartElement(DICTIONARY_ELEMENT);
        // NOpenNLP: Java writes String.valueOf(boolean), which is lowercase; C#'s
        // bool.ToString() yields "True"/"False", which the reader's bool.TryParse
        // would still accept but which would not match the dictionaries upstream
        // writes, so the lowercase spelling is produced explicitly.
        writer.WriteAttributeString(ATTRIBUTE_CASE_SENSITIVE, casesensitive ? "true" : "false");

        foreach (var entry in entries)
        {
            SerializeEntry(writer, entry);
        }

        writer.WriteEndElement();
        writer.WriteEndDocument();

        // NOpenNLP: upstream's endDocument() flushes through the transformer; XmlWriter
        // buffers, so it is flushed here to make sure everything reaches the stream
        // before the caller writes anything further to it.
        writer.Flush();
    }

    private static void SerializeEntry(XmlWriter writer, Entry entry)
    {
        writer.WriteStartElement(ENTRY_ELEMENT);

        // NOpenNLP: Attributes is optional on Entry, and the reader creates entries
        // with a null Attributes when the element carries none, so a round-trip has
        // to tolerate that; upstream's Entry always has one.
        if (entry.Attributes is { } entryAttributes)
        {
            foreach (string key in entryAttributes)
            {
                string? value = entryAttributes.GetValue(key);
                if (value != null)
                {
                    writer.WriteAttributeString(key, value);
                }
            }
        }

        StringList tokens = entry.Tokens;

        foreach (string token in tokens)
        {
            writer.WriteStartElement(TOKEN_ELEMENT);
            writer.WriteString(token);
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }
}
