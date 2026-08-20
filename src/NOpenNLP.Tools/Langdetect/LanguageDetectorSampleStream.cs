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

namespace NOpenNLP.Tools.Langdetect;

/// <summary>
/// This class reads in string encoded training samples, parses them and
/// outputs <see cref="LanguageSample"/> objects.
/// <para/>
/// Format:<br/>
/// Each line contains one sample document.<br/>
/// The language is the first string in the line followed by a tab and the document content.<br/>
/// Sample line: category-string tab-char document line-break-char(s)<br/>
/// </summary>
public class LanguageDetectorSampleStream(IObjectStream<string?> samples)
    : FilterObjectStream<string?, LanguageSample?>(samples)
{
    /// <inheritdoc/>
    /// <exception cref="IOException">if there is an error during reading</exception>
    public override LanguageSample? Read()
    {
        while (samples.Read() is { } sampleString)
        {
            int tabIndex = sampleString.IndexOf('\t');
            if (tabIndex > 0)
            {
                string lang = sampleString.Substring(0, tabIndex);
                string context = sampleString.Substring(tabIndex + 1);

                return new LanguageSample(new Language(lang), context);
            }
        }

        return null;
    }
}
