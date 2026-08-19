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

using System.IO;
using System.Text;
using J2N.Text;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Tokenize;

/// <summary>
/// This stream formats a <see cref="TokenSample"/>s into whitespace
/// separated token strings.
/// </summary>
public class WhitespaceTokenStream(IObjectStream<TokenSample?> tokens)
    : FilterObjectStream<TokenSample?, string?>(tokens)
{
    /// <inheritdoc/>
    /// <exception cref="IOException">if there is an error during reading</exception>
    public override string? Read()
    {
        TokenSample? tokenSample = samples.Read();

        if (tokenSample != null)
        {
            StringBuilder whitespaceSeparatedTokenString = new StringBuilder();

            foreach (Span token in tokenSample.TokenSpans)
            {
                whitespaceSeparatedTokenString.Append(
                    token.GetCoveredText(tokenSample.Text.AsCharSequence()));
                whitespaceSeparatedTokenString.Append(' ');
            }

            // Shorten string by one to get rid of last space
            if (whitespaceSeparatedTokenString.Length > 0)
            {
                whitespaceSeparatedTokenString.Length =
                    whitespaceSeparatedTokenString.Length - 1;
            }

            return whitespaceSeparatedTokenString.ToString();
        }

        return null;
    }
}
