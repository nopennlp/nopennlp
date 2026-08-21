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
using System.IO;
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Formats.Muc;

internal class DocumentSplitterStream(IObjectStream<string?> samples)
    : FilterObjectStream<string?, string?>(samples)
{
    private const string DocStartElement = "<DOC>";
    private const string DocEndElement = "</DOC>";

    private readonly JCG.List<string> docs = []; // NOpenNLP: made readonly

    /// <inheritdoc/>
    /// <exception cref="IOException">if there is an error during reading</exception>
    public override string? Read()
    {
        if (docs.Count == 0)
        {
            string? newDocs = samples.Read();

            if (newDocs != null)
            {
                int docStartOffset = 0;

                while (true)
                {
                    int startDocElement = newDocs.IndexOf(DocStartElement, docStartOffset, StringComparison.Ordinal);
                    int endDocElement = newDocs.IndexOf(DocEndElement, docStartOffset, StringComparison.Ordinal);

                    if (startDocElement != -1 && endDocElement != -1)
                    {
                        if (startDocElement < endDocElement)
                        {
                            docs.Add(newDocs.Substring(startDocElement,
                                endDocElement + DocEndElement.Length - startDocElement));
                            docStartOffset = endDocElement + DocEndElement.Length;
                        }
                        else
                        {
                            throw new InvalidFormatException("<DOC> element is not closed!");
                        }
                    }
                    else if (startDocElement != endDocElement)
                    {
                        throw new InvalidFormatException("Missing <DOC> or </DOC> element!");
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        if (docs.Count > 0)
        {
            string doc = docs[0];
            docs.RemoveAt(0);
            return doc;
        }
        else
        {
            return null;
        }
    }
}
