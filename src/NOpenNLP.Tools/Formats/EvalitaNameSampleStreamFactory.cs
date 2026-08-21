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
using NOpenNLP.Tools.Namefind;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Formats;

/// <summary>
/// <b>Note:</b> Do not use this class, internal use only!
/// </summary>
public class EvalitaNameSampleStreamFactory : LanguageSampleStreamFactory<NameSample?>
{
    // NOpenNLP: upstream's Parameters interface redeclares getLang() with its own
    // valueName rather than extending LanguageParams, so this is a distinct descriptor
    // and not the shared FormatParameters.Lang.
    private static readonly IFormatParameter LangParam =
        new FormatParameter<string>("-lang", "it");

    private static readonly IFormatParameter TypesParam =
        new FormatParameter<string>("-types", "per,loc,org,gpe");

    /// <inheritdoc/>
    public override IEnumerable<IFormatParameter> Parameters =>
        [FormatParameters.Data, FormatParameters.Encoding, LangParam, TypesParam];

    /// <inheritdoc/>
    public override IObjectStream<NameSample?> Create(IFormatParameterValues values)
    {
        string? langValue = values.Get<string>(LangParam);

        EvalitaNameSampleStream.Language lang;
        if ("it".Equals(langValue, StringComparison.Ordinal))
        {
            lang = EvalitaNameSampleStream.Language.IT;
            language = langValue;
        }
        else
        {
            throw new TerminateToolException(1, "Unsupported language: " + langValue);
        }

        string types = values.Get<string>(TypesParam)!;

        int typesToGenerate = 0;

        if (types.Contains("per"))
        {
            typesToGenerate = typesToGenerate |
                EvalitaNameSampleStream.GeneratePersonEntities;
        }
        if (types.Contains("org"))
        {
            typesToGenerate = typesToGenerate |
                EvalitaNameSampleStream.GenerateOrganizationEntities;
        }
        if (types.Contains("loc"))
        {
            typesToGenerate = typesToGenerate |
                EvalitaNameSampleStream.GenerateLocationEntities;
        }
        if (types.Contains("gpe"))
        {
            typesToGenerate = typesToGenerate |
                EvalitaNameSampleStream.GenerateGpeEntities;
        }

        try
        {
            return new EvalitaNameSampleStream(lang,
                CreateInputStreamFactory(values.Get<FileInfo>(FormatParameters.Data)!), typesToGenerate);
        }
        catch (IOException e)
        {
            // NOpenNLP: upstream calls CmdLineUtil.createObjectStreamError, which lives in
            // the CLI package; its exit code and message are reproduced verbatim here.
            throw new TerminateToolException(-1,
                "IO Error while creating an Input Stream: " + e.Message, e);
        }
    }

    public static void RegisterFactory() =>
        StreamFactoryRegistry.RegisterFactory<NameSample?>(
            "evalita", new EvalitaNameSampleStreamFactory());
}
