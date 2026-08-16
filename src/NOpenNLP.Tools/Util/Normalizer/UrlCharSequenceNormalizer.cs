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
/// Normalizer that removes URLs and email addresses.
/// </summary>
public class UrlCharSequenceNormalizer : ICharSequenceNormalizer
{
    private static readonly Regex URL_REGEX =
        new("https?://[-_.?&~;+=/#0-9A-Za-z]+", RegexOptions.Compiled);

    private static readonly Regex MAIL_REGEX =
        new("[-_.0-9A-Za-z]+@[-_0-9A-Za-z]+[-_.0-9A-Za-z]+", RegexOptions.Compiled);

    private static readonly UrlCharSequenceNormalizer INSTANCE = new();

    public static UrlCharSequenceNormalizer GetInstance() => INSTANCE;

    public string Normalize(string text)
    {
        string modified = URL_REGEX.Replace(text, " ");
        return MAIL_REGEX.Replace(modified, " ");
    }
}
