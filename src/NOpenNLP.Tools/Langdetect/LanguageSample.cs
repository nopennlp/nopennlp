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

namespace NOpenNLP.Tools.Langdetect;

/// <summary>
/// Class which holds a classified document and its <see cref="Langdetect.Language"/>.
/// </summary>
// NOpenNLP: upstream implements java.io.Serializable, which has no .NET
// counterpart the port needs; model artifacts are written by the serializers in
// NOpenNLP.Tools.Util.Model instead.
// NOpenNLP: upstream stores the context as a CharSequence. .NET has no such
// abstraction over string, so string is used throughout; ILanguageDetector
// already takes the document as a string.
public class LanguageSample(Language language, string context)
{
    public virtual Language Language { get; } = language ?? throw new ArgumentNullException(nameof(language), "language must not be null");

    public virtual string Context { get; } = context ?? throw new ArgumentNullException(nameof(context), "context must not be null");

    /// <inheritdoc/>
    public override string ToString() => $"{Language.Lang}\t{Context}";

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Context, Language);

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj is LanguageSample a)
        {
            return Language.Equals(a.Language)
                && Context.Equals(a.Context);
        }

        return false;
    }
}
