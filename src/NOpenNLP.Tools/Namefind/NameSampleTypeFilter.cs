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
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Namefind;

/// <summary>
/// A stream which removes Name Samples which do not have a certain type.
/// </summary>
public class NameSampleTypeFilter : FilterObjectStream<NameSample?, NameSample?>
{
    private readonly ISet<string> types;

    public NameSampleTypeFilter(string[] types, IObjectStream<NameSample?> samples)
        : base(samples)
        => this.types = new JCG.HashSet<string>(types).AsReadOnly();

    public NameSampleTypeFilter(ISet<string> types, IObjectStream<NameSample?> samples)
        : base(samples)
        => this.types = new JCG.HashSet<string>(types).AsReadOnly();

    /// <inheritdoc/>
    /// <exception cref="IOException">if there is an error during reading</exception>
    public override NameSample? Read()
    {
        NameSample? sample = samples.Read();

        if (sample != null)
        {
            JCG.List<Span> filteredNames = [];

            foreach (var name in sample.Names)
            {
                // NOpenNLP: Span.Type is nullable; a null type is never in the filter set.
                if (name.Type != null && types.Contains(name.Type))
                {
                    filteredNames.Add(name);
                }
            }

            return new NameSample(sample.Id, sample.Sentence,
                [.. filteredNames], null, sample.IsClearAdaptiveDataSet);
        }
        else
        {
            return null;
        }
    }
}
