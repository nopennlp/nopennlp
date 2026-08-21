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
using NOpenNLP.Tools.Formats.Convert;
using NOpenNLP.Tools.Namefind;
using NOpenNLP.Tools.Tokenize;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Formats.Ad;

/// <summary>
/// <b>Note:</b> Do not use this class, internal use only!
/// </summary>
public class ADTokenSampleStreamFactory : DetokenizerSampleStreamFactory<TokenSample?>
{
    // NOpenNLP: upstream looks the "ad" NameSample factory up in the registry and passes
    // it the subset of the command line that ADNameSampleStreamFactory.Parameters
    // declares, via ArgumentParser.filter. Here the parameter values are already keyed by
    // descriptor, so the name factory reads only the ones it declared from the same values
    // object, and holding the instance directly removes the dependency on registration
    // order and the nullable lookup it would otherwise require.
    private static readonly ADNameSampleStreamFactory NameSampleStreamFactory =
        new ADNameSampleStreamFactory();

    /// <inheritdoc/>
    public override IEnumerable<IFormatParameter> Parameters =>
        [ADNameSampleStreamFactory.EncodingParam, FormatParameters.Data,
            ADNameSampleStreamFactory.SplitHyphenatedTokensParam, FormatParameters.Lang,
            FormatParameters.Detokenizer];

    /// <inheritdoc/>
    public override IObjectStream<TokenSample?> Create(IFormatParameterValues values)
    {
        IObjectStream<NameSample?> samples = NameSampleStreamFactory.Create(values);

        return new NameToTokenSampleStream(CreateDetokenizer(values), samples);
    }

    public static void RegisterFactory() =>
        StreamFactoryRegistry.RegisterFactory<TokenSample?>(
            "ad", new ADTokenSampleStreamFactory());
}
