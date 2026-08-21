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
using NOpenNLP.Tools.Tokenize;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Formats.Irishsentencebank;

public class IrishSentenceBankTokenSampleStreamFactory : DetokenizerSampleStreamFactory<TokenSample?>
{
    // NOpenNLP: upstream extends DetokenizerSampleStreamFactory but declares only
    // BasicFormatParams and never calls createDetokenizer, so -detokenizer is not among
    // the parameters here either.
    /// <inheritdoc/>
    public override IEnumerable<IFormatParameter> Parameters =>
        [FormatParameters.Data, FormatParameters.Encoding];

    /// <inheritdoc/>
    public override IObjectStream<TokenSample?> Create(IFormatParameterValues values)
    {
        FileInfo data = values.Get<FileInfo>(FormatParameters.Data)!;

        CheckInputFile("Data", data);

        IrishSentenceBankDocument? isbDoc = null;
        try
        {
            isbDoc = IrishSentenceBankDocument.Parse(data);
        }
        catch (IOException ex)
        {
            // NOpenNLP: CmdLineUtil.handleCreateObjectStreamError lives in the CLI package
            // upstream and only ever rethrows; the message and exit code are its verbatim.
            throw new TerminateToolException(-1,
                "IO Error while creating an Input Stream: " + ex.Message, ex);
        }

        return new IrishSentenceBankTokenSampleStream(isbDoc);
    }

    public static void RegisterFactory() =>
        StreamFactoryRegistry.RegisterFactory<TokenSample?>("irishsentencebank",
            new IrishSentenceBankTokenSampleStreamFactory());
}
