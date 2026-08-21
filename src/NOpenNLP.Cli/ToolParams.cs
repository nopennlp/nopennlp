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

namespace NOpenNLP.Tools.Cmdline;

/// <summary>
/// The command line options shared by the tools.
/// </summary>
// NOpenNLP: these are the interfaces in opennlp.tools.cmdline.params -- LanguageParams,
// BasicTrainingParams, TrainingToolParams, EvaluatorParams, CVParams,
// DetailedFMeasureEvaluatorParams, FineGrainedEvaluatorParams and EncodingParameter.
// Upstream composes them by interface inheritance and reads them through a reflection
// proxy; here each is a factory method returning the System.CommandLine option, and a
// tool composes them by adding the ones it wants. Every name, value name, description
// and default is copied verbatim from the upstream annotations, since they are the
// user-facing contract.
//
// Each option is created fresh per call rather than shared as a static: System.CommandLine
// binds a parsed value to the Option instance, so one shared instance across the tools
// would leak a value from one invocation into the next.
public static class ToolParams
{
    /// <summary>From <c>LanguageParams</c>.</summary>
    public static Option<string> Lang() =>
        new Option<string>("-lang")
        {
            Description = "language which is being processed.",
            HelpName = "language",
            Required = true,
        };

    /// <summary>From <c>BasicTrainingParams</c>.</summary>
    public static Option<string?> Params() =>
        new Option<string?>("-params")
        {
            Description = "training parameters file.",
            HelpName = "paramsFile",
        };

    /// <summary>
    /// From <c>TrainingToolParams</c> (<paramref name="valueName"/> <c>modelFile</c>) and
    /// <c>EvaluatorParams</c> (<c>model</c>), which describe the same <c>-model</c>
    /// option with different value names and descriptions.
    /// </summary>
    public static Option<FileInfo> Model(string valueName, string description) =>
        new Option<FileInfo>("-model")
        {
            Description = description,
            HelpName = valueName,
            Required = true,
        };

    /// <summary>From <c>TrainingToolParams</c>.</summary>
    public static Option<FileInfo> ModelForTraining() =>
        Model("modelFile", "output model file.");

    /// <summary>From <c>EvaluatorParams</c>.</summary>
    public static Option<FileInfo> ModelForEvaluation() =>
        Model("model", "the model file to be evaluated.");

    /// <summary>From <c>BasicFormatParams</c>.</summary>
    public static Option<FileInfo> Data() =>
        new Option<FileInfo>("-data")
        {
            Description = "data to be used, usually a file name.",
            HelpName = "sampleData",
            Required = true,
        };

    /// <summary>From <c>EncodingParameter</c>.</summary>
    public static Option<string> Encoding() =>
        new Option<string>("-encoding")
        {
            Description = "encoding for reading and writing text, if absent the system default is used.",
            HelpName = "charsetName",
            DefaultValueFactory = _ => Formats.FormatParameters.DefaultCharset,
        };

    /// <summary>From <c>CVParams</c>.</summary>
    public static Option<int> Folds() =>
        new Option<int>("-folds")
        {
            Description = "number of folds, default is 10.",
            HelpName = "num",
            DefaultValueFactory = _ => 10,
        };

    /// <summary>From <c>EvaluatorParams</c> and <c>CVParams</c>.</summary>
    public static Option<bool> Misclassified() =>
        new Option<bool>("-misclassified")
        {
            Description = "if true will print false negatives and false positives.",
            HelpName = "true|false",
            DefaultValueFactory = _ => false,
        };

    /// <summary>From <c>DetailedFMeasureEvaluatorParams</c>.</summary>
    // NOpenNLP: upstream marks this @Deprecated with the note "this will be removed in
    // 1.8.0"; it is still present and still defaults to true in 1.9.4, so it is ported
    // as it stands rather than dropped.
    public static Option<bool> DetailedF() =>
        new Option<bool>("-detailedF")
        {
            Description = "if true (default) will print detailed FMeasure results.",
            HelpName = "true|false",
            DefaultValueFactory = _ => true,
        };

    /// <summary>From <c>FineGrainedEvaluatorParams</c>.</summary>
    public static Option<FileInfo?> ReportOutputFile() =>
        new Option<FileInfo?>("-reportOutputFile")
        {
            Description = "the path of the fine-grained report file.",
            HelpName = "outputFile",
        };

    /// <summary>
    /// The <c>-factory</c> option, whose description names a different base class in each
    /// package's <c>TrainingParams</c>.
    /// </summary>
    public static Option<string?> Factory(string description) =>
        new Option<string?>("-factory")
        {
            Description = description,
            HelpName = "factoryName",
        };
}
