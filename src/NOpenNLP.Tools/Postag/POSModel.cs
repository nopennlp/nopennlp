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

using NOpenNLP.Tools.Ml;
using NOpenNLP.Tools.Ml.Model;
using NOpenNLP.Tools.Util;
using NOpenNLP.Tools.Util.Model;
using NOpenNLP.Tools.Support;
using System;
using System.IO;
using System.Collections.Generic;

namespace NOpenNLP.Tools.Postag;

/// <summary>
/// The <see cref="POSModel"/> is the model used
/// by a learnable <see cref="IPOSTagger"/>.
/// </summary>
/// <seealso cref="POSTaggerME"/>
public sealed class POSModel : BaseModel, ISerializableArtifact
{
    private const string COMPONENT_NAME = "POSTaggerME";
    internal const string POS_MODEL_ENTRY_NAME = "pos.model";
    internal const string GENERATOR_DESCRIPTOR_ENTRY_NAME = "generator.featuregen";

    // NOpenNLP: these constructors are only used when training a new model,
    // which is not supported; we only support inference of existing models.
    // public POSModel(string languageCode, ISequenceClassificationModel<string> posModel, Dictionary<string, string> manifestInfoEntries, POSTaggerFactory posFactory) : base(COMPONENT_NAME, languageCode, manifestInfoEntries, posFactory)
    // {
    //     artifactMap.Put(POS_MODEL_ENTRY_NAME, posModel ?? throw new ArgumentNullException(nameof(posModel)));
    //     artifactMap.Put(GENERATOR_DESCRIPTOR_ENTRY_NAME, posFactory.GetFeatureGenerator());
    //     foreach (Map.Entry<String, Object> resource in posFactory.GetResources().EntrySet())
    //     {
    //         artifactMap.Put(resource.GetKey(), resource.GetValue());
    //     } // TODO: This fails probably for the sequence model ... ?!
    //     // checkArtifactMap();
    // }

    // public POSModel(string languageCode, IMaxentModel posModel, Dictionary<string, string> manifestInfoEntries, POSTaggerFactory posFactory) : this(languageCode, posModel, POSTaggerME.DEFAULT_BEAM_SIZE, manifestInfoEntries, posFactory)
    // {
    // }

    // public POSModel(string languageCode, IMaxentModel posModel, int beamSize, Dictionary<string, string> manifestInfoEntries, POSTaggerFactory posFactory) : base(COMPONENT_NAME, languageCode, manifestInfoEntries, posFactory)
    // {
    //     if (posModel is null)
    //     {
    //         throw new ArgumentNullException(nameof(posModel));
    //     }
//
    //     Properties manifest = (Properties)artifactMap[MANIFEST_ENTRY];
    //     manifest.SetProperty(BeamSearch.BEAM_SIZE_PARAMETER, Convert.ToString(beamSize));
    //     artifactMap.Put(POS_MODEL_ENTRY_NAME, posModel);
    //     artifactMap.Put(GENERATOR_DESCRIPTOR_ENTRY_NAME, posFactory.GetFeatureGenerator());
    //     foreach (Map.Entry<String, Object> resource in posFactory.GetResources().EntrySet())
    //     {
    //         artifactMap.Put(resource.GetKey(), resource.GetValue());
    //     }
//
    //     CheckArtifactMap();
    // }

    public POSModel(Stream @in) : base(COMPONENT_NAME, @in)
    {
    }

    public POSModel(FileInfo modelFile) : base(COMPONENT_NAME, modelFile)
    {
    }

    // NOpenNLP: the Path and URL overloads have no .NET equivalent in BaseModel.
    // public POSModel(Path modelPath) : this(modelPath.ToFile())
    // {
    // }

    // public POSModel(URL modelURL) : base(COMPONENT_NAME, modelURL)
    // {
    // }

    protected override Type DefaultFactory => typeof(POSTaggerFactory);

    protected override void ValidateArtifactMap()
    {
        base.ValidateArtifactMap();
        if (!(artifactMap[POS_MODEL_ENTRY_NAME] is IMaxentModel))
        {
            throw new InvalidFormatException("POS model is incomplete!");
        }
    }

    /// <summary>
    /// </summary>
    /// <remarks>
    /// Deprecated: Use getPosSequenceModel instead. This method will be removed soon.
    /// Only required for Parser 1.5.x backward compatibility. Newer models don't need this anymore.
    /// </remarks>
    public IMaxentModel GetPosModel()
    {
        if (artifactMap[POS_MODEL_ENTRY_NAME] is IMaxentModel)
        {
            return (IMaxentModel)artifactMap[POS_MODEL_ENTRY_NAME];
        }
        else
        {
            return null;
        }
    }

    public ISequenceClassificationModel<string> GetPosSequenceModel()
    {
        Properties manifest = (Properties)artifactMap[MANIFEST_ENTRY];
        if (artifactMap[POS_MODEL_ENTRY_NAME] is IMaxentModel)
        {
            string beamSizeString = manifest.GetProperty(BeamSearch.BEAM_SIZE_PARAMETER);
            int beamSize = POSTaggerME.DEFAULT_BEAM_SIZE;
            if (beamSizeString != null)
            {
                beamSize = int.Parse(beamSizeString);
            }

            return new BeamSearch<string>(beamSize, (IMaxentModel)artifactMap[POS_MODEL_ENTRY_NAME]);
        }
        else if (artifactMap[POS_MODEL_ENTRY_NAME] is ISequenceClassificationModel<string>)
        {
            return (ISequenceClassificationModel<string>)artifactMap[POS_MODEL_ENTRY_NAME];
        }
        else
        {
            return null;
        }
    }

    public POSTaggerFactory GetFactory()
    {
        return (POSTaggerFactory)this.toolFactory;
    }

    public override void CreateArtifactSerializers(IDictionary<string, IArtifactSerializer> serializers)
    {
        base.CreateArtifactSerializers(serializers);
        serializers.Put("featuregen", new ByteArraySerializer());
    }

    /// <summary>
    /// Retrieves the ngram dictionary.
    /// </summary>
    /// <returns>ngram dictionary or null if not used</returns>
    public NOpenNLP.Tools.Dictionary.Dictionary GetNgramDictionary()
    {
        if (GetFactory() != null)
            return GetFactory().GetDictionary();
        return null;
    }

    public Type GetArtifactSerializerClass()
    {
        return typeof(POSModelSerializer);
    }
}
