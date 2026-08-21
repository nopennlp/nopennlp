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
using NOpenNLP.Tools.Lemmatizer;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Formats.Conllu;

/// <summary>
/// <b>Note:</b> Do not use this class, internal use only!
/// </summary>
public class ConlluLemmaSampleStreamFactory : AbstractSampleStreamFactory<LemmaSample?>
{
    private static readonly IFormatParameter TagsetParam =
        FormatParameter<string>.Optional("-tagset", "tagset",
            "u|x u for unified tags and x for language-specific part-of-speech tags", "u");

    /// <inheritdoc/>
    public override IEnumerable<IFormatParameter> Parameters =>
        [FormatParameters.Data, FormatParameters.Encoding, TagsetParam];

    /// <inheritdoc/>
    public override IObjectStream<LemmaSample?> Create(IFormatParameterValues values)
    {
        string tagsetName = values.Get<string>(TagsetParam)!;

        ConlluTagset tagset = tagsetName switch
        {
            "u" => ConlluTagset.U,
            "x" => ConlluTagset.X,
            _ => throw new TerminateToolException(-1, "Unkown tagset parameter: " + tagsetName),
        };

        IInputStreamFactory inFactory =
            CreateInputStreamFactory(values.Get<FileInfo>(FormatParameters.Data)!);

        try
        {
            return new ConlluLemmaSampleStream(new ConlluStream(inFactory), tagset);
        }
        catch (IOException e)
        {
            // That will throw an exception
            // NOpenNLP: CmdLineUtil.handleCreateObjectStreamError lives in the CLI package
            // upstream and only ever rethrows; the message and exit code are its verbatim.
            throw new TerminateToolException(-1,
                "IO Error while creating an Input Stream: " + e.Message, e);
        }
    }

    public static void RegisterFactory() =>
        StreamFactoryRegistry.RegisterFactory<LemmaSample?>(
            ConlluPOSSampleStreamFactory.ConlluFormat, new ConlluLemmaSampleStreamFactory());
}
