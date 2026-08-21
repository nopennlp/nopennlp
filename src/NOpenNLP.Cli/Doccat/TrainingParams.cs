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

namespace NOpenNLP.Tools.Cmdline.Doccat;

/// <summary>
/// TrainingParams for DocCat.
/// <para/>
/// Note: Do not use this class, internal use only!
/// </summary>
// NOpenNLP: upstream is an annotated interface which ArgumentParser reflects over.
// Here each getter becomes a factory method returning the System.CommandLine option it
// described, and the options a tool wants are added to its command. The options
// BasicTrainingParams contributes -- -lang and -params -- live on ToolParams and are
// added there rather than repeated here.
internal static class TrainingParams
{
    public static Option<string?> FeatureGenerators() =>
        new Option<string?>("-featureGenerators")
        {
            Description =
                "Comma separated feature generator classes. Bag of words is used if not specified.",
            HelpName = "fg",
        };

    public static Option<string?> Factory() =>
        ToolParams.Factory(
            "A sub-class of DoccatFactory where to get implementation and resources.");
}
