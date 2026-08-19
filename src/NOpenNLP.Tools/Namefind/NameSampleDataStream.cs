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
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Namefind;

/// <summary>
/// The <see cref="NameSampleDataStream"/> class converts tagged <see cref="string"/>s
/// provided by an <see cref="IObjectStream{T}"/> to <see cref="NameSample"/> objects.
/// It uses text that is one-sentence per line and tokenized
/// with names identified by <c>&lt;START&gt;</c> and <c>&lt;END&gt;</c> tags.
/// </summary>
public class NameSampleDataStream(IObjectStream<string?> @in)
    : FilterObjectStream<string?, NameSample?>(@in)
{
    public const string START_TAG_PREFIX = "<START:";
    public const string START_TAG = "<START>";
    public const string END_TAG = "<END>";

    /// <inheritdoc/>
    /// <exception cref="IOException">if there is an error during reading</exception>
    public override NameSample? Read()
    {
        string? token = samples.Read();

        bool isClearAdaptiveData = false;

        // An empty line indicates the begin of a new article
        // for which the adaptive data in the feature generators
        // must be cleared
        while (token != null && token.Trim().Length == 0)
        {
            isClearAdaptiveData = true;
            token = samples.Read();
        }

        if (token != null)
        {
            return NameSample.Parse(token, isClearAdaptiveData);
        }
        else
        {
            return null;
        }
    }
}
