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

public class BioNLP2004NameSampleStreamFactory : AbstractSampleStreamFactory<NameSample?>
{
    private static readonly IFormatParameter TypesParam =
        new FormatParameter<string>("-types", "DNA,protein,cell_type,cell_line,RNA");

    /// <inheritdoc/>
    public override IEnumerable<IFormatParameter> Parameters =>
        [FormatParameters.Data, FormatParameters.Encoding, TypesParam];

    /// <inheritdoc/>
    public override IObjectStream<NameSample?> Create(IFormatParameterValues values)
    {
        string types = values.Get<string>(TypesParam)!;

        int typesToGenerate = 0;

        if (types.Contains("DNA"))
        {
            typesToGenerate = typesToGenerate |
                BioNLP2004NameSampleStream.GenerateDnaEntities;
        }
        else if (types.Contains("protein"))
        {
            typesToGenerate = typesToGenerate |
                BioNLP2004NameSampleStream.GenerateProteinEntities;
        }
        else if (types.Contains("cell_type"))
        {
            typesToGenerate = typesToGenerate |
                BioNLP2004NameSampleStream.GenerateCelltypeEntities;
        }
        else if (types.Contains("cell_line"))
        {
            typesToGenerate = typesToGenerate |
                BioNLP2004NameSampleStream.GenerateCelllineEntities;
        }
        else if (types.Contains("RNA"))
        {
            typesToGenerate = typesToGenerate |
                BioNLP2004NameSampleStream.GenerateRnaEntities;
        }

        try
        {
            return new BioNLP2004NameSampleStream(
                CreateInputStreamFactory(values.Get<FileInfo>(FormatParameters.Data)!), typesToGenerate);
        }
        catch (IOException e)
        {
            throw new InvalidOperationException(e.Message, e);
        }
    }

    public static void RegisterFactory() =>
        StreamFactoryRegistry.RegisterFactory<NameSample?>(
            "bionlp2004", new BioNLP2004NameSampleStreamFactory());
}
