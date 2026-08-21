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
using NOpenNLP.Tools.Namefind;
using NOpenNLP.Tools.Tokenize;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Formats.Convert;

/// <summary>
/// <b>Note:</b> Do not use this class, internal use only!
/// </summary>
public class NameToTokenSampleStreamFactory : DetokenizerSampleStreamFactory<TokenSample?>
{
    /// <inheritdoc/>
    // NOpenNLP: upstream's Parameters interface extends NameSampleDataStreamFactory.Parameters
    // (which is BasicFormatParams) and DetokenizerParameter.
    public override IEnumerable<IFormatParameter> Parameters =>
        [FormatParameters.Data, FormatParameters.Encoding, FormatParameters.Detokenizer];

    /// <inheritdoc/>
    public override IObjectStream<TokenSample?> Create(IFormatParameterValues values)
    {
        // NOpenNLP: see NameToSentenceSampleStreamFactory for why upstream's
        // ArgumentParser.filter call has no counterpart here.
        IObjectStream<NameSample?> nameSampleStream =
            StreamFactoryRegistry.GetFactory<NameSample?>(StreamFactoryRegistry.DefaultFormat)!
                .Create(values);

        return new NameToTokenSampleStream(CreateDetokenizer(values), nameSampleStream);
    }

    public static void RegisterFactory() =>
        StreamFactoryRegistry.RegisterFactory<TokenSample?>(
            "namefinder", new NameToTokenSampleStreamFactory());
}
