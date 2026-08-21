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

namespace NOpenNLP.Tools.Cmdline.Postag;

/// <summary>
/// TrainingParameters for Name Finder.
/// <para/>
/// Note: Do not use this class, internal use only!
/// </summary>
// NOpenNLP: the summary above is upstream's, and says "Name Finder" even though this is
// the POS tagger's parameter set; it is reproduced as written. The options
// BasicTrainingParams contributes -- -lang and -params -- live on ToolParams.
internal static class TrainingParams
{
    public static Option<FileInfo?> Featuregen() =>
        new Option<FileInfo?>("-featuregen")
        {
            Description = "The feature generator descriptor file",
            HelpName = "featuregenFile",
        };

    public static Option<DirectoryInfo?> Resources() =>
        new Option<DirectoryInfo?>("-resources")
        {
            Description = "The resources directory",
            HelpName = "resourcesDir",
        };

    public static Option<FileInfo?> Dict() =>
        new Option<FileInfo?>("-dict")
        {
            Description = "The XML tag dictionary file",
            HelpName = "dictionaryPath",
        };

    public static Option<int?> TagDictCutoff() =>
        new Option<int?>("-tagDictCutoff")
        {
            Description =
                "TagDictionary cutoff. If specified will create/expand a mutable TagDictionary",
            HelpName = "tagDictCutoff",
        };

    public static Option<string?> Factory() =>
        ToolParams.Factory(
            "A sub-class of POSTaggerFactory where to get implementation and resources.");
}
