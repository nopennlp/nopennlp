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
using System.Collections.ObjectModel;
using System.Text;
using NOpenNLP.Tools.Support;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Doccat;

/// <summary>
/// Class which holds a classified document and its category.
/// </summary>
// NOpenNLP: upstream implements java.io.Serializable, which has no .NET
// counterpart the port needs; model artifacts are written by the serializers in
// NOpenNLP.Tools.Util.Model instead.
public class DocumentSample
{
    private readonly IList<string> text;

    public DocumentSample(string category, string[] text, IDictionary<string, object>? extraInformation = null)
    {
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text), "text must not be null");
        }

        Category = category ?? throw new ArgumentNullException(nameof(category), "category must not be null");
        this.text = new ReadOnlyCollection<string>(new JCG.List<string>(text));

        // NOpenNLP: upstream uses Collections.emptyMap(), which is immutable; the
        // .NET counterpart is an empty ReadOnlyDictionary. A supplied map is stored
        // as-is, matching upstream.
        ExtraInformation = extraInformation
            ?? new ReadOnlyDictionary<string, object>(new JCG.Dictionary<string, object>());
    }

    public virtual string Category { get; }

    public virtual string[] Text => [.. text];

    public virtual IDictionary<string, object> ExtraInformation { get; }

    /// <inheritdoc/>
    public override string ToString()
    {
        StringBuilder sampleString = new();

        sampleString.Append(Category).Append('\t');

        foreach (string s in text)
        {
            sampleString.Append(s).Append(' ');
        }

        if (sampleString.Length > 0)
        {
            // remove last space
            sampleString.Length -= 1;
        }

        return sampleString.ToString();
    }

    /// <inheritdoc/>
    public override int GetHashCode() =>
        HashCode.Combine(Category, Arrays.GetHashCode(Text));

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj is DocumentSample a)
        {
            return Category.Equals(a.Category)
                && Arrays.Equals(Text, a.Text);
        }

        return false;
    }
}
