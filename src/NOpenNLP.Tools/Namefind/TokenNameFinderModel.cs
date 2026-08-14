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

// This file has been modified from the original Apache OpenNLP 1.9.4 source:
// translated from Java to C# and adapted for .NET. See NOTICE.

using NOpenNLP.Tools.Ml;
using NOpenNLP.Tools.Ml.Model;
using NOpenNLP.Tools.Util;
using NOpenNLP.Tools.Util.Model;
using NOpenNLP.Tools.Util.Featuregen;
using System;
using System.Collections.Generic;
using System.IO;
using NOpenNLP.Tools.Support;

namespace NOpenNLP.Tools.Namefind;

/// <summary>
/// The <see cref="TokenNameFinderModel"/> is the model used
/// by a learnable <see cref="ITokenNameFinder"/>.
/// </summary>
/// <seealso cref="NameFinderME"/>
// TODO: Fix the model validation, on loading via constructors and input streams
public class TokenNameFinderModel : BaseModel
{
    public class FeatureGeneratorCreationError : Exception
    {
        internal FeatureGeneratorCreationError(Exception t) : base(null, t)
        {
        }
    }

    private const string COMPONENT_NAME = "NameFinderME";
    private const string MAXENT_MODEL_ENTRY_NAME = "nameFinder.model";
    internal const string GENERATOR_DESCRIPTOR_ENTRY_NAME = "generator.featuregen";
    internal const string SEQUENCE_CODEC_CLASS_NAME_PARAMETER = "sequenceCodecImplName";

    public TokenNameFinderModel(string languageCode,
        ISequenceClassificationModel<string> nameFinderModel,
        byte[] generatorDescriptor,
        IDictionary<string, object> resources,
        IDictionary<string, string> manifestInfoEntries,
        ISequenceCodec<string> seqCodec,
        TokenNameFinderFactory factory)
        : base(COMPONENT_NAME, languageCode, manifestInfoEntries, factory)
    {
        Init(nameFinderModel, generatorDescriptor, resources, manifestInfoEntries, seqCodec);
        if (!seqCodec.AreOutcomesCompatible(nameFinderModel.Outcomes))
        {
            throw new ArgumentException("Model not compatible with name finder!");
        }
    }

    public TokenNameFinderModel(string languageCode,
        IMaxentModel nameFinderModel,
        int beamSize,
        byte[]? generatorDescriptor,
        IDictionary<string, object> resources,
        IDictionary<string, string> manifestInfoEntries,
        ISequenceCodec<string> seqCodec,
        TokenNameFinderFactory factory)
        : base(COMPONENT_NAME, languageCode, manifestInfoEntries, factory)
    {
        Properties manifest = (Properties)artifactMap[MANIFEST_ENTRY];
        manifest.Put(BeamSearch.BEAM_SIZE_PARAMETER, beamSize.ToString());
        Init(nameFinderModel, generatorDescriptor, resources, manifestInfoEntries, seqCodec);
        if (!IsModelValid(nameFinderModel))
        {
            throw new ArgumentException("Model not compatible with name finder!");
        }
    }

    // TODO: Extend this one with beam size!
    public TokenNameFinderModel(string languageCode,
        IMaxentModel nameFinderModel,
        byte[]? generatorDescriptor,
        IDictionary<string, object> resources,
        IDictionary<string, string> manifestInfoEntries)
        : this(languageCode, nameFinderModel, NameFinderME.DEFAULT_BEAM_SIZE, generatorDescriptor, resources, manifestInfoEntries, new BioCodec(), new TokenNameFinderFactory())
    {
    }

    public TokenNameFinderModel(string languageCode,
        IMaxentModel nameFinderModel,
        IDictionary<string, object> resources,
        IDictionary<string, string> manifestInfoEntries)
        : this(languageCode, nameFinderModel, null, resources, manifestInfoEntries)
    {
    }

    public TokenNameFinderModel(Stream @in)
        : base(COMPONENT_NAME, @in)
    {
    }

    public TokenNameFinderModel(FileInfo modelFile)
        : base(COMPONENT_NAME, modelFile)
    {
    }

    public TokenNameFinderModel(string modelPath)
        : this(new FileInfo(modelPath))
    {
    }

    // public TokenNameFinderModel(URL modelURL)
    //     : base(COMPONENT_NAME, modelURL)
    // {
    // }

