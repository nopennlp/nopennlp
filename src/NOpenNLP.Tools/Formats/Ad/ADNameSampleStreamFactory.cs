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
using NOpenNLP.Tools.Namefind;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Formats.Ad;

/// <summary>
/// A Factory to create a Arvores Deitadas NameSampleDataStream from the command line
/// utility.
/// <para/>
/// <b>Note:</b> Do not use this class, internal use only!
/// </summary>
public class ADNameSampleStreamFactory : LanguageSampleStreamFactory<NameSample?>
{
    // NOpenNLP: upstream's Parameters interface deliberately does NOT extend
    // EncodingParameter -- its own comment says "all have to be repeated, because encoding
    // is not optional". So -encoding is required here, unlike the shared
    // FormatParameters.Encoding, and it needs its own descriptor. The value name and
    // description are upstream's verbatim.
    internal static readonly IFormatParameter EncodingParam =
        new FormatParameter<string>("-encoding", "charsetName",
            "encoding for reading and writing text, if absent the system default is used.");

    internal static readonly IFormatParameter SplitHyphenatedTokensParam =
        FormatParameter<bool>.Optional("-splitHyphenatedTokens", "split",
            "if true all hyphenated tokens will be separated (default true)", true);

    /// <inheritdoc/>
    public override IEnumerable<IFormatParameter> Parameters =>
        [EncodingParam, FormatParameters.Data, SplitHyphenatedTokensParam, FormatParameters.Lang];

    /// <inheritdoc/>
    public override IObjectStream<NameSample?> Create(IFormatParameterValues values)
    {
        language = values.Get<string>(FormatParameters.Lang);

        IInputStreamFactory sampleDataIn =
            CreateInputStreamFactory(values.Get<FileInfo>(FormatParameters.Data)!);

        Encoding encoding = FormatParameters.ResolveEncoding(
            values.Get<string>(EncodingParam));

        IObjectStream<string?> lineStream = new PlainTextByLineStream(sampleDataIn, encoding);

        return new ADNameSampleStream(lineStream, values.Get<bool>(SplitHyphenatedTokensParam));
    }

    public static void RegisterFactory() =>
        StreamFactoryRegistry.RegisterFactory<NameSample?>(
            "ad", new ADNameSampleStreamFactory());
}
