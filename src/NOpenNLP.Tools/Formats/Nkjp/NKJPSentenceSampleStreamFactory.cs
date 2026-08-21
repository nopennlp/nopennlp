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
using NOpenNLP.Tools.Sentdetect;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Formats.Nkjp;

public class NKJPSentenceSampleStreamFactory : AbstractSampleStreamFactory<SentenceSample?>
{
    private static readonly IFormatParameter TextFileParam =
        new FormatParameter<FileInfo>("-textFile", "text", "file containing NKJP text");

    /// <inheritdoc/>
    public override IEnumerable<IFormatParameter> Parameters =>
        [FormatParameters.Data, FormatParameters.Encoding, TextFileParam];

    /// <inheritdoc/>
    public override IObjectStream<SentenceSample?> Create(IFormatParameterValues values)
    {
        FileInfo data = values.Get<FileInfo>(FormatParameters.Data)!;
        FileInfo textFile = values.Get<FileInfo>(TextFileParam)!;

        CheckInputFile("Data", data);

        CheckInputFile("Text", textFile);

        NKJPSegmentationDocument? segDoc = null;
        NKJPTextDocument? textDoc = null;
        try
        {
            segDoc = NKJPSegmentationDocument.Parse(data);
            textDoc = NKJPTextDocument.Parse(textFile);
        }
        catch (IOException ex)
        {
            // NOpenNLP: CmdLineUtil.handleCreateObjectStreamError lives in the CLI package
            // upstream and only ever rethrows; the message and exit code are its verbatim.
            throw new TerminateToolException(-1,
                "IO Error while creating an Input Stream: " + ex.Message, ex);
        }

        return new NKJPSentenceSampleStream(segDoc, textDoc);
    }

    public static void RegisterFactory() =>
        StreamFactoryRegistry.RegisterFactory<SentenceSample?>("nkjp",
            new NKJPSentenceSampleStreamFactory());
}
