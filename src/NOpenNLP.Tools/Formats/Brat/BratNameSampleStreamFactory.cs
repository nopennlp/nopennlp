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
using System.IO;
using NOpenNLP.Tools.Namefind;
using NOpenNLP.Tools.Sentdetect;
using NOpenNLP.Tools.Tokenize;
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Formats.Brat;

public class BratNameSampleStreamFactory : AbstractSampleStreamFactory<NameSample?>
{
    private static readonly IFormatParameter BratDataDirParam =
        new FormatParameter<FileInfo>("-bratDataDir", "bratDataDir",
            "location of brat data dir");

    private static readonly IFormatParameter AnnotationConfigParam =
        new FormatParameter<FileInfo>("-annotationConfig", "annConfFile");

    private static readonly IFormatParameter SentenceDetectorModelParam =
        FormatParameter<FileInfo>.Optional("-sentenceDetectorModel", "modelFile");

    private static readonly IFormatParameter TokenizerModelParam =
        FormatParameter<FileInfo>.Optional("-tokenizerModel", "modelFile");

    private static readonly IFormatParameter RuleBasedTokenizerParam =
        FormatParameter<string>.Optional("-ruleBasedTokenizer", "name");

    private static readonly IFormatParameter RecursiveParam =
        FormatParameter<bool>.Optional("-recursive", "value", defaultValue: false);

    private static readonly IFormatParameter NameTypesParam =
        FormatParameter<string>.Optional("-nameTypes", "names");

    // NOpenNLP: upstream's Parameters interface extends nothing, so this format takes
    // neither -data nor -encoding.
    /// <inheritdoc/>
    public override IEnumerable<IFormatParameter> Parameters =>
        [
            BratDataDirParam, AnnotationConfigParam, SentenceDetectorModelParam,
            TokenizerModelParam, RuleBasedTokenizerParam, RecursiveParam, NameTypesParam
        ];

    /// <summary>
    /// Checks that non of the passed values are null.
    /// </summary>
    /// <param name="objects"></param>
    /// <returns>true or false</returns>
    private static bool NotNull(params object?[] objects)
    {
        foreach (object? obj in objects)
        {
            if (obj == null)
                return false;
        }

        return true;
    }

    /// <inheritdoc/>
    public override IObjectStream<NameSample?> Create(IFormatParameterValues values)
    {
        string? ruleBasedTokenizer = values.Get<string>(RuleBasedTokenizerParam);
        FileInfo? tokenizerModelFile = values.Get<FileInfo>(TokenizerModelParam);

        if (NotNull(ruleBasedTokenizer, tokenizerModelFile))
        {
            throw new TerminateToolException(-1, "Either use rule based or statistical tokenizer!");
        }

        // TODO: Provide the file name to the annotation.conf file and implement the parser ...
        AnnotationConfiguration annConfig;
        try
        {
            annConfig = AnnotationConfiguration.Parse(values.Get<FileInfo>(AnnotationConfigParam)!);
        }
        catch (IOException)
        {
            throw new TerminateToolException(1, "Failed to parse annotation.conf file!");
        }

        // TODO: Add an optional parameter to search recursive
        // TODO: How to handle the error here ? terminate the tool? not nice if used by API!
        IObjectStream<BratDocument?> samples;
        try
        {
            // NOpenNLP: -bratDataDir is a FileInfo because upstream declares a
            // java.io.File, which Java uses for both files and directories.
            // BratDocumentStream takes a DirectoryInfo, so the same path is
            // reinterpreted here; the stream still rejects a non-directory as
            // upstream does.
            var bratDataDir =
                new DirectoryInfo(values.Get<FileInfo>(BratDataDirParam)!.FullName);

            samples = new BratDocumentStream(annConfig, bratDataDir,
                values.Get<bool>(RecursiveParam), null);
        }
        catch (IOException e)
        {
            throw new TerminateToolException(-1, e.Message);
        }

        ISentenceDetector sentDetector;

        FileInfo? sentenceDetectorModelFile = values.Get<FileInfo>(SentenceDetectorModelParam);
        if (sentenceDetectorModelFile != null)
        {
            try
            {
                sentDetector = new SentenceDetectorME(new SentenceModel(sentenceDetectorModelFile));
            }
            catch (IOException e)
            {
                throw new TerminateToolException(-1, "Failed to load sentence detector model!", e);
            }
        }
        else
        {
            sentDetector = new NewlineSentenceDetector();
        }

        ITokenizer tokenizer = WhitespaceTokenizer.INSTANCE;

        if (tokenizerModelFile != null)
        {
            try
            {
                tokenizer = new TokenizerME(new TokenizerModel(tokenizerModelFile));
            }
            catch (IOException e)
            {
                throw new TerminateToolException(-1, "Failed to load tokenizer model!", e);
            }
        }
        else if (ruleBasedTokenizer != null)
        {
            string tokenizerName = ruleBasedTokenizer;

            if ("simple".Equals(tokenizerName, StringComparison.Ordinal))
            {
                tokenizer = SimpleTokenizer.INSTANCE;
            }
            else if ("whitespace".Equals(tokenizerName, StringComparison.Ordinal))
            {
                tokenizer = WhitespaceTokenizer.INSTANCE;
            }
            else
            {
                throw new TerminateToolException(-1, "Unkown tokenizer: " + tokenizerName);
            }
        }

        ISet<string>? nameTypes = null;
        string? nameTypesValue = values.Get<string>(NameTypesParam);
        if (nameTypesValue != null)
        {
            // NOpenNLP: upstream's String.split(",") discards trailing empty strings, so
            // a trailing comma is ignored and a value of "," yields an empty array --
            // leaving nameTypes null, which means "accept every type". string.Split would
            // instead produce empty entries, filtering to a type name no annotation has.
            string[] nameTypesArr = StringUtil.SplitDroppingTrailingEmpty(nameTypesValue, ',');
            if (nameTypesArr.Length > 0)
            {
                var types = new JCG.HashSet<string>();
                foreach (string nameType in nameTypesArr)
                {
                    types.Add(nameType.Trim());
                }

                nameTypes = types;
            }
        }

        return new BratNameSampleStream(sentDetector, tokenizer, samples, nameTypes);
    }

    public static void RegisterFactory() =>
        StreamFactoryRegistry.RegisterFactory<NameSample?>("brat",
            new BratNameSampleStreamFactory());
}
