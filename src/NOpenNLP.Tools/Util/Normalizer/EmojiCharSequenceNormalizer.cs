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

using System.Text;

namespace NOpenNLP.Tools.Util.Normalizer;

/// <summary>
/// Normalizer for emojis.
/// </summary>
public class EmojiCharSequenceNormalizer : ICharSequenceNormalizer
{
    private static readonly EmojiCharSequenceNormalizer INSTANCE = new();

    public static EmojiCharSequenceNormalizer GetInstance() => INSTANCE;

    // NOpenNLP-specific: upstream compiles "[\uD83C-\uDBFF\uDC00-\uDFFF]+", but Java's
    // Pattern joins the adjacent \uDBFF\uDC00 escapes into the single code point
    // U+10FC00. What it actually compiles is the range U+D83C..U+10FC00, a literal
    // '-', and U+DFFF, matched by code point: hyphens, everything from U+D83C to
    // U+FFFF, and supplementary characters up to U+10FC00. .NET's regex engine works
    // on UTF-16 code units and cannot express that class, so it is matched by hand to
    // keep the language detector's features identical to upstream's. Verified against
    // a real JDK.
    private const int MIN_CODE_POINT = 0xD83C;
    private const int MAX_CODE_POINT = 0x10FC00;

    public string Normalize(string text)
    {
        StringBuilder? result = null;
        int copied = 0;
        int i = 0;

        while (i < text.Length)
        {
            if (!Matches(text, i, out int length))
            {
                i += length;
                continue;
            }

            // Collapse a run of matching code points into one space, as '+' does.
            int start = i;
            do
            {
                i += length;
            }
            while (i < text.Length && Matches(text, i, out length));

            result ??= new StringBuilder(text.Length);
            result.Append(text, copied, start - copied).Append(' ');
            copied = i;
        }

        return result is null ? text : result.Append(text, copied, text.Length - copied).ToString();
    }

    // Whether the code point at index is in the class. length is the number of chars it
    // occupies either way, so a surrogate pair outside the class is never split.
    private static bool Matches(string text, int index, out int length)
    {
        char c = text[index];
        if (char.IsHighSurrogate(c) && index + 1 < text.Length && char.IsLowSurrogate(text[index + 1]))
        {
            length = 2;
            return char.ConvertToUtf32(c, text[index + 1]) <= MAX_CODE_POINT;
        }

        // A lone surrogate is a code point of its own, as it is to Java.
        length = 1;
        return c == '-' || c >= MIN_CODE_POINT;
    }
}
