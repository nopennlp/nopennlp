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
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Tokenize;

/// <summary>
/// This class is a stream filter which reads in string encoded samples and creates
/// <see cref="TokenSample"/>s out of them. The input string sample is tokenized if a
/// whitespace or the special separator chars occur.
/// <para/>
/// Sample:<br/>
/// "token1 token2 token3&lt;SPLIT&gt;token4"<br/>
/// The tokens token1 and token2 are separated by a whitespace, token3 and token3
/// are separated by the special character sequence, in this case the default
/// split sequence.
/// <para/>
/// The sequence must be unique in the input string and is not escaped.
/// </summary>
public class TokenSampleStream : FilterObjectStream<string?, TokenSample?>
{
    private readonly string separatorChars;

    public TokenSampleStream(IObjectStream<string?> sampleStrings, string separatorChars)
        : base(sampleStrings ?? throw new ArgumentNullException(nameof(sampleStrings),
            "sampleStrings must not be null"))
    {
        this.separatorChars = separatorChars
            ?? throw new ArgumentNullException(nameof(separatorChars), "separatorChars must not be null");
    }

    public TokenSampleStream(IObjectStream<string?> sentences)
        : this(sentences, TokenSample.DEFAULT_SEPARATOR_CHARS)
    {
    }

    /// <inheritdoc/>
    /// <exception cref="IOException">if there is an error during reading</exception>
    public override TokenSample? Read()
    {
        string? sampleString = samples.Read();

        if (sampleString != null)
        {
            return TokenSample.Parse(sampleString, separatorChars);
        }
        else
        {
            return null;
        }
    }
}
