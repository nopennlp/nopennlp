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

namespace NOpenNLP.Tools.Doccat;

/// <summary>
/// A model for document categorization
/// </summary>
public class DoccatModel : BaseModel
{
    private const string COMPONENT_NAME = "DocumentCategorizerME";
    private const string DOCCAT_MODEL_ENTRY_NAME = "doccat.model";

    public DoccatModel(string languageCode, IMaxentModel doccatModel,
        IDictionary<string, string>? manifestInfoEntries, DoccatFactory factory)
        : base(COMPONENT_NAME, languageCode, manifestInfoEntries, factory)
    {
        artifactMap.Put(DOCCAT_MODEL_ENTRY_NAME, doccatModel);
        CheckArtifactMap();
    }

    public DoccatModel(Stream @in)
        : base(COMPONENT_NAME, @in)
    {
    }

    public DoccatModel(FileInfo modelFile)
        : base(COMPONENT_NAME, modelFile)
    {
    }

    // NOpenNLP: the URL overload has no .NET equivalent in BaseModel.
    // public DoccatModel(URL modelURL) : base(COMPONENT_NAME, modelURL)
    // {
    // }

    protected override void ValidateArtifactMap()
    {
        base.ValidateArtifactMap();

        if (artifactMap[DOCCAT_MODEL_ENTRY_NAME] is not AbstractModel)
        {
            throw new InvalidFormatException("Doccat model is incomplete!");
        }
    }

    public virtual DoccatFactory Factory => (DoccatFactory)this.toolFactory;

    protected override Type DefaultFactory => typeof(DoccatFactory);

    public virtual IMaxentModel MaxentModel => (IMaxentModel)artifactMap[DOCCAT_MODEL_ENTRY_NAME];
}
