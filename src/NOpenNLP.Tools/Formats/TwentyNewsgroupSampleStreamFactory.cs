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
using NOpenNLP.Tools.Doccat;
using NOpenNLP.Tools.Tokenize;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Formats;

public class TwentyNewsgroupSampleStreamFactory : AbstractSampleStreamFactory<DocumentSample?>
{
    private static readonly IFormatParameter DataDirParam =
        new FormatParameter<FileInfo>("-dataDir", "dataDir",
            "dir containing the 20newsgroup folders");

    private static readonly IFormatParameter TokenizerModelParam =
        FormatParameter<FileInfo>.Optional("-tokenizerModel", "modelFile");

    private static readonly IFormatParameter RuleBasedTokenizerParam =
        FormatParameter<string>.Optional("-ruleBasedTokenizer", "name");

    /// <inheritdoc/>
    public override IEnumerable<IFormatParameter> Parameters =>
        [FormatParameters.Encoding, DataDirParam, TokenizerModelParam, RuleBasedTokenizerParam];

    /// <inheritdoc/>
    public override IObjectStream<DocumentSample?> Create(IFormatParameterValues values)
    {
        ITokenizer tokenizer = WhitespaceTokenizer.INSTANCE;

        FileInfo? tokenizerModel = values.Get<FileInfo>(TokenizerModelParam);
        string? ruleBasedTokenizer = values.Get<string>(RuleBasedTokenizerParam);

        if (tokenizerModel != null)
        {
            try
            {
                tokenizer = new TokenizerME(new TokenizerModel(tokenizerModel));
            }
            catch (IOException e)
            {
                throw new TerminateToolException(-1, "Failed to load tokenizer model!", e);
            }
        }
        else if (ruleBasedTokenizer != null)
        {
            string tokenizerName = ruleBasedTokenizer;

            if ("simple".Equals(tokenizerName, StringComparison.Ordinal))
            {
                tokenizer = SimpleTokenizer.INSTANCE;
            }
            else if ("whitespace".Equals(tokenizerName, StringComparison.Ordinal))
            {
                tokenizer = WhitespaceTokenizer.INSTANCE;
            }
            else
            {
                throw new TerminateToolException(-1, "Unkown tokenizer: " + tokenizerName);
            }
        }

        try
        {
            // NOpenNLP: the -dataDir value names a directory. Upstream types it as
            // java.io.File, which stands for both, and calls toPath() here; the ported
            // stream takes a DirectoryInfo, so the path is rewrapped.
            return new TwentyNewsgroupSampleStream(
                tokenizer, new DirectoryInfo(values.Get<FileInfo>(DataDirParam)!.FullName));
        }
        catch (IOException e)
        {
            throw new TerminateToolException(-1, "IO error while opening sample data: " + e.Message, e);
        }
    }

    public static void RegisterFactory() =>
        StreamFactoryRegistry.RegisterFactory<DocumentSample?>(
            "20newsgroup", new TwentyNewsgroupSampleStreamFactory());
}
