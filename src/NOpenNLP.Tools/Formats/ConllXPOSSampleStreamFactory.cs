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
using System.Text;
using NOpenNLP.Tools.Postag;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Formats;

/// <summary>
/// <b>Note:</b> Do not use this class, internal use only!
/// </summary>
public class ConllXPOSSampleStreamFactory : AbstractSampleStreamFactory<POSSample?>
{
    public const string ConllxFormat = "conllx";

    /// <inheritdoc/>
    public override IEnumerable<IFormatParameter> Parameters =>
        [FormatParameters.Data, FormatParameters.Encoding];

    /// <inheritdoc/>
    public override IObjectStream<POSSample?> Create(IFormatParameterValues values)
    {
        IInputStreamFactory inFactory =
            CreateInputStreamFactory(values.Get<FileInfo>(FormatParameters.Data)!);

        // NOpenNLP: upstream replaces System.out with a UTF-8 PrintStream here, because the
        // JVM's default console encoding may not render the corpus. That is a side effect on
        // global state from inside a stream factory, and .NET has no equivalent need, so it
        // is dropped along with the UnsupportedEncodingException branch that guarded it.
        return new ConllXPOSSampleStream(inFactory, Encoding.UTF8);
    }

    public static void RegisterFactory() =>
        StreamFactoryRegistry.RegisterFactory<POSSample?>(
            ConllxFormat, new ConllXPOSSampleStreamFactory());
}
