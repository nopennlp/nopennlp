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

namespace NOpenNLP.Tools.Lemmatizer;

/// <summary>
/// The <see cref="LemmatizerModel"/> is the model used
/// by a learnable <see cref="ILemmatizer"/>.
/// </summary>
/// <seealso cref="LemmatizerME"/>
public class LemmatizerModel : BaseModel
{
    private const string COMPONENT_NAME = "StatisticalLemmatizer";
    private const string LEMMATIZER_MODEL_ENTRY_NAME = "lemmatizer.model";

    public LemmatizerModel(string languageCode, ISequenceClassificationModel<string> lemmatizerModel, Dictionary<string, string> manifestInfoEntries, LemmatizerFactory factory)
        : base(COMPONENT_NAME, languageCode, manifestInfoEntries, factory)
    {
        artifactMap.Put(LEMMATIZER_MODEL_ENTRY_NAME, lemmatizerModel);
        CheckArtifactMap();
    }

    public LemmatizerModel(string languageCode, IMaxentModel lemmatizerModel, Dictionary<string, string> manifestInfoEntries, LemmatizerFactory factory)
        : this(languageCode, lemmatizerModel, LemmatizerME.DEFAULT_BEAM_SIZE, manifestInfoEntries, factory)
    {
    }

    public LemmatizerModel(string languageCode, IMaxentModel lemmatizerModel, int beamSize, Dictionary<string, string> manifestInfoEntries, LemmatizerFactory factory)
        : base(COMPONENT_NAME, languageCode, manifestInfoEntries, factory)
    {
        artifactMap.Put(LEMMATIZER_MODEL_ENTRY_NAME, lemmatizerModel);
        Properties manifest = (Properties)artifactMap[MANIFEST_ENTRY];
        manifest.Put(BeamSearch.BEAM_SIZE_PARAMETER, beamSize.ToString());
        CheckArtifactMap();
    }

    public LemmatizerModel(string languageCode, IMaxentModel lemmatizerModel, LemmatizerFactory factory)
        : this(languageCode, lemmatizerModel, null, factory)
    {
    }

    public LemmatizerModel(Stream @in)
        : base(COMPONENT_NAME, @in)
    {
    }

    public LemmatizerModel(FileInfo modelFile)
        : base(COMPONENT_NAME, modelFile)
    {
    }

    public LemmatizerModel(string modelPath)
        : this(new FileInfo(modelPath))
    {
    }

    // public LemmatizerModel(URL modelURL)
    //     : base(COMPONENT_NAME, modelURL)
    // {
    // }

    protected override void ValidateArtifactMap()
    {
        base.ValidateArtifactMap();
        if (!(artifactMap[LEMMATIZER_MODEL_ENTRY_NAME] is AbstractModel))
        {
            throw new InvalidFormatException("ILemmatizer model is incomplete!");
        }
    }

    public virtual ISequenceClassificationModel<string> GetLemmatizerSequenceModel()
    {
        Properties manifest = (Properties)artifactMap[MANIFEST_ENTRY];
        if (artifactMap[LEMMATIZER_MODEL_ENTRY_NAME] is IMaxentModel)
        {
            string beamSizeString = manifest.GetProperty(BeamSearch.BEAM_SIZE_PARAMETER);
            int beamSize = LemmatizerME.DEFAULT_BEAM_SIZE;
            if (beamSizeString != null)
            {
                beamSize = int.Parse(beamSizeString);
            }

            return new BeamSearch<string>(beamSize, (IMaxentModel)artifactMap[LEMMATIZER_MODEL_ENTRY_NAME]);
        }
        else if (artifactMap[LEMMATIZER_MODEL_ENTRY_NAME] is ISequenceClassificationModel<string>)
        {
            return (ISequenceClassificationModel<string>)artifactMap[LEMMATIZER_MODEL_ENTRY_NAME];
        }
        else
        {
            return null;
        }
    }

    protected override Type DefaultFactory => typeof(LemmatizerFactory);

    public virtual LemmatizerFactory GetFactory()
    {
        return (LemmatizerFactory)toolFactory;
    }
}
