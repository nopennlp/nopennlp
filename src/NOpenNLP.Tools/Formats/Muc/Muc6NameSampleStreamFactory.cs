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
using System.Text;
using NOpenNLP.Tools.Formats.Convert;
using NOpenNLP.Tools.Namefind;
using NOpenNLP.Tools.Tokenize;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Formats.Muc;

public class Muc6NameSampleStreamFactory : AbstractSampleStreamFactory<NameSample?>
{
    private static readonly IFormatParameter TokenizerModelParam =
        new FormatParameter<FileInfo>("-tokenizerModel", "modelFile");

    /// <inheritdoc/>
    public override IEnumerable<IFormatParameter> Parameters =>
        [FormatParameters.Data, FormatParameters.Encoding, TokenizerModelParam];

    /// <inheritdoc/>
    public override IObjectStream<NameSample?> Create(IFormatParameterValues values)
    {
        // NOpenNLP: upstream loads through TokenizerModelLoader, which lives in the CLI
        // package. It checks the file, reads it, and maps a failure onto
        // TerminateToolException(-1); the messages and exit code here are its verbatim.
        FileInfo tokenizerModelFile = values.Get<FileInfo>(TokenizerModelParam)!;
        CheckInputFile("Tokenizer model", tokenizerModelFile);

        TokenizerModel tokenizerModel;
        try
        {
            tokenizerModel = new TokenizerModel(tokenizerModelFile);
        }
        catch (InvalidFormatException e)
        {
            throw new TerminateToolException(-1, "Model has invalid format", e);
        }
        catch (IOException e)
        {
            throw new TerminateToolException(-1,
                "IO error while loading model file '" + tokenizerModelFile + "'", e);
        }

        ITokenizer tokenizer = new TokenizerME(tokenizerModel);

        // NOpenNLP: -data is a FileInfo because BasicFormatParams declares a File, which
        // Java uses for both files and directories. DirectorySampleStream takes a
        // DirectoryInfo, so the same path is reinterpreted here; the stream still reports
        // a non-directory the way upstream does.
        var dataDir = new DirectoryInfo(values.Get<FileInfo>(FormatParameters.Data)!.FullName);

        IObjectStream<string?> mucDocStream = new FileToStringSampleStream(
            new DirectorySampleStream(dataDir,
                file => StringUtil.ToLowerCase(file.Name).EndsWith(".sgm", StringComparison.Ordinal),
                false),
            Encoding.UTF8);

        return new MucNameSampleStream(tokenizer, mucDocStream);
    }

    public static void RegisterFactory() =>
        StreamFactoryRegistry.RegisterFactory<NameSample?>("muc6",
            new Muc6NameSampleStreamFactory());
}
