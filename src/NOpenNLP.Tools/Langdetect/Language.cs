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
/// Class for holding the document language and its confidence.
/// </summary>
public class Language(string lang, double confidence = 0)
{
    public string Lang { get; } = lang ?? throw new ArgumentNullException(nameof(lang), "lang must not be null");

    public double Confidence { get; } = confidence;

    // NOpenNLP: J2N's Double.ToString reproduces Java's Double.toString, which
    // renders 0 as "0.0"; the .NET default would render it as "0".
    public override string ToString() =>
        $"{Lang} ({J2N.Numerics.Double.ToString(Confidence, "J", null)})";

    // NOpenNLP: upstream's hashCode includes the confidence while equals ignores
    // it, so two instances can be equal with differing hash codes. Ported as-is
    // because LanguageTest asserts both behaviours.
    public override int GetHashCode() =>
        HashCode.Combine(Lang, Confidence);

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj is Language a)
        {
            return Lang.Equals(a.Lang, StringComparison.Ordinal);
        }

        return false;
    }
}
