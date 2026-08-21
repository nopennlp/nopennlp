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
using NOpenNLP.Tools.Tokenize;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Formats.Brat;

public class AnnotationConfiguration(IDictionary<string, string> typeToClassMap)
{
    public const string SPAN_TYPE = "Span";
    public const string ENTITY_TYPE = "Entity";
    public const string RELATION_TYPE = "Relation";
    public const string ATTRIBUTE_TYPE = "Attribute";
    public const string EVENT_TYPE = "Event";

    private readonly IDictionary<string, string> typeToClassMap = new JCG.Dictionary<string, string>(typeToClassMap).AsReadOnly();

    /// <summary>
    /// Gets the type class registered for <paramref name="type"/>.
    /// </summary>
    /// <param name="type">the annotation type to look up</param>
    /// <returns>the type class, or <c>null</c> if <paramref name="type"/> is not configured</returns>
    // NOpenNLP: upstream returns Map.get, which yields null for an unconfigured type;
    // the C# indexer would throw instead, so this uses TryGetValue and keeps the null.
    // BratAnnotationStream relies on that null to skip unsupported annotation types.
    public string? GetTypeClass(string type) =>
        typeToClassMap.TryGetValue(type, out string? typeClass) ? typeClass : null;

    /// <summary>
    /// Parses an annotation configuration from the given <paramref name="in"/> stream.
    /// </summary>
    /// <param name="in">the stream to read the annotation.conf contents from</param>
    /// <returns>the parsed <see cref="AnnotationConfiguration"/></returns>
    /// <exception cref="IOException">if there is an error during reading</exception>
    public static AnnotationConfiguration Parse(Stream @in)
    {
        JCG.Dictionary<string, string> typeToClassMap = [];

        // NOpenNLP: leaveOpen keeps the reader from closing the caller's stream, matching
        // upstream, which never closes the BufferedReader it wraps around the stream.
        using var reader = new StreamReader(@in, Encoding.UTF8, detectEncodingFromByteOrderMarks: true,
            bufferSize: 1024, leaveOpen: true);

        // Note: This only supports entities and relations section
        string? sectionType = null;

        while (reader.ReadLine() is { } line)
        {
            line = line.Trim();

            if (line.Length != 0)
            {
                if (!line.StartsWith("#", System.StringComparison.Ordinal))
                {
                    if (line.StartsWith("[", System.StringComparison.Ordinal)
                        && line.EndsWith("]", System.StringComparison.Ordinal))
                    {
                        sectionType = line.Substring(line.IndexOf('[') + 1,
                            line.IndexOf(']') - (line.IndexOf('[') + 1));
                    }
                    else
                    {
                        string typeName = WhitespaceTokenizer.INSTANCE.Tokenize(line)[0];

                        // NOpenNLP: upstream switches on sectionType directly, which throws a
                        // NullPointerException when a type line precedes any section header. A
                        // C# switch on null simply matches no case, so the null is tested first
                        // to keep that malformed input failing rather than silently ignored.
                        if (sectionType is null)
                        {
                            throw new InvalidDataException(
                                "Expected a section header before type " + typeName + "!");
                        }

                        switch (sectionType)
                        {
                            case "entities":
                                typeToClassMap[typeName] = ENTITY_TYPE;
                                break;

                            case "relations":
                                typeToClassMap[typeName] = RELATION_TYPE;
                                break;

                            case "attributes":
                                typeToClassMap[typeName] = ATTRIBUTE_TYPE;
                                break;

                            case "events":
                                typeToClassMap[typeName] = EVENT_TYPE;
                                break;

                            default:
                                break;
                        }
                    }
                }
            }
        }

        return new AnnotationConfiguration(typeToClassMap);
    }

    /// <summary>
    /// Parses an annotation configuration from the given <paramref name="annConfigFile"/>.
    /// </summary>
    /// <param name="annConfigFile">the annotation.conf file to read</param>
    /// <returns>the parsed <see cref="AnnotationConfiguration"/></returns>
    /// <exception cref="IOException">if there is an error during reading</exception>
    public static AnnotationConfiguration Parse(FileInfo annConfigFile)
    {
        using Stream @in = annConfigFile.OpenRead();

        return Parse(@in);
    }
}
