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
using NOpenNLP.Tools.Namefind;

namespace NOpenNLP.Tools.Cmdline.Namefind;

/// <summary>
/// TrainingParameters for Name Finder.
/// <para/>
/// Note: Do not use this class, internal use only!
/// </summary>
// NOpenNLP: the options BasicTrainingParams contributes -- -lang and -params -- live on
// ToolParams; only the ones this package adds are here.
internal static class TrainingParams
{
    public static Option<string?> Type() =>
        new Option<string?>("-type")
        {
            Description = "The type of the token name finder model",
            HelpName = "modelType",
        };

    public static Option<DirectoryInfo?> Resources() =>
        new Option<DirectoryInfo?>("-resources")
        {
            Description = "The resources directory",
            HelpName = "resourcesDir",
        };

    public static Option<FileInfo?> Featuregen() =>
        new Option<FileInfo?>("-featuregen")
        {
            Description = "The feature generator descriptor file",
            HelpName = "featuregenFile",
        };

    public static Option<string?> NameTypes() =>
        new Option<string?>("-nameTypes")
        {
            Description = "name types to use for training",
            HelpName = "types",
        };

    public static Option<string> SequenceCodec() =>
        new Option<string>("-sequenceCodec")
        {
            Description = "sequence codec used to code name spans",
            HelpName = "codec",
            // NOpenNLP: upstream's default is the literal Java class name
            // "opennlp.tools.namefind.BioCodec". The ported type's full name is used
            // instead, since ExtensionLoader resolves that directly; a user passing the
            // Java name still resolves, because ExtensionLoader translates it.
            DefaultValueFactory = _ => typeof(BioCodec).FullName!,
        };

    public static Option<string?> Factory() =>
        ToolParams.Factory("A sub-class of TokenNameFinderFactory");
}
