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
using NOpenNLP.Tools.Chunker;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Formats.Ad;

/// <summary>
/// A Factory to create a Arvores Deitadas ChunkStream from the command line
/// utility.
/// <para/>
/// <b>Note:</b> Do not use this class, internal use only!
/// </summary>
public class ADChunkSampleStreamFactory : LanguageSampleStreamFactory<ChunkSample?>
{
    // NOpenNLP: upstream's Parameters interface deliberately does NOT extend
    // EncodingParameter -- its own comment says "all have to be repeated, because encoding
    // is not optional". So -encoding is required here, unlike the shared
    // FormatParameters.Encoding, and it needs its own descriptor. The value name and
    // description are upstream's verbatim.
    internal static readonly IFormatParameter EncodingParam =
        new FormatParameter<string>("-encoding", "charsetName",
            "encoding for reading and writing text, if absent the system default is used.");

    // NOpenNLP: upstream types these as java.lang.Integer with no @OptionalParameter
    // defaultValue, so an omitted option arrives as null and the create() body guards the
    // unboxing with a null check before its `> -1` test. A FormatParameter<int> cannot
    // carry null, and default(int) is 0 -- which the stream would honour as a real index
    // and truncate on. The default is -1 instead, the same value the stream's own start
    // and end fields hold when unset, so the `> -1` test below rejects an omitted option
    // exactly where upstream's null check did.
    private static readonly IFormatParameter StartParam =
        FormatParameter<int>.Optional("-start", "start", "index of first sentence", -1);

    private static readonly IFormatParameter EndParam =
        FormatParameter<int>.Optional("-end", "end", "index of last sentence", -1);

    /// <inheritdoc/>
    public override IEnumerable<IFormatParameter> Parameters =>
        [EncodingParam, FormatParameters.Data, FormatParameters.Lang,
            StartParam, EndParam];

    /// <inheritdoc/>
    public override IObjectStream<ChunkSample?> Create(IFormatParameterValues values)
    {
        language = values.Get<string>(FormatParameters.Lang);

        IInputStreamFactory sampleDataIn =
            CreateInputStreamFactory(values.Get<FileInfo>(FormatParameters.Data)!);

        Encoding encoding = FormatParameters.ResolveEncoding(
            values.Get<string>(EncodingParam));

        IObjectStream<string?> lineStream = new PlainTextByLineStream(sampleDataIn, encoding);

        ADChunkSampleStream sampleStream = new ADChunkSampleStream(lineStream);

        int start = values.Get<int>(StartParam);
        if (start > -1)
        {
            sampleStream.Start = start;
        }

        int end = values.Get<int>(EndParam);
        if (end > -1)
        {
            sampleStream.End = end;
        }

        return sampleStream;
    }

    public static void RegisterFactory() =>
        StreamFactoryRegistry.RegisterFactory<ChunkSample?>(
            "ad", new ADChunkSampleStreamFactory());
}
