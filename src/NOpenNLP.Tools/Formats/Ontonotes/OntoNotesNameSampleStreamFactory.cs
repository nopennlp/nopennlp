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
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Formats.Ontonotes;

public class OntoNotesNameSampleStreamFactory : AbstractSampleStreamFactory<NameSample?>
{
    /// <inheritdoc/>
    public override IEnumerable<IFormatParameter> Parameters =>
        [OntoNotesFormatParameters.OntoNotesDir];

    /// <inheritdoc/>
    public override IObjectStream<NameSample?> Create(IFormatParameterValues values)
    {
        IObjectStream<FileInfo?> documentStream = new DirectorySampleStream(
            new DirectoryInfo(values.Get<string>(OntoNotesFormatParameters.OntoNotesDir)!),
            file =>
            {
                if (file is FileInfo)
                {
                    return file.Name.EndsWith(".name", StringComparison.Ordinal);
                }

                return file is DirectoryInfo;
            }, true);

        return new OntoNotesNameSampleStream(
            new FileToStringSampleStream(documentStream, Encoding.UTF8));
    }

    public static void RegisterFactory() =>
        StreamFactoryRegistry.RegisterFactory<NameSample?>(
            "ontonotes", new OntoNotesNameSampleStreamFactory());
}
