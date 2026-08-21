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

namespace NOpenNLP.Tools.Cmdline.Langdetect;

/// <summary>
/// TrainingParams for Language Detector.
/// <para/>
/// Note: Do not use this class, internal use only!
/// </summary>
// NOpenNLP: unlike the other packages' TrainingParams, this one does NOT extend
// BasicTrainingParams -- the language detector infers the language rather than being
// told it -- so there is no -lang here. It declares -params itself, with exactly the
// description BasicTrainingParams uses, so ToolParams.Params supplies it.
internal static class TrainingParams
{
    public static Option<string?> Params() => ToolParams.Params();

    public static Option<string?> Factory() =>
        ToolParams.Factory(
            "A sub-class of LanguageDetectorFactory where to get implementation and resources.");
}
