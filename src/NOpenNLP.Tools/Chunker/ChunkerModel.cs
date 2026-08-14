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

// This file has been modified from the original Apache OpenNLP 1.9.1 source:
// translated from Java to C# and adapted for .NET. See NOTICE.

using System;
using NOpenNLP.Tools.Ml;
using NOpenNLP.Tools.Ml.Model;
using NOpenNLP.Tools.Util;
using NOpenNLP.Tools.Util.Model;
using System.Collections.Generic;
using System.IO;
using NOpenNLP.Tools.Support;
using InvalidFormatException = NOpenNLP.Tools.Util.InvalidFormatException;

namespace NOpenNLP.Tools.Chunker;

/// <summary>
/// The <see cref="ChunkerModel"/> is the model used
/// by a learnable <see cref="IChunker"/>.
/// </summary>
/// <seealso cref="ChunkerME"/>
public class ChunkerModel : BaseModel
{
    private const string COMPONENT_NAME = "ChunkerME";
    private const string CHUNKER_MODEL_ENTRY_NAME = "chunker.model";

    public ChunkerModel(string languageCode, ISequenceClassificationModel<string> chunkerModel, Dictionary<string, string> manifestInfoEntries, ChunkerFactory factory)
        : base(COMPONENT_NAME, languageCode, manifestInfoEntries, factory)
    {
        artifactMap.Put(CHUNKER_MODEL_ENTRY_NAME, chunkerModel);
        CheckArtifactMap();
    }

    public ChunkerModel(string languageCode, IMaxentModel chunkerModel, Dictionary<string, string> manifestInfoEntries, ChunkerFactory factory)
        : this(languageCode, chunkerModel, ChunkerME.DEFAULT_BEAM_SIZE, manifestInfoEntries, factory)
    {
    }

    public ChunkerModel(string languageCode, IMaxentModel chunkerModel, int beamSize, Dictionary<string, string> manifestInfoEntries, ChunkerFactory factory)
        : base(COMPONENT_NAME, languageCode, manifestInfoEntries, factory)
    {
        artifactMap.Put(CHUNKER_MODEL_ENTRY_NAME, chunkerModel);
        Properties manifest = (Properties)artifactMap[MANIFEST_ENTRY];
        manifest[BeamSearch.BEAM_SIZE_PARAMETER] = beamSize.ToString();
        CheckArtifactMap();
    }

    public ChunkerModel(string languageCode, IMaxentModel chunkerModel, ChunkerFactory factory) : this(languageCode, chunkerModel, null, factory)
    {
    }

    public ChunkerModel(Stream @in) : base(COMPONENT_NAME, @in)
    {
    }

    public ChunkerModel(FileInfo modelFile) : base(COMPONENT_NAME, modelFile)
    {
    }

    public ChunkerModel(string modelPath) : this(new FileInfo(modelPath))
    {
    }

    // public ChunkerModel(Uri modelURL) : base(COMPONENT_NAME, modelURL)
    // {
    // }

    protected override void ValidateArtifactMap()
    {
        base.ValidateArtifactMap();

        if (artifactMap[CHUNKER_MODEL_ENTRY_NAME] is not AbstractModel)
        {
            // NOpenNLP: "Chunker" here names the tool, not the IChunker interface.
            throw new InvalidFormatException("Chunker model is incomplete!");
        }

        // Since 1.8.0 we changed the ChunkerFactory signature. This will check the if the model
        // declares a not default factory, and if yes, check if it was created before 1.8
        if (GetManifestProperty(FACTORY_NAME) != null
            && !string.Equals(GetManifestProperty(FACTORY_NAME), "opennlp.tools.chunker.ChunkerFactory")
            && Version is { Major: <= 1, Minor: < 8 })
        {
            // NOpenNLP: "Chunker" here names the tool, not the IChunker interface;
            // the message must match upstream's text, which callers assert on.
            throw new InvalidFormatException($"The Chunker factory '{GetManifestProperty(FACTORY_NAME)}' is no longer compatible. Please update it to match the latest ChunkerFactory.");
        }
    }

    /// <summary>
    /// </summary>
    /// <remarks>NOpenNLP: This was <c>getChunkerModel()</c> in Java.</remarks>
    [Obsolete("Use ChunkerSequenceModel instead. This property will be removed soon.")]
    public virtual IMaxentModel? ChunkerModelValue
        => artifactMap[CHUNKER_MODEL_ENTRY_NAME] as IMaxentModel;

    public virtual ISequenceClassificationModel<TokenTag>? ChunkerSequenceModel
    {
        get
        {
            Properties manifest = (Properties)artifactMap[MANIFEST_ENTRY];
            if (artifactMap[CHUNKER_MODEL_ENTRY_NAME] is IMaxentModel)
            {
                string? beamSizeString = manifest.GetProperty(BeamSearch.BEAM_SIZE_PARAMETER);
                int beamSize = ChunkerME.DEFAULT_BEAM_SIZE;
                if (beamSizeString != null)
                {
                    beamSize = int.Parse(beamSizeString);
                }

                return new BeamSearch<TokenTag>(beamSize, (IMaxentModel)artifactMap[CHUNKER_MODEL_ENTRY_NAME]);
            }
            else if (artifactMap[CHUNKER_MODEL_ENTRY_NAME] is ISequenceClassificationModel<TokenTag>)
            {
                return (ISequenceClassificationModel<TokenTag>)artifactMap[CHUNKER_MODEL_ENTRY_NAME];
            }
            else
            {
                return null;
            }
        }
    }

    protected override Type DefaultFactory => typeof(ChunkerFactory);

    public virtual ChunkerFactory Factory => (ChunkerFactory)this.toolFactory;
}
