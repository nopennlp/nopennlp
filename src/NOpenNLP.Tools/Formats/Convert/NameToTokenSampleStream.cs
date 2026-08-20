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
using NOpenNLP.Tools.Namefind;
using NOpenNLP.Tools.Tokenize;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Formats.Convert;

/// <summary>
/// <b>Note:</b> Do not use this class, internal use only!
/// </summary>
public class NameToTokenSampleStream(IDetokenizer detokenizer, IObjectStream<NameSample?> samples)
    : FilterObjectStream<NameSample?, TokenSample?>(samples)
{
    private readonly IDetokenizer detokenizer = detokenizer;

    /// <inheritdoc/>
    /// <exception cref="IOException">if there is an error during reading</exception>
    public override TokenSample? Read()
    {
        NameSample? nameSample = samples.Read();

        TokenSample? tokenSample = null;

        if (nameSample != null)
        {
            tokenSample = new TokenSample(detokenizer, nameSample.Sentence);
        }

        return tokenSample;
    }
}
