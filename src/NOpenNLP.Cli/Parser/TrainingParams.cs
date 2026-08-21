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

namespace NOpenNLP.Tools.Cmdline.Parser;

/// <summary>
/// TrainingParams for Parser.
/// <para/>
/// Note: Do not use this class, internal use only!
/// </summary>
// NOpenNLP: the options BasicTrainingParams contributes -- -lang and -params -- live on
// ToolParams; only the ones this package adds are here.
internal static class TrainingParams
{
    public static Option<string> ParserType() =>
        new Option<string>("-parserType")
        {
            Description = "one of CHUNKING or TREEINSERT, default is CHUNKING.",
            HelpName = "CHUNKING|TREEINSERT",
            DefaultValueFactory = _ => "CHUNKING",
        };

    public static Option<string?> HeadRulesSerializerImpl() =>
        new Option<string?>("-headRulesSerializerImpl")
        {
            Description = "head rules artifact serializer class name",
            HelpName = "className",
        };

    public static Option<FileInfo> HeadRules() =>
        new Option<FileInfo>("-headRules")
        {
            Description = "head rules file.",
            HelpName = "headRulesFile",
            Required = true,
        };

    public static Option<bool> Fun() =>
        new Option<bool>("-fun")
        {
            Description = "Learn to generate function tags.",
            HelpName = "true|false",
            DefaultValueFactory = _ => false,
        };
}
