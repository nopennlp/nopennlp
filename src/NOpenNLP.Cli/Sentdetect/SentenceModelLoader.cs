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
using NOpenNLP.Tools.Sentdetect;

namespace NOpenNLP.Tools.Cmdline.Sentdetect;

/// <summary>
/// Loads a Tokenizer Model for the command line tools.
/// <para/>
/// <b>Note:</b> Do not use this class, internal use only!
/// </summary>
// NOpenNLP: upstream is package-private and used only from opennlp.tools.cmdline.sentdetect.
// C# has no package scope, so `internal` keeps it off the public API surface while the
// sentence detector tools in this assembly can still reach it.
internal sealed class SentenceModelLoader : ModelLoader<SentenceModel>
{
    public SentenceModelLoader()
        : base("Sentence Detector")
    {
    }

    /// <inheritdoc/>
    /// <exception cref="Util.InvalidFormatException">if the model has an invalid format</exception>
    protected override SentenceModel LoadModel(Stream modelIn) => new SentenceModel(modelIn);
}
