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
using NOpenNLP.Tools.Postag;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Formats.Ad;

/// <summary>
/// <b>Note:</b> Do not use this class, internal use only!
/// </summary>
public class ADPOSSampleStreamFactory : LanguageSampleStreamFactory<POSSample?>
{
    // NOpenNLP: upstream's Parameters interface declares -encoding itself rather than
    // extending EncodingParameter, so it is required here rather than optional. The value
    // name and description are upstream's verbatim.
    private static readonly IFormatParameter EncodingParam =
        new FormatParameter<string>("-encoding", "charsetName",
            "encoding for reading and writing text, if absent the system default is used.");

    private static readonly IFormatParameter ExpandMEParam =
        FormatParameter<bool>.Optional("-expandME", "expandME",
            "expand multiword expressions.", false);

    private static readonly IFormatParameter IncludeFeaturesParam =
        FormatParameter<bool>.Optional("-includeFeatures", "includeFeatures",
            "combine POS Tags with word features, like number and gender.", false);

    /// <inheritdoc/>
    public override IEnumerable<IFormatParameter> Parameters =>
        [EncodingParam, FormatParameters.Data, FormatParameters.Lang,
            ExpandMEParam, IncludeFeaturesParam];

    /// <inheritdoc/>
    public override IObjectStream<POSSample?> Create(IFormatParameterValues values)
    {
        language = values.Get<string>(FormatParameters.Lang);

        IInputStreamFactory sampleDataIn =
            CreateInputStreamFactory(values.Get<FileInfo>(FormatParameters.Data)!);

        Encoding encoding = FormatParameters.ResolveEncoding(
            values.Get<string>(EncodingParam));

        IObjectStream<string?> lineStream = new PlainTextByLineStream(sampleDataIn, encoding);

        return new ADPOSSampleStream(lineStream,
            values.Get<bool>(ExpandMEParam), values.Get<bool>(IncludeFeaturesParam));
    }

    public static void RegisterFactory() =>
        StreamFactoryRegistry.RegisterFactory<POSSample?>(
            "ad", new ADPOSSampleStreamFactory());
}