    private void Init(object nameFinderModel,
        byte[]? generatorDescriptor,
        IDictionary<string, object>? resources,
        IDictionary<string, string> manifestInfoEntries,
        ISequenceCodec<string> seqCodec)
    {
        Properties manifest = (Properties)artifactMap[MANIFEST_ENTRY];
        manifest.Put(SEQUENCE_CODEC_CLASS_NAME_PARAMETER, seqCodec.GetType().FullName);
        artifactMap.Put(MAXENT_MODEL_ENTRY_NAME, nameFinderModel);
        if (generatorDescriptor is { Length: > 0 })
            artifactMap.Put(GENERATOR_DESCRIPTOR_ENTRY_NAME, generatorDescriptor);
        if (resources != null)
        {

            // The resource map must not contain key which are already taken
            // like the name finder maxent model name
            if (resources.ContainsKey(MAXENT_MODEL_ENTRY_NAME) || resources.ContainsKey(GENERATOR_DESCRIPTOR_ENTRY_NAME))
            {
                throw new ArgumentException();
            }


            // TODO: Add checks to not put resources where no serializer exists,
            // make that case fail here, should be done in the BaseModel
            artifactMap.PutAll(resources);
        }

        CheckArtifactMap();
    }

    public virtual ISequenceClassificationModel<string>? NameFinderSequenceModel
    {
        get
        {
            Properties manifest = (Properties)artifactMap[MANIFEST_ENTRY];
            if (artifactMap[MAXENT_MODEL_ENTRY_NAME] is IMaxentModel)
            {
                string? beamSizeString = manifest.GetProperty(BeamSearch.BEAM_SIZE_PARAMETER);
                int beamSize = NameFinderME.DEFAULT_BEAM_SIZE;
                if (beamSizeString != null)
                {
                    beamSize = int.Parse(beamSizeString);
                }

                return new BeamSearch<string>(beamSize, (IMaxentModel)artifactMap[MAXENT_MODEL_ENTRY_NAME]);
            }
            else
            {
                return artifactMap[MAXENT_MODEL_ENTRY_NAME] as ISequenceClassificationModel<string>;
            }
        }
    }

    protected override Type DefaultFactory => typeof(TokenNameFinderFactory);

    public virtual ISequenceCodec<string> SequenceCodec => Factory.SequenceCodec;

    public virtual TokenNameFinderFactory Factory => (TokenNameFinderFactory)toolFactory;

    public override void CreateArtifactSerializers(IDictionary<string, IArtifactSerializer> serializers)
    {
        base.CreateArtifactSerializers(serializers);
        serializers.Put("featuregen", new ByteArraySerializer());
    }

    /// <summary>
    /// Create the artifact serializers. Currently for serializers related to
    /// features that require external resources, such as <c>W2VClassesDictionary</c>
    /// objects, the convention is to add its element tag name as key of the serializer map.
    /// For example, the element tag name for the <c>WordClusterFeatureGenerator</c> which
    /// uses <c>W2VClassesDictionary</c> objects serialized by the <c>W2VClassesDictionarySerializer</c>
    /// is 'wordcluster', which is the key used to add the serializer to the map.
    /// </summary>
    /// <returns>the map containing the added serializers</returns>
    public new static IDictionary<string, IArtifactSerializer> CreateArtifactSerializers()
    {

        // TODO: Not so nice, because code cannot really be reused by the other create serializer method
        //       Has to be redesigned, we need static access to default serializers
        //       and these should be able to extend during runtime ?!
        //
        //       The XML feature generator factory should provide these mappings.
        //       Usually the feature generators should know what type of resource they expect.
        var serializers = BaseModel.CreateArtifactSerializers();
        serializers.Put("featuregen", new ByteArraySerializer());
        serializers.Put("wordcluster", new WordClusterDictionary.WordClusterDictionarySerializer());
        serializers.Put("brownclustertoken", new BrownCluster.BrownClusterSerializer());
        serializers.Put("brownclustertokenclass", new BrownCluster.BrownClusterSerializer());
        serializers.Put("brownclusterbigram", new BrownCluster.BrownClusterSerializer());
        return serializers;
    }

    private bool IsModelValid(IMaxentModel model)
    {
        string[] outcomes = new string[model.NumOutcomes];

        for (int i = 0; i < model.NumOutcomes; i++)
        {
            outcomes[i] = model.GetOutcome(i);
        }

        return Factory.CreateSequenceCodec().AreOutcomesCompatible(outcomes);
    }

    protected override void ValidateArtifactMap()
    {
        base.ValidateArtifactMap();

        if (artifactMap[MAXENT_MODEL_ENTRY_NAME] is not IMaxentModel and not ISequenceClassificationModel<string>)
        {
            throw new InvalidFormatException("Token Name Finder model is incomplete!");
        }
    }
}
