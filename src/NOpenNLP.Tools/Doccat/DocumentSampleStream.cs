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
using System.IO;
using NOpenNLP.Tools.Tokenize;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Doccat;

/// <summary>
/// This class reads in string encoded training samples, parses them and
/// outputs <see cref="DocumentSample"/> objects.
/// <para/>
/// Format:<br/>
/// Each line contains one sample document.<br/>
/// The category is the first string in the line followed by a tab and whitespace
/// separated document tokens.<br/>
/// Sample line: category-string tab-char whitespace-separated-tokens line-break-char(s)<br/>
/// </summary>
public class DocumentSampleStream(IObjectStream<string?> samples)
    : FilterObjectStream<string?, DocumentSample?>(samples)
{
    /// <inheritdoc/>
    /// <exception cref="IOException">if there is an error during reading</exception>
    public override DocumentSample? Read()
    {
        string? sampleString = samples.Read();

        if (sampleString != null)
        {
            // Whitespace tokenize entire string
            string[] tokens = WhitespaceTokenizer.INSTANCE.Tokenize(sampleString);

            DocumentSample sample;

            if (tokens.Length > 1)
            {
                string category = tokens[0];
                string[] docTokens = new string[tokens.Length - 1];
                Array.Copy(tokens, 1, docTokens, 0, tokens.Length - 1);

                sample = new DocumentSample(category, docTokens);
            }
            else
            {
                throw new IOException("Empty lines, or lines with only a category string are not allowed!");
            }

            return sample;
        }

        return null;
    }
}
