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
/// Normalizer for emojis.
/// </summary>
public class EmojiCharSequenceNormalizer : ICharSequenceNormalizer
{
    private static readonly EmojiCharSequenceNormalizer INSTANCE = new();

    public static EmojiCharSequenceNormalizer GetInstance() => INSTANCE;

    // NOpenNLP: this matches individual UTF-16 surrogate code units, not whole
    // code points, exactly as the upstream Java pattern does. A single emoji
    // therefore matches as its two surrogates, and the '+' collapses a run of
    // adjacent emoji into one replacement.
    private static readonly Regex EMOJI_REGEX =
        new("[\\uD83C-\\uDBFF\\uDC00-\\uDFFF]+", RegexOptions.Compiled);

    public string Normalize(string text) => EMOJI_REGEX.Replace(text, " ");
}
