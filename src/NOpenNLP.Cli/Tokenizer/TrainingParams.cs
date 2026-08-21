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

using System.CommandLine;
using System.IO;

namespace NOpenNLP.Tools.Cmdline.Tokenizer;

/// <summary>
/// TrainingParameters for Tokenizer.
/// <para/>
/// Note: Do not use this class, internal use only!
/// </summary>
// NOpenNLP: the options BasicTrainingParams contributes -- -lang and -params -- live on
// ToolParams; only the ones this package adds are here.
internal static class TrainingParams
{
    // NOpenNLP: parsed leniently, the way Java's Boolean.parseBoolean is, and as a value
    // rather than a flag. An Option<bool> would reject `-alphaNumOpt 0` -- which upstream
    // reads as false -- and would silently take a bare `-alphaNumOpt` as true, training a
    // different model where upstream rejects the missing value. See ToolParams.JavaBoolean.
    public static Option<string?> AlphaNumOpt() =>
        ToolParams.JavaBoolean("-alphaNumOpt",
            "Optimization flag to skip alpha numeric tokens for further tokenization",
            defaultValue: false,
            helpName: "isAlphaNumOpt");

    public static Option<FileInfo?> AbbDict() =>
        new Option<FileInfo?>("-abbDict")
        {
            Description = "abbreviation dictionary in XML format.",
            HelpName = "path",
        };

    public static Option<string?> Factory() =>
        ToolParams.Factory(
            "A sub-class of TokenizerFactory where to get implementation and resources.");
}
