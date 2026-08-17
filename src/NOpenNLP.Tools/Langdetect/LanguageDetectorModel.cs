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
using NOpenNLP.Tools.Ml.Model;
using NOpenNLP.Tools.Support;
using NOpenNLP.Tools.Util;
using NOpenNLP.Tools.Util.Model;

namespace NOpenNLP.Tools.Langdetect;

/// <summary>
/// A model for language detection.
/// </summary>
public class LanguageDetectorModel : BaseModel
{
    private const string COMPONENT_NAME = "LanguageDetectorME";
    private const string LANGDETECT_MODEL_ENTRY_NAME = "langdetect.model";

    public LanguageDetectorModel(IMaxentModel langdetectModel,
        IDictionary<string, string>? manifestInfoEntries,
        LanguageDetectorFactory factory)
        : base(COMPONENT_NAME, "und", manifestInfoEntries, factory)
    {
        artifactMap.Put(LANGDETECT_MODEL_ENTRY_NAME, langdetectModel);
        CheckArtifactMap();
    }

    public LanguageDetectorModel(Stream @in)
        : base(COMPONENT_NAME, @in)
    {
    }

    public LanguageDetectorModel(FileInfo modelFile)
        : base(COMPONENT_NAME, modelFile)
    {
    }

    // NOpenNLP: the URL overload has no .NET equivalent in BaseModel.
    // public LanguageDetectorModel(URL modelURL) : base(COMPONENT_NAME, modelURL)
    // {
    // }

    protected override void ValidateArtifactMap()
    {
        base.ValidateArtifactMap();

        if (artifactMap[LANGDETECT_MODEL_ENTRY_NAME] is not AbstractModel)
        {
            throw new InvalidFormatException("Language detector model is incomplete!");
        }
    }

    public virtual LanguageDetectorFactory Factory => (LanguageDetectorFactory)this.toolFactory;

    protected override Type DefaultFactory => typeof(LanguageDetectorFactory);

    public virtual IMaxentModel MaxentModel => (IMaxentModel)artifactMap[LANGDETECT_MODEL_ENTRY_NAME];
}
