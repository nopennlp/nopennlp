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
using System.Globalization;
using System.IO;
using NOpenNLP.Tools.Langdetect;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Formats.Leipzig;

/// <summary>
/// <b>Note:</b> Do not use this class, internal use only!
/// </summary>
public class LeipzigLanguageSampleStreamFactory : AbstractSampleStreamFactory<LanguageSample?>
{
    private static readonly IFormatParameter SentencesDirParam =
        new FormatParameter<FileInfo>("-sentencesDir", "sentencesDir",
            "dir with Leipig sentences to be used");

    private static readonly IFormatParameter SentencesPerSampleParam =
        new FormatParameter<string>("-sentencesPerSample", "sentencesPerSample",
            "number of sentences per sample");

    private static readonly IFormatParameter SamplesPerLanguageParam =
        new FormatParameter<string>("-samplesPerLanguage", "samplesPerLanguage",
            "number of samples per language");

    private static readonly IFormatParameter SamplesToSkipParam =
        FormatParameter<string>.Optional("-samplesToSkip", "samplesToSkip",
            "number of samples to skip before returning", "0");

    // NOpenNLP: upstream's Parameters extends EncodingParameter rather than
    // BasicFormatParams, so this format takes -encoding but no -data.
    /// <inheritdoc/>
    public override IEnumerable<IFormatParameter> Parameters =>
        [
            FormatParameters.Encoding, SentencesDirParam, SentencesPerSampleParam,
            SamplesPerLanguageParam, SamplesToSkipParam
        ];

    /// <inheritdoc/>
    public override IObjectStream<LanguageSample?> Create(IFormatParameterValues values)
    {
        // NOpenNLP: the parameter is a FileInfo because upstream declares a java.io.File,
        // which Java uses for both files and directories. LeipzigLanguageSampleStream takes
        // a DirectoryInfo, so the same path is reinterpreted here.
        var sentencesFileDir =
            new DirectoryInfo(values.Get<FileInfo>(SentencesDirParam)!.FullName);

        int sentencesPerSample = int.Parse(
            values.Get<string>(SentencesPerSampleParam)!, CultureInfo.InvariantCulture);
        int samplesPerLanguage = int.Parse(
            values.Get<string>(SamplesPerLanguageParam)!, CultureInfo.InvariantCulture);
        int samplesToSkip = int.Parse(
            values.Get<string>(SamplesToSkipParam)!, CultureInfo.InvariantCulture);

        try
        {
            return new SampleSkipStream<LanguageSample>(
                new SampleShuffleStream<LanguageSample>(
                    new LeipzigLanguageSampleStream(sentencesFileDir, sentencesPerSample,
                        samplesPerLanguage + samplesToSkip)),
                samplesToSkip);
        }
        catch (IOException e)
        {
            throw new TerminateToolException(-1, "IO error while opening sample data.", e);
        }
    }

    public static void RegisterFactory() =>
        StreamFactoryRegistry.RegisterFactory<LanguageSample?>("leipzig",
            new LeipzigLanguageSampleStreamFactory());
}
