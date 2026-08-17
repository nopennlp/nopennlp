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

using System.Text.RegularExpressions;

namespace NOpenNLP.Tools.Util.Normalizer;

/// <summary>
/// Normalizer to shrink repeated spaces / chars.
/// </summary>
public class ShrinkCharSequenceNormalizer : ICharSequenceNormalizer
{
    private static readonly Regex REPEATED_CHAR_REGEX =
        new("(.)\\1{2,}", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // NOpenNLP: Java's \s is ASCII-only by default; ECMAScript keeps .NET from
    // also matching Unicode whitespace such as NBSP.
    private static readonly Regex SPACE_REGEX =
        new("\\s{2,}", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ECMAScript);

    private static readonly ShrinkCharSequenceNormalizer INSTANCE = new();

    // NOpenNLP: Java's String.trim() removes only characters <= U+0020, whereas
    // .NET's Trim() removes all Unicode whitespace and would additionally strip
    // characters such as NBSP (U+00A0) that upstream leaves in the text.
    private static readonly char[] JAVA_TRIM_CHARS =
        BuildJavaTrimChars();

    private static char[] BuildJavaTrimChars()
    {
        char[] chars = new char[0x21];
        for (int i = 0; i <= 0x20; i++)
        {
            chars[i] = (char)i;
        }

        return chars;
    }

    public static ShrinkCharSequenceNormalizer GetInstance() => INSTANCE;

    public string Normalize(string text)
    {
        text = SPACE_REGEX.Replace(text, " ");
        return REPEATED_CHAR_REGEX.Replace(text, "$1$1").Trim(JAVA_TRIM_CHARS);
    }
}
