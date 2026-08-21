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
using System.Globalization;
using System.IO;
using NOpenNLP.Tools.Sentdetect;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Formats.Conllu;

public class ConlluSentenceSampleStreamFactory : AbstractSampleStreamFactory<SentenceSample?>
{
    private static readonly IFormatParameter SentencesPerSampleParam =
        new FormatParameter<string>("-sentencesPerSample", "sentencesPerSample",
            "number of sentences per sample");

    /// <inheritdoc/>
    public override IEnumerable<IFormatParameter> Parameters =>
        [FormatParameters.Data, FormatParameters.Encoding, SentencesPerSampleParam];

    /// <inheritdoc/>
    public override IObjectStream<SentenceSample?> Create(IFormatParameterValues values)
    {
        IInputStreamFactory inFactory =
            CreateInputStreamFactory(values.Get<FileInfo>(FormatParameters.Data)!);

        try
        {
            return new ConlluSentenceSampleStream(new ConlluStream(inFactory),
                int.Parse(values.Get<string>(SentencesPerSampleParam)!, CultureInfo.InvariantCulture));
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
        StreamFactoryRegistry.RegisterFactory<SentenceSample?>(
            ConlluPOSSampleStreamFactory.ConlluFormat, new ConlluSentenceSampleStreamFactory());
}
