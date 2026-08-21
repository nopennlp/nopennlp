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

using System.IO;
using NOpenNLP.Tools.Namefind;
using NOpenNLP.Tools.Tokenize;
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Formats.Muc;

public class MucNameSampleStream : FilterObjectStream<string?, NameSample?>
{
    private readonly ITokenizer tokenizer;

    private readonly JCG.List<NameSample> storedSamples = []; // NOpenNLP: made readonly

    protected internal MucNameSampleStream(ITokenizer tokenizer, IObjectStream<string?> samples)
        : base(samples)
    {
        this.tokenizer = tokenizer;
    }

    /// <inheritdoc/>
    /// <exception cref="IOException">if there is an error during reading</exception>
    public override NameSample? Read()
    {
        if (storedSamples.Count == 0)
        {
            string? document = samples.Read();

            if (document != null)
            {
                // Note: This is a hack to fix invalid formating in
                // some MUC files ...
                document = document.Replace(">>", ">");

                new SgmlParser().Parse(new StringReader(document),
                    new MucNameContentHandler(tokenizer, storedSamples));
            }
        }

        if (storedSamples.Count > 0)
        {
            var sample = storedSamples[0];
            storedSamples.RemoveAt(0);
            return sample;
        }
        else
        {
            return null;
        }
    }
}
