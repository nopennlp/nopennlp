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
/// Normalizer for Twitter character sequences.
/// </summary>
public class TwitterCharSequenceNormalizer : ICharSequenceNormalizer
{
    // NOpenNLP: Java's \S is ASCII-only by default, so ECMAScript is used to
    // match its meaning rather than .NET's Unicode-aware default.
    private static readonly Regex HASH_USER_REGEX =
        new("[#@]\\S+", RegexOptions.Compiled | RegexOptions.ECMAScript);

    private static readonly Regex RT_REGEX =
        new("\\b(rt[ :])+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex FACE_REGEX =
        new("[:;x]-?[()dop]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex LAUGH_REGEX =
        new("([hj])+([aieou])+(\\1+\\2+)+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly TwitterCharSequenceNormalizer INSTANCE = new();

    public static TwitterCharSequenceNormalizer GetInstance() => INSTANCE;

    public string Normalize(string text)
    {
        string modified = HASH_USER_REGEX.Replace(text, " ");
        modified = RT_REGEX.Replace(modified, " ");
        modified = FACE_REGEX.Replace(modified, " ");
        return LAUGH_REGEX.Replace(modified, "$1$2$1$2");
    }
}
